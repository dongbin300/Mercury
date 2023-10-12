using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Albedo.Utils
{
    public class GlobalMouseEvents
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc mouseProc;
        private IntPtr hookHandle = IntPtr.Zero;

        public event EventHandler MouseLeftButtonDown = default!;
        public event EventHandler MouseLeftButtonUp = default!;

        public GlobalMouseEvents()
        {
            mouseProc = MouseHookCallback;
            hookHandle = SetHook(mouseProc);
        }

        ~GlobalMouseEvents()
        {
            UnhookWindowsHookEx(hookHandle);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule ?? default!;
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var eventType = wParam.ToInt32();
                if (eventType == WM_LBUTTONDOWN)
                {
                    MouseLeftButtonDown?.Invoke(this, EventArgs.Empty);
                }
                else if (eventType == WM_LBUTTONUP)
                {
                    MouseLeftButtonUp?.Invoke(this, EventArgs.Empty);
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        #region Native Methods
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}
