using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace UniGetUI.ExternalLibraries.Clipboard
{
    public class Main
    {
        public static string? GetText()
        {
            return WindowsClipboard.GetText();
        }

        public static void SetText(string text)
        {
            WindowsClipboard.SetText(text);
        }
    }

    static class WindowsClipboard
    {
        const uint cfUnicodeText = 13;

        public static string? GetText()
        {
            if (!IsClipboardFormatAvailable(cfUnicodeText))
            {
                return null;
            }

            TryOpenClipboard();

            return InnerGet();
        }

        public static void SetText(string text)
        {
            TryOpenClipboard();

            InnerSet(text);
        }

        static void InnerSet(string text)
        {
            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                int bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                IntPtr target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        static string? InnerGet()
        {
            IntPtr handle = default;

            IntPtr pointer = default;
            try
            {
                handle = GetClipboardData(cfUnicodeText);
                if (handle == default)
                {
                    return null;
                }

                pointer = GlobalLock(handle);
                if (pointer == default)
                {
                    return null;
                }

                int size = GlobalSize(handle);
                byte[] buff = new byte[size];

                Marshal.Copy(pointer, buff, 0, size);

                return Encoding.Unicode.GetString(buff).TrimEnd('\0');
            }
            finally
            {
                if (pointer != default)
                {
                    GlobalUnlock(handle);
                }

                CloseClipboard();
            }
        }

        static void TryOpenClipboard()
        {
            int num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();



        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int GlobalSize(IntPtr hMem);
    }
}