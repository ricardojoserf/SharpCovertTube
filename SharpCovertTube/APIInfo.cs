using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpCovertTube
{
    [DataContract]
    public class Thumbnail_Obj
    {
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string width { get; set; }
        [DataMember]
        public string height { get; set; }
    }


    [DataContract]
    public class Id_Obj
    {
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string videoId { get; set; }
    }


    [DataContract]
    public class Thumbnail
    {
        [DataMember]
        public Thumbnail_Obj default_ { get; set; }
        [DataMember]
        public Thumbnail_Obj medium { get; set; }
        [DataMember]
        public Thumbnail_Obj high { get; set; }
    }


    [DataContract]
    public class Snippet
    {
        [DataMember]
        public string publishedAt { get; set; }
        [DataMember]
        public string channelId { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public Thumbnail thumbnails { get; set; }
        [DataMember]
        public string channelTitle { get; set; }
        [DataMember]
        public string liveBroadcastContent { get; set; }
        [DataMember]
        public string publishTime { get; set; }
    }


    [DataContract]
    public class Item {
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string etag { get; set; }
        [DataMember]
        public Id_Obj id { get; set; }
        [DataMember]
        public Snippet snippet { get; set; }
    }


    [DataContract]
    public class APIInfo
    {
        [DataMember]
        public string kind { get; set; }

        [DataMember]
        public string etag { get; set; }

        [DataMember]
        public string[] pageInfo { get; set; }

        [DataMember]
        public Item[] items { get; set; }
    }
}
