using System.IdentityModel.Tokens.Jwt;
using BGarden.Domain.Entities;
using BGarden.Domain.Interfaces;
using BGarden.API.Interfaces;

namespace BGarden.API.Adapters
{
    /// <summary>
    /// Адаптер для совместимости IJwtService из API и Domain
    /// </summary>
    public class JwtServiceAdapter : BGarden.Domain.Interfaces.IJwtService
    {
        private readonly BGarden.API.Interfaces.IJwtService _jwtService;

        public JwtServiceAdapter(BGarden.API.Interfaces.IJwtService jwtService)
        {
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        }

        public string GenerateJwtToken(User user)
        {
            return _jwtService.GenerateJwtToken(user);
        }

        public bool ValidateToken(string token, out JwtSecurityToken jwtSecurityToken)
        {
            return _jwtService.ValidateToken(token, out jwtSecurityToken);
        }
    }
} 