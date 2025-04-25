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

    class MACDUtil {
    public static void CalcEMA(List<double> input, int n, List<double> output) {
        int inputSize = input.Count;
        if (inputSize > 0) {
            double lastEMA = input[0];
            output.Add(lastEMA);
            for (int i = 1; i < inputSize; i++) {
                double curEMA = (input[i] * 2 + lastEMA * (n - 1)) / (n + 1);
                output.Add(curEMA);
                lastEMA = curEMA;
            }
        }
    }

    public static void CalcMACD(List<double> closeList, int shortPeriod, int longPeriod, int smoothPeriod,
                         List<double> difList, List<double> deaList, List<double> macdList) {
        difList.Clear();
        deaList.Clear();
        macdList.Clear();
        List<double> shortEMA = new List<double>();
        List<double> longEMA = new List<double>();
        CalcEMA(closeList, shortPeriod, shortEMA);
        CalcEMA(closeList, longPeriod, longEMA);
        int shortCount = shortEMA.Count;
        int longCount = longEMA.Count;
        for (int i = 0; i < shortCount && i < longCount; i++) {
            difList.Add(shortEMA[i] - longEMA[i]);
        }

        CalcEMA(difList, smoothPeriod, deaList);
        int difCount = difList.Count;
        int deaCount = deaList.Count;
        for (int i = 0; i < difCount && i < deaCount; i++) {
            macdList.Add((difList[i] - deaList[i]) * 2);
        }
    }
}

    class MACDStrategyDemo : DemoBase
    {
    /// <summary>
    /// 简单的MACD买卖策略
    /// 根据之前100天的日K线判断当前是否是买点或者卖点
    /// </summary>
    public void Run()
    {
        Console.WriteLine("Run MACDStrategy");
        QotCommon.Security sec = MakeSec(QotCommon.QotMarket.QotMarket_HK_Security, "00700");
        TrdCommon.TrdMarket trdMarket = TrdCommon.TrdMarket.TrdMarket_HK;
        TrdCommon.TrdEnv trdEnv = TrdCommon.TrdEnv.TrdEnv_Simulate;

        bool ret = InitConnectTrdSync(Config.OpendIP, Config.OpendPort);
        if (!ret)
        {
            Console.WriteLine("ERROR: InitConnectTrd, retMsg = fail to connect trd");
            return;
        }
        ret = InitConnectQotSync(Config.OpendIP, Config.OpendPort);
        if (!ret)
        {
            Console.WriteLine("ERROR: InitConnectTrd, retMsg = fail to connect qot");
            return;
        }
        Console.WriteLine("Init connect succeed");

        // 获取第一个可以交易港股的模拟账户id
        ulong accId = GetFirstAccId(trdMarket, trdEnv);
        if (accId == 0)
        {
            Console.WriteLine("GetFirstAccId error");
            return;
        }
        Console.WriteLine("getAccList: first account id is {0}", accId);

        //// 下面使用的是模拟交易可以不解锁，这里展示解锁方法（真实环境交易之前需要解锁）
        //TrdUnlockTrade.Response unlockTrdRsp = UnlockTradeSync(Config.UnlockTradePwdMd5, Config.SecurityFirm, true);
        //if (unlockTrdRsp.RetType != (int)Common.RetType.RetType_Succeed)
        //{
        //    Console.WriteLine("unlockTradeSync err; retType={0} msg={1}", unlockTrdRsp.RetType, unlockTrdRsp.RetMsg);
        //}
        //else
        //{
        //    Console.WriteLine("unlock succeed");
        //}

        DateTime now = DateTime.Now;
        DateTime startDate = now.Subtract(new TimeSpan(100, 0, 0, 0));
        QotRequestHistoryKL.Response historyKLRsp = RequestHistoryKLSync(sec, QotCommon.KLType.KLType_Day,
                QotCommon.RehabType.RehabType_Forward,
                startDate.ToString("yyyy-MM-dd"),
                now.ToString("yyyy-MM-dd"),
                1000,
                null,
                new byte[] { },
                false);
        List<double> klCloseList = new List<double>();
        List<double> difList = new List<double>();
        List<double> deaList = new List<double>();
        List<double> macdList = new List<double>();
        foreach (QotCommon.KLine kl in historyKLRsp.S2C.KlListList)
        {
            klCloseList.Add(kl.ClosePrice);
        }
        MACDUtil.CalcMACD(klCloseList, 12, 26, 9, difList, deaList, macdList);
        int difCount = difList.Count;
        int deaCount = deaList.Count;
        if (difCount > 0 && deaCount > 0)
        {
            if (difList[difCount - 1] < deaList[deaCount - 1] &&
            difList[difCount - 2] > deaList[deaCount - 2])
            {
                // 查询持仓
                TrdCommon.TrdFilterConditions filterConditions = TrdCommon.TrdFilterConditions.CreateBuilder()
                        .AddCodeList(sec.Code)
                        .Build();
                TrdGetPositionList.Response getPositionListRsp = GetPositionListSync(accId, trdMarket,
                        trdEnv, filterConditions, null, null, false);
                if (getPositionListRsp.RetType != (int)Common.RetType.RetType_Succeed)
                {
                    return;
                }
                foreach (TrdCommon.Position pstn in getPositionListRsp.S2C.PositionListList)
                {
                    if (pstn.CanSellQty > 0)
                    {
                        Sell(sec, pstn, accId, trdMarket, trdEnv);
                        Console.WriteLine("MACDStrategy End");
                        return;
                    }
                }
            }
            else if (difList[difCount - 1] > deaList[deaCount - 1] &&
                    difList[difCount - 2] < deaList[deaCount - 2])
            {
                Buy(sec, accId, trdMarket, trdEnv);
                Console.WriteLine("MACDStrategy End");
                return;
            }
        }
        Console.WriteLine("No operation");
        Console.WriteLine("MACDStrategy End");
    }

    void Sell(QotCommon.Security sec, TrdCommon.Position pstn, ulong accId, TrdCommon.TrdMarket trdMarket,
              TrdCommon.TrdEnv trdEnv) {
        QotGetSecuritySnapshot.Response snapshotRsp = GetSecuritySnapshotSync(new List<QotCommon.Security>(){sec});
        if (snapshotRsp.RetType != (int)Common.RetType.RetType_Succeed) {
            return;
        }
        double price = snapshotRsp.S2C.SnapshotListList[0].Basic.CurPrice;
        TrdCommon.TrdSecMarket secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_Unknown;
        if (sec.Market == (int)QotCommon.QotMarket.QotMarket_HK_Security) {
            secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_HK;
        } else {
            secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_US;
        }
        TrdCommon.TrdHeader trdHeader = MakeTrdHeader(trdEnv, accId, trdMarket);
        TrdPlaceOrder.C2S c2s = TrdPlaceOrder.C2S.CreateBuilder()
                .SetPacketID(trd.NextPacketID())
                .SetHeader(trdHeader)
                .SetTrdSide((int)TrdCommon.TrdSide.TrdSide_Sell)
                .SetOrderType((int)TrdCommon.OrderType.OrderType_Normal)
                .SetCode(sec.Code)
                .SetQty(pstn.CanSellQty)
                .SetPrice(price)
                .SetAdjustPrice(true)
                .SetSecMarket((int)secMarket)
                .Build();
        Console.WriteLine("placeOrder: sell code: {0}, qty: {1}, price: {2}", sec.Code, pstn.CanSellQty, price);
        TrdPlaceOrder.Response placeOrderRsp = PlaceOrderSync(c2s);
    }

    void Buy(QotCommon.Security sec, ulong accId, TrdCommon.TrdMarket trdMarket,
             TrdCommon.TrdEnv trdEnv) {
        TrdCommon.TrdSecMarket secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_Unknown;
        if (sec.Market == (int)QotCommon.QotMarket.QotMarket_HK_Security) {
            secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_HK;
        } else {
            secMarket = TrdCommon.TrdSecMarket.TrdSecMarket_US;
        }

        TrdGetFunds.Response getFundsRsp = GetFundsSync(accId, trdMarket, trdEnv, false, TrdCommon.Currency.Currency_Unknown);
        if (getFundsRsp.RetType != (int)Common.RetType.RetType_Succeed) {
            return;
        }
        QotGetSecuritySnapshot.Response snapshotRsp = GetSecuritySnapshotSync(new List<QotCommon.Security>(){sec});
        if (snapshotRsp.RetType != (int)Common.RetType.RetType_Succeed) {
            return;
        }
        int lotSize = snapshotRsp.S2C.SnapshotListList[0].Basic.LotSize;
        double curPrice = snapshotRsp.S2C.SnapshotListList[0].Basic.CurPrice;
        double cash = getFundsRsp.S2C.Funds.Cash;
        int qty = (int)Math.Floor(cash / curPrice);
        qty = qty / lotSize * lotSize;
        TrdCommon.TrdHeader trdHeader = MakeTrdHeader(trdEnv, accId, trdMarket);
        TrdPlaceOrder.C2S c2s = TrdPlaceOrder.C2S.CreateBuilder()
                .SetPacketID(trd.NextPacketID())
                .SetHeader(trdHeader)
                .SetTrdSide((int)TrdCommon.TrdSide.TrdSide_Buy)
                .SetOrderType((int)TrdCommon.OrderType.OrderType_Normal)
                .SetCode(sec.Code)
                .SetQty(qty)
                .SetPrice(curPrice)
                .SetAdjustPrice(true)
                .SetSecMarket((int)secMarket)
                .Build();
        Console.WriteLine("placeOrder: buy code: {0}, qty: {1}, price: {2}", sec.Code, qty, curPrice);
        TrdPlaceOrder.Response placeOrderRsp = PlaceOrderSync(c2s);
    }
    }
}
