using libvt100;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        DynamicScreen _screen;
        IAnsiDecoder _vt100 = new AnsiDecoder();

        Font _font = new Font("Consolas", 10, FontStyle.Regular);
        StringFormat _sf = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags =  StringFormatFlags.FitBlackBox |
                            StringFormatFlags.DisplayFormatControl | StringFormatFlags.MeasureTrailingSpaces,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.None
        };
        int _charWidth = 0;

        int _width = 120;
        //int _height = 500;

        public MainWindow()
        {
            InitializeComponent();

            Paint += MainWindow_Paint;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _screen = new DynamicScreen(_width);
            _vt100.Encoding = Encoding.UTF8;
            _vt100.Subscribe(_screen);

            using (Stream stream = File.Open(@"..\..\..\..\tests\Program.cs.ans", FileMode.Open))
            {
                int read = 0;
                while ((read = stream.ReadByte()) != -1)
                {
                    _vt100.Input(new byte[] { (byte)read });
                }
            }
            _screen.CursorPosition = new Point(0, 0);

            _charWidth = (int)Graphics.FromHwnd(this.Handle).MeasureString("w", _font, 100, _sf).Width;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            base.OnKeyUp(e);

            switch (e.KeyCode)
            {
                case Keys.Down:
                    Invalidate(GetCursorRect());
                    if (_screen.CursorPosition.Y < _screen.Lines.Count)
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Down, 1);
                    Invalidate(GetCursorRect());
                    break;

                case Keys.Up:
                    Invalidate(GetCursorRect());
                    if (_screen.CursorPosition.Y > 0)
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Up, 1);
                    Invalidate(GetCursorRect());
                    break;

                case Keys.Right:
                    Invalidate(GetCursorRect());
                    if (_screen.CursorPosition.Y < _screen.Width)
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Forward, 1);
                    Invalidate(GetCursorRect());
                    break;

                case Keys.Left:
                    Invalidate(GetCursorRect());
                    if (_screen.CursorPosition.X > 0)
                        (_screen as IAnsiDecoderClient).MoveCursor(null, libvt100.Direction.Backward, 1);
                    Invalidate(GetCursorRect());
                    break;
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            int yPos = 0;
            foreach (var line in _screen.Lines)
            {
                int xPos = 0;
                foreach (var run in line.Runs)
                {
                    System.Drawing.Font font = _font;
                    //if (run.Attributes.Bold)
                    //{
                    //    if (run.Attributes.Italic)
                    //    {
                    //        font = new System.Drawing.Font(_font.FontFamily, _font.SizeInPoints, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
                    //    }
                    //    else
                    //    {
                    //        font = new System.Drawing.Font(_font.FontFamily, _font.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
                    //    }
                    //}
                    //else if (run.Attributes.Italic)
                    //{
                    //    font = new System.Drawing.Font(_font.FontFamily, _font.SizeInPoints, FontStyle.Italic, GraphicsUnit.Point);
                    //}
                    var fg = Color.Black;
                    if (run.Attributes.ForegroundColor != Color.White)
                        fg = run.Attributes.ForegroundColor;

                    var text = line.Text[run.Start..(run.Start + run.Length)];
                    e.Graphics.DrawString(text, font, new SolidBrush(fg), xPos, yPos, _sf);
                    xPos += _charWidth * text.Length;
                }
                yPos += (int)Math.Round(_font.GetHeight());

                if (_screen.CursorPosition.Y >= 0 && _screen.CursorPosition.Y < _screen.Lines.Count)
                {
                    e.Graphics.DrawRectangle(Pens.Blue, GetCursorRect(e.Graphics));
                }
            }
        }

        private Rectangle GetCursorRect(Graphics g = null)
        {
            using Bitmap bitmap = new Bitmap(1, 1);
            if (g == null)
            {
                g = Graphics.FromImage(bitmap);
            }
            return new Rectangle(_screen.CursorPosition.X * _charWidth, _screen.CursorPosition.Y * (int)Math.Round(_font.GetHeight()), _charWidth, (int)Math.Round(_font.GetHeight()));
        }
    }
}
