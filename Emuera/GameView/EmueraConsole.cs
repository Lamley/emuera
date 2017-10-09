﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using MinorShift.Emuera.Forms;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.Sub;
using MinorShift._Library;
using Process = MinorShift.Emuera.GameProc.Process;

namespace MinorShift.Emuera.GameView
{
    //入出力待ちの状況。
    //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
    [Obfuscation(Exclude = false)]
    internal enum ConsoleState
    {
        Initializing = 0,
        Quit = 5, //QUIT
        Error = 6, //Exceptionによる強制終了
        Running = 7,
        WaitInput = 20

        //WaitKey = 1,//WAIT
        //WaitSystemInteger = 2,//Systemが要求するInput
        //WaitInteger = 3,//INPUT
        //WaitString = 4,//INPUTS
        //WaitIntegerWithTimer = 8,
        //WaitStringWithTimer = 9,
        //Timeout = 10,
        //Timeouts = 11,
        //WaitKeyWithTimer = 12,
        //WaitKeyWithTimerF = 13,
        //WaitOneInteger = 14,
        //WaitOneString = 15,
        //WaitOneIntegerWithTimer = 16,
        //WaitOneStringWithTimer = 17,
        //WaitAnyKey = 18,
    }

    //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
    [Obfuscation(Exclude = false)]
    internal enum ConsoleRedraw
    {
        None = 0,
        Normal = 1
    }

    internal sealed partial class EmueraConsole : IDisposable
    {
        private const string ErrorButtonsText = "__openFileWithDebug__";
        private readonly MainWindow window;
        public bool byError;

        private Process emuera;

        private bool force_temporary;

        public bool notToTitle;
        private InputRequest prevReq;
        private ConsoleState prevState;
        private ConsoleState state = ConsoleState.Initializing;
        private bool timer_suspended;

        public EmueraConsole(MainWindow parent)
        {
            window = parent;

            //1.713 この段階でsetStBarを使用してはいけない
            //setStBar(StaticConfig.DrawLineString);
            state = ConsoleState.Initializing;
            if (Config.FPS > 0)
                msPerFrame = 1000 / (uint) Config.FPS;
            displayLineList = new List<ConsoleDisplayLine>();
            printBuffer = new PrintStringBuffer(this);

            timer = new Timer();
            timer.Enabled = false;
            timer.Tick += tickTimer;
            timer.Interval = 100;
        }

        public bool Enabled => window.Created;

        /// <summary>
        ///     スクリプトが継続中かどうか
        ///     入力系はメッセージスキップやマクロも含めてIsInProcessを参照すべき
        /// </summary>
        internal bool IsRunning
        {
            get
            {
                if (state == ConsoleState.Initializing)
                    return true;
                return state == ConsoleState.Running || RunERBFromMemory;
            }
        }

        internal bool IsInProcess
        {
            get
            {
                if (state == ConsoleState.Initializing)
                    return true;
                if (inProcess)
                    return true;
                return state == ConsoleState.Running || RunERBFromMemory;
            }
        }

        internal bool IsError => state == ConsoleState.Error;

        internal bool IsWaitingEnterKey
        {
            get
            {
                if (state == ConsoleState.Quit || state == ConsoleState.Error)
                    return true;
                if (state == ConsoleState.WaitInput)
                    return inputReq.InputType == InputType.AnyKey || inputReq.InputType == InputType.EnterKey;
                return false;
            }
        }

        internal bool IsWaitAnyKey => state == ConsoleState.WaitInput && inputReq.InputType == InputType.AnyKey;

        internal bool IsWaintingOnePhrase => state == ConsoleState.WaitInput && inputReq.OneInput;

        internal bool IsRunningTimer => state == ConsoleState.WaitInput && inputReq.Timelimit > 0 && !IsTimeOut;

        internal string SelectedString
        {
            get
            {
                if (selectingButton == null)
                    return null;
                if (state == ConsoleState.Error)
                    return selectingButton.Inputs;
                if (state != ConsoleState.WaitInput)
                    return null;
                if (inputReq.InputType == InputType.IntValue && selectingButton.IsInteger)
                    return selectingButton.Input.ToString();
                if (inputReq.InputType == InputType.StrValue)
                    return selectingButton.Inputs;
                return null;
            }
        }

        public void Dispose()
        {
            if (timer != null)
                timer.Dispose();
            timer = null;
            //stringMeasure.Dispose();
        }

        public void Initialize()
        {
            GlobalStatic.Console = this;
            GlobalStatic.MainWindow = window;
            emuera = new Process(this);
            GlobalStatic.Process = emuera;
            if (Program.DebugMode && Config.DebugShowWindow)
            {
                OpenDebugDialog();
                window.Focus();
            }
            ClearDisplay();
            if (!emuera.Initialize())
            {
                state = ConsoleState.Error;
                OutputLog(null);
                PrintFlush(false);
                RefreshStrings(true);
                return;
            }
            callEmueraProgram("");
            RefreshStrings(true);
        }


        public void Quit()
        {
            state = ConsoleState.Quit;
        }

        public void ThrowTitleError(bool error)
        {
            state = ConsoleState.Error;
            notToTitle = true;
            byError = error;
        }

        public void ThrowError(bool playSound)
        {
            if (playSound)
                SystemSounds.Hand.Play();
            forceUpdateGeneration();
            UseUserStyle = false;
            PrintFlush(false);
            RefreshStrings(false);
            state = ConsoleState.Error;
        }


        public void GotoTitle()
        {
            //if (state == ConsoleState.Error)
            //{
            //    MessageBox.Show("エラー発生時はこの機能は使えません");
            //}
            forceStopTimer();
            ClearDisplay();
            Redraw = ConsoleRedraw.Normal;
            UseUserStyle = false;
            userStyle = new StringStyle(Config.ForeColor, FontStyle.Regular, null);
            emuera.BeginTitle();
            ReadAnyKey(false, false);
            callEmueraProgram("");
            RefreshStrings(true);
        }

        public void ReloadErb()
        {
            if (state == ConsoleState.Error)
            {
                MessageBox.Show("エラー発生時はこの機能は使えません");
                return;
            }
            if (state == ConsoleState.Initializing)
            {
                MessageBox.Show("初期化中はこの機能は使えません");
                return;
            }
            var notRedraw = false;
            if (Redraw == ConsoleRedraw.None)
            {
                notRedraw = true;
                Redraw = ConsoleRedraw.Normal;
            }
            if (timer.Enabled)
            {
                timer.Enabled = false;
                timer_suspended = true;
            }
            prevState = state;
            prevReq = inputReq;
            state = ConsoleState.Initializing;
            PrintSingleLine("ERB再読み込み中……", true);
            force_temporary = true;
            emuera.ReloadErb();
            force_temporary = false;
            PrintSingleLine("再読み込み完了", true);
            RefreshStrings(true);
            //強制的にボタン世代が切り替わるのを防ぐ
            updatedGeneration = true;
            if (notRedraw)
                Redraw = ConsoleRedraw.None;
        }

        public void ReloadErbFinished()
        {
            state = prevState;
            inputReq = prevReq;
            PrintSingleLine(" ");
            if (timer_suspended)
            {
                timer_suspended = false;
                timer.Enabled = true;
            }
        }

        public void ReloadPartialErb(List<string> path)
        {
            if (state == ConsoleState.Error)
            {
                MessageBox.Show("エラー発生時はこの機能は使えません");
                return;
            }
            if (state == ConsoleState.Initializing)
            {
                MessageBox.Show("初期化中はこの機能は使えません");
                return;
            }
            var notRedraw = false;
            if (Redraw == ConsoleRedraw.None)
            {
                notRedraw = true;
                Redraw = ConsoleRedraw.Normal;
            }
            if (timer.Enabled)
            {
                timer.Enabled = false;
                timer_suspended = true;
            }
            prevState = state;
            prevReq = inputReq;
            state = ConsoleState.Initializing;
            PrintSingleLine("ERB再読み込み中……", true);
            force_temporary = true;
            emuera.ReloadPartialErb(path);
            force_temporary = false;
            PrintSingleLine("再読み込み完了", true);
            RefreshStrings(true);
            //強制的にボタン世代が切り替わるのを防ぐ
            updatedGeneration = true;
            if (notRedraw)
                Redraw = ConsoleRedraw.None;
        }

        public void ReloadFolder(string erbPath)
        {
            if (state == ConsoleState.Error)
            {
                MessageBox.Show("エラー発生時はこの機能は使えません");
                return;
            }
            if (state == ConsoleState.Initializing)
            {
                MessageBox.Show("初期化中はこの機能は使えません");
                return;
            }
            if (timer.Enabled)
            {
                timer.Enabled = false;
                timer_suspended = true;
            }
            var paths = new List<string>();
            var op = SearchOption.AllDirectories;
            if (!Config.SearchSubdirectory)
                op = SearchOption.TopDirectoryOnly;
            var fnames = Directory.GetFiles(erbPath, "*.ERB", op);
            for (var i = 0; i < fnames.Length; i++)
                if (Path.GetExtension(fnames[i]).ToUpper() == ".ERB")
                    paths.Add(fnames[i]);
            var notRedraw = false;
            if (Redraw == ConsoleRedraw.None)
            {
                notRedraw = true;
                Redraw = ConsoleRedraw.Normal;
            }
            prevState = state;
            prevReq = inputReq;
            state = ConsoleState.Initializing;
            PrintSingleLine("ERB再読み込み中……", true);
            force_temporary = true;
            emuera.ReloadPartialErb(paths);
            force_temporary = false;
            PrintSingleLine("再読み込み完了", true);
            RefreshStrings(true);
            //強制的にボタン世代が切り替わるのを防ぐ
            updatedGeneration = true;
            if (notRedraw)
                Redraw = ConsoleRedraw.None;
        }
        //public ScriptPosition ErrPos = null;

        #region button関連

        private bool lastButtonIsInput = true;
        public bool updatedGeneration;
        private int lastButtonGeneration; //最後に追加された選択肢の世代。これと世代が一致しない選択肢は選択できない。

        //public int LastButtonGeneration { get { return lastButtonGeneration; } }
        public int NewButtonGeneration { get; private set; }

        public void UpdateGeneration()
        {
            lastButtonGeneration = NewButtonGeneration;
            updatedGeneration = true;
        }

        public void forceUpdateGeneration()
        {
            NewButtonGeneration++;
            lastButtonGeneration = NewButtonGeneration;
            updatedGeneration = true;
        }

        private LogicalLine lastInputLine;

        private void newGeneration()
        {
            //値の入力を求められない時は更新は必要ないはず
            if (state != ConsoleState.WaitInput || !inputReq.NeedValue)
                return;
            if (!updatedGeneration && emuera.getCurrentLine != lastInputLine)
                lastButtonGeneration = NewButtonGeneration;
            else
                updatedGeneration = false;
            lastInputLine = emuera.getCurrentLine;
            //古い選択肢を選択できないように。INPUTで使った選択肢をINPUTSには流用できないように。
            if (inputReq.InputType == InputType.IntValue)
            {
                if (lastButtonGeneration == NewButtonGeneration)
                    unchecked
                    {
                        NewButtonGeneration++;
                    }
                else if (!lastButtonIsInput)
                    lastButtonGeneration = NewButtonGeneration;
                lastButtonIsInput = true;
            }
            if (inputReq.InputType == InputType.StrValue)
            {
                if (lastButtonGeneration == NewButtonGeneration)
                    unchecked
                    {
                        NewButtonGeneration++;
                    }
                else if (lastButtonIsInput)
                    lastButtonGeneration = NewButtonGeneration;
                lastButtonIsInput = false;
            }
        }

        /// <summary>
        ///     選択中のボタン。INPUTやINPUTSに対応したものでなければならない
        /// </summary>
        private ConsoleButtonString selectingButton;

        private ConsoleButtonString lastSelectingButton;
        public ConsoleButtonString SelectingButton => selectingButton;

        public bool ButtonIsSelected(ConsoleButtonString button)
        {
            return selectingButton == button;
        }

        /// <summary>
        ///     ToolTip表示したフラグ
        /// </summary>
        private bool tooltipUsed;

        /// <summary>
        ///     マウスの直下にあるテキスト。ボタンであってもよい。
        ///     ToolTip表示用。世代無視、履歴中も表示
        /// </summary>
        private ConsoleButtonString pointingString;

        private ConsoleButtonString lastPointingString;

        #endregion

        #region Input & Timer系

        //bool hasDefValue = false;
        //Int64 defNum;
        //string defStr;

        private InputRequest inputReq;

        public void WaitInput(InputRequest req)
        {
            state = ConsoleState.WaitInput;
            inputReq = req;
            //TODO:Timelimitが0以下だったら？
            if (req.Timelimit > 0)
            {
                if (req.OneInput)
                    window.update_lastinput();
                setTimer();
            }
            //updateMousePosition();
            //Point point = window.MainPicBox.PointToClient(Control.MousePosition);
            //if (window.MainPicBox.ClientRectangle.Contains(point))
            //{
            //	PrintFlush(false);
            //	MoveMouse(point);
            //}
        }

        public void ReadAnyKey(bool anykey = false, bool stopMesskip = false)
        {
            var req = new InputRequest();
            if (!anykey)
                req.InputType = InputType.EnterKey;
            else
                req.InputType = InputType.AnyKey;
            req.StopMesskip = stopMesskip;
            inputReq = req;
            state = ConsoleState.WaitInput;
            emuera.NeedWaitToEventComEnd = false;
        }


        private Timer timer;
        private long timerID = -1;
        private int countTime;
        private readonly bool wait_timeout = false;
        public bool IsTimeOut { get; private set; }

        private void setTimer()
        {
            countTime = 0;
            IsTimeOut = false;
            timerID = inputReq.ID;
            timer.Enabled = true;

            if (inputReq.DisplayTime)
            {
                var start = inputReq.Timelimit / 100;
                var timeString1 = "残り ";
                var timeString2 = (start / 10.0).ToString();
                PrintSingleLine(timeString1 + timeString2);
            }
        }

        //汎用
        private void tickTimer(object sender, EventArgs e)
        {
            if (!timer.Enabled)
                return;
            if (state != ConsoleState.WaitInput || inputReq.Timelimit <= 0 || timerID != inputReq.ID)
            {
#if DEBUG
                throw new ExeEE("");
#else
				stopTimer();
				return;
#endif
            }
            countTime += 100;
            if (countTime >= inputReq.Timelimit)
            {
                endTimer();
                return;
            }
            if (inputReq.DisplayTime)
            {
                var time = (inputReq.Timelimit - countTime) / 100;
                var timeString1 = "残り ";
                var timeString2 = (time / 10.0).ToString();
                changeLastLine(timeString1 + timeString2);
            }
        }

        private void stopTimer()
        {
            //if (state == ConsoleState.WaitKeyWithTimerF && countTime < timeLimit)
            //{
            //	wait_timeout = true;
            //	while (countTime < timeLimit)
            //	{
            //		Application.DoEvents();
            //	}
            //	wait_timeout = false;
            //}
            timer.Enabled = false;
            //timer.Dispose();
        }

        /// <summary>
        ///     tickTimerからのみ呼ぶ
        /// </summary>
        private void endTimer()
        {
            if (wait_timeout)
                return;
            stopTimer();
            IsTimeOut = true;
            if (inputReq.DisplayTime)
                changeLastLine(inputReq.TimeUpMes);
            else if (inputReq.TimeUpMes != null)
                PrintSingleLine(inputReq.TimeUpMes);
            callEmueraProgram(""); //ディフォルト入力の処理はcallEmueraProgram側で
            if (state == ConsoleState.WaitInput && inputReq.NeedValue)
            {
                var point = window.MainPicBox.PointToClient(Control.MousePosition);
                if (window.MainPicBox.ClientRectangle.Contains(point))
                    MoveMouse(point);
            }
            RefreshStrings(true);
        }

        public void forceStopTimer()
        {
            if (timer.Enabled)
                timer.Enabled = false;
        }

        #endregion

        #region Call系

        /// <summary>
        ///     スクリプト実行。RefreshStringsはしないので呼び出し側がすること
        /// </summary>
        /// <param name="str"></param>
        private void callEmueraProgram(string str)
        {
            if (!doInputToEmueraProgram(str))
                return;
            if (state == ConsoleState.Error)
                return;
            state = ConsoleState.Running;
            emuera.DoScript();
            if (state == ConsoleState.Running)
            {
//RunningならProcessは処理を継続するべき
                state = ConsoleState.Error;
                PrintError("emueraのエラー：プログラムの状態を特定できません");
            }
            if (state == ConsoleState.Error && !noOutputLog)
                OutputLog(Program.ExeDir + "emuera.log");
            PrintFlush(false);
            //1819 Refreshは呼び出し側で行う
            //RefreshStrings(false);
            newGeneration();
        }

        private bool doInputToEmueraProgram(string str)
        {
            if (state == ConsoleState.WaitInput)
            {
                long inputValue;

                switch (inputReq.InputType)
                {
                    case InputType.IntValue:
                        if (string.IsNullOrEmpty(str) && inputReq.HasDefValue && !IsRunningTimer)
                        {
                            inputValue = inputReq.DefIntValue;
                            str = inputValue.ToString();
                        }
                        else if (!long.TryParse(str, out inputValue))
                        {
                            return false;
                        }
                        if (inputReq.IsSystemInput)
                            emuera.InputSystemInteger(inputValue);
                        else
                            emuera.InputInteger(inputValue);
                        break;
                    case InputType.StrValue:
                        if (string.IsNullOrEmpty(str) && inputReq.HasDefValue && !IsRunningTimer)
                            str = inputReq.DefStrValue;
                        //空入力と時間切れ
                        if (str == null)
                            str = "";
                        emuera.InputString(str);
                        break;
                }
                stopTimer();
            }
            Print(str);
            PrintFlush(false);
            return true;
        }

        #endregion

        #region 入力系

        private readonly string[] spliter = {"\\n", "\r\n", "\n", "\r"}; //本物の改行コードが来ることは無いはずだけど一応

        public bool MesSkip;
        private bool inProcess;
        public volatile bool KillMacro;

        public void PressEnterKey(bool keySkip, string str, bool changedByMouse)
        {
            MesSkip = keySkip;
            if (state == ConsoleState.Running || state == ConsoleState.Initializing)
                return;
            if (state == ConsoleState.Quit)
            {
                window.Close();
                return;
            }
            if (state == ConsoleState.Error)
            {
                if (str == ErrorButtonsText && selectingButton != null && selectingButton.ErrPos != null)
                {
                    openErrorFile(selectingButton.ErrPos);
                    return;
                }
                window.Close();
                return;
            }
#if DEBUG
            if (state != ConsoleState.WaitInput || inputReq == null)
                throw new ExeEE("");
#endif
            KillMacro = false;
            try
            {
                if (str.StartsWith("@") && !inputReq.OneInput)
                {
                    doSystemCommand(str);
                    return;
                }
                if (inputReq.InputType == InputType.Void)
                    return;
                if (timer.Enabled &&
                    (inputReq.InputType == InputType.AnyKey || inputReq.InputType == InputType.EnterKey))
                    stopTimer();
                //if((inputReq.InputType == InputType.IntValue || inputReq.InputType == InputType.StrValue)
                if (str.Contains("("))
                    str = parseInput(new StringStream(str), false);
                var text = str.Split(spliter, StringSplitOptions.None);

                inProcess = true;
                for (var i = 0; i < text.Length; i++)
                {
                    var inputs = text[i];
                    if (inputs.IndexOf("\\e") >= 0)
                    {
                        inputs = inputs.Replace("\\e", ""); //\eの除去
                        MesSkip = true;
                    }

                    if (inputReq.OneInput && (!Config.AllowLongInputByMouse || !changedByMouse) && inputs.Length > 1)
                        inputs = inputs.Remove(1);
                    //1819 TODO:入力無効系（強制待ちTWAIT）でスキップとマクロを止めるかそのままか
                    //現在はそのまま。強制待ち中はスキップの開始もできないのにスキップ中なら飛ばせる。
                    if (inputReq.InputType == InputType.Void)
                    {
                        i--;
                        inputs = "";
                    }
                    callEmueraProgram(inputs);
                    RefreshStrings(false);
                    while (MesSkip && state == ConsoleState.WaitInput)
                    {
                        //TODO:入力無効を通していいか？スキップ停止をマクロでは飛ばせていいのか？
                        if (inputReq.NeedValue)
                            break;
                        if (inputReq.StopMesskip)
                            break;
                        callEmueraProgram("");
                        RefreshStrings(false);
                        //DoEventを呼ばないと描画処理すらまったく行われない
                        Application.DoEvents();
                        //EscがマクロストップかつEscがスキップ開始だからEscでスキップを止められても即開始しちゃったりするからあんまり意味ないよね
                        //if (KillMacro)
                        //	goto endMacro;
                    }
                    MesSkip = false;
                    if (state != ConsoleState.WaitInput)
                        break;
                    //マクロループ時は待ち処理が起こらないのでここでシステムキューを捌く
                    Application.DoEvents();
#if DEBUG
                    if (state != ConsoleState.WaitInput || inputReq == null)
                        throw new ExeEE("");
#endif
                    if (KillMacro)
                        goto endMacro;
                }
            }
            finally
            {
                inProcess = false;
            }
            endMacro:
            if (state == ConsoleState.WaitInput && inputReq.NeedValue)
            {
                var point = window.MainPicBox.PointToClient(Control.MousePosition);
                if (window.MainPicBox.ClientRectangle.Contains(point))
                    MoveMouse(point);
            }
            RefreshStrings(true);
        }

        private void openErrorFile(ScriptPosition pos)
        {
            var pInfo = new ProcessStartInfo();
            pInfo.FileName = Config.TextEditor;
            var fname = pos.Filename.ToUpper();
            if (fname.EndsWith(".CSV"))
            {
                if (fname.Contains(Program.CsvDir.ToUpper()))
                    fname = fname.Replace(Program.CsvDir.ToUpper(), "");
                fname = Program.CsvDir + fname;
            }
            else
            {
                //解析モードの場合は見ているファイルがERB\の下にあるとは限らないかつフルパスを持っているのでこの補正はしなくてよい
                if (!Program.AnalysisMode)
                {
                    if (fname.Contains(Program.ErbDir.ToUpper()))
                        fname = fname.Replace(Program.ErbDir.ToUpper(), "");
                    fname = Program.ErbDir + fname;
                }
            }
            switch (Config.EditorType)
            {
                case TextEditorType.SAKURA:
                    pInfo.Arguments = "-Y=" + pos.LineNo + " \"" + fname + "\"";
                    break;
                case TextEditorType.TERAPAD:
                    pInfo.Arguments = "/jl=" + pos.LineNo + " \"" + fname + "\"";
                    break;
                case TextEditorType.EMEDITOR:
                    pInfo.Arguments = "/l " + pos.LineNo + " \"" + fname + "\"";
                    break;
                case TextEditorType.USER_SETTING:
                    if (Config.EditorArg != "" && Config.EditorArg != null)
                        pInfo.Arguments = Config.EditorArg + pos.LineNo + " \"" + fname + "\"";
                    else
                        pInfo.Arguments = fname;
                    break;
            }
            try
            {
                System.Diagnostics.Process.Start(pInfo);
            }
            catch (Win32Exception)
            {
                SystemSounds.Hand.Play();
                PrintError("エディタを開くことができませんでした");
                forceUpdateGeneration();
            }
        }

        private string parseInput(StringStream st, bool isNest)
        {
            var sb = new StringBuilder(20);
            var num = new StringBuilder(20);
            var hasRet = false;
            var res = 0;
            while (!st.EOS && (!isNest || st.Current != ')'))
            {
                if (st.Current == '(')
                {
                    st.ShiftNext();
                    var tstr = parseInput(st, true);

                    if (!st.EOS)
                    {
                        st.ShiftNext();
                        if (st.Current == '*')
                        {
                            st.ShiftNext();
                            while (char.IsNumber(st.Current))
                            {
                                num.Append(st.Current);
                                st.ShiftNext();
                            }
                            if (num.ToString() != "" && num.ToString() != null)
                            {
                                int.TryParse(num.ToString(), out res);
                                for (var i = 0; i < res; i++)
                                    sb.Append(tstr);
                                num.Remove(0, num.Length);
                            }
                        }
                        else
                        {
                            sb.Append(tstr);
                        }
                        continue;
                    }
                    sb.Append(tstr);
                    break;
                }
                if (st.Current == '\\')
                {
                    st.ShiftNext();
                    switch (st.Current)
                    {
                        case 'n':
                            if (!hasRet)
                                sb.Append('\n');
                            else
                                hasRet = false;
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'e':
                            sb.Append("\\e\n");
                            hasRet = true;
                            break;
                        case '\n':
                            break;
                        default:
                            sb.Append(st.Current);
                            break;
                    }
                }
                else
                {
                    sb.Append(st.Current);
                }
                st.ShiftNext();
            }
            return sb.ToString();
        }


        /// <summary>
        ///     通常コンソールからのDebugコマンド、及びデバッグウインドウの変数ウォッチなど、
        ///     *.ERBファイルが存在しないスクリプトを実行中
        ///     1750 IsDebugから改名
        /// </summary>
        public bool RunERBFromMemory { get; set; }

        private void doSystemCommand(string command)
        {
            if (timer.Enabled)
            {
                PrintError("タイマー系命令の待ち時間中はコマンドを入力できません");
                PrintError(""); //タイマー表示処理に消されちゃうかもしれないので
                RefreshStrings(true);
                return;
            }
            if (IsInProcess)
            {
                PrintError("スクリプト実行中はコマンドを入力できません");
                RefreshStrings(true);
                return;
            }
            var sc = Config.SCVariable;
            Print(command);
            PrintFlush(false);
            RefreshStrings(true);
            var com = command.Substring(1);
            if (com.Length == 0)
                return;
            if (com.Equals("REBOOT", sc))
            {
                window.Reboot();
                return;
            }
            if (com.Equals("OUTPUT", sc) || com.Equals("OUTPUTLOG", sc))
            {
                OutputLog(Program.ExeDir + "emuera.log");
                return;
            }
            if (com.Equals("QUIT", sc) || com.Equals("EXIT", sc))
            {
                window.Close();
                return;
            }
            if (com.Equals("CONFIG", sc))
            {
                window.ShowConfigDialog();
                return;
            }
            if (com.Equals("DEBUG", sc))
            {
                if (!Program.DebugMode)
                {
                    PrintError("デバッグウインドウは-Debug引数付きで起動したときのみ使えます");
                    RefreshStrings(true);
                    return;
                }
                OpenDebugDialog();
            }
            else
            {
                if (!Config.UseDebugCommand)
                {
                    PrintError("デバッグコマンドを使用できない設定になっています");
                    RefreshStrings(true);
                    return;
                }
                //処理をDebugMode系へ移動
                DebugCommand(com, Config.ChangeMasterNameIfDebug, false);
                PrintFlush(false);
            }
            RefreshStrings(true);
        }

        #endregion

        #region 描画系

        private uint lastUpdate;
        private readonly uint msPerFrame = 1000 / 60; //60FPS
        public ConsoleRedraw Redraw { get; private set; } = ConsoleRedraw.Normal;

        public void SetRedraw(long i)
        {
            if ((i & 1) == 0)
                Redraw = ConsoleRedraw.None;
            else
                Redraw = ConsoleRedraw.Normal;
            if ((i & 2) != 0)
                RefreshStrings(true);
        }

        private string debugTitle;

        public void SetWindowTitle(string str)
        {
            if (Program.DebugMode)
            {
                debugTitle = str;
                window.Text = str + " (Debug Mode)";
            }
            else
            {
                window.Text = str;
            }
        }

        public void SetEmueraVersionInfo(string str)
        {
            window.TextBox.Text = str;
        }

        public string GetWindowTitle()
        {
            if (Program.DebugMode && debugTitle != null)
                return debugTitle;
            return window.Text;
        }


        /// <summary>
        ///     1818以前のRefreshStringsからselectingButton部分を抽出
        ///     ここでOnPaintを発行
        /// </summary>
        public void RefreshStrings(bool force_Paint)
        {
            var isBackLog = window.ScrollBar.Value != window.ScrollBar.Maximum;
            //ログ表示はREDRAWの設定に関係なく行うようにする
            if (Redraw == ConsoleRedraw.None && !force_Paint && !isBackLog)
                return;
            //選択中ボタンの適性チェック
            if (selectingButton != null)
                if (state != ConsoleState.Error && state != ConsoleState.WaitInput)
                    selectingButton = null;
                else if (state == ConsoleState.WaitInput && !inputReq.NeedValue)
                    selectingButton = null;
                //選択肢が最新でないなら無効
                else if (selectingButton.Generation != lastButtonGeneration)
                    selectingButton = null;
            if (!force_Paint)
            {
//forceならば確実に再描画。
                //履歴表示中でなく、最終行を表示済みであり、選択中ボタンが変更されていないなら更新不要
                if (!isBackLog && lastDrawnLineNo == lineNo && lastSelectingButton == selectingButton)
                    return;
                //Environment.TickCountは分解能が悪すぎるのでwinmmのタイマーを呼んで来る
                var sec = WinmmTimer.TickCount - lastUpdate;
                //まだ書き換えるタイミングでないなら次の更新を待ってみる
                //ただし、入力待ちなど、しばらく更新のタイミングがない場合には強制的に書き換えてみる
                if (sec < msPerFrame && (state == ConsoleState.Running || state == ConsoleState.Initializing))
                    return;
            }
            if (forceTextBoxColor)
            {
                var sec = WinmmTimer.TickCount - lastBgColorChange;
                //色変化が速くなりすぎないように一定時間以内の再呼び出しは強制待ちにする
                while (sec < 200)
                {
                    Application.DoEvents();
                    sec = WinmmTimer.TickCount - lastBgColorChange;
                }
                window.TextBox.BackColor = bgColor;
                lastBgColorChange = WinmmTimer.TickCount;
            }
            verticalScrollBarUpdate();
            window.Refresh(); //OnPaint発行
        }

        /// <summary>
        ///     1818以前のRefreshStringsの後半とm_RefreshStringsを融合
        ///     全面Clear法のみにしたのでさっぱりした。ダブルバッファリングはOnPaintが勝手にやるはず
        /// </summary>
        /// <param name="graph"></param>
        public void OnPaint(Graphics graph)
        {
            //描画中にEmueraが閉じられると廃棄されたPictureBoxにアクセスしてしまったりするので
            //OnPaintからgraphをもらった直後だから大丈夫だとは思うけど一応
            if (!Enabled)
                return;

            //描画命令を発行したRefresh時にすべきか、OnPaintの開始にすべきか、OnPaintの終了にするか
            lastUpdate = WinmmTimer.TickCount;

            var isBackLog = window.ScrollBar.Value != window.ScrollBar.Maximum;
            var pointY = window.MainPicBox.Height - Config.LineHeight;


            var bottomLineNo = window.ScrollBar.Value - 1;
            if (displayLineList.Count - 1 < bottomLineNo)
                bottomLineNo = displayLineList.Count - 1; //1820 この処理不要な気がするけどエラー報告があったので入れとく
            var topLineNo = bottomLineNo - (pointY / Config.LineHeight + 1);
            if (topLineNo < 0)
                topLineNo = 0;
            pointY -= (bottomLineNo - topLineNo) * Config.LineHeight;
            if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
            {
                GDI.GDIStart(graph, bgColor);
                GDI.FillRect(new Rectangle(0, 0, window.MainPicBox.Width, window.MainPicBox.Height));
                //for (int i = bottomLineNo; i >= topLineNo; i--)
                //{
                //	displayLineList[i].GDIDrawTo(pointY, isBackLog);
                //	pointY -= Config.LineHeight;
                //}
                //1820a12 上から下へ描画する方向へ変更
                for (var i = topLineNo; i <= bottomLineNo; i++)
                {
                    displayLineList[i].GDIDrawTo(pointY, isBackLog);
                    pointY += Config.LineHeight;
                }
                GDI.GDIEnd(graph);
            }
            else
            {
                graph.Clear(bgColor);
                //for (int i = bottomLineNo; i >= topLineNo; i--)
                //{
                //	displayLineList[i].DrawTo(graph, pointY, isBackLog, true, Config.TextDrawingMode);
                //	pointY -= Config.LineHeight;
                //}
                //1820a12 上から下へ描画する方向へ変更
                for (var i = topLineNo; i <= bottomLineNo; i++)
                {
                    displayLineList[i].DrawTo(graph, pointY, isBackLog, true, Config.TextDrawingMode);
                    pointY += Config.LineHeight;
                }
            }

            //ToolTip描画

            if (lastPointingString != pointingString)
            {
                if (tooltipUsed)
                    window.ToolTip.RemoveAll();
                if (pointingString != null && !string.IsNullOrEmpty(pointingString.Title))
                {
                    window.ToolTip.SetToolTip(window.MainPicBox, pointingString.Title);
                    tooltipUsed = true;
                }
                lastPointingString = pointingString;
            }
            if (isBackLog)
                lastDrawnLineNo = -1;
            else
                lastDrawnLineNo = lineNo;
            lastSelectingButton = selectingButton;
            /*デバッグ用。描画が超重い環境を想定
            System.Threading.Thread.Sleep(50);
            */
            forceTextBoxColor = false;
        }

        public void SetToolTipColor(Color foreColor, Color backColor)
        {
            window.ToolTip.ForeColor = foreColor;
            window.ToolTip.BackColor = backColor;
        }

        public void SetToolTipDelay(int delay)
        {
            window.ToolTip.InitialDelay = delay;
        }


        //private Graphics getGraphics()
        //{
        //	//消したいが怖いので残し
        //	if (!window.Created)
        //		throw new ExeEE("存在しないウィンドウにアクセスした");
        //	//if (Config.UseImageBuffer)
        //	//	return Graphics.FromImage(window.MainPicBox.Image);
        //	//else
        //		return window.MainPicBox.CreateGraphics();
        //}

        #endregion

        #region DebugMode系

        public DebugDialog DebugDialog { get; private set; }

        private readonly StringBuilder dConsoleLog = new StringBuilder("");
        public string DebugConsoleLog => dConsoleLog.ToString();
        private readonly List<string> dTraceLogList = new List<string>();
        private bool dTraceLogChanged = true;

        public string GetDebugTraceLog(bool force)
        {
            //if (!dTraceLogChanged && !force)
            //	return null;
            var builder = new StringBuilder("");
            var line = emuera.GetScaningLine();
            builder.AppendLine("*実行中の行");
            if (line == null || line.Position == null)
            {
                builder.AppendLine("ファイル名:なし");
                builder.AppendLine("行番号:なし 関数名:なし");
                builder.AppendLine("");
            }
            else
            {
                builder.AppendLine("ファイル名:" + line.Position.Filename);
                builder.AppendLine("行番号:" + line.Position.LineNo + " 関数名:" + line.ParentLabelLine.LabelName);
                builder.AppendLine("");
            }
            builder.AppendLine("*スタックトレース");
            for (var i = dTraceLogList.Count - 1; i >= 0; i--)
                builder.AppendLine(dTraceLogList[i]);
            return builder.ToString();
        }

        public void OpenDebugDialog()
        {
            if (!Program.DebugMode)
                return;
            if (DebugDialog != null)
                if (DebugDialog.Created)
                {
                    DebugDialog.Focus();
                    return;
                }
                else
                {
                    DebugDialog.Dispose();
                    DebugDialog = null;
                }
            DebugDialog = new DebugDialog();
            DebugDialog.SetParent(this, emuera);
            DebugDialog.Show();
        }

        public void DebugPrint(string str)
        {
            if (!Program.DebugMode)
                return;
            dConsoleLog.Append(str);
        }

        public void DebugClear()
        {
            dConsoleLog.Remove(0, dConsoleLog.Length);
        }

        public void DebugNewLine()
        {
            if (!Program.DebugMode)
                return;
            dConsoleLog.Append(Environment.NewLine);
        }

        public void DebugAddTraceLog(string str)
        {
            //Emueraがデバッグモードで起動されていないなら無視
            //ERBファイル以外のもの(デバッグコマンド、変数ウォッチ)を実行中なら無視
            if (!Program.DebugMode || RunERBFromMemory)
                return;
            dTraceLogChanged = true;
            dTraceLogList.Add(str);
        }

        public void DebugRemoveTraceLog()
        {
            if (!Program.DebugMode || RunERBFromMemory)
                return;
            dTraceLogChanged = true;
            if (dTraceLogList.Count > 0)
                dTraceLogList.RemoveAt(dTraceLogList.Count - 1);
        }

        public void DebugClearTraceLog()
        {
            if (!Program.DebugMode || RunERBFromMemory)
                return;
            dTraceLogChanged = true;
            dTraceLogList.Clear();
        }

        public void DebugCommand(string com, bool munchkin, bool outputDebugConsole)
        {
            var temp_state = state;
            RunERBFromMemory = true;
            //スクリプト等が失敗した場合に備えて念のための保存
            GlobalStatic.Process.saveCurrentState(false);
            try
            {
                LogicalLine line = null;
                if (!com.StartsWith("@") && !com.StartsWith("\"") && !com.StartsWith("\\"))
                    line = LogicalLineParser.ParseLine(com, null);
                if (line == null || line is InvalidLine)
                {
                    var wc = LexicalAnalyzer.Analyse(new StringStream(com), LexEndWith.EoL, LexAnalyzeFlag.None);
                    var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
                    if (term == null)
                        throw new CodeEE("解釈不能なコードです");
                    if (term.GetOperandType() == typeof(long))
                    {
                        if (outputDebugConsole)
                            com = "DEBUGPRINTFORML {" + com + "}";
                        else
                            com = "PRINTVL " + com;
                    }
                    else
                    {
                        if (outputDebugConsole)
                            com = "DEBUGPRINTFORML %" + com + "%";
                        else
                            com = "PRINTFORMSL " + com;
                    }
                    line = LogicalLineParser.ParseLine(com, null);
                }
                if (line == null)
                    throw new CodeEE("解釈不能なコードです");
                if (line is InvalidLine)
                    throw new CodeEE(line.ErrMes);
                if (!(line is InstructionLine))
                    throw new CodeEE("デバッグコマンドで使用できるのは代入文か命令文だけです");
                var func = (InstructionLine) line;
                if (func.Function.IsFlowContorol())
                    throw new CodeEE("フロー制御命令は使用できません");
                //__METHOD_SAFE__をみるならいらないかも
                if (func.Function.IsWaitInput())
                    throw new CodeEE(func.Function.Name + "命令は使用できません");
                //1750 __METHOD_SAFE__とほぼ条件同じだよねってことで
                if (!func.Function.IsMethodSafe())
                    throw new CodeEE(func.Function.Name + "命令は使用できません");
                //1756 SIFの次に来てはいけないものはここでも不可。
                if (func.Function.IsPartial())
                    throw new CodeEE(func.Function.Name + "命令は使用できません");
                switch (func.FunctionCode)
                {
//取りこぼし
                    //逆にOUTPUTLOG、QUITはDebugCommandの前に捕まえる
                    case FunctionCode.PUTFORM:
                    case FunctionCode.UPCHECK:
                    case FunctionCode.CUPCHECK:
                    case FunctionCode.SAVEDATA:
                        throw new CodeEE(func.Function.Name + "命令は使用できません");
                }
                ArgumentParser.SetArgumentTo(func);
                if (func.IsError)
                    throw new CodeEE(func.ErrMes);
                emuera.DoDebugNormalFunction(func, munchkin);
                if (func.FunctionCode == FunctionCode.SET)
                    if (!outputDebugConsole)
                        PrintSingleLine(com);
            }
            catch (Exception e)
            {
                if (outputDebugConsole)
                {
                    DebugPrint(e.Message);
                    DebugNewLine();
                }
                else
                {
                    PrintError(e.Message);
                }
                emuera.clearMethodStack();
            }
            finally
            {
                //確実に元の状態に戻す
                GlobalStatic.Process.loadPrevState();
                RunERBFromMemory = false;
                state = temp_state;
            }
        }

        #endregion

        #region Window.Form系

        /// <summary>
        ///     マウス位置をボタンの選択状態に反映させる
        /// </summary>
        /// <param name="point"></param>
        /// <returns>この後でRefreshStringsが必要かどうか</returns>
        public bool MoveMouse(Point point)
        {
            ConsoleButtonString select = null;
            ConsoleButtonString pointing = null;
            var canSelect = false;
            //数値か文字列の入力待ち状態でなければ選択中にはならない
            if (state == ConsoleState.Error)
                canSelect = true;
            else if (state == ConsoleState.WaitInput && inputReq.NeedValue)
                canSelect = true;
            //スクリプト実行中は無視//入力・マクロ処理中は無視
            if (IsInProcess)
                goto end;
            //履歴表示中は無視
            //if (window.ScrollBar.Value != window.ScrollBar.Maximum)
            //	goto end;
            var pointX = point.X;
            var pointY = point.Y;
            ConsoleDisplayLine curLine = null;

            var bottomLineNo = window.ScrollBar.Value - 1;
            if (displayLineList.Count - 1 < bottomLineNo)
                bottomLineNo = displayLineList.Count - 1; //1820 この処理不要な気がするけどエラー報告があったので入れとく
            var topLineNo = bottomLineNo - window.MainPicBox.Height / Config.LineHeight;
            if (topLineNo < 0)
                topLineNo = 0;
            var relPointY = pointY - window.MainPicBox.Height;
            //下から上へ探索し発見次第打ち切り
            for (var i = bottomLineNo; i >= topLineNo; i--)
            {
                relPointY += Config.LineHeight;
                curLine = displayLineList[i];

                for (var b = 0; b < curLine.Buttons.Length; b++)
                {
                    var button = curLine.Buttons[curLine.Buttons.Length - b - 1];
                    if (button == null || button.StrArray == null)
                        continue;
                    if (button.PointX <= pointX && button.PointX + button.Width >= pointX)
                        foreach (var part in button.StrArray)
                        {
                            if (part == null)
                                continue;
                            if (part.PointX <= pointX && part.PointX + part.Width >= pointX
                                && relPointY >= part.Top && relPointY <= part.Bottom)
                            {
                                pointing = button;
                                if (pointing.IsButton)
                                    goto breakfor;
                            }
                        }
                }
            }


            //int posy_bottom2up = window.MainPicBox.Height - pointY;
            //int logNum = window.ScrollBar.Maximum - window.ScrollBar.Value;
            ////表示中の一番下の行番号
            //int curBottomLineNo = displayLineList.Count - logNum;
            //int curPointingLineNo = curBottomLineNo - (posy_bottom2up / Config.LineHeight + 1);
            //if ((curPointingLineNo < 0) || (curPointingLineNo >= displayLineList.Count))
            //	curLine = null;
            //else
            //	curLine =  displayLineList[curPointingLineNo];
            //if (curLine == null)
            //	goto end;

            //pointing = curLine.GetPointingButton(pointX);
            breakfor:
            if (pointing == null || pointing.Generation != lastButtonGeneration)
                canSelect = false;
            else if (!pointing.IsButton)
                canSelect = false;
            else if (state == ConsoleState.WaitInput && inputReq.InputType == InputType.IntValue && !pointing.IsInteger)
                canSelect = false;
            end:
            if (canSelect)
                select = pointing;
            var needRefresh = select != selectingButton || pointing != pointingString;
            pointingString = pointing;
            selectingButton = select;
            return needRefresh;
        }


        public void LeaveMouse()
        {
            var needRefresh = selectingButton != null || pointingString != null;
            selectingButton = null;
            pointingString = null;
            if (needRefresh)
                RefreshStrings(true);
        }

        private void verticalScrollBarUpdate()
        {
            var max = displayLineList.Count;
            var move = max - window.ScrollBar.Maximum;
            if (move == 0)
                return;
            if (move > 0)
            {
                window.ScrollBar.Maximum = max;
                window.ScrollBar.Value += move;
            }
            else
            {
                if (max > window.ScrollBar.Value)
                    window.ScrollBar.Value = max;
                window.ScrollBar.Maximum = max;
            }
            window.ScrollBar.Enabled = max > 0;
        }

        #endregion
    }
}