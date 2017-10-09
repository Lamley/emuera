using System;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Function
{
    internal abstract class SuperUserDefinedMethodTerm : IOperandTerm
    {
        protected SuperUserDefinedMethodTerm(Type returnType)
            : base(returnType)
        {
        }

        public abstract UserDefinedFunctionArgument Argument { get; }
        public abstract CalledFunction Call { get; }

        public override long GetIntValue(ExpressionMediator exm)
        {
            var term = exm.Process.GetValue(this);
            if (term == null)
                return 0;
            return term.Int;
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            var term = exm.Process.GetValue(this);
            if (term == null)
                return "";
            return term.Str;
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            var term = exm.Process.GetValue(this);
            if (term == null)
                if (GetOperandType() == typeof(long))
                    return new SingleTerm(0);
                else
                    return new SingleTerm("");
            return term;
        }
    }

    internal sealed class UserDefinedMethodTerm : SuperUserDefinedMethodTerm
    {
        private UserDefinedMethodTerm(UserDefinedFunctionArgument arg, Type returnType, CalledFunction call)
            : base(returnType)
        {
            Argument = arg;
            Call = call;
        }

        public override UserDefinedFunctionArgument Argument { get; }

        public override CalledFunction Call { get; }

        /// <summary>
        ///     エラーならnullを返す。
        /// </summary>
        public static UserDefinedMethodTerm Create(FunctionLabelLine targetLabel, IOperandTerm[] srcArgs,
            out string errMes)
        {
            var call = CalledFunction.CreateCalledFunctionMethod(targetLabel, targetLabel.LabelName);
            var arg = call.ConvertArg(srcArgs, out errMes);
            if (arg == null)
                return null;
            return new UserDefinedMethodTerm(arg, call.TopLabel.MethodType, call);
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            Argument.Restructure(exm);
            return this;
        }
    }

    internal sealed class UserDefinedRefMethodTerm : SuperUserDefinedMethodTerm
    {
        private readonly UserDefinedRefMethod reffunc;
        private readonly IOperandTerm[] srcArgs;

        public UserDefinedRefMethodTerm(UserDefinedRefMethod reffunc, IOperandTerm[] srcArgs)
            : base(reffunc.RetType)
        {
            this.srcArgs = srcArgs;
            this.reffunc = reffunc;
        }

        public override UserDefinedFunctionArgument Argument
        {
            get
            {
                if (reffunc.CalledFunction == null)
                    throw new CodeEE("何も参照していない関数参照" + reffunc.Name + "を呼び出しました");
                string errMes;
                var arg = reffunc.CalledFunction.ConvertArg(srcArgs, out errMes);
                if (arg == null)
                    throw new CodeEE(errMes);
                return arg;
            }
        }

        public override CalledFunction Call
        {
            get
            {
                if (reffunc.CalledFunction == null)
                    throw new CodeEE("何も参照していない関数参照" + reffunc.Name + "を呼び出しました");
                return reffunc.CalledFunction;
            }
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            for (var i = 0; i < srcArgs.Length; i++)
                if ((reffunc.ArgTypeList[i] & UserDifinedFunctionDataArgType.__Ref) ==
                    UserDifinedFunctionDataArgType.__Ref)
                    srcArgs[i].Restructure(exm);
                else
                    srcArgs[i] = srcArgs[i].Restructure(exm);
            return this;
        }
    }

    internal sealed class UserDefinedRefMethodNoArgTerm : SuperUserDefinedMethodTerm
    {
        private readonly UserDefinedRefMethod reffunc;

        public UserDefinedRefMethodNoArgTerm(UserDefinedRefMethod reffunc)
            : base(reffunc.RetType)
        {
            this.reffunc = reffunc;
        }

        public override UserDefinedFunctionArgument Argument =>
            throw new CodeEE("引数のない関数参照" + reffunc.Name + "を呼び出しました");

        public override CalledFunction Call => throw new CodeEE("引数のない関数参照" + reffunc.Name + "を呼び出しました");

        public string GetRefName()
        {
            if (reffunc.CalledFunction == null)
                return "";
            return reffunc.CalledFunction.TopLabel.LabelName;
        }

        public override long GetIntValue(ExpressionMediator exm)
        {
            throw new CodeEE("引数のない関数参照" + reffunc.Name + "を呼び出しました");
        }

        public override string GetStrValue(ExpressionMediator exm)
        {
            throw new CodeEE("引数のない関数参照" + reffunc.Name + "を呼び出しました");
        }

        public override SingleTerm GetValue(ExpressionMediator exm)
        {
            throw new CodeEE("引数のない関数参照" + reffunc.Name + "を呼び出しました");
        }

        public override IOperandTerm Restructure(ExpressionMediator exm)
        {
            return this;
        }
    }
}