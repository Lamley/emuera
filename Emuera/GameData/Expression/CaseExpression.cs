using System;
using System.Reflection;

namespace MinorShift.Emuera.GameData.Expression
{
    //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
    [Obfuscation(Exclude = false)]
    internal enum CaseExpressionType
    {
        Normal = 1,
        To = 2,
        Is = 3
    }

    internal sealed class CaseExpression
    {
        public CaseExpressionType CaseType = CaseExpressionType.Normal;
        public IOperandTerm LeftTerm;

        public OperatorCode Operator;
        public IOperandTerm RightTerm;

        public Type GetOperandType()
        {
            if (LeftTerm != null)
                return LeftTerm.GetOperandType();
            return typeof(void);
        }

        public void Reduce(ExpressionMediator exm)
        {
            LeftTerm = LeftTerm.Restructure(exm);
            if (CaseType == CaseExpressionType.To)
                RightTerm = RightTerm.Restructure(exm);
        }

        public override string ToString()
        {
            switch (CaseType)
            {
                case CaseExpressionType.Normal:
                    return LeftTerm.ToString();
                case CaseExpressionType.Is:
                    return "Is " + Operator + " " + LeftTerm;
                case CaseExpressionType.To:
                    return LeftTerm + " To " + RightTerm;
            }

            return base.ToString();
        }

        public bool GetBool(long Is, ExpressionMediator exm)
        {
            if (CaseType == CaseExpressionType.To)
                return LeftTerm.GetIntValue(exm) <= Is && Is <= RightTerm.GetIntValue(exm);
            if (CaseType == CaseExpressionType.Is)
            {
                var term = OperatorMethodManager.ReduceBinaryTerm(Operator, new SingleTerm(Is), LeftTerm);
                return term.GetIntValue(exm) != 0;
            }
            return LeftTerm.GetIntValue(exm) == Is;
        }

        public bool GetBool(string Is, ExpressionMediator exm)
        {
            if (CaseType == CaseExpressionType.To)
                return string.Compare(LeftTerm.GetStrValue(exm), Is, Config.SCExpression) <= 0
                       && string.Compare(Is, RightTerm.GetStrValue(exm), Config.SCExpression) <= 0;
            if (CaseType == CaseExpressionType.Is)
            {
                var term = OperatorMethodManager.ReduceBinaryTerm(Operator, new SingleTerm(Is), LeftTerm);
                return term.GetIntValue(exm) != 0;
            }
            return LeftTerm.GetStrValue(exm) == Is;
        }
    }
}