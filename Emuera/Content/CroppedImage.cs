using System.Drawing;

namespace MinorShift.Emuera.Content
{
    internal sealed class CroppedImage : AContentItem
    {
        public readonly BaseImage BaseImage;
        public readonly bool NoResize;
        public readonly Rectangle Rectangle;

        public CroppedImage(string name, BaseImage image, Rectangle rect, bool noresize) : base(name)
        {
            BaseImage = image;
            Rectangle = rect;
            if (image != null)
                Enabled = image.Enabled;
            if (rect.Width <= 0 || rect.Height <= 0)
                Enabled = false;
            NoResize = noresize;
        }
    }
}