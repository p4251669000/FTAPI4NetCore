using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Futu.OpenApi
{
    public enum ProtoID : uint
    {
        InitConnect = 1001,  //初始化连接
        GetGlobalState = 1002, //获取全局状态
        KeepAlive = 1004, //心跳
        QotSub = 3001, //订阅或者反订阅
        QotRegQotPush = 3002, //注册推送
        QotGetSubInfo = 3003, //获取订阅信息
        QotGetTicker = 3010, //获取逐笔,调用该接口前需要先订阅(订阅位：Qot_Common.SubType_Ticker)
        QotGetBasicQot = 3004, //获取基本行情,调用该接口前需要先订阅(订阅位：Qot_Common.SubType_Basic)
        QotGetOrderBook = 3012, //获取摆盘,调用该接口前需要先订阅(订阅位：Qot_Common.SubType_OrderBook)
        QotGetKL = 3006, //获取K线，调用该接口前需要先订阅(订阅位：Qot_Common.SubType_KL_XXX)
        QotGetRT = 3008, //获取分时，调用该接口前需要先订阅(订阅位：Qot_Common.SubType_RT)
        QotGetBroker = 3014, //获取经纪队列，调用该接口前需要先订阅(订阅位：Qot_Common.SubType_Broker)
        QotRequestRehab = 3105, //在线请求历史复权信息，不读本地历史数据DB
        QotRequestHistoryKL = 3103, //在线请求历史K线，不读本地历史数据DB
        QotRequestHistoryKLQuota = 3104, //获取历史K线已经用掉的额度
        QotGetStaticInfo = 3202, //获取静态信息
        QotGetSecuritySnapshot = 3203, //获取股票快照
        QotGetPlateSet = 3204, //获取板块集合下的板块
        QotGetPlateSecurity = 3205, //获取板块下的股票
        QotGetReference = 3206, //获取相关股票
        QotGetOwnerPlate = 3207, //获取股票所属的板块
        QotGetHoldingChangeList = 3208, //获取大股东持股变化列表
        QotGetOptionChain = 3209, //筛选期权
        QotGetWarrant = 3210, //筛选窝轮
        QotGetCapitalFlow = 3211, //获取资金流向
        QotGetCapitalDistribution = 3212, //获取资金分布
        QotGetUserSecurity = 3213, //获取自选股分组下的股票
        QotModifyUserSecurity = 3214, //修改自选股分组下的股票
        QotStockFilter = 3215, // 获取条件选股
        QotGetCodeChange = 3216, // 获取股票代码变化信息
        QotGetIpoList = 3217, //获取新股Ipo
        QotGetFutureInfo = 3218,   //获取期货合约资料
        QotRequestTradeDate = 3219, //在线拉取交易日
        QotSetPriceReminder = 3220,  //设置到价提醒
        QotGetPriceReminder = 3221,  // 获取到价提醒
        QotGetUserSecurityGroup = 3222, //获取自选股分组
        QotGetMarketState = 3223, //获取指定品种的市场状态
        QotGetOptionExpirationDate = 3224,  //获取期权到期日
        Notify = 1003, //推送通知
        QotUpdateBasicQot = 3005, //推送基本行情
        QotUpdateKL = 3007, //推送K线
        QotUpdateRT = 3009, //推送分时
        QotUpdateTicker = 3011, //推送逐笔
        QotUpdateOrderBook = 3013, //推送买卖盘
        QotUpdateBroker = 3015, //推送经纪队列
        QotUpdatePriceReminder = 3019, //到价提醒通知
        TrdGetAccList = 2001, //获取交易账户列表
        TrdUnlockTrade = 2005, //解锁
        TrdSubAccPush = 2008, //订阅接收推送数据的交易账户
        TrdGetFunds = 2101, //获取账户资金
        TrdGetPositionList = 2102, //获取账户持仓
        TrdGetMaxTrdQtys = 2111, //获取最大交易数量
        TrdGetOrderList = 2201, //获取当日订单列表
        TrdPlaceOrder = 2202, //下单
        TrdModifyOrder = 2205, //修改订单
        TrdGetOrderFillList = 2211, //获取当日成交列表
        TrdGetHistoryOrderList = 2221, //获取历史订单列表
        TrdGetHistoryOrderFillList = 2222, //获取历史成交列表
        TrdGetMarginRatio = 2223,  //获取融资融券数据
        TrdGetOrderFee = 2225,  //获取订单费用
        TrdGetFlowSummary = 2226, //获取资金流水
        TrdUpdateOrder = 2208, //订单状态变动通知(推送)
        TrdUpdateOrderFill = 2218, //成交通知(推送)
    }

    public class ProtoUtil
    {
        public static bool IsPushProto(ProtoID protoID)
        {
            return protoID == ProtoID.QotUpdateBasicQot ||
                protoID == ProtoID.QotUpdateBroker ||
                protoID == ProtoID.QotUpdateKL ||
                protoID == ProtoID.QotUpdateOrderBook ||
                protoID == ProtoID.QotUpdatePriceReminder ||
                protoID == ProtoID.QotUpdateRT ||
                protoID == ProtoID.QotUpdateTicker ||
                protoID == ProtoID.TrdUpdateOrder ||
                protoID == ProtoID.TrdUpdateOrderFill ||
                protoID == ProtoID.Notify;
        }
    }
}
