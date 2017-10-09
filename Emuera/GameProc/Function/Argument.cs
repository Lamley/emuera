using System.Collections.Generic;
using System.Reflection;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameData.Variable;

namespace MinorShift.Emuera.GameProc.Function
{
    internal abstract class Argument
    {
        public long ConstInt;
        public string ConstStr;
        public bool IsConst;
    }

    internal sealed class VoidArgument : Argument
    {
    }

    internal sealed class ErrorArgument : Argument
    {
        private readonly string errorMes;

        public ErrorArgument(string errorMes)
        {
            this.errorMes = errorMes;
        }
    }

    internal sealed class ExpressionArgument : Argument
    {
        public readonly IOperandTerm Term;

        public ExpressionArgument(IOperandTerm termSrc)
        {
            Term = termSrc;
        }
    }

    internal sealed class ExpressionArrayArgument : Argument
    {
        public readonly IOperandTerm[] TermList;

        public ExpressionArrayArgument(List<IOperandTerm> termList)
        {
            TermList = new IOperandTerm[termList.Count];
            termList.CopyTo(TermList);
        }
    }

    internal sealed class SpPrintVArgument : Argument
    {
        public readonly IOperandTerm[] Terms;

        public SpPrintVArgument(IOperandTerm[] list)
        {
            Terms = list;
        }
    }

    internal sealed class SpTimesArgument : Argument
    {
        public readonly double DoubleValue;
        public readonly VariableTerm VariableDest;

        public SpTimesArgument(VariableTerm var, double d)
        {
            VariableDest = var;
            DoubleValue = d;
        }
    }

    internal sealed class SpBarArgument : Argument
    {
        public readonly IOperandTerm[] Terms = new IOperandTerm[3];

        public SpBarArgument(IOperandTerm value, IOperandTerm max, IOperandTerm length)
        {
            Terms[0] = value;
            Terms[1] = max;
            Terms[2] = length;
        }
    }


    internal sealed class SpSwapCharaArgument : Argument
    {
        public readonly IOperandTerm X;
        public readonly IOperandTerm Y;

        public SpSwapCharaArgument(IOperandTerm x, IOperandTerm y)
        {
            X = x;
            Y = y;
        }
    }

    internal sealed class SpSwapVarArgument : Argument
    {
        public readonly VariableTerm var1;
        public readonly VariableTerm var2;

        public SpSwapVarArgument(VariableTerm v1, VariableTerm v2)
        {
            var1 = v1;
            var2 = v2;
        }
    }

    internal sealed class SpVarsizeArgument : Argument
    {
        public readonly VariableToken VariableID;

        public SpVarsizeArgument(VariableToken var)
        {
            VariableID = var;
        }
    }

    internal sealed class SpSaveDataArgument : Argument
    {
        public readonly IOperandTerm StrExpression;
        public readonly IOperandTerm Target;

        public SpSaveDataArgument(IOperandTerm target, IOperandTerm var)
        {
            Target = target;
            StrExpression = var;
        }
    }

    internal sealed class SpTInputsArgument : Argument
    {
        public readonly IOperandTerm Def;
        public readonly IOperandTerm Disp;
        public readonly IOperandTerm Time;
        public readonly IOperandTerm Timeout;

        public SpTInputsArgument(IOperandTerm time, IOperandTerm def, IOperandTerm disp, IOperandTerm timeout)
        {
            Time = time;
            Def = def;
            Disp = disp;
            Timeout = timeout;
        }
    }

    //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
    [Obfuscation(Exclude = false)]
    internal enum SortOrder
    {
        UNDEF = 0,
        ASCENDING = 1,
        DESENDING = 2
    }

    internal sealed class SpSortcharaArgument : Argument
    {
        public readonly VariableTerm SortKey;
        public readonly SortOrder SortOrder;

        public SpSortcharaArgument(VariableTerm var, SortOrder order)
        {
            SortKey = var;
            SortOrder = order;
        }
    }

    internal sealed class SpCallFArgment : Argument
    {
        public readonly IOperandTerm FuncnameTerm;
        public readonly IOperandTerm[] RowArgs;
        public readonly IOperandTerm[] SubNames;
        public IOperandTerm FuncTerm;

        public SpCallFArgment(IOperandTerm funcname, IOperandTerm[] subNames, IOperandTerm[] args)
        {
            FuncnameTerm = funcname;
            SubNames = subNames;
            RowArgs = args;
        }
    }

    internal sealed class SpCallArgment : Argument
    {
        public readonly IOperandTerm FuncnameTerm;
        public readonly IOperandTerm[] RowArgs;
        public readonly IOperandTerm[] SubNames;
        public CalledFunction CallFunc;
        public UserDefinedFunctionArgument UDFArgument;

        public SpCallArgment(IOperandTerm funcname, IOperandTerm[] subNames, IOperandTerm[] args)
        {
            FuncnameTerm = funcname;
            SubNames = subNames;
            RowArgs = args;
        }
    }

    internal sealed class SpForNextArgment : Argument
    {
        public readonly VariableTerm Cnt;
        public readonly IOperandTerm End;
        public readonly IOperandTerm Start;
        public readonly IOperandTerm Step;

        public SpForNextArgment(VariableTerm var, IOperandTerm start, IOperandTerm end, IOperandTerm step)
        {
            Cnt = var;
            Start = start;
            End = end;
            Step = step;
        }
    }

    internal sealed class SpPowerArgument : Argument
    {
        public readonly VariableTerm VariableDest;
        public readonly IOperandTerm X;
        public readonly IOperandTerm Y;

        public SpPowerArgument(VariableTerm var, IOperandTerm x, IOperandTerm y)
        {
            VariableDest = var;
            X = x;
            Y = y;
        }
    }

    internal sealed class CaseArgument : Argument
    {
        public readonly CaseExpression[] CaseExps;

        public CaseArgument(CaseExpression[] args)
        {
            CaseExps = args;
        }
    }

    internal sealed class PrintDataArgument : Argument
    {
        public readonly VariableTerm Var;

        public PrintDataArgument(VariableTerm var)
        {
            Var = var;
        }
    }

    internal sealed class StrDataArgument : Argument
    {
        public readonly VariableTerm Var;

        public StrDataArgument(VariableTerm var)
        {
            Var = var;
        }
    }

    internal sealed class MethodArgument : Argument
    {
        public readonly IOperandTerm MethodTerm;

        public MethodArgument(IOperandTerm method)
        {
            MethodTerm = method;
        }
    }

    internal sealed class BitArgument : Argument
    {
        public readonly IOperandTerm[] Term;
        public readonly VariableTerm VariableDest;

        public BitArgument(VariableTerm var, IOperandTerm[] termSrc)
        {
            VariableDest = var;
            Term = termSrc;
        }
    }

    internal sealed class SpVarSetArgument : Argument
    {
        public readonly IOperandTerm End;
        public readonly IOperandTerm Start;
        public readonly IOperandTerm Term;
        public readonly VariableTerm VariableDest;

        public SpVarSetArgument(VariableTerm var, IOperandTerm termSrc, IOperandTerm start, IOperandTerm end)
        {
            VariableDest = var;
            Term = termSrc;
            Start = start;
            End = end;
        }
    }

    internal sealed class SpCVarSetArgument : Argument
    {
        public readonly IOperandTerm End;
        public readonly IOperandTerm Index;
        public readonly IOperandTerm Start;
        public readonly IOperandTerm Term;
        public readonly VariableTerm VariableDest;

        public SpCVarSetArgument(VariableTerm var, IOperandTerm indexTerm, IOperandTerm termSrc, IOperandTerm start,
            IOperandTerm end)
        {
            VariableDest = var;
            Index = indexTerm;
            Term = termSrc;
            Start = start;
            End = end;
        }
    }

    internal sealed class SpButtonArgument : Argument
    {
        public readonly IOperandTerm ButtonWord;
        public readonly IOperandTerm PrintStrTerm;

        public SpButtonArgument(IOperandTerm p1, IOperandTerm p2)
        {
            PrintStrTerm = p1;
            ButtonWord = p2;
        }
    }


    internal sealed class SpColorArgument : Argument
    {
        public readonly IOperandTerm B;
        public readonly IOperandTerm G;
        public readonly IOperandTerm R;
        public readonly IOperandTerm RGB;

        public SpColorArgument(IOperandTerm r, IOperandTerm g, IOperandTerm b)
        {
            R = r;
            G = g;
            B = b;
        }

        public SpColorArgument(IOperandTerm rgb)
        {
            RGB = rgb;
        }
    }

    internal sealed class SpSplitArgument : Argument
    {
        public readonly VariableTerm Num;
        public readonly IOperandTerm Split;
        public readonly IOperandTerm TargetStr;
        public readonly VariableToken Var;

        public SpSplitArgument(IOperandTerm s1, IOperandTerm s2, VariableToken varId, VariableTerm num)
        {
            TargetStr = s1;
            Split = s2;
            Var = varId;
            Num = num;
        }
    }

    internal sealed class SpHtmlSplitArgument : Argument
    {
        public readonly VariableTerm Num;
        public readonly IOperandTerm TargetStr;
        public readonly VariableToken Var;

        public SpHtmlSplitArgument(IOperandTerm s1, VariableToken varId, VariableTerm num)
        {
            TargetStr = s1;
            Var = varId;
            Num = num;
        }
    }

    internal sealed class SpGetIntArgument : Argument
    {
        public readonly VariableTerm VarToken;

        public SpGetIntArgument(VariableTerm var)
        {
            VarToken = var;
        }
    }

    internal sealed class SpArrayControlArgument : Argument
    {
        public readonly IOperandTerm Num1;
        public readonly IOperandTerm Num2;
        public readonly VariableTerm VarToken;

        public SpArrayControlArgument(VariableTerm var, IOperandTerm num1, IOperandTerm num2)
        {
            VarToken = var;
            Num1 = num1;
            Num2 = num2;
        }
    }

    internal sealed class SpArrayShiftArgument : Argument
    {
        public readonly IOperandTerm Num1;
        public readonly IOperandTerm Num2;
        public readonly IOperandTerm Num3;
        public readonly IOperandTerm Num4;
        public readonly VariableTerm VarToken;

        public SpArrayShiftArgument(VariableTerm var, IOperandTerm num1, IOperandTerm num2, IOperandTerm num3,
            IOperandTerm num4)
        {
            VarToken = var;
            Num1 = num1;
            Num2 = num2;
            Num3 = num3;
            Num4 = num4;
        }
    }

    internal sealed class SpArraySortArgument : Argument
    {
        public readonly IOperandTerm Num1;
        public readonly IOperandTerm Num2;
        public readonly SortOrder Order;
        public readonly VariableTerm VarToken;

        public SpArraySortArgument(VariableTerm var, SortOrder order, IOperandTerm num1, IOperandTerm num2)
        {
            VarToken = var;
            Order = order;
            Num1 = num1;
            Num2 = num2;
        }
    }

    internal sealed class SpCopyArrayArgument : Argument
    {
        public readonly IOperandTerm VarName1;
        public readonly IOperandTerm VarName2;

        public SpCopyArrayArgument(IOperandTerm str1, IOperandTerm str2)
        {
            VarName1 = str1;
            VarName2 = str2;
        }
    }

    internal sealed class SpSaveVarArgument : Argument
    {
        public readonly IOperandTerm SavMes;
        public readonly IOperandTerm Term;
        public readonly VariableToken[] VarTokens;

        public SpSaveVarArgument(IOperandTerm term, IOperandTerm mes, VariableToken[] varTokens)
        {
            Term = term;
            SavMes = mes;
            VarTokens = varTokens;
        }
    }

    internal sealed class RefArgument : Argument
    {
        public readonly UserDefinedRefMethod RefMethodToken;

        public readonly ReferenceToken RefVarToken;
        public readonly CalledFunction SrcCalledFunction;
        public readonly UserDefinedRefMethod SrcRefMethodToken;
        public readonly IOperandTerm SrcTerm;
        public readonly VariableToken SrcVarToken;

        public RefArgument(UserDefinedRefMethod udrm, UserDefinedRefMethod src)
        {
            RefMethodToken = udrm;
            SrcRefMethodToken = src;
        }

        public RefArgument(UserDefinedRefMethod udrm, CalledFunction src)
        {
            RefMethodToken = udrm;
            SrcCalledFunction = src;
        }

        public RefArgument(UserDefinedRefMethod udrm, IOperandTerm src)
        {
            RefMethodToken = udrm;
            SrcTerm = src;
        }

        public RefArgument(ReferenceToken vt, VariableToken src)
        {
            RefVarToken = vt;
            SrcVarToken = src;
        }

        public RefArgument(ReferenceToken vt, IOperandTerm src)
        {
            RefVarToken = vt;
            SrcTerm = src;
        }
    }

    internal sealed class OneInputArgument : Argument
    {
        public readonly IOperandTerm Flag;
        public readonly IOperandTerm Term;

        public OneInputArgument(IOperandTerm term, IOperandTerm flag)
        {
            Term = term;
            Flag = flag;
        }
    }

    internal sealed class OneInputsArgument : Argument
    {
        public readonly IOperandTerm Flag;
        public readonly IOperandTerm Term;

        public OneInputsArgument(IOperandTerm term, IOperandTerm flag)
        {
            Term = term;
            Flag = flag;
        }
    }

    #region set系

    internal sealed class SpSetArgument : Argument
    {
        public readonly IOperandTerm Term;
        public readonly VariableTerm VariableDest;
        public bool AddConst = false;

        public SpSetArgument(VariableTerm var, IOperandTerm termSrc)
        {
            VariableDest = var;
            Term = termSrc;
        }
    }

    internal sealed class SpSetArrayArgument : Argument
    {
        public readonly long[] ConstIntList;
        public readonly string[] ConstStrList;
        public readonly IOperandTerm[] TermList;
        public readonly VariableTerm VariableDest;

        public SpSetArrayArgument(VariableTerm var, IOperandTerm[] termList, long[] constList)
        {
            VariableDest = var;
            TermList = termList;
            ConstIntList = constList;
        }

        public SpSetArrayArgument(VariableTerm var, IOperandTerm[] termList, string[] constList)
        {
            VariableDest = var;
            TermList = termList;
            ConstStrList = constList;
        }
    }

    #endregion
}