using System;
using System.Collections.Generic;
using System.Media;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameProc
{
    internal sealed partial class Process
    {
        private const int AutoSaveIndex = 99;

        //(Train.csv中の値・定義されていなければ-1) == comAble[(表示されている値)];
        private int[] comAble; //

        private List<long> coms = new List<long>();
        private int count;

        private bool[] dataIsAvailable = new bool[21];

        private long doTrainSelectCom = -1;
        private bool isCTrain;
        private bool isFirstTime = true;
        private int lastAddCom = -1;
        private int lastCalledComable = -1;
        private bool needCheck = true;
        public bool NeedWaitToEventComEnd;
        private int page;

        private int printComCount;

        private int saveTarget = -1;

        private readonly Dictionary<SystemStateCode, SystemProcess> systemProcessDictionary =
            new Dictionary<SystemStateCode, SystemProcess>();


        private long systemResult;
        private string[] TrainName;
        public bool SkipPrint { get; set; }

        private void initSystemProcess()
        {
            comAble = new int[TrainName.Length];
            systemProcessDictionary.Add(SystemStateCode.Title_Begin, beginTitle);
            systemProcessDictionary.Add(SystemStateCode.Openning, endOpenning);

            systemProcessDictionary.Add(SystemStateCode.Train_Begin, beginTrain);
            systemProcessDictionary.Add(SystemStateCode.Train_CallEventTrain, endCallEventTrain);
            systemProcessDictionary.Add(SystemStateCode.Train_CallShowStatus, endCallShowStatus);
            systemProcessDictionary.Add(SystemStateCode.Train_CallComAbleXX, endCallComAbleXX);
            systemProcessDictionary.Add(SystemStateCode.Train_CallShowUserCom, endCallShowUserCom);
            systemProcessDictionary.Add(SystemStateCode.Train_WaitInput, trainWaitInput);
            systemProcessDictionary.Add(SystemStateCode.Train_CallEventCom, endEventCom);
            systemProcessDictionary.Add(SystemStateCode.Train_CallComXX, endCallComXX);
            systemProcessDictionary.Add(SystemStateCode.Train_CallSourceCheck, endCallSourceCheck);
            systemProcessDictionary.Add(SystemStateCode.Train_CallEventComEnd, endCallEventComEnd);
            ;
            systemProcessDictionary.Add(SystemStateCode.Train_DoTrain, doTrain);

            systemProcessDictionary.Add(SystemStateCode.AfterTrain_Begin, beginAfterTrain);

            systemProcessDictionary.Add(SystemStateCode.Ablup_Begin, beginAblup);
            systemProcessDictionary.Add(SystemStateCode.Ablup_CallShowJuel, endCallShowJuel);
            systemProcessDictionary.Add(SystemStateCode.Ablup_CallShowAblupSelect, endCallShowAblupSelect);
            systemProcessDictionary.Add(SystemStateCode.Ablup_WaitInput, ablupWaitInput);
            systemProcessDictionary.Add(SystemStateCode.Ablup_CallAblupXX, endCallAblupXX);

            systemProcessDictionary.Add(SystemStateCode.Turnend_Begin, beginTurnend);

            systemProcessDictionary.Add(SystemStateCode.Shop_Begin, beginShop);
            systemProcessDictionary.Add(SystemStateCode.Shop_CallEventShop, endCallEventShop);
            systemProcessDictionary.Add(SystemStateCode.Shop_CallShowShop, endCallShowShop);
            systemProcessDictionary.Add(SystemStateCode.Shop_WaitInput, shopWaitInput);
            systemProcessDictionary.Add(SystemStateCode.Shop_CallEventBuy, endCallEventBuy);

            systemProcessDictionary.Add(SystemStateCode.SaveGame_Begin, beginSaveGame);
            systemProcessDictionary.Add(SystemStateCode.SaveGame_WaitInput, saveGameWaitInput);
            systemProcessDictionary.Add(SystemStateCode.SaveGame_WaitInputOverwrite, saveGameWaitInputOverwrite);
            systemProcessDictionary.Add(SystemStateCode.SaveGame_CallSaveInfo, endCallSaveInfo);
            systemProcessDictionary.Add(SystemStateCode.LoadGame_Begin, beginLoadGame);
            systemProcessDictionary.Add(SystemStateCode.LoadGame_WaitInput, loadGameWaitInput);
            systemProcessDictionary.Add(SystemStateCode.LoadGameOpenning_Begin, beginLoadGameOpening);
            systemProcessDictionary.Add(SystemStateCode.LoadGameOpenning_WaitInput, loadGameWaitInput);

            //stateEndProcessDictionary.Add(ProgramState.AutoSave_Begin, new stateEndProcess(this.beginAutoSave));
            systemProcessDictionary.Add(SystemStateCode.AutoSave_CallSaveInfo, endAutoSaveCallSaveInfo);
            systemProcessDictionary.Add(SystemStateCode.AutoSave_CallUniqueAutosave, endAutoSave);

            systemProcessDictionary.Add(SystemStateCode.LoadData_DataLoaded, beginDataLoaded);
            systemProcessDictionary.Add(SystemStateCode.LoadData_CallSystemLoad, endSystemLoad);
            systemProcessDictionary.Add(SystemStateCode.LoadData_CallEventLoad, endEventLoad);

            systemProcessDictionary.Add(SystemStateCode.Openning_TitleLoadgame, endTitleLoadgame);

            systemProcessDictionary.Add(SystemStateCode.System_Reloaderb, endReloaderb);
            systemProcessDictionary.Add(SystemStateCode.First_Begin, beginFirst);


            systemProcessDictionary.Add(SystemStateCode.Normal, endNormal);
        }


        private void runSystemProc()
        {
            //スクリプト実行中にここには来ないはず
            //if (!state.ScriptEnd)
            //    throw new ExeEE("不正な呼び出し");

            //ない物を渡す処理は現状ない
            //if (systemProcessDictionary.ContainsKey(state.SystemState))
            systemProcessDictionary[getCurrentState.SystemState]();
            //else
            //    throw new ExeEE("未定義の状態");
        }

        private void setWait()
        {
            console.ReadAnyKey();
        }

        private void setWaitInput()
        {
            var req = new InputRequest();
            req.InputType = InputType.IntValue;
            req.IsSystemInput = true;
            console.WaitInput(req);
        }


        private bool callFunction(string functionName, bool force, bool isEvent)
        {
            CalledFunction call = null;
            if (isEvent)
                call = CalledFunction.CallEventFunction(this, functionName, null);
            else
                call = CalledFunction.CallFunction(this, functionName, null);
            if (call == null)
                if (!force)
                    return false;
                else
                    throw new CodeEE("関数\"@" + functionName + "\"が見つかりません");
            //そもそも非イベント関数では関数1個分しか与えないので条件を満たすわけがない
            //if ((!isEvent) && (call.Count > 1))
            //    throw new ExeEE("イベント関数でない関数\"@" + functionName + "\"の候補が複数ある");
            getCurrentState.IntoFunction(call, null, null);
            return true;
        }

        //CheckState()から呼ばれる関数群。ScriptEndに達したときの処理。

        private void beginTitle()
        {
            //連続調教コマンド処理中の状態が持ち越されていたらここで消しておく
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            console.ResetStyle();
            deleteAllPrevState();
            if (Program.AnalysisMode)
            {
                console.PrintSystemLine("ファイル解析終了：Analysis.logに出力します");
                console.OutputLog(Program.ExeDir + "Analysis.log");
                console.noOutputLog = true;
                console.PrintSystemLine("エンターキーもしくはクリックで終了します");
                SystemSounds.Asterisk.Play();
                console.ThrowTitleError(false);
                return;
            }
            if (!noError && !Config.CompatiErrorLine)
            {
                console.PrintSystemLine("ERBコードに解釈不可能な行があるためEmueraを終了します");
                console.PrintSystemLine("※互換性オプション「" + Config.GetConfigName(ConfigCode.CompatiErrorLine) +
                                        "」により強制的に動作させることができます");
                console.PrintSystemLine("emuera.logにログを出力します");
                console.OutputLog(Program.ExeDir + "emuera.log");
                console.noOutputLog = true;
                console.PrintSystemLine("エンターキーもしくはクリックで終了します");
                //System.Media.SystemSounds.Asterisk.Play();
                console.ThrowTitleError(true);
                return;
            }
            if (callFunction("SYSTEM_TITLE", false, false))
            {
//独自定義
                getCurrentState.SystemState = SystemStateCode.Normal;
                return;
            }
            //標準のタイトル画面
            console.PrintBar();
            console.NewLine();
            console.Alignment = DisplayLineAlignment.CENTER;
            console.PrintSingleLine(gamebase.ScriptTitle);
            if (gamebase.ScriptVersion != 0)
                console.PrintSingleLine(gamebase.ScriptVersionText);
            console.PrintSingleLine(gamebase.ScriptAutherName);
            console.PrintSingleLine("(" + gamebase.ScriptYear + ")");
            console.NewLine();
            console.PrintSingleLine(gamebase.ScriptDetail);
            console.Alignment = DisplayLineAlignment.LEFT;

            console.PrintBar();
            console.NewLine();
            console.PrintSingleLine("[0] " + Config.TitleMenuString0);
            console.PrintSingleLine("[1] " + Config.TitleMenuString1);
            openingInput();
        }

        private void openingInput()
        {
            setWaitInput();
            getCurrentState.SystemState = SystemStateCode.Openning;
        }

        private void endOpenning()
        {
            if (systemResult == 0)
            {
//[0] 最初からはじめる
                vEvaluator.ResetData();
                //vEvaluator.AddCharacter(0, false);
                vEvaluator.AddCharacterFromCsvNo(0);
                if (gamebase.DefaultCharacter > 0)
                    //vEvaluator.AddCharacter(gamebase.DefaultCharacter, false);
                    vEvaluator.AddCharacterFromCsvNo(gamebase.DefaultCharacter);
                console.PrintBar();
                console.NewLine();
                beginFirst();
            }
            else if (systemResult == 1)
            {
                if (callFunction("TITLE_LOADGAME", false, false))
                    getCurrentState.SystemState = SystemStateCode.Openning_TitleLoadgame;
                else
                    beginLoadGameOpening();
            }
            else //入力が正しくないならもう一回選択肢を書き直し、正しい選択を要求する。
            {
//RESUELASTLINEと同様の処理を行うように変更
                console.deleteLine(1);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                openingInput();
                //beginTitle();
            }
        }

        private void beginFirst()
        {
            getCurrentState.SystemState = SystemStateCode.Normal;
            //連続調教コマンド処理中の状態が持ち越されていたらここで消しておく
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            callFunction("EVENTFIRST", true, true);
        }

        private void endTitleLoadgame()
        {
            beginTitle();
        }

        private void beginTrain()
        {
            vEvaluator.UpdateInBeginTrain();
            getCurrentState.SystemState = SystemStateCode.Train_CallEventTrain;
            //EVENTTRAINを呼び出してTrain_CallEventTrainへ移行。
            if (!callFunction("EVENTTRAIN", false, true))
                endCallEventTrain();
        }

        private void endCallEventTrain()
        {
            if (vEvaluator.NEXTCOM >= 0)
            {
//NEXTCOMの処理
                getCurrentState.SystemState = SystemStateCode.Train_CallEventCom;
                vEvaluator.SELECTCOM = vEvaluator.NEXTCOM;
                vEvaluator.NEXTCOM = 0;
                //-1ではなく0を代入するのでERB側で変更しない限り無限にはまることになるがeramakerからの仕様である。
                callEventCom();
            }
            else
            {
                //if (!isCTrain)
                //{
                //SHOW_STATUSを呼び出してTrain_CallShowStatusへ移行。
                if (isCTrain)
                    SkipPrint = true;
                callFunction("SHOW_STATUS", true, false);
                getCurrentState.SystemState = SystemStateCode.Train_CallShowStatus;
                //}
                //else
                //{
                //連続調教モードならCOMABLE処理へ
                //	endCallShowStatus();
                //}
            }
        }

        private void endCallShowStatus()
        {
            //SHOW_STATUSが終わったらComAbleXXの呼び出し状態をリセットしてTrain_CallComAbleXXへ移行。
            getCurrentState.SystemState = SystemStateCode.Train_CallComAbleXX;
            lastCalledComable = -1;
            lastAddCom = -1;
            printComCount = 0;
            for (var i = 0; i < comAble.Length; i++)
                comAble[i] = -1;
            endCallComAbleXX();
        }

        private string getTrainComString(int trainCode, int comNo)
        {
            var trainName = TrainName[trainCode];
            return string.Format("{0}[{1,3}]", trainName, comNo);
        }

        private void endCallComAbleXX()
        {
            //選択肢追加。RESULTが0の場合は選択肢の番号のみ増やして追加はしない。
            if (lastCalledComable >= 0 && TrainName[lastCalledComable] != null)
            {
                lastAddCom++;
                if (vEvaluator.RESULT != 0)
                {
                    comAble[lastAddCom] = lastCalledComable;
                    if (!isCTrain)
                    {
                        console.PrintC(getTrainComString(lastCalledComable, lastAddCom), true);
                        printComCount++;
                        if (Config.PrintCPerLine > 0 && printComCount % Config.PrintCPerLine == 0)
                            console.PrintFlush(false);
                    }
                    console.RefreshStrings(false);
                }
            }
            //ComAbleXXの呼び出し。train.csvに定義されていないものはスキップ、ComAbleXXが見つからなければREUTRN 1と同様に扱う。
            while (++lastCalledComable < TrainName.Length)
            {
                if (TrainName[lastCalledComable] == null)
                    continue;
                var comName = string.Format("COM_ABLE{0}", lastCalledComable);
                if (!callFunction(comName, false, false))
                {
                    lastAddCom++;
                    if (Config.ComAbleDefault == 0)
                        continue;
                    comAble[lastAddCom] = lastCalledComable;
                    if (!isCTrain)
                    {
                        console.PrintC(getTrainComString(lastCalledComable, lastAddCom), true);
                        printComCount++;
                        if (Config.PrintCPerLine > 0 && printComCount % Config.PrintCPerLine == 0)
                            console.PrintFlush(false);
                    }
                    continue;
                }
                console.RefreshStrings(false);
                return;
            }
            //全部検索したら終了し、SHOW_USERCOMを呼び出す。
            if (lastCalledComable >= TrainName.Length)
            {
                getCurrentState.SystemState = SystemStateCode.Train_CallShowUserCom;
                //if (!isCTrain)
                //{
                console.PrintFlush(false);
                console.RefreshStrings(false);
                callFunction("SHOW_USERCOM", true, false);
                //}
                //else
                //	endCallShowUserCom();
            }
        }

        private void endCallShowUserCom()
        {
            if (SkipPrint)
                SkipPrint = false;
            vEvaluator.UpdateAfterShowUsercom();
            if (!isCTrain)
            {
                //数値入力待ち状態にしてTrain_WaitInputへ移行。
                setWaitInput();

                getCurrentState.SystemState = SystemStateCode.Train_WaitInput;
            }
            else
            {
                if (count < coms.Count)
                {
                    systemResult = coms[count];
                    count++;
                    trainWaitInput();
                }
            }
        }

        private void trainWaitInput()
        {
            var selectCom = -1;
            if (!isCTrain)
            {
                if (systemResult >= 0 && systemResult < comAble.Length)
                    selectCom = comAble[systemResult];
            }
            else
            {
                for (var i = 0; i < comAble.Length; i++)
                    if (comAble[i] == systemResult)
                        selectCom = (int) systemResult;
                console.PrintSingleLine(string.Format("＜コマンド連続実行：{0}/{1}＞", count, coms.Count));
            }
            //TrainNameが定義されていて使用可能(COMABLEが非0を返した)である
            if (selectCom >= 0)
            {
                vEvaluator.SELECTCOM = selectCom;
                callEventCom();
            }
            else
            {
//されていない。
                if (isCTrain)
                    console.PrintSingleLine("コマンドを実行できませんでした");
                vEvaluator.RESULT = systemResult;
                getCurrentState.SystemState = SystemStateCode.Train_CallEventComEnd;
                callFunction("USERCOM", true, false);
                //COM中の必要なことは全部USERCOM内でやる。
            }
        }

        private void doTrain()
        {
            vEvaluator.UpdateAfterShowUsercom();
            vEvaluator.SELECTCOM = doTrainSelectCom;
            callEventCom();
        }

        private void callEventCom()
        {
            vEvaluator.UpdateAfterInputCom();
            getCurrentState.SystemState = SystemStateCode.Train_CallEventCom;
            if (!callFunction("EVENTCOM", false, true))
                endEventCom();
        }

        private void endEventCom()
        {
            var selectCom = vEvaluator.SELECTCOM;
            var comName = string.Format("COM{0}", selectCom);
            getCurrentState.SystemState = SystemStateCode.Train_CallComXX;
            callFunction(comName, true, false);
        }

        private void endCallComXX()
        {
            //実行に失敗した
            if (vEvaluator.RESULT == 0)
            {
                //Com終了。
                endCallEventComEnd();
            }
            else
            {
//成功したならSOURCE_CHECKへ移行。
                getCurrentState.SystemState = SystemStateCode.Train_CallSourceCheck;
                callFunction("SOURCE_CHECK", true, false);
            }
        }

        private void endCallSourceCheck()
        {
            //SOURCEはここでリセット
            vEvaluator.UpdateAfterSourceCheck();
            //EVENTCOMENDを呼び出してTrain_CallEventComEndへ移行。
            getCurrentState.SystemState = SystemStateCode.Train_CallEventComEnd;
            //EVENTCOMENDが存在しない、またはEVENTCOMEND内でWAIT系命令が行われない場合、EVENTCOMEND後にWAITを追加する。
            NeedWaitToEventComEnd = true;
            if (!callFunction("EVENTCOMEND", false, true))
                endCallEventComEnd();
        }

        private void endCallEventComEnd()
        {
            if (console.LastLineIsTemporary && !isCTrain && needCheck)
            {
                console.deleteLine(2);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                endCallShowUserCom();
            }
            else
            {
                if (isCTrain && count == coms.Count)
                {
                    isCTrain = false;
                    SkipPrint = false;
                    coms.Clear();
                    count = 0;
                    if (callFunction("CALLTRAINEND", false, false))
                    {
                        needCheck = false;
                        return;
                    }
                }
                needCheck = true;
                ////1.701	ここでWAITは不要だった。
                ////setWait();
                //1.703 やはり必要な場合もあった
                if (NeedWaitToEventComEnd)
                    setWait();
                NeedWaitToEventComEnd = false;
                //SHOW_STATUSからやり直す。
                //処理はTrain_CallEventTrainと同じ。
                endCallEventTrain();
            }
        }

        private void beginAfterTrain()
        {
            //連続調教モード中にここに来る場合があるので、ここで解除
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            getCurrentState.SystemState = SystemStateCode.Normal;
            //EVENTENDを呼び出す。exe側が状態を把握する必要が無くなるのでNormalへ移行。
            callFunction("EVENTEND", true, true);
        }

        private void beginAblup()
        {
            //連続調教コマンド処理中の状態が持ち越されていたらここで消しておく
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            getCurrentState.SystemState = SystemStateCode.Ablup_CallShowJuel;
            //SHOW_JUELを呼び出しAblup_CallShowJuelへ移行。
            callFunction("SHOW_JUEL", true, false);
        }

        private void endCallShowJuel()
        {
            getCurrentState.SystemState = SystemStateCode.Ablup_CallShowAblupSelect;
            //SHOW_ABLUP_SELECTを呼び出しAblup_CallAblupSelectへ移行。
            callFunction("SHOW_ABLUP_SELECT", true, false);
        }

        private void endCallShowAblupSelect()
        {
            //数値入力待ち状態にしてAblup_WaitInputへ移行。
            setWaitInput();
            getCurrentState.SystemState = SystemStateCode.Ablup_WaitInput;
        }

        private void ablupWaitInput()
        {
            //定義されていなくても100未満ならABLUPが呼ばれ、USERABLUPは呼ばれない。そうしないと[99]反発刻印とかが出来ない。
            if (systemResult >= 0 && systemResult < 100)
            {
                getCurrentState.SystemState = SystemStateCode.Ablup_CallAblupXX;
                var ablName = string.Format("ABLUP{0}", systemResult);
                if (!callFunction(ablName, false, false))
                {
                    //見つからなければ終了
                    console.deleteLine(1);
                    console.PrintTemporaryLine("無効な値です");
                    console.updatedGeneration = true;
                    endCallShowAblupSelect();
                }
            }
            else
            {
                vEvaluator.RESULT = systemResult;
                getCurrentState.SystemState = SystemStateCode.Ablup_CallAblupXX;
                callFunction("USERABLUP", true, false);
            }
        }

        private void endCallAblupXX()
        {
            if (console.LastLineIsTemporary)
            {
                console.deleteLine(2);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                endCallShowAblupSelect();
            }
            else
            {
                beginAblup();
            }
        }

        private void beginTurnend()
        {
            //連続調教コマンド処理中の状態が持ち越されていたらここで消しておく
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            //EVENTTURNENDを呼び出しNormalへ移行
            callFunction("EVENTTURNEND", true, true);
            getCurrentState.SystemState = SystemStateCode.Normal;
        }

        private void beginShop()
        {
            //連続調教コマンド処理中の状態が持ち越されていたらここで消しておく
            if (isCTrain)
                if (ClearCommands())
                    return;
            SkipPrint = false;
            getCurrentState.SystemState = SystemStateCode.Shop_CallEventShop;
            //EVENTSHOPを呼び出してShop_CallEventShopへ移行。
            if (!callFunction("EVENTSHOP", false, true))
                endCallEventShop();
        }

        private void endCallEventShop()
        {
            saveTarget = -1;
            if (Config.AutoSave && getCurrentState.calledWhenNormal)
            {
                beginAutoSave();
            }
            else
            {
                getCurrentState.SystemState = SystemStateCode.AutoSave_Skipped;
                endAutoSaveCallSaveInfo();
            }
        }

        private void beginAutoSave()
        {
            if (callFunction("SYSTEM_AUTOSAVE", false, false))
            {
//@SYSTEM_AUTOSAVEが存在するならそれを使う。
                getCurrentState.SystemState = SystemStateCode.AutoSave_CallUniqueAutosave;
                return;
            }
            saveTarget = AutoSaveIndex;
            vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
            getCurrentState.SystemState = SystemStateCode.AutoSave_CallSaveInfo;
            if (!callFunction("SAVEINFO", false, false))
                endAutoSaveCallSaveInfo(); //存在しなければスキップ
        }

        private void endAutoSaveCallSaveInfo()
        {
            if (saveTarget == AutoSaveIndex)
                if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
                {
                    console.PrintError("オートセーブ中に予期しないエラーが発生しました");
                    console.PrintError("オートセーブをスキップします");
                    console.ReadAnyKey();
                }
            endAutoSave();
        }

        private void endAutoSave()
        {
            if (getCurrentState.isBegun)
            {
                getCurrentState.Begin();
                return;
            }
            getCurrentState.SystemState = SystemStateCode.Shop_CallShowShop;
            //SHOW_SHOPを呼び出しShop_CallShowShopへ移行
            callFunction("SHOW_SHOP", true, false);
        }

        private void endCallShowShop()
        {
            //数値入力待ち状態にしてShop_WaitInputへ移行。
            setWaitInput();
            getCurrentState.SystemState = SystemStateCode.Shop_WaitInput;
        }

        //PRINT_SHOPITEMとは独立している。
        //BOUGHTが100以上のアイテムが有り、ITEMSALESがTRUEだとしても強制的に@USERSHOP行き。
        private void shopWaitInput()
        {
            if (systemResult >= 0 && systemResult < Config.MaxShopItem)
            {
                if (vEvaluator.ItemSales(systemResult))
                {
                    if (vEvaluator.BuyItem(systemResult))
                    {
                        getCurrentState.SystemState = SystemStateCode.Shop_CallEventBuy;
                        //EVENTBUYを呼び出しShop_CallEventBuyへ移行
                        if (!callFunction("EVENTBUY", false, true))
                            endCallEventBuy();
                        return;
                    }
                    //console.Print("お金が足りません。");
                    //console.NewLine();
                    console.deleteLine(1);
                    console.PrintTemporaryLine("お金が足りません。");
                }
                else
                {
                    //console.Print("売っていません。");
                    //console.NewLine();
                    console.deleteLine(1);
                    console.PrintTemporaryLine("売っていません。");
                }
                //購入に失敗した場合、endCallEventShop()に戻す。
                //endCallEventShop();
                endCallShowShop();
            }
            else
            {
                //RESULTを更新
                vEvaluator.RESULT = systemResult;

                //USERSHOPを呼び出しShop_CallEventBuyへ移行
                callFunction("USERSHOP", true, false);
                getCurrentState.SystemState = SystemStateCode.Shop_CallEventBuy;
            }
        }

        private void endCallEventBuy()
        {
            if (console.LastLineIsTemporary)
            {
                console.deleteLine(2);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                endCallShowShop();
            }
            else
            {
                //最初に戻る
                endAutoSave();
            }
        }


        private void beginDataLoaded()
        {
            getCurrentState.SystemState = SystemStateCode.LoadData_CallSystemLoad;

            if (!callFunction("SYSTEM_LOADEND", false, false))
                endSystemLoad(); //存在しなければスキップ
        }

        private void endSystemLoad()
        {
            getCurrentState.SystemState = SystemStateCode.LoadData_CallEventLoad;
            //EVENTLOADを呼び出してLoadData_CallEventLoadへ移行。
            if (!callFunction("EVENTLOAD", false, true))
                endAutoSave();
        }

        private void endEventLoad()
        {
            //@EVENTLOAD中にBEGIN命令が行われればここには来ない。
            //ここに来たらBEGIN SHOP扱い。オートセーブはしない。
            endAutoSave();
        }

        private void beginSaveGame()
        {
            console.PrintSingleLine("何番にセーブしますか？");
            getCurrentState.SystemState = SystemStateCode.SaveGame_Begin;
            printSaveDataText();
        }

        private void beginLoadGame()
        {
            console.PrintSingleLine("何番をロードしますか？");
            getCurrentState.SystemState = SystemStateCode.LoadGame_Begin;
            printSaveDataText();
        }

        private void beginLoadGameOpening()
        {
            console.PrintSingleLine("何番をロードしますか？");
            getCurrentState.SystemState = SystemStateCode.LoadGameOpenning_Begin;
            printSaveDataText();
        }

        private void printSaveDataText()
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                dataIsAvailable = new bool[Config.SaveDataNos + 1];
            }
            var dataNo = 0;
            for (var i = 0; i < page; i++)
            {
                console.PrintFlush(false);
                console.Print(string.Format("[{0, 2}] セーブデータ{0, 2}～{1, 2}を表示", i * 20, i * 20 + 19));
            }
            for (var i = 0; i < 20; i++)
            {
                dataNo = page * 20 + i;
                if (dataNo == dataIsAvailable.Length - 1)
                    break;
                dataIsAvailable[dataNo] = false;
                console.PrintFlush(false);
                console.Print(string.Format("[{0, 2}] ", dataNo));
                if (!writeSavedataTextFrom(dataNo))
                    continue;
                dataIsAvailable[dataNo] = true;
            }
            for (var i = page; i < (dataIsAvailable.Length - 2) / 20; i++)
            {
                console.PrintFlush(false);
                console.Print(string.Format("[{0, 2}] セーブデータ{0, 2}～{1, 2}を表示", (i + 1) * 20, (i + 1) * 20 + 19));
            }
            //オートセーブの処理は別途切り出し（表示処理の都合上）
            dataIsAvailable[dataIsAvailable.Length - 1] = false;
            if (getCurrentState.SystemState != SystemStateCode.SaveGame_Begin)
            {
                dataNo = AutoSaveIndex;
                console.PrintFlush(false);
                console.Print(string.Format("[{0, 2}] ", dataNo));
                if (writeSavedataTextFrom(dataNo))
                    dataIsAvailable[dataIsAvailable.Length - 1] = true;
            }
            console.RefreshStrings(false);
            //描画全部終わり
            console.PrintSingleLine("[100] 戻る");
            setWaitInput();
            if (getCurrentState.SystemState == SystemStateCode.SaveGame_Begin)
                getCurrentState.SystemState = SystemStateCode.SaveGame_WaitInput;
            else if (getCurrentState.SystemState == SystemStateCode.LoadGame_Begin)
                getCurrentState.SystemState = SystemStateCode.LoadGame_WaitInput;
            else // if (state.SystemState == SystemStateCode.LoadGameOpenning_Begin)
                getCurrentState.SystemState = SystemStateCode.LoadGameOpenning_WaitInput;
            //きちんと処理されてるので、ここには来ない
            //else
            //    throw new ExeEE("異常な状態");
        }

        private void saveGameWaitInput()
        {
            if (systemResult == 100)
            {
                //キャンセルなら直前の状態を呼び戻す
                loadPrevState();
                return;
            }
            if ((int) systemResult / 20 != page && systemResult != AutoSaveIndex && systemResult >= 0 &&
                systemResult < dataIsAvailable.Length - 1)
            {
                page = (int) systemResult / 20;
                getCurrentState.SystemState = SystemStateCode.SaveGame_Begin;
                printSaveDataText();
                return;
            }
            var available = false;
            if (systemResult >= 0 && systemResult < dataIsAvailable.Length - 1)
            {
                available = dataIsAvailable[systemResult];
            }
            else
            {
//入力しなおし
                console.deleteLine(1);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                setWaitInput();
                return;
            }
            saveTarget = (int) systemResult;
            //既存データがあるなら選択肢を表示してSaveGame_WaitInputOverwriteへ移行。
            if (available)
            {
                console.PrintSingleLine("既にデータが存在します。上書きしますか？");
                console.PrintC("[0] はい", false);
                console.PrintC("[1] いいえ", false);
                setWaitInput();
                getCurrentState.SystemState = SystemStateCode.SaveGame_WaitInputOverwrite;
                return;
            }
            //既存データがないなら「はい」を選んだことにして直接ジャンプ
            systemResult = 0;
            saveGameWaitInputOverwrite();
        }

        private void saveGameWaitInputOverwrite()
        {
            if (systemResult == 1) //いいえ
            {
                beginSaveGame();
                return;
            }
            if (systemResult != 0) //「はい」でもない
            {
//入力しなおし
                console.deleteLine(1);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                setWaitInput();
                return;
            }
            vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
            getCurrentState.SystemState = SystemStateCode.SaveGame_CallSaveInfo;
            if (!callFunction("SAVEINFO", false, false))
                endCallSaveInfo(); //存在しなければスキップ
        }

        private void endCallSaveInfo()
        {
            if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
            {
                console.PrintError("セーブ中に予期しないエラーが発生しました");
                console.ReadAnyKey();
            }
            loadPrevState();
        }

        private void loadGameWaitInput()
        {
            if (systemResult == 100)
            {
//キャンセルなら
                //オープニングならオープニングへ戻る
                if (getCurrentState.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
                {
                    beginTitle();
                    return;
                }
                //それ以外から来たなら直前の状態を呼び戻す
                loadPrevState();
                return;
            }
            if ((int) systemResult / 20 != page && systemResult != AutoSaveIndex && systemResult >= 0 &&
                systemResult < dataIsAvailable.Length - 1)
            {
                page = (int) systemResult / 20;
                if (getCurrentState.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
                    getCurrentState.SystemState = SystemStateCode.LoadGameOpenning_Begin;
                else
                    getCurrentState.SystemState = SystemStateCode.LoadGame_Begin;
                printSaveDataText();
                return;
            }
            var available = false;
            if (systemResult >= 0 && systemResult < dataIsAvailable.Length - 1)
            {
                available = dataIsAvailable[systemResult];
            }
            else if (systemResult == AutoSaveIndex)
            {
                available = dataIsAvailable[dataIsAvailable.Length - 1];
            }
            else
            {
//入力しなおし
                console.deleteLine(1);
                console.PrintTemporaryLine("無効な値です");
                console.updatedGeneration = true;
                setWaitInput();
                return;
            }
            if (!available)
            {
                console.PrintSingleLine(systemResult.ToString());
                console.PrintError("データがありません");
                if (getCurrentState.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
                {
                    beginLoadGameOpening();
                    return;
                }
                beginLoadGame();
                return;
            }

            if (!vEvaluator.LoadFrom((int) systemResult))
                throw new ExeEE("ファイルのロード中に予期しないエラーが発生しました");
            deletePrevState();
            beginDataLoaded();
        }


        private void endNormal()
        {
            throw new CodeEE("予期しないスクリプト終端です");
        }

        private void endReloaderb()
        {
            loadPrevState();
            console.ReloadErbFinished();
        }

        private bool writeSavedataTextFrom(int saveIndex)
        {
            var result = vEvaluator.CheckData(saveIndex, EraSaveFileType.Normal);
            console.Print(result.DataMes);
            console.NewLine();
            return result.State == EraDataState.OK;
        }

        private delegate void SystemProcess();

        //1808 vEvaluator.SaveTo()などに移動
        //private bool loadFrom(int dataIndex)
        //private bool saveTo(int saveIndex, string saveText)
        //private string getSaveDataPath(int index)
    }
}