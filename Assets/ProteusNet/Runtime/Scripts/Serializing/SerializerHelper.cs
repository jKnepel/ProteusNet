using System;

namespace jKnepel.ProteusNet.Serializing
{
    public static class SerializerHelper
    {
		public static string GetTypeName(Type type)
		{
			if (type.IsArray)
				return "Array";

			if (!type.IsGenericType)
				return type.Name;

			int index = type.Name.IndexOf("`");
			return type.Name[..index];
		}
    }
}
