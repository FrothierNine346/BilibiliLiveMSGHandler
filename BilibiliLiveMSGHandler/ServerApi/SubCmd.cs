using System.Text.Json.Serialization;

namespace BilibiliLiveMSGHandler.ServerApi
{
    public enum ProtoVer
    {
        NORMAL = 0,
        HEARTBEAT = 1,
        DEFLATE = 2,
        BROTLI = 3
    }

    public enum Operation
    {
        HANDSHAKE = 0,
        HANDSHAKE_REPLY = 1,
        HEARTBEAT = 2,
        HEARTBEAT_REPLY = 3,
        SEND_MSG = 4,
        SEND_MSG_REPLY = 5,
        DISCONNECT_REPLY = 6,
        AUTH = 7,
        AUTH_REPLY = 8,
        RAW = 9,
        PROTO_READY = 10,
        PROTO_FINISH = 11,
        CHANGE_ROOM = 12,
        CHANGE_ROOM_REPLY = 13,
        REGISTER = 14,
        REGISTER_REPLY = 15,
        UNREGISTER = 16,
        UNREGISTER_REPLY = 17
    }

    public class AuthRootobject
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; } = 0;
        [JsonPropertyName("roomid")]
        public int Roomid { get; set; } = 3;
        [JsonPropertyName("protover")]
        public int Protover { get; set; } = (int)ProtoVer.NORMAL;
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = "web";
        [JsonPropertyName("type")]
        public int Type { get; set; } = 2;
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }
}
