using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Variable
{
    //1756 全ての機能をVariableTokenとManagerに委譲、消滅
    //……しようと思ったがConstantDataから参照されているので捨て切れなかった。
    /// <summary>
    ///     VariableCodeのラッパー
    /// </summary>
    internal sealed class VariableIdentifier
    {
        private static readonly Dictionary<string, VariableCode> nameDic = new Dictionary<string, VariableCode>();

        private static readonly Dictionary<string, VariableCode> localvarNameDic =
            new Dictionary<string, VariableCode>();

        private static readonly Dictionary<VariableCode, List<VariableCode>> extSaveListDic =
            new Dictionary<VariableCode, List<VariableCode>>();


        static VariableIdentifier()
        {
            var array = Enum.GetValues(typeof(VariableCode));

            nameDic.Add(VariableCode.__FILE__.ToString(), VariableCode.__FILE__);
            nameDic.Add(VariableCode.__LINE__.ToString(), VariableCode.__LINE__);
            nameDic.Add(VariableCode.__FUNCTION__.ToString(), VariableCode.__FUNCTION__);
            foreach (var name in array)
            {
                var code = (VariableCode) name;
                var key = code.ToString();
                if (key == null || key.StartsWith("__") && key.EndsWith("__"))
                    continue;
                if (Config.ICVariable)
                    key = key.ToUpper();
                if (nameDic.ContainsKey(key))
                    continue;
#if DEBUG
                if ((code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__)
                    if ((code & VariableCode.__ARRAY_1D__) == VariableCode.__ARRAY_1D__)
                        throw new ExeEE("ARRAY2DとARRAY1Dは排他");
                if ((code & VariableCode.__INTEGER__) != VariableCode.__INTEGER__
                    && (code & VariableCode.__STRING__) != VariableCode.__STRING__)
                    throw new ExeEE("INTEGERとSTRINGのどちらかは必須");
                if ((code & VariableCode.__INTEGER__) == VariableCode.__INTEGER__
                    && (code & VariableCode.__STRING__) == VariableCode.__STRING__)
                    throw new ExeEE("INTEGERとSTRINGは排他");
                if ((code & VariableCode.__EXTENDED__) != VariableCode.__EXTENDED__)
                {
                    if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__)
                        throw new ExeEE("SAVE_EXTENDEDにはEXTENDEDフラグ必須");
                    if ((code & VariableCode.__LOCAL__) == VariableCode.__LOCAL__)
                        throw new ExeEE("LOCALにはEXTENDEDフラグ必須");
                    if ((code & VariableCode.__GLOBAL__) == VariableCode.__GLOBAL__)
                        throw new ExeEE("GLOBALにはEXTENDEDフラグ必須");
                    if ((code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__)
                        throw new ExeEE("ARRAY2DにはEXTENDEDフラグ必須");
                }
                if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__
                    && (code & VariableCode.__UNCHANGEABLE__) == VariableCode.__UNCHANGEABLE__)
                    throw new ExeEE("CALCとSAVE_EXTENDEDは排他");
                if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__
                    && (code & VariableCode.__CALC__) == VariableCode.__CALC__)
                    throw new ExeEE("UNCHANGEABLEとSAVE_EXTENDEDは排他");
                if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__
                    && (code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__
                    && (code & VariableCode.__STRING__) == VariableCode.__STRING__)
                    throw new ExeEE("STRINGかつARRAY2DのSAVE_EXTENDEDは未実装");
#endif
                nameDic.Add(key, code);
                ////セーブが必要な変数リストの作成

                ////__SAVE_EXTENDED__フラグ持ち
                //if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__)
                //{
                //    if ((code & VariableCode.__CHARACTER_DATA__) == VariableCode.__CHARACTER_DATA__)
                //        charaSaveDataList.Add(code);
                //    else
                //        saveDataList.Add(code);
                //}
                //else if ( ((code & VariableCode.__EXTENDED__) != VariableCode.__EXTENDED__)
                //    && ((code & VariableCode.__CALC__) != VariableCode.__CALC__)
                //    && ((code & VariableCode.__UNCHANGEABLE__) != VariableCode.__UNCHANGEABLE__)
                //    && ((code & VariableCode.__LOCAL__) != VariableCode.__LOCAL__)
                //    && (!key.StartsWith("NOTUSE_")) )
                //{//eramaker由来の変数でセーブするもの

                //    VariableCode flag = code & (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ | VariableCode.__ARRAY_3D__ | VariableCode.__STRING__ | VariableCode.__INTEGER__ | VariableCode.__CHARACTER_DATA__);
                //    int codeInt = (int)VariableCode.__LOWERCASE__ & (int)code;
                //    switch (flag)
                //    {
                //        case VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_INTEGER__)
                //                charaSaveDataList.Add(code);
                //            break;
                //        case VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_STRING__)
                //                charaSaveDataList.Add(code);
                //            break;
                //        case VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_INTEGER_ARRAY__)
                //                charaSaveDataList.Add(code);
                //            break;
                //        case VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_STRING_ARRAY__)
                //                charaSaveDataList.Add(code);
                //            break;
                //        case VariableCode.__INTEGER__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_INTEGER__)
                //                saveDataList.Add(code);
                //            break;
                //        case VariableCode.__STRING__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_STRING__)
                //                saveDataList.Add(code);
                //            break;
                //        case VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_INTEGER_ARRAY__)
                //                saveDataList.Add(code);
                //            break;
                //        case VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
                //            if (codeInt < (int)VariableCode.__COUNT_SAVE_STRING_ARRAY__)
                //                saveDataList.Add(code);
                //            break;
                //    }
                //}


                if ((code & VariableCode.__LOCAL__) == VariableCode.__LOCAL__)
                    localvarNameDic.Add(key, code);
                if ((code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__)
                {
                    var flag = code &
                               (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ | VariableCode.__ARRAY_3D__ |
                                VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__ | VariableCode.__INTEGER__);
                    if (!extSaveListDic.ContainsKey(flag))
                        extSaveListDic.Add(flag, new List<VariableCode>());
                    extSaveListDic[flag].Add(code);
                }
            }
        }

        private VariableIdentifier(VariableCode code)
        {
            Code = code;
        }

        private VariableIdentifier(VariableCode code, string scope)
        {
            Code = code;
            Scope = scope;
        }

        public VariableCode Code { get; }

        public string Scope { get; }

        public int CodeInt => (int) (Code & VariableCode.__LOWERCASE__);

        public VariableCode CodeFlag => Code & VariableCode.__UPPERCASE__;
        //public int Dimension
        //{
        //    get
        //    {
        //        int dim = 0;
        //        if ((code & VariableCode.__ARRAY_1D__) == VariableCode.__ARRAY_1D__)
        //            dim++;
        //        if ((code & VariableCode.__CHARACTER_DATA__) == VariableCode.__CHARACTER_DATA__)
        //            dim++;
        //        if ((code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__)
        //            dim += 2;
        //        return dim;
        //    }
        //}

        public bool IsNull => Code == VariableCode.__NULL__;

        public bool IsCharacterData => (Code & VariableCode.__CHARACTER_DATA__) == VariableCode.__CHARACTER_DATA__;

        public bool IsInteger => (Code & VariableCode.__INTEGER__) == VariableCode.__INTEGER__;

        public bool IsString => (Code & VariableCode.__STRING__) == VariableCode.__STRING__;

        public bool IsArray1D => (Code & VariableCode.__ARRAY_1D__) == VariableCode.__ARRAY_1D__;

        public bool IsArray2D => (Code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__;

        public bool IsArray3D => (Code & VariableCode.__ARRAY_3D__) == VariableCode.__ARRAY_3D__;

        public bool Readonly => (Code & VariableCode.__UNCHANGEABLE__) == VariableCode.__UNCHANGEABLE__;

        public bool IsCalc => (Code & VariableCode.__CALC__) == VariableCode.__CALC__;

        public bool IsLocal => (Code & VariableCode.__LOCAL__) == VariableCode.__LOCAL__;

        //public bool IsConstant
        //{
        //    get
        //    {
        //        return ((code & VariableCode.__CONSTANT__) == VariableCode.__CONSTANT__);
        //    }
        //}
        public bool CanForbid => (Code & VariableCode.__CAN_FORBID__) == VariableCode.__CAN_FORBID__;

        public static Dictionary<string, VariableCode> GetVarNameDic()
        {
            return nameDic;
        }

        public static List<VariableCode> GetExtSaveList(VariableCode flag)
        {
            var gFlag = flag &
                        (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ | VariableCode.__ARRAY_3D__ |
                         VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__ | VariableCode.__INTEGER__);
            if (!extSaveListDic.ContainsKey(gFlag))
                return new List<VariableCode>();
            return extSaveListDic[gFlag];
        }

        public static VariableIdentifier GetVariableId(VariableCode code)
        {
            return new VariableIdentifier(code);
        }

        public static VariableIdentifier GetVariableId(string key)
        {
            return GetVariableId(key, null);
        }

        public static VariableIdentifier GetVariableId(string key, string subStr)
        {
            var ret = VariableCode.__NULL__;
            if (string.IsNullOrEmpty(key))
                return null;
            if (Config.ICVariable)
                key = key.ToUpper();
            if (subStr != null)
            {
                if (Config.ICFunction)
                    subStr = subStr.ToUpper();
                if (localvarNameDic.TryGetValue(key, out ret))
                    return new VariableIdentifier(ret, subStr);
                if (nameDic.ContainsKey(key))
                    throw new CodeEE("ローカル変数でない変数" + key + "に対して@が使われました");
                throw new CodeEE("@の使い方が不正です");
            }
            nameDic.TryGetValue(key, out ret);
            return new VariableIdentifier(ret);
        }

        public override string ToString()
        {
            return Code.ToString();
        }
    }
}