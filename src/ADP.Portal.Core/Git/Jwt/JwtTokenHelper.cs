using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ADP.Portal.Core.Git.Jwt
{
    public static class JwtTokenHelper
    {
        private static readonly long TicksSince197011 = DateTime.UnixEpoch.Ticks;
        public static string CreateEncodedJwtToken(string privateKeyBae64, int githubAppId, int expirationSeconds = 600, TimeSpan? iatOffset = null)
        {
            var utcNow = DateTime.UtcNow.Add(iatOffset ?? TimeSpan.Zero);
            var privateKeyString = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBae64));

            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyString);

            var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Iss, githubAppId.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUtcSeconds(utcNow).ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, ToUtcSeconds(utcNow.AddSeconds(expirationSeconds)).ToString())
            };

            var jwt = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                signingCredentials: signingCredentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return token;
        }

        private static long ToUtcSeconds(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - TicksSince197011) / TimeSpan.TicksPerSecond;
        }

    
    }
}
