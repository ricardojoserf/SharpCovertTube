using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;


namespace SharpCovertTube
{
    internal class Program
    {
        static List<string> GetVideoIds(string channel_url)
        {
            List<string> VideoIds = new List<string>();
            WebRequest wrGETURL;
            wrGETURL = WebRequest.Create(channel_url);
            Stream objStream;
            objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            string sLine = "";
            int i = 0;

            while (sLine != null)
            {
                i++;
                sLine = objReader.ReadLine();
                if (sLine != null)
                {
                    if (sLine.IndexOf("videoId") != -1)
                    {
                        int index1 = sLine.IndexOf(":");
                        int index2 = sLine.IndexOf("\"", index1);
                        int index3 = sLine.IndexOf("\"", index2 + 1);
                        string video_id = sLine.Substring(index2 + 1, (index3 - index2 - 1));
                        VideoIds.Add(video_id);
                    }
                }
            }
            return VideoIds;
        }


        static void Main(string[] args)
        {
            // FILL VALUES
            string channel_id = "";
            string api_key = "";
            int seconds_delay = 60;

            string base_video_url = "https://www.youtube.com/watch?v=";
            string base_search_url = "https://www.googleapis.com/youtube/v3/search?";

            string channel_url = base_search_url + "part=snippet&channelId=" + channel_id + "&maxResults=100&order=date&type=video&key=" + api_key;
            Console.WriteLine("[+] URL to test for file upload: {0}", channel_url);

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
                if (VideoIds.Count > number_of_videos)
                {
                    Console.WriteLine("[+] New videos uploaded!");

                    var firstNotSecond = VideoIds.Except(Initial_VideoIds);
                    foreach (var video_id in firstNotSecond)
                    {
                        Console.WriteLine("[+] New ID: \t{0}", video_id);
                    }
                    
                    number_of_videos = VideoIds.Count;
                    Initial_VideoIds = VideoIds;
                }
                else {
                    Console.WriteLine("[+] No new videos... Count is {0}", VideoIds.Count);
                }
            }
        }
    }
}
