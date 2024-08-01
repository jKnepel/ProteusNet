using jKnepel.ProteusNet.Serialising;
using UnityEngine;

namespace jKnepel.ProteusNet.Samples
{
    public struct ClientVisualiserData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public ClientVisualiserData(Vector3 position, Quaternion rotation)
	    {
            Position = position;
            Rotation = rotation;
	    }

        public static ClientVisualiserData ReadClientVisualiserData(Reader reader)
	    {
            var position = reader.ReadVector3();
            var rotation = reader.ReadQuaternion();
            return new(position, rotation);
	    }

        public static void WriteClientVisualiserData(Writer writer, ClientVisualiserData data)
	    {
            writer.WriteVector3(data.Position);
            writer.WriteQuaternion(data.Rotation);
	    }
    }
}
