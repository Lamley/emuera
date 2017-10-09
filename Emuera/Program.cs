﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MinorShift.Emuera.Content;
using MinorShift._Library;

namespace MinorShift.Emuera
{
    internal static class Program
    {
        public static bool Reboot;

        //public static int RebootClientX = 0;
        public static int RebootClientY;

        public static FormWindowState RebootWinState = FormWindowState.Normal;
        public static Point RebootLocation;

        public static bool AnalysisMode;
        public static List<string> AnalysisFiles;

        public static bool debugMode;

        /// <summary>
        ///     実行ファイルのディレクトリ。最後に\を付けたstring
        /// </summary>
        public static string ExeDir { get; private set; }

        public static string CsvDir { get; private set; }
        public static string ErbDir { get; private set; }
        public static string DebugDir { get; private set; }
        public static string DatDir { get; private set; }
        public static string ContentDir { get; private set; }
        public static string ExeName { get; private set; }
        public static bool DebugMode => debugMode;


        public static uint StartTime { get; private set; }

        /*
        コードの開始地点。
        ここでMainWindowを作り、
        MainWindowがProcessを作り、
        ProcessがGameBase・ConstantData・Variableを作る。
        
        
        *.ERBの読み込み、実行、その他の処理をProcessが、
        入出力をMainWindowが、
        定数の保存をConstantDataが、
        変数の管理をVariableが行う。
         
        と言う予定だったが改変するうちに境界が曖昧になってしまった。
         
        後にEmueraConsoleを追加し、それに入出力を担当させることに。
        
        1750 DebugConsole追加
         Debugを全て切り離すことはできないので一部EmueraConsoleにも担当させる
        
        TODO: 1819 MainWindow & Consoleの入力・表示組とProcess&Dataのデータ処理組だけでも分離したい

        */
        /// <summary>
        ///     アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            ExeDir = Sys.ExeDir;
#if DEBUG
            //debugMode = true;
#endif
            CsvDir = ExeDir + "csv\\";
            ErbDir = ExeDir + "erb\\";
            DebugDir = ExeDir + "debug\\";
            DatDir = ExeDir + "dat\\";
            ContentDir = ExeDir + "resources\\";
            //エラー出力用
            //1815 .exeが東方板のNGワードに引っかかるそうなので除去
            ExeName = Path.GetFileNameWithoutExtension(Sys.ExeName);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ConfigData.Instance.LoadConfig();
            //二重起動の禁止かつ二重起動
            if (!Config.AllowMultipleInstances && Sys.PrevInstance())
            {
                MessageBox.Show("多重起動を許可する場合、emuera.configを書き換えて下さい", "既に起動しています");
                return;
            }
            if (!Directory.Exists(CsvDir))
            {
                MessageBox.Show("csvフォルダが見つかりません", "フォルダなし");
                return;
            }
            if (!Directory.Exists(ErbDir))
            {
                MessageBox.Show("erbフォルダが見つかりません", "フォルダなし");
                return;
            }
            var argsStart = 0;
            if (args.Length > 0 && args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase))
            {
                argsStart = 1; //デバッグモードかつ解析モード時に最初の1っこ(-DEBUG)を飛ばす
                debugMode = true;
            }
            if (debugMode)
            {
                ConfigData.Instance.LoadDebugConfig();
                if (!Directory.Exists(DebugDir))
                    try
                    {
                        Directory.CreateDirectory(DebugDir);
                    }
                    catch
                    {
                        MessageBox.Show("debugフォルダの作成に失敗しました", "フォルダなし");
                        return;
                    }
            }
            if (args.Length > argsStart)
            {
                AnalysisFiles = new List<string>();
                for (var i = argsStart; i < args.Length; i++)
                {
                    if (!File.Exists(args[i]) && !Directory.Exists(args[i]))
                    {
                        MessageBox.Show("与えられたファイル・フォルダは存在しません");
                        return;
                    }
                    if ((File.GetAttributes(args[i]) & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        var fnames = Config.GetFiles(args[i] + "\\", "*.ERB");
                        for (var j = 0; j < fnames.Count; j++)
                            AnalysisFiles.Add(fnames[j].Value);
                    }
                    else
                    {
                        if (Path.GetExtension(args[i]).ToUpper() != ".ERB")
                        {
                            MessageBox.Show("ドロップ可能なファイルはERBファイルのみです");
                            return;
                        }
                        AnalysisFiles.Add(args[i]);
                    }
                }
                AnalysisMode = true;
            }
            MainWindow win = null;
            while (true)
            {
                StartTime = WinmmTimer.TickCount;
                using (win = new MainWindow())
                {
                    Application.Run(win);
                    AppContents.UnloadContents();
                    if (!Reboot)
                        break;

                    RebootWinState = win.WindowState;
                    if (win.WindowState == FormWindowState.Normal)
                    {
                        RebootClientY = win.ClientSize.Height;
                        RebootLocation = win.Location;
                    }
                    else
                    {
                        RebootClientY = 0;
                        RebootLocation = new Point();
                    }
                }
                //条件次第ではParserMediatorが空でない状態で再起動になる場合がある
                ParserMediator.ClearWarningList();
                ParserMediator.Initialize(null);
                GlobalStatic.Reset();
                //GC.Collect();
                Reboot = false;
                ConfigData.Instance.LoadConfig();
            }
        }
    }
}