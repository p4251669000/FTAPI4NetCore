using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Futu.OpenApi;
using Futu.OpenApi.Pb;
using System.Security.Cryptography;
using Google.ProtocolBuffers;

namespace FTAPI4NetSample
{
    class Program 
    {
        enum DemoName
        {
            DEMO_UNKNOWN = 0,
            DEMO_GET_SECURITY_SNAPSHOT,
            DEMO_STOCK_SIMPLE_SELL,
            DEMO_STOCK_SMART_SELL,
            DEMO_QOT_CALLBACK,
            DEMO_TRD_CALLBACK,
            DEMO_QUOTE_AND_TRADE,
            DEMO_MACD_STRATEGY
        }
        static string[] demoNames = { "Unknown", "GetSecuritySnapshot", "StockSimpleSell", "StockSmartSell",
                                     "QotCallback", "TrdCallback", "QuoteAndTrade", "MACDStrategy" };
        // 不传参数时，默认运行的demo
        const DemoName DEFAULT_DEMO_NAME = DemoName.DEMO_MACD_STRATEGY;

        static void Main(string[] args)
        {
            FTAPI.Init(); //初始化api

            // 解析参数中的demo名称运行指定demo，如果没有指定，运行默认demo
            string demoName = "Unknown";
            DemoName emDemo = DemoName.DEMO_UNKNOWN;
            if (args.Length > 0)
            {
                // 只支持一个参数并且仅为要运行的demo名称
                demoName = args[0];
            }

            for (int i = 0; i < demoNames.Length; i++)
            {
                if (demoName == demoNames[i])
                {
                    emDemo = (DemoName)i;
                }
            }
            if (emDemo == DemoName.DEMO_UNKNOWN)
            {
                emDemo = DEFAULT_DEMO_NAME;
            }

            switch (emDemo)
            {
                case DemoName.DEMO_GET_SECURITY_SNAPSHOT:
                    {
                        //获取快照示例
                        GetSecuritySnapshotDemo demo = new GetSecuritySnapshotDemo();
                        demo.Run();
                    }
                    break;
                case DemoName.DEMO_STOCK_SIMPLE_SELL:
                    {
                        // 按照指定价格挂卖单
                        StockSellDemo demo = new StockSellDemo();
                        demo.simpleSell();
                    }
                    break;
                case DemoName.DEMO_STOCK_SMART_SELL:
                    {
                        // 按照指定价格挂卖单
                        StockSellDemo demo = new StockSellDemo();
                        demo.smartSell();
                    }
                    break;
                case DemoName.DEMO_QOT_CALLBACK:
                    {
                        //演示行情对象
                        QotCallbackDemo demo = new QotCallbackDemo();
                        demo.Run();
                    }
                    break;
                case DemoName.DEMO_TRD_CALLBACK:
                    {
                        //演示交易对象
                        TrdCallbackDemo demo = new TrdCallbackDemo();
                        demo.Run();
                    }
                    break;
                case DemoName.DEMO_QUOTE_AND_TRADE:
                    {
                        //交易示例
                        QuoteAndTradeDemo demo = new QuoteAndTradeDemo();
                        demo.TradeHkTest();
                    }
                    break;
                case DemoName.DEMO_MACD_STRATEGY:
                    {
                        // 简单的MACD买卖策略
                        MACDStrategyDemo demo = new MACDStrategyDemo();
                        demo.Run();
                    }
                    break;
            }

            // 主线程等待10秒后退出
            Thread.Sleep(1000 * 10);
        }
    }
}
