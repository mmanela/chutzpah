using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Chutzpah.Utility
{
    public class Hasher : IHasher
    {
        private readonly MD5CryptoServiceProvider md5;
        private readonly string versionSalt;

        public Hasher()
        {
            md5 = new MD5CryptoServiceProvider();
            versionSalt = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string Hash(string input)
        {
            if (String.IsNullOrEmpty(input)) return null;
            input += versionSalt;
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