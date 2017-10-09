﻿using System;
using System.Collections.Generic;

namespace MinorShift.Emuera.Sub
{
    [Serializable]
    internal abstract class EmueraException : ApplicationException
    {
        public ScriptPosition Position;

        protected EmueraException(string errormes, ScriptPosition position)
            : base(errormes)
        {
            Position = position;
        }

        protected EmueraException(string errormes)
            : base(errormes)
        {
            Position = null;
        }
    }

    /// <summary>
    ///     emuera本体に起因すると思われるエラー
    /// </summary>
    [Serializable]
    internal sealed class ExeEE : EmueraException
    {
        public ExeEE(string errormes)
            : base(errormes)
        {
        }

        public ExeEE(string errormes, ScriptPosition position)
            : base(errormes, position)
        {
        }
    }

    /// <summary>
    ///     スクリプト側に起因すると思われるエラー
    /// </summary>
    [Serializable]
    internal class CodeEE : EmueraException
    {
        public CodeEE(string errormes, ScriptPosition position)
            : base(errormes, position)
        {
        }

        public CodeEE(string errormes)
            : base(errormes)
        {
        }
    }

    /// <summary>
    ///     未実装エラー
    /// </summary>
    [Serializable]
    internal sealed class NotImplCodeEE : CodeEE
    {
        public NotImplCodeEE(ScriptPosition position)
            : base("この機能は現バージョンでは使えません", position)
        {
        }

        public NotImplCodeEE()
            : base("この機能は現バージョンでは使えません")
        {
        }
    }

    /// <summary>
    ///     Save, Load中のエラー
    /// </summary>
    [Serializable]
    internal sealed class FileEE : EmueraException
    {
        public FileEE(string errormes)
            : base(errormes)
        {
        }
    }

    /// <summary>
    ///     エラー箇所を表示するための位置データ。整形前のデータなのでエラー表示以外の理由で参照するべきではない。
    /// </summary>
    internal sealed class ScriptPosition : IEquatable<ScriptPosition>, IEqualityComparer<ScriptPosition>
    {
        public readonly string Filename;
        public readonly int LineNo;
        public readonly string RowLine;

        public ScriptPosition(string srcLine)
        {
            LineNo = -1;
            RowLine = srcLine;
            Filename = "";
        }

        public ScriptPosition(string srcFile, int srcLineNo, string srcLine)
        {
            LineNo = srcLineNo;
            RowLine = srcLine;
            if (srcFile == null)
                Filename = "";
            else
                Filename = srcFile;
        }

        #region IEquatable<ScriptPosition> メンバ

        public bool Equals(ScriptPosition other)
        {
            return Equals(this, other);
        }

        #endregion

        public override string ToString()
        {
            if (LineNo == -1)
                return base.ToString();
            return Filename + ":" + LineNo;
        }

        #region IEqualityComparer<ScriptPosition> メンバ

        public bool Equals(ScriptPosition x, ScriptPosition y)
        {
            if (x == null || y == null)
                return false;
            return x.Filename == y.Filename && x.LineNo == y.LineNo;
        }

        public int GetHashCode(ScriptPosition obj)
        {
            return Filename.GetHashCode() ^ LineNo.GetHashCode();
        }

        #endregion
    }
}