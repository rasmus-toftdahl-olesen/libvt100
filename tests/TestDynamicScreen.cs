using libvt100;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using static libvt100.DynamicScreen;

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

        [Test]
        // Example:
        // [01musing[00m [38;2;85;85;85mSystem[39m;
        // |    1    |  2  |        3            |  4  
        // <bold>using</bold><normal> </normal><color>System</color><normal>;</normal>
        public void TestAttributeRuns()
        {
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            screen = new DynamicScreen(100);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("[01musing[00m [38;2;85;85;85mSystem[39m;"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(4, screen.Lines[0].Runs.Count);

            Screen.Character c;
            // Test using grid based API
            c = screen[0, 0];
            Assert.AreEqual('u', c.Char);
            Assert.AreEqual(true, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[1, 0];
            Assert.AreEqual('s', c.Char);
            Assert.AreEqual(true, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[2, 0];
            Assert.AreEqual('i', c.Char);
            Assert.AreEqual(true, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[3, 0];
            Assert.AreEqual('n', c.Char);
            Assert.AreEqual(true, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[4, 0];
            Assert.AreEqual('g', c.Char);
            Assert.AreEqual(true, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[5, 0];
            Assert.AreEqual(' ', c.Char);
            Assert.AreEqual(false, c.Attributes.Bold);
            Assert.AreEqual(Color.White, c.Attributes.ForegroundColor);

            c = screen[6, 0];
            Assert.AreEqual('S', c.Char);
            Assert.AreEqual(false, c.Attributes.Bold);
            Assert.AreEqual(Color.FromArgb(85, 85, 85), c.Attributes.ForegroundColor);
            // Test using Run-based API

            var run = screen.Lines[0].Runs[0];
            Assert.AreEqual("using", screen.Lines[0].Text[run.Start..(run.Start + run.Length)]);
            Assert.AreEqual(true, run.Attributes.Bold);
            Assert.AreEqual(Color.White, run.Attributes.ForegroundColor);

            run = screen.Lines[0].Runs[1];
            Assert.AreEqual(" ", screen.Lines[0].Text[run.Start..(run.Start + run.Length)]);
            Assert.AreEqual(false, run.Attributes.Bold);
            Assert.AreEqual(Color.White, run.Attributes.ForegroundColor);

            run = screen.Lines[0].Runs[2];
            Assert.AreEqual("System", screen.Lines[0].Text[run.Start..(run.Start + run.Length)]);
            Assert.AreEqual(false, run.Attributes.Bold);
            Assert.AreEqual(Color.FromArgb(85, 85, 85), run.Attributes.ForegroundColor);

            run = screen.Lines[0].Runs[3];
            Assert.AreEqual(";", screen.Lines[0].Text[run.Start..(run.Start + run.Length)]);
            Assert.AreEqual(false, run.Attributes.Bold);
            Assert.AreEqual(Color.White, run.Attributes.ForegroundColor);
        }

        [Test]
        public void TestGridAccess()
        {
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            screen = new DynamicScreen(20);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("[01musing[00m [38;2;85;85;85mSystem[39m;"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(1, screen.Lines.Count);
            Assert.AreEqual(4, screen.Lines[0].Runs.Count);
            Assert.AreEqual('u', screen.Lines[0][0].Char);
            Assert.AreEqual('s', screen.Lines[0][1].Char);
            Assert.AreEqual('i', screen.Lines[0][2].Char);
            Assert.AreEqual('n', screen.Lines[0][3].Char);
            Assert.AreEqual('g', screen.Lines[0][4].Char);
            Assert.AreEqual(' ', screen.Lines[0][5].Char);
            Assert.AreEqual('S', screen.Lines[0][6].Char);
            Assert.AreEqual(';', screen.Lines[0][12].Char);

            Assert.AreEqual('u', screen[0,0].Char);
            Assert.AreEqual('s', screen[1,0].Char);
            Assert.AreEqual('i', screen[2,0].Char);
            Assert.AreEqual('n', screen[3,0].Char);
            Assert.AreEqual('g', screen[4,0].Char);
            Assert.AreEqual(' ', screen[5,0].Char);
            Assert.AreEqual('S', screen[6,0].Char);
            Assert.AreEqual(';', screen[12,0].Char);

            var i = screen.Lines[0].Text.Length;
            // if x is greater text suceed but return null
            Assert.Null(screen.Lines[0][i]);
            Assert.Null(screen[i, 0]);

            // if x is greater than width
            Assert.Throws<ArgumentOutOfRangeException>(() => { var c = screen[screen.Width, 0]; });

            // if y is greater than height
            Assert.Throws<ArgumentOutOfRangeException>(() => { var c = screen[0, screen.Height]; } );

            // Write
            // This should not change underlying text
            screen.Lines[0][0].Char = 'U';
            Assert.AreEqual('u', screen.Lines[0][0].Char);

            Assert.Null(screen.Lines[0][i]);

            // if x is greater than width
            Assert.Throws<ArgumentOutOfRangeException>(() => { screen[screen.Width, 0] = new Screen.Character('!'); });

            // if y is greater than height
            Assert.Throws<ArgumentOutOfRangeException>(() => { screen[0, screen.Height] = new Screen.Character('!'); });

        }

        [Test]
        public void TestLine()
        {
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            screen = new DynamicScreen(50);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("[01musing[00m [38;2;85;85;85mSystem[39m;"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(4, screen.Lines[0].Runs.Count);

            Line line = screen.Lines[0];
            Assert.NotNull(line);

            Assert.AreEqual('u', line[0].Char);

            line[0].Char = 'U';
            Assert.AreEqual('u', line[0].Char);

           
            line[0] = new Screen.Character('!') { Attributes = new Screen.GraphicAttributes() { Italic = true } };
            Assert.AreEqual(5, screen.Lines[0].Runs.Count);
            Assert.AreEqual('!', line[0].Char);
            Assert.IsFalse(line[0].Attributes.Bold);
            Assert.IsTrue(line[0].Attributes.Italic);

            Assert.AreEqual('s', line[1].Char);
            Assert.IsTrue(line[1].Attributes.Bold);
            Assert.IsFalse(line[1].Attributes.Italic);

            Assert.AreEqual('!', screen[0,0].Char);

            // Make longer
            var i = line.Text.Length;
            line[i] = new Screen.Character('*') { Attributes = new Screen.GraphicAttributes() { Italic = true } };
            Assert.AreEqual(6, screen.Lines[0].Runs.Count);
            Assert.AreEqual('*', line[i].Char);
            Assert.IsFalse(line[i].Attributes.Bold);
            Assert.IsTrue(line[i].Attributes.Italic);
            Assert.AreEqual('!', screen[0,0].Char);
            Assert.AreEqual('*', screen[i,0].Char);

            // Add (pads with spaces)
            i = screen.Width - 10;
            line[i] = new Screen.Character('_') { Attributes = new Screen.GraphicAttributes() { Italic = true } };
            Assert.AreEqual(8, screen.Lines[0].Runs.Count);
            Assert.AreEqual('_', line[i].Char);
            Assert.IsFalse(line[i].Attributes.Bold);
            Assert.IsTrue(line[i].Attributes.Italic);
            Assert.AreEqual('!', screen[0, 0].Char);
            Assert.AreEqual('_', screen[i, 0].Char);

            // Add to very end (pads with spaces)
            i = screen.Width - 1;
            line[i] = new Screen.Character('^') { Attributes = new Screen.GraphicAttributes() { Italic = true } };
            Assert.AreEqual(10, screen.Lines[0].Runs.Count);
            Assert.AreEqual('^', line[i].Char);
            Assert.IsFalse(line[i].Attributes.Bold);
            Assert.IsTrue(line[i].Attributes.Italic);
            Assert.AreEqual('!', screen[0, 0].Char);
            Assert.AreEqual('^', screen[i, 0].Char);

            // Add @ screen width (this is actually invalid, but there's no way to protect against it)
            i = screen.Width;
            line[i] = new Screen.Character('&') { Attributes = new Screen.GraphicAttributes() { Italic = true } };
        }

        [Test]
        public void TestSize()
        {
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding.UTF8;

            DynamicScreen screen;

            screen = new DynamicScreen(50);
            vt100.Subscribe(screen);
            vt100.Input(Encoding.UTF8.GetBytes("[01musing[00m [38;2;85;85;85mSystem[39m;"));
            Assert.AreEqual(1, screen.NumLines);
            Assert.AreEqual(4, screen.Lines[0].Runs.Count);

        }

    }
}