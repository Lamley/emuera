using MinorShift.Emuera.GameData.Expression;

namespace MinorShift.Emuera.Sub
{
    internal abstract class Word
    {
        public bool IsMacro;
        public abstract char Type { get; }

        public virtual void SetIsMacro()
        {
            IsMacro = true;
        }
    }

    internal sealed class NullWord : Word
    {
        public override char Type => '\0';

        public override string ToString()
        {
            return "/null/";
        }
    }

    internal sealed class IdentifierWord : Word
    {
        public IdentifierWord(string s)
        {
            Code = s;
        }

        public string Code { get; }

        public override char Type => 'A';

        public override string ToString()
        {
            return Code;
        }
    }

    internal sealed class LiteralIntegerWord : Word
    {
        public LiteralIntegerWord(long i)
        {
            Int = i;
        }

        public long Int { get; }

        public override char Type => '0';

        public override string ToString()
        {
            return Int.ToString();
        }
    }

    internal sealed class LiteralStringWord : Word
    {
        public LiteralStringWord(string s)
        {
            Str = s;
        }

        public string Str { get; }

        public override char Type => '\"';

        public override string ToString()
        {
            return "\"" + Str + "\"";
        }
    }


    internal sealed class OperatorWord : Word
    {
        public OperatorWord(OperatorCode op)
        {
            Code = op;
        }

        public OperatorCode Code { get; }

        public override char Type => '=';

        public override string ToString()
        {
            return Code.ToString();
        }
    }

    internal sealed class SymbolWord : Word
    {
        private readonly char code;

        public SymbolWord(char c)
        {
            code = c;
        }

        public override char Type => code;

        public override string ToString()
        {
            return code.ToString();
        }
    }

    internal sealed class StrFormWord : Word
    {
        public StrFormWord(string[] s, SubWord[] SWT)
        {
            Strs = s;
            SubWords = SWT;
        }

        public string[] Strs { get; }

        public SubWord[] SubWords { get; }

        public override char Type //@はSymbolがつかっちゃった
            => 'F';

        public override void SetIsMacro()
        {
            IsMacro = true;
            foreach (var subword in SubWords)
                subword.SetIsMacro();
        }
    }


    internal sealed class TermWord : Word
    {
        public TermWord(IOperandTerm term)
        {
            Term = term;
        }

        public IOperandTerm Term { get; }

        public override char Type => 'T';
    }

    internal sealed class MacroWord : Word
    {
        public MacroWord(int num)
        {
            Number = num;
        }

        public int Number { get; }

        public override char Type => 'M';

        public override string ToString()
        {
            return "Arg" + Number;
        }
    }
}