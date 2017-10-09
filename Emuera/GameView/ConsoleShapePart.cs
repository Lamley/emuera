using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MinorShift._Library;

namespace MinorShift.Emuera.GameView
{
    internal abstract class ConsoleShapePart : AConsoleColoredPart
    {
        public override bool CanDivide => false;

        public static ConsoleShapePart CreateShape(string shapeType, int[] param, Color color, Color bcolor,
            bool colorchanged)
        {
            var type = shapeType.ToLower();
            colorchanged = colorchanged || color != Config.ForeColor;
            var sb = new StringBuilder();
            sb.Append("<shape type='");
            sb.Append(type);
            sb.Append("' param='");
            for (var i = 0; i < param.Length; i++)
            {
                sb.Append(param[i].ToString());
                if (i < param.Length - 1)
                    sb.Append(", ");
            }
            sb.Append("'");
            if (colorchanged)
            {
                sb.Append(" color='");
                sb.Append(HtmlManager.GetColorToString(color));
                sb.Append("'");
            }
            if (bcolor != Config.FocusColor)
            {
                sb.Append(" bcolor='");
                sb.Append(HtmlManager.GetColorToString(bcolor));
                sb.Append("'");
            }
            sb.Append(">");
            ConsoleShapePart ret = null;
            var lineHeight = Config.FontSize;
            var paramPixel = new float[param.Length];
            for (var i = 0; i < param.Length; i++)
                paramPixel[i] = (float) param[i] * lineHeight / 100f;
            RectangleF rectF;

            switch (type)
            {
                case "space":
                    if (paramPixel.Length == 1 && paramPixel[0] >= 0)
                    {
                        rectF = new RectangleF(0, 0, paramPixel[0], lineHeight);
                        ret = new ConsoleSpacePart(rectF);
                    }
                    break;
                case "rect":
                    if (paramPixel.Length == 1 && paramPixel[0] > 0)
                    {
                        rectF = new RectangleF(0, 0, paramPixel[0], lineHeight);
                        ret = new ConsoleRectangleShapePart(rectF);
                    }
                    else if (paramPixel.Length == 4)
                    {
                        rectF = new RectangleF(paramPixel[0], paramPixel[1], paramPixel[2], paramPixel[3]);
                        //1820a12 サイズ上限撤廃
                        if (rectF.X >= 0 && rectF.Width > 0 && rectF.Height > 0)
                            //	rectF.Y >= 0 && (rectF.Y + rectF.Height) <= lineHeight)
                            ret = new ConsoleRectangleShapePart(rectF);
                    }
                    break;
                case "polygon":
                    break;
            }
            if (ret == null)
                ret = new ConsoleErrorShapePart(sb.ToString());
            ret.AltText = sb.ToString();
            ret.Color = color;
            ret.ButtonColor = bcolor;
            ret.colorChanged = colorchanged;
            return ret;
        }

        public override string ToString()
        {
            if (AltText == null)
                return "";
            return AltText;
        }
    }

    internal sealed class ConsoleRectangleShapePart : ConsoleShapePart
    {
        private readonly RectangleF originalRectF;
        private Rectangle rect;
        private bool visible;

        public ConsoleRectangleShapePart(RectangleF theRect)
        {
            Str = "";
            originalRectF = theRect;
            WidthF = theRect.X + theRect.Width;
            rect.Y = (int) theRect.Y;
            //if (rect.Y == 0 && theRect.Y >= 0.001f)
            //	rect.Y = 1;
            rect.Height = (int) theRect.Height;
            if (rect.Height == 0 && theRect.Height >= 0.001f)
                rect.Height = 1;
            Top = Math.Min(0, rect.Y);
            Bottom = Math.Max(Config.FontSize, rect.Y + rect.Height);
        }

        public override int Top { get; }

        public override int Bottom { get; }

        public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
        {
            if (!visible)
                return;
            var targetRect = rect;
            targetRect.X = targetRect.X + PointX;
            targetRect.Y = targetRect.Y + pointY;
            var dcolor = isSelecting ? ButtonColor : Color;
            graph.FillRectangle(new SolidBrush(dcolor), targetRect);
        }

        public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
        {
            if (!visible)
                return;
            var targetRect = rect;
            targetRect.X = targetRect.X + PointX;
            targetRect.Y = targetRect.Y + pointY;
            var dcolor = isSelecting ? ButtonColor : Color;
            GDI.FillRect(targetRect, dcolor, dcolor);
        }

        public override void SetWidth(StringMeasure sm, float subPixel)
        {
            var widF = subPixel + WidthF;
            Width = (int) widF;
            XsubPixel = widF - Width;
            rect.X = (int) (subPixel + originalRectF.X);
            rect.Width = Width - rect.X;
            rect.X += Config.DrawingParam_ShapePositionShift;
            visible = rect.X >= 0 && rect.Width > 0; // && rect.Y >= 0 && (rect.Y + rect.Height) <= Config.FontSize);
        }
    }

    internal sealed class ConsoleSpacePart : ConsoleShapePart
    {
        public ConsoleSpacePart(RectangleF theRect)
        {
            Str = "";
            WidthF = theRect.Width;
            //Width = width;
        }

        public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
        {
        }

        public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
        {
        }

        public override void SetWidth(StringMeasure sm, float subPixel)
        {
            var widF = subPixel + WidthF;
            Width = (int) widF;
            XsubPixel = widF - Width;
        }
    }

    internal sealed class ConsoleErrorShapePart : ConsoleShapePart
    {
        public ConsoleErrorShapePart(string errMes)
        {
            Str = errMes;
            AltText = errMes;
        }

        public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
        {
            if (mode == TextDrawingMode.GRAPHICS)
                graph.DrawString(Str, Config.Font, new SolidBrush(Config.ForeColor), new Point(PointX, pointY));
            else
                TextRenderer.DrawText(graph, Str, Config.Font, new Point(PointX, pointY), Config.ForeColor,
                    TextFormatFlags.NoPrefix);
        }

        public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
        {
            GDI.TabbedTextOutFull(Config.Font, Config.ForeColor, Str, PointX, pointY);
        }

        public override void SetWidth(StringMeasure sm, float subPixel)
        {
            if (Error)
            {
                Width = 0;
                return;
            }
            Width = sm.GetDisplayLength(Str, Config.Font);
            XsubPixel = subPixel;
        }
    }
}