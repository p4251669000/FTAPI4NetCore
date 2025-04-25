using Futu.OpenApi;
using Futu.OpenApi.Pb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.ProtocolBuffers;
using System.Threading;

namespace FTAPI4NetSample
{
    class SampleQotCallback : FTSPI_Qot, FTSPI_Conn
    {
        public void OnInitConnect(FTAPI_Conn client, long errCode, string desc)
        {
            Console.WriteLine("InitConnected: {0} {1}", errCode, desc);
            if (errCode == 0) //初始化连接成功后，才能发起其它调用
            {
                FTAPI_Qot qot = client as FTAPI_Qot;

                Sub(qot);
                //getOptionExpirationDate(qot);
            }
        }

        void getOptionExpirationDate(FTAPI_Qot qot)
        {
            QotCommon.Security sec = QotCommon.Security.CreateBuilder()
                .SetCode("00388")
                .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security)
                .Build();
            QotGetOptionExpirationDate.C2S c2s = QotGetOptionExpirationDate.C2S.CreateBuilder().SetOwner(sec).Build();
            QotGetOptionExpirationDate.Request req = QotGetOptionExpirationDate.Request.CreateBuilder().SetC2S(c2s).Build();
            uint serialNo = qot.GetOptionExpirationDate(req);
            Console.WriteLine("getOptionExpirationDate: sn={0}", serialNo);
        }

        public void OnDisconnect(FTAPI_Conn client, long errCode)
        {
            Console.WriteLine("DisConnected");
        }

        void SendGetGlobalState(FTAPI_Qot qot)
        {
            GetGlobalState.Request req = GetGlobalState.Request.CreateBuilder().SetC2S(GetGlobalState.C2S.CreateBuilder().SetUserID(900019)).Build();
            uint serialNo = qot.GetGlobalState(req);
            Console.WriteLine("SendGetGlobalState: {0}", serialNo);
        }

        /// <summary>
        /// 订阅腾讯的实时逐笔
        /// （可以在OnReply_UpdateTicker函数中拿到推送的信息）
        /// </summary>
        /// <param name="qot"></param>
        void Sub(FTAPI_Qot qot)
        {
            QotSub.Request.Builder reqBuilder = QotSub.Request.CreateBuilder();
            QotSub.C2S.Builder csReqBuilder = QotSub.C2S.CreateBuilder();
            QotCommon.Security.Builder stock = QotCommon.Security.CreateBuilder();
                stock.SetCode("00700");
                stock.SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security);
            QotCommon.Security.Builder stock1 = QotCommon.Security.CreateBuilder()
                .SetCode("999010")
                .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security);
            QotCommon.Security.Builder stock2 = QotCommon.Security.CreateBuilder()
                .SetCode("AAPL")
                .SetMarket((int)QotCommon.QotMarket.QotMarket_US_Security);
            QotCommon.Security.Builder stock3 = QotCommon.Security.CreateBuilder()
                .SetCode("GOOG")
                .SetMarket((int)QotCommon.QotMarket.QotMarket_US_Security);
            csReqBuilder.AddSecurityList(stock).AddSecurityList(stock1).AddSecurityList(stock2).AddSecurityList(stock3);
            csReqBuilder.AddSubTypeList((int)QotCommon.SubType.SubType_Ticker)
                .AddSubTypeList((int)QotCommon.SubType.SubType_KL_1Min)
                .AddSubTypeList((int)QotCommon.SubType.SubType_Basic);
            csReqBuilder.SetIsSubOrUnSub(true);
            csReqBuilder.SetIsRegOrUnRegPush(true);
            reqBuilder.SetC2S(csReqBuilder);
            uint serialNo = qot.Sub(reqBuilder.Build());
            Console.WriteLine("SendSub: {0}", serialNo);
        }

        void StockFilter(FTAPI_Qot qot)
        {
            QotStockFilter.BaseFilter baseFilter = QotStockFilter.BaseFilter.CreateBuilder()
                .SetFieldName((int)QotStockFilter.StockField.StockField_MarketVal)
                .SetFilterMin(10000)
                .SetFilterMax(10000000000)
                .SetIsNoFilter(false)
                .SetSortDir((int)QotStockFilter.SortDir.SortDir_Descend)
                .Build();
            QotStockFilter.C2S c2s = QotStockFilter.C2S.CreateBuilder()
                .SetBegin(0)
                .SetNum(100)
                .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security)
                .AddBaseFilterList(baseFilter)
                .Build();
            uint serialNo = qot.StockFilter(QotStockFilter.Request.CreateBuilder().SetC2S(c2s).Build());
            Console.WriteLine("SendQotStockFilter: {0}", serialNo);
        }

        void SetPriceReminder(FTAPI_Qot qot)
        {
            QotCommon.Security sec = QotCommon.Security.CreateBuilder().SetCode("00700")
            .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security)
            .Build();
            QotSetPriceReminder.C2S c2s = QotSetPriceReminder.C2S.CreateBuilder().SetSecurity(sec)
                    .SetOp((int)QotSetPriceReminder.SetPriceReminderOp.SetPriceReminderOp_Add)
                    .SetType((int)QotCommon.PriceReminderType.PriceReminderType_PriceUp)
                    .SetFreq((int)QotCommon.PriceReminderFreq.PriceReminderFreq_Always)
                    .SetValue(380)
                    .Build();
            QotSetPriceReminder.Request req = QotSetPriceReminder.Request.CreateBuilder().SetC2S(c2s).Build();
            qot.SetPriceReminder(req);
        }

        void GetMarketState(FTAPI_Qot qot)
        {
            QotCommon.Security sec = QotCommon.Security.CreateBuilder().SetCode("00700")
             .SetMarket((int)QotCommon.QotMarket.QotMarket_HK_Security)
             .Build();
            QotGetMarketState.C2S c2s = QotGetMarketState.C2S.CreateBuilder().AddSecurityList(sec)
                .Build();
            QotGetMarketState.Request req = QotGetMarketState.Request.CreateBuilder().SetC2S(c2s).Build();
            qot.GetMarketState(req);
        }

        public void OnReply_GetGlobalState(FTAPI_Conn client, uint nSerialNo, GetGlobalState.Response rsp)
        {
            Console.WriteLine("OnReply_GetGlobalState: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_Sub(FTAPI_Conn client, uint nSerialNo, QotSub.Response rsp)
        {

        }

        public void OnReply_RegQotPush(FTAPI_Conn client, uint nSerialNo, QotRegQotPush.Response rsp)
        {

        }

        public void OnReply_GetSubInfo(FTAPI_Conn client, uint nSerialNo, QotGetSubInfo.Response rsp)
        {

        }

        public void OnReply_GetTicker(FTAPI_Conn client, uint nSerialNo, QotGetTicker.Response rsp)
        {

        }

        public void OnReply_GetBasicQot(FTAPI_Conn client, uint nSerialNo, QotGetBasicQot.Response rsp)
        {

        }

        public void OnReply_GetOrderBook(FTAPI_Conn client, uint nSerialNo, QotGetOrderBook.Response rsp)
        {

        }

        public void OnReply_GetKL(FTAPI_Conn client, uint nSerialNo, QotGetKL.Response rsp)
        {

        }

        public void OnReply_GetRT(FTAPI_Conn client, uint nSerialNo, QotGetRT.Response rsp)
        {

        }

        public void OnReply_GetBroker(FTAPI_Conn client, uint nSerialNo, QotGetBroker.Response rsp)
        {

        }

        public void OnReply_RequestRehab(FTAPI_Conn client, uint nSerialNo, QotRequestRehab.Response rsp)
        {

        }

        public void OnReply_RequestHistoryKL(FTAPI_Conn client, uint nSerialNo, QotRequestHistoryKL.Response rsp)
        {

        }

        public void OnReply_RequestHistoryKLQuota(FTAPI_Conn client, uint nSerialNo, QotRequestHistoryKLQuota.Response rsp)
        {

        }


        public void OnReply_GetStaticInfo(FTAPI_Conn client, uint nSerialNo, QotGetStaticInfo.Response rsp)
        {

        }

        public void OnReply_GetSecuritySnapshot(FTAPI_Conn client, uint nSerialNo, QotGetSecuritySnapshot.Response rsp)
        {

        }

        public void OnReply_GetPlateSet(FTAPI_Conn client, uint nSerialNo, QotGetPlateSet.Response rsp)
        {

        }

        public void OnReply_GetPlateSecurity(FTAPI_Conn client, uint nSerialNo, QotGetPlateSecurity.Response rsp)
        {

        }

        public void OnReply_GetReference(FTAPI_Conn client, uint nSerialNo, QotGetReference.Response rsp)
        {

        }

        public void OnReply_GetOwnerPlate(FTAPI_Conn client, uint nSerialNo, QotGetOwnerPlate.Response rsp)
        {

        }

        public void OnReply_GetHoldingChangeList(FTAPI_Conn client, uint nSerialNo, QotGetHoldingChangeList.Response rsp)
        {

        }

        public void OnReply_GetOptionChain(FTAPI_Conn client, uint nSerialNo, QotGetOptionChain.Response rsp)
        {

        }

        public void OnReply_GetWarrant(FTAPI_Conn client, uint nSerialNo, QotGetWarrant.Response rsp)
        {

        }

        public void OnReply_GetCapitalFlow(FTAPI_Conn client, uint nSerialNo, QotGetCapitalFlow.Response rsp)
        {

        }

        public void OnReply_GetCapitalDistribution(FTAPI_Conn client, uint nSerialNo, QotGetCapitalDistribution.Response rsp)
        {

        }
        
        public void OnReply_GetUserSecurity(FTAPI_Conn client, uint nSerialNo, QotGetUserSecurity.Response rsp)
        {
            Console.WriteLine(rsp.S2C.ToJson());
        }

        public void OnReply_SetPriceReminder(FTAPI_Conn client, uint nSerialNo, QotSetPriceReminder.Response rsp)
        {
            Console.WriteLine("OnReply_SetPriceReminder: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_GetPriceReminder(FTAPI_Conn client, uint nSerialNo, QotGetPriceReminder.Response rsp)
        {
            Console.WriteLine("OnReply_GetPriceReminder: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_ModifyUserSecurity(FTAPI_Conn client, uint nSerialNo, QotModifyUserSecurity.Response rsp)
        {

        }

        public void OnReply_Notify(FTAPI_Conn client, uint nSerialNo, Notify.Response rsp)
        {

        }


        public void OnReply_UpdateBasicQot(FTAPI_Conn client, uint nSerialNo, QotUpdateBasicQot.Response rsp)
        {
            Console.WriteLine("OnReply_UpdateBasicQot: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnReply_UpdateKL(FTAPI_Conn client, uint nSerialNo, QotUpdateKL.Response rsp)
        {
            Console.WriteLine("OnReply_UpdateKL: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnReply_UpdateRT(FTAPI_Conn client, uint nSerialNo, QotUpdateRT.Response rsp)
        {

        }

        public void OnReply_UpdateTicker(FTAPI_Conn client, uint nSerialNo, QotUpdateTicker.Response rsp)
        {
            Console.WriteLine("OnReply_UpdateTicker: {0} {1}", nSerialNo, rsp.S2C.ToJson());
        }

        public void OnReply_UpdateOrderBook(FTAPI_Conn client, uint nSerialNo, QotUpdateOrderBook.Response rsp)
        {

        }

        public void OnReply_UpdateBroker(FTAPI_Conn client, uint nSerialNo, QotUpdateBroker.Response rsp)
        {

        }


        public void OnReply_UpdatePriceReminder(FTAPI_Conn client, uint nSerialNo, QotUpdatePriceReminder.Response rsp)
        {
            Console.WriteLine("OnReply_UpdatePriceReminder: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_StockFilter(FTAPI_Conn client, uint nSerialNo, QotStockFilter.Response rsp)
        {
            Console.WriteLine("OnReply_StockFilter: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_GetCodeChange(FTAPI_Conn client, uint nSerialNo, QotGetCodeChange.Response rsp)
        {

        }


        public void OnReply_GetIpoList(FTAPI_Conn client, uint nSerialNo, QotGetIpoList.Response rsp)
        {
            throw new NotImplementedException();
        }

        public void OnReply_GetFutureInfo(FTAPI_Conn client, uint nSerialNo, QotGetFutureInfo.Response rsp)
        {
            throw new NotImplementedException();
        }

        public void OnReply_RequestTradeDate(FTAPI_Conn client, uint nSerialNo, QotRequestTradeDate.Response rsp)
        {
            throw new NotImplementedException();
        }


        public void OnReply_GetUserSecurityGroup(FTAPI_Conn client, uint nSerialNo, QotGetUserSecurityGroup.Response rsp)
        {
            Console.WriteLine("OnReply_GetUserSecurityGroup: {0} {1}", nSerialNo, rsp);
        }

        public void OnReply_GetMarketState(FTAPI_Conn client, uint nSerialNo, QotGetMarketState.Response rsp)
        {
            Console.WriteLine("OnReply_GetMarketState: {0} {1}", nSerialNo, rsp);
        }


        public void OnReply_GetOptionExpirationDate(FTAPI_Conn client, uint nSerialNo, QotGetOptionExpirationDate.Response rsp)
        {
            Console.WriteLine("OnReply_GetOptionExpirationDate: {0} {1}", nSerialNo, rsp);
        }
    }

    class QotCallbackDemo : DemoBase
    {
        public void Run()
        {
            Console.WriteLine("Run QotCallback");
            //演示行情对象
            FTAPI_Qot client = new FTAPI_Qot(); //创建行情对象
            var callback = new SampleQotCallback();
            client.SetConnCallback(callback); //设置连接回调
            client.SetQotCallback(callback);  //设置行情回调
            client.SetClientInfo("FTAPI4NET_Sample", 1);
            //client.SetRSAPrivateKey(System.IO.File.ReadAllText(Config.RsaKeyFilePath, Encoding.UTF8)); //设置rsa私钥，可选
            // 建立连接，并触发OnInitConnect函数
            client.InitConnect(Config.OpendIP, Config.OpendPort, false); //开始连接

            // 等待一分钟再退出，观察推送的情况
            while (true)
            {
                Thread.Sleep(1000 * 60);
            }
        }
    }
}
