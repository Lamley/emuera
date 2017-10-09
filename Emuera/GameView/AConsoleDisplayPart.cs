﻿using System.Drawing;

namespace MinorShift.Emuera.GameView
{
    /// <summary>
    ///     描画の最小単位
    /// </summary>
    internal abstract class AConsoleDisplayPart
    {
        public bool Error { get; protected set; }

        public string Str { get; protected set; }
        public string AltText { get; protected set; }
        public int PointX { get; set; }
        public float XsubPixel { get; set; }
        public float WidthF { get; set; }
        public int Width { get; set; }
        public virtual int Top => 0;
        public virtual int Bottom => Config.FontSize;
        public abstract bool CanDivide { get; }

        public abstract void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode);
        public abstract void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog);

        public abstract void SetWidth(StringMeasure sm, float subPixel);

        public override string ToString()
        {
            if (Str == null)
                return "";
            return Str;
        }
    }

    /// <summary>
    ///     色つき
    /// </summary>
    internal abstract class AConsoleColoredPart : AConsoleDisplayPart
    {
        protected bool colorChanged;
        protected Color Color { get; set; }
        protected Color ButtonColor { get; set; }
    }
}