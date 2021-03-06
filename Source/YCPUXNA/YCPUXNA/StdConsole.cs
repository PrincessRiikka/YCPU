﻿using System;
using System.Runtime.InteropServices;

namespace YCPUXNA
{
    internal static class StdConsole
    {
        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, SW_HIDE);
        }

        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
            }
        }

        public static void StdOutWriteLine(string line)
        {
            Console.WriteLine(line);
        }

        public static ConsoleKeyInfo StdInReadKey()
        {
            return Console.ReadKey(true);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public static void Clear()
        {
            Console.Clear();
        }
    }
}
