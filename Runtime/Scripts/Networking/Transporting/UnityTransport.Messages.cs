// Copyright (c) 2021 Unity Technologies
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// License: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/develop/com.unity.netcode.gameobjects/LICENSE.md

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public sealed partial class UnityTransport
    {
        private readonly struct SendTarget : IEquatable<SendTarget>
        {
            public readonly NetworkConnection Connection;
            public readonly NetworkPipeline Pipeline;
            public readonly bool IsReliable;
            
            public SendTarget(NetworkConnection conn, NetworkPipeline pipe, bool isReliable)
            {
                Connection = conn;
                Pipeline = pipe;
                IsReliable = isReliable;
            }
            
            public bool Equals(SendTarget other)
            {
                return other.Connection.Equals(Connection) && other.Pipeline.Equals(Pipeline);
            }

            public override bool Equals(object obj)
            {
                return obj is SendTarget other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Connection, Pipeline);
            }
        }
        
        private struct SendQueue : IDisposable
        {
            private NativeList<byte> _data;
            private NativeArray<int> _headTailIndices;
            private readonly int _maxCapacity;
            private readonly int _minCapacity;

            public const int MESSAGE_OVERHEAD = sizeof(int);

            private const int HEAD_INTERNAL_INDEX = 0;
            private const int TAIL_INTERNAL_INDEX = 1;

            /// <summary>Index of the first byte of the oldest data in the queue.</summary>
            private int HeadIndex
            {
                get => _headTailIndices[HEAD_INTERNAL_INDEX];
                set => _headTailIndices[HEAD_INTERNAL_INDEX] = value;
            }
            
            /// <summary>Index one past the last byte of the most recent data in the queue.</summary>
            private int TailIndex
            {
                get => _headTailIndices[TAIL_INTERNAL_INDEX];
                set => _headTailIndices[TAIL_INTERNAL_INDEX] = value;
            }

            public int Length => TailIndex - HeadIndex;
            public int Capacity => _data.Length;
            public bool IsEmpty => HeadIndex == TailIndex;
            public bool IsCreated => _data.IsCreated;

            public SendQueue(int capacity)
            {
                // Make sure the maximum capacity will be even.
                _maxCapacity = capacity + (capacity & 1);
                
                // We pick the minimum capacity such that if we keep doubling it, we'll eventually hit
                // the maximum capacity exactly. The alternative would be to use capacities that are
                // powers of 2, but this can lead to over-allocating quite a bit of memory (especially
                // since we expect maximum capacities to be in the megabytes range). The approach taken
                // here avoids this issue, at the cost of not having allocations of nice round sizes.
                _minCapacity = _maxCapacity;
                while (_minCapacity / 2 >= 4096)
                    _minCapacity /= 2;

                _data = new(_minCapacity, Allocator.Persistent);
                _headTailIndices = new(2, Allocator.Persistent);
                _data.ResizeUninitialized(_minCapacity);

                HeadIndex = TailIndex = 0;
            }

            public void Dispose()
            {
                if (IsCreated)
                {
                    _data.Dispose();
                    _headTailIndices.Dispose();
                }
            }

            /// <summary>Append data at the tail of the queue. No safety checks.</summary>
            private void AppendDataAtTail(ArraySegment<byte> data)
            {
                unsafe
                {
                    var writer = new DataStreamWriter(_data.GetUnsafePtr() + TailIndex, Capacity - TailIndex);
                    writer.WriteInt(data.Count);

                    fixed (byte* dataPtr = data.Array)
                        Write(ref writer, dataPtr + data.Offset, data.Count);
                }

                TailIndex += MESSAGE_OVERHEAD + data.Count;
            }

            /// <summary>Append a new message to the queue.</summary>
            /// <param name="message">Message to append to the queue.</param>
            /// <returns>
            /// Whether the message was appended successfully. The only way it can fail is if there's
            /// no more room in the queue. On failure, nothing is written to the queue.
            /// </returns>
            public bool PushMessage(ArraySegment<byte> message)
            {
                if (!IsCreated) return false;
                
                // Check if there's enough room after the current tail index.
                if (Capacity - TailIndex >= MESSAGE_OVERHEAD + message.Count)
                {
                    AppendDataAtTail(message);
                    return true;
                }

                // Move the data at the beginning of of m_Data. Either it will leave enough space for
                // the message, or we'll grow m_Data and will want the data at the beginning anyway.
                if (HeadIndex > 0 && Length > 0)
                {
                    unsafe
                    {
                        UnsafeUtility.MemMove(_data.GetUnsafePtr(), _data.GetUnsafePtr() + HeadIndex, Length);
                    }

                    TailIndex = Length;
                    HeadIndex = 0;
                }

                // If there's enough space left at the end for the message, now is a good time to trim
                // the capacity of m_Data if it got very large. We define "very large" here as having
                // more than 75% of m_Data unused after adding the new message.
                if (Capacity - TailIndex >= MESSAGE_OVERHEAD + message.Count)
                {
                    AppendDataAtTail(message);
                    while (TailIndex < Capacity / 4 && Capacity > _minCapacity)
                        _data.ResizeUninitialized(Capacity / 2);
                    return true;
                }

                // If we get here we need to grow m_Data until the data fits (or it's too large).
                while (Capacity - TailIndex < MESSAGE_OVERHEAD + message.Count)
                {   // Can't grow m_Data anymore. Message simply won't fit.
                    if (Capacity * 2 > _maxCapacity)
                        return false;
                    _data.ResizeUninitialized(Capacity * 2);
                }
                
                // If we get here we know there's now enough room for the message.
                AppendDataAtTail(message);
                return true;
            }

            /// <summary>
            /// Fill as much of a <see cref="DataStreamWriter"/> as possible with data from the head of
            /// the queue. Only full messages (and their length) are written to the writer.
            /// </summary>
            /// <remarks>
            /// This does NOT actually consume anything from the queue. That is, calling this method
            /// does not reduce the length of the queue. Callers are expected to call
            /// <see cref="Consume"/> with the value returned by this method afterwards if the data can
            /// be safely removed from the queue (e.g. if it was sent successfully).
            ///
            /// This method should not be used together with <see cref="FillWriterWithBytes"/> since this
            /// could lead to a corrupted queue.
            /// </remarks>
            /// <param name="writer">The <see cref="DataStreamWriter"/> to write to.</param>
            /// <param name="softMaxBytes">
            /// Maximum number of bytes to copy (0 means writer capacity). This is a soft limit only.
            /// If a message is larger than that but fits in the writer, it will be written. In effect,
            /// this parameter is the maximum size that small messages can be coalesced together.
            /// </param>
            /// <returns>How many bytes were written to the writer.</returns>
            public int FillWriterWithMessages(ref DataStreamWriter writer, int softMaxBytes = 0)
            {
                if (!IsCreated || Length == 0)
                    return 0;

                softMaxBytes = softMaxBytes == 0 ? writer.Capacity : Math.Min(softMaxBytes, writer.Capacity);

                unsafe
                {
                    var reader = new DataStreamReader(_data.AsArray());
                    var readerOffset = HeadIndex;
                    
                    reader.SeekSet(readerOffset);
                    var messageLength = reader.ReadInt();
                    var bytesToWrite = messageLength + MESSAGE_OVERHEAD;

                    if (bytesToWrite > softMaxBytes && bytesToWrite <= writer.Capacity)
                    {
                        writer.WriteInt(messageLength);
                        Write(ref writer, _data.GetUnsafePtr() + reader.GetBytesRead(), messageLength);
                        return bytesToWrite;
                    }

                    var bytesWritten = 0;

                    while (readerOffset < TailIndex)
                    {
                        reader.SeekSet(readerOffset);
                        messageLength = reader.ReadInt();
                        bytesToWrite = messageLength + MESSAGE_OVERHEAD;

                        if (bytesWritten + bytesToWrite <= softMaxBytes)
                        {
                            writer.WriteInt(messageLength);
                            Write(ref writer, _data.GetUnsafePtr() + reader.GetBytesRead(), messageLength);

                            readerOffset += bytesToWrite;
                            bytesWritten += bytesToWrite;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return bytesWritten;
                }
            }

            /// <summary>
            /// Fill the given <see cref="DataStreamWriter"/> with as many bytes from the queue as
            /// possible, disregarding message boundaries.
            /// </summary>
            /// <remarks>
            /// This does NOT actually consume anything from the queue. That is, calling this method
            /// does not reduce the length of the queue. Callers are expected to call
            /// <see cref="Consume"/> with the value returned by this method afterwards if the data can
            /// be safely removed from the queue (e.g. if it was sent successfully).
            ///
            /// This method should not be used together with <see cref="FillWriterWithMessages"/> since
            /// this could lead to reading messages from a corrupted queue.
            /// </remarks>
            /// <param name="writer">The <see cref="DataStreamWriter"/> to write to.</param>
            /// <param name="maxBytes">Max number of bytes to copy (0 means writer capacity).</param>
            /// <returns>How many bytes were written to the writer.</returns>
            public int FillWriterWithBytes(ref DataStreamWriter writer, int maxBytes = 0)
            {
                if (!IsCreated || Length == 0)
                    return 0;

                var maxLength = maxBytes == 0 ? writer.Capacity : Math.Min(maxBytes, writer.Capacity);
                var copyLength = Math.Min(maxLength, Length);

                unsafe
                {
                    Write(ref writer, _data.GetUnsafePtr() + HeadIndex, copyLength);
                }

                return copyLength;
            }
            
            /// <summary>Consume a number of bytes from the head of the queue.</summary>
            /// <remarks>
            /// This should only be called with a size that matches the last value returned by
            /// <see cref="FillWriter"/>. Anything else will result in a corrupted queue.
            /// </remarks>
            /// <param name="size">Number of bytes to consume from the queue.</param>
            public void Consume(int size)
            {
                // Adjust the head/tail indices such that we consume the given size.
                if (size >= Length)
                {
                    HeadIndex = 0;
                    TailIndex = 0;
                    
                    // This is a no-op if m_Data is already at minimum capacity.
                    _data.ResizeUninitialized(_minCapacity);
                }
                else
                {
                    HeadIndex += size;
                }
            }
            
            private static unsafe void Write(ref DataStreamWriter writer, byte* data, int length)
            {
                writer.WriteBytesUnsafe(data, length);
            }
        }

        private struct ReceiveQueue
        {
            private byte[] _data;
            private int _offset;
            private int _length;
            
            public const int MESSAGE_OVERHEAD = sizeof(int);

            public bool IsEmpty => _length <= 0;

            public ReceiveQueue(DataStreamReader reader)
            {
                _data = new byte[reader.Length];
                unsafe
                {
                    fixed (byte* dataPtr = _data)
                        reader.ReadBytesUnsafe(dataPtr, reader.Length);
                }

                _offset = 0;
                _length = reader.Length;
            }

            /// <summary>
            /// Push the entire data from a <see cref="DataStreamReader"/> (as returned by popping an
            /// event from a <see cref="NetworkDriver"/>) to the queue.
            /// </summary>
            /// <param name="reader">The <see cref="DataStreamReader"/> to push the data of.</param>
            public void PushReader(DataStreamReader reader)
            {
                // Resize the array and copy the existing data to the beginning if there's not enough
                // room to copy the reader's data at the end of the existing data.
                var available = _data.Length - (_offset + _length);
                if (available < reader.Length)
                {
                    if (_length > 0)
                        Array.Copy(_data, _offset, _data, 0, _length);

                    _offset = 0;
                    while (_data.Length - _length < reader.Length)
                        Array.Resize(ref _data, _data.Length * 2);
                }

                unsafe
                {
                    fixed (byte* dataPtr = _data)
                        reader.ReadBytesUnsafe(dataPtr + _offset + _length, reader.Length);
                }

                _length += reader.Length;
            }

            /// <summary>Pop the next full message in the queue.</summary>
            /// <returns>The message, or the default value if no more full messages.</returns>
            public ArraySegment<byte> PopMessage()
            {
                if (_length < MESSAGE_OVERHEAD)
                    return default;

                var messageLength = BitConverter.ToInt32(_data, _offset);
                if (_length - MESSAGE_OVERHEAD < messageLength)
                    return default;

                var data = new ArraySegment<byte>(_data, _offset + MESSAGE_OVERHEAD, messageLength);
                _offset += MESSAGE_OVERHEAD + messageLength;
                _length -= MESSAGE_OVERHEAD + messageLength;

                return data;
            }
        }

        [BurstCompile]
        private struct SendQueueJob : IJob
        {
            public NetworkDriver.Concurrent Driver;
            public SendTarget Target;
            public SendQueue Queue;
            
            public void Execute()
            {
                while (!Queue.IsEmpty)
                {
                    var result = Driver.BeginSend(Target.Pipeline, Target.Connection, out var writer);
                    if (result != (int)StatusCode.Success)
                    {
                        Debug.LogError($"Error sending message: {ParseStatusCode(result)}");
                        return;
                    }
                    
                    var written = Target.IsReliable 
                        ? Queue.FillWriterWithBytes(ref writer)
                        : Queue.FillWriterWithMessages(ref writer);

                    result = Driver.EndSend(writer);
                    if (result == written)
                    {
                        Queue.Consume(written);
                        continue;
                    }

                    if (result != (int)StatusCode.NetworkSendQueueFull)
                    {
                        Debug.LogError($"Error sending message: {ParseStatusCode(result)}");
                        Queue.Consume(written);
                    }

                    return;
                }
            }
        }
    }
}
