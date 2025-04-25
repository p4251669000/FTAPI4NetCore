using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Google.ProtocolBuffers;

namespace Futu.OpenApi
{
    public enum ConnectFailType
    {
        Unknown = -1,
        None = 0,
        CreateFailed = 1,
        CloseFailed = 2,
        ShutdownFailed = 3,
        GetHostByNameFailed = 4,
        GetHostByNameWrong = 5,
        ConnectFailed = 6,
        BindFailed = 7,
        ListenFailed = 8,
        SelectReturnError = 9,
        SendFailed = 10,
        RecvFailed = 11,
    }

    public enum ReqReplyType
    {
        SvrReply = 0,       //来自服务器的应答 
        Timeout = -100,     //等待服务器应答超时
        DisConnect = -200,   //因连接已断开(被动断开或主动关闭)的应答
        Unknown = -400,
        Invalid = -500
    }

    public enum InitFailType
    {
        Unknow = 0,             //未知
        Timeout = 1,            //超时
        DisConnect = 3,         //连接断开
        SeriaNoNotMatch = 4,    //序列号不符
        SendInitReqFailed = 4,  //发送初始化协议失败
        OpenDReject = 5,        //FutuOpenD回包指定错误，具体错误看描述
    }

    public enum ConnStatus
    {
        Start,
        Connecting,
        Connected,
        Ready,
        Closed
    }


    /// <summary>
    /// 连接状态的回调
    /// </summary>
    public interface FTSPI_Conn
    {
        /// <summary>
        /// 初始化连接完成
        /// </summary>
        /// <param name="client"></param>
        /// <param name="errCode">当高32位在ConnectFailType取值范围内时，低32位为系统错误码；当高32位等于FTAPI_Conn.InitFail，则低32位为InitFailType类型。</param>
        /// <param name="desc">错误描述</param>
        void OnInitConnect(FTAPI_Conn client, long errCode, string desc);
        /// <summary>
        /// 连接断开
        /// </summary>
        /// <param name="client"></param>
        /// <param name="errCode">高32位为FTAPI_ConnectFailType类型，低32位为系统错误码；</param>
        void OnDisconnect(FTAPI_Conn client, long errCode);
    }

    public class FTAPI
    {
        private static bool isInited = false;
        private static object initLock = new object();

        /// <summary>
        /// 初始化底层库，程序启动时调用一次。
        /// </summary>
        public static void Init()
        {
            lock (initLock)
            {
                if (isInited) return;
                //FTCAPI.FTAPIChannel_Init();
                isInited = true;
            }
        }

        /// <summary>
        /// 清理底层库，程序退出时调用一次。
        /// </summary>
        public static void UnInit()
        {
            lock (initLock)
            {
                if (!isInited) return;
                isInited = false;
            }
        }
    }
}

