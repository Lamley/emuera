using System.Runtime.InteropServices;

namespace MinorShift._Library
{
    /// <summary>
    ///     wrapされたtimer。外からは、このTickCountだけを呼び出す。
    /// </summary>
    internal sealed class WinmmTimer
    {
        /// <summary>
        ///     起動時にBeginPeriod、終了時にEndPeriodを呼び出すためだけのインスタンス。
        ///     staticなデストラクタがあればいらないんだけど
        /// </summary>
        private static volatile WinmmTimer instance;

        static WinmmTimer()
        {
            instance = new WinmmTimer();
        }

        private WinmmTimer()
        {
            mm_BeginPeriod(1);
        }

        public static uint TickCount => mm_GetTime();

        ~WinmmTimer()
        {
            mm_EndPeriod(1);
        }

        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        private static extern uint mm_GetTime();

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint mm_BeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint mm_EndPeriod(uint uMilliseconds);
    }
}