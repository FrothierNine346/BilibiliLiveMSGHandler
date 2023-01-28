using BilibiliLiveMSGHandler.ServerApi;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BilibiliLiveMSGHandler.MessageManager
{
    internal class MessageManager
    {
        private const int HeaderLength = 16;

        private static readonly Range HeaderPackLen = new(0, 4);
        private static readonly Range HeaderSize = new(4, 6);
        private static readonly Range HeaderVer = new(6, 8);
        private static readonly Range HeaderOperation = new(8, 12);
        private static readonly Range HeaderSqeId = new(12, 16);

        private class Header
        {
            public readonly byte[] ReceiveBytes;
            public int PackHeaderPackLen => BitConverter.ToInt32(ReceiveBytes[HeaderPackLen].Reverse().ToArray());
            public short PackHeaderSize => BitConverter.ToInt16(ReceiveBytes[HeaderSize].Reverse().ToArray());
            public short PackHeaderVer => BitConverter.ToInt16(ReceiveBytes[HeaderVer].Reverse().ToArray());
            public int PackHeaderOperation => BitConverter.ToInt32(ReceiveBytes[HeaderOperation].Reverse().ToArray());
            public int PackHeaderSqeId => BitConverter.ToInt32(ReceiveBytes[HeaderSqeId].Reverse().ToArray());

            public Header(byte[] receiveBytes)
            {
                ReceiveBytes = receiveBytes;
            }

            public Header(int packDataLen, Operation PackHeaderOperation)
            {

                byte[] header = Array.Empty<byte>();

                header = header.Concat(BitConverter.GetBytes(HeaderLength + packDataLen).Reverse()).ToArray();
                header = header.Concat(BitConverter.GetBytes((short)HeaderLength).Reverse()).ToArray();
                header = header.Concat(BitConverter.GetBytes((short)ProtoVer.HEARTBEAT).Reverse()).ToArray();
                header = header.Concat(BitConverter.GetBytes((int)PackHeaderOperation).Reverse()).ToArray();
                header = header.Concat(BitConverter.GetBytes(1).Reverse()).ToArray();

                ReceiveBytes = header;
            }
        }

        public static byte[] Pack(string data, Operation operation, bool brotltCompress = false)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            Header header = new(dataBytes.Length, operation);

            if (brotltCompress)
            {
                using MemoryStream stream = new();
                using (BrotliStream brotliStream = new(stream, CompressionMode.Compress))
                {
                    brotliStream.Write(dataBytes, 0, dataBytes.Length);
                }
                dataBytes = stream.ToArray();
            }

            return header.ReceiveBytes.Concat(dataBytes).ToArray();
        }

        public static IEnumerable<JsonElement> UnPack(byte[] receiveBytes)
        {
            Header header = new(receiveBytes[..HeaderLength]);
            byte[] messageBytes;

            if (header.PackHeaderOperation == (int)Operation.SEND_MSG_REPLY || header.PackHeaderOperation == (int)Operation.AUTH_REPLY)
            {
                if (header.PackHeaderVer == (short)ProtoVer.BROTLI)
                {
                    //Console.WriteLine("BROTLI");
                    byte[] dataBytes = receiveBytes[header.PackHeaderSize..].ToArray();
                    messageBytes = Decompress(dataBytes, CompressType.Brotli);
                    foreach (JsonElement messageElement in UnPack(messageBytes))
                    {
                        yield return messageElement;
                    }
                }
                else if (header.PackHeaderVer == (short)ProtoVer.DEFLATE)
                {
                    //Console.WriteLine("DEFLATE");
                    byte[] dataBytes = receiveBytes[header.PackHeaderSize..].ToArray();
                    messageBytes = Decompress(dataBytes, CompressType.Deflate);
                    foreach (JsonElement messageElement in UnPack(messageBytes))
                    {
                        yield return messageElement;
                    }
                }
                else if (header.PackHeaderVer == (short)ProtoVer.NORMAL)
                {
                    //Console.WriteLine("NORMAL");
                    messageBytes = (byte[])receiveBytes.Clone();

                    int offset = 0;
                    while (true)
                    {
                        byte[] tempBytes = messageBytes[(offset + header.PackHeaderSize)..(offset + header.PackHeaderPackLen)];
                        using MemoryStream tempStream = new(tempBytes);
                        using JsonDocument tempDocument = JsonDocument.Parse(tempStream);
                        yield return tempDocument.RootElement.Clone();

                        offset += header.PackHeaderPackLen;
                        if (offset < messageBytes.Length)
                        {
                            header = new(messageBytes[(offset)..(offset + HeaderLength)]);
                        }
                        else
                        {
                            break;
                        }
                    };
                }
                else if (header.PackHeaderVer == (short)ProtoVer.HEARTBEAT)
                {
                    if (header.PackHeaderOperation == (short)Operation.AUTH_REPLY) // TODO:服务器心跳回应被忽略，包含人气值
                    {
                        //Console.WriteLine("HEARTBEAT");
                        byte[] dataBytes = receiveBytes[header.PackHeaderSize..].ToArray();
                        messageBytes = (byte[])dataBytes.Clone();
                        while (messageBytes.Length > 0)
                        {
                            byte[] tempBytes = messageBytes[..(header.PackHeaderPackLen - header.PackHeaderSize)];
                            using JsonDocument tempDocument = JsonDocument.Parse(tempBytes);
                            yield return tempDocument.RootElement.Clone();

                            messageBytes = messageBytes[tempBytes.Length..];
                        };
                    }
                }
                else
                {
                    throw new Exception("收到未知类型包。");
                }
            }
        }

        public enum CompressType
        {
            Gzip = 1,
            Deflate = 2,
            Brotli = 3
        }

        public static byte[] Decompress(byte[] dataBytes, CompressType compressType)
        {
            switch (compressType)
            {
                case CompressType.Gzip:
                    {
                        using MemoryStream dataStream = new();
                        using MemoryStream messageStream = new();
                        dataStream.Write(dataBytes, 0, dataBytes.Length);
                        dataStream.Position = 0;
                        using GZipStream gZipStream = new(dataStream, CompressionMode.Decompress);
                        gZipStream.CopyTo(messageStream);
                        messageStream.Position = 0;
                        return messageStream.ToArray();
                    }
                case CompressType.Deflate:
                    {
                        using MemoryStream dataStream = new();
                        using MemoryStream messageStream = new();
                        dataStream.Write(dataBytes, 0, dataBytes.Length);
                        dataStream.Position = 0;
                        using DeflateStream deflateStream = new(dataStream, CompressionMode.Decompress);
                        deflateStream.CopyTo(messageStream);
                        messageStream.Position = 0;
                        return messageStream.ToArray();
                    }
                case CompressType.Brotli:
                    {
                        using MemoryStream dataStream = new();
                        using MemoryStream messageStream = new();
                        dataStream.Write(dataBytes, 0, dataBytes.Length);
                        dataStream.Position = 0;
                        using BrotliStream brotliStream = new(dataStream, CompressionMode.Decompress);
                        brotliStream.CopyTo(messageStream);
                        messageStream.Position = 0;
                        return messageStream.ToArray();
                    }
                default:
                    throw new NotSupportedException("不支持的压缩类型");
            }
        }
        public static void Decompress(Stream dataStream, CompressType compressType, out Stream decompressDataStream)
        {
            decompressDataStream = new MemoryStream();
            switch (compressType)
            {
                case CompressType.Gzip:
                    {
                        using Stream messageStream = new MemoryStream();
                        using GZipStream messageDecompress = new(dataStream, CompressionMode.Decompress);
                        messageDecompress.CopyTo(decompressDataStream);
                        break;
                        //return messageStream;
                    }
                case CompressType.Deflate:
                    {
                        using Stream messageStream = new MemoryStream();
                        using DeflateStream messageDecompress = new(dataStream, CompressionMode.Decompress);
                        messageDecompress.CopyTo(decompressDataStream);
                        break;
                        //return messageStream;
                    }
                case CompressType.Brotli:
                    {
                        using Stream messageStream = new MemoryStream();
                        using BrotliStream messageDecompress = new(dataStream, CompressionMode.Decompress);
                        messageDecompress.CopyTo(decompressDataStream);
                        break;
                        //return messageStream;
                    }
                default:
                    throw new NotSupportedException("不支持的压缩类型");
            }
        }
    }
}
