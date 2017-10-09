﻿using System;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Variable
{
    internal class VariableTerm : IOperandTerm
    {
        private readonly IOperandTerm[] arguments;
        protected bool allArgIsConst;
        public VariableToken Identifier;
        protected long[] transporter;

        protected VariableTerm(VariableToken token) : base(token.VariableType)
        {
        }

        public VariableTerm(VariableToken token, IOperandTerm[] args)
            : base(token.VariableType)
        {
            Identifier = token;
            arguments = args;
            transporter = new long[arguments.Length];

            allArgIsConst = false;
            for (var i = 0; i < arguments.Length; i++)
            {
                if (!(arguments[i] is SingleTerm))
                    return;
                transporter[i] = ((SingleTerm) arguments[i]).Int;
            }
            allArgIsConst = true;
        }

        public bool isAllConst => allArgIsConst;
        public int getEl1forArg => (int) transporter[0];

        public long GetElementInt(int i, ExpressionMediator exm)
        {
            if (allArgIsConst)
                return transporter[i];
            return arguments[i].GetIntValue(exm);
        }

        public override long GetIntValue(ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                return Identifier.GetIntValue(exm, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                var ret = Identifier.GetStrValue(exm, transporter);
                if (ret == null)
                    return "";
                return ret;
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public virtual void SetValue(long value, ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                Identifier.SetValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public virtual void SetValue(string value, ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                Identifier.SetValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw e;
            }
        }

        public virtual void SetValue(long[] array, ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                Identifier.SetValue(array, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                {
                    Identifier.CheckElement(transporter);
                    throw new CodeEE("配列変数" + Identifier.Name + "の要素数を超えて代入しようとしました");
                }
                throw;
            }
        }

        public virtual void SetValue(string[] array, ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                Identifier.SetValue(array, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                {
                    Identifier.CheckElement(transporter);
                    throw new CodeEE("配列変数" + Identifier.Name + "の要素数を超えて代入しようとしました");
                }
                throw;
            }
        }

        public virtual long PlusValue(long value, ExpressionMediator exm)
        {
            try
            {
                if (!allArgIsConst)
                    for (var i = 0; i < arguments.Length; i++)
                        transporter[i] = arguments[i].GetIntValue(exm);
                return Identifier.PlusValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            if (Identifier.VariableType == typeof(long))
                return new SingleTerm(GetIntValue(exm));
            return new SingleTerm(GetStrValue(exm));
        }

        public virtual void SetValue(SingleTerm value, ExpressionMediator exm)
        {
            if (Identifier.VariableType == typeof(long))
                SetValue(value.Int, exm);
            else
                SetValue(value.Str, exm);
        }

        public virtual void SetValue(IOperandTerm value, ExpressionMediator exm)
        {
            if (Identifier.VariableType == typeof(long))
                SetValue(value.GetIntValue(exm), exm);
            else
                SetValue(value.GetStrValue(exm), exm);
        }

        public int GetLength()
        {
            return Identifier.GetLength();
        }

        public int GetLength(int dimension)
        {
            return Identifier.GetLength(dimension);
        }

        public int GetLastLength()
        {
            if (Identifier.IsArray1D)
                return Identifier.GetLength();
            if (Identifier.IsArray2D)
                return Identifier.GetLength(1);
            if (Identifier.IsArray3D)
                return Identifier.GetLength(2);
            return 0;
        }

        public virtual FixedVariableTerm GetFixedVariableTerm(ExpressionMediator exm)
        {
            if (!allArgIsConst)
                for (var i = 0; i < arguments.Length; i++)
                    transporter[i] = arguments[i].GetIntValue(exm);
            var fp = new FixedVariableTerm(Identifier);
            if (transporter.Length >= 1)
                fp.Index1 = transporter[0];
            if (transporter.Length >= 2)
                fp.Index2 = transporter[1];
            if (transporter.Length >= 3)
                fp.Index3 = transporter[2];
            return fp;
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            var canCheck = new bool[arguments.Length];
            allArgIsConst = true;
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = arguments[i].Restructure(exm);
                if (!(arguments[i] is SingleTerm))
                {
                    allArgIsConst = false;
                    canCheck[i] = false;
                }
                else
                {
                    //キャラクターデータの第1引数はこの時点でチェックしても意味がないのと
                    //ARG系は限界超えてても必要な数に拡張されるのでチェックしなくていい
                    if (i == 0 && Identifier.IsCharacterData || Identifier.Name == "ARG" || Identifier.Name == "ARGS")
                        canCheck[i] = false;
                    else
                        canCheck[i] = true;
                    //if (allArgIsConst)
                    //チェックのために値が必要
                    transporter[i] = arguments[i].GetIntValue(exm);
                }
            }
            if (!Identifier.IsReference)
                Identifier.CheckElement(transporter, canCheck);
            if (Identifier.CanRestructure && allArgIsConst)
                return GetValue(exm);
            if (allArgIsConst)
                return new FixedVariableTerm(Identifier, transporter);
            return this;
        }

        //以下添え字解析用の追加関数
        public bool checkSameTerm(VariableTerm term)
        {
            //添え字が全部定数があることがこの関数の前提(そもそもそうでないと使い道がない)
            if (!allArgIsConst)
                return false;
            if (Identifier.Name != term.Identifier.Name)
                return false;
            for (var i = 0; i < transporter.Length; i++)
                if (transporter[i] != term.transporter[i])
                    return false;
            return true;
        }

        public string GetFullString()
        {
            //添え字が全部定数があることがこの関数の前提(IOperandTermから変数名を取れないため)
            if (!allArgIsConst)
                return "";
            if (Identifier.IsArray1D)
                return Identifier.Name + ":" + transporter[0];
            if (Identifier.IsArray2D)
                return Identifier.Name + ":" + transporter[0] + ":" + transporter[1];
            if (Identifier.IsArray3D)
                return Identifier.Name + ":" + transporter[0] + ":" + transporter[1] + ":" + transporter[2];
            return Identifier.Name;
        }
    }


    internal sealed class FixedVariableTerm : VariableTerm
    {
        public FixedVariableTerm(VariableToken token)
            : base(token)
        {
            Identifier = token;
            transporter = new long[3];
            allArgIsConst = true;
        }

        public FixedVariableTerm(VariableToken token, long[] args)
            : base(token)
        {
            allArgIsConst = true;
            Identifier = token;
            transporter = new long[3];
            for (var i = 0; i < args.Length; i++)
                transporter[i] = args[i];
        }

        public long Index1
        {
            get => transporter[0];
            set => transporter[0] = value;
        }

        public long Index2
        {
            get => transporter[1];
            set => transporter[1] = value;
        }

        public long Index3
        {
            get => transporter[2];
            set => transporter[2] = value;
        }


        public override long GetIntValue(ExpressionMediator exm)
        {
            try
            {
                return Identifier.GetIntValue(exm, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            try
            {
                var ret = Identifier.GetStrValue(exm, transporter);
                if (ret == null)
                    return "";
                return ret;
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override void SetValue(long value, ExpressionMediator exm)
        {
            try
            {
                Identifier.SetValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override void SetValue(string value, ExpressionMediator exm)
        {
            try
            {
                Identifier.SetValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override long PlusValue(long value, ExpressionMediator exm)
        {
            try
            {
                return Identifier.PlusValue(value, transporter);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
                    Identifier.CheckElement(transporter);
                throw;
            }
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            if (Identifier.CanRestructure)
                return GetValue(exm);
            return this;
        }

        public void IsArrayRangeValid(long index1, long index2, string funcName, long i1, long i2)
        {
            Identifier.IsArrayRangeValid(transporter, index1, index2, funcName, i1, i2);
        }
    }


    /// <summary>
    ///     引数がない変数。値を参照、代入できない
    /// </summary>
    internal sealed class VariableNoArgTerm : VariableTerm
    {
        public VariableNoArgTerm(VariableToken token)
            : base(token)
        {
            Identifier = token;
            allArgIsConst = true;
        }

        public override long GetIntValue(ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(long value, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(string value, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(long[] array, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(string[] array, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override long PlusValue(long value, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(SingleTerm value, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override void SetValue(IOperandTerm value, ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override FixedVariableTerm GetFixedVariableTerm(ExpressionMediator exm)
        {
            throw new CodeEE("変数" + Identifier.Name + "に必要な引数が不足しています");
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            return this;
        }
    }
}