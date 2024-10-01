using System.Runtime.InteropServices;

namespace OSKTrayChat
{
    public partial class MainWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}