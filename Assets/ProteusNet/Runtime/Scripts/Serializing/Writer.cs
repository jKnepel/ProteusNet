using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace jKnepel.ProteusNet.Serializing
{
    public class Writer
    {
		#region fields

		/// <summary>
		/// The current byte position of the writer header within the buffer.
		/// </summary>
		/// <remarks>
		/// Do not use this position unless you know what you are doing! 
		/// Setting the position manually will not check for buffer bounds or update the length of the written buffer.
		/// </remarks>
		public int Position { get; set; }
		/// <summary>
		/// The highest byte position to which the writer has written a value.
		/// </summary>
		public int Length { get; set; }
		/// <summary>
		/// The max capacity of the internal buffer.
		/// </summary>
		public int Capacity => _buffer.Length;

		/// <summary>
		/// The settings used by the writer.
		/// </summary>
		public readonly SerializerSettings Settings;

		private byte[] _buffer = new byte[32];

		private static readonly ConcurrentDictionary<Type, Action<Writer, object>> _typeHandlerCache = new();
        private static readonly HashSet<Type> _unknownTypes = new();

		#endregion

		#region lifecycle

		public Writer(SerializerSettings settings = null)
		{
			Settings = settings ?? new();
		}

		#endregion

		#region automatic type handler

		public void Write<T>(T val)
        {
            var type = typeof(T);
            Write(val, type);
        }

        private void Write<T>(T val, Type type)
		{
            if (!_unknownTypes.Contains(type))
            {   
                if (WriteBuildInType(val))
					return;

                if (WriteCustomType(type, out var customHandler))
                {   // use custom type handler if user defined method was found
                    customHandler(this, val);
                    return;
                }

                // save types that don't have any a type handler and need to be recursively serialized
                _unknownTypes.Add(type);
            }

            // recursively serialize type if no handler is found
            // TODO : circular dependencies will cause crash
            // TODO : add attributes for serialisation
            // TODO : add serialisation options to handle size, circular dependencies etc. 
            // TODO : handle properties
            var fieldInfos = type.GetFields();               
            if (fieldInfos.Length == 0 || fieldInfos.Any(x => x.FieldType == type))
			{
                var typeName = SerializerHelper.GetTypeName(type);
                throw new SerializeNotImplemented($"No write method implemented for the type {typeName}!"
                    + $" Implement a Write{typeName} method or use an extension method in the parent type!");
			}

            foreach (var fieldInfo in fieldInfos)
                Write(fieldInfo.GetValue(val), fieldInfo.FieldType);
        }

        private bool WriteBuildInType<T>(T val)
        {
	        switch (val)
	        {
		        case bool boolValue:
			        WriteBoolean(boolValue);
			        return true;
		        case byte byteValue:
			        WriteByte(byteValue);
			        return true;
		        case sbyte sbyteValue:
			        WriteSByte(sbyteValue);
			        return true;
		        case ushort ushortValue:
			        WriteUInt16(ushortValue);
			        return true;
		        case short shortValue:
			        WriteInt16(shortValue);
			        return true;
		        case uint uintValue:
			        WriteUInt32(uintValue);
			        return true;
		        case int intValue:
			        WriteInt32(intValue);
			        return true;
		        case ulong ulongValue:
			        WriteUInt64(ulongValue);
			        return true;
		        case long longValue:
			        WriteInt64(longValue);
			        return true;
		        case string stringValue:
			        WriteString(stringValue);
			        return true;
		        case char charValue:
			        WriteChar(charValue);
			        return true;
		        case float floatValue:
			        WriteSingle(floatValue);
			        return true;
		        case double doubleValue:
			        WriteDouble(doubleValue);
			        return true;
		        case decimal decimalValue:
			        WriteDecimal(decimalValue);
			        return true;
		        case Vector2 vector2Value:
			        WriteVector2(vector2Value);
			        return true;
		        case Vector3 vector3Value:
			        WriteVector3(vector3Value);
			        return true;
		        case Vector4 vector4Value:
			        WriteVector4(vector4Value);
			        return true;
		        case Matrix4x4 matrix4X4Value:
			        WriteMatrix4x4(matrix4X4Value);
			        return true;
		        case Color colorValue:
			        WriteColor(colorValue);
			        return true;
		        case Color32 color32Value:
			        WriteColor32(color32Value);
			        return true;
		        case DateTime dateTimeValue:
			        WriteDateTime(dateTimeValue);
			        return true;
		        case Array arrayValue:
		        {
			        WriteInt32(arrayValue.Length);
			        foreach (var t in arrayValue)
				        Write(t);
			        return true;
		        }
		        case IList listValue:
		        {
			        WriteInt32(listValue.Count);
			        foreach (var t in listValue)
				        Write(t);
			        return true;
		        }
		        case IDictionary dictValue:
		        {
					WriteInt32(dictValue.Count);
					var t = dictValue.GetEnumerator();
					using (t as IDisposable)
					{
						while (t.MoveNext())
						{
							Write(t.Key);
							Write(t.Value);
						}
						return true;
					}
		        }
		        default: return false;
	        }
        }

        /// <summary>
        /// Constructs and caches pre-compiled expression delegate of type handlers.
        /// </summary>
        /// <param name="type">The type of the variable for which the writer is defined</param>
        /// <param name="typeHandler">The handler of the defined type</param>
        /// <returns></returns>
        private static bool WriteCustomType(Type type, out Action<Writer, object> typeHandler)
        {   // find implemented or custom write method
            var writerMethod = type.GetMethod("Write", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (writerMethod == null)
            {
	            typeHandler = null;
                return false;
            }

            // parameters
            var instanceArg = Expression.Parameter(typeof(Writer), "instance");
            var objectArg = Expression.Parameter(typeof(object), "value");
            var castArg = Expression.Convert(objectArg, type);

            // construct handler call body
            MethodCallExpression call;
            if (writerMethod.IsGenericMethod)
			{
                var genericWriter = type.IsArray
                    ? writerMethod.MakeGenericMethod(type.GetElementType())
                    : writerMethod.MakeGenericMethod(type.GetGenericArguments());
                call = Expression.Call(genericWriter, instanceArg, castArg);
            }
            else
			{
                call = Expression.Call(writerMethod, instanceArg, castArg);
			}

            // cache delegate
            var lambda = Expression.Lambda<Action<Writer, object>>(call, instanceArg, objectArg);
            typeHandler = lambda.Compile();
            _typeHandlerCache.TryAdd(type, typeHandler);
            return true;
        }

        #endregion

        #region helpers

        private void AdjustBufferSize(int size)
		{
            if (Position + size > _buffer.Length)
                Array.Resize(ref _buffer, _buffer.Length * 2 + size);
		}

		/// <summary>
		/// Skips the writer header ahead by the given number of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public void Skip(int bytes)
		{
            AdjustBufferSize(bytes);
            Position += bytes;
            Length = Math.Max(Length, Position);
		}

		/// <summary>
		/// Reverts the writer header back by the given number of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public void Revert(int bytes)
		{
			Position -= bytes;
			Position = Math.Max(Position, 0);
		}

		/// <summary>
		/// Clears the writer buffer.
		/// </summary>
		public void Clear()
		{
            Position = 0;
            Length = 0;
		}

		/// <returns>The written buffer.</returns>
		public byte[] GetBuffer()
        {
            byte[] result = new byte[Length];
            Array.Copy(_buffer, 0, result, 0, Length);
            return result;
        }

		/// <returns>The entire internal buffer.</returns>
		public byte[] GetFullBuffer()
        {
            return _buffer;
        }

		/// <summary>
		/// Writes a specified number of bytes from a source array starting at a particular offset to the buffer.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcOffset"></param>
		/// <param name="count"></param>
		public void BlockCopy(ref byte[] src, int srcOffset, int count)
        {
			AdjustBufferSize(count);
            Buffer.BlockCopy(src, srcOffset, _buffer, Position, count);
            Position += count;
            Length = Math.Max(Length, Position);
        }

		/// <summary>
		/// Writes a source array to the buffer.
		/// </summary>
		/// <param name="src"></param>
		public void WriteByteSegment(ArraySegment<byte> src)
        {
            var srcArray = src.Array;
            BlockCopy(ref srcArray, 0, src.Count);
        }

		/// <summary>
		/// Writes a source array to the buffer.
		/// </summary>
		/// <param name="src"></param>
		public void WriteByteArray(byte[] src)
        {
            BlockCopy(ref src, 0, src.Length);
        }
		
        /// <summary>
        /// "ZigZagEncoding" based on google protocol buffers.
        /// See for <a href="https://protobuf.dev/programming-guides/encoding/">reference</a>.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ZigZagEncode(short val)
        {
	        return (ulong)((val >> 15) ^ (val << 1));
        }

        /// <summary>
        /// "ZigZagEncoding" based on google protocol buffers.
        /// See for <a href="https://protobuf.dev/programming-guides/encoding/">reference</a>.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ZigZagEncode(int val)
        {
	        return (ulong)((val >> 31) ^ (val << 1));
        }
		
        /// <summary>
        /// "ZigZagEncoding" based on google protocol buffers.
        /// See for <a href="https://protobuf.dev/programming-guides/encoding/">reference</a>.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ZigZagEncode(long val)
        {
	        return (ulong)((val >> 63) ^ (val << 1));
        }

        /// <summary>
        /// Uses a 7-bit VLQ encoding scheme based on the MIDI compression system.
        /// See for <a href="https://web.archive.org/web/20051129113105/http://www.borg.com/~jglatt/tech/midifile/vari.htm">reference</a>
        /// </summary>
        /// <param name="val"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteVLQCompression(ulong val)
        {
	        do
	        {
		        var lowerBits = (byte)(val & 0x7F);
		        val >>= 7;
		        if (val > 0)
			        lowerBits |= 0x80;
		        _buffer[Position++] = lowerBits;
	        } while (val > 0);

	        Length = Math.Max(Length, Position);
        }

        #endregion

        #region primitives

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool val)
		{
            AdjustBufferSize(1);
            _buffer[Position++] = (byte)(val ? 1 : 0);
            Length = Math.Max(Length, Position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte val)
        {
            AdjustBufferSize(1);
            _buffer[Position++] = val;
            Length = Math.Max(Length, Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByte(sbyte val)
        {
            WriteByte((byte)val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort val)
        {
	        AdjustBufferSize(2);
	        if (Settings.UseCompression)
	        {
				WriteVLQCompression(val);
	        }
	        else
	        {
	            _buffer[Position++] = (byte)val;
	            _buffer[Position++] = (byte)(val >> 8);
	            Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short val)
        {
		    AdjustBufferSize(2);
	        if (Settings.UseCompression)
	        {
		        WriteVLQCompression(ZigZagEncode(val));
	        }
	        else
	        {
		        _buffer[Position++] = (byte)val;
		        _buffer[Position++] = (byte)(val >> 8);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint val)
        {
		    AdjustBufferSize(4);
	        if (Settings.UseCompression)
	        {
		        WriteVLQCompression(val);
	        }
	        else
	        {
		        _buffer[Position++] = (byte)val;
		        _buffer[Position++] = (byte)(val >> 8);
		        _buffer[Position++] = (byte)(val >> 16);
		        _buffer[Position++] = (byte)(val >> 24);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int val)
        {
		    AdjustBufferSize(4);
	        if (Settings.UseCompression)
	        {
		        WriteVLQCompression(ZigZagEncode(val));
	        }
	        else
	        {
		        _buffer[Position++] = (byte)val;
		        _buffer[Position++] = (byte)(val >> 8);
		        _buffer[Position++] = (byte)(val >> 16);
		        _buffer[Position++] = (byte)(val >> 24);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong val)
        {
		    AdjustBufferSize(8);
	        if (Settings.UseCompression)
	        {
		        WriteVLQCompression(val);
	        }
	        else
	        {
		        _buffer[Position++] = (byte)val;
		        _buffer[Position++] = (byte)(val >> 8);
		        _buffer[Position++] = (byte)(val >> 16);
		        _buffer[Position++] = (byte)(val >> 24);
		        _buffer[Position++] = (byte)(val >> 32);
		        _buffer[Position++] = (byte)(val >> 40);
		        _buffer[Position++] = (byte)(val >> 48);
		        _buffer[Position++] = (byte)(val >> 56);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long val)
        {
		    AdjustBufferSize(8);
	        if (Settings.UseCompression)
	        {
		        WriteVLQCompression(ZigZagEncode(val));
	        }
	        else
	        {
		        _buffer[Position++] = (byte)val;
		        _buffer[Position++] = (byte)(val >> 8);
		        _buffer[Position++] = (byte)(val >> 16);
		        _buffer[Position++] = (byte)(val >> 24);
		        _buffer[Position++] = (byte)(val >> 32);
		        _buffer[Position++] = (byte)(val >> 40);
		        _buffer[Position++] = (byte)(val >> 48);
		        _buffer[Position++] = (byte)(val >> 56);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char val)
        {
            WriteUInt16(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float val)
        {
	        AdjustBufferSize(4);
	        if (Settings.UseCompression)
	        {
		        var compressed = val * Mathf.Pow(10, Settings.NumberOfDecimalPlaces);
		        WriteVLQCompression(ZigZagEncode((int)compressed));
	        }
	        else
	        {
	            TypeConverter.UIntToFloat converter = new() { Float = val };
	            var uInt = converter.UInt;
		        _buffer[Position++] = (byte)uInt;
		        _buffer[Position++] = (byte)(uInt >> 8);
		        _buffer[Position++] = (byte)(uInt >> 16);
		        _buffer[Position++] = (byte)(uInt >> 24);
		        Length = Math.Max(Length, Position);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double val)
        {
            TypeConverter.ULongToDouble converter = new() { Double = val };
            WriteUInt64(converter.ULong);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimal(decimal val)
        {
            TypeConverter.ULongsToDecimal converter = new() { Decimal = val };
            WriteUInt64(converter.ULong1);
            WriteUInt64(converter.ULong2);
        }

        #endregion

        #region unity objects

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVector2(Vector2 val)
        {
            WriteSingle(val.x);
            WriteSingle(val.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVector3(Vector3 val)
        {
            WriteSingle(val.x);
            WriteSingle(val.y);
            WriteSingle(val.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVector4(Vector4 val)
        {
            WriteSingle(val.x);
            WriteSingle(val.y);
            WriteSingle(val.z);
            WriteSingle(val.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteQuaternion(Quaternion val)
        {
	        if (Settings.UseCompression)
	        {
		        var bits = Settings.BitsPerComponent * 3 + 2;
		        var bytes = bits / 8 + 1;
		        AdjustBufferSize(bytes);
		        CompressedQuaternion q = new(val, Settings.BitsPerComponent);
		        WriteVLQCompression(q.PackedQuaternion);
	        }
	        else
	        {
	            WriteSingle(val.x);
	            WriteSingle(val.y);
	            WriteSingle(val.z);
	            WriteSingle(val.w);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMatrix4x4(Matrix4x4 val)
        {
            WriteSingle(val.m00);
            WriteSingle(val.m01);
            WriteSingle(val.m02);
            WriteSingle(val.m03);
            WriteSingle(val.m10);
            WriteSingle(val.m11);
            WriteSingle(val.m12);
            WriteSingle(val.m13);
            WriteSingle(val.m20);
            WriteSingle(val.m21);
            WriteSingle(val.m22);
            WriteSingle(val.m23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteColor(Color val)
        {
            WriteByte((byte)(val.r * 100f));
            WriteByte((byte)(val.g * 100f));
            WriteByte((byte)(val.b * 100f));
            WriteByte((byte)(val.a * 100f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteColorWithoutAlpha(Color val)
        {
            WriteByte((byte)(val.r * 100f));
            WriteByte((byte)(val.g * 100f));
            WriteByte((byte)(val.b * 100f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteColor32(Color32 val)
        {
            WriteByte(val.r);
            WriteByte(val.g);
            WriteByte(val.b);
            WriteByte(val.a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteColor32WithoutAlpha(Color32 val)
        {
            WriteByte(val.r);
            WriteByte(val.g);
            WriteByte(val.b);
        }

        #endregion

        #region objects

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                WriteByte(0);
                return;
            }

            if (val.Length > ushort.MaxValue)
                throw new FormatException($"The string can't be longer than {ushort.MaxValue}!");

            WriteUInt16((ushort)val.Length);
            var bytes = Encoding.UTF8.GetBytes(val);
            BlockCopy(ref bytes, 0, bytes.Length);
            Length = Math.Max(Length, Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArray<T>(T[] val)
		{
            if (val == null)
			{
                WriteInt32(0);
                return;
			}

            WriteInt32(val.Length);
            foreach (var t in val)
                Write(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteList<T>(List<T> val)
        {
            if (val == null)
			{
                WriteInt32(0);
                return;
			}

            WriteInt32(val.Count);
            foreach (var t in val)
                Write(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> val)
		{
            if (val == null)
            {
                WriteInt32(0);
                return;
            }

            WriteInt32(val.Count);
            foreach (var entry in val)
			{
                Write(entry.Key);
                Write(entry.Value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTime(DateTime val)
		{
            WriteInt64(val.ToBinary()); 
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteIPAddress(IPAddress val)
		{
			var bytes = val.GetAddressBytes();
			WriteByte((byte)bytes.Length);
			WriteByteArray(bytes);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteIPEndpoint(IPEndPoint val)
		{
			WriteIPAddress(val.Address);
			WriteUInt16((ushort)val.Port);
		}

		#endregion
	}
}
