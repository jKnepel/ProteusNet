using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public sealed partial class UnityTransport
    {
        private readonly struct SendTarget : IEquatable<SendTarget>
        {
            public readonly NetworkConnection Connection;
            public readonly NetworkPipeline Pipeline;
            
            public SendTarget(NetworkConnection conn, NetworkPipeline pipe)
            {
                Connection = conn;
                Pipeline = pipe;
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
        
        // TODO : implement batching
        private struct SendQueue : IDisposable
        {
            private NativeQueue<NativeArray<byte>> _messages;

            public int Count => _messages.Count;

            public SendQueue(int i)
            {
                _messages = new(Allocator.Persistent);
            }

            public void Dispose()
            {
                if (_messages.IsCreated)
                    _messages.Dispose();
            }

            public void Enqueue(NativeArray<byte> message)
            {
                _messages.Enqueue(message);
            }

            public NativeArray<byte> Dequeue()
            {
                return _messages.Dequeue();
            }

            public NativeArray<byte> Peek()
            {
                return _messages.Peek();
            }
        }

        /*
         * TODO : implement once message batching works
        [BurstCompile]
        private struct SendQueueJob : IJob
        {
            public NetworkDriver.Concurrent Driver;
            public SendTarget Target;
            public SendQueue Queue;
            
            public void Execute()
            {
                while (Queue.Count > 0)
                {
                    var result = Driver.BeginSend(Target.Pipeline, Target.Connection, out var writer);
                    if (result != (int)StatusCode.Success)
                    {
                        Debug.LogError($"Sending data failed: {result}");
                        return;
                    }

                    var data = Queue.Peek();
                    writer.WriteBytes(data);
                    result = Driver.EndSend(writer);

                    if (result == data.Length)
                    {
                        Queue.Dequeue();
                        continue;
                    }

                    if (result != (int)StatusCode.NetworkSendQueueFull)
                    {
                        Debug.LogError("Error sending a message!");
                        Queue.Dequeue();
                    }

                    return;
                }
            }
        }
        */
    }
}
