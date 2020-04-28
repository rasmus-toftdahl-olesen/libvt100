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
            
            public Command ( byte _command, string _parameter )
            {
                m_command = _command;
                m_parameter = _parameter;
            }
        }
        
        private List<char[]> m_chars;
        private List<Command> m_commands;
        
        [SetUp]
        public void SetUp ()
        {
            m_chars = new List<char[]>();
            m_commands = new List<Command>();
        }
        
        [TearDown]
        public void TearDown ()
        {
            m_chars = null;
            m_commands = null;
        }
        
        [Test]
        public void TestNormalCharactersAreJustPassedThrough ()
        {
            (this as IDecoder).Input ( new byte[] { (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E' } );
            
            Assert.AreEqual ( "ABCDE", ReceivedCharacters );
        }
        
        [Test]
        public void TestCommandsAreNotInterpretedAsNormalCharacters ()
        {
            (this as IDecoder).Input ( new byte[] { (byte) 'A', (byte) 'B', 0x1B, (byte) '1', (byte) '2', (byte) '3', (byte) 'm', (byte) 'C', (byte) 'D', (byte) 'E' } );
            Assert.AreEqual ( "ABCDE", ReceivedCharacters );
            
            Input ( "\x001B123mA" );
            Assert.AreEqual ( "A", ReceivedCharacters );
            
            Input ( "\x001B123m\x001B123mA" );
            Input ( "A" );
            Assert.AreEqual ( "AA", ReceivedCharacters );
            
            Input ( "AB\x001B123mCDE" );
            Assert.AreEqual ( "ABCDE", ReceivedCharacters );
            
            Input ( "AB\x001B123m" );
            Assert.AreEqual ( "AB", ReceivedCharacters );
            
            Input ( "A" );
            Input ( "AB\x001B123mCDE\x001B123m\x001B123mCDE" );
            Assert.AreEqual ( "AABCDECDE", ReceivedCharacters );

            Input ( "A\x001B[123m\x001B[123mA" );
            Input ( "A" );
            Assert.AreEqual ( "AAA", ReceivedCharacters );
            
            Input ( "A\x001B123m\x001B[123mA" );
            Assert.AreEqual ( "AA", ReceivedCharacters );

            Input ( "A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA" );
            Assert.AreEqual ( "AA", ReceivedCharacters );
        }
        
        [Test]
        public void TestCommands ()
        {
            Input ( "A\x001B123m\x001B[123mA" );
            AssertCommand ( 'm', "123" );
            AssertCommand ( 'm', "123" );
            
            Input ( "A\x001B[123;321;456a\x001B[\"This string is part of the command\"123bA" );
            AssertCommand ( 'a', "123;321;456" );
            AssertCommand ( 'b', "\"This string is part of the command\"123" );
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
        [Ignore("This test has been disaled until the stack overflow issue has been fixed (https://github.com/rasmus-toftdahl-olesen/libvt100/issues/6)")]
        public void TestStackOverFlow()
        {
            var iterations = 3500;
            var line = "\x001B123mA";
            Input(line.Repeat(iterations));
            AssertCommand('m', "123");
            var result = ReceivedCharacters;
            Assert.AreEqual("A".Repeat(iterations), result);
        }

        private void AssertCommand ( char _command, string _parameter )
        {
            Assert.IsNotEmpty ( m_commands );
            Assert.AreEqual ( (byte) _command, m_commands[0].m_command );
            Assert.AreEqual ( _parameter, m_commands[0].m_parameter );
            m_commands.RemoveAt ( 0 );
        }
        
        private void Input ( String _input )
        {
            byte[] data = new byte[_input.Length];
            int i = 0;
            foreach ( char c in _input )
            {
                data[i] = (byte) c;
                i++;
            }
            (this as IDecoder).Input ( data );
        }
        
        private String ReceivedCharacters
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach ( char[] chars in m_chars )
                {
                    builder.Append ( chars );
                }
                m_chars.Clear();
                return builder.ToString();
            }
        }
        
        override protected void ProcessCommand ( byte _command, String _parameter )
        {
            m_commands.Add ( new Command(_command, _parameter) );
        }
        
        override protected void OnCharacters ( char[] _chars )
        {
            m_chars.Add ( _chars );
        }

       protected override void OnOutput( byte[] _data )
       {
          if ( Output != null )
          {
             Output( this, _data );
          }
       }
        override public event DecoderOutputDelegate Output;
    }
}
