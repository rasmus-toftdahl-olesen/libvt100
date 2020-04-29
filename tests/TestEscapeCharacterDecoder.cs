using System;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using libvt100;
using System.Threading;

namespace libvt100.Tests
{
    public static class StringExtensions
    {
        public static string Repeat(this string s, int n)
        {
            return new StringBuilder(s.Length * n)
                            .AppendJoin(s, new string[n + 1])
                            .ToString();
        }
    }

    [TestFixture]
    public class TestEscapeCharacterDecoder : EscapeCharacterDecoder
    {
        private struct Command
        {
            public byte m_command;
            public string m_parameter;

            public Command(byte _command, string _parameter)
            {
                m_command = _command;
                m_parameter = _parameter;
            }
        }

        private List<char[]> m_chars;
        private List<Command> m_commands;

        [SetUp]
        public void SetUp()
        {
            m_chars = new List<char[]>();
            m_commands = new List<Command>();
        }

        [TearDown]
        public void TearDown()
        {
            m_chars = null;
            m_commands = null;
        }

        [Test]
        public void TestNormalCharactersAreJustPassedThrough()
        {
            (this as IDecoder).Input(new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E' });

            Assert.AreEqual("ABCDE", ReceivedCharacters);
        }

        [Test]
        public void TestNewLine()
        {
            Input("AB\nCDE");
            Assert.AreEqual("AB\nCDE", ReceivedCharacters);
        }

        [Test]
        public void TestTabs()
        {
            Input("AB\tCDE");
            Assert.AreEqual("AB\tCDE", ReceivedCharacters);
        }

        [Test]
        public void TestCommandsAreNotInterpretedAsNormalCharacters()
        {
            (this as IDecoder).Input(new byte[] { (byte)'A', (byte)'B', 0x1B, (byte)'1', (byte)'2', (byte)'3', (byte)'m', (byte)'C', (byte)'D', (byte)'E' });
            Assert.AreEqual("ABCDE", ReceivedCharacters);

            Input("\x001B123mA");
            Assert.AreEqual("A", ReceivedCharacters);

            Input("\x001B123m\x001B123mA");
            Input("A");
            Assert.AreEqual("AA", ReceivedCharacters);

            Input("AB\x001B123mCDE");
            Assert.AreEqual("ABCDE", ReceivedCharacters);

            Input("AB\x001B123m");
            Assert.AreEqual("AB", ReceivedCharacters);

            Input("A");
            Input("AB\x001B123mCDE\x001B123m\x001B123mCDE");
            Assert.AreEqual("AABCDECDE", ReceivedCharacters);

            Input("A\x001B[123m\x001B[123mA");
            Input("A");
            Assert.AreEqual("AAA", ReceivedCharacters);

            Input("A\x001B123m\x001B[123mA");
            Assert.AreEqual("AA", ReceivedCharacters);

            Input("A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA");
            Assert.AreEqual("AA", ReceivedCharacters);
        }

        [Test]
        public void TestCommands()
        {
            Input("A\x001B123m\x001B[123mA");
            AssertCommand('m', "123");
            AssertCommand('m', "123");
            Assert.IsEmpty(m_commands);

            Input("A\x001B123n\x001B[123mA");
            AssertCommand('n', "123");
            AssertCommand('m', "123");
            Assert.IsEmpty(m_commands);

            Input("A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA");
            AssertCommand('a', "123;321;456");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);

            // With '['
            Input("\x001B[\"This string is part of the command\"123b");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);

            // No '['
            Input("\x001B\"This string is part of the command\"123b");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);

            Assert.AreEqual("AAAAAA", ReceivedCharacters);

            // This is the same commands we use for testing for stack overflow
            Input("\x001B[123mA".Repeat(4));
            for (int i = 0; i < 4; i++)
            {
                AssertCommand('m', "123");
            }
            Assert.IsEmpty(m_commands);
            Assert.AreEqual("AAAA", ReceivedCharacters);

            // "using System;" highlighted by Pygments' Terminal256/16m Formatter
            Input("\x001B[34musing\x001B[39;49;00m \x001B[04m\x001B[36mSystem\x001B[39;49;00m;");
            AssertCommand('m', "34");
            AssertCommand('m', "39;49;00");
            AssertCommand('m', "04");
            AssertCommand('m', "36");
            AssertCommand('m', "39;49;00");
            Assert.IsEmpty(m_commands);
            Assert.AreEqual("using System;", ReceivedCharacters);
        }

        [Test]
        public void TestSingleCharCommands()
        {
            Input("\x001B=");
            AssertCommand('=', string.Empty);
            Input("\x001B>");
            AssertCommand('>', string.Empty);

            //Input("\x001B?");
            // TODO: how to test invalid?
            //AssertCommand('?', string.Empty);
        }

        [Test]
        public void TestPartialInput()
        {
            // Input("A\x001B123m\x001B[123mA");
            Input("A");
            Input("\x001B");
            Input("1");
            Input("2");
            Input("3");
            Input("m");
            Input("\x001B");
            Input("[");
            Input("1");
            Input("2");
            Input("3");
            Input("m");
            Input("A");
            AssertCommand('m', "123");
            AssertCommand('m', "123");
            Assert.IsEmpty(m_commands);

            Input("A");
            Input("\x001B1");
            Input("2");
            Input("3");
            Input("m\x001B");
            Input("[1");
            Input("2");
            Input("3m");
            Input("A");
            AssertCommand('m', "123");
            AssertCommand('m', "123");
            Assert.IsEmpty(m_commands);

            Input("A");
            Input("\x001B1");
            Input("2");
            Input("3");
            Input("m\x001B");
            Input("[");
            Input("1");
            Input("2");
            Input("3mA");
            AssertCommand('m', "123");
            AssertCommand('m', "123");
            Assert.IsEmpty(m_commands);

            //Input("A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA");
            Input("A\x001B[123");
            Input(";321;");
            Input("4");
            Input("56");
            Input("a");
            Input("\x001B[\"");
            Input("This string is part of the command");
            Input("\"");
            Input("123bA");
            AssertCommand('a', "123;321;456");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);

            //Input("A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA");
            Input("A\x001B[123");
            Input(";321;");
            Input("4");
            Input("56");
            Input("a\x001B");
            Input("[\"");
            Input("This string is part of the comman");
            Input("d\"");
            Input("123bA");
            AssertCommand('a', "123;321;456");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);

            //Input("A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA");
            Input("A\x001B[123");
            Input(";321;");
            Input("4");
            Input("56");
            Input("a\x001B");
            Input("[\"T");
            Input("his string is part of the comman");
            Input("d\"1");
            Input("23bA");
            AssertCommand('a', "123;321;456");
            AssertCommand('b', "\"This string is part of the command\"123");
            Assert.IsEmpty(m_commands);
            Assert.AreEqual("AAAAAAAAAAAA", ReceivedCharacters);
        }

        [Test]
        public void TestUnicode()
        {
            // "⏻ ⏼ ⏽ ⭘ ⏾"
            var input = "\u23fb \u23fc \u23fd ⭘ \u23fe (Unicode Power Symbols)";
            (this as IDecoder).Encoding = Encoding.UTF8;
            var bytes = (this as IDecoder).Encoding.GetBytes(input);
            (this as IDecoder).Input(bytes);
            Assert.AreEqual(input, ReceivedCharacters);

            // Command with unicode char "⏻" in it
            input = "\x001b[\"\u23fb\"b";
            (this as IDecoder).Encoding = Encoding.UTF8;
            bytes = (this as IDecoder).Encoding.GetBytes(input);
            (this as IDecoder).Input(bytes);
            AssertCommand('b', "\"\u23fb\"");

            // Unicode between commands
            // Command with unicode char "⏻" in it
            input = "\x001b[123m\u23fb\x001b[124n";
            (this as IDecoder).Encoding = Encoding.UTF8;
            bytes = (this as IDecoder).Encoding.GetBytes(input);
            (this as IDecoder).Input(bytes);
            AssertCommand('m', "123");
            AssertCommand('n', "124");
            Assert.AreEqual("\u23fb", ReceivedCharacters);

            // Snippet of C# code, encoded with pigmentize 16m
            input = "\u001b[38;2;143;89;2m// Unicode Power Symbols: \u23fb \u23fc \u23fd ⭘ \u23fe\u001b[39m";
            (this as IDecoder).Encoding = Encoding.UTF8;
            bytes = (this as IDecoder).Encoding.GetBytes(input);
            (this as IDecoder).Input(bytes);
            AssertCommand('m', "38;2;143;89;2");
            AssertCommand('m', "39");
            Assert.AreEqual("// Unicode Power Symbols: \u23fb \u23fc \u23fd ⭘ \u23fe", ReceivedCharacters);
        }

        /// <summary>
        /// This attempts to test for a stack overflow caused by the recursion in ProcessCommandBuffer.
        /// See https://github.com/rasmus-toftdahl-olesen/libvt100/issues/6
        /// It is not really possible to test for stack overflow failures in .NET. 
        /// Values of `iteration` above ~3000 cause the tests to abort and the `TestEscapeCharacterDecoder`
        /// set of tests will not complete.
        /// Values under ~3000 will succeed. 
        /// </summary>
        [Test]
        //[Ignore("This test has been disaled until the stack overflow issue has been fixed (https://github.com/rasmus-toftdahl-olesen/libvt100/issues/6)")]
        public void TestStackOverFlow()
        {
            var iterations = 100000;
            Input("\x001B[123mA".Repeat(iterations));
            for (int i = 0; i < iterations; i++)
            {
                AssertCommand('m', "123");
            }
            Assert.IsEmpty(m_commands);
            var result = ReceivedCharacters;
            Assert.AreEqual("A".Repeat(iterations), result);
        }

        private void AssertCommand(char _command, string _parameter)
        {
            Assert.IsNotEmpty(m_commands);
            Assert.AreEqual(_command, (char)m_commands[0].m_command);
            Assert.AreEqual(_parameter, m_commands[0].m_parameter);
            m_commands.RemoveAt(0);
        }

        private void Input(String _input)
        {
            byte[] data = new byte[_input.Length];
            int i = 0;
            foreach (char c in _input)
            {
                data[i] = (byte)c;
                i++;
            }
            (this as IDecoder).Input(data);
        }

        private String ReceivedCharacters
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (char[] chars in m_chars)
                {
                    builder.Append(chars);
                }
                m_chars.Clear();
                return builder.ToString();
            }
        }

        override protected void ProcessCommand(byte _command, String _parameter)
        {
            m_commands.Add(new Command(_command, _parameter));
        }

        override protected void OnCharacters(char[] _chars)
        {
            m_chars.Add(_chars);
        }

        protected override void OnOutput(byte[] _data)
        {
            if (Output != null)
            {
                Output(this, _data);
            }
        }
        override public event DecoderOutputDelegate Output;
    }
}
