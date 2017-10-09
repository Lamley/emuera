using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Sub;
using MinorShift._Library;

namespace MinorShift.Emuera.GameData
{
    internal sealed class StrForm
    {
        private string[] strs; //terms.Length + 1
        private IOperandTerm[] terms;

        private StrForm()
        {
        }

        public bool IsConst => strs.Length == 1;

        public IOperandTerm GetIOperandTerm()
        {
            if (strs.Length == 2 && strs[0].Length == 0 && strs[1].Length == 0)
                return terms[0];
            return null;
        }

        public void Restructure(ExpressionMediator exm)
        {
            if (strs.Length == 1)
                return;
            var canRestructure = false;
            for (var i = 0; i < terms.Length; i++)
            {
                terms[i] = terms[i].Restructure(exm);
                if (terms[i] is SingleTerm)
                    canRestructure = true;
            }
            if (!canRestructure)
                return;
            var strList = new List<string>();
            var termList = new List<IOperandTerm>();
            strList.AddRange(strs);
            termList.AddRange(terms);
            for (var i = 0; i < termList.Count; i++)
                if (termList[i] is SingleTerm)
                {
                    var str = termList[i].GetStrValue(exm);
                    strList[i] = strList[i] + str + strList[i + 1];
                    termList.RemoveAt(i);
                    strList.RemoveAt(i + 1);
                    i--;
                }
            strs = new string[strList.Count];
            terms = new IOperandTerm[termList.Count];
            strList.CopyTo(strs);
            termList.CopyTo(terms);
        }

        public string GetString(ExpressionMediator exm)
        {
            if (strs.Length == 1)
                return strs[0];
            var builder = new StringBuilder(100);
            for (var i = 0; i < strs.Length - 1; i++)
            {
                builder.Append(strs[i]);
                builder.Append(terms[i].GetStrValue(exm));
            }
            builder.Append(strs[strs.Length - 1]);
            return builder.ToString();
        }

        #region static

        private static FormattedStringMethod formatCurlyBrace;
        private static FormattedStringMethod formatPercent;
        private static FormattedStringMethod formatYenAt;
        private static FunctionMethodTerm NameTarget; // "***"
        private static FunctionMethodTerm CallnameMaster; // "+++"
        private static FunctionMethodTerm CallnamePlayer; // "==="
        private static FunctionMethodTerm NameAssi; // "///"
        private static FunctionMethodTerm CallnameTarget; // "$$$"

        public static void Initialize()
        {
            formatCurlyBrace = new FormatCurlyBrace();
            formatPercent = new FormatPercent();
            formatYenAt = new FormatYenAt();
            var nameID = GlobalStatic.VariableData.GetSystemVariableToken("NAME");
            var callnameID = GlobalStatic.VariableData.GetSystemVariableToken("CALLNAME");
            IOperandTerm[] zeroArg = {new SingleTerm(0)};
            var target = new VariableTerm(GlobalStatic.VariableData.GetSystemVariableToken("TARGET"), zeroArg);
            var master = new VariableTerm(GlobalStatic.VariableData.GetSystemVariableToken("MASTER"), zeroArg);
            var player = new VariableTerm(GlobalStatic.VariableData.GetSystemVariableToken("PLAYER"), zeroArg);
            var assi = new VariableTerm(GlobalStatic.VariableData.GetSystemVariableToken("ASSI"), zeroArg);

            var nametarget = new VariableTerm(nameID, new IOperandTerm[] {target});
            var callnamemaster = new VariableTerm(callnameID, new IOperandTerm[] {master});
            var callnameplayer = new VariableTerm(callnameID, new IOperandTerm[] {player});
            var nameassi = new VariableTerm(nameID, new IOperandTerm[] {assi});
            var callnametarget = new VariableTerm(callnameID, new IOperandTerm[] {target});
            NameTarget = new FunctionMethodTerm(formatPercent, new IOperandTerm[] {nametarget, null, null});
            CallnameMaster = new FunctionMethodTerm(formatPercent, new IOperandTerm[] {callnamemaster, null, null});
            CallnamePlayer = new FunctionMethodTerm(formatPercent, new IOperandTerm[] {callnameplayer, null, null});
            NameAssi = new FunctionMethodTerm(formatPercent, new IOperandTerm[] {nameassi, null, null});
            CallnameTarget = new FunctionMethodTerm(formatPercent, new IOperandTerm[] {callnametarget, null, null});
        }

        public static StrForm FromWordToken(StrFormWord wt)
        {
            var ret = new StrForm();
            ret.strs = wt.Strs;
            var termArray = new IOperandTerm[wt.SubWords.Length];
            for (var i = 0; i < wt.SubWords.Length; i++)
            {
                var SWT = wt.SubWords[i];
                var tSymbol = SWT as TripleSymbolSubWord;
                if (tSymbol != null)
                {
                    switch (tSymbol.Code)
                    {
                        case '*':
                            termArray[i] = NameTarget;
                            continue;
                        case '+':
                            termArray[i] = CallnameMaster;
                            continue;
                        case '=':
                            termArray[i] = CallnamePlayer;
                            continue;
                        case '/':
                            termArray[i] = NameAssi;
                            continue;
                        case '$':
                            termArray[i] = CallnameTarget;
                            continue;
                    }
                    throw new ExeEE("何かおかしい");
                }
                WordCollection wc = null;
                IOperandTerm operand = null;
                var yenat = SWT as YenAtSubWord;
                if (yenat != null)
                {
                    wc = yenat.Words;
                    if (wc != null)
                    {
                        operand = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
                        if (!wc.EOL)
                            throw new CodeEE("三項演算子\\@の第一オペランドが異常です");
                    }
                    else
                    {
                        operand = new SingleTerm(0);
                    }
                    IOperandTerm left = new StrFormTerm(FromWordToken(yenat.Left));
                    IOperandTerm right = null;
                    if (yenat.Right == null)
                        right = new SingleTerm("");
                    else
                        right = new StrFormTerm(FromWordToken(yenat.Right));
                    termArray[i] = new FunctionMethodTerm(formatYenAt, new[] {operand, left, right});
                    continue;
                }
                wc = SWT.Words;
                operand = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
                if (operand == null)
                    if (SWT is CurlyBraceSubWord)
                        throw new CodeEE("{}の中に式が存在しません");
                    else
                        throw new CodeEE("%%の中に式が存在しません");
                IOperandTerm second = null;
                SingleTerm third = null;
                wc.ShiftNext();
                if (!wc.EOL)
                {
                    second = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma);

                    wc.ShiftNext();
                    if (!wc.EOL)
                    {
                        var id = wc.Current as IdentifierWord;
                        if (id == null)
                            throw new CodeEE("','の後にRIGHT又はLEFTがありません");
                        if (string.Equals(id.Code, "LEFT", Config.SCVariable)) //標準RIGHT
                            third = new SingleTerm(1);
                        else if (!string.Equals(id.Code, "RIGHT", Config.SCVariable))
                            throw new CodeEE("','の後にRIGHT又はLEFT以外の単語があります");
                        wc.ShiftNext();
                    }
                    if (!wc.EOL)
                        throw new CodeEE("RIGHT又はLEFTの後に余分な文字があります");
                }
                if (SWT is CurlyBraceSubWord)
                {
                    if (operand.GetOperandType() != typeof(long))
                        throw new CodeEE("{}の中の式が数式ではありません");
                    termArray[i] = new FunctionMethodTerm(formatCurlyBrace, new[] {operand, second, third});
                    continue;
                }
                if (operand.GetOperandType() != typeof(string))
                    throw new CodeEE("%%の中の式が文字列式ではありません");
                termArray[i] = new FunctionMethodTerm(formatPercent, new[] {operand, second, third});
            }
            ret.terms = termArray;
            return ret;
        }

        #endregion

        #region FormattedStringMethod 書式付文字列の内部

        private abstract class FormattedStringMethod : FunctionMethod
        {
            public FormattedStringMethod()
            {
                CanRestructure = true;
                ReturnType = typeof(string);
                argumentTypeArray = null;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                throw new ExeEE("型チェックは呼び出し元が行うこと");
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                throw new ExeEE("戻り値の型が違う");
            }

            public override SingleTerm GetReturnValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return new SingleTerm(GetStrValue(exm, arguments));
            }
        }

        private sealed class FormatCurlyBrace : FormattedStringMethod
        {
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm).ToString();
                if (arguments[1] == null)
                    return ret;
                if (arguments[2] != null)
                    ret = ret.PadRight((int) arguments[1].GetIntValue(exm), ' '); //LEFT
                else
                    ret = ret.PadLeft((int) arguments[1].GetIntValue(exm), ' '); //RIGHT
                return ret;
            }
        }

        private sealed class FormatPercent : FormattedStringMethod
        {
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetStrValue(exm);
                if (arguments[1] == null)
                    return ret;
                var totalLength = (int) arguments[1].GetIntValue(exm);
                var currentLength = LangManager.GetStrlenLang(ret);
                totalLength -= currentLength - ret.Length; //全角文字の数だけマイナス。タブ文字？ゼロ幅文字？知るか！
                if (totalLength < ret.Length)
                    return ret; //PadLeftは0未満を送ると例外を投げる
                if (arguments[2] != null)
                    ret = ret.PadRight(totalLength, ' '); //LEFT
                else
                    ret = ret.PadLeft(totalLength, ' '); //RIGHT
                return ret;
            }
        }

        private sealed class FormatYenAt : FormattedStringMethod
        {
//Operator のTernaryIntStrStrとやってることは同じ
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return arguments[0].GetIntValue(exm) != 0
                    ? arguments[1].GetStrValue(exm)
                    : arguments[2].GetStrValue(exm);
            }
        }

        #endregion
    }
}