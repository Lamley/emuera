namespace MinorShift.Emuera.GameProc
{
    internal enum InputType
    {
        EnterKey = 1, //Enterキーかクリック
        AnyKey = 2, //なんでもいいから入力
        IntValue = 3, //整数値。OneInputかどうかは別の変数で
        StrValue = 4, //文字列。
        Void = 5 //入力不能。待つしかない→スキップ中orマクロ中ならなかったことになる
    }


    // 1819追加 入力・表示系とData、Process系の結合を弱くしよう計画の一つ
    // できるだけ間にクッションをおいていきたい。最終的には別スレッドに

    //クラスを毎回使い捨てるのはどうなんだろう 使いまわすべきか
    internal sealed class InputRequest
    {
        private static long LastRequestID;
        public readonly long ID;
        public long DefIntValue;
        public string DefStrValue;
        public bool DisplayTime;

        public bool HasDefValue = false;
        public InputType InputType;
        public bool IsSystemInput = false;
        public bool OneInput = false;
        public bool StopMesskip = false;

        public long Timelimit = -1;
        public string TimeUpMes;

        public InputRequest()
        {
            ID = LastRequestID++;
        }

        public bool NeedValue => InputType == InputType.IntValue || InputType == InputType.StrValue;
    }
}