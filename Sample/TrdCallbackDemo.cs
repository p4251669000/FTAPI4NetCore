using Futu.OpenApi;
using Futu.OpenApi.Pb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.ProtocolBuffers;

namespace FTAPI4NetSample
{
    class SampleTrdCallback : FTSPI_Trd, FTSPI_Conn
    {
        private TrdCommon.TrdEnv trdEnv = TrdCommon.TrdEnv.TrdEnv_Simulate;
        private TrdCommon.TrdMarket trdMkt = TrdCommon.TrdMarket.TrdMarket_HK;
        private ulong accID;
        private string svrOrderId;

        public void OnReply_GetAccList(FTAPI_Conn client, uint nSerialNo, TrdGetAccList.Response rsp)
        {
            if (rsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("ERROR: GetAccList, retMsg = {0}", rsp.RetMsg);
                return;
            }

            Console.Write("Recv GetAccList succeed. accCount: {0}\n", rsp.S2C.AccListCount);
            foreach (TrdCommon.TrdAcc acc in rsp.S2C.AccListList)
            {
                if (acc.TrdEnv == (int)trdEnv && acc.TrdMarketAuthListList.Contains((int)trdMkt))
                {
                    this.accID = acc.AccID;
                    // 打印账户信息
                    Console.Write("accInfo: accId: {0}, trdEnv: {1}, trdMarketAuthList: {2}, simAccType: {3}\n",
                        acc.AccID, (TrdCommon.TrdEnv)acc.TrdEnv, (TrdCommon.TrdMarket)acc.TrdMarketAuthListList[0],
                        (TrdCommon.TrdAccType)acc.SimAccType);
                    break;
                }
            }
            GetMarginRatio(client);
            // GetHistoryOrderList(client);
        }

        public void OnReply_UnlockTrade(FTAPI_Conn client, uint nSerialNo, TrdUnlockTrade.Response rsp)
        {
            
        }

        public void OnReply_SubAccPush(FTAPI_Conn client, uint nSerialNo, TrdSubAccPush.Response rsp)
        {

        }

        public void OnReply_GetFunds(FTAPI_Conn client, uint nSerialNo, TrdGetFunds.Response rsp)
        {

        }

        public void OnReply_GetPositionList(FTAPI_Conn client, uint nSerialNo, TrdGetPositionList.Response rsp)
        {

        }

        public void OnReply_GetMaxTrdQtys(FTAPI_Conn client, uint nSerialNo, TrdGetMaxTrdQtys.Response rsp)
        {

        }

        public void OnReply_GetOrderList(FTAPI_Conn client, uint nSerialNo, TrdGetOrderList.Response rsp)
        {

        }

        public void OnReply_GetOrderFillList(FTAPI_Conn client, uint nSerialNo, TrdGetOrderFillList.Response rsp)
        {

        }

        public void OnReply_GetHistoryOrderList(FTAPI_Conn client, uint nSerialNo, TrdGetHistoryOrderList.Response rsp)
        {
            if (rsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("ERROR: GetHistoryOrderList, retMsg = {0}", rsp.RetMsg);
                return;
            }

            Console.WriteLine("OnReply_GetHistoryOrderList, retMsg = {0}", rsp);

            foreach (TrdCommon.Order ord in rsp.S2C.OrderListList)
            {
                if (ord.Qty != 0)
                {
                    svrOrderId = ord.OrderIDEx;
                    break;
                }
            }

            GetOrderFee(client);
        }

        public void OnReply_GetHistoryOrderFillList(FTAPI_Conn client, uint nSerialNo, TrdGetHistoryOrderFillList.Response rsp)
        {

        }

        public void OnReply_UpdateOrder(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrder.Response rsp)
        {
            Console.WriteLine("Recv UpdateOrder: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_UpdateOrderFill(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrderFill.Response rsp)
        {
            Console.WriteLine("Recv UpdateOrderFill: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_PlaceOrder(FTAPI_Conn client, uint nSerialNo, TrdPlaceOrder.Response rsp)
        {
        }

        public void OnReply_ModifyOrder(FTAPI_Conn client, uint nSerialNo, TrdModifyOrder.Response rsp)
        {
        }

        public void OnReply_GetMarginRatio(FTAPI_Conn client, uint nSerialNo, TrdGetMarginRatio.Response rsp)
        {
            if (rsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("ERROR: GetMarginRatio, retMsg = {0}", rsp.RetMsg);
                return;
            }
            Console.WriteLine("Recv OnReply_GetMarginRatio: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnReply_GetOrderFee(FTAPI_Conn client, uint nSerialNo, TrdGetOrderFee.Response rsp)
        {
            if (rsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("ERROR: GetOrderFee, retMsg = {0}", rsp.RetMsg);
                return;
            }
            Console.WriteLine("Recv OnReply_GetOrderFee: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnReply_GetFlowSummary(FTAPI_Conn client, uint nSerialNo, TrdFlowSummary.Response rsp)
        {
            if (rsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("ERROR: GetFlowSummary, retMsg = {0}", rsp.RetMsg);
                return;
            }
            Console.WriteLine("Recv OnReply_GetFlowSummary: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnInitConnect(FTAPI_Conn client, long errCode, string desc)
        {
            Console.WriteLine("OnInitConnect: errCode={0} desc={1}", errCode, desc);

            if (errCode == 0)
            {
                FTAPI_Trd trd = client as FTAPI_Trd;
                GetAccList(trd);
            }
        }

        public void OnDisconnect(FTAPI_Conn client, long errCode)
        {
            Console.WriteLine("OnDisconnect: errCode={0}", errCode);
        }


        /// <summary>
        /// 获取交易业务账户列表
        /// </summary>
        /// <param name="client"></param>
        void GetAccList(FTAPI_Conn client)
        {
            FTAPI_Trd trd = client as FTAPI_Trd;
            TrdGetAccList.C2S c2s = TrdGetAccList.C2S.CreateBuilder().SetUserID(0)
                .SetTrdCategory((int)TrdCommon.TrdCategory.TrdCategory_Security)
                .SetNeedGeneralSecAccount(true)
                .Build();
            TrdGetAccList.Request req = TrdGetAccList.Request.CreateBuilder().SetC2S(c2s).Build();
            trd.GetAccList(req);
        }

        /// <summary>
        /// 获取融资融券数据
        /// </summary>
        /// <param name="client"></param>
        void GetMarginRatio(FTAPI_Conn client)
        {
            FTAPI_Trd trd = client as FTAPI_Trd;
            QotCommon.Security sec = QotCommon.Security.CreateBuilder()
                .SetCode("00700")
                .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security)
                .Build();
            TrdCommon.TrdHeader header = TrdCommon.TrdHeader.CreateBuilder().SetTrdEnv((int)trdEnv)
                .SetTrdMarket((int)trdMkt)
                .SetAccID(accID)
                .Build();
            TrdGetMarginRatio.C2S c2s = TrdGetMarginRatio.C2S.CreateBuilder().SetHeader(header).AddSecurityList(sec).Build();
            TrdGetMarginRatio.Request req = TrdGetMarginRatio.Request.CreateBuilder().SetC2S(c2s).Build();
            trd.GetMarginRatio(req);
        }

        /// <summary>
        /// 获取成交的订单
        /// </summary>
        /// <param name="client"></param>
        void GetHistoryOrderList(FTAPI_Conn client)
        {
            FTAPI_Trd trd = client as FTAPI_Trd;
            TrdCommon.TrdHeader header = TrdCommon.TrdHeader.CreateBuilder().SetTrdEnv((int)trdEnv)
                .SetTrdMarket((int)trdMkt)
                .SetAccID(accID)
                .Build();
            TrdCommon.TrdFilterConditions conditions = TrdCommon.TrdFilterConditions.CreateBuilder()
                .AddCodeList("AAPL")
                .SetBeginTime("2024-04-01")
                .SetEndTime("2024-04-05")
                .Build();
            TrdGetHistoryOrderList.C2S c2s = TrdGetHistoryOrderList.C2S.CreateBuilder().SetHeader(header)
                .SetFilterConditions(conditions)
                .AddFilterStatusList((int)TrdCommon.OrderStatus.OrderStatus_Filled_All)
                .Build();
            TrdGetHistoryOrderList.Request req = TrdGetHistoryOrderList.Request.CreateBuilder().SetC2S(c2s).Build();
            trd.GetHistoryOrderList(req);
        }

        /// <summary>
        /// 查询指定账号服务器订单的订单费用
        /// </summary>
        /// <param name="client"></param>
        void GetOrderFee(FTAPI_Conn client)
        {
            if (svrOrderId == null)
                return;

            FTAPI_Trd trd = client as FTAPI_Trd;
            TrdCommon.TrdHeader header = TrdCommon.TrdHeader.CreateBuilder().SetTrdEnv((int)trdEnv)
                .SetTrdMarket((int)trdMkt)
                .SetAccID(accID)
                .Build();
            TrdGetOrderFee.C2S c2s = TrdGetOrderFee.C2S.CreateBuilder().SetHeader(header)
                .AddOrderIdExList(svrOrderId).Build();
            TrdGetOrderFee.Request req = TrdGetOrderFee.Request.CreateBuilder().SetC2S(c2s).Build();
            trd.GetOrderFee(req);
        }
    }

    class TrdCallbackDemo : DemoBase
    {
        public void Run()
        {
            Console.WriteLine("Run TrdCallback");
            //演示交易对象
            FTAPI_Trd trd = new FTAPI_Trd();
            var trdCallback = new SampleTrdCallback();
            trd.SetConnCallback(trdCallback);
            trd.SetTrdCallback(trdCallback);
            trd.SetClientInfo("FTAPI4NET_Sample", 1);
            // 建立连接，并触发OnInitConnect函数
            trd.InitConnect(Config.OpendIP, Config.OpendPort, false);
            Thread.Sleep(1000 * 5);
            Console.WriteLine("TrdCallback End");
        }
    }
}
