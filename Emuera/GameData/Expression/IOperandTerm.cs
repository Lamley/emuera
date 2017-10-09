using System;

namespace MinorShift.Emuera.GameData.Expression
{
    internal abstract class IOperandTerm
    {
        private readonly Type type;

        public IOperandTerm(Type t)
        {
            type = t;
        }

        public bool IsInteger => type == typeof(long);

        public bool IsString => type == typeof(string);

        public Type GetOperandType()
        {
            return type;
        }

        public virtual long GetIntValue(ExpressionMediator exm)
        {
            return 0;
        }

        public virtual string GetStrValue(ExpressionMediator exm)
        {
            return "";
        }

        public virtual SingleTerm GetValue(ExpressionMediator exm)
        {
            if (type == typeof(long))
                return new SingleTerm(0);
            return new SingleTerm("");
        }

        /// <summary>
        ///     定数を解体して可能ならSingleTerm化する
        ///     defineの都合上、2回以上呼ばれる可能性がある
        /// </summary>
        public virtual IOperandTerm Restructure(ExpressionMediator exm)
        {
            return this;
        }
    }
}