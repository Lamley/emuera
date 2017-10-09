using System;
using System.IO;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    ///     文字列を1文字ずつ評価するためのクラス
    /// </summary>
    internal sealed class StringStream
    {
        public const char EndOfString = '\0';

        public StringStream(string s)
        {
            RowString = s;
            if (RowString == null)
                RowString = "";
            CurrentPosition = 0;
        }

        public string RowString { get; private set; }

        public int CurrentPosition { get; set; }

        public char Current
        {
            get
            {
                if (CurrentPosition >= RowString.Length)
                    return EndOfString;
                return RowString[CurrentPosition];
            }
        }

        /// <summary>
        ///     文字列終端に達した
        /// </summary>
        public bool EOS => CurrentPosition >= RowString.Length;

        ///変数の区切りである"[["と"]]"の先読みなどに使用
        public char Next
        {
            get
            {
                if (CurrentPosition + 1 >= RowString.Length)
                    return EndOfString;
                return RowString[CurrentPosition + 1];
            }
        }

        public void AppendString(string str)
        {
            if (CurrentPosition > RowString.Length)
                CurrentPosition = RowString.Length;
            RowString += " " + str;
        }

        public string Substring()
        {
            if (CurrentPosition >= RowString.Length)
                return "";
            if (CurrentPosition == 0)
                return RowString;
            return RowString.Substring(CurrentPosition);
        }

        public string Substring(int start, int length)
        {
            if (start >= RowString.Length || length == 0)
                return "";
            if (start + length > RowString.Length)
                length = RowString.Length - start;
            return RowString.Substring(start, length);
        }

        internal void Replace(int start, int count, string src)
        {
            //引数に正しい数字が送られてくること前提
            RowString = RowString.Remove(start, count).Insert(start, src);
            CurrentPosition = start;
        }

        public void ShiftNext()
        {
            CurrentPosition++;
        }

        public void Jump(int skip)
        {
            CurrentPosition += skip;
        }

        /// <summary>
        ///     検索文字列の相対位置を返す。見つからない場合、負の値。
        /// </summary>
        /// <param name="str"></param>
        public int Find(string str)
        {
            return RowString.IndexOf(str, CurrentPosition) - CurrentPosition;
        }

        /// <summary>
        ///     検索文字列の相対位置を返す。見つからない場合、負の値。
        /// </summary>
        public int Find(char c)
        {
            return RowString.IndexOf(c, CurrentPosition) - CurrentPosition;
        }

        public override string ToString()
        {
            if (RowString == null)
                return "";
            return RowString;
        }

        public bool CurrentEqualTo(string rother)
        {
            if (CurrentPosition + rother.Length > RowString.Length)
                return false;

            for (var i = 0; i < rother.Length; i++)
                if (RowString[CurrentPosition + i] != rother[i])
                    return false;
            return true;
        }

        public bool TripleSymbol()
        {
            if (CurrentPosition + 3 > RowString.Length)
                return false;
            return RowString[CurrentPosition] == RowString[CurrentPosition + 1] &&
                   RowString[CurrentPosition] == RowString[CurrentPosition + 2];
        }


        public bool CurrentEqualTo(string rother, StringComparison comp)
        {
            if (CurrentPosition + rother.Length > RowString.Length)
                return false;
            var sub = RowString.Substring(CurrentPosition, rother.Length);
            return sub.Equals(rother, comp);
        }

        public void Seek(int offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                CurrentPosition = offset;
            else if (origin == SeekOrigin.Current)
                CurrentPosition = CurrentPosition + offset;
            else if (origin == SeekOrigin.End)
                CurrentPosition = RowString.Length + offset;
            if (CurrentPosition < 0)
                CurrentPosition = 0;
        }
    }
}