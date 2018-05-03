using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Services
{
    public class Authorization : IAuthorization
    {
        private string _apiSecret = null;
        private string _apiSecretHash = null;

        public Authorization(string apiSecret)
        {
            _apiSecret = apiSecret;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(apiSecret));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                _apiSecretHash = sb.ToString();
            }
        }

        public bool CheckHash(string hash)
        {
            return string.Compare(hash, _apiSecretHash, ignoreCase: true) == 0;
        }
    }
}
