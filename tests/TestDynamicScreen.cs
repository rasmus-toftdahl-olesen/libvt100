using libvt100;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace libvt100.Tests
{
    [TestFixture]
    class TestDynamicScreen 
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }


        [Test]
        public void TestCreate()
        {
            var screen = new DynamicScreen(80);
            Assert.AreEqual(80, screen.Width);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(new System.Drawing.Point(0, 0), screen.CursorPosition);
        }

        [Test]
        public void TestLineWrap()
        {
            var testLine = "01";
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            // If a file has a single \n in it, it has two lines.
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("\n"));
            Assert.AreEqual(2, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);

            // If a file has a one line that is small + a \n in it, it has two lines.
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"0\n"));
            Assert.AreEqual(2, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);

            // If a file has a one line that fits + a \n in it, it has two lines.
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"{testLine}\n"));
            Assert.AreEqual(2, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);

            // A full width line, one line
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes(testLine));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);

            // Two full width lines, two lines
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"{testLine}\n{testLine}"));
            Assert.AreEqual(2, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);

            // wrapping scenarios

            // a full width line + 1 char, two lines
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes(testLine + "!"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);

            // one full width line + one full width line + 1 char, three lines
            screen = new DynamicScreen(testLine.Length);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"{testLine}\n{testLine}!"));
            Assert.AreEqual(2, screen.NumLines);
            Assert.AreEqual(3, screen.Lines.Count);

        }

        [Test]
        public void TestTabs()
        {
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"012\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(2, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            // \t
            // 012
            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"\t012"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            // 0\t
            // 12
            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"0\t12"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(2, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            // 01\t
            // 2
            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"01\t2"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(2, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            // 0123
            // \t
            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"0123\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(1, screen.Lines[1].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            // 0123
            // 4\t
            screen = new DynamicScreen(4);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"01234\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(4, screen.Lines[0].Text.Length);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[1].Text.Length);

            // Odd TabSpaces
            screen = new DynamicScreen(3);
            screen.TabSpaces = 3;
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);

            screen = new DynamicScreen(3);
            screen.TabSpaces = 3;
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes($"023\t"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(2, screen.Lines.Count);
            Assert.AreEqual(1, screen.Lines[0].Runs.Count);
            Assert.AreEqual(screen.TabSpaces, screen.Lines[0].Text.Length);
        }
    }
}
