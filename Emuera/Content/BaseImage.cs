using System;
using System.Drawing;
using MinorShift._Library;

namespace MinorShift.Emuera.Content
{
    internal sealed class BaseImage : AContentFile
    {
        public Bitmap Bitmap;
        private Graphics g;
        private IntPtr hBitmap;
        private IntPtr hDefaultImg;

        public BaseImage(string name, string path)
            : base(name, path)
        {
        }

        public IntPtr GDIhDC { get; private set; }

        public void Load(bool useGDI)
        {
            if (Loaded)
                return;
            try
            {
                Bitmap = new Bitmap(Filepath);
                if (useGDI)
                {
                    hBitmap = Bitmap.GetHbitmap();
                    g = Graphics.FromImage(Bitmap);
                    GDIhDC = g.GetHdc();
                    hDefaultImg = GDI.SelectObject(GDIhDC, hBitmap);
                }
                Loaded = true;
                Enabled = true;
            }
            catch
            {
            }
        }

        public override void Dispose()
        {
            if (Bitmap == null)
                return;
            if (g != null)
            {
                GDI.SelectObject(GDIhDC, hDefaultImg);
                GDI.DeleteObject(hBitmap);
                g.ReleaseHdc(GDIhDC);
                g.Dispose();
                g = null;
            }
            Bitmap.Dispose();
            Bitmap = null;
        }
    }
}