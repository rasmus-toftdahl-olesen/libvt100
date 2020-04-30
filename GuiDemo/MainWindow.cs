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

namespace GuiDemo
{
    public partial class MainWindow : Form
    {
        public string File { get; set; } = @"..\..\..\..\tests\Fixed Pitch Alignment.c.ans";
        DynamicScreen _screen;
        IAnsiDecoder _vt100 = new AnsiDecoder();

        private SizeF _charSize;

        private int _border = 5;
        private int _lineNumberWidth;

        StringFormat _sf = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.DisplayFormatControl | StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip | StringFormatFlags.DisplayFormatControl,
            Trimming = StringTrimming.None,
            Alignment = StringAlignment.Near,
        };
        TextRenderingHint _textRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        public MainWindow()
        {
            InitializeComponent();

            Paint += MainWindow_Paint;

            _charSize = Graphics.FromHwnd(this.Handle).MeasureString("a", Font, ClientSize.Width, _sf);
            _lineNumberWidth = (int)Math.Round(_charSize.Width) * 5;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Load();
        }

        private void Load()
        {
            _screen = new DynamicScreen((ClientSize.Width - _lineNumberWidth - (_border * 2)) / (int)Math.Floor(_charSize.Width));
            _screen.TabSpaces = 4;
            _vt100.Encoding = Encoding.UTF8;
            _vt100.Subscribe(_screen);

            _vt100.Input(System.IO.File.ReadAllBytes(File));
            _screen.CursorPosition = new Point(0, 0);

            Text = $" - {File}  ({_screen.Width}x{_screen.Height})";

            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            // Get current cursor
            var rect = GetCursorRect();
            rect.Inflate(1, 1);

            switch (e.KeyCode)
            {
                case Keys.F5:
                    Load();
                    break;

                case Keys.Home:
                    Invalidate(rect);
                    (_screen as IAnsiDecoderClient).MoveCursorTo(null, new Point(0,0));
                    rect = GetCursorRect();
                    rect.Inflate(1, 1);
                    Invalidate(rect);
                    break;

                case Keys.End:
                    Invalidate(rect);
                    (_screen as IAnsiDecoderClient).MoveCursorTo(null, new Point(_screen.Width - 1, _screen.Height - 1));
                    rect = GetCursorRect();
                    rect.Inflate(1, 1);
                    Invalidate(rect);
                    break;


                case Keys.Down:
                    if (_screen.CursorPosition.Y < _screen.Lines.Count)
                    {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Down, 1);
                        rect = GetCursorRect();
                        rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Up:
                    if (_screen.CursorPosition.Y > 0)
                    {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Up, 1);
                        rect = GetCursorRect();
                        rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Right:
                    if (_screen.CursorPosition.Y < _screen.Width)
                    {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Forward, 1);
                        rect = GetCursorRect();
                        rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;

                case Keys.Left:
                    if (_screen.CursorPosition.X > 0)
                    {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Backward, 1);
                        rect = GetCursorRect();
                        rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    else if (_screen.CursorPosition.Y > 0)
                    {
                        Invalidate(rect);
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Up, 1);
                        (_screen as IAnsiDecoderClient).MoveCursorToColumn(null, _screen.Width-1);
                        rect = GetCursorRect();
                        rect.Inflate(1, 1);
                        Invalidate(rect);
                    }
                    break;
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            //e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
            int yPos = _border;
            foreach (var line in _screen.Lines)
            {
                int xPos = _lineNumberWidth + _border;
                foreach (var run in line.Runs)
                {
                    System.Drawing.Font font = this.Font;

                    if (line.LineNumber != 0)
                    {
                        e.Graphics.DrawString($"{line.LineNumber}", font, new SolidBrush(Color.DarkGray), _lineNumberWidth, yPos,
                            new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far });
                    }

                    if (run.Attributes.Bold)
                    {
                        if (run.Attributes.Italic)
                        {
                            font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
                        }
                        else
                        {
                            font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
                        }
                    }
                    else if (run.Attributes.Italic)
                    {
                        font = new System.Drawing.Font(font.FontFamily, font.SizeInPoints, FontStyle.Italic, GraphicsUnit.Point);
                    }
                    var fg = Color.Black;
                    if (run.Attributes.ForegroundColor != Color.White)
                        fg = run.Attributes.ForegroundColor;

                    var text = line.Text[run.Start..(run.Start + run.Length)];
                    for (var i = 0; i < text.Length; i++)
                    {
                        e.Graphics.DrawString($"{text[i]}", font, new SolidBrush(fg), xPos + (i * (int)Math.Floor(_charSize.Width)), yPos);
                    }

                    if (run.HasTab)
                    {
                        var pen = new Pen(Color.Red, 1);
                        //e.Graphics.DrawRectangle(pen, xPos, yPos, text.Length * (int)Math.Floor(_charSize.Width), (int)Math.Floor(_charSize.Height));
                        e.Graphics.DrawString($"→", font, new SolidBrush(Color.DarkGray), xPos, yPos);
                    }
                    xPos += (int)Math.Floor(_charSize.Width) * text.Length;
                }
                yPos += (int)Math.Floor(_charSize.Height);

                if (_screen.CursorPosition.Y >= 0 && _screen.CursorPosition.Y < _screen.Lines.Count)
                {
                    var pen = new Pen(Color.Blue, 1);
                    e.Graphics.DrawRectangle(pen, GetCursorRect());
                }
            }
            e.Graphics.DrawRectangle(new Pen(Color.Green), _lineNumberWidth + _border, _border, _screen.Width * (int)Math.Floor(_charSize.Width), _screen.Height * (int)Math.Floor(_charSize.Height));
        }

        private Rectangle GetCursorRect()
        {
            var rect = new Rectangle(_lineNumberWidth + _border + _screen.CursorPosition.X * (int)Math.Floor(_charSize.Width), 
                _border + _screen.CursorPosition.Y * (int)Math.Floor(_charSize.Height), 
                (int)Math.Floor(_charSize.Width), 
                (int)Math.Floor(_charSize.Height));
            return rect;
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            Load();
        }
    }
}
