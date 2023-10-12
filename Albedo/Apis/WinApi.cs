using System.Runtime.InteropServices;
using System.Windows;

namespace Albedo.Apis
{
    public class WinApi
    {
        public static int VK_LBUTTON = 0x01; // Left Mouse Button
        public static int VK_RBUTTON = 0x02; // Right Mouse Button
        public static int VK_MBUTTON = 0x04; // Middle Mouse Button
        public static int VK_TAB = 0x09;
        public static int VK_RETURN = 0x0D; // == Enter Key
        public static int VK_ENTER = VK_RETURN;
        public static int VK_SHIFT = 0x10;
        public static int VK_CONTROL = 0x11;
        public static int VK_MENU = 0x12; // Alt Key
        public static int VK_CAPITAL = 0x14; // Caps Lock Key
        public static int VK_ESCAPE = 0x1B; // == ESC key
        public static int VK_ESC = VK_ESCAPE;
        public static int VK_LEFT = 0x25;
        public static int VK_UP = 0x26;
        public static int VK_RIGHT = 0x27;
        public static int VK_DOWN = 0x28;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("User32.dll")]
        public static extern bool GetCursorPos(ref POINT lpPoint);

        public static Point GetCursorPosition()
        {
            var lpPoint = new POINT();
            GetCursorPos(ref lpPoint);
            return new Point(lpPoint.X, lpPoint.Y);
        }

        public static bool IsMouseLeftButtonDown()
        {
            return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        }

        public static bool IsMouseRightButtonDown()
        {
            return (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
        }
    }
}
