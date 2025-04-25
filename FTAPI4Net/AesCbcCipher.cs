using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Futu.OpenApi
{
    public class AesCbcCipher
    {
        IBufferedCipher cipher;
        ICipherParameters cipherParams;

        public AesCbcCipher(byte[] key, byte[] iv)
        {
            cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7Padding");
            KeyParameter keyParamter = ParameterUtilities.CreateKeyParameter("AES", key);
            cipherParams = new ParametersWithIV(keyParamter, iv);
        }

        public byte[] encrypt(byte[] src)
        {
            cipher.Reset();
            cipher.Init(true, cipherParams);
            return cipher.DoFinal(src);
        }

        public byte[] decrypt(byte[] src)
        {
            cipher.Reset();
            cipher.Init(false, cipherParams);
            return cipher.DoFinal(src);
        }
    }
}
