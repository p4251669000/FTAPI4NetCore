using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Futu.OpenApi
{
    class BinaryDataReader
    {
        bool isLittleEndian = true;
        byte[] buf = new byte[4];

        internal BinaryDataReader(bool isLittleEndian)  
        {
            this.isLittleEndian = isLittleEndian;
        }

        internal uint ReadUint(byte[] src, int offset)
        {
            Buffer.BlockCopy(src, offset, buf, 0, 4);
            if (BitConverter.IsLittleEndian != isLittleEndian)
            {
                Array.Reverse(buf);
            }
            return BitConverter.ToUInt32(buf, 0);
        }
    }

    class BinaryDataWriter
    {
        bool isLittleEndian = true;

        internal BinaryDataWriter(bool isLittleEndian)
        {
            this.isLittleEndian = isLittleEndian;
        }

        internal void WriteUint(uint data, byte[] dst, int offset)
        {
            byte[] buf = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian != isLittleEndian)
            {
                Array.Reverse(buf);
            }
            Buffer.BlockCopy(buf, 0, dst, offset, buf.Length);
        }
    }


    public class ProtoHeader
    {
        public const int HeaderSize = 2 + 4 + 1 + 1 + 4 + 4 + 20 + 8;

        public byte[] szHeaderFlag = { (byte)'F', (byte)'T' };
        public uint nProtoID;
        public byte nProtoFmtType;
        public byte nProtoVer;
        public uint nSerialNo;
        public uint nBodyLen;
        public byte[] arrBodySHA1 = new byte[20];
        public byte[] arrReserved = new byte[8];

        public static ProtoHeader Parse(byte[] data, int offset)
        {
            if (offset + HeaderSize > data.Length)
                return null;

            BinaryDataReader readerHelper = new BinaryDataReader(true);
            ProtoHeader header = new ProtoHeader();
            Buffer.BlockCopy(data, offset, header.szHeaderFlag, 0, 2);
            offset += 2;
            header.nProtoID = readerHelper.ReadUint(data, offset);
            offset += 4;
            header.nProtoFmtType = data[offset];
            offset += 1;
            header.nProtoVer = data[offset];
            offset += 1;
            header.nSerialNo = readerHelper.ReadUint(data, offset);
            offset += 4;
            header.nBodyLen = readerHelper.ReadUint(data, offset);
            offset += 4;
            Buffer.BlockCopy(data, offset, header.arrBodySHA1, 0, header.arrBodySHA1.Length);
            offset += header.arrBodySHA1.Length;
            Buffer.BlockCopy(data, offset, header.arrReserved, 0, header.arrReserved.Length);
            return header;
        }

        public void Write(byte[] dst)
        {
            if (dst.Length < HeaderSize)
            {
                throw new ArgumentException("dst capacity not enough");
            }

            BinaryDataWriter writerHelper = new BinaryDataWriter(true);
            int offset = 0;
            Buffer.BlockCopy(szHeaderFlag, 0, dst, offset, 2);
            offset += 2;
            writerHelper.WriteUint(nProtoID, dst, offset);
            offset += 4;
            dst[offset] = nProtoFmtType;
            offset += 1;
            dst[offset] = nProtoVer;
            offset += 1;
            writerHelper.WriteUint(nSerialNo, dst, offset);
            offset += 4;
            writerHelper.WriteUint(nBodyLen, dst, offset);
            offset += 4;
            Buffer.BlockCopy(arrBodySHA1, 0, dst, offset, arrBodySHA1.Length);
            offset += arrBodySHA1.Length;
            Buffer.BlockCopy(arrReserved, 0, dst, offset, arrReserved.Length);
        }
    }
}
