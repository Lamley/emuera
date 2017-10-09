using System;
using System.IO;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera
{
    internal static class KeyMacro
    {
        public const string gID = "グループ";
        public const int MaxGroup = 10;
        public const int MaxFkey = 12;
        public const int MaxMacro = MaxFkey * MaxGroup;
        private static readonly string macroPath = Program.ExeDir + "macro.txt";

        /// <summary>
        ///     マクロの内容
        /// </summary>
        private static readonly string[] macro = new string[MaxMacro];

        /// <summary>
        ///     マクロキー
        /// </summary>
        private static readonly string[] macroName = new string[MaxMacro];

        private static readonly string[] groupName = new string[MaxGroup];
        private static bool isMacroChanged;

        static KeyMacro()
        {
            for (var g = 0; g < MaxGroup; g++)
            {
                groupName[g] = "マクログループ" + g + "に設定";
                for (var f = 0; f < MaxFkey; f++)
                {
                    var i = f + g * MaxFkey;
                    macro[i] = "";
                    if (g == 0)
                        macroName[i] = "マクロキーF" + (f + 1) + ":";
                    else
                        macroName[i] = "G" + g + ":マクロキーF" + (f + 1) + ":";
                }
            }
        }

        public static bool SaveMacro()
        {
            if (!isMacroChanged)
                return true;
            StreamWriter writer = null;

            try
            {
                writer = new StreamWriter(macroPath, false, Config.Encode);
                for (var g = 0; g < MaxGroup; g++)
                    writer.WriteLine(gID + g + ":" + groupName[g]);
                for (var i = 0; i < MaxMacro; i++)
                    writer.WriteLine(macroName[i] + macro[i]);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            return true;
        }

        public static void LoadMacroFile(string filename)
        {
            var eReader = new EraStreamReader(false);
            if (!eReader.Open(filename))
                return;
            try
            {
                string line = null;
                while ((line = eReader.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == ';')
                        continue;
                    if (line.StartsWith(gID))
                    {
                        if (line.Length < gID.Length + 4)
                            continue;
                        var num = line[gID.Length] - '0';
                        if (num < 0 || num > 9)
                            continue;
                        if (line[gID.Length + 1] != ':')
                            continue;
                        groupName[num] = line.Substring(gID.Length + 2);
                    }
                    for (var i = 0; i < MaxMacro; i++)
                        if (line.StartsWith(macroName[i]))
                        {
                            macro[i] = line.Substring(macroName[i].Length);
                            break;
                        }
                }
            }
            catch
            {
            }
            finally
            {
                eReader.Dispose();
            }
        }

        public static void SetMacro(int FkeyNum, int groupNum, string macroStr)
        {
            isMacroChanged = true;
            macro[FkeyNum + groupNum * MaxFkey] = macroStr;
        }

        public static string GetMacro(int FkeyNum, int groupNum)
        {
            return macro[FkeyNum + groupNum * MaxFkey];
        }

        public static string GetGroupName(int groupNum)
        {
            return groupName[groupNum];
        }
    }
}