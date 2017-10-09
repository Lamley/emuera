using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MinorShift.Emuera.Sub;
using MinorShift._Library;

namespace MinorShift.Emuera.GameView
{
    //1820 EmueraConsoleのうちdisplayLineListやprintBufferに触るもの
    //いつかEmueraConsoleから分離したい
    internal sealed partial class EmueraConsole : IDisposable
    {
        private readonly List<ConsoleDisplayLine> displayLineList;

        private readonly PrintStringBuffer printBuffer;
        private readonly StringMeasure stringMeasure = new StringMeasure();
        public Color bgColor = Config.BackColor;
        public bool noOutputLog = false;

        public void ClearDisplay()
        {
            displayLineList.Clear();
            LineCount = 0;
            lineNo = 0;
            lastDrawnLineNo = -1;
            verticalScrollBarUpdate();
            window.Refresh(); //OnPaint発行
        }


        private bool outputLog(string fullpath)
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(fullpath, false, Encoding.Unicode);
                foreach (var line in displayLineList)
                    writer.WriteLine(line.ToString());
            }
            catch (Exception)
            {
                MessageBox.Show("ログの出力に失敗しました", "ログ出力失敗");
                return false;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            return true;
        }


        public bool OutputLog(string filename)
        {
            if (filename == null)
                filename = Program.ExeDir + "emuera.log";

            if (!filename.StartsWith(Program.ExeDir, StringComparison.CurrentCultureIgnoreCase))
            {
                MessageBox.Show("ログファイルは実行ファイル以下のディレクトリにのみ保存できます", "ログ出力失敗");
                return false;
            }

            if (outputLog(filename))
            {
                if (window.Created)
                {
                    PrintSystemLine("※※※ログファイルを" + filename + "に出力しました※※※");
                    RefreshStrings(true);
                }
                return true;
            }
            return false;
        }

        public void GetDisplayStrings(StringBuilder builder)
        {
            if (displayLineList.Count == 0)
                return;
            for (var i = 0; i < displayLineList.Count; i++)
                builder.AppendLine(displayLineList[i].ToString());
        }

        public ConsoleDisplayLine[] GetDisplayLines(long lineNo)
        {
            if (lineNo < 0 || lineNo > displayLineList.Count)
                return null;
            var count = 0;
            var list = new List<ConsoleDisplayLine>();
            for (var i = displayLineList.Count - 1; i >= 0; i--)
            {
                if (count == lineNo)
                    list.Insert(0, displayLineList[i]);
                if (displayLineList[i].IsLogicalLine)
                    count++;
                if (count > lineNo)
                    break;
            }
            if (list.Count == 0)
                return null;
            var ret = new ConsoleDisplayLine[list.Count];
            list.CopyTo(ret);
            return ret;
        }

        public ConsoleDisplayLine[] PopDisplayingLines()
        {
            if (!Enabled)
                return null;
            if (printBuffer.IsEmpty)
                return null;
            return printBuffer.Flush(stringMeasure, force_temporary);
        }


        #region Print系

        //private bool useUserStyle = true;
        public bool UseUserStyle { get; set; }

        public bool UseSetColorStyle { get; set; }
        private StringStyle defaultStyle = new StringStyle(Config.ForeColor, FontStyle.Regular, null);

        private StringStyle userStyle = new StringStyle(Config.ForeColor, FontStyle.Regular, null);

        //private StringStyle style = new StringStyle(Config.ForeColor, FontStyle.Regular, null);
        private StringStyle Style
        {
            get
            {
                if (!UseUserStyle)
                    return defaultStyle;
                if (UseSetColorStyle)
                    return userStyle;
                //PRINTD系(SETCOLORを無視する)
                if (userStyle.Color == defaultStyle.Color)
                    return userStyle;
                return new StringStyle(defaultStyle.Color, userStyle.FontStyle, userStyle.Fontname);
            }
        }

        //private StringStyle Style { get { return (useUserStyle ? userStyle : defaultStyle); } }
        public StringStyle StringStyle => userStyle;

        public void SetStringStyle(FontStyle fs)
        {
            userStyle.FontStyle = fs;
        }

        public void SetStringStyle(Color color)
        {
            userStyle.Color = color;
            userStyle.ColorChanged = color != Config.ForeColor;
        }

        public void SetFont(string fontname)
        {
            if (!string.IsNullOrEmpty(fontname)) userStyle.Fontname = fontname;
            else userStyle.Fontname = Config.FontName;
        }

        public DisplayLineAlignment Alignment { get; set; } = DisplayLineAlignment.LEFT;

        public void ResetStyle()
        {
            userStyle = defaultStyle;
            Alignment = DisplayLineAlignment.LEFT;
        }

        public bool EmptyLine => printBuffer.IsEmpty;

        /// <summary>
        ///     DRAWLINE用文字列
        /// </summary>
        private string stBar;

        private uint lastBgColorChange;
        private bool forceTextBoxColor;

        public void SetBgColor(Color color)
        {
            bgColor = color;
            forceTextBoxColor = true;
            //REDRAWされない場合はTextBoxの色は変えずにフラグだけ立てる
            //最初の再描画時に現在の背景色に合わせる
            if (Redraw == ConsoleRedraw.None && window.ScrollBar.Value == window.ScrollBar.Maximum)
                return;
            var sec = WinmmTimer.TickCount - lastBgColorChange;
            //色変化が速くなりすぎないように一定時間以内の再呼び出しは強制待ちにする
            while (sec < 200)
            {
                Application.DoEvents();
                sec = WinmmTimer.TickCount - lastBgColorChange;
            }
            RefreshStrings(true);
            lastBgColorChange = WinmmTimer.TickCount;
        }

        /// <summary>
        ///     最後に描画した時にlineNoの値
        /// </summary>
        private int lastDrawnLineNo = -1;

        private int lineNo;
        public long LineCount { get; private set; }

        private void addRangeDisplayLine(ConsoleDisplayLine[] lineList)
        {
            for (var i = 0; i < lineList.Length; i++)
                addDisplayLine(lineList[i], false);
        }

        private void addDisplayLine(ConsoleDisplayLine line, bool force_LEFT)
        {
            if (LastLineIsTemporary)
                deleteLine(1);
            //不適正なFontのチェック
            AConsoleDisplayPart errorStr = null;
            foreach (var button in line.Buttons)
            foreach (var css in button.StrArray)
                if (css.Error)
                {
                    errorStr = css;
                    break;
                }
            if (errorStr != null)
            {
                MessageBox.Show("Emueraの表示処理中に不適正なフォントを検出しました\n描画処理を続行できないため強制終了します", "フォント不適正");
                Quit();
                return;
            }
            if (force_LEFT)
                line.SetAlignment(DisplayLineAlignment.LEFT);
            else
                line.SetAlignment(Alignment);
            line.LineNo = lineNo;
            displayLineList.Add(line);
            lineNo++;
            if (line.IsLogicalLine)
                LineCount++;
            if (lineNo == int.MaxValue)
            {
                lastDrawnLineNo = -1;
                lineNo = 0;
            }
            if (LineCount == long.MaxValue)
                LineCount = 0;
            if (displayLineList.Count > Config.MaxLog)
                displayLineList.RemoveAt(0);
        }


        public void deleteLine(int argNum)
        {
            var delNum = 0;
            var num = argNum;
            while (delNum < num)
            {
                if (displayLineList.Count == 0)
                    break;
                var line = displayLineList[displayLineList.Count - 1];
                displayLineList.RemoveAt(displayLineList.Count - 1);
                lineNo--;
                if (line.IsLogicalLine)
                {
                    delNum++;
                    LineCount--;
                }
            }
            if (lineNo < 0)
                lineNo += int.MaxValue;
            lastDrawnLineNo = -1;
            //RefreshStrings(true);
        }

        public bool LastLineIsTemporary
        {
            get
            {
                if (displayLineList.Count == 0)
                    return false;
                return displayLineList[displayLineList.Count - 1].IsTemporary;
            }
        }

        //最終行を書き換え＋次の行追加時にはその行を再利用するように設定
        public void PrintTemporaryLine(string str)
        {
            PrintSingleLine(str, true);
        }

        //最終行だけを書き換える
        private void changeLastLine(string str)
        {
            deleteLine(1);
            PrintSingleLine(str, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="position"></param>
        /// <param name="level">警告レベル.0:軽微なミス.1:無視できる行.2:行が実行されなければ無害.3:致命的</param>
        public void PrintWarning(string str, ScriptPosition position, int level)
        {
            if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
                return;
            //警告だけは強制表示
            var b = force_temporary;
            force_temporary = false;
            if (position != null)
                if (position.LineNo >= 0)
                {
                    PrintErrorButton(
                        string.Format("警告Lv{0}:{1}:{2}行目:{3}", level, position.Filename, position.LineNo, str),
                        position);
                    if (position.RowLine != null)
                        PrintError(position.RowLine);
                }
                else
                {
                    PrintErrorButton(string.Format("警告Lv{0}:{1}:{2}", level, position.Filename, str), position);
                }
            else
                PrintError(string.Format("警告Lv{0}:{1}", level, str));
            force_temporary = b;
        }


        /// <summary>
        ///     ユーザー指定のフォントを無視する。ウィンドウサイズを考慮せず確実に一行で書く。システム用。
        /// </summary>
        /// <param name="str"></param>
        public void PrintSystemLine(string str)
        {
            PrintFlush(false);
            //RefreshStrings(false);
            UseUserStyle = false;
            PrintSingleLine(str, false);
        }

        public void PrintError(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (Program.DebugMode)
            {
                DebugPrint(str);
                DebugNewLine();
            }
            PrintFlush(false);
            UseUserStyle = false;
            var dispLine = PrintPlainwithSingleLine(str);
            if (dispLine == null)
                return;
            addDisplayLine(dispLine, true);
            RefreshStrings(false);
        }

        internal void PrintErrorButton(string str, ScriptPosition pos)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (Program.DebugMode)
            {
                DebugPrint(str);
                DebugNewLine();
            }
            UseUserStyle = false;
            var dispLine = printBuffer.AppendAndFlushErrButton(str, Style, ErrorButtonsText, pos, stringMeasure);
            if (dispLine == null)
                return;
            addDisplayLine(dispLine, true);
            RefreshStrings(false);
        }

        /// <summary>
        ///     1813 従来のPrintLineを用途を考慮してPrintSingleLineとPrintSystemLineに分割
        /// </summary>
        /// <param name="str"></param>
        public void PrintSingleLine(string str)
        {
            PrintSingleLine(str, false);
        }

        public void PrintSingleLine(string str, bool temporary)
        {
            if (string.IsNullOrEmpty(str))
                return;
            PrintFlush(false);
            printBuffer.Append(str, Style);
            var dispLine = BufferToSingleLine(true, temporary);
            if (dispLine == null)
                return;
            addDisplayLine(dispLine, false);
            RefreshStrings(false);
        }

        public void Print(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (str.Contains("\n"))
            {
                var newline = str.IndexOf('\n');
                var upper = str.Substring(0, newline);
                printBuffer.Append(upper, Style);
                NewLine();
                if (newline < str.Length - 1)
                {
                    var lower = str.Substring(newline + 1);
                    Print(lower);
                }
                return;
            }
            printBuffer.Append(str, Style);
        }


        public void PrintImg(string str)
        {
            printBuffer.Append(new ConsoleImagePart(str, null, 0, 0, 0));
        }

        public void PrintShape(string type, int[] param)
        {
            var part = ConsoleShapePart.CreateShape(type, param, userStyle.Color, userStyle.ButtonColor, false);
            printBuffer.Append(part);
        }

        public void PrintHtml(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (!Enabled)
                return;
            if (!printBuffer.IsEmpty)
            {
                var dispList = printBuffer.Flush(stringMeasure, force_temporary);
                addRangeDisplayLine(dispList);
            }
            addRangeDisplayLine(HtmlManager.Html2DisplayLine(str, stringMeasure, this));
            RefreshStrings(false);
        }

        private int printCWidth = -1;
        private int printCWidthL = -1;
        private int printCWidthL2 = -1;

        public void PrintC(string str, bool alignmentRight)
        {
            if (string.IsNullOrEmpty(str))
                return;

            printBuffer.Append(CreateTypeCString(str, alignmentRight), Style, true);
        }

        private void calcPrintCWidth(StringMeasure stringMeasure)
        {
            var str = new string(' ', Config.PrintCLength);
            var font = Config.Font;
            printCWidth = stringMeasure.GetDisplayLength(str, font);

            str += " ";
            printCWidthL = stringMeasure.GetDisplayLength(str, font);

            str += " ";
            printCWidthL2 = stringMeasure.GetDisplayLength(str, font);
        }

        private string CreateTypeCString(string str, bool alignmentRight)
        {
            if (printCWidth == -1)
                calcPrintCWidth(stringMeasure);
            var length = 0;
            var width = 0;
            if (str != null)
                length = Config.Encode.GetByteCount(str);
            var printcLength = Config.PrintCLength;
            Font font = null;
            try
            {
                font = new Font(Style.Fontname, Config.Font.Size, Style.FontStyle, GraphicsUnit.Pixel);
            }
            catch
            {
                return str;
            }

            if (alignmentRight && length < printcLength)
            {
                str = new string(' ', printcLength - length) + str;
                width = stringMeasure.GetDisplayLength(str, font);
                while (width > printCWidth)
                {
                    if (str[0] != ' ')
                        break;
                    str = str.Remove(0, 1);
                    width = stringMeasure.GetDisplayLength(str, font);
                }
            }
            else if (!alignmentRight && length < printcLength + 1)
            {
                str += new string(' ', printcLength + 1 - length);
                width = stringMeasure.GetDisplayLength(str, font);
                while (width > printCWidthL)
                {
                    if (str[str.Length - 1] != ' ')
                        break;
                    str = str.Remove(str.Length - 1, 1);
                    width = stringMeasure.GetDisplayLength(str, font);
                }
            }
            return str;
        }

        internal void PrintButton(string str, string p)
        {
            if (string.IsNullOrEmpty(str))
                return;
            printBuffer.AppendButton(str, Style, p);
        }

        internal void PrintButton(string str, long p)
        {
            if (string.IsNullOrEmpty(str))
                return;
            printBuffer.AppendButton(str, Style, p);
        }

        internal void PrintButtonC(string str, string p, bool isRight)
        {
            if (string.IsNullOrEmpty(str))
                return;
            printBuffer.AppendButton(CreateTypeCString(str, isRight), Style, p);
        }

        internal void PrintButtonC(string str, long p, bool isRight)
        {
            if (string.IsNullOrEmpty(str))
                return;
            printBuffer.AppendButton(CreateTypeCString(str, isRight), Style, p);
        }

        internal void PrintPlain(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            printBuffer.AppendPlainText(str, Style);
        }

        public void NewLine()
        {
            PrintFlush(true);
            RefreshStrings(false);
        }

        public ConsoleDisplayLine BufferToSingleLine(bool force, bool temporary)
        {
            if (!Enabled)
                return null;
            if (!force && printBuffer.IsEmpty)
                return null;
            if (force && printBuffer.IsEmpty)
                printBuffer.Append(" ", Style);
            var dispLine = printBuffer.FlushSingleLine(stringMeasure, temporary | force_temporary);
            return dispLine;
        }

        internal ConsoleDisplayLine PrintPlainwithSingleLine(string str)
        {
            if (!Enabled)
                return null;
            if (string.IsNullOrEmpty(str))
                return null;
            printBuffer.AppendPlainText(str, Style);
            var dispLine = printBuffer.FlushSingleLine(stringMeasure, false);
            return dispLine;
        }

        /// <summary>
        /// </summary>
        /// <param name="force">バッファーが空でも改行する</param>
        public void PrintFlush(bool force)
        {
            if (!Enabled)
                return;
            if (!force && printBuffer.IsEmpty)
                return;
            if (force && printBuffer.IsEmpty)
                printBuffer.Append(" ", Style);
            var dispList = printBuffer.Flush(stringMeasure, force_temporary);
            //ConsoleDisplayLine[] dispList = printBuffer.Flush(stringMeasure, temporary | force_temporary);
            addRangeDisplayLine(dispList);
            //1819描画命令は分離
            //RefreshStrings(false);
        }

        /// <summary>
        ///     DRAWLINE命令に対応。これのフォントを変更できると面倒なことになるのでRegularに固定する。
        /// </summary>
        public void PrintBar()
        {
            //初期に設定済みなので見る必要なし
            //if (stBar == null)
            //    setStBar(StaticConfig.DrawLineString);

            //1806beta001 CompatiDRAWLINEの廃止、CompatiLinefeedAs1739へ移行
            //CompatiLinefeedAs1739の処理はPrintStringBuffer.csで行う
            //if (Config.CompatiDRAWLINE)
            //	PrintFlush(false);
            var ss = userStyle;
            userStyle.FontStyle = FontStyle.Regular;
            Print(stBar);
            userStyle = ss;
        }

        public void printCustomBar(string barStr)
        {
            if (string.IsNullOrEmpty(barStr))
                throw new CodeEE("空文字列によるDRAWLINEが行われました");
            var ss = userStyle;
            userStyle.FontStyle = FontStyle.Regular;
            Print(getStBar(barStr));
            userStyle = ss;
        }

        public string getDefStBar()
        {
            return stBar;
        }

        public string getStBar(string barStr)
        {
            var bar = new StringBuilder();
            bar.Append(barStr);
            var width = 0;
            var font = Config.Font;
            while (width < Config.DrawableWidth)
            {
//境界を越えるまで一文字ずつ増やす
                bar.Append(barStr);
                width = stringMeasure.GetDisplayLength(bar.ToString(), font);
            }
            while (width > Config.DrawableWidth)
            {
//境界を越えたら、今度は超えなくなるまで一文字ずつ減らす（barStrに複数字の文字列がきた場合に対応するため）
                bar.Remove(bar.Length - 1, 1);
                width = stringMeasure.GetDisplayLength(bar.ToString(), font);
            }
            return bar.ToString();
        }

        public void setStBar(string barStr)
        {
            stBar = getStBar(barStr);
        }

        #endregion
    }
}