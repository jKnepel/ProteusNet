using jKnepel.ProteusNet.Serializing;
using System;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Packets
{
    internal class TransformPacket
    {
        [Flags]
        public enum EFlags : ushort
        {
            Nothing = 0,
            PositionX = 1,
            PositionY = 2,
            PositionZ = 4,
            PositionAll = PositionX | PositionY | PositionZ,
            RotationX = 8,
            RotationY = 16,
            RotationZ = 32,
            RotationAll = RotationX | RotationY | RotationZ,
            ScaleX = 64,
            ScaleY = 128,
            ScaleZ = 256,
            ScaleAll = ScaleX | ScaleY | ScaleZ,
            Rigidbody = 512
        }
        
        public static byte PacketType => (byte)EPacketType.Transform;
        public uint ObjectIdentifier { get; }
        public EFlags Flags { get; private set; } = 0;
        
        public float? PositionX { get; private set; }
        public float? PositionY { get; private set; }
        public float? PositionZ { get; private set; }
        
        public float? RotationX { get; private set; }
        public float? RotationY { get; private set; }
        public float? RotationZ { get; private set; }
        
        public float? ScaleX { get; private set; }
        public float? ScaleY { get; private set; }
        public float? ScaleZ { get; private set; }
        
        public Vector3? LinearVelocity { get; private set; }
        public Vector3? AngularVelocity { get; private set; }
        
        private TransformPacket(uint objectIdentifier)
        {
            ObjectIdentifier = objectIdentifier;
        }
        
        public static TransformPacket Read(Reader reader)
        {
            var packet = new TransformPacket(reader.ReadUInt32());
            packet.Flags = (EFlags)reader.ReadUInt16();
            
            if (packet.Flags.HasFlag(EFlags.PositionX))
                packet.PositionX = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.PositionY))
                packet.PositionY = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.PositionZ))
                packet.PositionZ = reader.ReadSingle();
            
            if (packet.Flags.HasFlag(EFlags.RotationX))
                packet.RotationX = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.RotationY))
                packet.RotationY = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.RotationZ))
                packet.RotationZ = reader.ReadSingle();
            
            if (packet.Flags.HasFlag(EFlags.ScaleX))
                packet.ScaleX = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.ScaleY))
                packet.ScaleY = reader.ReadSingle();
            if (packet.Flags.HasFlag(EFlags.ScaleZ))
                packet.ScaleZ = reader.ReadSingle();

            if (packet.Flags.HasFlag(EFlags.Rigidbody))
            {
                packet.LinearVelocity = reader.ReadVector3();
                packet.AngularVelocity = reader.ReadVector3();
            }

            return packet;
        }

        public static void Write(Writer writer, TransformPacket packet)
        {
            writer.WriteUInt32(packet.ObjectIdentifier);
            writer.WriteUInt16((ushort)packet.Flags);

            if (packet.Flags.HasFlag(EFlags.PositionX))
            {
                Debug.Assert(packet.PositionX != null, "PositionX is null and included in Flags");
                writer.WriteSingle((float)packet.PositionX);
            }
            if (packet.Flags.HasFlag(EFlags.PositionY))
            {
                Debug.Assert(packet.PositionY != null, "PositionY is null and included in Flags");
                writer.WriteSingle((float)packet.PositionY);
            }
            if (packet.Flags.HasFlag(EFlags.PositionZ))
            {
                Debug.Assert(packet.PositionZ != null, "PositionZ is null and included in Flags");
                writer.WriteSingle((float)packet.PositionZ);
            }
            
            if (packet.Flags.HasFlag(EFlags.RotationX))
            {
                Debug.Assert(packet.RotationX != null, "RotationX is null and included in Flags");
                writer.WriteSingle((float)packet.RotationX);
            }
            if (packet.Flags.HasFlag(EFlags.RotationY))
            {
                Debug.Assert(packet.RotationY != null, "RotationY is null and included in Flags");
                writer.WriteSingle((float)packet.RotationY);
            }
            if (packet.Flags.HasFlag(EFlags.RotationZ))
            {
                Debug.Assert(packet.RotationZ != null, "RotationZ is null and included in Flags");
                writer.WriteSingle((float)packet.RotationZ);
            }
            
            if (packet.Flags.HasFlag(EFlags.ScaleX))
            {
                Debug.Assert(packet.ScaleX != null, "ScaleX is null and included in Flags");
                writer.WriteSingle((float)packet.ScaleX);
            }
            if (packet.Flags.HasFlag(EFlags.ScaleY))
            {
                Debug.Assert(packet.ScaleY != null, "ScaleY is null and included in Flags");
                writer.WriteSingle((float)packet.ScaleY);
            }
            if (packet.Flags.HasFlag(EFlags.ScaleZ))
            {
                Debug.Assert(packet.ScaleZ != null, "ScaleZ is null and included in Flags");
                writer.WriteSingle((float)packet.ScaleZ);
            }

            if (packet.Flags.HasFlag(EFlags.Rigidbody))
            {
                Debug.Assert(packet.LinearVelocity != null && packet.AngularVelocity != null, "Rigidbody velocities are null and included in Flags");
                writer.WriteVector3((Vector3)packet.LinearVelocity);
                writer.WriteVector3((Vector3)packet.AngularVelocity);
            }
        }
        
        public class Builder
        {
            private readonly TransformPacket _packet;

            public Builder(uint objectIdentifier)
            {
                _packet = new(objectIdentifier);
            }

            public Builder WithPositionX(float x)
            {
                _packet.PositionX = x;
                _packet.Flags |= EFlags.PositionX;
                return this;
            }
            
            public Builder WithPositionY(float y)
            {
                _packet.PositionY = y;
                _packet.Flags |= EFlags.PositionY;
                return this;
            }
            
            public Builder WithPositionZ(float z)
            {
                _packet.PositionZ = z;
                _packet.Flags |= EFlags.PositionZ;
                return this;
            }

            public Builder WithRotationX(float x)
            {
                _packet.RotationX = x;
                _packet.Flags |= EFlags.RotationX;
                return this;
            }
            
            public Builder WithRotationY(float y)
            {
                _packet.RotationY = y;
                _packet.Flags |= EFlags.RotationY;
                return this;
            }
            
            public Builder WithRotationZ(float z)
            {
                _packet.RotationZ = z;
                _packet.Flags |= EFlags.RotationZ;
                return this;
            }
            
            public Builder WithScaleX(float x)
            {
                _packet.ScaleX = x;
                _packet.Flags |= EFlags.ScaleX;
                return this;
            }
            
            public Builder WithScaleY(float y)
            {
                _packet.ScaleY = y;
                _packet.Flags |= EFlags.ScaleY;
                return this;
            }
            
            public Builder WithScaleZ(float z)
            {
                _packet.ScaleZ = z;
                _packet.Flags |= EFlags.ScaleZ;
                return this;
            }
            
            public Builder WithRigidbody(Vector3 linearVelocity, Vector3 angularVelocity)
            {
                _packet.LinearVelocity = linearVelocity;
                _packet.AngularVelocity = angularVelocity;
                _packet.Flags |= EFlags.Rigidbody;
                return this;
            }

            public TransformPacket Build() => _packet;
        }
    }
}
