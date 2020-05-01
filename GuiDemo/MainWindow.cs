#define Use_Grid
using libvt100;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static libvt100.Screen;
using Screen = libvt100.Screen;

namespace GuiDemo {
    public partial class MainWindow : Form {
        public string File { get; set; } = @"..\..\..\..\tests\Program.cs.ans";
        //public string File { get; set; } = @"..\..\..\..\tests\Fixed Pitch Alignment.c.ans";
        DynamicScreen _screen;
        IAnsiDecoder _vt100 = new AnsiDecoder();

        private Size _charSize;

        private int _border = 5;
        private int _lineNumberWidth;

        StringFormat _sf = new StringFormat(StringFormat.GenericTypographic) {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.DisplayFormatControl | StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip | StringFormatFlags.DisplayFormatControl,
            Trimming = StringTrimming.None,
            Alignment = StringAlignment.Near,
        };
        TextRenderingHint _textRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        public MainWindow() {
            InitializeComponent();

            Paint += MainWindow_Paint;

            var sizef = Graphics.FromHwnd(this.Handle).MeasureString("a", Font, ClientSize.Width, _sf);
            _charSize = new Size((int)Math.Round(sizef.Width), (int)Math.Round(sizef.Height));
            _lineNumberWidth = _charSize.Width * 5;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Load();
        }

        private void Load() {
            _screen = new DynamicScreen((ClientSize.Width - _lineNumberWidth - (_border * 2)) / _charSize.Width);
            _screen.TabSpaces = 4;
            _vt100.Encoding = Encoding.UTF8;
            _vt100.Subscribe(_screen);

            _vt100.Input(System.IO.File.ReadAllBytes(File));
            _screen.CursorPosition = new Point(0, 0);

            Text = $" - {File}  ({_screen.Width}x{_screen.Height})";

            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e is null) {
                throw new ArgumentNullException(nameof(e));
            }

            // Get current cursor
            var rect = GetCursorRect(_screen.CursorPosition);
            //rect.Inflate(1, 1);

            switch (e.KeyCode) {
                case Keys.F5:
                    Load();
                    break;

                case Keys.Home:
                    Invalidate(rect);
                    (_screen as IAnsiDecoderClient).MoveCursorTo(null, new Point(0, 0));
                    rect = GetCursorRect(_screen.CursorPosition);
                    //rect.Inflate(1, 1);
                    Invalidate(rect);
                    break;

                case Keys.End:
                    Invalidate(rect);
                    (_screen as IAnsiDecoderClient).MoveCursorTo(null, new Point(_screen.Width - 1, _screen.Height - 1));
                    rect = GetCursorRect(_screen.CursorPosition);
                    //rect.Inflate(1, 1);
                    Invalidate(rect);
                    break;


                case Keys.Down:
                    if (_screen.CursorPosition.Y < _screen.Lines.Count) {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Down, 1);
                        rect = GetCursorRect(_screen.CursorPosition);
                        // rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Up:
                    if (_screen.CursorPosition.Y > 0) {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Up, 1);
                        rect = GetCursorRect(_screen.CursorPosition);
                        //rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Right:
                    if (_screen.CursorPosition.Y < _screen.Width) {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Forward, 1);
                        rect = GetCursorRect(_screen.CursorPosition);
                        //rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Left:
                    if (_screen.CursorPosition.X > 0) {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Backward, 1);
                        rect = GetCursorRect(_screen.CursorPosition);
                        //rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    else if (_screen.CursorPosition.Y > 0) {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Up, 1);
                        (_screen as IAnsiDecoderClient).MoveCursorToColumn(null, _screen.Width - 1);
                        rect = GetCursorRect(_screen.CursorPosition);
                        //rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e) {
            //e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
            Point current = new Point(0, 0);
            for (; current.Y < _screen.Height; current.Y++) {
                if (_screen[current.Y].LineNumber != 0) {
                    e.Graphics.DrawString($"{_screen[current.Y].LineNumber}", this.Font, new SolidBrush(Color.DarkGray), _lineNumberWidth, current.Y * _charSize.Height + _border,
                        new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far });
                }

#if Use_Grid
                for (current.X = 0; current.X < _screen.Width; current.X++) {

                    Character v = _screen[current.X, current.Y];
                    if (v == null) {
                        v = new Character(' ');
                    }
#else
                current.X = 0;
                foreach (var v in _screen.Lines[current.Y].Runs) 
                { 
#endif
                    System.Drawing.Font font = this.Font;
                    if (v.Attributes.Bold) {
                        if (v.Attributes.Italic) {
                            font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
                        }
                        else {
                            font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
                        }
                    }
                    else if (v.Attributes.Italic) {
                        font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Italic, GraphicsUnit.Point);
                    }


#if Use_Grid
                    var fg = ForeColor;
                    var bg = BackColor;

                    if (current == _screen.CursorPosition) {
                        fg = BackColor;
                        bg = ForeColor;
                    }
                    else {
                        //if (c.Attributes.BackgroundColor != BackColor)
                        bg = v.Attributes.BackgroundColor;
                        //if (c.Attributes.ForegroundColor != ForeColor)
                        fg = v.Attributes.ForegroundColor;
                        if (fg == bg)
                            fg = ForeColor;
                    }
                    var fgBrush = new SolidBrush(fg);
                    var bgBrush = new SolidBrush(bg);

                    e.Graphics.FillRectangle(bgBrush, GetCursorRect(current));
                    var c = v.Char;
                    e.Graphics.DrawString($"{c}", font, fgBrush, _lineNumberWidth + _border + (current.X * _charSize.Width), _border + current.Y * _charSize.Height);
#else

                    var text = _screen.Lines[current.Y].Text[v.Start..(v.Start + v.Length)];
                    for (var i = 0; i < text.Length; i++)
                    {
                        var fg = ForeColor;
                        var bg = BackColor;

                        if (current == _screen.CursorPosition)
                        {
                            fg = BackColor;
                            bg = ForeColor;
                        }
                        else
                        {
                            bg = v.Attributes.BackgroundColor;
                            fg = v.Attributes.ForegroundColor;
                            if (fg == bg)
                                fg = ForeColor;
                        }
                        var fgBrush = new SolidBrush(fg);
                        var bgBrush = new SolidBrush(bg);
                        e.Graphics.FillRectangle(bgBrush, GetCursorRect(current));
                        e.Graphics.DrawString($"{text[i]}", font, fgBrush, _lineNumberWidth + _border + (current.X * _charSize.Width), _border + current.Y * _charSize.Height);
                        current.X++;
                    }

                    if (v.HasTab)
                    {
                        e.Graphics.DrawString($"⭾", Font, new SolidBrush(Color.DarkGray), _lineNumberWidth + _border + ((current.X - text.Length) * _charSize.Width), _border + current.Y * _charSize.Height);
                    }
#endif
                }
                while (current.X < _screen.Width) {
                    var fg = ForeColor;
                    var bg = BackColor;

                    if (current == _screen.CursorPosition) {
                        fg = BackColor;
                        bg = ForeColor;
                    }
                    var fgBrush = new SolidBrush(fg);
                    var bgBrush = new SolidBrush(bg);
                    e.Graphics.FillRectangle(bgBrush, GetCursorRect(current));
                    current.X++;
                }

            }
            e.Graphics.DrawRectangle(new Pen(Color.Green),
                _lineNumberWidth + _border - 1,
                _border - 1,
                _screen.Width * _charSize.Width + _border + 1,
                _screen.Height * _charSize.Height + _border + 1);
        }

        private Rectangle GetCursorRect(Point cursorPosition) {
            return new Rectangle(new Point(cursorPosition.X * _charSize.Width + _lineNumberWidth + _border + 2, cursorPosition.Y * _charSize.Height + _border + 1),
                new Size(_charSize.Width, _charSize.Height - 1));
        }

        private void MainWindow_Resize(object sender, EventArgs e) {
            Load();
        }
    }
}
