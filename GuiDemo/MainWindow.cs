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

        Screen _screen;
        IAnsiDecoder _vt100 = new AnsiDecoder();

        Font _font = new Font("Cascadia Code", 8, FontStyle.Regular);

        int _width = 100;
        int _height = 500;

        public MainWindow()
        {
            InitializeComponent();

            Paint += MainWindow_Paint;

            

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = CodePagesEncodingProvider.Instance.GetEncoding("ibm437");

            _screen = new Screen(_width, _height);
            _vt100.Encoding = CodePagesEncodingProvider.Instance.GetEncoding("ibm437");
            _vt100.Subscribe(_screen);

            using (Stream stream = File.Open(@"..\..\..\..\tests\zv-v01d.ans", FileMode.Open))
            {
                int read = 0;
                while ((read = stream.ReadByte()) != -1)
                {
                    _vt100.Input(new byte[] { (byte)read });
                }
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            for (int y = 0; y < _height; ++y)
            {
                for (int x = 0; x < _width; ++x)
                {
                    Character character = _screen[x, y];

                    Rectangle rect = new Rectangle(_font.Height * x, _font.Height * y, _font.Height, _font.Height);
                    e.Graphics.FillRectangle(new SolidBrush(character.Attributes.BackgroundColor), rect);

                    Font font = _font;
                    if (character.Attributes.Bold)
                    {
                        if (character.Attributes.Italic)
                        {
                            font = new Font(_font.FontFamily, _font.Size, FontStyle.Bold | FontStyle.Italic);
                        }
                        else
                        {
                            font = new Font(_font.FontFamily, _font.Size, FontStyle.Bold);
                        }
                    }
                    else if (character.Attributes.Italic)
                    {
                        font = new Font(_font.FontFamily, _font.Size, FontStyle.Italic);
                    }
                    String text = new String(character.Char, 1);
                    e.Graphics.DrawString(text, font, new SolidBrush(character.Attributes.ForegroundColor), rect);
                }
            }
        }
    }
}
