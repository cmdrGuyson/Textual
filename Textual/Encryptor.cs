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
        private string key = "dofkrfaosrdedofkrfaosrdedofkrfao";
        private string IV = "zxcvbnmdfrasdfgh";

        private string Encrypt(string text)
        {
            byte[] input_text_bytes = ASCIIEncoding.ASCII.GetBytes(text);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            aes.BlockSize = 128;
            aes.KeySize = 256;
            aes.Key = ASCIIEncoding.ASCII.GetBytes(key);
            aes.IV = ASCIIEncoding.ASCII.GetBytes(IV);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            ICryptoTransform crypto = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encrypted = crypto.TransformFinalBlock(input_text_bytes, 0, input_text_bytes.Length);
            crypto.Dispose();

            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string encrypted)

        {

            byte[] encryptedbytes = Convert.FromBase64String(encrypted);

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            aes.BlockSize = 128; aes.KeySize = 256;
            aes.Key = ASCIIEncoding.ASCII.GetBytes(key);
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
