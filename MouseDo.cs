using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiFu
{
   public class MouseDo
    {

        [Flags]
        public enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            //XDOWN = 0x00000080,
            //XUP = 0x00000100,
            //WHEEL = 0x00000800,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
        }
        const int SM_CXSCREEN = 0x0;
        const int SM_CYSCREEN = 0x01;
        const uint WM_KEYUP = 0x101;
        const uint WM_KEYDOWN = 0x100;
        public enum MouseEventDataXButtons : uint
        {
            NONE = 0x00000000,
            XBUTTON1 = 0x00000001,
            XBUTTON2 = 0x00000002,
        }
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static void SendMouse(MouseEventFlags flags, MouseEventDataXButtons buttons, int x, int y)
        {
            if ((flags & MouseEventFlags.ABSOLUTE) == MouseEventFlags.ABSOLUTE)
            {
                int mx = GetSystemMetrics(SM_CXSCREEN);
                int my = GetSystemMetrics(SM_CYSCREEN);
                x = (int)(65536.0 / mx * x);
                y = (int)(65536.0 / my * y);
            }
            mouse_event((uint)flags, x, y, (uint)buttons, 0);
        }
        public static void RightClick()
        {
            Task.Run(() =>
            {

                Thread.Sleep(10);
                SendMouse(0 | MouseEventFlags.RIGHTDOWN, MouseEventDataXButtons.NONE, 500, 500);
                Thread.Sleep(10);
                SendMouse(0 | MouseEventFlags.RIGHTUP, MouseEventDataXButtons.NONE, 500, 500);
            });
        }
        public static void SendKeycode(uint keycode)
        {
            Process[] procs = Process.GetProcesses();
            foreach (var p in procs)
            {
                IntPtr hWnd = p.MainWindowHandle;
                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)keycode, (IntPtr)0);
                SendMessage(hWnd, WM_KEYUP, (IntPtr)keycode, (IntPtr)0);
            }
        }

    }
}
