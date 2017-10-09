﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;
using MinorShift._Library;

namespace MinorShift.Emuera.GameData.Function
{
    internal static partial class FunctionMethodCreator
    {
        #region CSVデータ関係

        private sealed class GetcharaMethod : FunctionMethod
        {
            public GetcharaMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常２つ、１つ省略可能で１～２の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";

                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(long))
                    return name + "関数の1番目の引数の型が正しくありません";
                //2は省略可能
                if (arguments.Length == 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var integer = arguments[0].GetIntValue(exm);
                var chara = -1L;

                if (!Config.CompatiSPChara)
                    return exm.VEvaluator.GetChara(integer);
                //以下互換性用の旧処理
                var CheckSp = false;
                if (arguments.Length > 1 && arguments[1] != null && arguments[1].GetIntValue(exm) != 0)
                    CheckSp = true;
                if (CheckSp)
                {
                    chara = exm.VEvaluator.GetChara_UseSp(integer, false);
                    if (chara != -1)
                        return chara;
                    return exm.VEvaluator.GetChara_UseSp(integer, true);
                }
                return exm.VEvaluator.GetChara_UseSp(integer, false);
            }
        }

        private sealed class GetspcharaMethod : FunctionMethod
        {
            public GetspcharaMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (!Config.CompatiSPChara)
                    throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
                var integer = arguments[0].GetIntValue(exm);
                return exm.VEvaluator.GetChara_UseSp(integer, true);
            }
        }

        private sealed class CsvStrDataMethod : FunctionMethod
        {
            private readonly CharacterStrData charaStr;

            public CsvStrDataMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                charaStr = CharacterStrData.NAME;
                CanRestructure = true;
            }

            public CsvStrDataMethod(CharacterStrData cStr)
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                charaStr = cStr;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!arguments[0].IsInteger)
                    return name + "関数の1番目の引数が数値ではありません";
                if (arguments.Length == 1)
                    return null;
                if (arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var x = arguments[0].GetIntValue(exm);
                var y = arguments.Length > 1 && arguments[1] != null ? arguments[1].GetIntValue(exm) : 0;
                if (!Config.CompatiSPChara && y != 0)
                    throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
                return exm.VEvaluator.GetCharacterStrfromCSVData(x, charaStr, y != 0, 0);
            }
        }

        private sealed class CsvcstrMethod : FunctionMethod
        {
            public CsvcstrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!arguments[0].IsInteger)
                    return name + "関数の1番目の引数が数値ではありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                if (arguments.Length == 2)
                    return null;
                if (arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の変数が数値ではありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var x = arguments[0].GetIntValue(exm);
                var y = arguments[1].GetIntValue(exm);
                var z = arguments.Length == 3 && arguments[2] != null ? arguments[2].GetIntValue(exm) : 0;
                if (!Config.CompatiSPChara && z != 0)
                    throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
                return exm.VEvaluator.GetCharacterStrfromCSVData(x, CharacterStrData.CSTR, z != 0, y);
            }
        }

        private sealed class CsvDataMethod : FunctionMethod
        {
            private readonly CharacterIntData charaInt;

            public CsvDataMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                charaInt = CharacterIntData.BASE;
                CanRestructure = true;
            }

            public CsvDataMethod(CharacterIntData cInt)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                charaInt = cInt;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!arguments[0].IsInteger)
                    return name + "関数の1番目の引数が数値ではありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                if (arguments.Length == 2)
                    return null;
                if (arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の変数が数値ではありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var x = arguments[0].GetIntValue(exm);
                var y = arguments[1].GetIntValue(exm);
                var z = arguments.Length == 3 && arguments[2] != null ? arguments[2].GetIntValue(exm) : 0;
                if (!Config.CompatiSPChara && z != 0)
                    throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
                return exm.VEvaluator.GetCharacterIntfromCSVData(x, charaInt, z != 0, y);
            }
        }

        private sealed class FindcharaMethod : FunctionMethod
        {
            private readonly bool isLast;

            public FindcharaMethod(bool last)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
                isLast = last;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常3つ、1つ省略可能で2～3の引数が必要。
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 4)
                    return name + "関数の引数が多すぎます";

                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (!((VariableTerm) arguments[0]).Identifier.IsCharacterData)
                    return name + "関数の1番目の引数の変数がキャラクタ変数ではありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
                    return name + "関数の2番目の引数の型が正しくありません";
                //3番目は省略可能
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                //4番目は省略可能
                if (arguments.Length >= 4 && arguments[3] != null && arguments[3].GetOperandType() != typeof(long))
                    return name + "関数の4番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var vTerm = (VariableTerm) arguments[0];
                var varID = vTerm.Identifier;

                long elem = 0;
                if (vTerm.Identifier.IsArray1D)
                {
                    elem = vTerm.GetElementInt(1, exm);
                }
                else if (vTerm.Identifier.IsArray2D)
                {
                    elem = vTerm.GetElementInt(1, exm) << 32;
                    elem += vTerm.GetElementInt(2, exm);
                }
                long startindex = 0;
                var lastindex = exm.VEvaluator.CHARANUM;
                if (arguments.Length >= 3 && arguments[2] != null)
                    startindex = arguments[2].GetIntValue(exm);
                if (arguments.Length >= 4 && arguments[3] != null)
                    lastindex = arguments[3].GetIntValue(exm);
                long ret = -1;
                if (startindex < 0 || startindex >= exm.VEvaluator.CHARANUM)
                    throw new CodeEE((isLast ? "" : "") + "関数の第3引数(" + startindex + ")はキャラクタ位置の範囲外です");
                if (lastindex < 0 || lastindex > exm.VEvaluator.CHARANUM)
                    throw new CodeEE((isLast ? "" : "") + "関数の第4引数(" + lastindex + ")はキャラクタ位置の範囲外です");
                if (varID.IsString)
                {
                    var word = arguments[1].GetStrValue(exm);
                    ret = exm.VEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
                }
                else
                {
                    var word = arguments[1].GetIntValue(exm);
                    ret = exm.VEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
                }
                return ret;
            }
        }

        private sealed class ExistCsvMethod : FunctionMethod
        {
            public ExistCsvMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!arguments[0].IsInteger)
                    return name + "関数の1番目の引数が数値ではありません";
                if (arguments.Length == 1)
                    return null;
                if (arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var no = arguments[0].GetIntValue(exm);
                var isSp = arguments.Length == 2 && arguments[1] != null ? arguments[1].GetIntValue(exm) != 0 : false;
                if (!Config.CompatiSPChara && isSp)
                    throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");

                return exm.VEvaluator.ExistCsv(no, isSp);
            }
        }

        #endregion

        #region 汎用処理系

        private sealed class VarsizeMethod : FunctionMethod
        {
            public VarsizeMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = true;
                //1808beta009 参照型変数の追加によりちょっと面倒になった
                HasUniqueRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!arguments[0].IsString)
                    return name + "関数の1番目の引数が文字列ではありません";
                if (arguments[0] is SingleTerm)
                {
                    var varName = ((SingleTerm) arguments[0]).Str;
                    if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
                        return name + "関数の1番目の引数が変数名ではありません";
                }
                if (arguments.Length == 1)
                    return null;
                if (arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                if (arguments.Length == 2)
                    return null;
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
                if (var == null)
                    throw new CodeEE("VARSIZEの1番目の引数(\"" + arguments[0].GetStrValue(exm) + "\")が変数名ではありません");
                var dim = 0;
                if (arguments.Length == 2 && arguments[1] != null)
                    dim = (int) arguments[1].GetIntValue(exm);
                return var.GetLength(dim);
            }

            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[0].Restructure(exm);
                if (arguments.Length > 1)
                    arguments[1].Restructure(exm);
                if (arguments[0] is SingleTerm && (arguments.Length == 1 || arguments[1] is SingleTerm))
                {
                    var var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null,
                        true);
                    if (var == null || var.IsReference) //可変長の場合は定数化できない
                        return false;
                    return true;
                }
                return false;
            }
        }

        private sealed class CheckfontMethod : FunctionMethod
        {
            public CheckfontMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true; //起動中に変わることもそうそうないはず……
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                var ifc = new InstalledFontCollection();
                long isInstalled = 0;
                foreach (var ff in ifc.Families)
                    if (ff.Name == str)
                    {
                        isInstalled = 1;
                        break;
                    }
                return isInstalled;
            }
        }

        private sealed class CheckdataMethod : FunctionMethod
        {
            private readonly string name;
            private readonly EraSaveFileType type;

            public CheckdataMethod(string name, EraSaveFileType type)
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = false;
                this.name = name;
                this.type = type;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var target = arguments[0].GetIntValue(exm);
                if (target < 0)
                    throw new CodeEE(name + "の引数に負の値(" + target + ")が指定されました");
                if (target > int.MaxValue)
                    throw new CodeEE(name + "の引数(" + target + ")が大きすぎます");
                EraDataResult result = null;
                result = exm.VEvaluator.CheckData((int) target, type);
                exm.VEvaluator.RESULTS = result.DataMes;
                return (long) result.State;
            }
        }

        /// <summary>
        ///     ファイル名をstringで指定する版・CHKVARDATAとCHKCHARADATAはこっちに分類
        /// </summary>
        private sealed class CheckdataStrMethod : FunctionMethod
        {
            private string name;
            private readonly EraSaveFileType type;

            public CheckdataStrMethod(string name, EraSaveFileType type)
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = false;
                this.name = name;
                this.type = type;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var datFilename = arguments[0].GetStrValue(exm);
                EraDataResult result = null;
                result = exm.VEvaluator.CheckData(datFilename, type);
                exm.VEvaluator.RESULTS = result.DataMes;
                return (long) result.State;
            }
        }

        /// <summary>
        ///     ファイル探索関数
        /// </summary>
        private sealed class FindFilesMethod : FunctionMethod
        {
            private string name;
            private readonly EraSaveFileType type;

            public FindFilesMethod(string name, EraSaveFileType type)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
                this.name = name;
                this.type = type;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length > 1)
                    return name + "関数の引数が多すぎます";
                if (arguments.Length == 0 || arguments[0] == null)
                    return null;
                if (!arguments[0].IsString)
                    return name + "関数の1番目の引数が文字列ではありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var pattern = "*";
                if (arguments.Length > 0 && arguments[0] != null)
                    pattern = arguments[0].GetStrValue(exm);
                List<string> filepathes = null;
                filepathes = exm.VEvaluator.GetDatFiles(type == EraSaveFileType.CharVar, pattern);
                var results =
                    exm.VEvaluator.VariableData.DataStringArray[
                        (int) (VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
                if (filepathes.Count <= results.Length)
                    filepathes.CopyTo(results);
                else
                    filepathes.CopyTo(0, results, 0, results.Length);
                return filepathes.Count;
            }
        }


        private sealed class IsSkipMethod : FunctionMethod
        {
            public IsSkipMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return exm.Process.SkipPrint ? 1L : 0L;
            }
        }

        private sealed class MesSkipMethod : FunctionMethod
        {
            private readonly bool warn;

            public MesSkipMethod(bool warn)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
                this.warn = warn;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length > 0)
                    return name + "関数の引数が多すぎます";
                if (warn)
                    ParserMediator.Warn("関数MOUSESKIP()は推奨されません。代わりに関数MESSKIP()を使用してください",
                        GlobalStatic.Process.GetScaningLine(), 1, false, false, null);
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return GlobalStatic.Console.MesSkip ? 1L : 0L;
            }
        }


        private sealed class GetColorMethod : FunctionMethod
        {
            private readonly bool defaultColor;

            public GetColorMethod(bool isDef)
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = isDef;
                defaultColor = isDef;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var color = defaultColor ? Config.ForeColor : GlobalStatic.Console.StringStyle.Color;
                return color.ToArgb() & 0xFFFFFF;
            }
        }

        private sealed class GetFocusColorMethod : FunctionMethod
        {
            public GetFocusColorMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Config.FocusColor.ToArgb() & 0xFFFFFF;
            }
        }

        private sealed class GetBGColorMethod : FunctionMethod
        {
            private readonly bool defaultColor;

            public GetBGColorMethod(bool isDef)
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = isDef;
                defaultColor = isDef;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var color = defaultColor ? Config.BackColor : GlobalStatic.Console.bgColor;
                return color.ToArgb() & 0xFFFFFF;
            }
        }

        private sealed class GetStyleMethod : FunctionMethod
        {
            public GetStyleMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var fontstyle = GlobalStatic.Console.StringStyle.FontStyle;
                long ret = 0;
                if ((fontstyle & FontStyle.Bold) == FontStyle.Bold)
                    ret |= 1;
                if ((fontstyle & FontStyle.Italic) == FontStyle.Italic)
                    ret |= 2;
                if ((fontstyle & FontStyle.Strikeout) == FontStyle.Strikeout)
                    ret |= 4;
                if ((fontstyle & FontStyle.Underline) == FontStyle.Underline)
                    ret |= 8;
                return ret;
            }
        }

        private sealed class GetFontMethod : FunctionMethod
        {
            public GetFontMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return GlobalStatic.Console.StringStyle.Fontname;
            }
        }

        private sealed class BarStringMethod : FunctionMethod
        {
            public BarStringMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(long), typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var var = arguments[0].GetIntValue(exm);
                var max = arguments[1].GetIntValue(exm);
                var length = arguments[2].GetIntValue(exm);
                return exm.CreateBar(var, max, length);
            }
        }

        private sealed class CurrentAlignMethod : FunctionMethod
        {
            public CurrentAlignMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (exm.Console.Alignment == DisplayLineAlignment.LEFT)
                    return "LEFT";
                if (exm.Console.Alignment == DisplayLineAlignment.CENTER)
                    return "CENTER";
                return "RIGHT";
            }
        }

        private sealed class CurrentRedrawMethod : FunctionMethod
        {
            public CurrentRedrawMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return exm.Console.Redraw == ConsoleRedraw.None ? 0L : 1L;
            }
        }

        private sealed class ColorFromNameMethod : FunctionMethod
        {
            public ColorFromNameMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var colorName = arguments[0].GetStrValue(exm);
                var color = Color.FromName(colorName);
                var i = 0;
                if (color.A > 0)
                {
                    i = (color.R << 16) + (color.G << 8) + color.B;
                }
                else
                {
                    if (colorName.Equals("transparent", StringComparison.OrdinalIgnoreCase))
                        throw new CodeEE("無色透明(Transparent)は色として指定できません");
                    //throw new CodeEE("指定された色名\"" + colorName + "\"は無効な色名です");
                    i = -1;
                }
                return i;
            }
        }

        private sealed class ColorFromRGBMethod : FunctionMethod
        {
            public ColorFromRGBMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var r = arguments[0].GetIntValue(exm);
                if (r < 0 || r > 255)
                    throw new CodeEE("第１引数が0から255の範囲外です");
                var g = arguments[1].GetIntValue(exm);
                if (g < 0 || g > 255)
                    throw new CodeEE("第２引数が0から255の範囲外です");
                var b = arguments[2].GetIntValue(exm);
                if (b < 0 || b > 255)
                    throw new CodeEE("第３引数が0から255の範囲外です");
                return (r << 16) + (g << 8) + b;
            }
        }

        /// <summary>
        ///     1810 作ったけど保留
        /// </summary>
        private sealed class GetRefMethod : FunctionMethod
        {
            public GetRefMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 1)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is UserDefinedRefMethodNoArgTerm))
                    return name + "関数の1番目の引数が関数参照ではありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return ((UserDefinedRefMethodNoArgTerm) arguments[0]).GetRefName();
            }
        }

        #endregion

        #region 定数取得

        private sealed class MoneyStrMethod : FunctionMethod
        {
            public MoneyStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常2つ、1つ省略可能で1～2の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(long))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(string))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var money = arguments[0].GetIntValue(exm);
                if (arguments.Length < 2 || arguments[1] == null)
                    return Config.MoneyFirst ? Config.MoneyLabel + money : money + Config.MoneyLabel;
                var format = arguments[1].GetStrValue(exm);
                string ret;
                try
                {
                    ret = money.ToString(format);
                }
                catch (FormatException)
                {
                    throw new CodeEE("MONEYSTR関数の第2引数の書式指定が間違っています");
                }
                return Config.MoneyFirst ? Config.MoneyLabel + ret : ret + Config.MoneyLabel;
            }
        }

        private sealed class GetPrintCPerLineMethod : FunctionMethod
        {
            public GetPrintCPerLineMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Config.PrintCPerLine;
            }
        }

        private sealed class PrintCLengthMethod : FunctionMethod
        {
            public PrintCLengthMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Config.PrintCLength;
            }
        }

        private sealed class GetSaveNosMethod : FunctionMethod
        {
            public GetSaveNosMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Config.SaveDataNos;
            }
        }

        private sealed class GettimeMethod : FunctionMethod
        {
            public GettimeMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long date = DateTime.Now.Year;
                date = date * 100 + DateTime.Now.Month;
                date = date * 100 + DateTime.Now.Day;
                date = date * 100 + DateTime.Now.Hour;
                date = date * 100 + DateTime.Now.Minute;
                date = date * 100 + DateTime.Now.Second;
                date = date * 1000 + DateTime.Now.Millisecond;
                return date; //17桁。2京くらい。
            }
        }

        private sealed class GettimesMethod : FunctionMethod
        {
            public GettimesMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
        }

        private sealed class GetmsMethod : FunctionMethod
        {
            public GetmsMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                //西暦0001年1月1日からの経過時間をミリ秒で。
                //Ticksは100ナノ秒単位であるが実際にはそんな精度はないので無駄。
                return DateTime.Now.Ticks / 10000;
            }
        }

        private sealed class GetSecondMethod : FunctionMethod
        {
            public GetSecondMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                //西暦0001年1月1日からの経過時間を秒で。
                //Ticksは100ナノ秒単位であるが実際にはそんな精度はないので無駄。
                return DateTime.Now.Ticks / 10000000;
            }
        }

        #endregion

        #region 数学関数

        private sealed class RandMethod : FunctionMethod
        {
            public RandMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常2つ、1つ省略可能で1～2の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments.Length == 1)
                {
                    if (arguments[0] == null)
                        return name + "関数には少なくとも1つの引数が必要です";
                    if (arguments[0].GetOperandType() != typeof(long))
                        return name + "関数の1番目の引数の型が正しくありません";
                    return null;
                }
                //1番目は省略可能
                if (arguments[0] != null && arguments[0].GetOperandType() != typeof(long))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long max = 0;
                long min = 0;
                if (arguments.Length == 1)
                {
                    max = arguments[0].GetIntValue(exm);
                }
                else
                {
                    if (arguments[0] != null)
                        min = arguments[0].GetIntValue(exm);
                    max = arguments[1].GetIntValue(exm);
                }
                if (max <= min)
                    if (min == 0)
                        throw new CodeEE("RANDの最大値に0以下の値(" + max + ")が指定されました");
                    else
                        throw new CodeEE("RANDの最大値に最小値以下の値(" + max + ")が指定されました");
                return exm.VEvaluator.GetNextRand(max - min) + min;
            }
        }

        private sealed class MaxMethod : FunctionMethod
        {
            private readonly bool isMax;

            public MaxMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isMax = true;
                CanRestructure = true;
            }

            public MaxMethod(bool max)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isMax = max;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + "関数の" + (i + 1) + "番目の引数は省略できません";
                    if (arguments[i].GetOperandType() != typeof(long))
                        return name + "関数の" + (i + 1) + "番目の引数の型が正しくありません";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);

                for (var i = 1; i < arguments.Length; i++)
                {
                    var newRet = arguments[i].GetIntValue(exm);
                    if (isMax)
                    {
                        if (ret < newRet)
                            ret = newRet;
                    }
                    else
                    {
                        if (ret > newRet)
                            ret = newRet;
                    }
                }
                return ret;
            }
        }

        private sealed class AbsMethod : FunctionMethod
        {
            public AbsMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                return Math.Abs(ret);
            }
        }

        private sealed class PowerMethod : FunctionMethod
        {
            public PowerMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var x = arguments[0].GetIntValue(exm);
                var y = arguments[1].GetIntValue(exm);
                var pow = Math.Pow(x, y);
                if (double.IsNaN(pow))
                    throw new CodeEE("累乗結果が非数値です");
                if (double.IsInfinity(pow))
                    throw new CodeEE("累乗結果が無限大です");
                if (pow >= long.MaxValue || pow <= long.MinValue)
                    throw new CodeEE("累乗結果(" + pow + ")が64ビット符号付き整数の範囲外です");
                return (long) pow;
            }
        }

        private sealed class SqrtMethod : FunctionMethod
        {
            public SqrtMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                if (ret < 0)
                    throw new CodeEE("SQRT関数の引数に負の値が指定されました");
                return (long) Math.Sqrt(ret);
            }
        }

        private sealed class CbrtMethod : FunctionMethod
        {
            public CbrtMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                if (ret < 0)
                    throw new CodeEE("CBRT関数の引数に負の値が指定されました");
                return (long) Math.Pow(ret, 1.0 / 3.0);
            }
        }

        private sealed class LogMethod : FunctionMethod
        {
            private readonly double Base;

            public LogMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                Base = Math.E;
                CanRestructure = true;
            }

            public LogMethod(double b)
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                Base = b;
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                if (ret <= 0)
                    throw new CodeEE("対数関数の引数に0以下の値が指定されました");
                if (Base <= 0.0d)
                    throw new CodeEE("対数関数の底に0以下の値が指定されました");
                double dret = ret;
                if (Base == Math.E)
                    dret = Math.Log(dret);
                else
                    dret = Math.Log10(dret);
                if (double.IsNaN(dret))
                    throw new CodeEE("計算値が非数値です");
                if (double.IsInfinity(dret))
                    throw new CodeEE("計算値が無限大です");
                if (dret >= long.MaxValue || dret <= long.MinValue)
                    throw new CodeEE("計算結果(" + dret + ")が64ビット符号付き整数の範囲外です");
                return (long) dret;
            }
        }

        private sealed class ExpMethod : FunctionMethod
        {
            public ExpMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                var dret = Math.Exp(ret);
                if (double.IsNaN(dret))
                    throw new CodeEE("計算値が非数値です");
                if (double.IsInfinity(dret))
                    throw new CodeEE("計算値が無限大です");
                if (dret >= long.MaxValue || dret <= long.MinValue)
                    throw new CodeEE("計算結果(" + dret + ")が64ビット符号付き整数の範囲外です");

                return (long) dret;
            }
        }

        private sealed class SignMethod : FunctionMethod
        {
            public SignMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var ret = arguments[0].GetIntValue(exm);
                return Math.Sign(ret);
            }
        }

        private sealed class GetLimitMethod : FunctionMethod
        {
            public GetLimitMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var value = arguments[0].GetIntValue(exm);
                var min = arguments[1].GetIntValue(exm);
                var max = arguments[2].GetIntValue(exm);
                long ret = 0;
                if (value < min)
                    ret = min;
                else if (value > max)
                    ret = max;
                else
                    ret = value;
                return ret;
            }
        }

        #endregion

        #region 変数操作系

        private sealed class SumArrayMethod : FunctionMethod
        {
            private readonly bool isCharaRange;

            public SumArrayMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = false;
                CanRestructure = false;
            }

            public SumArrayMethod(bool isChara)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数が変数ではありません";
                var varToken = (VariableTerm) arguments[0];
                if (varToken.IsString)
                    return name + "関数の1番目の引数が数値変数ではありません";
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + "関数の1番目の引数がキャラクタ変数ではありません";
                if (!isCharaRange && !varToken.Identifier.IsArray1D && !varToken.Identifier.IsArray2D &&
                    !varToken.Identifier.IsArray3D)
                    return name + "関数の1番目の引数が配列変数ではありません";
                if (arguments.Length == 1)
                    return null;
                if (arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の変数が数値ではありません";
                if (arguments.Length == 2)
                    return null;
                if (arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の変数が数値ではありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var varTerm = (VariableTerm) arguments[0];
                var index1 = arguments.Length >= 2 && arguments[1] != null ? arguments[1].GetIntValue(exm) : 0;
                var index2 = arguments.Length == 3 && arguments[2] != null
                    ? arguments[2].GetIntValue(exm)
                    : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLastLength());

                var p = varTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(index1, index2, "SUMARRAY", 2L, 3L);
                    return exm.VEvaluator.GetArraySum(p, index1, index2);
                }
                var charaNum = exm.VEvaluator.CHARANUM;
                if (index1 >= charaNum || index1 < 0 || index2 > charaNum || index2 < 0)
                    throw new CodeEE("SUMCARRAY関数の範囲指定がキャラクタ配列の範囲を超えています(" + index1 + "～" + index2 + ")");
                return exm.VEvaluator.GetArraySumChara(p, index1, index2);
            }
        }

        private sealed class MatchMethod : FunctionMethod
        {
            private readonly bool isCharaRange;

            public MatchMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = false;
                CanRestructure = false;
                HasUniqueRestructure = true;
            }

            public MatchMethod(bool isChara)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
                HasUniqueRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 4)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数が変数ではありません";
                var varToken = (VariableTerm) arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + "関数の1番目の引数がキャラクタ変数ではありません";
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + "関数は二重配列・三重配列には対応していません";
                if (!isCharaRange && !varToken.Identifier.IsArray1D)
                    return name + "関数の1番目の引数が配列変数ではありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
                    return name + "関数の1番目の引数と2番目の引数の型が異なります";
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                if (arguments.Length >= 4 && arguments[3] != null && arguments[3].GetOperandType() != typeof(long))
                    return name + "関数の4番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var varTerm = arguments[0] as VariableTerm;
                var start = arguments.Length > 2 && arguments[2] != null ? arguments[2].GetIntValue(exm) : 0;
                var end = arguments.Length > 3 && arguments[3] != null
                    ? arguments[3].GetIntValue(exm)
                    : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

                var p = varTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, "MATCH", 3L, 4L);
                    if (arguments[0].GetOperandType() == typeof(long))
                    {
                        var targetValue = arguments[1].GetIntValue(exm);
                        return exm.VEvaluator.GetMatch(p, targetValue, start, end);
                    }
                    var targetStr = arguments[1].GetStrValue(exm);
                    return exm.VEvaluator.GetMatch(p, targetStr, start, end);
                }
                var charaNum = exm.VEvaluator.CHARANUM;
                if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                    throw new CodeEE("CMATCH関数の範囲指定がキャラクタ配列の範囲を超えています(" + start + "～" + end + ")");
                if (arguments[0].GetOperandType() == typeof(long))
                {
                    var targetValue = arguments[1].GetIntValue(exm);
                    return exm.VEvaluator.GetMatchChara(p, targetValue, start, end);
                }
                {
                    var targetStr = arguments[1].GetStrValue(exm);
                    return exm.VEvaluator.GetMatchChara(p, targetStr, start, end);
                }
            }

            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[0].Restructure(exm);
                for (var i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        continue;
                    arguments[i] = arguments[i].Restructure(exm);
                }
                return false;
            }
        }

        private sealed class GroupMatchMethod : FunctionMethod
        {
            public GroupMatchMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                var baseType = arguments[0].GetOperandType();
                for (var i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + "関数の" + (i + 1) + "番目の引数は省略できません";
                    if (arguments[i].GetOperandType() != baseType)
                        return name + "関数の" + (i + 1) + "番目の引数の型が正しくありません";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long ret = 0;
                if (arguments[0].GetOperandType() == typeof(long))
                {
                    var baseValue = arguments[0].GetIntValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseValue == arguments[i].GetIntValue(exm))
                            ret += 1;
                }
                else
                {
                    var baseString = arguments[0].GetStrValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseString == arguments[i].GetStrValue(exm))
                            ret += 1;
                }
                return ret;
            }
        }

        private sealed class NosamesMethod : FunctionMethod
        {
            public NosamesMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                var baseType = arguments[0].GetOperandType();
                for (var i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + "関数の" + (i + 1) + "番目の引数は省略できません";
                    if (arguments[i].GetOperandType() != baseType)
                        return name + "関数の" + (i + 1) + "番目の引数の型が正しくありません";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (arguments[0].GetOperandType() == typeof(long))
                {
                    var baseValue = arguments[0].GetIntValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseValue == arguments[i].GetIntValue(exm))
                            return 0L;
                }
                else
                {
                    var baseValue = arguments[0].GetStrValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseValue == arguments[i].GetStrValue(exm))
                            return 0L;
                }
                return 1L;
            }
        }

        private sealed class AllsamesMethod : FunctionMethod
        {
            public AllsamesMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                var baseType = arguments[0].GetOperandType();
                for (var i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + "関数の" + (i + 1) + "番目の引数は省略できません";
                    if (arguments[i].GetOperandType() != baseType)
                        return name + "関数の" + (i + 1) + "番目の引数の型が正しくありません";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (arguments[0].GetOperandType() == typeof(long))
                {
                    var baseValue = arguments[0].GetIntValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseValue != arguments[i].GetIntValue(exm))
                            return 0L;
                }
                else
                {
                    var baseValue = arguments[0].GetStrValue(exm);
                    for (var i = 1; i < arguments.Length; i++)
                        if (baseValue != arguments[i].GetStrValue(exm))
                            return 0L;
                }
                return 1L;
            }
        }

        private sealed class MaxArrayMethod : FunctionMethod
        {
            private readonly string funcName;
            private readonly bool isCharaRange;
            private readonly bool isMax;

            public MaxArrayMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = false;
                isMax = true;
                funcName = "MAXARRAY";
                CanRestructure = false;
            }

            public MaxArrayMethod(bool isChara)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = isChara;
                isMax = true;
                if (isCharaRange)
                    funcName = "MAXCARRAY";
                else
                    funcName = "MAXARRAY";
                CanRestructure = false;
            }

            public MaxArrayMethod(bool isChara, bool isMaxFunc)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = isChara;
                isMax = isMaxFunc;
                funcName = (isMax ? "MAX" : "MIN") + (isCharaRange ? "C" : "") + "ARRAY";
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数が変数ではありません";
                var varToken = (VariableTerm) arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + "関数の1番目の引数がキャラクタ変数ではありません";
                if (!varToken.IsInteger)
                    return name + "関数の1番目の引数が数値変数ではありません";
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + "関数は二重配列・三重配列には対応していません";
                if (!varToken.Identifier.IsArray1D)
                    return name + "関数の1番目の引数が配列変数ではありません";
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var vTerm = (VariableTerm) arguments[0];
                var start = arguments.Length > 1 && arguments[1] != null ? arguments[1].GetIntValue(exm) : 0;
                long end = arguments.Length > 2 && arguments[2] != null
                    ? end = arguments[2].GetIntValue(exm)
                    : (isCharaRange ? exm.VEvaluator.CHARANUM : vTerm.GetLength());
                var p = vTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, funcName, 2L, 3L);
                    return exm.VEvaluator.GetMaxArray(p, start, end, isMax);
                }
                var charaNum = exm.VEvaluator.CHARANUM;
                if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                    throw new CodeEE(funcName + "関数の範囲指定がキャラクタ配列の範囲を超えています(" + start + "～" + end + ")");
                return exm.VEvaluator.GetMaxArrayChara(p, start, end, isMax);
            }
        }

        private sealed class GetbitMethod : FunctionMethod
        {
            public GetbitMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                var ret = base.CheckArgumentType(name, arguments);
                if (ret != null)
                    return ret;
                if (arguments[1] is SingleTerm)
                {
                    var m = ((SingleTerm) arguments[1]).Int;
                    if (m < 0 || m > 63)
                        return "GETBIT関数の第２引数(" + m + ")が範囲(０～６３)を超えています";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var n = arguments[0].GetIntValue(exm);
                var m = arguments[1].GetIntValue(exm);
                if (m < 0 || m > 63)
                    throw new CodeEE("GETBIT関数の第２引数(" + m + ")が範囲(０～６３)を超えています");
                var mi = (int) m;
                return (n >> mi) & 1;
            }
        }

        private sealed class GetnumMethod : FunctionMethod
        {
            public GetnumMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = true;
                HasUniqueRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length != 2)
                    return name + "関数には2つの引数が必要です";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != typeof(string))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var vToken = (VariableTerm) arguments[0];
                var varCode = vToken.Identifier.Code;
                var key = arguments[1].GetStrValue(exm);
                var ret = 0;
                if (exm.VEvaluator.Constant.TryKeywordToInteger(out ret, varCode, key, -1))
                    return ret;
                return -1;
            }

            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[1] = arguments[1].Restructure(exm);
                return arguments[1] is SingleTerm;
            }
        }

        private sealed class GetnumBMethod : FunctionMethod
        {
            public GetnumBMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string), typeof(string)};
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                var errStr = base.CheckArgumentType(name, arguments);
                if (errStr != null)
                    return errStr;
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0] is SingleTerm)
                {
                    var varName = ((SingleTerm) arguments[0]).Str;
                    if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
                        return name + "関数の1番目の引数が変数名ではありません";
                }
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
                if (var == null)
                    throw new CodeEE("GETNUMBの1番目の引数(\"" + arguments[0].GetStrValue(exm) + "\")が変数名ではありません");
                var key = arguments[1].GetStrValue(exm);
                var ret = 0;
                if (exm.VEvaluator.Constant.TryKeywordToInteger(out ret, var.Code, key, -1))
                    return ret;
                return -1;
            }
        }

        private sealed class GetPalamLVMethod : FunctionMethod
        {
            public GetPalamLVMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long)};
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                var errStr = base.CheckArgumentType(name, arguments);
                if (errStr != null)
                    return errStr;
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var value = arguments[0].GetIntValue(exm);
                var maxLv = arguments[1].GetIntValue(exm);

                return exm.VEvaluator.getPalamLv(value, maxLv);
            }
        }

        private sealed class GetExpLVMethod : FunctionMethod
        {
            public GetExpLVMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long)};
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                var errStr = base.CheckArgumentType(name, arguments);
                if (errStr != null)
                    return errStr;
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var value = arguments[0].GetIntValue(exm);
                var maxLv = arguments[1].GetIntValue(exm);

                return exm.VEvaluator.getExpLv(value, maxLv);
            }
        }

        private sealed class FindElementMethod : FunctionMethod
        {
            private readonly string funcName;
            private readonly bool isLast;

            public FindElementMethod(bool last)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = true; //すべて定数項ならできるはず
                HasUniqueRestructure = true;
                isLast = last;
                funcName = isLast ? "FINDLASTELEMENT" : "FINDELEMENT";
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 5)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                var varToken = arguments[0] as VariableTerm;
                if (varToken == null)
                    return name + "関数の1番目の引数が変数ではありません";
                if (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D)
                    return name + "関数は二重配列・三重配列には対応していません";
                if (!varToken.Identifier.IsArray1D)
                    return name + "関数の1番目の引数が配列変数ではありません";
                var baseType = arguments[0].GetOperandType();
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != baseType)
                    return name + "関数の2番目の引数の型が正しくありません";
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                if (arguments.Length >= 4 && arguments[3] != null && arguments[3].GetOperandType() != typeof(long))
                    return name + "関数の4番目の引数の型が正しくありません";
                if (arguments.Length >= 5 && arguments[4] != null && arguments[4].GetOperandType() != typeof(long))
                    return name + "関数の5番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var isExact = false;
                var varTerm = (VariableTerm) arguments[0];

                var start = arguments.Length > 2 && arguments[2] != null ? arguments[2].GetIntValue(exm) : 0;
                var end = arguments.Length > 3 && arguments[3] != null
                    ? arguments[3].GetIntValue(exm)
                    : varTerm.GetLength();
                if (arguments.Length > 4 && arguments[4] != null)
                    isExact = arguments[4].GetIntValue(exm) != 0;

                var p = varTerm.GetFixedVariableTerm(exm);
                p.IsArrayRangeValid(start, end, funcName, 3L, 4L);

                if (arguments[0].GetOperandType() == typeof(long))
                {
                    var targetValue = arguments[1].GetIntValue(exm);
                    return exm.VEvaluator.FindElement(p, targetValue, start, end, isExact, isLast);
                }
                Regex targetString = null;
                try
                {
                    targetString = new Regex(arguments[1].GetStrValue(exm));
                }
                catch (ArgumentException)
                {
                    throw new CodeEE("第2引数が正規表現として不正です");
                }
                return exm.VEvaluator.FindElement(p, targetString, start, end, isExact, isLast);
            }


            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var isConst = true;

                arguments[0].Restructure(exm);
                var varToken = arguments[0] as VariableTerm;
                isConst = varToken.Identifier.IsConst;

                for (var i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        continue;
                    arguments[i] = arguments[i].Restructure(exm);
                    if (isConst && !(arguments[i] is SingleTerm))
                        isConst = false;
                }
                return isConst;
            }
        }

        private sealed class InRangeMethod : FunctionMethod
        {
            public InRangeMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(long), typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var value = arguments[0].GetIntValue(exm);
                var min = arguments[1].GetIntValue(exm);
                var max = arguments[2].GetIntValue(exm);
                return value >= min && value <= max ? 1L : 0L;
            }
        }

        private sealed class InRangeArrayMethod : FunctionMethod
        {
            private readonly bool isCharaRange;

            public InRangeArrayMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public InRangeArrayMethod(bool isChara)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 6)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (!(arguments[0] is VariableTerm))
                    return name + "関数の1番目の引数が変数ではありません";
                var varToken = (VariableTerm) arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + "関数の1番目の引数がキャラクタ変数ではありません";
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + "関数は二重配列・三重配列には対応していません";
                if (!isCharaRange && !varToken.Identifier.IsArray1D)
                    return name + "関数の1番目の引数が配列変数ではありません";
                if (!varToken.IsInteger)
                    return name + "関数の1番目の引数が数値型変数ではありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数が数値型ではありません";
                if (arguments[2] == null)
                    return name + "関数の3番目の引数は省略できません";
                if (arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数が数値型ではありません";
                if (arguments.Length >= 4 && arguments[3] != null && arguments[3].GetOperandType() != typeof(long))
                    return name + "関数の4番目の引数の型が正しくありません";
                if (arguments.Length >= 5 && arguments[4] != null && arguments[4].GetOperandType() != typeof(long))
                    return name + "関数の5番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var min = arguments[1].GetIntValue(exm);
                var max = arguments[2].GetIntValue(exm);

                var varTerm = arguments[0] as VariableTerm;
                var start = arguments.Length > 3 && arguments[3] != null ? arguments[3].GetIntValue(exm) : 0;
                var end = arguments.Length > 4 && arguments[4] != null
                    ? arguments[4].GetIntValue(exm)
                    : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

                var p = varTerm.GetFixedVariableTerm(exm);

                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, "INRANGEARRAY", 4L, 5L);
                    return exm.VEvaluator.GetInRangeArray(p, min, max, start, end);
                }
                var charaNum = exm.VEvaluator.CHARANUM;
                if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                    throw new CodeEE("INRANGECARRAY関数の範囲指定がキャラクタ配列の範囲を超えています(" + start + "～" + end + ")");
                return exm.VEvaluator.GetInRangeArrayChara(p, min, max, start, end);
            }
        }

        #endregion

        #region 文字列操作系

        private sealed class StrlenMethod : FunctionMethod
        {
            public StrlenMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                return LangManager.GetStrlenLang(str);
            }
        }

        private sealed class StrlenuMethod : FunctionMethod
        {
            public StrlenuMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                return str.Length;
            }
        }

        private sealed class SubstringMethod : FunctionMethod
        {
            public SubstringMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常３つ、２つ省略可能で１～３の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";

                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + "関数の1番目の引数の型が正しくありません";
                //2、３は省略可能
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                var start = 0;
                var length = -1;
                if (arguments.Length >= 2 && arguments[1] != null)
                    start = (int) arguments[1].GetIntValue(exm);
                if (arguments.Length >= 3 && arguments[2] != null)
                    length = (int) arguments[2].GetIntValue(exm);

                return LangManager.GetSubStringLang(str, start, length);
            }
        }

        private sealed class SubstringuMethod : FunctionMethod
        {
            public SubstringuMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常３つ、２つ省略可能で１～３の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";

                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + "関数の1番目の引数の型が正しくありません";
                //2、３は省略可能
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                var start = 0;
                var length = -1;
                if (arguments.Length >= 2 && arguments[1] != null)
                    start = (int) arguments[1].GetIntValue(exm);
                if (arguments.Length >= 3 && arguments[2] != null)
                    length = (int) arguments[2].GetIntValue(exm);
                if (start >= str.Length || length == 0)
                    return "";
                if (length < 0 || length > str.Length)
                    length = str.Length;
                if (start <= 0)
                    if (length == str.Length)
                        return str;
                    else
                        start = 0;
                if (start + length > str.Length)
                    length = str.Length - start;

                return str.Substring(start, length);
            }
        }

        private sealed class StrfindMethod : FunctionMethod
        {
            private readonly bool unicode;

            public StrfindMethod(bool unicode)
            {
                ReturnType = typeof(long);
                argumentTypeArray = null;
                CanRestructure = true;
                this.unicode = unicode;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常３つ、１つ省略可能で２～３の引数が必要。
                if (arguments.Length < 2)
                    return name + "関数には少なくとも2つの引数が必要です";
                if (arguments.Length > 3)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments[1] == null)
                    return name + "関数の2番目の引数は省略できません";
                if (arguments[1].GetOperandType() != typeof(string))
                    return name + "関数の2番目の引数の型が正しくありません";
                //3つ目は省略可能
                if (arguments.Length >= 3 && arguments[2] != null && arguments[2].GetOperandType() != typeof(long))
                    return name + "関数の3番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var target = arguments[0].GetStrValue(exm);
                var word = arguments[1].GetStrValue(exm);
                int JISstart = 0, UFTstart = 0;
                if (arguments.Length >= 3 && arguments[2] != null)
                    if (unicode)
                    {
                        UFTstart = (int) arguments[2].GetIntValue(exm);
                    }
                    else
                    {
                        JISstart = (int) arguments[2].GetIntValue(exm);
                        UFTstart = LangManager.GetUFTIndex(target, JISstart);
                    }
                if (UFTstart < 0 || UFTstart >= target.Length)
                    return -1;
                var index = target.IndexOf(word, UFTstart);
                if (index > 0 && !unicode)
                {
                    var subStr = target.Substring(0, index);
                    index = LangManager.GetStrlenLang(subStr);
                }
                return index;
            }
        }

        private sealed class StrCountMethod : FunctionMethod
        {
            public StrCountMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string), typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Regex reg = null;
                try
                {
                    reg = new Regex(arguments[1].GetStrValue(exm));
                }
                catch (ArgumentException e)
                {
                    throw new CodeEE("第2引数が正規表現として不正です：" + e.Message);
                }
                return reg.Matches(arguments[0].GetStrValue(exm)).Count;
            }
        }

        private sealed class ToStrMethod : FunctionMethod
        {
            public ToStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常2つ、1つ省略可能で1～2の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(long))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(string))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var i = arguments[0].GetIntValue(exm);
                if (arguments.Length < 2 || arguments[1] == null)
                    return i.ToString();
                var format = arguments[1].GetStrValue(exm);
                string ret;
                try
                {
                    ret = i.ToString(format);
                }
                catch (FormatException)
                {
                    throw new CodeEE("TOSTR関数の書式指定が間違っています");
                }
                return ret;
            }
        }

        private sealed class ToIntMethod : FunctionMethod
        {
            public ToIntMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                if (str == null || str == "")
                    return 0;
                //全角文字が入ってるなら無条件で0を返す
                if (str.Length < LangManager.GetStrlenLang(str))
                    return 0;
                var st = new StringStream(str);
                if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
                    return 0;
                if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
                    return 0;
                var ret = LexicalAnalyzer.ReadInt64(st, true);
                if (!st.EOS)
                    if (st.Current == '.')
                    {
                        st.ShiftNext();
                        while (!st.EOS)
                        {
                            if (!char.IsDigit(st.Current))
                                return 0;
                            st.ShiftNext();
                        }
                    }
                    else
                    {
                        return 0;
                    }
                return ret;
            }
        }

        //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
        [Obfuscation(Exclude = false)]
        //TOUPPER等の処理を汎用化するためのenum
        private enum StrFormType
        {
            Upper = 0,
            Lower = 1,
            Half = 2,
            Full = 3
        }

        private sealed class StrChangeStyleMethod : FunctionMethod
        {
            private readonly StrFormType strType;

            public StrChangeStyleMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                strType = StrFormType.Upper;
                CanRestructure = true;
            }

            public StrChangeStyleMethod(StrFormType type)
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                strType = type;
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                if (str == null || str == "")
                    return "";
                switch (strType)
                {
                    case StrFormType.Upper:
                        return str.ToUpper();
                    case StrFormType.Lower:
                        return str.ToLower();
                    case StrFormType.Half:
                        return Strings.StrConv(str, VbStrConv.Narrow, Config.Language);
                    case StrFormType.Full:
                        return Strings.StrConv(str, VbStrConv.Wide, Config.Language);
                }
                return "";
            }
        }

        private sealed class LineIsEmptyMethod : FunctionMethod
        {
            public LineIsEmptyMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return GlobalStatic.Console.EmptyLine ? 1L : 0L;
            }
        }

        private sealed class ReplaceMethod : FunctionMethod
        {
            public ReplaceMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string), typeof(string), typeof(string)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var baseString = arguments[0].GetStrValue(exm);
                Regex reg;
                try
                {
                    reg = new Regex(arguments[1].GetStrValue(exm));
                }
                catch (ArgumentException e)
                {
                    throw new CodeEE("第２引数が正規表現として不正です：" + e.Message);
                }
                return reg.Replace(baseString, arguments[2].GetStrValue(exm));
            }
        }

        private sealed class UnicodeMethod : FunctionMethod
        {
            public UnicodeMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(long)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var i = arguments[0].GetIntValue(exm);
                if (i < 0 || i > 0xFFFF)
                    throw new CodeEE("UNICODE関数に範囲外の値(" + i + ")が渡されました");
                var s = new string(new[] {(char) i}); // char.ConvertFromUtf32(i);

                return s;
            }
        }

        private sealed class UnicodeByteMethod : FunctionMethod
        {
            public UnicodeByteMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var target = arguments[0].GetStrValue(exm);
                var length = Encoding.UTF32.GetEncoder().GetByteCount(target.ToCharArray(), 0, target.Length, false);
                var bytes = new byte[length];
                Encoding.UTF32.GetEncoder().GetBytes(target.ToCharArray(), 0, target.Length, bytes, 0, false);
                long i = BitConverter.ToInt32(bytes, 0);

                return i;
            }
        }

        private sealed class ConvertIntMethod : FunctionMethod
        {
            public ConvertIntMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(long), typeof(long)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var toBase = arguments[1].GetIntValue(exm);
                if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
                    throw new CodeEE("CONVERT関数の第２引数は2, 8, 10, 16のいずれかでなければなりません");
                return Convert.ToString(arguments[0].GetIntValue(exm), (int) toBase);
            }
        }

        private sealed class IsNumericMethod : FunctionMethod
        {
            public IsNumericMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var baseStr = arguments[0].GetStrValue(exm);

                //全角文字があるなら数値ではない
                if (baseStr.Length < LangManager.GetStrlenLang(baseStr))
                    return 0;
                var st = new StringStream(baseStr);
                if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
                    return 0;
                if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
                    return 0;
                var ret = LexicalAnalyzer.ReadInt64(st, true);
                if (!st.EOS)
                    if (st.Current == '.')
                    {
                        st.ShiftNext();
                        while (!st.EOS)
                        {
                            if (!char.IsDigit(st.Current))
                                return 0;
                            st.ShiftNext();
                        }
                    }
                    else
                    {
                        return 0;
                    }
                return 1;
            }
        }

        private sealed class EscapeMethod : FunctionMethod
        {
            public EscapeMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Regex.Escape(arguments[0].GetStrValue(exm));
            }
        }

        private sealed class EncodeToUniMethod : FunctionMethod
        {
            public EncodeToUniMethod()
            {
                ReturnType = typeof(long);
                argumentTypeArray = new Type[] {null};
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常2つ、1つ省略可能で1～2の引数が必要。
                if (arguments.Length < 1)
                    return name + "関数には少なくとも1つの引数が必要です";
                if (arguments.Length > 2)
                    return name + "関数の引数が多すぎます";
                if (arguments[0] == null)
                    return name + "関数の1番目の引数は省略できません";
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + "関数の1番目の引数の型が正しくありません";
                if (arguments.Length >= 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(long))
                    return name + "関数の2番目の引数の型が正しくありません";
                return null;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var baseStr = arguments[0].GetStrValue(exm);
                if (baseStr.Length == 0)
                    return -1;
                var position = arguments.Length > 1 && arguments[1] != null ? arguments[1].GetIntValue(exm) : 0;
                if (position < 0)
                    throw new CodeEE("ENCOIDETOUNI関数の第２引数(" + position + ")が負の値です");
                if (position >= baseStr.Length)
                    throw new CodeEE("ENCOIDETOUNI関数の第２引数(" + position + ")が第１引数の文字列(" + baseStr + ")の文字数を超えています");
                return char.ConvertToUtf32(baseStr, (int) position);
            }
        }

        public sealed class CharAtMethod : FunctionMethod
        {
            public CharAtMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string), typeof(long)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                var pos = arguments[1].GetIntValue(exm);
                if (pos < 0 || pos >= str.Length)
                    return "";
                return str[(int) pos].ToString();
            }
        }

        public sealed class GetLineStrMethod : FunctionMethod
        {
            public GetLineStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                if (string.IsNullOrEmpty(str))
                    throw new CodeEE("GETLINESTR関数の引数が空文字列です");
                return exm.Console.getStBar(str);
            }
        }

        public sealed class StrFormMethod : FunctionMethod
        {
            public StrFormMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                var destStr = str;
                try
                {
                    var wt = LexicalAnalyzer.AnalyseFormattedString(new StringStream(str), FormStrEndWith.EoL, false);
                    var strForm = StrForm.FromWordToken(wt);
                    destStr = strForm.GetString(exm);
                }
                catch (CodeEE e)
                {
                    throw new CodeEE("STRFORM関数:文字列\"" + str + "\"の展開エラー:" + e.Message);
                }
                catch
                {
                    throw new CodeEE("STRFORM関数:文字列\"" + str + "\"の展開処理中にエラーが発生しました");
                }
                return destStr;
            }
        }

        public sealed class GetConfigMethod : FunctionMethod
        {
            private readonly string funcname;

            public GetConfigMethod(bool typeisInt)
            {
                if (typeisInt)
                {
                    funcname = "GETCONFIG";
                    ReturnType = typeof(long);
                }
                else
                {
                    funcname = "GETCONFIGS";
                    ReturnType = typeof(string);
                }
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = true;
            }

            private SingleTerm getSingleTerm(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var str = arguments[0].GetStrValue(exm);
                if (str == null || str.Length == 0)
                    throw new CodeEE(funcname + "関数に空文字列が渡されました");
                string errMes = null;
                var term = ConfigData.Instance.GetConfigValueInERB(str, ref errMes);
                if (errMes != null)
                    throw new CodeEE(funcname + "関数:" + errMes);
                return term;
            }

            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (ReturnType != typeof(long))
                    throw new ExeEE(funcname + "関数:不正な呼び出し");
                var term = getSingleTerm(exm, arguments);
                if (term.GetOperandType() != typeof(long))
                    throw new CodeEE(funcname + "関数:型が違います（GETCONFIGS関数を使用してください）");
                return term.Int;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (ReturnType != typeof(string))
                    throw new ExeEE(funcname + "関数:不正な呼び出し");
                var term = getSingleTerm(exm, arguments);
                if (term.GetOperandType() != typeof(string))
                    throw new CodeEE(funcname + "関数:型が違います（GETCONFIG関数を使用してください）");
                return term.Str;
            }
        }

        #endregion

        #region html系

        private sealed class HtmlGetPrintedStrMethod : FunctionMethod
        {
            public HtmlGetPrintedStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //通常１つ。省略可能。
                if (arguments.Length > 1)
                    return name + "関数の引数が多すぎます";
                if (arguments.Length == 0 || arguments[0] == null)
                    return null;
                if (arguments[0].GetOperandType() != typeof(long))
                    return name + "関数の1番目の引数の型が正しくありません";
                return null;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long lineNo = 0;
                if (arguments.Length > 0)
                    lineNo = arguments[0].GetIntValue(exm);
                if (lineNo < 0)
                    throw new CodeEE("引数を0未満にできません");
                var dispLines = exm.Console.GetDisplayLines(lineNo);
                if (dispLines == null)
                    return "";
                return HtmlManager.DisplayLine2Html(dispLines, true);
            }
        }

        private sealed class HtmlPopPrintingStrMethod : FunctionMethod
        {
            public HtmlPopPrintingStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                var dispLines = exm.Console.PopDisplayingLines();
                if (dispLines == null)
                    return "";
                return HtmlManager.DisplayLine2Html(dispLines, false);
            }
        }

        private sealed class HtmlToPlainTextMethod : FunctionMethod
        {
            public HtmlToPlainTextMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return HtmlManager.Html2PlainText(arguments[0].GetStrValue(exm));
            }
        }

        private sealed class HtmlEscapeMethod : FunctionMethod
        {
            public HtmlEscapeMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new[] {typeof(string)};
                CanRestructure = false;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return HtmlManager.Escape(arguments[0].GetStrValue(exm));
            }
        }

        #endregion
    }
}