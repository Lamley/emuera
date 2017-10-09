using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Variable
{
    internal sealed class CharacterData : IDisposable
    {
        private const int strCount = (int) VariableCode.__COUNT_SAVE_CHARACTER_STRING__;
        private const int intCount = (int) VariableCode.__COUNT_SAVE_CHARACTER_INTEGER__;
        private const int intArrayCount = (int) VariableCode.__COUNT_SAVE_CHARACTER_INTEGER_ARRAY__;
        private const int strArrayCount = (int) VariableCode.__COUNT_SAVE_CHARACTER_STRING_ARRAY__;

        public CharacterData(ConstantData constant, VariableData varData)
        {
            DataInteger = new long[(int) VariableCode.__COUNT_CHARACTER_INTEGER__];
            DataString = new string[(int) VariableCode.__COUNT_CHARACTER_STRING__];
            DataIntegerArray = new long[(int) VariableCode.__COUNT_CHARACTER_INTEGER_ARRAY__][];
            DataStringArray = new string[(int) VariableCode.__COUNT_CHARACTER_STRING_ARRAY__][];
            DataIntegerArray2D = new long[(int) VariableCode.__COUNT_CHARACTER_INTEGER_ARRAY_2D__][,];
            DataStringArray2D = new string[(int) VariableCode.__COUNT_CHARACTER_STRING_ARRAY_2D__][,];
            for (var i = 0; i < DataIntegerArray.Length; i++)
                DataIntegerArray[i] = new long[constant.CharacterIntArrayLength[i]];
            for (var i = 0; i < DataStringArray.Length; i++)
                DataStringArray[i] = new string[constant.CharacterStrArrayLength[i]];
            for (var i = 0; i < DataIntegerArray2D.Length; i++)
            {
                var length64 = constant.CharacterIntArray2DLength[i];
                var length = (int) (length64 >> 32);
                var length2 = (int) (length64 & 0x7FFFFFFF);
                DataIntegerArray2D[i] = new long[length, length2];
            }
            for (var i = 0; i < DataStringArray2D.Length; i++)
            {
                var length64 = constant.CharacterStrArray2DLength[i];
                var length = (int) (length64 >> 32);
                var length2 = (int) (length64 & 0x7FFFFFFF);
                DataStringArray2D[i] = new string[length, length2];
            }
            UserDefCVarDataList = new List<object>();
            for (var i = 0; i < varData.UserDefinedCharaVarList.Count; i++)
            {
                var d = varData.UserDefinedCharaVarList[i].DimData;
                object array = null;
                if (d.TypeIsStr)
                    switch (d.Dimension)
                    {
                        case 1:
                            array = new string[d.Lengths[0]];
                            break;
                        case 2:
                            array = new string[d.Lengths[0], d.Lengths[1]];
                            break;
                        case 3:
                            array = new string[d.Lengths[0], d.Lengths[1], d.Lengths[2]];
                            break;
                    }
                else
                    switch (d.Dimension)
                    {
                        case 1:
                            array = new long[d.Lengths[0]];
                            break;
                        case 2:
                            array = new long[d.Lengths[0], d.Lengths[1]];
                            break;
                        case 3:
                            array = new long[d.Lengths[0], d.Lengths[1], d.Lengths[2]];
                            break;
                    }
                if (array == null)
                    throw new ExeEE("");
                UserDefCVarDataList.Add(array);
            }
        }


        public CharacterData(ConstantData constant, CharacterTemplate tmpl, VariableData varData)
            : this(constant, varData)
        {
            DataInteger[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.NO] = tmpl.No;
            DataString[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.NAME] = tmpl.Name;
            DataString[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.CALLNAME] = tmpl.Callname;
            DataString[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.NICKNAME] = tmpl.Nickname;
            DataString[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.MASTERNAME] = tmpl.Mastername;
            long[] array, array2;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.MAXBASE];
            array2 = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.BASE];
            foreach (var pair in tmpl.Maxbase)
            {
                array[pair.Key] = pair.Value;
                array2[pair.Key] = pair.Value;
            }
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.MARK];
            foreach (var pair in tmpl.Mark)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.EXP];
            foreach (var pair in tmpl.Exp)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.ABL];
            foreach (var pair in tmpl.Abl)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.TALENT];
            foreach (var pair in tmpl.Talent)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.RELATION];
            for (var i = 0; i < array.Length; i++)
                array[i] = Config.RelationDef;
            foreach (var pair in tmpl.Relation)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.CFLAG];
            foreach (var pair in tmpl.CFlag)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.EQUIP];
            foreach (var pair in tmpl.Equip)
                array[pair.Key] = pair.Value;
            array = DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.JUEL];
            foreach (var pair in tmpl.Juel)
                array[pair.Key] = pair.Value;
            var arrays = DataStringArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.CSTR];
            foreach (var pair in tmpl.CStr)
                arrays[pair.Key] = pair.Value;
            /*
            //tmpl.Maxbase.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MAXBASE], 0);
            Buffer.BlockCopy(tmpl.Maxbase, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MAXBASE], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MAXBASE]);
            //tmpl.Maxbase.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.BASE], 0);
            Buffer.BlockCopy(tmpl.Maxbase, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.BASE], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.BASE]);

            //tmpl.Mark.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MARK], 0);
            Buffer.BlockCopy(tmpl.Mark, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MARK], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.MARK]);
            //tmpl.Exp.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EXP], 0);
            Buffer.BlockCopy(tmpl.Exp, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EXP], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EXP]);
            //tmpl.Abl.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.ABL], 0);
            Buffer.BlockCopy(tmpl.Abl, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.ABL], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.ABL]);
            //tmpl.Talent.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.TALENT], 0);
            Buffer.BlockCopy(tmpl.Talent, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.TALENT], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.TALENT]);
            //tmpl.Relation.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.RELATION], 0);
            Buffer.BlockCopy(tmpl.Relation, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.RELATION], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.RELATION]);
            //tmpl.CFlag.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.CFLAG], 0);
            Buffer.BlockCopy(tmpl.CFlag, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.CFLAG], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.CFLAG]);
            //tmpl.Equip.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EQUIP], 0);
            Buffer.BlockCopy(tmpl.Equip, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EQUIP], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.EQUIP]);
            //tmpl.Juel.CopyTo(dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.JUEL], 0);
            Buffer.BlockCopy(tmpl.Juel, 0, dataIntegerArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.JUEL], 0, 8 * constant.CharacterIntArrayLength[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.JUEL]);

            tmpl.CStr.CopyTo(dataStringArray[(int)VariableCode.__LOWERCASE__ & (int)VariableCode.CSTR], 0);
            */
        }

        public long[] DataInteger { get; }

        public string[] DataString { get; }

        public long[][] DataIntegerArray { get; }

        public string[][] DataStringArray { get; }

        public long[][,] DataIntegerArray2D { get; }

        public string[][,] DataStringArray2D { get; }

        public List<object> UserDefCVarDataList { get; set; }

        public long[] CFlag => DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.CFLAG];

        public long NO => DataInteger[(int) VariableCode.__LOWERCASE__ & (int) VariableCode.NO];

        #region IDisposable メンバ

        public void Dispose()
        {
            for (var i = 0; i < DataIntegerArray.Length; i++)
                DataIntegerArray[i] = null;
            for (var i = 0; i < DataStringArray.Length; i++)
                DataStringArray[i] = null;
            for (var i = 0; i < DataIntegerArray2D.Length; i++)
                DataIntegerArray2D[i] = null;
        }

        #endregion

        public static int[] CharacterVarLength(VariableCode code, ConstantData constant)
        {
            int[] ret = null;
            var type = code & (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ |
                               VariableCode.__ARRAY_3D__ | VariableCode.__INTEGER__ | VariableCode.__STRING__);
            var i = (int) (code & VariableCode.__LOWERCASE__);
            if (i >= 0xF0)
                return null;
            long length64 = 0;
            switch (type)
            {
                case VariableCode.__STRING__:
                case VariableCode.__INTEGER__:
                    ret = new int[0];
                    break;
                case VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
                    ret = new int[1];
                    ret[0] = constant.CharacterIntArrayLength[i];
                    break;
                case VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
                    ret = new int[1];
                    ret[0] = constant.CharacterStrArrayLength[i];
                    break;
                case VariableCode.__INTEGER__ | VariableCode.__ARRAY_2D__:
                    ret = new int[2];
                    length64 = constant.CharacterIntArray2DLength[i];
                    ret[0] = (int) (length64 >> 32);
                    ret[1] = (int) (length64 & 0x7FFFFFFF);
                    break;
                case VariableCode.__STRING__ | VariableCode.__ARRAY_2D__:
                    ret = new int[2];
                    length64 = constant.CharacterStrArray2DLength[i];
                    ret[0] = (int) (length64 >> 32);
                    ret[1] = (int) (length64 & 0x7FFFFFFF);
                    break;
                case VariableCode.__INTEGER__ | VariableCode.__ARRAY_3D__:
                    throw new NotImplCodeEE();
                case VariableCode.__STRING__ | VariableCode.__ARRAY_3D__:
                    throw new NotImplCodeEE();
            }
            return ret;
        }

        public void CopyTo(CharacterData other)
        {
            for (var i = 0; i < DataInteger.Length; i++)
                other.DataInteger[i] = DataInteger[i];
            for (var i = 0; i < DataString.Length; i++)
                other.DataString[i] = DataString[i];

            for (var i = 0; i < DataIntegerArray.Length; i++)
            for (var j = 0; j < DataIntegerArray[i].Length; j++)
                other.DataIntegerArray[i][j] = DataIntegerArray[i][j];
            for (var i = 0; i < DataStringArray.Length; i++)
            for (var j = 0; j < DataStringArray[i].Length; j++)
                other.DataStringArray[i][j] = DataStringArray[i][j];

            for (var i = 0; i < DataIntegerArray2D.Length; i++)
            {
                var length1 = DataIntegerArray2D[i].GetLength(0);
                var length2 = DataIntegerArray2D[i].GetLength(1);
                for (var j = 0; j < length1; j++)
                for (var k = 0; k < length2; k++)
                    other.DataIntegerArray2D[i][j, k] = DataIntegerArray2D[i][j, k];
            }
            for (var i = 0; i < DataStringArray2D.Length; i++)
            {
                var length1 = DataStringArray2D[i].GetLength(0);
                var length2 = DataStringArray2D[i].GetLength(1);
                for (var j = 0; j < length1; j++)
                for (var k = 0; k < length2; k++)
                    other.DataStringArray2D[i][j, k] = DataStringArray2D[i][j, k];
            }
        }

        public void SaveToStream(EraDataWriter writer)
        {
            for (var i = 0; i < strCount; i++)
                writer.Write(DataString[i]);
            for (var i = 0; i < intCount; i++)
                writer.Write(DataInteger[i]);
            for (var i = 0; i < intArrayCount; i++)
                writer.Write(DataIntegerArray[i]);
            for (var i = 0; i < strArrayCount; i++)
                writer.Write(DataStringArray[i]);
        }

        public void LoadFromStream(EraDataReader reader)
        {
            for (var i = 0; i < strCount; i++)
                DataString[i] = reader.ReadString();
            for (var i = 0; i < intCount; i++)
                DataInteger[i] = reader.ReadInt64();
            for (var i = 0; i < intArrayCount; i++)
                reader.ReadInt64Array(DataIntegerArray[i]);
            for (var i = 0; i < strArrayCount; i++)
                reader.ReadStringArray(DataStringArray[i]);
        }

        public void SaveToStreamExtended(EraDataWriter writer)
        {
            List<VariableCode> codeList = null;

            //dataString
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(), DataString[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();

            //datainteger
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(), DataInteger[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();

            //dataStringArray
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__STRING__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(), DataStringArray[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();

            //dataIntegerArray
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__INTEGER__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(), DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();

            //dataStringArray2D
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_2D__ |
                                                         VariableCode.__STRING__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(), DataStringArray2D[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();

            //dataIntegerArray2D
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_2D__ |
                                                         VariableCode.__INTEGER__);
            foreach (var code in codeList)
                writer.WriteExtended(code.ToString(),
                    DataIntegerArray2D[(int) VariableCode.__LOWERCASE__ & (int) code]);
            writer.EmuSeparete();
        }

        public void LoadFromStreamExtended(EraDataReader reader)
        {
            var strDic = reader.ReadStringExtended();
            var intDic = reader.ReadInt64Extended();
            var strListDic = reader.ReadStringArrayExtended();
            var intListDic = reader.ReadInt64ArrayExtended();
            var str2DListDic = reader.ReadStringArray2DExtended();
            var int2DListDic = reader.ReadInt64Array2DExtended();

            List<VariableCode> codeList = null;

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__);
            foreach (var code in codeList)
                if (strDic.ContainsKey(code.ToString()))
                    DataString[(int) VariableCode.__LOWERCASE__ & (int) code] = strDic[code.ToString()];

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__);
            foreach (var code in codeList)
                if (intDic.ContainsKey(code.ToString()))
                    DataInteger[(int) VariableCode.__LOWERCASE__ & (int) code] = intDic[code.ToString()];


            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__STRING__);
            foreach (var code in codeList)
                if (strListDic.ContainsKey(code.ToString()))
                    copyListToArray(strListDic[code.ToString()],
                        DataStringArray[(int) VariableCode.__LOWERCASE__ & (int) code]);

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__INTEGER__);
            foreach (var code in codeList)
                if (intListDic.ContainsKey(code.ToString()))
                    copyListToArray(intListDic[code.ToString()],
                        DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) code]);

            //dataStringArray2D
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_2D__ |
                                                         VariableCode.__STRING__);
            foreach (var code in codeList)
                if (int2DListDic.ContainsKey(code.ToString()))
                    copyListToArray2D(str2DListDic[code.ToString()],
                        DataStringArray2D[(int) VariableCode.__LOWERCASE__ & (int) code]);

            //dataIntegerArray2D
            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_2D__ |
                                                         VariableCode.__INTEGER__);
            foreach (var code in codeList)
                if (int2DListDic.ContainsKey(code.ToString()))
                    copyListToArray2D(int2DListDic[code.ToString()],
                        DataIntegerArray2D[(int) VariableCode.__LOWERCASE__ & (int) code]);
        }

        public void LoadFromStreamExtended_Old1802(EraDataReader reader)
        {
            var strDic = reader.ReadStringExtended();
            var intDic = reader.ReadInt64Extended();
            var strListDic = reader.ReadStringArrayExtended();
            var intListDic = reader.ReadInt64ArrayExtended();

            List<VariableCode> codeList = null;

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__);
            foreach (var code in codeList)
                if (strDic.ContainsKey(code.ToString()))
                    DataString[(int) VariableCode.__LOWERCASE__ & (int) code] = strDic[code.ToString()];

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__);
            foreach (var code in codeList)
                if (intDic.ContainsKey(code.ToString()))
                    DataInteger[(int) VariableCode.__LOWERCASE__ & (int) code] = intDic[code.ToString()];


            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__STRING__);
            foreach (var code in codeList)
                if (strListDic.ContainsKey(code.ToString()))
                    copyListToArray(strListDic[code.ToString()],
                        DataStringArray[(int) VariableCode.__LOWERCASE__ & (int) code]);

            codeList = VariableIdentifier.GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__ARRAY_1D__ |
                                                         VariableCode.__INTEGER__);
            foreach (var code in codeList)
                if (intListDic.ContainsKey(code.ToString()))
                    copyListToArray(intListDic[code.ToString()],
                        DataIntegerArray[(int) VariableCode.__LOWERCASE__ & (int) code]);
        }

        public void SaveToStreamBinary(EraBinaryDataWriter writer, VariableData varData)
        {
            //eramaker変数の保存
            foreach (var pair in varData.GetVarTokenDic())
            {
                var var = pair.Value;
                if (!var.IsSavedata || !var.IsCharacterData || var.IsGlobal)
                    continue;
                var code = var.Code;
                var flag = code & (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ | VariableCode.__ARRAY_3D__ |
                                   VariableCode.__STRING__ | VariableCode.__INTEGER__);
                var CodeInt = var.CodeInt;
                switch (flag)
                {
                    case VariableCode.__INTEGER__:
                        writer.WriteWithKey(code.ToString(), DataInteger[CodeInt]);
                        break;
                    case VariableCode.__STRING__:
                        writer.WriteWithKey(code.ToString(), DataString[CodeInt]);
                        break;
                    case VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
                        writer.WriteWithKey(code.ToString(), DataIntegerArray[CodeInt]);
                        break;
                    case VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
                        writer.WriteWithKey(code.ToString(), DataStringArray[CodeInt]);
                        break;
                    case VariableCode.__INTEGER__ | VariableCode.__ARRAY_2D__:
                        writer.WriteWithKey(code.ToString(), DataIntegerArray2D[CodeInt]);
                        break;
                    case VariableCode.__STRING__ | VariableCode.__ARRAY_2D__:
                        writer.WriteWithKey(code.ToString(), DataStringArray2D[CodeInt]);
                        break;
                    //case VariableCode.__INTEGER__ | VariableCode.__ARRAY_3D__:
                    //    writer.Write(code.ToString(), dataIntegerArray3D[CodeInt]);
                    //    break;
                    //case VariableCode.__STRING__ | VariableCode.__ARRAY_3D__:
                    //    writer.Write(code.ToString(), dataStringArray3D[CodeInt]);
                    //    break;
                }
            }

            //1813追加
            if (UserDefCVarDataList.Count != 0)
            {
                writer.WriteSeparator();
                //#DIM宣言変数の保存
                foreach (var var in varData.UserDefinedCharaVarList)
                {
                    if (!var.IsSavedata || !var.IsCharacterData || var.IsGlobal)
                        continue;
                    writer.WriteWithKey(var.Name, UserDefCVarDataList[var.ArrayIndex]);
                }
            }

            writer.WriteEOC();
        }

        public void LoadFromStreamBinary(EraBinaryDataReader reader)
        {
            var codeInt = 0;
            var userDefineData = false;
            while (true)
            {
                var nameAndType = reader.ReadVariableCode();
                VariableToken vToken = null;
                object array = null;
                if (nameAndType.Key != null)
                {
                    vToken = GlobalStatic.IdentifierDictionary.GetVariableToken(nameAndType.Key, null, false);
                    if (userDefineData)
                    {
                        if (vToken == null || !vToken.IsSavedata || !vToken.IsCharacterData ||
                            !(vToken is UserDefinedCharaVariableToken))
                            array = null;
                        else
                            array = UserDefCVarDataList[((UserDefinedCharaVariableToken) vToken).ArrayIndex];
                        vToken = null;
                    }
                    else
                    {
                        codeInt = (int) VariableCode.__LOWERCASE__ & (int) vToken.Code;
                        array = null;
                    }
                }
                switch (nameAndType.Value)
                {
                    case EraSaveDataType.Separator:
                        userDefineData = true;
                        continue;
                    case EraSaveDataType.EOF:
                    case EraSaveDataType.EOC:
                        goto whilebreak;
                    case EraSaveDataType.Int:
                        if (vToken == null || !vToken.IsInteger || vToken.Dimension != 0)
                            reader.ReadInt();
                        else
                            DataInteger[codeInt] = reader.ReadInt();
                        break;
                    case EraSaveDataType.Str:
                        if (vToken == null || !vToken.IsString || vToken.Dimension != 0)
                            reader.ReadString();
                        else
                            DataString[codeInt] = reader.ReadString();
                        break;
                    case EraSaveDataType.IntArray:
                        if (userDefineData && array != null)
                            reader.ReadIntArray(array as long[], true);
                        else if (vToken == null || !vToken.IsInteger || vToken.Dimension != 1)
                            reader.ReadIntArray(null, true);
                        else
                            reader.ReadIntArray(DataIntegerArray[codeInt], true);
                        break;
                    case EraSaveDataType.StrArray:
                        if (userDefineData && array != null)
                            reader.ReadStrArray(array as string[], true);
                        else if (vToken == null || !vToken.IsString || vToken.Dimension != 1)
                            reader.ReadStrArray(null, true);
                        else
                            reader.ReadStrArray(DataStringArray[codeInt], true);
                        break;
                    case EraSaveDataType.IntArray2D:
                        if (userDefineData && array != null)
                            reader.ReadIntArray2D(array as long[,], true);
                        else if (vToken == null || !vToken.IsInteger || vToken.Dimension != 2)
                            reader.ReadIntArray2D(null, true);
                        else
                            reader.ReadIntArray2D(DataIntegerArray2D[codeInt], true);
                        break;
                    case EraSaveDataType.StrArray2D:
                        if (userDefineData && array != null)
                            reader.ReadStrArray2D(array as string[,], true);
                        else if (vToken == null || !vToken.IsString || vToken.Dimension != 2)
                            reader.ReadStrArray2D(null, true);
                        else
                            reader.ReadStrArray2D(DataStringArray2D[codeInt], true);
                        break;
                    //case EraSaveDataType.IntArray3D:
                    //    if (vToken == null || !vToken.IsInteger || vToken.Dimension != 3)
                    //        reader.ReadIntArray3D(null, true);
                    //    else
                    //        reader.ReadIntArray3D(dataIntegerArray3D[codeInt], true);
                    //    break;
                    //case EraSaveDataType.StrArray3D:
                    //    if (vToken == null || !vToken.IsString || vToken.Dimension != 3)
                    //        reader.ReadStrArray3D(null, true);
                    //    else
                    //        reader.ReadStrArray3D(dataStringArray3D[codeInt], true);
                    //    break;
                    default:
                        throw new FileEE("データ異常");
                }
            }
            whilebreak:
            ;
        }


        private void copyListToArray<T>(List<T> srcList, T[] destArray)
        {
            var count = Math.Min(srcList.Count, destArray.Length);
            srcList.CopyTo(0, destArray, 0, count);
            //for (int i = 0; i < count; i++)
            //{
            //    destArray[i] = srcList[i];
            //}
        }

        private void copyListToArray2D<T>(List<T[]> srcList, T[,] destArray)
        {
            var countX = Math.Min(srcList.Count, destArray.GetLength(0));
            var dLength = destArray.GetLength(1);
            for (var x = 0; x < countX; x++)
            {
                var srcArray = srcList[x];
                var countY = Math.Min(srcArray.Length, dLength);
                for (var y = 0; y < countY; y++)
                    destArray[x, y] = srcArray[y];
            }
        }

        public void setValueAll(int varInt, long value)
        {
            DataInteger[varInt] = value;
        }

        public void setValueAll(int varInt, string value)
        {
            DataString[varInt] = value;
        }

        public void setValueAll1D(int varInt, long value, int start, int end)
        {
            var array = DataIntegerArray[varInt];
            for (var i = start; i < end; i++)
                array[i] = value;
        }

        public void setValueAll1D(int varInt, string value, int start, int end)
        {
            var array = DataStringArray[varInt];
            for (var i = start; i < end; i++)
                array[i] = value;
        }

        public void setValueAll2D(int varInt, long value)
        {
            var array = DataIntegerArray2D[varInt];
            var a1 = array.GetLength(0);
            var a2 = array.GetLength(1);
            for (var i = 0; i < a1; i++)
            for (var j = 0; j < a2; j++)
                array[i, j] = value;
        }

        public void setValueAll2D(int varInt, string value)
        {
            var array = DataStringArray2D[varInt];
            var a1 = array.GetLength(0);
            var a2 = array.GetLength(1);
            for (var i = 0; i < a1; i++)
            for (var j = 0; j < a2; j++)
                array[i, j] = value;
        }

        #region sort

        public IComparable temp_SortKey;

        public int temp_CurrentOrder;

        //Comparison<CharacterData>
        public static int AscCharacterComparison(CharacterData x, CharacterData y)
        {
            var ret = x.temp_SortKey.CompareTo(y.temp_SortKey);
            if (ret != 0)
                return ret;
            return x.temp_CurrentOrder.CompareTo(y.temp_CurrentOrder);
        }

        public static int DescCharacterComparison(CharacterData x, CharacterData y)
        {
            var ret = x.temp_SortKey.CompareTo(y.temp_SortKey);
            if (ret != 0)
                return -ret;
            return x.temp_CurrentOrder.CompareTo(y.temp_CurrentOrder);
        }

        public void SetSortKey(VariableToken sortkey, long elem64)
        {
            //チェック済み
            //if (!sortkey.IsCharacterData)
            //    throw new ExeEE("キャラクタ変数でない");
            if (sortkey.IsString)
            {
                if (sortkey.IsArray2D)
                {
                    var array = DataStringArray2D[sortkey.CodeInt];
                    var elem1 = (int) (elem64 >> 32);
                    var elem2 = (int) (elem64 & 0x7FFFFFFF);
                    if (elem1 < 0 || elem1 >= array.GetLength(0) || elem2 < 0 || elem2 >= array.GetLength(1))
                        throw new CodeEE("ソートキーが配列外を参照しています");
                    temp_SortKey = array[elem1, elem2];
                }
                else if (sortkey.IsArray1D)
                {
                    var array = DataStringArray[sortkey.CodeInt];
                    if (elem64 < 0 || elem64 >= array.Length)
                        throw new CodeEE("ソートキーが配列外を参照しています");
                    if (array[(int) elem64] != null)
                        temp_SortKey = array[(int) elem64];
                    else
                        temp_SortKey = "";
                }
                else
                {
                    if (DataString[sortkey.CodeInt] != null)
                        temp_SortKey = DataString[sortkey.CodeInt];
                    else
                        temp_SortKey = "";
                }
            }
            else
            {
                if (sortkey.IsArray2D)
                {
                    var array = DataIntegerArray2D[sortkey.CodeInt];
                    var elem1 = (int) (elem64 >> 32);
                    var elem2 = (int) (elem64 & 0x7FFFFFFF);
                    if (elem1 < 0 || elem1 >= array.GetLength(0) || elem2 < 0 || elem2 >= array.GetLength(1))
                        throw new CodeEE("ソートキーが配列外を参照しています");
                    temp_SortKey = array[elem1, elem2];
                }
                else if (sortkey.IsArray1D)
                {
                    var array = DataIntegerArray[sortkey.CodeInt];
                    if (elem64 < 0 || elem64 >= array.Length)
                        throw new CodeEE("ソートキーが配列外を参照しています");
                    temp_SortKey = array[(int) elem64];
                }
                else
                {
                    temp_SortKey = DataInteger[sortkey.CodeInt];
                }
            }
        }

        #endregion
    }
}