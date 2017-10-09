using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using MinorShift.Emuera.Content;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;
using MinorShift._Library;

namespace MinorShift.Emuera.GameProc
{
    internal sealed partial class Process
    {
        private readonly EmueraConsole console;

        private readonly string scaningScope = null;
        private ExpressionMediator exm;
        private GameBase gamebase;

        private IdentifierDictionary idDic;

        //色々あって復活させてみる

        /// <summary>
        ///     @~~と$~~を集めたもの。CALL命令などで使う
        ///     実行順序はLogicalLine自身が保持する。
        /// </summary>
        private LabelDictionary labelDic;

        private int methodStack;
        private bool noError;
        private ProcessState originalState; //リセットする時のために

        public LogicalLine scaningLine = null;

        private uint startTime;

        /// <summary>
        ///     変数全部。スクリプト中で必要になる変数は（ユーザーが直接触れないものも含め）この中にいれる
        /// </summary>
        private VariableEvaluator vEvaluator;

        public Process(EmueraConsole view)
        {
            console = view;
        }

        public LogicalLine getCurrentLine => getCurrentState.CurrentLine;
        public LabelDictionary LabelDictionary => labelDic;
        public VariableEvaluator VEvaluator => vEvaluator;
        public bool inInitializeing { get; private set; }

        public bool Initialize()
        {
            LexicalAnalyzer.UseMacro = false;
            getCurrentState = new ProcessState(console);
            originalState = getCurrentState;
            inInitializeing = true;
            try
            {
                ParserMediator.Initialize(console);
                if (ParserMediator.HasWarning)
                {
                    ParserMediator.FlushWarningList();
                    if (MessageBox.Show("コンフィグファイルに異常があります\nEmueraを終了しますか", "コンフィグエラー", MessageBoxButtons.YesNo)
                        == DialogResult.Yes)
                    {
                        console.PrintSystemLine("コンフィグファイルに異常があり、終了が選択されたため処理を終了しました");
                        return false;
                    }
                }
                AppContents.LoadContents();

                if (Config.UseKeyMacro && !Program.AnalysisMode)
                    if (File.Exists(Program.ExeDir + "macro.txt"))
                    {
                        if (Config.DisplayReport)
                            console.PrintSystemLine("macro.txt読み込み中・・・");
                        KeyMacro.LoadMacroFile(Program.ExeDir + "macro.txt");
                    }
                if (Config.UseReplaceFile && !Program.AnalysisMode)
                    if (File.Exists(Program.CsvDir + "_Replace.csv"))
                    {
                        if (Config.DisplayReport)
                            console.PrintSystemLine("_Replace.csv読み込み中・・・");
                        ConfigData.Instance.LoadReplaceFile(Program.CsvDir + "_Replace.csv");
                        if (ParserMediator.HasWarning)
                        {
                            ParserMediator.FlushWarningList();
                            if (MessageBox.Show("_Replace.csvに異常があります\nEmueraを終了しますか", "_Replace.csvエラー",
                                    MessageBoxButtons.YesNo)
                                == DialogResult.Yes)
                            {
                                console.PrintSystemLine("_Replace.csvに異常があり、終了が選択されたため処理を終了しました");
                                return false;
                            }
                        }
                    }
                Config.SetReplace(ConfigData.Instance);
                //ここでBARを設定すれば、いいことに気づいた予感
                console.setStBar(Config.DrawLineString);

                if (Config.UseRenameFile)
                    if (File.Exists(Program.CsvDir + "_Rename.csv"))
                    {
                        if (Config.DisplayReport || Program.AnalysisMode)
                            console.PrintSystemLine("_Rename.csv読み込み中・・・");
                        ParserMediator.LoadEraExRenameFile(Program.CsvDir + "_Rename.csv");
                    }
                    else
                    {
                        console.PrintError("csv\\_Rename.csvが見つかりません");
                    }
                if (!Config.DisplayReport)
                {
                    console.PrintSingleLine(Config.LoadLabel);
                    console.RefreshStrings(true);
                }
                gamebase = new GameBase();
                if (!gamebase.LoadGameBaseCsv(Program.CsvDir + "GAMEBASE.CSV"))
                {
                    console.PrintSystemLine("GAMEBASE.CSVの読み込み中に問題が発生したため処理を終了しました");
                    return false;
                }
                console.SetWindowTitle(gamebase.ScriptWindowTitle);
                GlobalStatic.GameBaseData = gamebase;

                var constant = new ConstantData(gamebase);
                constant.LoadData(Program.CsvDir, console, Config.DisplayReport);
                GlobalStatic.ConstantData = constant;
                TrainName = constant.GetCsvNameList(VariableCode.TRAINNAME);

                vEvaluator = new VariableEvaluator(gamebase, constant);
                GlobalStatic.VEvaluator = vEvaluator;

                idDic = new IdentifierDictionary(vEvaluator.VariableData);
                GlobalStatic.IdentifierDictionary = idDic;

                StrForm.Initialize();
                VariableParser.Initialize();

                exm = new ExpressionMediator(this, vEvaluator, console);
                GlobalStatic.EMediator = exm;

                labelDic = new LabelDictionary();
                GlobalStatic.LabelDictionary = labelDic;
                var hLoader = new HeaderFileLoader(console, idDic, this);

                LexicalAnalyzer.UseMacro = false;
                if (!hLoader.LoadHeaderFiles(Program.ErbDir, Config.DisplayReport))
                {
                    console.PrintSystemLine("ERHの読み込み中にエラーが発生したため処理を終了しました");
                    return false;
                }
                LexicalAnalyzer.UseMacro = idDic.UseMacro();

                var loader = new ErbLoader(console, exm, this);
                if (Program.AnalysisMode)
                    noError = loader.loadErbs(Program.AnalysisFiles, labelDic);
                else
                    noError = loader.LoadErbFiles(Program.ErbDir, Config.DisplayReport, labelDic);
                initSystemProcess();
                inInitializeing = false;
            }
            catch (Exception e)
            {
                handleException(e, null, true);
                console.PrintSystemLine("初期化中に致命的なエラーが発生したため処理を終了しました");
                return false;
            }
            if (labelDic == null)
                return false;
            getCurrentState.Begin(BeginType.TITLE);
            GC.Collect();
            return true;
        }

        public void ReloadErb()
        {
            saveCurrentState(false);
            getCurrentState.SystemState = SystemStateCode.System_Reloaderb;
            var loader = new ErbLoader(console, exm, this);
            loader.LoadErbFiles(Program.ErbDir, false, labelDic);
            console.ReadAnyKey();
        }

        public void ReloadPartialErb(List<string> path)
        {
            saveCurrentState(false);
            getCurrentState.SystemState = SystemStateCode.System_Reloaderb;
            var loader = new ErbLoader(console, exm, this);
            loader.loadErbs(path, labelDic);
            console.ReadAnyKey();
        }

        public void SetCommnds(long count)
        {
            coms = new List<long>((int) count);
            isCTrain = true;
            var selectcom = vEvaluator.SELECTCOM_ARRAY;
            if (count >= selectcom.Length)
                throw new CodeEE("CALLTRAIN命令の引数の値がSELECTCOMの要素数を超えています");
            for (var i = 0; i < (int) count; i++)
                coms.Add(selectcom[i + 1]);
        }

        public bool ClearCommands()
        {
            coms.Clear();
            count = 0;
            isCTrain = false;
            SkipPrint = true;
            return callFunction("CALLTRAINEND", false, false);
        }

        public void InputInteger(long i)
        {
            vEvaluator.RESULT = i;
        }

        public void InputSystemInteger(long i)
        {
            systemResult = i;
        }

        public void InputString(string s)
        {
            vEvaluator.RESULTS = s;
        }

        public void DoScript()
        {
            startTime = WinmmTimer.TickCount;
            getCurrentState.lineCount = 0;
            var systemProcRunning = true;
            try
            {
                while (true)
                {
                    methodStack = 0;
                    systemProcRunning = true;
                    while (getCurrentState.ScriptEnd && console.IsRunning)
                        runSystemProc();
                    if (!console.IsRunning)
                        break;
                    systemProcRunning = false;
                    runScriptProc();
                }
            }
            catch (Exception ec)
            {
                var currentLine = getCurrentState.ErrorLine;
                if (currentLine != null && currentLine is NullLine)
                    currentLine = null;
                if (systemProcRunning)
                    handleExceptionInSystemProc(ec, currentLine, true);
                else
                    handleException(ec, currentLine, true);
            }
        }

        public void BeginTitle()
        {
            vEvaluator.ResetData();
            getCurrentState = originalState;
            getCurrentState.Begin(BeginType.TITLE);
        }

        private void checkInfiniteLoop()
        {
            //うまく動かない。BEEP音が鳴るのを止められないのでこの処理なかったことに（1.51）
            ////フリーズ防止。処理中でも履歴を見たりできる
            //System.Windows.Forms.Application.DoEvents();
            ////System.Threading.Thread.Sleep(0);

            //if (!console.Enabled)
            //{
            //    //DoEvents()の間にウインドウが閉じられたらおしまい。
            //    console.ReadAnyKey();
            //    return;
            //}
            var time = WinmmTimer.TickCount - startTime;
            if (time < Config.InfiniteLoopAlertTime)
                return;
            var currentLine = getCurrentState.CurrentLine;
            if (currentLine == null || currentLine is NullLine)
                return; //現在の行が特殊な状態ならスルー
            if (!console.Enabled)
                return; //クローズしてるとMessageBox.Showができないので。
            var caption = "無限ループの可能性があります";
            var text = string.Format(
                "現在、{0}の{1}行目を実行中です。\n最後の入力から{3}ミリ秒経過し{2}行が実行されました。\n処理を中断し強制終了しますか？",
                currentLine.Position.Filename, currentLine.Position.LineNo, getCurrentState.lineCount, time);
            var result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
                throw new CodeEE("無限ループの疑いにより強制終了が選択されました");
            getCurrentState.lineCount = 0;
            startTime = WinmmTimer.TickCount;
        }

        public SingleTerm GetValue(SuperUserDefinedMethodTerm udmt)
        {
            methodStack++;
            if (methodStack > 100)
                throw new CodeEE("関数の呼び出しスタックが溢れました(無限に再帰呼び出しされていませんか？)");
            SingleTerm ret = null;
            var temp_current = getCurrentState.currentMin;
            getCurrentState.currentMin = getCurrentState.functionCount;
            udmt.Call.updateRetAddress(getCurrentState.CurrentLine);
            try
            {
                getCurrentState.IntoFunction(udmt.Call, udmt.Argument, exm);
                //do whileの中でthrow されたエラーはここではキャッチされない。
                //#functionを全て抜けてDoScriptでキャッチされる。
                runScriptProc();
                ret = getCurrentState.MethodReturnValue;
            }
            finally
            {
                if (udmt.Call.TopLabel.hasPrivDynamicVar)
                    udmt.Call.TopLabel.Out();
                //1756beta2+v3:こいつらはここにないとデバッグコンソールで式中関数が事故った時に大事故になる
                getCurrentState.currentMin = temp_current;
                methodStack--;
            }
            return ret;
        }

        public void clearMethodStack()
        {
            methodStack = 0;
        }

        public int MethodStack()
        {
            return methodStack;
        }

        public ScriptPosition GetRunningPosition()
        {
            var line = getCurrentState.ErrorLine;
            if (line == null)
                return null;
            return line.Position;
        }

        private string GetScaningScope()
        {
            if (scaningScope != null)
                return scaningScope;
            return getCurrentState.Scope;
        }

        internal LogicalLine GetScaningLine()
        {
            if (scaningLine != null)
                return scaningLine;
            var line = getCurrentState.ErrorLine;
            if (line == null)
                return null;
            return line;
        }


        private void handleExceptionInSystemProc(Exception exc, LogicalLine current, bool playSound)
        {
            console.ThrowError(playSound);
            if (exc is CodeEE)
            {
                console.PrintError("関数の終端でエラーが発生しました:" + Program.ExeName);
                console.PrintError(exc.Message);
            }
            else if (exc is ExeEE)
            {
                console.PrintError("関数の終端でEmueraのエラーが発生しました:" + Program.ExeName);
                console.PrintError(exc.Message);
            }
            else
            {
                console.PrintError("関数の終端で予期しないエラーが発生しました:" + Program.ExeName);
                console.PrintError(exc.GetType() + ":" + exc.Message);
                var stack = exc.StackTrace.Split('\n');
                for (var i = 0; i < stack.Length; i++)
                    console.PrintError(stack[i]);
            }
        }

        private void handleException(Exception exc, LogicalLine current, bool playSound)
        {
            console.ThrowError(playSound);
            ScriptPosition position = null;
            var ee = exc as EmueraException;
            if (ee != null && ee.Position != null)
                position = ee.Position;
            else if (current != null && current.Position != null)
                position = current.Position;
            var posString = "";
            if (position != null)
                if (position.LineNo >= 0)
                    posString = position.Filename + "の" + position.LineNo + "行目で";
                else
                    posString = position.Filename + "で";
            if (exc is CodeEE)
            {
                if (position != null)
                {
                    var procline = current as InstructionLine;
                    if (procline != null && procline.FunctionCode == FunctionCode.THROW)
                    {
                        console.PrintErrorButton(posString + "THROWが発生しました", position);
                        if (position.RowLine != null)
                            console.PrintError(position.RowLine);
                        console.PrintError("THROW内容：" + exc.Message);
                    }
                    else
                    {
                        console.PrintErrorButton(posString + "エラーが発生しました:" + Program.ExeName, position);
                        if (position.RowLine != null)
                            console.PrintError(position.RowLine);
                        console.PrintError("エラー内容：" + exc.Message);
                    }
                    console.PrintError("現在の関数：@" + current.ParentLabelLine.LabelName + "（" +
                                       current.ParentLabelLine.Position.Filename + "の" +
                                       current.ParentLabelLine.Position.LineNo + "行目）");
                    console.PrintError("関数呼び出しスタック：");
                    LogicalLine parent = null;
                    var depth = 0;
                    while ((parent = getCurrentState.GetReturnAddressSequensial(depth++)) != null)
                        if (parent.Position != null)
                            console.PrintErrorButton(
                                "↑" + parent.Position.Filename + "の" + parent.Position.LineNo + "行目（関数@" +
                                parent.ParentLabelLine.LabelName + "内）", parent.Position);
                }
                else
                {
                    console.PrintError(posString + "エラーが発生しました:" + Program.ExeName);
                    console.PrintError(exc.Message);
                }
            }
            else if (exc is ExeEE)
            {
                console.PrintError(posString + "Emueraのエラーが発生しました:" + Program.ExeName);
                console.PrintError(exc.Message);
            }
            else
            {
                console.PrintError(posString + "予期しないエラーが発生しました:" + Program.ExeName);
                console.PrintError(exc.GetType() + ":" + exc.Message);
                var stack = exc.StackTrace.Split('\n');
                for (var i = 0; i < stack.Length; i++)
                    console.PrintError(stack[i]);
            }
        }
    }
}