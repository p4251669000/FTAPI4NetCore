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
    class StockSellDemo : DemoBase
    {
        static bool is_init = false;
        void Init()
        {
            if (is_init) return;

            // 建立行情和交易连接
            bool ret = InitConnectQotSync(Config.OpendIP, Config.OpendPort);
            if (!ret)
            {
                Console.WriteLine("ERROR: InitConnectQot, retMsg = fail to connect opend");
                return;
            }
            ret = InitConnectTrdSync(Config.OpendIP, Config.OpendPort);
            if (!ret)
            {
                Console.WriteLine("ERROR: InitConnectTrd, retMsg = fail to connect opend");
                return;
            }
            Console.WriteLine("Init connect succeed");
            is_init = true;
        }

        /// <summary>
        /// 使用第一个模拟账号在港股市场按照price的价格挂100股 00700股票的卖单，并两秒后取消订单
        /// 运行这个demo的时候，模拟账户中需要至少有100股腾讯（HK.00700）
        /// </summary>
        /// <param name="price">卖单的挂单价格，默认800</param>
        public void simpleSell(double price = 800) {
            Init();
            QotCommon.QotMarket qotMarket = QotCommon.QotMarket.QotMarket_HK_Security;
            TrdCommon.TrdMarket trdMarket = TrdCommon.TrdMarket.TrdMarket_HK; // 交易市场
            string code = "00700";
            
            int volume = 100;
            TrdCommon.TrdEnv trdEnv = TrdCommon.TrdEnv.TrdEnv_Simulate;

            // 获取第一个可以交易港股的模拟账户id
            ulong accID = GetFirstAccId(trdMarket, trdEnv);
            if (accID == 0)
            {
                Console.WriteLine("GetFirstAccId error");
                return;
            }
            Console.WriteLine("getAccList: first account id is {0}", accID);
            // 从快照中获取腾讯控股的每手股数
            int lotSize = 0;
            QotCommon.Security sec = MakeSec(qotMarket, code);
            {
                List<QotCommon.Security> secList = new List<QotCommon.Security>();
                secList.Add(sec);
                QotGetSecuritySnapshot.Response rsp = GetSecuritySnapshotSync(secList);
                if (rsp.RetType != (int)Common.RetType.RetType_Succeed) {
                    return;
                }
                lotSize = rsp.S2C.SnapshotListList[0].Basic.LotSize;
                if (lotSize <= 0) {
                    Console.WriteLine("invalid lot size; code={0} lotSize={1}", code, lotSize);
                    return;
                }
            }
            Console.WriteLine("getLotSize from securitySnapshot succeed: lotSize: {0}", lotSize);

            // 下单
            int qty = (volume / lotSize) * lotSize; // 将数量调整为整手的股数
            TrdCommon.TrdSecMarket secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_HK; // 证券所属市场
            TrdCommon.OrderType orderType = TrdCommon.OrderType.OrderType_Normal; // 订单类型
            TrdCommon.TrdHeader trdHeader = MakeTrdHeader(trdEnv, accID, trdMarket);
            TrdPlaceOrder.C2S c2s = TrdPlaceOrder.C2S.CreateBuilder()
                    .SetHeader(trdHeader)
                    .SetPacketID(trd.NextPacketID())
                    .SetTrdSide((int)TrdCommon.TrdSide.TrdSide_Sell)
                    .SetOrderType((int)orderType)
                    .SetCode(code)
                    .SetQty(qty)
                    .SetPrice(price)
                    .SetAdjustPrice(true)
                    .SetSecMarket((int)secMarket)
                    .Build();
            Console.WriteLine("Place sell order: code: {0}, qty: {1}, price: {2}", code, qty, price);
            TrdPlaceOrder.Response placeOrderRsp = PlaceOrderSync(c2s);
            if (placeOrderRsp.RetType != (int)Common.RetType.RetType_Succeed)
            {
                return;
            }
            ulong orderID = placeOrderRsp.S2C.OrderID;
            Console.WriteLine("placeOrder succeed: accId: {0}, orderId: {1}", placeOrderRsp.S2C.Header.AccID, orderID);
            
            // 2秒后撤掉这个订单
            Thread.Sleep(1000 * 2);
            TrdModifyOrder.C2S modifyOrderC2S = TrdModifyOrder.C2S.CreateBuilder()
                    .SetPacketID(trd.NextPacketID())
                    .SetHeader(trdHeader)
                    .SetOrderID(orderID)
                    .SetModifyOrderOp((int)TrdCommon.ModifyOrderOp.ModifyOrderOp_Cancel)
                    .Build();
            TrdModifyOrder.Response modifyOrderRsp = ModifyOrderSync(modifyOrderC2S);
            if (modifyOrderRsp.RetType == (int)Common.RetType.RetType_Succeed)
            {
                Console.WriteLine("Cancel order {0} succeed", orderID);
            }
        }

        /// <summary>
        /// 使用第一个模拟账号在港股市场按照买一价的1.2倍的价格挂100股 00700股票的卖单，并两秒后取消订单
        /// 买一价的价格从实时摆盘中获取
        /// 运行这个demo的时候，模拟账户中需要至少有100股腾讯（HK.00700）
        /// </summary>
        public void smartSell() {
            Init();
            QotCommon.QotMarket qotMarket = QotCommon.QotMarket.QotMarket_HK_Security;
            string code = "00700";
            QotCommon.Security sec = MakeSec(qotMarket, code);
            // 获取实时摆盘之前需要订阅
            QotSub.Response subRsp = SubSync(new List<QotCommon.Security>(){sec},
                    new List<QotCommon.SubType>(){QotCommon.SubType.SubType_OrderBook},
                    true,
                    false);
            if (subRsp.RetType != (int)Common.RetType.RetType_Succeed) {
                return;
            }
            Console.WriteLine("Sub succeed");

            // 获取实时摆盘信息并从中拿到买一价格
            QotGetOrderBook.Response getOrderBookRsp = GetOrderBookSync(sec, 1);
            if (getOrderBookRsp.RetType != (int)Common.RetType.RetType_Succeed) {
                return;
            }
            double bid1Price = getOrderBookRsp.S2C.OrderBookBidListList[0].Price;
            Console.WriteLine("get bid1Price succeed. bid1Price: {0}", bid1Price);
            simpleSell(bid1Price * 1.2);
        }
    }
}
