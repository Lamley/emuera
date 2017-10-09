using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MinorShift._Library;

namespace MinorShift.Emuera.GameView
{
    /// <summary>
    ///     テキスト長計測装置
    ///     1819 必要になるたびにCreateGraphicsする方式をやめてあらかじめGraphicsを用意しておくことにする
    /// </summary>
    internal sealed class StringMeasure : IDisposable
    {
        private readonly Bitmap bmp;
        private readonly float fontDisplaySize;

        private readonly Graphics graph;
        private readonly RectangleF layoutRect;
        private readonly Size layoutSize;
        private readonly CharacterRange[] ranges = {new CharacterRange(0, 1)};
        private readonly StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);

        private readonly TextDrawingMode textDrawingMode;


        private bool disposed;

        public StringMeasure()
        {
            textDrawingMode = Config.TextDrawingMode;
            layoutSize = new Size(Config.WindowX * 2, Config.LineHeight);
            layoutRect = new RectangleF(0, 0, Config.WindowX * 2, Config.LineHeight);
            fontDisplaySize = Config.Font.Size / 2 * 1.04f; //実際には指定したフォントより若干幅をとる？
            //bmp = new Bitmap(Config.WindowX, Config.LineHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            graph = Graphics.FromImage(bmp);
            if (textDrawingMode == TextDrawingMode.WINAPI)
                GDI.GdiMesureTextStart(graph);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (textDrawingMode == TextDrawingMode.WINAPI)
                GDI.GdiMesureTextEnd(graph);
            graph.Dispose();
            bmp.Dispose();
            sf.Dispose();
        }

        public int GetDisplayLength(string s, Font font)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            if (textDrawingMode == TextDrawingMode.GRAPHICS)
            {
                if (s.Contains("\t"))
                    s = s.Replace("\t", "        ");
                ranges[0].Length = s.Length;
                //CharacterRange[] ranges = new CharacterRange[] { new CharacterRange(0, s.Length) };
                sf.SetMeasurableCharacterRanges(ranges);
                var regions = graph.MeasureCharacterRanges(s, font, layoutRect, sf);
                var rectF = regions[0].GetBounds(graph);
                //return (int)rectF.Width;//プロポーショナルでなくても数ピクセルずれる
                return (int) ((int) ((rectF.Width - 1) / fontDisplaySize + 0.95f) * fontDisplaySize);
            }
            if (textDrawingMode == TextDrawingMode.TEXTRENDERER)
            {
                var size = TextRenderer.MeasureText(graph, s, font, layoutSize,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                //Size size = TextRenderer.MeasureText(g, s, StaticConfig.Font);
                return size.Width;
            }
            else // if (StaticConfig.TextDrawingMode == TextDrawingMode.WINAPI)
            {
                var size = GDI.MeasureText(s, font);
                return size.Width;
            }
            //来るわけがない
            //else
            //    throw new ExeEE("描画モード不明");
        }
    }
}