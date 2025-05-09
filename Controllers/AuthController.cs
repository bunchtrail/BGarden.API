using BGarden.Application.DTO;
using BGarden.Application.UseCases.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BGarden.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthUseCase _authUseCase;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthUseCase authUseCase, ILogger<AuthController> logger)
        {
            _authUseCase = authUseCase;
            _logger = logger;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<TokenDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                // Получаем IP-адрес клиента
                var ipAddress = GetIpAddress();
                
                // Обогащаем DTO данными из запроса
                registerDto.IpAddress = ipAddress;
                registerDto.UserAgent = Request.Headers["User-Agent"].ToString();
                
                var result = await _authUseCase.RegisterAsync(registerDto);
                
                // Установка refresh token в куки
                SetRefreshTokenCookie(result.RefreshToken);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя {Username}", registerDto.Username);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var ipAddress = GetIpAddress();
                loginDto.IpAddress = ipAddress;
                loginDto.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authUseCase.LoginAsync(loginDto);

                if (result == null)
                {
                    // 🔴 Логин/пароль неверны → 401
                    return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
                }

                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации пользователя");
                return BadRequest(new { message = "Ошибка авторизации" });
            }
        }
        /// <summary>
        /// Обновление токена доступа
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenDto>> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                var ipAddress = GetIpAddress();
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new { message = "Refresh token отсутствует" });
                }
                
                // Теперь передаем token и ipAddress напрямую вместо DTO
                var result = await _authUseCase.RefreshTokenAsync(refreshToken, ipAddress);
                
                // Устанавливаем новый refresh token в куки
                SetRefreshTokenCookie(result.RefreshToken);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена: {Message}", ex.Message);
                return BadRequest(new { message = "Невозможно обновить токен доступа" });
            }
        }

        /// <summary>
        /// Выход пользователя из системы
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                
                if (string.IsNullOrEmpty(refreshToken))
                    return BadRequest(new { message = "Токен обновления отсутствует" });
                
                await _authUseCase.LogoutAsync(refreshToken);
                
                // Удаление куки с refresh token
                Response.Cookies.Delete("refreshToken");
                
                return Ok(new { message = "Выход успешно выполнен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе из системы");
                return BadRequest(new { message = "Ошибка при выходе из системы" });
            }
        }

        /// <summary>
        /// Подтверждение кода двухфакторной аутентификации
        /// </summary>
        [HttpPost("verify-2fa")]
        public async Task<ActionResult<TokenDto>> VerifyTwoFactor([FromBody] VerifyTwoFactorDto verifyDto)
        {
            try
            {
                var ipAddress = GetIpAddress();
                
                var result = await _authUseCase.VerifyTwoFactorAsync(
                    verifyDto.Username, 
                    verifyDto.Code, 
                    verifyDto.RememberMe);
                
                // Установка refresh token в куки
                SetRefreshTokenCookie(result.RefreshToken);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке двухфакторной аутентификации");
                return BadRequest(new { message = "Неверный код подтверждения" });
            }
        }

        /// <summary>
        /// Настройка двухфакторной аутентификации
        /// </summary>
        [Authorize]
        [HttpGet("setup-2fa")]
        public async Task<ActionResult<TwoFactorSetupDto>> SetupTwoFactor()
        {
            try
            {
                var username = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();
                
                var setupData = await _authUseCase.SetupTwoFactorAsync(username);
                return Ok(setupData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при настройке двухфакторной аутентификации");
                return BadRequest(new { message = "Ошибка при настройке двухфакторной аутентификации" });
            }
        }

        /// <summary>
        /// Включение двухфакторной аутентификации
        /// </summary>
        [Authorize]
        [HttpPost("enable-2fa")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorCodeDto verifyDto)
        {
            try
            {
                var username = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();
                
                var result = await _authUseCase.EnableTwoFactorAsync(username, verifyDto.Code);
                
                if (!result)
                    return BadRequest(new { message = "Неверный код подтверждения" });
                
                return Ok(new { message = "Двухфакторная аутентификация успешно включена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при включении двухфакторной аутентификации");
                return BadRequest(new { message = "Ошибка при включении двухфакторной аутентификации" });
            }
        }

        /// <summary>
        /// Отключение двухфакторной аутентификации
        /// </summary>
        [Authorize]
        [HttpPost("disable-2fa")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] VerifyTwoFactorCodeDto verifyDto)
        {
            try
            {
                var username = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();
                
                var result = await _authUseCase.DisableTwoFactorAsync(username, verifyDto.Code);
                
                if (!result)
                    return BadRequest(new { message = "Неверный код подтверждения" });
                
                return Ok(new { message = "Двухфакторная аутентификация успешно отключена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отключении двухфакторной аутентификации");
                return BadRequest(new { message = "Ошибка при отключении двухфакторной аутентификации" });
            }
        }

        /// <summary>
        /// Получение истории аутентификации
        /// </summary>
        [Authorize]
        [HttpGet("auth-history")]
        public async Task<ActionResult<IEnumerable<AuthLogDto>>> GetAuthHistory()
        {
            try
            {
                var username = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();
                
                var history = await _authUseCase.GetAuthHistoryAsync(username);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении истории аутентификации");
                return BadRequest(new { message = "Ошибка при получении истории аутентификации" });
            }
        }

        /// <summary>
        /// Разблокировка заблокированного пользователя (только для администраторов)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("unlock-user/{username}")]
        public async Task<IActionResult> UnlockUser(string username)
        {
            try
            {
                var result = await _authUseCase.UnlockUserAsync(username);
                
                if (!result)
                    return NotFound(new { message = "Пользователь не найден" });
                
                return Ok(new { message = "Пользователь успешно разблокирован" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при разблокировке пользователя");
                return BadRequest(new { message = "Ошибка при разблокировке пользователя" });
            }
        }

        #region Helper Methods
        
        private string GetIpAddress()
        {
            // Получаем IP адрес из заголовка X-Forwarded-For (если запрос идет через прокси)
            // или из соединения клиента
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0.0.0.0";
        }
        
        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Secure = true // в продакшн окружении установить true
            };
            
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
        
        #endregion
    }
    
    // Вспомогательные DTO
    public class VerifyTwoFactorDto
    {
        public string Username { get; set; }
        public string Code { get; set; }
        public bool RememberMe { get; set; }
    }
    
    public class VerifyTwoFactorCodeDto
    {
        public string Code { get; set; }
    }
} 