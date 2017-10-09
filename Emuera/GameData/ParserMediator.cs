using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera
{
    //1756 新設。ParserやLexicalAnalyzerなどが知りたい情報をまとめる
    //本当は引数として渡すべきなのかもしれないが全てのParserの引数を書きなおすのが面倒なのでstatic
    internal static class ParserMediator
    {
        private static EmueraConsole console;

        private static readonly List<ParserWarning> warningList = new List<ParserWarning>();

        public static bool HasWarning => warningList.Count > 0;

        /// <summary>
        ///     emuera.config等で発生した警告
        ///     Initializeより前に発生する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="?"></param>
        public static void ConfigWarn(string str, ScriptPosition pos, int level, string stack)
        {
            if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
                return;
            warningList.Add(new ParserWarning(str, pos, level, stack));
        }

        public static void Initialize(EmueraConsole console)
        {
            ParserMediator.console = console;
        }


        public static void Warn(string str, ScriptPosition pos, int level)
        {
            Warn(str, pos, level, null);
        }

        public static void Warn(string str, ScriptPosition pos, int level, string stack)
        {
            if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
                return;
            if (console != null && !console.RunERBFromMemory)
                warningList.Add(new ParserWarning(str, pos, level, stack));
        }

        /// <summary>
        ///     Parser中での警告出力
        /// </summary>
        /// <param name="str"></param>
        /// <param name="line"></param>
        /// <param name="level">警告レベル.0:軽微なミス.1:無視できる行.2:行が実行されなければ無害.3:致命的</param>
        public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp)
        {
            Warn(str, line, level, isError, isBackComp, null);
        }

        public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp, string stack)
        {
            if (isError)
            {
                line.IsError = true;
                line.ErrMes = str;
            }
            if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
                return;
            if (isBackComp && !Config.WarnBackCompatibility)
                return;
            if (console != null && !console.RunERBFromMemory)
                warningList.Add(new ParserWarning(str, line.Position, level, stack));
            //				console.PrintWarning(str, line.Position, level);
        }

        public static void ClearWarningList()
        {
            warningList.Clear();
        }

        public static void FlushWarningList()
        {
            for (var i = 0; i < warningList.Count; i++)
            {
                var warning = warningList[i];
                console.PrintWarning(warning.WarningMes, warning.WarningPos, warning.WarningLevel);
                if (warning.StackTrace != null)
                {
                    var stacks = warning.StackTrace.Split('\n');
                    for (var j = 0; j < stacks.Length; j++)
                        console.PrintSystemLine(stacks[j]);
                }
            }
            warningList.Clear();
        }

        private class ParserWarning
        {
            public readonly string StackTrace;
            public readonly int WarningLevel;
            public readonly string WarningMes;
            public readonly ScriptPosition WarningPos;

            public ParserWarning(string mes, ScriptPosition pos, int level, string stackTrace)
            {
                WarningMes = mes;
                WarningPos = pos;
                WarningLevel = level;
                StackTrace = stackTrace;
            }
        }

        #region Rename

        public static Dictionary<string, string> RenameDic { get; private set; }

        //1756 Process.Load.csより移動
        public static void LoadEraExRenameFile(string filepath)
        {
            if (RenameDic != null)
                RenameDic.Clear();
            //とにかく辞書を作る。辞書がnullのときは UseRenameFileがNOの時のみ
            RenameDic = new Dictionary<string, string>();
            var eReader = new EraStreamReader(false);
            if (!File.Exists(filepath) || !eReader.Open(filepath))
                return;
            string line = null;
            ScriptPosition pos = null;
            var reg = new Regex(@"\\,", RegexOptions.Compiled);
            try
            {
                while ((line = eReader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        continue;
                    if (line.StartsWith(";"))
                        continue;
                    var baseTokens = reg.Split(line);
                    if (!baseTokens[baseTokens.Length - 1].Contains(","))
                        continue;
                    var last = baseTokens[baseTokens.Length - 1].Split(',');
                    baseTokens[baseTokens.Length - 1] = last[0];
                    var tokens = new string[2];
                    tokens[0] = string.Join(",", baseTokens);
                    tokens[1] = last[1];
                    pos = new ScriptPosition(eReader.Filename, eReader.LineNo, line);
                    //右がERB中の表記、左が変換先になる。
                    var value = tokens[0].Trim();
                    var key = string.Format("[[{0}]]", tokens[1].Trim());
                    RenameDic[key] = value;
                    pos = null;
                }
            }
            catch (Exception e)
            {
                if (pos != null)
                    throw new CodeEE(e.Message, pos);
                else
                    throw new CodeEE(e.Message);
            }
            finally
            {
                eReader.Close();
            }
        }

        #endregion
    }
}