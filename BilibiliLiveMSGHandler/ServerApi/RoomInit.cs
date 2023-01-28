using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilibiliLiveMSGHandler.ServerApi
{
    internal class RoomInit
    {
        public static RoomInitRootobject GetRoomInit(HttpClient client, int roomId)
        {
            int reTry = 0;
            Stream messageStream = new MemoryStream();
            while (reTry < 3)
            {
                HttpResponseMessage message = client.GetAsync($"https://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}").Result;
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
                            messageStream.Dispose();
                            throw new NotSupportedException("不支持的压缩类型");
                        }
                }

                messageStream.Position = 0;
                RoomInitRootobject? roomInitRootobject = JsonSerializer.Deserialize<RoomInitRootobject>(messageStream);
                messageStream.Dispose();
                if (roomInitRootobject == null)
                {
                    reTry++;
                }
                else
                {
                    return roomInitRootobject;
                }
            }
            Console.WriteLine("初始化房间3次失败，按任意键退出。");
            Console.ReadKey();
            Environment.Exit(0);
            return new();
        }
    }
    public class RoomInitRootobject
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public RoomInitData? Data { get; set; }
    }

    public class RoomInitData
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }
        [JsonPropertyName("short_id")]
        public int ShortId { get; set; }
        [JsonPropertyName("uid")]
        public int Uid { get; set; }
        [JsonPropertyName("need_p2p")]
        public int NeedP2P { get; set; }
        [JsonPropertyName("is_hidden")]
        public bool IsHidden { get; set; }
        [JsonPropertyName("is_locked")]
        public bool IsLocked { get; set; }
        [JsonPropertyName("is_portrait")]
        public bool IsPortrait { get; set; }
        [JsonPropertyName("live_status")]
        public int LiveStatus { get; set; }
        [JsonPropertyName("hidden_till")]
        public int HiddenTill { get; set; }
        [JsonPropertyName("lock_till")]
        public int LockTill { get; set; }
        [JsonPropertyName("encrypted")]
        public bool Encrypted { get; set; }
        [JsonPropertyName("pwd_verified")]
        public bool PwdVerified { get; set; }
        [JsonPropertyName("live_time")]
        public long LiveTime { get; set; }
        [JsonPropertyName("room_shield")]
        public int RoomShield { get; set; }
        [JsonPropertyName("is_sp")]
        public int IsSp { get; set; }
        [JsonPropertyName("special_type")]
        public int SpecialType { get; set; }
    }

}
