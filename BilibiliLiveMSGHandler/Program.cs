using BilibiliLiveMSGHandler.MessageHandlers;

namespace BilibiliLiveMSGHandler
{
    internal class Program
    {
        static void Main()
        {
            int roomId;
            while (true)
            {
                Console.Write("请输入房间ID：");
                if (int.TryParse(Console.ReadLine(), out roomId))
                {
                    break;
                }
                Console.WriteLine("解析失败，请重试。");
            }
            LiveMSGClient.LiveMSGClient liveChat = new(roomId);
            liveChat.RegisterHandler(new PrintDanmuMSGHandler());
            //liveChat.RegisterHandler(new SaveLogHendler());
            liveChat.Connection();
            Console.ReadLine();
            liveChat.Close();
            //Console.ReadLine();
        }
    }
}