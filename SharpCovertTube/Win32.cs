using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpCovertTube
{
    internal class Win32
    {
        //////////////////// CONSTANTS ////////////////////
        public const uint DELETE = (uint)0x00010000L;
        public const uint SYNCHRONIZE = (uint)0x00100000L;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint OPEN_EXISTING = 3;
        public const int MAX_PATH = 256;


        /////////////////////  ENUMS  /////////////////////
        public enum FileInformationClass : int
        {
            FileBasicInfo = 0,
            FileStandardInfo = 1,
            FileNameInfo = 2,
            FileRenameInfo = 3,
            FileDispositionInfo = 4,
            FileAllocationInfo = 5,
            FileEndOfFileInfo = 6,
            FileStreamInfo = 7,
            FileCompressionInfo = 8,
            FileAttributeTagInfo = 9,
            FileIdBothDirectoryInfo = 10,
            FileIdBothDirectoryRestartInfo = 11,
            FileIoPriorityHintInfo = 12,
            FileRemoteProtocolInfo = 13,
            FileFullDirectoryInfo = 14,
            FileFullDirectoryRestartInfo = 15,
            FileStorageInfo = 16,
            FileAlignmentInfo = 17,
            FileIdInfo = 18,
            FileIdExtdDirectoryInfo = 19,
            FileIdExtdDirectoryRestartInfo = 20,
        }


        //////////////////// FUNCTIONS //////////////////// 
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(
            out int Description,
            int ReservedValue);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            uint lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            uint hTemplateFile);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern int SetFileInformationByHandle(
            IntPtr hFile,
            FileInformationClass FileInformationClass,
            IntPtr FileInformation,
            Int32 dwBufferSize);
    
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)][PreserveSig]
        public static extern uint GetModuleFileName(
            [In] IntPtr hModule,
            [Out] StringBuilder lpFilename,
            [In][MarshalAs(UnmanagedType.U4)] int nSize);


        ///////////////////// STRUCTS /////////////////////
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct filerenameinfo_struct {
            public bool ReplaceIfExists;
            public IntPtr RootDirectory;
            public uint FileNameLength;
            public fixed byte filename[255]; 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct filedispositioninfo_struct { 
            public bool DeleteFile; 
        }
    }
}
