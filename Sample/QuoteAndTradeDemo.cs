using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Futu.OpenApi;
using Futu.OpenApi.Pb;
using Google.ProtocolBuffers;

namespace FTAPI4NetSample
{
    class QuoteAndTradeDemo : DemoBase
    {
        public void QuoteTest()
        {
            bool ret = InitConnectQotSync(Config.OpendIP, Config.OpendPort);
            if (!ret)
            {
                Console.WriteLine("ERROR: InitConnectQot, retMsg = fail to connect opend");
                return;
            }
            List<QotCommon.Security> secArr = new List<QotCommon.Security>() {
                    MakeSec(QotCommon.QotMarket.QotMarket_HK_Security, "00388"),
                    MakeSec(QotCommon.QotMarket.QotMarket_HK_Security, "00700"),
                    MakeSec(QotCommon.QotMarket.QotMarket_HK_Security, "HSImain")
            };
            List<QotCommon.SubType> subTypes = new List<QotCommon.SubType>() {
                    QotCommon.SubType.SubType_Basic,
                    QotCommon.SubType.SubType_Broker,
                    QotCommon.SubType.SubType_OrderBook,
                    QotCommon.SubType.SubType_RT,
                    QotCommon.SubType.SubType_KL_Day,
                    QotCommon.SubType.SubType_Ticker
            };
            QotSub.Response subRsp = SubSync(secArr, subTypes, true, true);
        }

        public void TradeHkTest()
        {
            Console.WriteLine("Run QuoteAndTrade");
            bool ret = InitConnectTrdSync(Config.OpendIP, Config.OpendPort);
            if (!ret)
            {
                Console.WriteLine("ERROR: InitConnectTrd, retMsg = fail to connect opend");
                return;
            }
            else
            {
                Console.WriteLine("trd connected");
            }

            //// 下面使用的是模拟交易可以不解锁，这里展示解锁方法（真实环境交易之前需要解锁）
            //TrdUnlockTrade.Response unlockTradeRsp = UnlockTradeSync(Config.UnlockTradePwdMd5, Config.SecurityFirm, true);
            //if (unlockTradeRsp.RetType == (int)Common.RetType.RetType_Succeed)
            //{
            //    Console.WriteLine("unlock succeed");
            //}

            // 获取账户列表，并从中拿到第一个能交易港股的模拟账户演示模拟交易
            ulong accId = GetFirstAccId(TrdCommon.TrdMarket.TrdMarket_HK, TrdCommon.TrdEnv.TrdEnv_Simulate);
            if (accId == 0)
            {
                Console.WriteLine("GetAccList error");
                return;
            }
            Console.WriteLine("getAccList: first account id is {0}", accId);

            // 获取第一个模拟账户的账户资金
            TrdGetFunds.Response getFundsRsp = GetFundsSync(accId, TrdCommon.TrdMarket.TrdMarket_HK, TrdCommon.TrdEnv.TrdEnv_Simulate,
                false, TrdCommon.Currency.Currency_Unknown);
            Console.WriteLine("getFunds succeed: totalAssets: {0}", getFundsRsp.S2C.Funds.TotalAssets);


            {
                // 查询盈亏在-50% 到 50% 的持仓
                TrdGetPositionList.Response getPositionListRsp = GetPositionListSync(accId,
                        TrdCommon.TrdMarket.TrdMarket_HK,
                        TrdCommon.TrdEnv.TrdEnv_Simulate, null,
                        -50.0, 50.0, false);
                Console.WriteLine("getPositionList: position list count: {0}", getPositionListRsp.S2C.PositionListCount);
            }

            {
                // 今日订单列表
                TrdGetOrderList.Response getOrderListRsp = GetOrderListSync(accId, TrdCommon.TrdMarket.TrdMarket_HK,
                        TrdCommon.TrdEnv.TrdEnv_Simulate, false, null,
                        new List<TrdCommon.OrderStatus> { TrdCommon.OrderStatus.OrderStatus_Submitted });
                Console.WriteLine("getOrderList: Count: {0}", getOrderListRsp.S2C.OrderListCount);
            }

            {
                // 模拟交易下单
                TrdCommon.TrdHeader header = TrdCommon.TrdHeader.CreateBuilder()
                        .SetTrdEnv((int)TrdCommon.TrdEnv.TrdEnv_Simulate)
                        .SetAccID(accId)
                        .SetTrdMarket((int)TrdCommon.TrdMarket.TrdMarket_HK)
                        .Build();
                TrdPlaceOrder.C2S c2s = TrdPlaceOrder.C2S.CreateBuilder()
                        .SetPacketID(trd.NextPacketID())
                        .SetHeader(header)
                        .SetTrdSide((int)TrdCommon.TrdSide.TrdSide_Buy)
                        .SetOrderType((int)TrdCommon.OrderType.OrderType_Normal)
                        .SetCode("00700")
                        .SetQty(100)
                        .SetPrice(500)
                        .SetAdjustPrice(true)
                        .SetSecMarket((int)TrdCommon.TrdSecMarket.TrdSecMarket_HK)
                        .Build();
                TrdPlaceOrder.Response placeOrderRsp = PlaceOrderSync(c2s);
                Console.WriteLine("placeOrder: order id: {0}", placeOrderRsp.S2C.OrderID);

                ulong orderId = placeOrderRsp.S2C.OrderID;
                // 2秒后撤掉这个订单
                Thread.Sleep(1000 * 2);
                TrdModifyOrder.C2S modifyOrderC2S = TrdModifyOrder.C2S.CreateBuilder()
                        .SetPacketID(trd.NextPacketID())
                        .SetHeader(header)
                        .SetOrderID(orderId)
                        .SetModifyOrderOp((int)TrdCommon.ModifyOrderOp.ModifyOrderOp_Cancel)
                        .Build();
                TrdModifyOrder.Response modifyOrderRsp = ModifyOrderSync(modifyOrderC2S);
                if (modifyOrderRsp.RetType == (int)Common.RetType.RetType_Succeed)
                {
                    Console.WriteLine("Cancel order {0} succeed", orderId);
                }
            }

            {
                //// 模拟交易不支持成交数据
                //TrdCommon.TrdFilterConditions filterConditions = TrdCommon.TrdFilterConditions.CreateBuilder()
                //        .AddCodeList("00700")
                //        .Build();
                //TrdGetOrderFillList.Response getOrderFillListRsp = GetOrderFillListSync(accId,
                //        TrdCommon.TrdMarket.TrdMarket_HK,
                //        TrdCommon.TrdEnv.TrdEnv_Simulate, false, filterConditions);
                //Console.WriteLine("getOrderFillList: {0}", getOrderFillListRsp.RetMsg);
            }
            Console.WriteLine("QuoteAndTrade End");
        }


        public override void OnReply_UpdateOrderBook(FTAPI_Conn client, uint nSerialNo, QotUpdateOrderBook.Response rsp)
        {
            Console.Write("OnReply_UpdateOrderBook: {0}\n", rsp);
        }


        public override void OnReply_UpdateBasicQot(FTAPI_Conn client, uint nSerialNo, QotUpdateBasicQot.Response rsp)
        {
            Console.Write("OnReply_UpdateBasicQuote: {0}\n", rsp);
        }


        public override void OnReply_UpdateKL(FTAPI_Conn client, uint nSerialNo, QotUpdateKL.Response rsp)
        {
            Console.Write("OnReply_UpdateKL: {0}\n", rsp);
        }


        public override void OnReply_UpdateRT(FTAPI_Conn client, uint nSerialNo, QotUpdateRT.Response rsp)
        {
            Console.Write("OnReply_UpdateRT: {0}\n", rsp);
        }


        public override void OnReply_UpdateTicker(FTAPI_Conn client, uint nSerialNo, QotUpdateTicker.Response rsp)
        {
            Console.Write("OnReply_UpdateTicker: {0}\n", rsp);
        }


        public override void OnReply_UpdateBroker(FTAPI_Conn client, uint nSerialNo, QotUpdateBroker.Response rsp)
        {
            Console.Write("OnReply_UpdateBroker: {0}\n", rsp);
        }


        public override void OnReply_UpdateOrder(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrder.Response rsp)
        {
            Console.Write("OnReply_UpdateOrder: {}\n", rsp);
        }


        public override void OnReply_UpdateOrderFill(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrderFill.Response rsp)
        {
            Console.Write("OnReply_UpdateOrderFill: {}\n", rsp);
        }
    }
}
