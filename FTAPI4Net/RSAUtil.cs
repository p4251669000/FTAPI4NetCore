using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.OpenSsl;
using System.IO;

namespace Futu.OpenApi
{
    public class RSAUtil
    {
        public static AsymmetricCipherKeyPair LoadKeyPair(String privateKey)
        {
            using (TextReader reader = new StringReader(privateKey))
            {
                PemReader pr = new PemReader(reader);
                AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
                return keyPair;
            }
        }

        public static byte[] decrypt(byte[] src, AsymmetricKeyParameter privKey)
        {
            const int block = 128;
            var decryptEngine = new Pkcs1Encoding(new RsaEngine());
            decryptEngine.Init(false, privKey);

            List<byte> buffer = new List<byte>();
            for (int i = 0; i < src.Length; i += block)
            {
                int size = block;
                if (src.Length - i < block)
                {
                    size = src.Length - i;
                }
                byte[] enc = decryptEngine.ProcessBlock(src, i, size);
                buffer.AddRange(enc);
            }

            byte[] result = new byte[buffer.Count];
            buffer.CopyTo(result);
            return result;
        }

        public static byte[] encrypt(byte[] src, AsymmetricKeyParameter pubKey)
        {
            const int block = 100;
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            encryptEngine.Init(true, pubKey);

            List<byte> buffer = new List<byte>();
            for (int i = 0; i < src.Length; i += block)
            {
                int size = block;
                if (src.Length - i < block)
                {
                    size = src.Length - i;
                }
                byte[] enc = encryptEngine.ProcessBlock(src, i, size);
                buffer.AddRange(enc);
            }

            byte[] result = new byte[buffer.Count];
            buffer.CopyTo(result);
            return result;
        }
    }
}
