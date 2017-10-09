using System.Collections.Generic;
using System.Windows.Forms;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameProc
{
    internal sealed class HeaderFileLoader
    {
        private readonly IdentifierDictionary idDic;
        private readonly EmueraConsole output;
        private readonly Process parentProcess;

        private bool noError = true;

        public HeaderFileLoader(EmueraConsole main, IdentifierDictionary idDic, Process proc)
        {
            output = main;
            parentProcess = proc;
            this.idDic = idDic;
        }

        /// <summary>
        /// </summary>
        /// <param name="erbDir"></param>
        /// <param name="displayReport"></param>
        /// <returns></returns>
        public bool LoadHeaderFiles(string headerDir, bool displayReport)
        {
            var headerFiles = Config.GetFiles(headerDir, "*.ERH");
            var noError = true;
            try
            {
                for (var i = 0; i < headerFiles.Count; i++)
                {
                    var filename = headerFiles[i].Key;
                    var file = headerFiles[i].Value;
                    if (displayReport)
                        output.PrintSystemLine(filename + "読み込み中・・・");
                    noError = loadHeaderFile(file, filename);
                    if (!noError)
                        break;
                    Application.DoEvents();
                }
            }
            finally
            {
                ParserMediator.FlushWarningList();
            }
            return noError;
        }


        private bool loadHeaderFile(string filepath, string filename)
        {
            StringStream st = null;
            ScriptPosition position = null;
            //EraStreamReader eReader = new EraStreamReader(false);
            //1815修正 _rename.csvの適用
            //eramakerEXの仕様的には.ERHに適用するのはおかしいけど、もうEmueraの仕様になっちゃってるのでしかたないか
            var eReader = new EraStreamReader(true);

            if (!eReader.Open(filepath, filename))
                throw new CodeEE(eReader.Filename + "のオープンに失敗しました");
            try
            {
                while ((st = eReader.ReadEnabledLine()) != null)
                {
                    if (!noError)
                        return false;
                    position = new ScriptPosition(filename, eReader.LineNo, st.RowString);
                    LexicalAnalyzer.SkipWhiteSpace(st);
                    if (st.Current != '#')
                        throw new CodeEE("ヘッダーの中に#で始まらない行があります", position);
                    st.ShiftNext();
                    var sharpID = LexicalAnalyzer.ReadSingleIdentifier(st);
                    if (sharpID == null)
                    {
                        ParserMediator.Warn("解釈できない#行です", position, 1);
                        return false;
                    }
                    if (Config.ICFunction)
                        sharpID = sharpID.ToUpper();
                    LexicalAnalyzer.SkipWhiteSpace(st);
                    switch (sharpID)
                    {
                        case "DEFINE":
                            analyzeSharpDefine(st, position);
                            break;
                        case "FUNCTION":
                        case "FUNCTIONS":
                            analyzeSharpFunction(st, position, sharpID == "FUNCTIONS");
                            break;
                        case "DIM":
                        case "DIMS":
                            analyzeSharpDim(st, position, sharpID == "DIMS");
                            break;
                        default:
                            throw new CodeEE("#" + sharpID + "は解釈できないプリプロセッサです", position);
                    }
                }
            }
            catch (CodeEE e)
            {
                if (e.Position != null)
                    position = e.Position;
                ParserMediator.Warn(e.Message, position, 2);
                return false;
            }
            finally
            {
                eReader.Close();
            }
            return true;
        }

        //#define FOO (～～)     id to wc
        //#define BAR($1) (～～)     idwithargs to wc(replaced)
        //#diseble FOOBAR             
        //#dim piyo, i
        //#dims puyo, j
        //static List<string> keywordsList = new List<string>();

        private void analyzeSharpDefine(StringStream st, ScriptPosition position)
        {
            //LexicalAnalyzer.SkipWhiteSpace(st);呼び出し前に行う。
            var srcID = LexicalAnalyzer.ReadSingleIdentifier(st);
            if (srcID == null)
                throw new CodeEE("置換元の識別子がありません", position);
            if (Config.ICVariable)
                srcID = srcID.ToUpper();

            //ここで名称重複判定しないと、大変なことになる
            var errMes = "";
            var errLevel = -1;
            idDic.CheckUserMacroName(ref errMes, ref errLevel, srcID);
            if (errLevel >= 0)
            {
                ParserMediator.Warn(errMes, position, errLevel);
                if (errLevel >= 2)
                {
                    noError = false;
                    return;
                }
            }

            var hasArg = st.Current == '('; //引数を指定する場合には直後に(が続いていなければならない。ホワイトスペースも禁止。
            //1808a3 代入演算子許可（関数宣言用）
            var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
            if (wc.EOL)
            {
                //throw new CodeEE("置換先の式がありません", position);
                //1808a3 空マクロの許可
                var nullmac = new DefineMacro(srcID, new WordCollection(), 0);
                idDic.AddMacro(nullmac);
                return;
            }

            var argID = new List<string>();
            if (hasArg) //関数型マクロの引数解析
            {
                wc.ShiftNext(); //'('を読み飛ばす
                if (wc.Current.Type == ')')
                    throw new CodeEE("関数型マクロの引数を0個にすることはできません", position);
                while (!wc.EOL)
                {
                    var word = wc.Current as IdentifierWord;
                    if (word == null)
                        throw new CodeEE("置換元の引数指定の書式が間違っています", position);
                    word.SetIsMacro();
                    var id = word.Code;
                    if (argID.Contains(id))
                        throw new CodeEE("置換元の引数に同じ文字が2回以上使われています", position);
                    argID.Add(id);
                    wc.ShiftNext();
                    if (wc.Current.Type == ',')
                    {
                        wc.ShiftNext();
                        continue;
                    }
                    if (wc.Current.Type == ')')
                        break;
                    throw new CodeEE("置換元の引数指定の書式が間違っています", position);
                }
                if (wc.EOL)
                    throw new CodeEE("')'が閉じられていません", position);

                wc.ShiftNext();
            }
            if (wc.EOL)
                throw new CodeEE("置換先の式がありません", position);
            var destWc = new WordCollection();
            while (!wc.EOL)
            {
                destWc.Add(wc.Current);
                wc.ShiftNext();
            }
            if (hasArg) //関数型マクロの引数セット
            {
                while (!destWc.EOL)
                {
                    var word = destWc.Current as IdentifierWord;
                    if (word == null)
                    {
                        destWc.ShiftNext();
                        continue;
                    }
                    for (var i = 0; i < argID.Count; i++)
                        if (string.Equals(word.Code, argID[i], Config.SCVariable))
                        {
                            destWc.Remove();
                            destWc.Insert(new MacroWord(i));
                            break;
                        }
                    destWc.ShiftNext();
                }
                destWc.Pointer = 0;
            }
            if (hasArg) //1808a3 関数型マクロの封印
                throw new CodeEE("関数型マクロは宣言できません", position);
            var mac = new DefineMacro(srcID, destWc, argID.Count);
            idDic.AddMacro(mac);
        }

        private void analyzeSharpDim(StringStream st, ScriptPosition position, bool dims)
        {
            var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
            var data = UserDefinedVariableData.Create(wc, dims, false, position);
            if (data.Reference)
                throw new NotImplCodeEE();
            VariableToken var = null;
            if (data.CharaData)
                var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
            else
                var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
            idDic.AddUseDefinedVariable(var);
        }

        private void analyzeSharpFunction(StringStream st, ScriptPosition position, bool funcs)
        {
            throw new NotImplCodeEE();
            //WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
            //UserDefinedFunctionData data = UserDefinedFunctionData.Create(wc, funcs, position);
            //idDic.AddRefMethod(UserDefinedRefMethod.Create(data));
        }
    }
}