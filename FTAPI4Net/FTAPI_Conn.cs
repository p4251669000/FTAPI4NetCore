using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Google.ProtocolBuffers;
using Org.BouncyCastle.Crypto;

namespace Futu.OpenApi
{
    class SentProtoData
    {
        internal ProtoHeader header;
        internal long sentTime;
        internal ManualResetEvent evt;

        internal SentProtoData(bool isSync)
        {
            if (isSync)
            {
                evt = new ManualResetEvent(false);
            }
        }
    }

    enum ConnCloseMode
    {
        None,
        Active,
        Passive
    }

    public class FTAPI_Conn : IDisposable
    {
        /// <summary>
        /// <see cref="FTSPI_Conn.OnInitConnect"/>
        /// </summary>
        public const int InitFail = 100;
        public const int ReplyTimeout = 12 * 1000;
        public const int ConnectTimeout = 10 * 1000;

        protected long localConnID;
        private FTSPI_Conn connSpi;
        private object connSpiLock = new object();
        string clientID = "";
        int clientVer = 0;
        AsymmetricCipherKeyPair rsaKeyPair = null;
        AesCbcCipher cipher;
        int nextPacketSN = 1;
        ConnStatus connStatus = ConnStatus.Start;
        ConnCloseMode closeMode = ConnCloseMode.None;
        string ip = "";
        int port;
        bool isEncrypt = false;
        Dictionary<uint, SentProtoData> sentProtoMap = new Dictionary<uint, SentProtoData>();
        SimpleBuffer readBuf = new SimpleBuffer(64 * 1024);
        SimpleBuffer writeBuf = new SimpleBuffer(64 * 1024);
        SimpleBuffer writeBufBak = new SimpleBuffer(64 * 1024);
        ulong connID;
        ulong userID;
        string aesKey = "";
        string aesCBIV = "";
        int keepAliveInterval = 9 * 1000; //心跳间隔，单位ms
        long lastKeepAliveTime = 0; //上次心跳时间，单位ms
        TcpClient tcp = new TcpClient();
        Timer tickTimer;

        public FTAPI_Conn()
        {
            
        }

        public void SetConnCallback(FTSPI_Conn connCallback)
        {
            lock(connSpiLock)
            {
                connSpi = connCallback;
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (tickTimer != null)
                {
                    tickTimer.Dispose();
                    tickTimer = null;
                }

                if (connStatus != ConnStatus.Start && connStatus != ConnStatus.Closed)
                {
                    SetCloseMode(ConnCloseMode.Active);
                    if (tcp != null)
                    {
                        tcp.Close();    
                    }
                }
                else
                {
                    connStatus = ConnStatus.Closed;
                }

                tcp = null;
            }
        }

        public void SetClientInfo(string clientID, int clientVer)
        {
            lock (this)
            {
                this.clientID = clientID;
                this.clientVer = clientVer;
            }
        }

        public void SetRSAPrivateKey(string key)
        {
            lock (this)
            {
                this.rsaKeyPair = RSAUtil.LoadKeyPair(key);
            }
        }

        public ulong GetConnectID()
        {
            lock (this)
            {
                return this.connID;
            }
        }

        public bool InitConnect(string ip, ushort port, bool isEnableEncrypt)
        {
            lock (this)
            {
                if (connStatus == ConnStatus.Start)
                {
                    this.ip = ip;
                    this.port = port;
                    this.isEncrypt = isEnableEncrypt;
                    tcp.BeginConnect(ip, port, this.OnTcpConnect, null);
                    connStatus = ConnStatus.Connecting;
                }

                if (connStatus == ConnStatus.Closed)
                    return false;
            }

            return true;
        }

        long ConvTimeStamp(DateTime t)
        {
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)t.Subtract(start).TotalMilliseconds;
        }

        long GetTimeStamp()
        {
            return ConvTimeStamp(DateTime.UtcNow);
        }

        void SetCloseMode(ConnCloseMode mode)
        {
            lock(this)
            {
                if (closeMode == ConnCloseMode.None)
                {
                    closeMode = mode;
                }
            }
        }

        protected uint SendProto<TMessage, TBuilder>(uint protoID, GeneratedMessage<TMessage, TBuilder> req)
            where TMessage : Google.ProtocolBuffers.GeneratedMessage<TMessage, TBuilder>
            where TBuilder : Google.ProtocolBuffers.GeneratedBuilder<TMessage, TBuilder>, new()
        {
            lock (this)
            {
                if (connStatus != ConnStatus.Connected && connStatus != ConnStatus.Ready)
                {
                    return 0;
                }
   
                byte[] body = req.ToByteArray();
                byte[] bodySha1 = SHA1Util.Calc(body);
                if (protoID == (uint)ProtoID.InitConnect)
                {
                    if (rsaKeyPair != null)
                    {
                        body = RSAUtil.encrypt(body, rsaKeyPair.Public);
                    }
                }
                else if (isEncrypt && cipher != null)
                {
                    body = cipher.encrypt(body);
                }

                ProtoHeader header = new ProtoHeader();
                header.nProtoID = protoID;
                header.nProtoFmtType = (byte)0;
                header.nProtoVer = 0;
                header.nSerialNo = (uint)Interlocked.Increment(ref nextPacketSN);
                header.nBodyLen = (uint)body.Length;
                header.arrBodySHA1 = bodySha1;

                SentProtoData sentData = new SentProtoData(false);
                sentData.header = header;
                sentData.sentTime = GetTimeStamp();
                sentProtoMap[header.nSerialNo] = sentData;

                byte[] buffer = new byte[ProtoHeader.HeaderSize + body.Length];
                header.Write(buffer);
                Buffer.BlockCopy(body, 0, buffer, ProtoHeader.HeaderSize, body.Length);
                if (writeBuf.Length == 0)
                {
                    writeBuf.EnsureAppend(buffer, 0, buffer.Length);
                    tcp.GetStream().BeginWrite(writeBuf.Buf, writeBuf.Start, writeBuf.Length, this.OnTcpWrite, null);
                }
                else
                {
                    writeBufBak.EnsureAppend(buffer, 0, buffer.Length);
                }
                return header.nSerialNo;
            }
        }

        void OnTcpConnect(IAsyncResult ar)
        {
            string errMsg = "";

            lock(this)
            {
                if (tcp == null)
                {
                    return;
                }

                try
                {
                    tcp.EndConnect(ar);
                    connStatus = ConnStatus.Connected;
                    tcp.GetStream().BeginRead(readBuf.Buf, 0, readBuf.Limit, this.OnTcpRead, null);
                    SendInitConnect();
                    return;
                }
                catch (Exception e)
                {
                    errMsg = e.ToString();
                    SetCloseMode(ConnCloseMode.Passive);
                    Close();
                }
            }

            lock(connSpiLock)
            {
                if (connSpi != null)
                {
                    long errCode = MakeInitConnectErrCode((int)ConnectFailType.ConnectFailed, 0);
                    connSpi.OnInitConnect(this, errCode, errMsg);
                }
            }
        }

        void OnTcpRead(IAsyncResult ar)
        {
            String errMsg = "";
            long errCode = 0;
            int count = 0;

            lock(this)
            {
                if (tcp == null)
                {
                    return;
                }

                try
                {
                    count = tcp.GetStream().EndRead(ar);
                }
                catch (Exception e)
                {
                    errCode = MakeInitConnectErrCode((int)ConnectFailType.RecvFailed, 0);
                    errMsg = e.ToString();
                    SetCloseMode(ConnCloseMode.Passive);
                }
            }

            if (count > 0)
            {
                readBuf.Length += count;
                HandleReadBuf();
                int bufAvailable = readBuf.Limit - readBuf.Start - readBuf.Length;
                if (bufAvailable == 0)
                {
                    readBuf.Compact();
                    bufAvailable = readBuf.Limit - readBuf.Start - readBuf.Length;
                }

                lock (this)
                {
                    if (tcp != null)
                    {
                        tcp.GetStream().BeginRead(readBuf.Buf, readBuf.Start + readBuf.Length, bufAvailable, this.OnTcpRead, null);   
                    }
                }
            }
            else
            {
                if (errCode == 0)
                {
                    errCode = MakeInitConnectErrCode((int)ConnectFailType.RecvFailed, 0);
                    SetCloseMode(ConnCloseMode.Passive);    
                }
                HandleDisconnect(errCode, errMsg);
            }
        }

        void OnTcpWrite(IAsyncResult ar)
        {
            String errMsg = "";
            long errCode = 0;

            lock(this)
            {
                if (tcp == null)
                {
                    return;
                }

                try
                {
                    tcp.GetStream().EndWrite(ar);
                    writeBuf.Start = 0;
                    writeBuf.Length = 0;
                    if (writeBufBak.Length > 0)
                    {
                        var tmp = writeBuf;
                        writeBuf = writeBufBak;
                        writeBufBak = tmp;
                        tcp.GetStream().BeginWrite(writeBuf.Buf, writeBuf.Start, writeBuf.Length, this.OnTcpWrite, null);
                    }
                    return;
                }
                catch (Exception e)
                {
                    errCode = MakeInitConnectErrCode((int)ConnectFailType.SendFailed, 0);
                    errMsg = e.ToString();
                    SetCloseMode(ConnCloseMode.Passive);
                }
            }

            HandleDisconnect(errCode, errMsg);
        }


        protected virtual void OnTimeTick(object stateInfo)
        {
            long now = GetTimeStamp();

            List<KeyValuePair<uint, SentProtoData>> pendingRemove = new List<KeyValuePair<uint, SentProtoData>>();
            lock(this)
            {
                if (now - lastKeepAliveTime >= keepAliveInterval && connStatus == ConnStatus.Ready)
                {
                    SendKeepAlive();
                    lastKeepAliveTime = now;
                }

                foreach (var item in sentProtoMap)
                {
                    SentProtoData protoData = item.Value;
                    if (now - protoData.sentTime > ReplyTimeout)
                    {
                        pendingRemove.Add(item);
                    }
                }

                foreach (var item in pendingRemove)
                {
                    sentProtoMap.Remove(item.Key);
                }
            }

            foreach (var item in pendingRemove)
            {
                HandleReplyPacket(ReqReplyType.Timeout, item.Value.header, null, false);    
            }
        }

        protected virtual void OnReply(ReqReplyType replyType, ProtoHeader protoHeader, byte[] data)
        {

        }

        protected virtual void OnPush(ProtoHeader protoHeader, byte[] data)
        {

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        ~FTAPI_Conn()
        {
            Dispose(false);
        }

        private void HandleDisconnect(long errCode, string msg)
        {
            bool isClosed = false;

            lock(this)
            {
                if (closeMode == ConnCloseMode.Active) //主动关闭
                {
                    errCode = 0;
                    msg = "";
                }

                isClosed = this.connStatus == ConnStatus.Closed;
                Close();
                connStatus = ConnStatus.Closed;
            }

            lock(connSpiLock)
            {
                if (connSpi != null)
                {
                    if (HasPendingInitConnect(true))
                    {
                        errCode = MakeInitConnectErrCode((int)InitFailType.DisConnect, 0);
                        connSpi.OnInitConnect(this, errCode, msg);
                    }
                    
                    if (!isClosed)
                    {
                        connSpi.OnDisconnect(this, errCode);
                    }
                }
            }
        }

        private bool HasPendingInitConnect(bool remove)
        {
            lock (this)
            {
                foreach (KeyValuePair<uint, SentProtoData> item in sentProtoMap)
                {
                    if (item.Value.header.nProtoID == (uint)ProtoID.InitConnect)
                    {
                        if (remove)
                        {
                            sentProtoMap.Remove(item.Key);
                        }
                        return true;
                    }
                }
            }
            
            return false;
        }

        private uint SendInitConnect()
        {
            Pb.InitConnect.C2S.Builder c2s = Pb.InitConnect.C2S.CreateBuilder()
                .SetClientVer(clientVer)
                .SetClientID(clientID)
                .SetRecvNotify(true)
                .SetPushProtoFmt((int)Pb.Common.ProtoFmt.ProtoFmt_Protobuf)
                .SetProgrammingLanguage("CSharp");
            if (isEncrypt)
            {
                c2s.SetPacketEncAlgo((int)Pb.Common.PacketEncAlgo.PacketEncAlgo_AES_CBC);
            }
            else
            {
                c2s.SetPacketEncAlgo((int)Pb.Common.PacketEncAlgo.PacketEncAlgo_None);
            }

            Pb.InitConnect.Request req = Pb.InitConnect.Request.CreateBuilder().SetC2S(c2s).Build();
            return SendProto((uint)ProtoID.InitConnect, req);
        }

        private uint SendKeepAlive()
        {
            Pb.KeepAlive.C2S c2s = Pb.KeepAlive.C2S.CreateBuilder().SetTime(GetTimeStamp()).Build();
            Pb.KeepAlive.Request req = Pb.KeepAlive.Request.CreateBuilder().SetC2S(c2s).Build();
            return SendProto((uint)ProtoID.KeepAlive, req);
        }

        private long MakeInitConnectErrCode(int high, int low)
        {
            long errCode;
            errCode = (long)high << 32;
            errCode |= (uint)low;
            return errCode;
        }

        private void HandleReadBuf()
        {
            while (ProtoHeader.HeaderSize <= readBuf.Length)
            {
                ProtoHeader header = ProtoHeader.Parse(readBuf.Buf, readBuf.Start);
                if (header == null)
                {
                    break;
                }
                if (ProtoHeader.HeaderSize + header.nBodyLen > readBuf.Limit)
                {
                    readBuf.Resize(ProtoHeader.HeaderSize + (int)header.nBodyLen);
                }
                if (ProtoHeader.HeaderSize + header.nBodyLen > readBuf.Length)
                {
                    break;
                }
                readBuf.Consume(ProtoHeader.HeaderSize);

                byte[] body = null;
                ReqReplyType replyType = ReqReplyType.SvrReply;
                if (header.nBodyLen > 0)
                {
                    body = new byte[header.nBodyLen];
                    Buffer.BlockCopy(readBuf.Buf, readBuf.Start, body, 0, (int)header.nBodyLen);

                    try
                    {
                        if (isEncrypt)
                        {
                            if (header.nProtoID == (uint)ProtoID.InitConnect)
                            {
                                body = RSAUtil.decrypt(body, rsaKeyPair.Private);
                            }
                            else
                            {
                                body = cipher.decrypt(body);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        body = null;
                        replyType = ReqReplyType.Invalid;
                    }
                    finally
                    {
                        readBuf.Consume((int)header.nBodyLen);
                    }
                }

                if (ProtoUtil.IsPushProto((ProtoID)header.nProtoID))
                {
                    if (replyType == ReqReplyType.SvrReply)
                    {
                        HandlePushPacket(header, body);
                    }
                }
                else
                {
                    HandleReplyPacket(replyType, header, body, true);
                }
            }
        }

        void HandlePushPacket(ProtoHeader header, byte[] body)
        {
            OnPush(header, body);
        }

        void HandleReplyPacket(ReqReplyType replyType, ProtoHeader header, byte[] body, bool isRemoveFromSent)
        {
            SentProtoData sentData;

            lock (this)
            {
                if (!sentProtoMap.TryGetValue(header.nSerialNo, out sentData))
                    return;
                if (sentData.header.nProtoID != header.nProtoID)
                {
                    return;
                }
                if (isRemoveFromSent)
                {
                    sentProtoMap.Remove(header.nSerialNo);
                }
            }
            
            if (header.nProtoID == (uint)ProtoID.InitConnect)
            {
                HandleInitConnect(replyType, header, body);
            }
            else
            {
                OnReply(replyType, header, body);
            }
        }

        void HandleInitConnect(ReqReplyType replyType, ProtoHeader header, byte[] body)
        {
            Pb.InitConnect.Response rsp;
            long errCode = 0;
            string desc = "";

            if (replyType == ReqReplyType.SvrReply)
            {
                try
                {
                    rsp = Pb.InitConnect.Response.ParseFrom(body);
                    if (rsp.RetType == (int)Pb.Common.RetType.RetType_Succeed)
                    {
                        lock (this)
                        {
                            connID = rsp.S2C.ConnID;
                            userID = rsp.S2C.LoginUserID;
                            keepAliveInterval = rsp.S2C.KeepAliveInterval * 1000 * 4 / 5;
                            aesKey = rsp.S2C.ConnAESKey;
                            aesCBIV = rsp.S2C.AesCBCiv;
                            cipher = new AesCbcCipher(Encoding.UTF8.GetBytes(aesKey), Encoding.UTF8.GetBytes(aesCBIV));
                        }
                    }
                    else
                    {
                        errCode = MakeInitConnectErrCode(InitFail, (int)InitFailType.OpenDReject);
                        if (!String.IsNullOrEmpty(rsp.RetMsg))
                        {
                            desc = rsp.RetMsg;
                        }
                        else
                        {
                            desc = String.Format("retType={0}", rsp.RetType);
                        }
                    }
                }
                catch (Exception e)
                {
                    errCode = MakeInitConnectErrCode(InitFail, (int)InitFailType.OpenDReject);
                    desc = String.Format("Parse packet fail, serialNO={0} err={1}", header.nSerialNo, e.ToString());
                }
            }
            else if (replyType == ReqReplyType.Timeout)
            {
                errCode = MakeInitConnectErrCode(InitFail, (int)InitFailType.Timeout);
            }
            else if (replyType == ReqReplyType.DisConnect)
            {
                errCode = MakeInitConnectErrCode(InitFail, (int)InitFailType.DisConnect);
            }
            else if (replyType == ReqReplyType.Invalid)
            {
                errCode = MakeInitConnectErrCode(InitFail, (int)InitFailType.Unknow);
                desc = String.Format("Invalid packet body, serialNO={0}", header.nSerialNo);
            }

            OnInitConnect(errCode, desc);
        }

        void OnInitConnect(long errCode, string desc)
        {
            lock(this)
            {
                if (errCode == 0)
                {
                    connStatus = ConnStatus.Ready;
                    tickTimer = new Timer(this.OnTimeTick, null, 1000, 1000);
                }
                else
                {
                    SetCloseMode(ConnCloseMode.Passive);
                    Close();
                }
            }

            lock(connSpiLock)
            {
                if (connSpi != null)
                {
                    connSpi.OnInitConnect(this, errCode, desc);
                }
            }
        }
    }
}
