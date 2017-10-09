﻿using System.Drawing;

namespace MinorShift.Emuera.GameView
{
    /// <summary>
    ///     装飾付文字列(ConsoleStyledString)用のスタイル構造体
    /// </summary>
    internal struct StringStyle
    {
        public StringStyle(Color color, FontStyle fontStyle, string fontname)
        {
            Color = color;
            ButtonColor = Config.FocusColor;
            ColorChanged = false; //こっちのパターンでは色変更を後で検知
            FontStyle = fontStyle;
            if (string.IsNullOrEmpty(fontname))
                Fontname = Config.FontName;
            else
                Fontname = fontname;
        }

        /// <summary>
        ///     HTML用。ColorChangedを固定する。
        /// </summary>
        public StringStyle(Color color, bool colorChanged, Color buttonColor, FontStyle fontStyle, string fontname)
        {
            Color = color;
            ButtonColor = buttonColor;
            ColorChanged = colorChanged;
            FontStyle = fontStyle;
            if (string.IsNullOrEmpty(fontname))
                Fontname = Config.FontName;
            else
                Fontname = fontname;
        }

        public Color Color;
        public Color ButtonColor;
        public bool ColorChanged;
        public FontStyle FontStyle;
        public string Fontname;

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is StringStyle))
                return false;
            var ss = (StringStyle) obj;
            return Color == ss.Color && ButtonColor == ss.ButtonColor && ColorChanged == ss.ColorChanged &&
                   FontStyle == ss.FontStyle && Fontname.Equals(ss.Fontname, Config.SCIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Color.GetHashCode() ^ ButtonColor.GetHashCode() ^ ColorChanged.GetHashCode() ^
                   FontStyle.GetHashCode() ^ Fontname.GetHashCode();
        }

        public static bool operator ==(StringStyle x, StringStyle y)
        {
            return x.Color == y.Color && x.ButtonColor == y.ButtonColor && x.ColorChanged == y.ColorChanged &&
                   x.FontStyle == y.FontStyle && x.Fontname.Equals(y.Fontname, Config.SCIgnoreCase);
        }

        public static bool operator !=(StringStyle x, StringStyle y)
        {
            return !(x == y);
        }
    }
}