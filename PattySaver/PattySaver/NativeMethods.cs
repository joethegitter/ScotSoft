using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;

namespace ScotSoft.PattySaver
{
    /// <summary>
    /// This class adds support for calling Win32 and Win64 API's directly.
    /// </summary>
    public class NativeMethods
    {

        public const int WM_SETICON = 0x80;
        public const int WM_NCHITTEST = 0x84;
        public const int WM_SYSCOMMAND = 0x0112;

        public const int GWL_STYLE = -16;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_DLGMODALFRAME = 0x00000001;
        public const int WS_EX_CONTEXTHELP = 0x00000400;

        public const int SC_CONTEXTHELP = 0xf180;

        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;


        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "This class is shared by various projects and may not be used by a specific project")]
        [StructLayout(LayoutKind.Sequential)]
        public sealed class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
                this.x = 0;
                this.y = 0;
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }



        [DllImport("user32.dll")]
        internal static extern void SetLastErrorEx(uint dwErrCode, uint dwType);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        internal enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        // This static method is required because legacy OSes do not support SetWindowLongPtr
        internal static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }


        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        internal static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        internal static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        [SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "return", Justification = "Calling code is expected to handle the different size of IntPtr")]
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        internal static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        internal static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This function is shared by various projects and may not be used by a specific project")]
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This function is shared by various projects and may not be used by a specific project")]
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetActiveWindow();

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This function is shared by various projects and may not be used by a specific project")]
        [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This function is shared by various projects and may not be used by a specific project")]
        [DllImport("User32", EntryPoint = "ScreenToClient", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int ScreenToClient(IntPtr hWnd, [In, Out] POINT pt);



        public static bool IsWindowVisible_Api(IntPtr hWnd)
        {
            return IsWindowVisible(hWnd);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public static bool GetClientRectViaApi(IntPtr hWnd, ref Rectangle rect)
        {
            return GetClientRect(hWnd, ref rect);
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);


        [DllImport("user32.DLL", EntryPoint = "IsWindowVisible", SetLastError = true)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr hWnd, ref Rectangle rect);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);

        public static string CompactPath(string longPathName, int wantedLength)
        {
            // NOTE: You need to create the builder with the required capacity before calling function.
            // See http://msdn.microsoft.com/en-us/library/aa446536.aspx
            StringBuilder sb = new StringBuilder(wantedLength + 1);
            PathCompactPathEx(sb, longPathName, wantedLength + 1, 0);
            return sb.ToString();
        }

        public enum WindowLongFlags : int
        {
            GWL_EXSTYLE = -20,
            GWLP_HINSTANCE = -6,
            GWLP_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4,
            DWLP_USER = 0x8,
            DWLP_MSGRESULT = 0x0,
            DWLP_DLGPROC = 0x4
        }

        public static class WindowStyles
        {

            public static readonly Int32

            WS_BORDER = 0x00800000,

            WS_CAPTION = 0x00C00000,

            WS_CHILD = 0x40000000,

            WS_CHILDWINDOW = 0x40000000,

            WS_CLIPCHILDREN = 0x02000000,

            WS_CLIPSIBLINGS = 0x04000000,

            WS_DISABLED = 0x08000000,

            WS_DLGFRAME = 0x00400000,

            WS_GROUP = 0x00020000,

            WS_HSCROLL = 0x00100000,

            WS_ICONIC = 0x20000000,

            WS_MAXIMIZE = 0x01000000,

            WS_MAXIMIZEBOX = 0x00010000,

            WS_MINIMIZE = 0x20000000,

            WS_MINIMIZEBOX = 0x00020000,

            WS_OVERLAPPED = 0x00000000,

            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

            WS_POPUP = unchecked((int)0x80000000),

            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,

            WS_SIZEBOX = 0x00040000,

            WS_SYSMENU = 0x00080000,

            WS_TABSTOP = 0x00010000,

            WS_THICKFRAME = 0x00040000,

            WS_TILED = 0x00000000,

            WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

            WS_VISIBLE = 0x10000000,

            WS_VSCROLL = 0x00200000;

        }
    }
}
