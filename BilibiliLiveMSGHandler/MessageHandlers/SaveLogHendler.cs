using System.Text.Encodings.Web;
using System.Text.Json;

namespace BilibiliLiveMSGHandler.MessageHandlers
{
    /// <summary>
    /// 消息处理器
    /// 保存所有消息为json文件在Log文件夹下，会覆盖已有文件
    /// </summary>
    internal class SaveLogHendler : MessageHandler
    {
        public override bool AllCmd => true;

        public override string MessageCmd { get; set; } = "";

        private int number = 1;

        private readonly object numberLockObject = new();

        public override void MessageHendle(JsonElement messageElement)
        {
            Directory.CreateDirectory("Log");
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            lock (numberLockObject)
            {
                File.WriteAllBytes($"Log\\{number}.json", JsonSerializer.SerializeToUtf8Bytes(messageElement, options));
                number++;
            }
        }
    }
}
