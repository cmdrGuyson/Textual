using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Textual
{
    class Encryptor
    {

        public static string Encrypt(string text, string key)
        {
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            string IV = "zxcvbnmdfrasdfgh";
            byte[] input_text_bytes = ASCIIEncoding.ASCII.GetBytes(text);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            aes.BlockSize = 128;
            aes.KeySize = 256;
            aes.Key = pdb.GetBytes(32);
            aes.IV = ASCIIEncoding.ASCII.GetBytes(IV);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            ICryptoTransform crypto = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encrypted = crypto.TransformFinalBlock(input_text_bytes, 0, input_text_bytes.Length);
            crypto.Dispose();

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encrypted, string key)
        {
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            string IV = "zxcvbnmdfrasdfgh";
            byte[] encryptedbytes = Convert.FromBase64String(encrypted);

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            aes.BlockSize = 128; 
            aes.KeySize = 256;
            aes.Key = pdb.GetBytes(32);
            aes.IV = ASCIIEncoding.ASCII.GetBytes(IV);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            ICryptoTransform crypto = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] secret = crypto.TransformFinalBlock(encryptedbytes, 0, encryptedbytes.Length);
            crypto.Dispose();

            return ASCIIEncoding.ASCII.GetString(secret);

        }
    }
}
