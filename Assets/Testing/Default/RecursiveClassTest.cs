using jKnepel.ProteusNet.Serializing;
using System.Collections.Generic;
using UnityEngine;

public class RecursiveClassTest : MonoBehaviour
{
    private void Start()
    {
        TestMessage[] messages2 = { new("jkl"), new("mno") };
        Dictionary<uint, TestMessage> messages = new() { { 1, new TestMessage("def") }, { 2, new TestMessage("ghi") } };
        Writer writer = new();
        writer.Write(new TestMessage("abc", messages));

        Reader reader = new(writer.GetBuffer());
        TestMessage message = reader.Read<TestMessage>();

		static void ReadMessages(TestMessage message)
		{
            Debug.Log(message.Text);
            if (message.Messages == null)
                return;

            foreach (TestMessage msg in message.Messages.Values)
                ReadMessages(msg);
        }
        ReadMessages(message);
    }
}

public class TestMessage
{
    public string Text;
    public Dictionary<uint, TestMessage> Messages;

    public TestMessage(string msg)
	{
        Text = msg;
        Messages = null;
	}

    public TestMessage(string msg, Dictionary<uint, TestMessage> messages)
    {
        Text = msg;
        Messages = messages;
    }

    /*
    public static void WriteTestMessage(Writer writer, TestMessage value)
	{
        Debug.Log("1234");
        writer.WriteString(value.Text);
        writer.WriteArray(value.Messages);
	}

    public static TestMessage ReadTestMessage(Reader reader)
    {
        Debug.Log("5678");
        string text = reader.ReadString();
        TestMessage[] messages = reader.ReadArray<TestMessage>();
        return new TestMessage(text, messages);
    }
    */
}

public struct TestMessage2
{
    public string Text2;
    public int Tester;

    public TestMessage2(string msg)
    {
        Text2 = msg;
        Tester = 2;
    }
}