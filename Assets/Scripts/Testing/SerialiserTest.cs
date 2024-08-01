using jKnepel.ProteusNet.Serialising;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SerialiserTest : MonoBehaviour
{
    [SerializeField] private SerialiserConfiguration _serialiserConfiguration;
    [SerializeField] private int _repetitions;

    private void Start()
    {
        ValueStruct input = new()
        {
            Byte = 1,
            Array = new []{ 1, 2, 3 },
            List = new() { 4, 5, 6 },
            Dict = new() { {7, "a"}, {8, "b"}, {9, "c"} },
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123
        };
        ValueStruct output = default;

        var size = 0;
        var start = DateTime.Now;
        for (var i = 0; i < _repetitions; i++)
        {
            Writer writer = new(_serialiserConfiguration.Settings);
            writer.Write(input);
            size = writer.Length;
            Reader reader = new(writer.GetBuffer(), _serialiserConfiguration.Settings);
            output = reader.Read<ValueStruct>();
        }
        var end = DateTime.Now;
        
        Debug.Log(size);
        Debug.Log((float)end.Subtract(start).Milliseconds / _repetitions);
        Debug.Log(
            $"Byte = {output.Byte},\n" +
                  $"Array = {string.Join( ',', output.Array)},\n" +
                  $"List = {string.Join( ',', output.List)},\n" +
                  $"Dict = {string.Join( ',', output.Dict)},\n" +
                  $"Short = {output.Short},\n" +
                  $"UShort = {output.UShort},\n" +
                  $"Int = {output.Int},\n" +
                  $"UInt = {output.UInt},\n" +
                  $"Long = {output.Long},\n" +
                  $"ULong = {output.ULong}");
    }

    private struct ValueStruct
    {
        public byte Byte;
        public int[] Array;
        public List<byte> List;
        public Dictionary<short, string> Dict;
        public short Short;
        public ushort UShort;
        public int Int;
        public uint UInt;
        public long Long;
        public ulong ULong;
    }
}