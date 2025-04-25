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
    class GetSecuritySnapshotDemo : DemoBase
    {
        public void Run() 
        {
            Console.WriteLine("Run GetSecuritySnapshotDemo");
            QotCommon.QotMarket market = QotCommon.QotMarket.QotMarket_HK_Security;
            bool ret = InitConnectQotSync("127.0.0.1", (ushort)11111);
            if (ret) {
                Console.WriteLine("qot connected");
            } else {
                Console.WriteLine("ERROR: InitConnectQot, retMsg = fail to connect opend");
                return;
            }

            int[] stockTypes = {
                (int)QotCommon.SecurityType.SecurityType_Eqty,
                (int)QotCommon.SecurityType.SecurityType_Index,
                (int)QotCommon.SecurityType.SecurityType_Trust,
                (int)QotCommon.SecurityType.SecurityType_Warrant,
                (int)QotCommon.SecurityType.SecurityType_Bond
            };
            List<QotCommon.Security> stockCodes = new List<QotCommon.Security>();
            foreach (int stockType in stockTypes) {
                QotGetStaticInfo.C2S c2s = QotGetStaticInfo.C2S.CreateBuilder()
                        .SetMarket((int)market)
                        .SetSecType(stockType)
                        .Build();
                QotGetStaticInfo.Response rsp = GetStaticInfoSync(c2s);
                if (rsp.RetType != (int)Common.RetType.RetType_Succeed) {
                    return;
                }
                foreach (QotCommon.SecurityStaticInfo info in rsp.S2C.StaticInfoListList) {
                    stockCodes.Add(info.Basic.Security);
                }
            }

            if (stockCodes.Count > 0) {
                Console.WriteLine("Get {0} stock codes", stockCodes.Count);
            }
            else
            {
                Console.WriteLine("market:'{0}' can not get stock info ", market);
                return;
            }

            // 分批获取前面最多1000只股票的快照信息
            for (int i = 0; i < 1000; i += 200)
            {
                int count = i + 200 <= stockCodes.Count ? 200 : stockCodes.Count - i;
                List<QotCommon.Security> codes = stockCodes.GetRange(i, count);
                QotGetSecuritySnapshot.Response rsp = GetSecuritySnapshotSync(codes);
                if (rsp.RetType == (int)Common.RetType.RetType_Succeed) {
                    // 打印出这一批200只股票中的第一支信息，可以在这里断点或者输出到文件中查看具体信息
                    //foreach (QotGetSecuritySnapshot.Snapshot snapshot in rsp.S2C.SnapshotListList) {
                    //    Console.WriteLine(snapshot);
                    //}
                    QotGetSecuritySnapshot.Snapshot snapshot = rsp.S2C.SnapshotListList[0];
                    Console.WriteLine("index: {0}, code: {1}, listTime: {2}", i, snapshot.Basic.Security.Code, snapshot.Basic.ListTime);
                }
                Thread.Sleep(3000);
            }
            Console.WriteLine("GetSecuritySnapshotDemo End");
        }
    }
}
