using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilibiliLiveMSGHandler.ServerApi
{
    public class DanmuInfo
    {
        public static DanmuInfoRootobject GetDanmuInfo(int roomId, out int realRoomId)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("cookie", Config.Config.GetConfig().Cookie);
            client.DefaultRequestHeaders.Add("origin", "https://live.bilibili.com");
            client.DefaultRequestHeaders.Add("referer", $"https://live.bilibili.com/{roomId}");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 Edg/109.0.1518.55");

            RoomInitRootobject roomInit = RoomInit.GetRoomInit(client, roomId);
            if (roomInit.Data != null)
            {
                realRoomId = roomInit.Data.RoomId;
                if (roomInit.Data.LiveStatus == 2)
                {
                    Console.WriteLine("直播间未开播");
                }
                if (roomInit.Data.IsLocked)
                {
                    Console.WriteLine("直播间是上锁的，程序退出");
                    Environment.Exit(0);
                }
                if (roomInit.Data.IsHidden)
                {
                    Console.WriteLine("直播间是隐藏的，程序退出");
                    Environment.Exit(0);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(roomInit.Data), "roomInit.Data is null.");
            }

            Stream messageStream = new MemoryStream();
            HttpResponseMessage message = client.GetAsync($"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={realRoomId}&type=0").Result;
            switch (message.Content.Headers.GetValues("content-encoding").First())
            {
                case "br":
                    {
                        MessageManager.MessageManager.Decompress(message.Content.ReadAsStream(), MessageManager.MessageManager.CompressType.Brotli, out messageStream);
                        break;
                    };
                case "gzip":
                    {
                        MessageManager.MessageManager.Decompress(message.Content.ReadAsStream(), MessageManager.MessageManager.CompressType.Gzip, out messageStream);
                        break;
                    };
                case "deflate":
                    {
                        MessageManager.MessageManager.Decompress(message.Content.ReadAsStream(), MessageManager.MessageManager.CompressType.Deflate, out messageStream);
                        break;
                    };
                default:
                    {
                        Console.WriteLine("ResponseMessage headers: content-encoding error, value not in switch.");
                        Environment.Exit(0);
                        break;
                    }
            }

            messageStream.Position = 0;
            DanmuInfoRootobject? danmuInfo = JsonSerializer.Deserialize<DanmuInfoRootobject>(messageStream);
            messageStream.Dispose();
            if (danmuInfo != null)
            {
                return danmuInfo;
            }
            else
            {
                throw new ArgumentNullException(nameof(danmuInfo), "danmuInfo is null.");
            }
        }
    }

    public class DanmuInfoRootobject
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }
        [JsonPropertyName("data")]
        public DanmuInfoData? Data { get; set; }
    }

    public class DanmuInfoData
    {
        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;
        [JsonPropertyName("business_id")]
        public int BusinessId { get; set; }
        [JsonPropertyName("refresh_row_factor")]
        public float RefreshRowFactor { get; set; }
        [JsonPropertyName("refresh_rate")]
        public int RefreshRate { get; set; }
        [JsonPropertyName("max_delay")]
        public int MaxDelay { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("host_list")]
        public Host_List[]? HostList { get; set; }
    }

    public class Host_List
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;
        [JsonPropertyName("port")]
        public int Port { get; set; }
        [JsonPropertyName("wss_port")]
        public int WssPort { get; set; }
        [JsonPropertyName("ws_port")]
        public int WsPort { get; set; }

        [JsonIgnore]
        public Uri ChatServer => new($"wss://{Host}:{WssPort}/sub");
    }
}
