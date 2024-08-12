using UnityEngine;

namespace jKnepel.ProteusNet.Serializing
{
    public struct CompressedQuaternion
	{   // thanks to Glenn Fiedler https://gafferongames.com/post/snapshot_compression/
		private const float MINIMUM = -0.70710678f; // -1 / sqrt(2)
		private const float MAXIMUM = +0.70710678f; // +1 / sqrt(2)

        public readonly int Bits;

        public readonly Quaternion Quaternion;

        public readonly uint Largest;
        public readonly uint A;
        public readonly uint B;
        public readonly uint C;

        public readonly ulong PackedQuaternion;

		public CompressedQuaternion(Quaternion q, int bits = 10)
        {
            Bits = bits;
            Quaternion = q;

            var absX = Mathf.Abs(q.x);
            var absY = Mathf.Abs(q.y);
            var absZ = Mathf.Abs(q.z);
            var absW = Mathf.Abs(q.w);

            Largest = 0;
            var largestValue = q.x;
            var largestAbsValue = absX;

            if (absY > largestAbsValue)
            {
				Largest = 1;
                largestValue = q.y;
                largestAbsValue = absY;
			}
            if (absZ > largestAbsValue)
            {
				Largest = 2;
                largestValue = q.z;
                largestAbsValue = absZ;
            }
            if (absW > largestAbsValue)
            {
				Largest = 3;
                largestValue = q.w;
            }

            float a = 0, b = 0, c = 0;
            switch(Largest)
            {
                case 0:
                    a = q.y;
                    b = q.z;
                    c = q.w;
                    break;
                case 1:
					a = q.x;
					b = q.z;
					c = q.w;
					break;
                case 2:
					a = q.x;
					b = q.y;
					c = q.w;
					break;
				case 3:
					a = q.x;
					b = q.y;
					c = q.z;
					break;
			}

            if (largestValue < 0)
            {
                a = -a; b = -b; c = -c;
            }

            var normalisedA = (a - MINIMUM) / (MAXIMUM - MINIMUM);
            var normalisedB = (b - MINIMUM) / (MAXIMUM - MINIMUM);
            var normalisedC = (c - MINIMUM) / (MAXIMUM - MINIMUM);

            float scale = (1 << bits) - 1;
            A = (uint)Mathf.Floor(normalisedA * scale + 0.5f);
            B = (uint)Mathf.Floor(normalisedB * scale + 0.5f);
            C = (uint)Mathf.Floor(normalisedC * scale + 0.5f);

            PackedQuaternion = ((ulong)Largest << Bits * 3) | ((ulong)A << Bits * 2) | ((ulong)B << Bits) | C;
        }

        public CompressedQuaternion(ulong packedQuaternion, int bits = 10)
        {
	        PackedQuaternion = packedQuaternion;
            Bits = bits;
            
	        var mask = (ulong)((1 << Bits) - 1);
            Largest = (uint)( 0x3 & packedQuaternion >> Bits * 3);
            A =       (uint)(mask & packedQuaternion >> Bits * 2);
            B =       (uint)(mask & packedQuaternion >> Bits);
            C =       (uint)(mask & packedQuaternion);

			var scale = (1 << Bits) - 1;
            var inverseScale = 1f / scale;

            var floatA = A * inverseScale * (MAXIMUM - MINIMUM) + MINIMUM;
            var floatB = B * inverseScale * (MAXIMUM - MINIMUM) + MINIMUM;
            var floatC = C * inverseScale * (MAXIMUM - MINIMUM) + MINIMUM;

            float x = 0, y = 0, z = 0, w = 0;
            switch(Largest) 
            {
                case 0:
                    x = Mathf.Sqrt(1 - floatA * floatA - floatB * floatB - floatC * floatC);
                    y = floatA;
                    z = floatB;
                    w = floatC;
                    break;
				case 1:
                    x = floatA;
					y = Mathf.Sqrt(1 - floatA * floatA - floatB * floatB - floatC * floatC);
					z = floatB;
					w = floatC;
					break;
				case 2:
                    x = floatA;
					y = floatB;
					z = Mathf.Sqrt(1 - floatA * floatA - floatB * floatB - floatC * floatC);
					w = floatC;
					break;
				case 3:
                    x = floatA;
					y = floatB;
					z = floatC;
					w = Mathf.Sqrt(1 - floatA * floatA - floatB * floatB - floatC * floatC);
					break;
			}

            float norm = x*x + y*y + z*z + w*w;
			if (norm > 0.000001f)
			{
				float length = Mathf.Sqrt(norm);
				Quaternion = new(x, y, z, w);
				Quaternion.x /= length;
				Quaternion.y /= length;
				Quaternion.z /= length;
				Quaternion.w /= length;
			}
			else
			{
				Quaternion = new(0, 0, 0, 1);
			}
		}
    }
}
