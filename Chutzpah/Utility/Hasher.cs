using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Chutzpah.Utility
{
    public class Hasher : IHasher
    {
        private readonly string versionSalt;
        private readonly ThreadLocal<SHA1Managed> sha1;

        public Hasher()
        {
            sha1 = new ThreadLocal<SHA1Managed>(() => new SHA1Managed());
            versionSalt = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string Hash(string input)
        {
            if (String.IsNullOrEmpty(input)) return null;
            input += versionSalt;
            return BytesToString(sha1.Value.ComputeHash(Encoding.UTF8.GetBytes(input)));
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