using System;
using System.Runtime.InteropServices;

namespace Win32Interoperation {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OSVERSIONINFOEXW {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public UInt16 wServicePackMajor;
        public UInt16 wServicePackMinor;
        public UInt16 wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    public static partial class Interop {
        [DllImport("kernel32.dll")]
        public static extern ulong VerSetConditionMask(ulong ConditionMask, uint TypeMask, byte Condition);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool VerifyVersionInfoW([In] ref OSVERSIONINFOEXW lpVersionInformation, uint dwTypeMask, ulong dwlConditionMask);
    }
}
