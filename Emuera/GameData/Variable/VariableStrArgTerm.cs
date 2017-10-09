using System.Collections.Generic;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Variable
{
    //変数の引数のうち文字列型のもの。
    internal sealed class VariableStrArgTerm : IOperandTerm
    {
        private readonly int index;
        private readonly VariableCode parentCode;
        private Dictionary<string, int> dic;
        private string errPos;
        private IOperandTerm strTerm;

        public VariableStrArgTerm(VariableCode code, IOperandTerm strTerm, int index)
            : base(typeof(long))
        {
            this.strTerm = strTerm;
            parentCode = code;
            this.index = index;
        }

        public override long GetIntValue(ExpressionMediator exm)
        {
            if (dic == null)
                dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index);
            var key = strTerm.GetStrValue(exm);
            if (key == "")
                throw new CodeEE("キーワードを空には出来ません");
            int i;
            if (!dic.TryGetValue(key, out i))
                if (errPos == null)
                    throw new CodeEE("配列変数" + parentCode + "の要素を文字列で指定することはできません");
                else
                    throw new CodeEE(errPos + "の中に\"" + key + "\"の定義がありません");
            return i;
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            if (dic == null)
                dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index);
            strTerm = strTerm.Restructure(exm);
            if (!(strTerm is SingleTerm))
                return this;
            return new SingleTerm(GetIntValue(exm));
        }
    }
}