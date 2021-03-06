﻿using System.Collections.Generic;
using MinorShift.Emuera.GameProc;

namespace MinorShift.Emuera.GameData.Variable
{
    internal delegate LocalVariableToken CreateLocalVariableToken(VariableCode varCode, string subKey, int size);

    internal sealed class VariableLocal
    {
        private readonly int size;

        //VariableData varData;
        private readonly CreateLocalVariableToken creater;

        private readonly Dictionary<string, LocalVariableToken> localVarTokens =
            new Dictionary<string, LocalVariableToken>();

        private readonly VariableCode varCode;

        public VariableLocal(VariableCode varCode, int size, CreateLocalVariableToken creater)
        {
            this.size = size;
            this.varCode = varCode;
            this.creater = creater;
        }

        public bool IsForbid => size == 0;

        public LocalVariableToken GetExistLocalVariableToken(string subKey)
        {
            LocalVariableToken ret = null;
            if (localVarTokens.TryGetValue(subKey, out ret))
                return ret;
            return ret;
        }

        public int GetDefaultSize()
        {
            return size;
        }

        public LocalVariableToken GetNewLocalVariableToken(string subKey, FunctionLabelLine func)
        {
            LocalVariableToken ret = null;
            var newSize = 0;
            if (varCode == VariableCode.LOCAL)
                newSize = func.LocalLength;
            else if (varCode == VariableCode.LOCALS)
                newSize = func.LocalsLength;
            else if (varCode == VariableCode.ARG)
                newSize = func.ArgLength;
            else if (varCode == VariableCode.ARGS)
                newSize = func.ArgsLength;
            if (newSize > 0)
            {
                if (newSize < size && (varCode == VariableCode.ARG || varCode == VariableCode.ARGS))
                    newSize = size;
                ret = creater(varCode, subKey, newSize);
            }
            else if (newSize == 0)
            {
                ret = creater(varCode, subKey, size);
            }
            else
            {
                ret = creater(varCode, subKey, size);
                var line = GlobalStatic.Process.GetScaningLine();
                if (line != null)
                    if (!func.IsSystem)
                        ParserMediator.Warn(
                            "関数宣言に引数変数\"" + varCode + "\"が使われていない関数中で\"" + varCode +
                            "\"が使われています(関数の引数以外の用途に使うことは推奨されません。代わりに#DIMの使用を検討してください)", line, 1, false, false);
                    else
                        ParserMediator.Warn(
                            "システム関数" + func.LabelName + "中で\"" + varCode +
                            "\"が使われています(関数の引数以外の用途に使うことは推奨されません。代わりに#DIMの使用を検討してください)", line, 1, false, false);
                //throw new CodeEE("この関数に引数変数\"" + varCode + "\"は定義されていません");
            }
            localVarTokens.Add(subKey, ret);
            return ret;
        }

        public void ResizeLocalVariableToken(string subKey, int newSize)
        {
            LocalVariableToken ret = null;
            if (localVarTokens.TryGetValue(subKey, out ret))
            {
                if (size < newSize)
                    ret.resize(newSize);
                else
                    ret.resize(size);
            }
            else
            {
                if (newSize > size)
                    ret = creater(varCode, subKey, newSize);
                else if (newSize == 0)
                    ret = creater(varCode, subKey, size);
                else
                    return;
                localVarTokens.Add(subKey, ret);
            }
        }

        public void Clear()
        {
            localVarTokens.Clear();
        }

        public void SetDefault()
        {
            foreach (var pair in localVarTokens)
                pair.Value.SetDefault();
        }
    }
}