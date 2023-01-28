using System.Text.Json;

namespace BilibiliLiveMSGHandler.MessageHandlers
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    internal abstract class MessageHandler
    {
        public abstract bool AllCmd { get; }
        public string HandlerCmd
        {
            get
            {
                if (AllCmd)
                {
                    return "All";
                }
                else
                {
                    return MessageCmd;
                }
            }
        }
        public abstract string MessageCmd { get; set; } // 当AllCmd为true时不会读取
        public abstract void MessageHendle(JsonElement messageElement);
    }
}
