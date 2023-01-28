using BilibiliLiveMSGHandler.Config;
using BilibiliLiveMSGHandler.MessageHandlers;
using BilibiliLiveMSGHandler.ServerApi;
using System.Net.WebSockets;
using System.Text.Json;

namespace BilibiliLiveMSGHandler.LiveMSGClient
{
    internal class LiveMSGClient
    {
        public DanmuInfoRootobject danmuInfo;

        public int roomId;
        public Uri chatServer;
        public CancellationTokenSource stopCancellationTokenSource;
        public CancellationToken stopCancellationToken;

        public ClientWebSocket client;

        private readonly Dictionary<string, MessageHandler[]> handlers = new() { { "All", Array.Empty<MessageHandler>() } };
        private bool isAuth = false;
        private bool isClose = false;
        private int reTryCount = 0;

        public LiveMSGClient(int RoomId)
        {
            danmuInfo = DanmuInfo.GetDanmuInfo(RoomId, out roomId);

            if (danmuInfo.Data != null)
            {
                if (danmuInfo.Data.HostList != null)
                {
                    chatServer = danmuInfo.Data.HostList.First().ChatServer;
                }
                else
                {
                    throw new ArgumentNullException(nameof(danmuInfo.Data.HostList), "danmuInfo.Data.HostList is null.");
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(danmuInfo.Data), "danmuInfo.Data is null.");
            }
            stopCancellationTokenSource = new CancellationTokenSource();
            stopCancellationToken = stopCancellationTokenSource.Token;

            client = new ClientWebSocket();
            client.Options.SetRequestHeader("Accept-Encoding", "br");
            client.Options.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            client.Options.SetRequestHeader("Cache-Control", "no-cache");
            client.Options.SetRequestHeader("Host", $"{chatServer.Host}:{chatServer.Port}");
            client.Options.SetRequestHeader("Origin", "https://live.bilibili.com");
            client.Options.SetRequestHeader("Origin", "https://live.bilibili.com");
            client.Options.SetRequestHeader("Pragma", "no-cache");
            client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 Edg/109.0.1518.61");
        }

        /// <summary>
        /// 连接弹幕WebSocket服务器并认证、心跳
        /// </summary>
        public void Connection()
        {
            client.ConnectAsync(chatServer, stopCancellationToken).Wait();
            Auth();
            Receive();
            while (!isAuth)
            {
                if (stopCancellationToken.IsCancellationRequested) { return; }
            }
            Console.WriteLine("服务器连接成功");
            Heart();
        }

        public async void Receive()
        {
            List<byte> data = new();
            byte[] buffer = new byte[1024 * 1024 * 4];
            WebSocketReceiveResult result;

            while (true)
            {
                try
                {
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), stopCancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (WebSocketException)
                {
                    ReTry();
                    return;
                }
                data.AddRange(buffer.Take(result.Count));
                if (result.EndOfMessage)
                {
                    ParseReceiveData(data.ToArray());
                    data = new();
                }
            }
        }

        public async void ParseReceiveData(byte[] receiveByte)
        {
            await Task.Run(() =>
            {
                foreach (JsonElement receiveElement in MessageManager.MessageManager.UnPack(receiveByte))
                {
                    MessageHandle(receiveElement);
                }
            });
        }

        public void RegisterHandler(MessageHandler messageHandler)
        {
            if (handlers.TryGetValue(messageHandler.HandlerCmd, out MessageHandler[]? handler))
            {
                handlers[messageHandler.HandlerCmd] = handler.Append(messageHandler).ToArray();
            }
            else
            {
                handlers.Add(messageHandler.HandlerCmd, new MessageHandler[] { messageHandler });
            }
        }

        public async void MessageHandle(JsonElement messageElement)
        {
            await Task.Run(() =>
            {
                if (messageElement.TryGetProperty("cmd", out JsonElement CmdElement))
                {
                    string? cmd = CmdElement.GetString();
                    if (cmd != null)
                    {
                        foreach (MessageHandler allHandler in handlers["All"])
                        {
                            allHandler.MessageHendle(messageElement);
                        }
                        if (handlers.TryGetValue(cmd, out MessageHandler[]? messageHandlers))
                        {
                            if (messageHandlers != null)
                            {
                                foreach (MessageHandler messageHandler in messageHandlers)
                                {
                                    messageHandler.MessageHendle(messageElement);
                                }
                            }
                        }
                    }
                }
                else if (messageElement.TryGetProperty("code", out _))
                {
                    isAuth = true;
                }
            });
        }

        private void Auth()
        {
            AuthRootobject auth;
            ConfigRootobject config = Config.Config.GetConfig();
            if (danmuInfo.Data != null)
            {
                auth = new AuthRootobject()
                {
                    Uid = config.Uid,
                    Roomid = roomId,
                    Key = danmuInfo.Data.Token,
                };
            }
            else
            {
                throw new ArgumentNullException("danmuInfo.Data is null.");
            }
            Send(JsonSerializer.Serialize(auth), Operation.AUTH);
        }

        private async void Heart()
        {
            while (!stopCancellationToken.IsCancellationRequested)
            {
                Send("", Operation.HEARTBEAT);
                await Task.Delay(30000);
            }
        }

        public async void Send(string data, Operation operation, bool brotltCompress = false)
        {
            if (client.State == WebSocketState.Open && !stopCancellationToken.IsCancellationRequested)
            {
                await client.SendAsync(MessageManager.MessageManager.Pack(data, operation, brotltCompress), WebSocketMessageType.Binary, true, stopCancellationToken);
            }
        }

        public void ReTry()
        {
            if (reTryCount < 5)
            {
                reTryCount++;
                Console.WriteLine($"服务器连接失败，重试第{reTryCount}次");
                stopCancellationTokenSource.Cancel();
                stopCancellationTokenSource.Dispose();
                stopCancellationTokenSource = new();
                stopCancellationToken = stopCancellationTokenSource.Token;
                client = new ClientWebSocket();
                client.Options.SetRequestHeader("Accept-Encoding", "br");
                client.Options.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                client.Options.SetRequestHeader("Cache-Control", "no-cache");
                client.Options.SetRequestHeader("Host", $"{chatServer.Host}:{chatServer.Port}");
                client.Options.SetRequestHeader("Origin", "https://live.bilibili.com");
                client.Options.SetRequestHeader("Origin", "https://live.bilibili.com");
                client.Options.SetRequestHeader("Pragma", "no-cache");
                client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 Edg/109.0.1518.61");
                Connection();
            }
            else
            {
                Close();
            }
        }

        public void Close(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? statusDescription = null)
        {
            if (!isClose)
            {
                Console.WriteLine("Close");
                stopCancellationTokenSource.Cancel();
                client.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
                client.Dispose();
                isClose = true;
            }
        }
    }
}
