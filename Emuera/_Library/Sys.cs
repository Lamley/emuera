using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace MinorShift._Library
{
    public static class Sys
    {
        /// <summary>
        ///     実行ファイルのパス
        /// </summary>
        public static readonly string ExePath;

        /// <summary>
        ///     実行ファイルのディレクトリ。最後に\を付けたstring
        /// </summary>
        public static readonly string ExeDir;

        /// <summary>
        ///     実行ファイルの名前。ディレクトリなし
        /// </summary>
        public static readonly string ExeName;

        static Sys()
        {
            ExePath = Application.ExecutablePath;
            ExeDir = Path.GetDirectoryName(ExePath) + "\\";
            ExeName = Path.GetFileName(ExePath);
        }

        /// <summary>
        ///     2重起動防止。既に同名exeが実行されているならばtrueを返す
        /// </summary>
        /// <returns></returns>
        public static bool PrevInstance()
        {
            var thisProcessName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(thisProcessName).Length > 1)
                return true;
            return false;
        }
    }
}