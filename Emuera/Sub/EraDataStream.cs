using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MinorShift.Emuera.Sub
{
    //難読化用属性。enum.ToString()やenum.Parse()を行うなら(Exclude=true)にすること。
    [Obfuscation(Exclude = false)]
    internal enum EraDataState
    {
        OK = 0, //ロード可能
        FILENOTFOUND = 1, //ファイルが存在せず
        GAME_ERROR = 2, //ゲームが違う
        VIRSION_ERROR = 3, //バージョンが違う
        ETC_ERROR = 4 //その他のエラー
    }

    internal sealed class EraDataResult
    {
        public string DataMes = "";
        public EraDataState State = EraDataState.OK;
    }

    /// <summary>
    ///     セーブデータ読み取り
    /// </summary>
    internal sealed class EraDataReader : IDisposable
    {
        public const string FINISHER = "__FINISHED";
        public const string EMU_1700_START = "__EMUERA_STRAT__";
        public const string EMU_1708_START = "__EMUERA_1708_STRAT__";
        public const string EMU_1729_START = "__EMUERA_1729_STRAT__";
        public const string EMU_1803_START = "__EMUERA_1803_STRAT__";
        public const string EMU_1808_START = "__EMUERA_1808_STRAT__";
        public const string EMU_SEPARATOR = "__EMU_SEPARATOR__";
        private FileStream file;

        private StreamReader reader;

        //public EraDataReader(string filepath)
        //{
        //    file = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        //    reader = new StreamReader(file, Config.Encode);
        //}
        public EraDataReader(FileStream file)
        {
            this.file = file;
            file.Seek(0, SeekOrigin.Begin);
            reader = new StreamReader(file, Config.Encode);
        }

        #region IDisposable メンバ

        public void Dispose()
        {
            if (reader != null)
                reader.Close();
            else if (file != null)
                file.Close();
            file = null;
            reader = null;
        }

        #endregion

        public void Close()
        {
            Dispose();
        }

        #region eramaker

        public string ReadString()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var str = reader.ReadLine();
            if (str == null)
                throw new FileEE("読み取るべき文字列がありません");
            return str;
        }

        public long ReadInt64()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            long ret = 0;
            var str = reader.ReadLine();
            if (str == null)
                throw new FileEE("読み取るべき数値がありません");
            if (!long.TryParse(str, out ret))
                throw new FileEE("数値として認識できません");
            return ret;
        }


        public void ReadInt64Array(long[] array)
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var i = -1;
            string str = null;
            long integer = 0;
            i = -1;
            while (true)
            {
                i++;
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    break;
                if (i >= array.Length) //配列を超えて保存されていても動じないで読み飛ばす。
                    continue;
                if (!long.TryParse(str, out integer))
                    throw new FileEE("数値として認識できません");
                array[i] = integer;
            }
            for (; i < array.Length; i++) //保存されている値が無いなら0に初期化
                array[i] = 0;
        }

        public void ReadStringArray(string[] array)
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var i = -1;
            string str = null;
            i = -1;
            while (true)
            {
                i++;
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    break;
                if (i >= array.Length) //配列を超えて保存されていても動じないで読み飛ばす。
                    continue;
                array[i] = str;
            }
            for (; i < array.Length; i++) //保存されている値が無いなら""に初期化
                array[i] = "";
        }

        #endregion

        #region Emuera

        public int DataVersion { get; private set; } = -1;

        public bool SeekEmuStart()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            if (reader.EndOfStream)
                return false;
            while (true)
            {
                var str = reader.ReadLine();
                if (str == null)
                    return false;
                if (str.Equals(EMU_1700_START, StringComparison.Ordinal))
                {
                    DataVersion = 1700;
                    return true;
                }
                if (str.Equals(EMU_1708_START, StringComparison.Ordinal))
                {
                    DataVersion = 1708;
                    return true;
                }
                if (str.Equals(EMU_1729_START, StringComparison.Ordinal))
                {
                    DataVersion = 1729;
                    return true;
                }
                if (str.Equals(EMU_1803_START, StringComparison.Ordinal))
                {
                    DataVersion = 1803;
                    return true;
                }
                if (str.Equals(EMU_1808_START, StringComparison.Ordinal))
                {
                    DataVersion = 1808;
                    return true;
                }
            }
        }

        public Dictionary<string, string> ReadStringExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var strList = new Dictionary<string, string>();
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var index = str.IndexOf(':');
                if (index < 0)
                    throw new FileEE("セーブデータの形式が不正です");
                var key = str.Substring(0, index);
                var value = str.Substring(index + 1, str.Length - index - 1);
                if (!strList.ContainsKey(key))
                    strList.Add(key, value);
            }
            return strList;
        }

        public Dictionary<string, long> ReadInt64Extended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var intList = new Dictionary<string, long>();
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var index = str.IndexOf(':');
                if (index < 0)
                    throw new FileEE("セーブデータの形式が不正です");
                var key = str.Substring(0, index);
                var valueStr = str.Substring(index + 1, str.Length - index - 1);
                long value = 0;
                if (!long.TryParse(valueStr, out value))
                    throw new FileEE("数値として認識できません");
                if (!intList.ContainsKey(key))
                    intList.Add(key, value);
            }
            return intList;
        }

        public Dictionary<string, List<long>> ReadInt64ArrayExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<long>>();
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var key = str;
                var valueList = new List<long>();
                while (true)
                {
                    str = reader.ReadLine();
                    if (str == null)
                        throw new FileEE("予期しないセーブデータの終端です");
                    if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                        throw new FileEE("セーブデータの形式が不正です");
                    if (str.Equals(FINISHER, StringComparison.Ordinal))
                        break;
                    long value = 0;
                    if (!long.TryParse(str, out value))
                        throw new FileEE("数値として認識できません");
                    valueList.Add(value);
                }
                if (!ret.ContainsKey(key))
                    ret.Add(key, valueList);
            }
            return ret;
        }

        public Dictionary<string, List<string>> ReadStringArrayExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<string>>();
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var key = str;
                var valueList = new List<string>();
                while (true)
                {
                    str = reader.ReadLine();
                    if (str == null)
                        throw new FileEE("予期しないセーブデータの終端です");
                    if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                        throw new FileEE("セーブデータの形式が不正です");
                    if (str.Equals(FINISHER, StringComparison.Ordinal))
                        break;
                    valueList.Add(str);
                }
                if (!ret.ContainsKey(key))
                    ret.Add(key, valueList);
            }
            return ret;
        }

        public Dictionary<string, List<long[]>> ReadInt64Array2DExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<long[]>>();
            if (DataVersion < 1708)
                return ret;
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var key = str;
                var valueList = new List<long[]>();
                while (true)
                {
                    str = reader.ReadLine();
                    if (str == null)
                        throw new FileEE("予期しないセーブデータの終端です");
                    if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                        throw new FileEE("セーブデータの形式が不正です");
                    if (str.Equals(FINISHER, StringComparison.Ordinal))
                        break;
                    if (str.Length == 0)
                    {
                        valueList.Add(new long[0]);
                        continue;
                    }
                    var tokens = str.Split(',');
                    var intTokens = new long[tokens.Length];

                    for (var x = 0; x < tokens.Length; x++)
                        if (!long.TryParse(tokens[x], out intTokens[x]))
                            throw new FileEE(tokens[x] + "は数値として認識できません");
                    valueList.Add(intTokens);
                }
                if (!ret.ContainsKey(key))
                    ret.Add(key, valueList);
            }
            return ret;
        }

        public Dictionary<string, List<string[]>> ReadStringArray2DExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<string[]>>();
            if (DataVersion < 1708)
                return ret;
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                throw new FileEE("StringArray2Dのロードには対応していません");
            }
            return ret;
        }

        public Dictionary<string, List<List<long[]>>> ReadInt64Array3DExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<List<long[]>>>();
            if (DataVersion < 1729)
                return ret;
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                var key = str;
                var valueList = new List<List<long[]>>();
                while (true)
                {
                    str = reader.ReadLine();
                    if (str == null)
                        throw new FileEE("予期しないセーブデータの終端です");
                    if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                        throw new FileEE("セーブデータの形式が不正です");
                    if (str.Equals(FINISHER, StringComparison.Ordinal))
                        break;
                    if (str.Contains("{"))
                    {
                        var tokenList = new List<long[]>();
                        while (true)
                        {
                            str = reader.ReadLine();
                            if (str == "}")
                                break;
                            if (str.Length == 0)
                            {
                                tokenList.Add(new long[0]);
                                continue;
                            }
                            var tokens = str.Split(',');
                            var intTokens = new long[tokens.Length];

                            for (var x = 0; x < tokens.Length; x++)
                                if (!long.TryParse(tokens[x], out intTokens[x]))
                                    throw new FileEE(tokens[x] + "は数値として認識できません");
                            tokenList.Add(intTokens);
                        }
                        valueList.Add(tokenList);
                    }
                }
                if (!ret.ContainsKey(key))
                    ret.Add(key, valueList);
            }
            return ret;
        }

        public Dictionary<string, List<List<string[]>>> ReadStringArray3DExtended()
        {
            if (reader == null)
                throw new FileEE("無効なストリームです");
            var ret = new Dictionary<string, List<List<string[]>>>();
            if (DataVersion < 1729)
                return ret;
            string str = null;
            while (true)
            {
                str = reader.ReadLine();
                if (str == null)
                    throw new FileEE("予期しないセーブデータの終端です");
                if (str.Equals(FINISHER, StringComparison.Ordinal))
                    throw new FileEE("セーブデータの形式が不正です");
                if (str.Equals(EMU_SEPARATOR, StringComparison.Ordinal))
                    break;
                throw new FileEE("StringArray2Dのロードには対応していません");
            }
            return ret;
        }

        #endregion
    }

    /// <summary>
    ///     セーブデータ書き込み
    /// </summary>
    internal sealed class EraDataWriter : IDisposable
    {
        public const string FINISHER = EraDataReader.FINISHER;
        public const string EMU_START = EraDataReader.EMU_1808_START;
        public const string EMU_SEPARATOR = EraDataReader.EMU_SEPARATOR;
        private FileStream file;

        private StreamWriter writer;

        //public EraDataWriter(string filepath)
        //{
        //    FileStream file = new FileStream(filepath, FileMode.Create, FileAccess.Write);
        //    writer = new StreamWriter(file, Config.SaveEncode);
        //    //writer = new StreamWriter(filepath, false, Config.SaveEncode);
        //}
        public EraDataWriter(FileStream file)
        {
            this.file = file;
            writer = new StreamWriter(file, Config.SaveEncode);
        }

        #region IDisposable メンバ

        public void Dispose()
        {
            if (writer != null)
                writer.Close();
            else if (file != null)
                file.Close();
            writer = null;
            file = null;
        }

        #endregion

        public void Close()
        {
            Dispose();
        }

        #region eramaker

        public void Write(long integer)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            writer.WriteLine(integer.ToString());
        }


        public void Write(string str)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (str == null)
                writer.WriteLine("");
            else
                writer.WriteLine(str);
        }

        public void Write(long[] array)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var count = -1;
            for (var i = 0; i < array.Length; i++)
                if (array[i] != 0)
                    count = i;
            count++;
            for (var i = 0; i < count; i++)
                writer.WriteLine(array[i].ToString());
            writer.WriteLine(FINISHER);
        }

        public void Write(string[] array)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var count = -1;
            for (var i = 0; i < array.Length; i++)
                if (!string.IsNullOrEmpty(array[i]))
                    count = i;
            count++;
            for (var i = 0; i < count; i++)
                if (array[i] == null)
                    writer.WriteLine("");
                else
                    writer.WriteLine(array[i]);
            writer.WriteLine(FINISHER);
        }

        #endregion

        #region Emuera

        public void EmuStart()
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            writer.WriteLine(EMU_START);
        }

        public void EmuSeparete()
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            writer.WriteLine(EMU_SEPARATOR);
        }

        public void WriteExtended(string key, long value)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (value == 0)
                return;
            writer.WriteLine("{0}:{1}", key, value);
        }

        public void WriteExtended(string key, string value)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (string.IsNullOrEmpty(value))
                return;
            writer.WriteLine("{0}:{1}", key, value);
        }


        public void WriteExtended(string key, long[] array)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var count = -1;
            for (var i = 0; i < array.Length; i++)
                if (array[i] != 0)
                    count = i;
            count++;
            if (count == 0)
                return;
            writer.WriteLine(key);
            for (var i = 0; i < count; i++)
                writer.WriteLine(array[i].ToString());
            writer.WriteLine(FINISHER);
        }

        public void WriteExtended(string key, string[] array)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array == null)
                throw new FileEE("無効な配列が渡されました");
            var count = -1;
            for (var i = 0; i < array.Length; i++)
                if (!string.IsNullOrEmpty(array[i]))
                    count = i;
            count++;
            if (count == 0)
                return;
            writer.WriteLine(key);
            for (var i = 0; i < count; i++)
                if (array[i] == null)
                    writer.WriteLine("");
                else
                    writer.WriteLine(array[i]);
            writer.WriteLine(FINISHER);
        }

        public void WriteExtended(string key, long[,] array2D)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array2D == null)
                throw new FileEE("無効な配列が渡されました");
            var countX = 0;
            var length0 = array2D.GetLength(0);
            var length1 = array2D.GetLength(1);
            var countY = new int[length0];
            for (var x = 0; x < length0; x++)
            for (var y = 0; y < length1; y++)
                if (array2D[x, y] != 0)
                {
                    countX = x + 1;
                    countY[x] = y + 1;
                }
            if (countX == 0)
                return;
            writer.WriteLine(key);
            for (var x = 0; x < countX; x++)
            {
                if (countY[x] == 0)
                {
                    writer.WriteLine("");
                    continue;
                }
                var builder = new StringBuilder("");
                for (var y = 0; y < countY[x]; y++)
                {
                    builder.Append(array2D[x, y].ToString());
                    if (y != countY[x] - 1)
                        builder.Append(",");
                }
                writer.WriteLine(builder.ToString());
            }
            writer.WriteLine(FINISHER);
        }

        public void WriteExtended(string key, string[,] array2D)
        {
            throw new NotImplementedException("まだ実装してないよ");
        }

        public void WriteExtended(string key, long[,,] array3D)
        {
            if (writer == null)
                throw new FileEE("無効なストリームです");
            if (array3D == null)
                throw new FileEE("無効な配列が渡されました");
            var countX = 0;
            var length0 = array3D.GetLength(0);
            var length1 = array3D.GetLength(1);
            var length2 = array3D.GetLength(2);
            var countY = new int[length0];
            var countZ = new int[length0, length1];
            for (var x = 0; x < length0; x++)
            for (var y = 0; y < length1; y++)
            for (var z = 0; z < length2; z++)
                if (array3D[x, y, z] != 0)
                {
                    countX = x + 1;
                    countY[x] = y + 1;
                    countZ[x, y] = z + 1;
                }
            if (countX == 0)
                return;
            writer.WriteLine(key);
            for (var x = 0; x < countX; x++)
            {
                writer.WriteLine(x + "{");
                if (countY[x] == 0)
                {
                    writer.WriteLine("}");
                    continue;
                }
                for (var y = 0; y < countY[x]; y++)
                {
                    var builder = new StringBuilder("");
                    if (countZ[x, y] == 0)
                    {
                        writer.WriteLine("");
                        continue;
                    }
                    for (var z = 0; z < countZ[x, y]; z++)
                    {
                        builder.Append(array3D[x, y, z].ToString());
                        if (z != countZ[x, y] - 1)
                            builder.Append(",");
                    }
                    writer.WriteLine(builder.ToString());
                }
                writer.WriteLine("}");
            }
            writer.WriteLine(FINISHER);
        }

        public void WriteExtended(string key, string[,,] array2D)
        {
            throw new NotImplementedException("まだ実装してないよ");
        }

        #endregion
    }
}