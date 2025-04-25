using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Futu.OpenApi
{
    public class SHA1Util
    {
        public static byte[] Calc(byte[] input)
        {
            HashAlgorithm algo = SHA1.Create();
            return algo.ComputeHash(input);
        }
    }
}
