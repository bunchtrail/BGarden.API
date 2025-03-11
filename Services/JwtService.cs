using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BGarden.Domain.Entities;
using BGarden.API.Interfaces;

namespace BGarden.API.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            _issuer = jwtSettings["Issuer"];
            _audience = jwtSettings["Audience"];
            _expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"]);
        }

        public string GenerateJwtToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
                
            var claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.Username),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Создаем подпись и заголовок
            var signingCredentials = new SigningCredentials(
                _signingKey, 
                SecurityAlgorithms.HmacSha256
            );
            
            // Явно создаем объект заголовка и добавляем kid
            var header = new JwtHeader(signingCredentials);
            header["kid"] = "BGardenSigningKey";
            
            // Создаем полезную нагрузку
            var payload = new JwtPayload(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                issuedAt: DateTime.UtcNow
            );
            
            // Создаем токен с явным указанием заголовка и полезной нагрузки
            var secToken = new JwtSecurityToken(header, payload);
            var token = tokenHandler.WriteToken(secToken);
            
            // Убираем отладочную информацию в рабочей версии
            /* 
            #if DEBUG
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                var headerBase64 = parts[0];
                var headerBytes = Convert.FromBase64String(headerBase64.PadRight(headerBase64.Length + (4 - headerBase64.Length % 4) % 4, '='));
                var headerJson = Encoding.UTF8.GetString(headerBytes);
                Console.WriteLine($"Заголовок токена: {headerJson}");
            }
            #endif
            */
            
            return token;
        }

        public bool ValidateToken(string token, out JwtSecurityToken jwtSecurityToken)
        {
            jwtSecurityToken = null;
            
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = true
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                jwtSecurityToken = validatedToken as JwtSecurityToken;
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 