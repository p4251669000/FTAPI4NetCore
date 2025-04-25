using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Futu.OpenApi
{
    class SimpleBuffer
    {
        internal byte[] Buf;
        internal int Start;
        internal int Length;
        internal int Limit;

        internal SimpleBuffer(int limit)
        {
            if (limit <= 0)
                throw new ArgumentException("limit should be greater than 0");

            Buf = new byte[limit];
            this.Limit = limit;
        }

        internal void Compact()
        {
            if (Length > 0)
            {
                Buffer.BlockCopy(Buf, Start, Buf, 0, Length);
            }
            Start = 0;
        }

        internal int Append(byte[] src, int srcPos, int srcLen)
        {
            if (Start + Length + srcLen > Limit)
                Compact();
            int available = Limit - Start - Length;
            int copyLen = Math.Min(available, srcLen);
            if (copyLen == 0)
                return 0;
            Buffer.BlockCopy(src, srcPos, Buf, Start + Length, copyLen);
            Length += copyLen;
            return copyLen;
        }

        internal void EnsureAppend(byte[] src, int srcPos, int srcLen)
        {
            if (Start + Length + srcLen > Limit)
                Compact();
            int available = Limit - Start - Length;
            if (available < srcLen)
            {
                Resize(Length + srcLen);
            }
            Buffer.BlockCopy(src, srcPos, Buf, Start + Length, srcLen);
            Length += srcLen;
        }

        internal void Consume(int consumption)
        {
            if (consumption < 0 || consumption > Length)
                throw new ArgumentException("Invalid consumption");
            Start += consumption;
            Length -= consumption;
        }

        internal void Resize(int newLimit)
        {
            if (newLimit != Limit)
            {
                int len = Math.Min(newLimit, Length);
                byte[] newBuf = new byte[newLimit];
                Buffer.BlockCopy(Buf, Start, newBuf, 0, len);
                Buf = newBuf;
                Limit = newLimit;
                Length = len;
                Start = 0;
            }
        }
    }

}
