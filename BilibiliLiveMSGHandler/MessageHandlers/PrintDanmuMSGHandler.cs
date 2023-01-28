using System.Drawing;
using System.Text.Json;

namespace BilibiliLiveMSGHandler.MessageHandlers
{
    /// <summary>
    /// 消息处理器
    /// 打印弹幕
    /// </summary>
    internal class PrintDanmuMSGHandler : MessageHandler
    {
        public override bool AllCmd => false;

        public override string MessageCmd { get; set; } = "DANMU_MSG";

        private static readonly object printLockObject = new();
        private static readonly Color defaultColor = Color.White;

        public override void MessageHendle(JsonElement messageElement)
        {
            JsonElement.ArrayEnumerator infoElement = messageElement.GetProperty("info").EnumerateArray();

            JsonElement notShowElement = infoElement.First().EnumerateArray().Last();
            if (notShowElement.GetProperty("not_show").GetInt32() == 1) // 不显示抽奖弹幕（大概，不确定not_show是什么）
            {
                return;
            }

            JsonElement.ArrayEnumerator fanMedalElement = infoElement.Skip(3).First().EnumerateArray();
            JsonElement fanMedalNameElement;
            JsonElement fanMedalLevelElement;
            JsonElement fanMedalIsLightElement;
            JsonElement isAdmin = infoElement.Skip(2).First().EnumerateArray().Skip(2).First();
            JsonElement userNameElement = infoElement.Skip(2).First().EnumerateArray().Skip(1).First();
            JsonElement chatElement = infoElement.Skip(1).First();

            string userFanMedal = string.Empty;
            Color userFanMedalColor = defaultColor;
            if (fanMedalElement.Any())
            {
                fanMedalNameElement = fanMedalElement.Skip(1).First();
                fanMedalLevelElement = fanMedalElement.First();
                fanMedalIsLightElement = fanMedalElement.Skip(11).First();

                userFanMedal = $"{fanMedalNameElement}>{fanMedalLevelElement}";

                if (fanMedalIsLightElement.GetInt32() == 0)
                {
                    userFanMedalColor = Color.Gray;
                }
            }

            string adminUser = string.Empty;
            Color adminUserColor = Color.Gold;
            if (isAdmin.GetInt32() == 1)
            {
                adminUser = "房 ";
            }

            string user = $"{userNameElement}：";
            Color userNameColor;
            string? userNameColorString = infoElement.Skip(2).First().EnumerateArray().Last().GetString();
            if (userNameColorString != null && userNameColorString.Length > 0)
            {
                userNameColor = ColorTranslator.FromHtml(userNameColorString);
            }
            else
            {
                userNameColor = defaultColor;
            }

            string chat = $"{chatElement}";
            Color chatColor = Color.FromArgb(infoElement.First().EnumerateArray().Skip(3).First().GetInt32());

            lock (printLockObject)
            {
                Console.Write($"\x1b[38;2;{userFanMedalColor.R};{userFanMedalColor.G};{userFanMedalColor.B}m{userFanMedal}\r\t");
                Console.CursorLeft += 2; // 对齐
                Console.Write($"\x1b[38;2;{adminUserColor.R};{adminUserColor.G};{adminUserColor.B}m{adminUser}");
                Console.Write($"\x1b[38;2;{userNameColor.R};{userNameColor.G};{userNameColor.B}m{user}");
                Console.Write($"\x1b[38;2;{chatColor.R};{chatColor.G};{chatColor.B}m{chat}");
                Console.WriteLine($"\x1b[38;2;{defaultColor.R};{defaultColor.G};{defaultColor.B}m");
            }
        }

        public PrintDanmuMSGHandler()
        {

        }

        public PrintDanmuMSGHandler(string messageCmd)
        {
            MessageCmd = messageCmd;
        }
    }
}
