using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WebChangeNotifier.Helpers
{
    public enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    public class ConsoleExitDetector
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        public delegate bool ConsoleCtrlHandler(CtrlType sig);

        private static ConsoleCtrlHandler _exitHandler;
        public static event ConsoleCtrlHandler ExitHandler
        {
            add
            {
                _exitHandler += value;
                SetConsoleCtrlHandler(value, true);
            }
            remove
            {
                // ReSharper disable once DelegateSubtraction
                _exitHandler -= value;
                SetConsoleCtrlHandler(value, false); 
            }
        } 
    }
}
