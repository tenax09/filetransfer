using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileConnection
{
    public enum CMD : int
    {
        None = 0,
        C2S_Init = 1,
        C2S_Data = 2,
        C2S_End = 3,
        S2C_Start = 11,
        S2C_End = 12,
        S2C_Current = 13,
        S2C_Error = 100,
    }

    public class FilePacket
    {
        public CMD TypeID = 0;
        public string Token = string.Empty;
        public string FileName = string.Empty;
        public long FileLen = 0;
        public long Position = 0;
        public int DataLen = 0;
        public byte[] Data;

        public override string ToString()
        {
            string str = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", TypeID, Token, FileName, FileLen, Position, DataLen);
            return str;
        }
        public void ReadPacket(BinaryReader reader)
        {
            TypeID = (CMD)reader.ReadInt32();
            Token = reader.ReadString();
            FileName = reader.ReadString();
            FileLen = reader.ReadInt64();
            Position = reader.ReadInt64();
            DataLen = reader.ReadInt32();
            if (DataLen > 0)
            {
                Data = reader.ReadBytes(DataLen);
            }
        }
        public void WritePacket(BinaryWriter writer)
        {
            writer.Write((int)TypeID);
            writer.Write(Token);
            writer.Write(FileName);
            writer.Write(FileLen);
            writer.Write(Position);
            writer.Write(DataLen);
            if (DataLen > 0)
            {
                writer.Write(Data);
            }
        }
        public byte[] GetPacketBytes()
        {
            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                stream.SetLength(4);
                stream.Seek(4, SeekOrigin.Begin);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    WritePacket(writer);
                    stream.Seek(0, SeekOrigin.Begin);
                    writer.Write((int)stream.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                }
            }
            return data;
        }
    }


    public class ServerConnection
    {
        private TcpClient _client;
        public bool _connected;
        public Func<string, bool> Output { get; set; } = null;

        private readonly ConcurrentQueue<FilePacket> _recvList = new ConcurrentQueue<FilePacket>();
        private Queue<FilePacket> _sendList = new Queue<FilePacket>();

        private byte[] _rawData = new byte[0];

        bool isLogin = false;

        bool _finished = false;

        string recvFilePath = string.Empty;

        long filePosition = 0, fileLength = 0;

        string filePath = string.Empty;

        string fileName = string.Empty;

        FileStream fileStream = null;

        BinaryWriter writer = null;

        
        public ServerConnection(TcpClient client, string _recvFilePath)
        {
            _client = client;
            _client.NoDelay = true;
            _connected = true;
            recvFilePath = _recvFilePath;
            OutputLog(_client.Client.RemoteEndPoint.ToString() + " 已连接 ");

            new Thread(() =>
            {
                while (_connected && !_finished)
                {
                    try
                    {
                        while (_recvList != null && !_recvList.IsEmpty)
                        {
                            FilePacket p;
                            if (!_recvList.TryDequeue(out p)) continue;
                            ProcessPacket(p);
                        }
                        if (_sendList == null || _sendList.Count <= 0) continue;
                        List<byte> data = new List<byte>();
                        while (_sendList.Count > 0)
                        {
                            FilePacket p = _sendList.Dequeue();
                            if (p == null) continue;
                            data.AddRange(p.GetPacketBytes());
                        }
                        BeginSend(data);
                    }
                    catch (Exception ex)
                    {
                        Notify(ex);
                    }
                    Enqueue(new FilePacket { TypeID = CMD.S2C_Current, Position = filePosition });
                    Thread.Sleep(1);
                }
                
                if (fileStream != null && writer != null)
                {
                    OutputLog(string.Format("文件 {0} 在 {1} 中断开了", fileName, filePosition));
                    Enqueue(new FilePacket { TypeID = CMD.S2C_Current, Position = filePosition }, true);

                    writer.Flush();
                    fileStream.Flush();
                    writer.Close();
                    writer.Dispose();
                    fileStream.Close();
                    fileStream.Dispose();
                    writer = null;
                    fileStream = null;
                    if (filePosition == fileLength)
                    {
                        OutputLog(string.Format("{0} 已接收完毕...", fileName));
                        if (File.Exists(filePath + ".cfg"))
                            File.Delete(filePath + ".cfg");
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(filePath + ".cfg", FileMode.Create))
                        using (BinaryWriter wr = new BinaryWriter(fs))
                        {
                            wr.Write(fileLength);
                            wr.Write(filePosition);
                        }
                    }
                }
                Disconnect();
            })
            { IsBackground = true }.Start();

            BeginReceive();

        }

        private void ProcessPacket(FilePacket p)
        {
            if (p.TypeID == CMD.C2S_Init)
            {
                OutputLog(p.ToString());
                if (!File.Exists(@"./tokens/" + p.Token))
                {
                    Enqueue(new FilePacket { TypeID = CMD.S2C_Error, Token = "未登陆验证" });
                    OutputLog("非法客户端: " + _client.Client.RemoteEndPoint.ToString());
                    Disconnect();
                    return;
                }
                isLogin = true;
                filePath = recvFilePath + "\\" + p.FileName;
                fileName = p.FileName;
                fileLength = p.FileLen;
                if (File.Exists(filePath))
                {
                    if (File.Exists(filePath + ".cfg"))
                    {
                        using (FileStream fs = new FileStream(filePath + ".cfg", FileMode.Open))
                        using (BinaryReader reader = new BinaryReader(fs))
                        {
                            if (reader.ReadInt64() != fileLength)
                            {
                                //已存在同名的文件,拒绝接收
                                Enqueue(new FilePacket { TypeID = CMD.S2C_Error, Token = "已存在同名的文件,服务器拒绝接收" });
                                Disconnect();
                                return;
                            }
                            filePosition = reader.ReadInt64();
                        }
                        fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
                        fileStream.Seek(filePosition, SeekOrigin.Begin);
                        writer = new BinaryWriter(fileStream);
                        Enqueue(new FilePacket { TypeID = CMD.S2C_Start, Position = filePosition });
                        return;
                    }
                    //已存在同名同大小的文件,拒绝接收
                    Enqueue(new FilePacket { TypeID = CMD.S2C_Error, Token = "已存在同名的文件,服务器拒绝接收" });
                    Disconnect();
                    return;
                }
                else
                {
                    fileStream = File.Create(filePath);
                    writer = new BinaryWriter(fileStream);
                    Enqueue(new FilePacket { TypeID = CMD.S2C_Start });
                }
                return;
            }
            if (!isLogin)
            {
                Enqueue(new FilePacket { TypeID = CMD.S2C_Error, Token = "未登陆验证" });
                OutputLog("非法客户端: " + _client.Client.RemoteEndPoint.ToString());
                Disconnect();
                return;
            }
            if (p.TypeID == CMD.C2S_Data && p.DataLen > 0)
            {
                writer.Write(p.Data);
                filePosition += p.DataLen;
                if (filePosition == fileLength)
                {
                    _finished = true;
                }
            }

            if (p.TypeID == CMD.C2S_End && filePosition == fileLength)
            {
                OutputLog(p.ToString());
                _finished = true;
                if (File.Exists(filePath + ".cfg"))
                    File.Delete(filePath + ".cfg");
            }
        }

        #region 异步函数s
        public void OutputLog(string msg)
        {
            if (Output != null)
                Output(msg);
        }
        public void Notify(Exception ex)
        {
            if (Output != null)
                Output(ex.ToString());
        }

        private void BeginReceive()
        {
            if (!_connected) return;
            if (_client == null || !_client.Connected) return;

            try
            {
                byte[] _rawBytes = new byte[1024 * 8];
                _client.Client.BeginReceive(_rawBytes, 0, _rawBytes.Length, SocketFlags.None, ReceiveData, _rawBytes);
            }
            catch
            {
                Disconnect();
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            if (_client == null || !_client.Connected || !_connected) return;

            int dataRead;

            try
            {
                dataRead = _client.Client.EndReceive(result);
            }
            catch
            {
                Disconnect();
                return;
            }

            if (dataRead == 0)
            {
                Disconnect();
                return;
            }

            try
            {
                byte[] rawBytes = result.AsyncState as byte[];

                byte[] temp = _rawData;
                _rawData = new byte[dataRead + temp.Length];
                Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
                Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

                FilePacket p;
                while ((p = ReceivePacket(_rawData, out _rawData)) != null)
                    _recvList.Enqueue(p);
            }
            catch (Exception ex)
            {
                Notify(ex);
            }

            BeginReceive();
        }

        public FilePacket ReceivePacket(byte[] rawBytes, out byte[] extra)
        {
            extra = rawBytes;

            FilePacket p = new FilePacket();

            if (rawBytes.Length < 4) return null; //| 4Bytes: Packet Size

            int length = BitConverter.ToInt32(rawBytes, 0);

            if (length > rawBytes.Length || length < 4) return null;

            using (MemoryStream stream = new MemoryStream(rawBytes, 4, length - 4))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                try
                {
                    p.ReadPacket(reader);
                }
                catch
                {
                    return null;
                }
            }

            extra = new byte[rawBytes.Length - length];
            Buffer.BlockCopy(rawBytes, length, extra, 0, rawBytes.Length - length);

            return p;
        }

        public void Enqueue(FilePacket p, bool immediately = false)
        {
            if (immediately)
            {
                List<byte> data = new List<byte>();
                data.AddRange(p.GetPacketBytes());
                BeginSend(data);
            }
            if (_sendList != null && p != null)
            {
                lock (_sendList)
                {
                    _sendList.Enqueue(p);
                }
            }
        }

        public void BeginSend(List<byte> data)
        {
            if (!_connected) return;
            if (_client == null || !_client.Connected || data.Count == 0) return;
            try
            {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, asyncResult =>
                {
                    try
                    {
                        _client.Client.EndSend(asyncResult);
                    }
                    catch
                    { }
                }, null);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_client == null) return;
            OutputLog(_client.Client.RemoteEndPoint.ToString() + " 断开连接");
            _client.Client.Dispose();
            _client = null;
            _connected = false;
            _rawData = new byte[0];
        }
        #endregion
    }

    public class ClientConnection
    {
        private TcpClient _client = null;
        public bool Connected = false, Connecting = false;
        public Func<string, bool> Output { get; set; } = null;
        public Func<string, bool> UploadNotify { get; set; } = null;

        private ConcurrentQueue<FilePacket> _receiveList = new ConcurrentQueue<FilePacket>();
        private ConcurrentQueue<FilePacket> _sendList = new ConcurrentQueue<FilePacket>();

        byte[] _rawData = new byte[0];

        string Token;

        string ServerIP;

        int ServerPort;

        long filePosition = 0, fileLength = 0;

        string filePath = string.Empty;

        string fileName = string.Empty;

        FileStream fileStream = null;

        BinaryReader reader = null;
        
        public void Start(string _filePath, string token, string serverIp, int serverPort)
        {
            filePath = _filePath;
            Token = token;
            if (!File.Exists(filePath))
            {
                OutputLog("文件不存在");
                return;
            }
            FileInfo fi = new FileInfo(filePath);
            fileLength = fi.Length;
            fileName = fi.Name;
            fileStream = new FileStream(filePath, FileMode.Open);
            reader = new BinaryReader(fileStream);
            ServerIP = serverIp;
            ServerPort = serverPort;
            Connect();
        }
        private void Connect()
        {
            if (Connecting) return;
            _client = new TcpClient { NoDelay = true };
            Connecting = true;
            _client.BeginConnect(ServerIP, ServerPort, Connection, null);
        }
        
        private void BeginProcess()
        {
            new Thread(() =>
            {
                Enqueue(new FilePacket { TypeID = CMD.C2S_Init, Token = Token, FileLen = fileLength, FileName = fileName });
                while (Connected)
                {
                    while (_receiveList != null && !_receiveList.IsEmpty)
                    {
                        FilePacket p;
                        if (!_receiveList.TryDequeue(out p) || p == null) continue;
                        ProcessPacket(p);
                    }
                    if (_sendList == null || _sendList.IsEmpty) continue;
                    List<byte> data = new List<byte>();
                    while (!_sendList.IsEmpty)
                    {
                        FilePacket p;
                        if (!_sendList.TryDequeue(out p) || p == null) continue;
                        data.AddRange(p.GetPacketBytes());
                    }
                    BeginSend(data);
                    Thread.Sleep(1);
                }
            })
            { IsBackground = true }.Start();
        }

        public void ProcessPacket(FilePacket p)
        {
            //OutputLog(p.ToString());
            if (p.TypeID == CMD.S2C_Error || p.TypeID == CMD.S2C_End)
            {
                OutputLog(p.Token);
                Disconnect();
                return;
            }
            if (p.TypeID == CMD.S2C_Current)
            {
                Notify(string.Format("{0}", p.Position / 1024));
                if (p.Position == fileLength)
                {
                    OutputLog("上传完毕...");
                    Enqueue(new FilePacket { TypeID = CMD.C2S_End });
                    Disconnect();
                }
                return;
            }
            if (p.TypeID == CMD.S2C_Start)
            {
                if (p.Position != 0 && p.Position < fileLength)
                {
                    filePosition = p.Position;
                    fileStream.Seek(p.Position, SeekOrigin.Begin);
                }
                if (p.Position != 0 && p.Position == fileLength)
                {
                    Enqueue(new FilePacket { TypeID = CMD.C2S_End });
                    Disconnect();
                    return;
                }
                new Thread(() =>
                {
                    while (Connected)
                    {
                        FilePacket datapkg = new FilePacket { TypeID = CMD.C2S_Data };
                        if (fileLength - filePosition < 4096)
                        {
                            datapkg.Data = reader.ReadBytes((int)(fileLength - filePosition));
                            datapkg.DataLen = (int)(fileLength - filePosition);
                        }
                        else
                        {
                            datapkg.Data = reader.ReadBytes(4096);
                            datapkg.DataLen = 4096;
                        }
                        filePosition = filePosition + datapkg.DataLen;
                        Enqueue(datapkg);
                        Thread.Sleep(1);
                    }

                })
                { IsBackground = true }.Start();
            }
        }

        #region 异步函数s
        public void OutputLog(string msg)
        {
            if (Output != null)
                Output(msg);
        }
        public void OutputLog(Exception ex)
        {
            if (Output != null)
                Output(ex.ToString());
        }
        public void Notify(string msg)
        {
            if (UploadNotify != null)
                UploadNotify(msg);
        }
        
        private void Connection(IAsyncResult result)
        {
            try
            {
                Connecting = false;
                _client.EndConnect(result);
                if (_client.Connected)
                {
                    OutputLog("链接服务器成功...");
                    Connected = true;
                    BeginReceive();
                    BeginProcess();
                    return;
                }
                OutputLog("无法连接客户端");
            }
            catch (Exception ex)
            {
                OutputLog(ex);
            }
            Action act = () =>
            {
                OutputLog("10秒后重新连接...");
                Thread.Sleep(10000);
                Connect();
            };
            act.BeginInvoke(asyncCallBack =>
            {
                Console.WriteLine("BeginInvoke");
            }, null);
        }

        private void BeginReceive()
        {
            if (!Connected || _client == null || !_client.Connected) return;
            try
            {
                byte[] rawBytes = new byte[8 * 1024];
                _client.Client.BeginReceive(rawBytes, 0, rawBytes.Length, SocketFlags.None, ReceiveData, rawBytes);
            }
            catch (Exception ex)
            {
                OutputLog(ex);
                Disconnect();
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            if (!Connected || _client == null || !_client.Connected) return;

            int dataRead;

            try
            {
                dataRead = _client.Client.EndReceive(result);
            }
            catch
            {
                Disconnect();
                return;
            }

            if (dataRead == 0)
            {
                Disconnect();
            }

            byte[] rawBytes = result.AsyncState as byte[];

            byte[] temp = _rawData;
            _rawData = new byte[dataRead + temp.Length];
            Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
            Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

            FilePacket p;
            while ((p = ReceivePacket(_rawData, out _rawData)) != null)
            {
                _receiveList.Enqueue(p);
            }
            BeginReceive();
        }

        public FilePacket ReceivePacket(byte[] rawBytes, out byte[] extra)
        {
            extra = rawBytes;

            FilePacket p = new FilePacket();

            if (rawBytes.Length < 4) return null; //| 4Bytes: Packet Size

            int length = BitConverter.ToInt32(rawBytes, 0);

            if (length > rawBytes.Length || length < 4) return null;

            using (MemoryStream stream = new MemoryStream(rawBytes, 4, length - 4))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                try
                {
                    p.ReadPacket(reader);
                }
                catch
                {
                    return null;
                }
            }

            extra = new byte[rawBytes.Length - length];
            Buffer.BlockCopy(rawBytes, length, extra, 0, rawBytes.Length - length);

            return p;
        }

        private void BeginSend(List<byte> data)
        {
            if (!Connected || _client == null || !_client.Connected || data.Count == 0) return;

            try
            {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, asyncResult =>
                {
                    try
                    {
                        _client.Client.EndSend(asyncResult);
                    }
                    catch
                    { }
                }, null);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_client == null) return;

            _client.Close();

            Connecting = false;
            Connected = false;

            _receiveList = new ConcurrentQueue<FilePacket>();
            _sendList = new ConcurrentQueue<FilePacket>();
            _rawData = new byte[0];

            if (reader!=null && fileStream != null)
            {
                reader.Close();
                reader.Dispose();
                fileStream.Close();
                fileStream.Dispose();
                reader = null;
                fileStream = null;
            }
            
        }
        
        public void Enqueue(FilePacket p)
        {
            if (_sendList != null && p != null)
            {
                lock (_sendList)
                {
                    _sendList.Enqueue(p);
                }
            }
        }
        #endregion
    }

}
