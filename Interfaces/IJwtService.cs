using System.IdentityModel.Tokens.Jwt;
using BGarden.Domain.Entities;

namespace BGarden.API.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с JWT токенами
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Генерирует JWT токен на основе данных пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого генерируется токен</param>
        /// <returns>JWT токен в виде строки</returns>
        string GenerateJwtToken(User user);
        
        /// <summary>
        /// Проверяет валидность JWT токена
        /// </summary>
        /// <param name="token">Токен для проверки</param>
        /// <param name="jwtSecurityToken">Выходной параметр с распарсенным токеном</param>
        /// <returns>true если токен валиден, иначе false</returns>
        bool ValidateToken(string token, out JwtSecurityToken jwtSecurityToken);
    }
} 