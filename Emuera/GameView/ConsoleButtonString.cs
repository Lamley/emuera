using System.Collections.Generic;
using System.Drawing;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameView
{
    /// <summary>
    ///     ボタン。1つ以上の装飾付文字列（ConsoleStyledString）からなる。
    /// </summary>
    internal sealed class ConsoleButtonString
    {
        private EmueraConsole parent;

        public ConsoleButtonString(EmueraConsole console, AConsoleDisplayPart[] strs)
        {
            parent = console;
            StrArray = strs;
            IsButton = false;
            PointX = -1;
            Width = -1;
            ErrPos = null;
        }

        public ConsoleButtonString(EmueraConsole console, AConsoleDisplayPart[] strs, long input)
            : this(console, strs)
        {
            Input = input;
            Inputs = input.ToString();
            IsButton = true;
            IsInteger = true;
            if (console != null)
            {
                Generation = parent.NewButtonGeneration;
                console.UpdateGeneration();
            }
            ErrPos = null;
        }

        public ConsoleButtonString(EmueraConsole console, AConsoleDisplayPart[] strs, string inputs)
            : this(console, strs)
        {
            Inputs = inputs;
            IsButton = true;
            IsInteger = false;
            if (console != null)
            {
                Generation = parent.NewButtonGeneration;
                console.UpdateGeneration();
            }
            ErrPos = null;
        }

        public ConsoleButtonString(EmueraConsole console, AConsoleDisplayPart[] strs, long input, string inputs)
            : this(console, strs)
        {
            Input = input;
            Inputs = inputs;
            IsButton = true;
            IsInteger = true;
            if (console != null)
            {
                Generation = parent.NewButtonGeneration;
                console.UpdateGeneration();
            }
            ErrPos = null;
        }

        public ConsoleButtonString(EmueraConsole console, AConsoleDisplayPart[] strs, string inputs, ScriptPosition pos)
            : this(console, strs)
        {
            Inputs = inputs;
            IsButton = true;
            IsInteger = false;
            if (console != null)
            {
                Generation = parent.NewButtonGeneration;
                console.UpdateGeneration();
            }
            ErrPos = pos;
        }

        public AConsoleDisplayPart[] StrArray { get; private set; }

        public ConsoleDisplayLine ParentLine { get; set; }
        public bool IsButton { get; private set; }
        public bool IsInteger { get; private set; }
        public long Input { get; private set; }
        public string Inputs { get; private set; }
        public int PointX { get; set; }
        public bool PointXisLocked { get; set; }
        public int Width { get; set; }
        public float XsubPixel { get; set; }
        public long Generation { get; private set; }
        public ScriptPosition ErrPos { get; set; }
        public string Title { get; set; }


        public int RelativePointX { get; private set; }

        public void LockPointX(int rel_px)
        {
            PointX = rel_px * Config.FontSize / 100;
            XsubPixel = rel_px * Config.FontSize / 100.0f - PointX;
            PointXisLocked = true;
            RelativePointX = rel_px;
        }

        //indexの文字数の前方文字列とindex以降の後方文字列に分割
        public ConsoleButtonString DivideAt(int divIndex, StringMeasure sm)
        {
            if (divIndex <= 0)
                return null;
            var cssListA = new List<AConsoleDisplayPart>();
            var cssListB = new List<AConsoleDisplayPart>();
            var index = 0;
            var cssIndex = 0;
            var b = false;
            for (cssIndex = 0; cssIndex < StrArray.Length; cssIndex++)
            {
                if (b)
                {
                    cssListB.Add(StrArray[cssIndex]);
                    continue;
                }
                var length = StrArray[cssIndex].Str.Length;
                if (divIndex < index + length)
                {
                    var oldcss = StrArray[cssIndex] as ConsoleStyledString;
                    if (oldcss == null || !oldcss.CanDivide)
                        throw new ExeEE("文字列分割異常");
                    var newCss = oldcss.DivideAt(divIndex - index, sm);
                    cssListA.Add(oldcss);
                    if (newCss != null)
                        cssListB.Add(newCss);
                    b = true;
                    continue;
                }
                if (divIndex == index + length)
                {
                    cssListA.Add(StrArray[cssIndex]);
                    b = true;
                    continue;
                }
                index += length;
                cssListA.Add(StrArray[cssIndex]);
            }
            if (cssIndex >= StrArray.Length && cssListB.Count == 0)
                return null;
            var cssArrayA = new AConsoleDisplayPart[cssListA.Count];
            var cssArrayB = new AConsoleDisplayPart[cssListB.Count];
            cssListA.CopyTo(cssArrayA);
            cssListB.CopyTo(cssArrayB);
            StrArray = cssArrayA;
            var ret = new ConsoleButtonString(null, cssArrayB);
            CalcWidth(sm, XsubPixel);
            ret.CalcWidth(sm, 0);
            CalcPointX(PointX);
            ret.CalcPointX(PointX + Width);
            ret.parent = parent;
            ret.ParentLine = ParentLine;
            ret.IsButton = IsButton;
            ret.IsInteger = IsInteger;
            ret.Input = Input;
            ret.Inputs = Inputs;
            ret.Generation = Generation;
            ret.ErrPos = ErrPos;
            ret.Title = Title;
            return ret;
        }

        public void CalcWidth(StringMeasure sm, float subpixel)
        {
            Width = -1;
            if (StrArray != null && StrArray.Length > 0)
            {
                Width = 0;
                foreach (var css in StrArray)
                {
                    if (css.Width <= 0)
                        css.SetWidth(sm, subpixel);
                    Width += css.Width;
                    subpixel = css.XsubPixel;
                }
                if (Width <= 0)
                    Width = -1;
            }
            XsubPixel = subpixel;
        }

        /// <summary>
        ///     先にCalcWidthすること。
        /// </summary>
        /// <param name="sm"></param>
        public void CalcPointX(int pointx)
        {
            var px = pointx;
            if (!PointXisLocked)
                PointX = px;
            else
                px = PointX;
            for (var i = 0; i < StrArray.Length; i++)
            {
                StrArray[i].PointX = px;
                px += StrArray[i].Width;
            }
            if (StrArray.Length > 0)
            {
                PointX = StrArray[0].PointX;
                Width = StrArray[StrArray.Length - 1].PointX + StrArray[StrArray.Length - 1].Width - PointX;
                //if (Width < 0)
                //	Width = -1;
            }
        }

        internal void ShiftPositionX(int shiftX)
        {
            PointX += shiftX;
            foreach (var css in StrArray)
                css.PointX += shiftX;
        }

        public void DrawTo(Graphics graph, int pointY, bool isBackLog, TextDrawingMode mode)
        {
            var isSelecting = IsButton && parent.ButtonIsSelected(this);
            foreach (var css in StrArray)
                css.DrawTo(graph, pointY, isSelecting, isBackLog, mode);
        }

        public void GDIDrawTo(int pointY, bool isBackLog)
        {
            var isSelecting = IsButton && parent.ButtonIsSelected(this);
            foreach (var css in StrArray)
                css.GDIDrawTo(pointY, isSelecting, isBackLog);
        }

        public override string ToString()
        {
            if (StrArray == null)
                return "";
            var str = "";
            foreach (var css in StrArray)
                str += css.ToString();
            return str;
        }
    }
}