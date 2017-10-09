using System;
using System.Collections.Generic;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    ///     命令文1行に相当する抽象クラス
    /// </summary>
    internal abstract class LogicalLine
    {
        protected string errMes = "";

        protected bool isError;

        //LogicalLine prevLine;

        protected ScriptPosition position;

        public ScriptPosition Position => position;

        public FunctionLabelLine ParentLabelLine { get; set; }

        public LogicalLine NextLine { get; set; }

        public virtual string ErrMes
        {
            get => errMes;
            set => errMes = value;
        }

        public virtual bool IsError
        {
            get => isError;
            set => isError = value;
        }

        public override string ToString()
        {
            if (position == null)
                return base.ToString();
            return string.Format("{0}:{1}:{2}", position.Filename, position.LineNo, position.RowLine);
        }
    }

    ///// <summary>
    ///// コメント行。
    ///// </summary>
    //internal sealed class CommentLine : LogicalLine
    //{
    //    public CommentLine(ScriptPosition thePosition, string str)
    //    {
    //        base.position = thePosition;
    //        //comment = str;
    //    }
    //    //string comment;
    //    public override bool IsError
    //    {
    //        get { return false; }
    //    }
    //}

    /// <summary>
    ///     無効な行。
    /// </summary>
    internal sealed class InvalidLine : LogicalLine
    {
        public InvalidLine(ScriptPosition thePosition, string err)
        {
            position = thePosition;
            errMes = err;
        }

        public override bool IsError => true;
    }

    /// <summary>
    ///     命令文
    /// </summary>
    internal sealed class InstructionLine : LogicalLine
    {
        private StringStream argprimitive;

        private WordCollection assigndest;

        //TRYCALLLIST系が使う
        public List<InstructionLine> callList = null;

        //PRINTDATA文のみが使う。
        public List<List<InstructionLine>> dataList = null;

        //IF文とSELECT文のみが使う。
        public List<InstructionLine> IfCaseList = null;

        public InstructionLine(ScriptPosition thePosition, FunctionIdentifier theFunc, StringStream theArgPrimitive)
        {
            position = thePosition;
            Function = theFunc;
            argprimitive = theArgPrimitive;
        }

        public InstructionLine(ScriptPosition thePosition, FunctionIdentifier functionIdentifier, OperatorCode assignOP,
            WordCollection dest, StringStream theArgPrimitive)
        {
            position = thePosition;
            Function = functionIdentifier;
            AssignOperator = assignOP;
            assigndest = dest;
            argprimitive = theArgPrimitive;
        }

        public OperatorCode AssignOperator { get; }

        public FunctionCode FunctionCode => Function.Code;

        public FunctionIdentifier Function { get; }

        public Argument Argument { get; set; }

        /// <summary>
        ///     繰り返しの終了を記憶する
        /// </summary>
        public long LoopEnd { get; set; } = 0;

        /// <summary>
        ///     繰り返しにつかう変数を記憶する
        /// </summary>
        public VariableTerm LoopCounter { get; set; }

        /// <summary>
        ///     繰り返しのたびに増加する値を記憶する
        /// </summary>
        public long LoopStep { get; set; }

        public LogicalLine JumpTo { get; set; } = null;

        public LogicalLine JumpToEndCatch { get; set; } = null;

        public StringStream PopArgumentPrimitive()
        {
            var ret = argprimitive;
            argprimitive = null;
            return ret;
        }

        public WordCollection PopAssignmentDestStr()
        {
            var ret = assigndest;
            assigndest = null;
            return ret;
        }
    }

    /// <summary>
    ///     ファイルの始端と終端
    /// </summary>
    internal sealed class NullLine : LogicalLine
    {
    }

    /// <summary>
    ///     ラベルがエラーになっている関数行専用のクラス
    /// </summary>
    internal sealed class InvalidLabelLine : FunctionLabelLine
    {
        public InvalidLabelLine(ScriptPosition thePosition, string labelname, string err)
        {
            position = thePosition;
            LabelName = labelname;
            errMes = err;
            IsSingle = false;
            Index = -1;
            Depth = -1;
            IsMethod = false;
            MethodType = typeof(void);
        }

        public override bool IsError => true;
    }

    /// <summary>
    ///     @で始まるラベル行
    /// </summary>
    internal class FunctionLabelLine : LogicalLine, IComparable<FunctionLabelLine>
    {
        private WordCollection wc;

        protected FunctionLabelLine()
        {
        }

        public FunctionLabelLine(ScriptPosition thePosition, string labelname, WordCollection wc)
        {
            position = thePosition;
            LabelName = labelname;
            IsSingle = false;
            hasPrivDynamicVar = false;
            Index = -1;
            Depth = -1;
            LocalLength = 0;
            LocalsLength = 0;
            ArgLength = 0;
            ArgsLength = 0;
            IsMethod = false;
            MethodType = typeof(void);
            this.wc = wc;

            //ArgOptional = true;
            //ArgAutoConvert = true;
        }

        public string LabelName { get; protected set; }
        public bool IsEvent { get; set; }
        public bool IsSystem { get; set; }
        public bool IsSingle { get; set; }
        public bool IsPri { get; set; }
        public bool IsLater { get; set; }
        public bool IsOnly { get; set; }
        public bool hasPrivDynamicVar { get; set; }
        public int LocalLength { get; set; }
        public int LocalsLength { get; set; }
        public int ArgLength { get; set; }
        public int ArgsLength { get; set; }

        //public bool ArgOptional { get; set; }
        //public bool ArgAutoConvert { get; set; }

        public bool IsMethod { get; set; }
        public Type MethodType { get; set; }
        public VariableTerm[] Arg { get; set; }

        public SingleTerm[] Def { get; set; }

        //public SingleTerm[] SubNames { get; set; }
        public int Depth { get; set; }

        public WordCollection PopRowArgs()
        {
            var ret = wc;
            wc = null;
            return ret;
        }

        #region IComparable<FunctionLabelLine> メンバ

        //ソート用情報
        public int Index { get; set; }

        public int FileIndex { get; set; }

        public int CompareTo(FunctionLabelLine other)
        {
            if (FileIndex != other.FileIndex)
                return FileIndex.CompareTo(other.FileIndex);
            //position == nullであるLine(デバッグコマンドなど)をSortすることはないはず
            if (position.LineNo != other.position.LineNo)
                return position.LineNo.CompareTo(other.position.LineNo);
            return Index.CompareTo(other.Index);
        }

        #endregion

        #region private変数

        private readonly Dictionary<string, UserDefinedVariableToken> privateVar =
            new Dictionary<string, UserDefinedVariableToken>();

        internal bool AddPrivateVariable(UserDefinedVariableData data)
        {
            if (privateVar.ContainsKey(data.Name))
                return false;
            var var = GlobalStatic.VariableData.CreatePrivateVariable(data);
            privateVar.Add(data.Name, var);
            //静的な変数のみの場合は関数呼び出し時に何もする必要がない
            if (!data.Static)
                hasPrivDynamicVar = true;
            return true;
        }

        internal UserDefinedVariableToken GetPrivateVariable(string key)
        {
            UserDefinedVariableToken var = null;
            privateVar.TryGetValue(key, out var);
            return var;
        }

        /// <summary>
        ///     引数の値の確定後、引数の代入より前に呼ぶこと
        /// </summary>
        internal void In()
        {
#if DEBUG
            GlobalStatic.StackList.Add(this);
#endif
            foreach (var var in privateVar.Values)
                if (!var.IsStatic)
                    var.In();
        }

        internal void Out()
        {
#if DEBUG
            GlobalStatic.StackList.Remove(this);
#endif
            foreach (var var in privateVar.Values)
                if (!var.IsStatic)
                    var.Out();
        }

        #endregion
    }

    /// <summary>
    ///     $で始まるラベル行
    /// </summary>
    internal sealed class GotoLabelLine : LogicalLine, IEqualityComparer<GotoLabelLine>
    {
        public GotoLabelLine(ScriptPosition thePosition, string labelname)
        {
            position = thePosition;
            LabelName = labelname;
        }

        public string LabelName { get; } = "";

        #region IEqualityComparer<GotoLabelLine> メンバ

        public bool Equals(GotoLabelLine x, GotoLabelLine y)
        {
            if (x == null || y == null)
                return false;
            return x.ParentLabelLine == y.ParentLabelLine && x.LabelName == y.LabelName;
        }

        public int GetHashCode(GotoLabelLine obj)
        {
            return LabelName.GetHashCode() ^ ParentLabelLine.GetHashCode();
        }

        #endregion
    }
}