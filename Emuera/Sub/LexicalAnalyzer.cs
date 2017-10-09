﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Expression;

namespace MinorShift.Emuera.Sub
{
    internal enum LexEndWith
    {
        //いずれにせよEoLで強制終了
        None = 0,
        EoL, //常に最後まで解析
        Operator, //演算子を見つけたら終了。代入式の左辺
        Question, //三項演算子?により終了。\@～～?～～#～～\@
        Percent, //%により終了。%～～%
        RightCurlyBrace, //}により終了。{～～}
        Comma, //,により終了。TIMES第一引数

        //Single,//Identifier一つで終了//1807 Single削除
        GreaterThan //'>'により終了。Htmlタグ解析
    }

    internal enum FormStrEndWith
    {
        //いずれにせよEoLで強制終了
        None = 0,
        EoL, //常に最後まで解析
        DoubleQuotation, //"で終了。@"～～"
        Sharp, //#で終了。\@～～?～～#～～\@　の一つ目
        YenAt, //\@で終了。\@～～?～～#～～\@　の二つ目
        Comma, //,により終了。ANY_FORM引数
        LeftParenthesis_Bracket_Comma_Semicolon //[または(または,または;により終了。CALLFORM系の関数名部分。
    }

    internal enum StrEndWith
    {
        //いずれにせよEoLで強制終了
        None = 0,
        EoL, //常に最後まで解析
        SingleQuotation, //"で終了。'～～'
        DoubleQuotation, //"で終了。"～～"
        Comma, //,により終了。PRINTV'～～,
        LeftParenthesis_Bracket_Comma_Semicolon //[または(または,または;により終了。関数名部分。
    }

    internal enum LexAnalyzeFlag
    {
        None = 0,
        AnalyzePrintV = 1,
        AllowAssignment = 2,
        AllowSingleQuotationStr = 4
    }

    /// <summary>
    ///     1756 TokenReaderより改名
    ///     Lexicalといいつつ構文解析を含む
    /// </summary>
    internal static class LexicalAnalyzer
    {
        private const int MAX_EXPAND_MACRO = 100;

        //readonly static IList<char> operators = new char[] { '+', '-', '*', '/', '%', '=', '!', '<', '>', '|', '&', '^', '~', '?', '#' };
        //readonly static IList<char> whiteSpaces = new char[] { ' ', '　', '\t' };
        //readonly static IList<char> endOfExpression = new char[] { ')', '}', ']', ',', ':' };
        //readonly static IList<char> startOfExpression = new char[] { '(' };
        //readonly static IList<char> stringToken = new char[] { '\"', };
        //readonly static IList<char> stringFormToken = new char[] { '@', };
        //readonly static IList<char> etcSymbol = new char[] { '[', '{', '$', '\\', };
        //readonly static IList<char> decimalDigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', };
        private static readonly IList<char> hexadecimalDigits = new[]
            {'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F'};

        //1819 正規表現使うとやや遅い。いずれdoubleにも対応させたい。そのうち考える
        //readonly static Regex DigitsReg = new Regex("" +
        //	"(" +
        //	"((?<simple>[-]?[0-9]+)([^.xXbBeEpP]|$))" +
        //	"|(" +
        //	"(0(x|X)(?<hex>[0-9a-fA-F]+))|"+
        //	"(0(b|B)(?<bin>[01]+))|"+
        //	"(" + //base10
        //	"(?<integer>[-]?[0-9]*(?<double>[.][0-9])?)" +
        //	"(((p|P)(?<exp2>[0-9]+))|" +
        //	"((e|E)(?<exp10>[0-9]+)))?" +
        //	")"+
        //	"))"
        //	, RegexOptions.Compiled);
        //readonly static Regex idReg = new Regex(@"[^][ \t+*/%=!<>|&^~?#(){},:$\\'""@.;　-]+", RegexOptions.Compiled);
        //public static Int64 ReadInt64(StringStream st, bool retZero)
        //{
        //	Match m = DigitsReg.Match(st.RowString, st.CurrentPosition);
        //	string numstr = m.Groups["simple"].Value;
        //	if (numstr.Length > 0)
        //	{
        //		st.Jump(numstr.Length);
        //		return Convert.ToInt64(numstr, 10);
        //	}
        //	st.Jump(m.Length);
        //	if (m.Groups["bin"].Length > 0)
        //		return Convert.ToInt64(m.Groups["bin"].Value, 2);
        //	if(m.Groups["hex"].Length > 0)
        //		return Convert.ToInt64(m.Groups["hex"].Value, 16);
        //	numstr = m.Groups["number"].Value;
        //	if (numstr.Length > 0)
        //	{
        //		int exp = 0;
        //		string exp2 = m.Groups["exp2"].Value;
        //		string exp10 = m.Groups["exp10"].Value;
        //		if(m.Groups["double"].Length == 0 && exp2.Length == 0 && exp10.Length == 0)
        //		{
        //			return Convert.ToInt64(numstr,10);
        //		}
        //		double d = Convert.ToDouble(numstr);
        //		if (exp2.Length > 0)
        //		{
        //			exp = Convert.ToInt32(exp2, 10);
        //			d = d * Math.Pow(2, exp);
        //		}
        //		else if (exp10.Length > 0)
        //		{
        //			exp = Convert.ToInt32(exp10, 10);
        //			d = d * Math.Pow(10, exp);
        //		}
        //		return ((Int64)(d + 0.49));
        //	}
        //	throw new CodeEE("数字で始まるトークンが適切でありません");
        //}


        public static bool UseMacro = true;

        #region read

        public static long ReadInt64(StringStream st, bool retZero)
        {
            long significand = 0;
            var expBase = 0;
            var exponent = 0;
            var stStartPos = st.CurrentPosition;
            var stEndPos = st.CurrentPosition;
            var fromBase = 10;
            if (st.Current == '0')
            {
                var c = st.Next;
                if (c == 'x' || c == 'X')
                {
                    fromBase = 16;
                    st.ShiftNext();
                    st.ShiftNext();
                }
                else if (c == 'b' || c == 'B')
                {
                    fromBase = 2;
                    st.ShiftNext();
                    st.ShiftNext();
                }
                //8進法は互換性の問題から採用しない。
                //else if (dchar.IsDigit(c))
                //{
                //    fromBase = 8;
                //    st.ShiftNext();
                //}
            }
            if (retZero && st.Current != '+' && st.Current != '-' && !char.IsDigit(st.Current))
                if (fromBase != 16)
                    return 0;
                else if (!hexadecimalDigits.Contains(st.Current))
                    return 0;
            significand = readDigits(st, fromBase);
            if (st.Current == 'p' || st.Current == 'P')
                expBase = 2;
            else if (st.Current == 'e' || st.Current == 'E')
                expBase = 10;
            if (expBase != 0)
            {
                st.ShiftNext();
                unchecked
                {
                    exponent = (int) readDigits(st, fromBase);
                }
            }
            stEndPos = st.CurrentPosition;
            if (expBase != 0 && exponent != 0)
            {
                var d = significand * Math.Pow(expBase, exponent);
                if (double.IsNaN(d) || double.IsInfinity(d) || d > long.MaxValue || d < long.MinValue)
                    throw new CodeEE("\"" + st.Substring(stStartPos, stEndPos) + "\"は64ビット符号付整数の範囲を超えています");
                significand = (long) d;
            }
            return significand;
        }

        //static Regex reg = new Regex(@"[0-9A-Fa-f]+", RegexOptions.Compiled);
        private static long readDigits(StringStream st, int fromBase)
        {
            var start = st.CurrentPosition;
            //1756 正規表現を使ってみたがほぼ変わらなかったので没
            //Match m = reg.Match(st.RowString, st.CurrentPosition);
            //st.Jump(m.Length);
            var c = st.Current;
            if (c == '-' || c == '+')
                st.ShiftNext();
            if (fromBase == 10)
                while (!st.EOS)
                {
                    c = st.Current;
                    if (char.IsDigit(c))
                    {
                        st.ShiftNext();
                        continue;
                    }
                    break;
                }
            else if (fromBase == 16)
                while (!st.EOS)
                {
                    c = st.Current;
                    if (char.IsDigit(c) || hexadecimalDigits.Contains(c))
                    {
                        st.ShiftNext();
                        continue;
                    }
                    break;
                }
            else if (fromBase == 2)
                while (!st.EOS)
                {
                    c = st.Current;
                    if (char.IsDigit(c))
                    {
                        if (c != '0' && c != '1')
                            throw new CodeEE("二進法表記の中で使用できない文字が使われています");
                        st.ShiftNext();
                        continue;
                    }
                    break;
                }
            var strInt = st.Substring(start, st.CurrentPosition - start);
            try
            {
                return Convert.ToInt64(strInt, fromBase);
            }
            catch (FormatException)
            {
                throw new CodeEE("\"" + strInt + "\"は整数値に変換できません");
            }
            catch (OverflowException)
            {
                throw new CodeEE("\"" + strInt + "\"は64ビット符号付き整数の範囲を超えています");
            }
            catch (ArgumentOutOfRangeException)
            {
                if (string.IsNullOrEmpty(strInt))
                    throw new CodeEE("数値として認識できる文字が必要です");
                throw new CodeEE("文字列\"" + strInt + "\"は数値として認識できません");
            }
        }

        /// <summary>
        ///     TIMES第二引数のみが使用する。
        ///     Convertクラスが発行する例外をそのまま投げるので適切に処理すること。
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static double ReadDouble(StringStream st)
        {
            var start = st.CurrentPosition;
            //大雑把に読み込んでエラー処理はConvertクラスに任せる。
            //仮数小数部

            if (st.Current == '-' || st.Current == '+')
                st.ShiftNext();
            while (!st.EOS)
            {
//仮数部
                var c = st.Current;
                if (char.IsDigit(c) || c == '.')
                {
                    st.ShiftNext();
                    continue;
                }
                break;
            }
            if (st.Current == 'e' || st.Current == 'E')
            {
                st.ShiftNext();
                if (st.Current == '-')
                    st.ShiftNext();
                while (!st.EOS)
                {
//指数部
                    var c = st.Current;
                    if (char.IsDigit(c) || c == '.')
                    {
                        st.ShiftNext();
                        continue;
                    }
                    break;
                }
            }
            return Convert.ToDouble(st.Substring(start, st.CurrentPosition - start));
        }

        /// <summary>
        ///     行頭の単語の取得。マクロ展開あり。ただし単語でないマクロ展開はしない。
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static IdentifierWord ReadFirstIdentifierWord(StringStream st)
        {
            var startpos = st.CurrentPosition;
            var str = ReadSingleIdentifier(st);
            if (string.IsNullOrEmpty(str))
                throw new CodeEE("不正な文字で行が始まっています");
            //1808a3 先頭1単語の展開をやめる。－命令の置換を禁止。
            //if (UseMacro)
            //{
            //    int i = 0;
            //    while (true)
            //    {
            //        DefineMacro macro = GlobalStatic.IdentifierDictionary.GetMacro(str);
            //        i++;
            //        if (i > MAX_EXPAND_MACRO)
            //            throw new CodeEE("マクロの展開数が1文あたりの上限を超えました(自己参照・循環参照のおそれ)");
            //        if (macro == null)
            //            break;
            //        //単語（識別子一個）でないマクロが出現したらここでは処理しない
            //        if (macro.IDWord == null)
            //        {
            //            st.CurrentPosition = startpos;
            //            return null;//変数処理に任せる。
            //        }
            //        str = macro.IDWord.Code;
            //    }
            //}
            return new IdentifierWord(str);
        }

        /// <summary>
        ///     単語の取得。マクロ展開あり。関数型マクロ展開なし
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static IdentifierWord ReadSingleIdentifierWord(StringStream st)
        {
            var str = ReadSingleIdentifier(st);
            if (string.IsNullOrEmpty(str))
                return null;
            if (UseMacro)
            {
                var i = 0;
                while (true)
                {
                    var macro = GlobalStatic.IdentifierDictionary.GetMacro(str);
                    i++;
                    if (i > MAX_EXPAND_MACRO)
                        throw new CodeEE("マクロの展開数が1文あたりの上限値" + MAX_EXPAND_MACRO + "を超えました(自己参照・循環参照のおそれ)");
                    if (macro == null)
                        break;
                    if (macro.IDWord != null)
                        throw new CodeEE("マクロ" + macro.Keyword + "はこの文脈では使用できません(1単語に置き換えるマクロのみが使用できます)");
                    str = macro.IDWord.Code;
                }
            }
            return new IdentifierWord(str);
        }

        /// <summary>
        ///     単語を文字列で取得。マクロ適用なし
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static string ReadSingleIdentifier(StringStream st)
        {
            //1819 やや遅い。でもいずれやりたい
            //Match m = idReg.Match(st.RowString, st.CurrentPosition);
            //st.Jump(m.Length);
            //return m.Value;
            var start = st.CurrentPosition;
            while (!st.EOS)
            {
                switch (st.Current)
                {
                    case ' ':
                    case '\t':
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '%':
                    case '=':
                    case '!':
                    case '<':
                    case '>':
                    case '|':
                    case '&':
                    case '^':
                    case '~':
                    case '?':
                    case '#':
                    case ')':
                    case '}':
                    case ']':
                    case ',':
                    case ':':
                    case '(':
                    case '{':
                    case '[':
                    case '$':
                    case '\\':
                    case '\'':
                    case '\"':
                    case '@':
                    case '.':
                    case ';': //コメントに関しては直後に行われるであろうSkipWhiteSpaceなどが対応する。
                        goto end;
                    case '　':
                        if (!Config.SystemAllowFullSpace)
                            throw new CodeEE("予期しない全角スペースを発見しました(この警告はシステムオプション「" +
                                             Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + "」により無視できます)");
                        goto end;
                }
                st.ShiftNext();
            }
            end:
            return st.Substring(start, st.CurrentPosition - start);
        }

        /// <summary>
        ///     endWithが見つかるまで読み込む。始点と終端のチェックは呼び出し側で行うこと。
        ///     エスケープあり。
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static string ReadString(StringStream st, StrEndWith endWith)
        {
            var buffer = new StringBuilder(100);
            while (true)
            {
                switch (st.Current)
                {
                    case '\0':
                        goto end;
                    case '\"':
                        if (endWith == StrEndWith.DoubleQuotation)
                            goto end;
                        break;
                    case '\'':
                        if (endWith == StrEndWith.SingleQuotation)
                            goto end;
                        break;
                    case ',':
                        if (endWith == StrEndWith.Comma ||
                            endWith == StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
                            goto end;
                        break;
                    case '(':
                    case '[':
                    case ';':
                        if (endWith == StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
                            goto end;
                        break;
                    case '\\': //エスケープ処理
                        st.ShiftNext(); //\を読み飛ばす
                        switch (st.Current)
                        {
                            case StringStream.EndOfString:
                                throw new CodeEE("エスケープ文字\\の後に文字がありません");
                            case '\n': break;
                            case 's':
                                buffer.Append(' ');
                                break;
                            case 'S':
                                buffer.Append('　');
                                break;
                            case 't':
                                buffer.Append('\t');
                                break;
                            case 'n':
                                buffer.Append('\n');
                                break;
                            default:
                                buffer.Append(st.Current);
                                break;
                        }
                        st.ShiftNext(); //\の次の文字を読み飛ばす
                        continue;
                }
                buffer.Append(st.Current);
                st.ShiftNext();
            }
            end:
            return buffer.ToString();
        }

        /// <summary>
        ///     失敗したらCodeEE。OperatorManagerには頼らない
        ///     OperatorCode.Assignmentを返すことがある。
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static OperatorCode ReadOperator(StringStream st, bool allowAssignment)
        {
            var cur = st.Current;
            st.ShiftNext();
            var next = st.Current;
            switch (cur)
            {
                case '+':
                    if (next == '+')
                    {
                        st.ShiftNext();
                        return OperatorCode.Increment;
                    }
                    return OperatorCode.Plus;
                case '-':
                    if (next == '-')
                    {
                        st.ShiftNext();
                        return OperatorCode.Decrement;
                    }
                    return OperatorCode.Minus;
                case '*':
                    return OperatorCode.Mult;
                case '/':
                    return OperatorCode.Div;
                case '%':
                    return OperatorCode.Mod;
                case '=':
                    if (next == '=')
                    {
                        st.ShiftNext();
                        return OperatorCode.Equal;
                    }
                    if (allowAssignment)
                        return OperatorCode.Assignment;
                    throw new CodeEE("予期しない代入演算子'='を発見しました(等価比較には'=='を使用してください)");
                case '!':
                    if (next == '=')
                    {
                        st.ShiftNext();
                        return OperatorCode.NotEqual;
                    }
                    else if (next == '&')
                    {
                        st.ShiftNext();
                        return OperatorCode.Nand;
                    }
                    else if (next == '|')
                    {
                        st.ShiftNext();
                        return OperatorCode.Nor;
                    }
                    return OperatorCode.Not;
                case '<':
                    if (next == '=')
                    {
                        st.ShiftNext();
                        return OperatorCode.LessEqual;
                    }
                    else if (next == '<')
                    {
                        st.ShiftNext();
                        return OperatorCode.LeftShift;
                    }
                    return OperatorCode.Less;
                case '>':
                    if (next == '=')
                    {
                        st.ShiftNext();
                        return OperatorCode.GreaterEqual;
                    }
                    else if (next == '>')
                    {
                        st.ShiftNext();
                        return OperatorCode.RightShift;
                    }
                    return OperatorCode.Greater;
                case '|':
                    if (next == '|')
                    {
                        st.ShiftNext();
                        return OperatorCode.Or;
                    }
                    return OperatorCode.BitOr;
                case '&':
                    if (next == '&')
                    {
                        st.ShiftNext();
                        return OperatorCode.And;
                    }
                    return OperatorCode.BitAnd;
                case '^':
                    if (next == '^')
                    {
                        st.ShiftNext();
                        return OperatorCode.Xor;
                    }
                    return OperatorCode.BitXor;
                case '~':
                    return OperatorCode.BitNot;
                case '?':
                    return OperatorCode.Ternary_a;
                case '#':
                    return OperatorCode.Ternary_b;
            }
            throw new CodeEE("'" + cur + "'は演算子として認識できません");
        }

        /// <summary>
        ///     失敗したらCodeEE。OperatorManagerには頼らない
        ///     "="の時、OperatorCode.Assignmentを返す。"=="の時はEqual
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static OperatorCode ReadAssignmentOperator(StringStream st)
        {
            var ret = OperatorCode.NULL;
            var cur = st.Current;
            st.ShiftNext();
            var next = st.Current;
            switch (cur)
            {
                case '+':
                    if (next == '+')
                        ret = OperatorCode.Increment;
                    else if (next == '=')
                        ret = OperatorCode.Plus;
                    break;
                case '-':
                    if (next == '-')
                        ret = OperatorCode.Decrement;
                    else if (next == '=')
                        ret = OperatorCode.Minus;
                    break;
                case '*':
                    if (next == '=')
                        ret = OperatorCode.Mult;
                    break;
                case '/':
                    if (next == '=')
                        ret = OperatorCode.Div;
                    break;
                case '%':
                    if (next == '=')
                        ret = OperatorCode.Mod;
                    break;
                case '=':
                    if (next == '=')
                    {
                        ret = OperatorCode.Equal;
                        break;
                    }
                    return OperatorCode.Assignment;
                case '\'':
                    if (next == '=')
                    {
                        ret = OperatorCode.AssignmentStr;
                        break;
                    }
                    throw new CodeEE("\"\'\"は代入演算子として認識できません");
                case '<':
                    if (next == '<')
                    {
                        st.ShiftNext();
                        if (st.Current == '=')
                        {
                            ret = OperatorCode.LeftShift;
                            break;
                        }
                        throw new CodeEE("'<'は代入演算子として認識できません");
                    }
                    break;
                case '>':
                    if (next == '>')
                    {
                        st.ShiftNext();
                        if (st.Current == '=')
                        {
                            ret = OperatorCode.RightShift;
                            break;
                        }
                        throw new CodeEE("'>'は代入演算子として認識できません");
                    }
                    break;
                case '|':
                    if (next == '=')
                        ret = OperatorCode.BitOr;
                    break;
                case '&':
                    if (next == '=')
                        ret = OperatorCode.BitAnd;
                    break;
                case '^':
                    if (next == '=')
                        ret = OperatorCode.BitXor;
                    break;
            }
            if (ret == OperatorCode.NULL)
                throw new CodeEE("'" + cur + "'は代入演算子として認識できません");
            st.ShiftNext();
            return ret;
        }


        /// <summary>
        ///     Consoleの文字表示用。字句解析や構文解析に使ってはならない
        /// </summary>
        public static int SkipAllSpace(StringStream st)
        {
            var count = 0;
            while (true)
            {
                switch (st.Current)
                {
                    case ' ':
                    case '\t':
                    case '　':
                        count++;
                        st.ShiftNext();
                        continue;
                }
                return count;
            }
        }

        public static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '　';
        }

        /// <summary>
        ///     字句解析・構文解析用。ホワイトスペースの他、コメントも飛ばす。
        /// </summary>
        public static int SkipWhiteSpace(StringStream st)
        {
            var count = 0;
            while (true)
            {
                switch (st.Current)
                {
                    case ' ':
                    case '\t':
                        count++;
                        st.ShiftNext();
                        continue;
                    case '　':
                        if (!Config.SystemAllowFullSpace)
                            return count;
                        goto case ' ';
                    case ';':
                        if (st.CurrentEqualTo(";#;") && Program.DebugMode)
                        {
                            st.Jump(3);
                            continue;
                        }
                        else if (st.CurrentEqualTo(";!;"))
                        {
                            st.Jump(3);
                            continue;
                        }
                        st.Seek(0, SeekOrigin.End);
                        return count;
                }
                return count;
            }
        }

        /// <summary>
        ///     字句解析・構文解析用。文字列直前の半角スペースを飛ばす。性質上、半角スペースのみを見る。
        /// </summary>
        public static int SkipHalfSpace(StringStream st)
        {
            var count = 0;
            while (st.Current == ' ')
            {
                count++;
                st.ShiftNext();
            }
            return count;
        }

        #endregion

        #region analyse

        /// <summary>
        ///     解析できるものは関数宣言や式のみ。FORM文字列や普通の文字列を送ってはいけない
        ///     return時にはendWithの文字がCurrentになっているはず。終端の適切さの検証は呼び出し元が行う。
        /// </summary>
        /// <returns></returns>
        public static WordCollection Analyse(StringStream st, LexEndWith endWith, LexAnalyzeFlag flag)
        {
            var ret = new WordCollection();
            var nestBracketS = 0;
            //int nestBracketM = 0;
            var nestBracketL = 0;
            while (true)
                switch (st.Current)
                {
                    case '\n':
                    case '\0':
                        goto end;
                    case ' ':
                    case '\t':
                        st.ShiftNext();
                        continue;
                    case '　':
                        if (!Config.SystemAllowFullSpace)
                            throw new CodeEE("字句解析中に予期しない全角スペースを発見しました(この警告はシステムオプション「" +
                                             Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + "」により無視できます)");
                        st.ShiftNext();
                        continue;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        ret.Add(new LiteralIntegerWord(ReadInt64(st, false)));
                        break;
                    case '>':
                        if (endWith == LexEndWith.GreaterThan)
                            goto end;
                        goto case '+';
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '%':
                    case '=':
                    case '!':
                    case '<':
                    case '|':
                    case '&':
                    case '^':
                    case '~':
                    case '?':
                    case '#':
                        if (nestBracketS == 0 && nestBracketL == 0)
                            if (endWith == LexEndWith.Operator)
                                goto end; //代入演算子のはずである。呼び出し元がチェックするはず
                            else if (endWith == LexEndWith.Percent && st.Current == '%')
                                goto end;
                            else if (endWith == LexEndWith.Question && st.Current == '?')
                                goto end;
                        ret.Add(new OperatorWord(ReadOperator(st,
                            (flag & LexAnalyzeFlag.AllowAssignment) == LexAnalyzeFlag.AllowAssignment)));
                        break;
                    case ')':
                        ret.Add(new SymbolWord(')'));
                        nestBracketS--;
                        st.ShiftNext();
                        continue;
                    case ']':
                        ret.Add(new SymbolWord(']'));
                        nestBracketL--;
                        st.ShiftNext();
                        continue;
                    case '(':
                        ret.Add(new SymbolWord('('));
                        nestBracketS++;
                        st.ShiftNext();
                        continue;
                    case '[':
                        if (st.Next == '[')
                        {
                            //throw new CodeEE("字句解析中に予期しない文字'[['を発見しました");
                            ////1808alpha006 rename処理変更
                            //1808beta009 ここだけ戻す
                            //現在の処理だとここに来た時点でrename失敗確定だが警告内容を元に戻すため
                            if (ParserMediator.RenameDic == null)
                                throw new CodeEE("字句解析中に予期しない文字\"[[\"を発見しました");
                            var start = st.CurrentPosition;
                            var find = st.Find("]]");
                            if (find <= 2)
                                if (find == 2)
                                    throw new CodeEE("空の[[]]です");
                                else
                                    throw new CodeEE("対応する\"]]\"のない\"[[\"です");
                            var key = st.Substring(start, find + 2);
                            //1810 ここまでで置換できなかったものは強制エラーにする
                            //行連結前に置換不能で行連結より置換することができるようになったものまで置換されていたため
                            throw new CodeEE("字句解析中に置換(rename)できない符号" + key + "を発見しました");
                            //string value = null;
                            //if (!ParserMediator.RenameDic.TryGetValue(key, out value))
                            //    throw new CodeEE("字句解析中に置換(rename)できない符号" + key + "を発見しました");
                            //st.Replace(start, find + 2, value);
                            //continue;//その場から再度解析スタート
                        }
                        ret.Add(new SymbolWord('['));
                        nestBracketL++;
                        st.ShiftNext();
                        continue;
                    case ':':
                        ret.Add(new SymbolWord(':'));
                        st.ShiftNext();
                        continue;
                    case ',':
                        if (endWith == LexEndWith.Comma && nestBracketS == 0) // && (nestBracketL == 0))
                            goto end;
                        ret.Add(new SymbolWord(','));
                        st.ShiftNext();
                        continue;
                    //case '}': ret.Add(new SymbolWT('}')); nestBracketM--; continue;
                    //case '{': ret.Add(new SymbolWT('{')); nestBracketM++; continue;
                    case '\'':
                        if ((flag & LexAnalyzeFlag.AllowSingleQuotationStr) == LexAnalyzeFlag.AllowSingleQuotationStr)
                        {
                            st.ShiftNext();
                            ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.SingleQuotation)));
                            if (st.Current != '\'')
                                throw new CodeEE("\'が閉じられていません");
                            st.ShiftNext();
                            break;
                        }
                        if ((flag & LexAnalyzeFlag.AnalyzePrintV) != LexAnalyzeFlag.AnalyzePrintV)
                        {
                            //AssignmentStr用特殊処理 代入文の代入演算子を探索中で'=の場合のみ許可
                            if (endWith == LexEndWith.Operator && nestBracketS == 0 && nestBracketL == 0 &&
                                st.Next == '=')
                                goto end;
                            throw new CodeEE("字句解析中に予期しない文字'" + st.Current + "'を発見しました");
                        }
                        st.ShiftNext();
                        ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.Comma)));
                        if (st.Current == ',')
                            goto case ','; //続きがあるなら,の処理へ。それ以外は行終端のはず
                        goto end;
                    case '}':
                        if (endWith == LexEndWith.RightCurlyBrace)
                            goto end;
                        throw new CodeEE("字句解析中に予期しない文字'" + st.Current + "'を発見しました");
                    case '\"':
                        st.ShiftNext();
                        ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.DoubleQuotation)));
                        if (st.Current != '\"')
                            throw new CodeEE("\"が閉じられていません");
                        st.ShiftNext();
                        break;
                    case '@':
                        if (st.Next != '\"')
                        {
                            ret.Add(new SymbolWord('@'));
                            st.ShiftNext();
                            continue;
                        }
                        st.ShiftNext();
                        st.ShiftNext();
                        ret.Add(AnalyseFormattedString(st, FormStrEndWith.DoubleQuotation, false));
                        if (st.Current != '\"')
                            throw new CodeEE("\"が閉じられていません");
                        st.ShiftNext();
                        break;
                    case '.':
                        ret.Add(new SymbolWord('.'));
                        st.ShiftNext();
                        continue;

                    case '\\':
                        if (st.Next != '@')
                            throw new CodeEE("字句解析中に予期しない文字'" + st.Current + "'を発見しました");
                    {
                        st.Jump(2);
                        ret.Add(new StrFormWord(new[] {"", ""}, new SubWord[] {AnalyseYenAt(st)}));
                    }
                        break;
                    case '{':
                    case '$':
                        throw new CodeEE("字句解析中に予期しない文字'" + st.Current + "'を発見しました");
                    case ';': //1807 行中コメント
                        if (st.CurrentEqualTo(";#;") && Program.DebugMode)
                        {
                            st.Jump(3);
                            break;
                        }
                        else if (st.CurrentEqualTo(";!;"))
                        {
                            st.Jump(3);
                            break;
                        }
                        st.Seek(0, SeekOrigin.End);
                        goto end;
                    default:
                    {
                        ret.Add(new IdentifierWord(ReadSingleIdentifier(st)));
                        break;
                    }
                }
            end:
            if (nestBracketS != 0 || nestBracketL != 0)
            {
                if (nestBracketS < 0)
                    throw new CodeEE("字句解析中に対応する'('のない')'を発見しました");
                if (nestBracketS > 0)
                    throw new CodeEE("字句解析中に対応する')'のない'('を発見しました");
                if (nestBracketL < 0)
                    throw new CodeEE("字句解析中に対応する'['のない']'を発見しました");
                if (nestBracketL > 0)
                    throw new CodeEE("字句解析中に対応する']'のない'['を発見しました");
            }
            if (UseMacro)
                return expandMacro(ret);
            return ret;
        }

        private static WordCollection expandMacro(WordCollection wc)
        {
            //マクロ展開
            wc.Pointer = 0;
            var count = 0;
            while (!wc.EOL)
            {
                var word = wc.Current as IdentifierWord;
                if (word == null)
                {
                    wc.ShiftNext();
                    continue;
                }
                var idStr = word.Code;
                var macro = GlobalStatic.IdentifierDictionary.GetMacro(idStr);
                if (macro == null)
                {
                    wc.ShiftNext();
                    continue;
                }
                count++;
                if (count > MAX_EXPAND_MACRO)
                    throw new CodeEE("マクロの展開数が1文あたりの上限" + MAX_EXPAND_MACRO + "を超えました(自己参照・循環参照のおそれ)");
                if (!macro.HasArguments)
                {
                    wc.Remove();
                    wc.InsertRange(macro.Statement);
                    continue;
                }
                //関数型マクロ
                wc = expandFunctionlikeMacro(macro, wc);
            }
            wc.Pointer = 0;
            return wc;
        }

        private static WordCollection expandFunctionlikeMacro(DefineMacro macro, WordCollection wc)
        {
            var macroStart = wc.Pointer;
            wc.ShiftNext();
            var symbol = wc.Current as SymbolWord;
            if (symbol == null || symbol.Type != '(')
                throw new CodeEE("関数形式のマクロ" + macro.Keyword + "に引数がありません");
            var macroWC = macro.Statement.Clone();
            var args = new WordCollection[macro.ArgCount];
            //引数部読み取りループ
            for (var i = 0; i < macro.ArgCount; i++)
            {
                var macroNestBracketS = 0;
                args[i] = new WordCollection();
                while (true)
                {
                    wc.ShiftNext();
                    if (wc.EOL)
                        throw new CodeEE("関数形式のマクロ" + macro.Keyword + "の用法が正しくありません");
                    symbol = wc.Current as SymbolWord;
                    if (symbol == null)
                    {
                        args[i].Add(wc.Current);
                        continue;
                    }
                    switch (symbol.Type)
                    {
                        case '(':
                            macroNestBracketS++;
                            break;
                        case ')':
                            if (macroNestBracketS > 0)
                            {
                                macroNestBracketS--;
                                break;
                            }
                            if (i != macro.ArgCount - 1)
                                throw new CodeEE("関数形式のマクロ" + macro.Keyword + "の引数の数が正しくありません");
                            goto exitfor;
                        case ',':
                            if (macroNestBracketS == 0)
                                goto exitwhile;
                            break;
                    }
                    args[i].Add(wc.Current);
                }
                exitwhile:
                if (args[i].Collection.Count == 0)
                    throw new CodeEE("関数形式のマクロ" + macro.Keyword + "の引数を省略することはできません");
            }
            //引数部読み取りループ終端
            exitfor:
            symbol = wc.Current as SymbolWord;
            if (symbol == null || symbol.Type != ')')
                throw new CodeEE("関数形式のマクロ" + macro.Keyword + "の用法が正しくありません");
            var macroLength = wc.Pointer - macroStart + 1;
            wc.Pointer = macroStart;
            for (var j = 0; j < macroLength; j++)
                wc.Collection.RemoveAt(macroStart);
            while (!macroWC.EOL)
            {
                var w = macroWC.Current as MacroWord;
                if (w == null)
                {
                    macroWC.ShiftNext();
                    continue;
                }
                macroWC.Remove();
                macroWC.InsertRange(args[w.Number]);
                macroWC.Pointer += args[w.Number].Collection.Count;
            }
            wc.InsertRange(macroWC);
            wc.Pointer = macroStart;
            return wc;
        }

        /// <summary>
        ///     @"などの直後からの開始
        ///     return時にはendWithの文字がCurrentになっているはず。終端の適切さの検証は呼び出し元が行う。
        /// </summary>
        /// <returns></returns>
        public static StrFormWord AnalyseFormattedString(StringStream st, FormStrEndWith endWith, bool trim)
        {
            var strs = new List<string>();
            var SWTs = new List<SubWord>();
            var buffer = new StringBuilder(100);
            while (true)
            {
                var cur = st.Current;
                switch (cur)
                {
                    case '\n':
                    case '\0':
                        goto end;
                    case '\"':
                        if (endWith == FormStrEndWith.DoubleQuotation)
                            goto end;
                        buffer.Append(cur);
                        break;
                    case '#':
                        if (endWith == FormStrEndWith.Sharp)
                            goto end;
                        buffer.Append(cur);
                        break;
                    case ',':
                        if (endWith == FormStrEndWith.Comma ||
                            endWith == FormStrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
                            goto end;
                        buffer.Append(cur);
                        break;
                    case '(':
                    case '[':
                    case ';':
                        if (endWith == FormStrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
                            goto end;
                        buffer.Append(cur);
                        break;
                    case '%':
                        strs.Add(buffer.ToString());
                        buffer.Remove(0, buffer.Length);
                        st.ShiftNext();
                        SWTs.Add(new PercentSubWord(Analyse(st, LexEndWith.Percent, LexAnalyzeFlag.None)));
                        if (st.Current != '%')
                            throw new CodeEE("\'%\'が使われましたが対応する\'%\'が見つかりません");
                        break;
                    case '{':
                        strs.Add(buffer.ToString());
                        buffer.Remove(0, buffer.Length);
                        st.ShiftNext();
                        SWTs.Add(new CurlyBraceSubWord(Analyse(st, LexEndWith.RightCurlyBrace, LexAnalyzeFlag.None)));
                        if (st.Current != '}')
                            throw new CodeEE("\'{\'が使われましたが対応する\'}\'が見つかりません");
                        break;
                    case '*':
                    case '+':
                    case '=':
                    case '/':
                    case '$':
                        if (!Config.SystemIgnoreTripleSymbol && st.TripleSymbol())
                        {
                            strs.Add(buffer.ToString());
                            buffer.Remove(0, buffer.Length);
                            st.Jump(3);
                            SWTs.Add(new TripleSymbolSubWord(cur));
                            continue;
                        }
                        else
                        {
                            buffer.Append(cur);
                        }
                        break;
                    case '\\': //エスケープ文字の使用

                        st.ShiftNext();
                        cur = st.Current;
                        switch (cur)
                        {
                            case '\0':
                                throw new CodeEE("エスケープ文字\\の後に文字がありません");
                            case '\n': break;
                            case 's':
                                buffer.Append(' ');
                                break;
                            case 'S':
                                buffer.Append('　');
                                break;
                            case 't':
                                buffer.Append('\t');
                                break;
                            case 'n':
                                buffer.Append('\n');
                                break;
                            case '@': //\@～～?～～#～～\@
                            {
                                if (endWith == FormStrEndWith.YenAt || endWith == FormStrEndWith.Sharp)
                                    goto end;
                                strs.Add(buffer.ToString());
                                buffer.Remove(0, buffer.Length);
                                st.ShiftNext();
                                SWTs.Add(AnalyseYenAt(st));
                                continue;
                            }
                            default:
                                buffer.Append(cur);
                                st.ShiftNext();
                                continue;
                        }
                        break;
                    default:
                        buffer.Append(cur);
                        break;
                }
                st.ShiftNext();
            }
            end:
            strs.Add(buffer.ToString());

            var retStr = new string[strs.Count];
            var retSWTs = new SubWord[SWTs.Count];
            strs.CopyTo(retStr);
            SWTs.CopyTo(retSWTs);
            if (trim && retStr.Length > 0)
            {
                retStr[0] = retStr[0].TrimStart(' ', '\t');
                retStr[retStr.Length - 1] = retStr[retStr.Length - 1].TrimEnd(' ', '\t');
            }
            return new StrFormWord(retStr, retSWTs);
        }


        /// <summary>
        ///     \@直後からの開始、\@の直後がCurrentになる
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static YenAtSubWord AnalyseYenAt(StringStream st)
        {
            var w = Analyse(st, LexEndWith.Question, LexAnalyzeFlag.None);
            if (st.Current != '?')
                throw new CodeEE("\'\\@\'が使われましたが対応する\'?\'が見つかりません");
            st.ShiftNext();
            var left = AnalyseFormattedString(st, FormStrEndWith.Sharp, true);
            if (st.Current != '#')
            {
                if (st.Current != '@')
                    throw new CodeEE("\'\\@\',\'?\'が使われましたが対応する\'#\'が見つかりません");
                st.ShiftNext();
                ParserMediator.Warn("\'\\@\',\'?\'が使われましたが対応する\'#\'が見つかりません", GlobalStatic.Process.GetScaningLine(), 1,
                    false, false);
                return new YenAtSubWord(w, left, null);
            }
            st.ShiftNext();
            var right = AnalyseFormattedString(st, FormStrEndWith.YenAt, true);
            if (st.Current != '@')
                throw new CodeEE("\'\\@\',\'?\',\'#\'が使われましたが対応する\'\\@\'が見つかりません");
            st.ShiftNext();
            return new YenAtSubWord(w, left, right);
        }

        #endregion
    }
}