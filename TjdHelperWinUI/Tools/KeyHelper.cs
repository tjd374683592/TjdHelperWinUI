using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public static class KeyHelper
    {
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static bool IsCtrlPressed()
        {
            const int VK_CONTROL = 0x11;
            return (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        }
    }
}
