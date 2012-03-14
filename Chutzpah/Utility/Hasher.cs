using System;
using System.Security.Cryptography;
using System.Text;

namespace Chutzpah.Utility
{
    public class Hasher : IHasher
    {
        private MD5CryptoServiceProvider md5;

        public Hasher()
        {
            md5 = new MD5CryptoServiceProvider();
        }

        public string Hash(string input)
        {
            if (String.IsNullOrEmpty(input)) return null;

            return BytesToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private string BytesToString(byte[] arr)
        {
            var output = new StringBuilder(arr.Length);
            for (var i = 0; i < arr.Length; i++)
            {
                output.Append(arr[i].ToString("x2"));
            }
            return output.ToString();
        }
    }
}