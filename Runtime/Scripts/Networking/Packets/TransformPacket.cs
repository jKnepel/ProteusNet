using jKnepel.ProteusNet.Serializing;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Packets
{
    public class TransformPacket
        {
            private enum ETransformPacketFlag : byte
            {
                Position,
                Rotation,
                Scale,
                LinearVelocity,
                AngularVelocity
            }
            
            public static byte PacketType => (byte)EPacketType.Transform;
            public readonly uint ObjectIdentifier;

            public TransformPacket(uint objectIdentifier)
            {
                ObjectIdentifier = objectIdentifier;
            }
            
            public Vector3? Position { get; private set; }
            public Quaternion? Rotation { get; private set; }
            public Vector3? Scale { get; private set; }
            public Vector3? LinearVelocity { get; private set; }
            public Vector3? AngularVelocity { get; private set; }

            public int NumberOfValues { get; private set; }
            
            public static TransformPacket Read(Reader reader)
            {
                var packet = new TransformPacket(reader.ReadUInt32());
                while (reader.Remaining > 0)
                {
                    var flag = (ETransformPacketFlag)reader.ReadByte();
                    switch (flag)
                    {
                        case ETransformPacketFlag.Position:
                            packet.Position = reader.ReadVector3();
                            break;
                        case ETransformPacketFlag.Rotation:
                            packet.Rotation = reader.ReadQuaternion();
                            break;
                        case ETransformPacketFlag.Scale:
                            packet.Scale = reader.ReadVector3();
                            break;
                        case ETransformPacketFlag.LinearVelocity:
                            packet.LinearVelocity = reader.ReadVector3();
                            break;
                        case ETransformPacketFlag.AngularVelocity:
                            packet.AngularVelocity = reader.ReadVector3();
                            break;
                    }
                }

                return packet;
            }

            public static void Write(Writer writer, TransformPacket packet)
            {
                writer.WriteUInt32(packet.ObjectIdentifier);
                
                if (packet.Position is not null)
                {
                    writer.WriteByte((byte)ETransformPacketFlag.Position);
                    writer.WriteVector3((Vector3)packet.Position);
                }
                
                if (packet.Rotation is not null)
                {
                    writer.WriteByte((byte)ETransformPacketFlag.Rotation);
                    writer.WriteQuaternion((Quaternion)packet.Rotation);
                }
                
                if (packet.Scale is not null)
                {
                    writer.WriteByte((byte)ETransformPacketFlag.Scale);
                    writer.WriteVector3((Vector3)packet.Scale);
                }
                
                if (packet.LinearVelocity is not null && ((Vector3)packet.LinearVelocity).magnitude > 0)
                {
                    writer.WriteByte((byte)ETransformPacketFlag.LinearVelocity);
                    writer.WriteVector3((Vector3)packet.LinearVelocity);
                }
                
                if (packet.AngularVelocity is not null && ((Vector3)packet.AngularVelocity).magnitude > 0)
                {
                    writer.WriteByte((byte)ETransformPacketFlag.AngularVelocity);
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

                public Builder WithPosition(Vector3 position)
                {
                    _packet.Position = position;
                    _packet.NumberOfValues++;
                    return this;
                }

                public Builder WithRotation(Quaternion rotation)
                {
                    _packet.Rotation = rotation;
                    _packet.NumberOfValues++;
                    return this;
                }

                public Builder WithScale(Vector3 scale)
                {
                    _packet.Scale = scale;
                    _packet.NumberOfValues++;
                    return this;
                }

                public Builder WithLinearVelocity(Vector3 linearVelocity)
                {
                    _packet.LinearVelocity = linearVelocity;
                    _packet.NumberOfValues++;
                    return this;
                }

                public Builder WithAngularVelocity(Vector3 angularVelocity)
                {
                    _packet.AngularVelocity = angularVelocity;
                    _packet.NumberOfValues++;
                    return this;
                }

                public TransformPacket Build()
                {
                    return _packet;
                }
            }
        }
}
