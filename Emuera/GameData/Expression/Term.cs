namespace MinorShift.Emuera.GameData.Expression
{
    internal sealed class NullTerm : IOperandTerm
    {
        public NullTerm(long i)
            : base(typeof(long))
        {
        }

        public NullTerm(string s)
            : base(typeof(string))
        {
        }
    }

    /// <summary>
    ///     項。一単語だけ。
    /// </summary>
    internal sealed class SingleTerm : IOperandTerm
    {
        public SingleTerm(bool i)
            : base(typeof(long))
        {
            if (i)
                Int = 1;
            else
                Int = 0;
        }

        public SingleTerm(long i)
            : base(typeof(long))
        {
            Int = i;
        }

        public SingleTerm(string s)
            : base(typeof(string))
        {
            Str = s;
        }

        public string Str { get; }

        public long Int { get; }

        public override long GetIntValue(ExpressionMediator exm)
        {
            return Int;
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            return Str;
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            return this;
        }

        public override string ToString()
        {
            if (GetOperandType() == typeof(long))
                return Int.ToString();
            if (GetOperandType() == typeof(string))
                return Str;
            return base.ToString();
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            return this;
        }
    }

    /// <summary>
    ///     項。一単語だけ。
    /// </summary>
    internal sealed class StrFormTerm : IOperandTerm
    {
        public StrFormTerm(StrForm sf)
            : base(typeof(string))
        {
            StrForm = sf;
        }

        public StrForm StrForm { get; }

        public override string GetStrValue(ExpressionMediator exm)
        {
            return StrForm.GetString(exm);
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            return new SingleTerm(StrForm.GetString(exm));
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            StrForm.Restructure(exm);
            if (StrForm.IsConst)
                return new SingleTerm(StrForm.GetString(exm));
            var term = StrForm.GetIOperandTerm();
            if (term != null)
                return term;
            return this;
        }
    }
}