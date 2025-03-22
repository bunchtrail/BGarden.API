using BGarden.Application.DTO;
using BGarden.Application.Interfaces;
using BGarden.Application.UseCases.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace BGarden.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthUseCase _authUseCase;

        public UserController(IUserService userService, 
                             IAuthUseCase authUseCase)
        {
            _userService = userService;
            _authUseCase = authUseCase;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var username = User.Identity?.Name;
                

                // Находим claim с именем
                var nameClaim = User.Claims.FirstOrDefault(c => c.Type == "name");
                if (nameClaim != null)
                {
                    // Console.WriteLine($"Name из claim: {nameClaim.Value}");
                    username = nameClaim.Value;
                }

                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // GET: api/User/username/{username}
        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserDto>> GetByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedUser = await _userService.UpdateUserAsync(id, updateUserDto);
            return Ok(updatedUser);
        }

        // POST: api/User/5/changepassword
        [HttpPost("{userId}/changepassword")]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _userService.ChangePasswordAsync(userId, changePasswordDto);
            return NoContent();
        }

        // POST: api/User/5/deactivate
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            await _userService.DeactivateUserAsync(id);
            return NoContent();
        }

        // POST: api/User/5/activate
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            await _userService.ActivateUserAsync(id);
            return NoContent();
        }

        // POST: api/User/login
        [HttpPost("login")]
        public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Получаем IP-адрес клиента
                var ipAddress = GetIpAddress();
                
                // Обогащаем DTO данными из запроса
                loginDto.IpAddress = ipAddress;
                loginDto.UserAgent = Request.Headers["User-Agent"].ToString();
                
                var result = await _authUseCase.LoginAsync(loginDto);
                
                if (result == null)
                {
                    // Если результат null, это означает, что требуется двухфакторная аутентификация
                    return Ok(new { requiresTwoFactor = true, username = loginDto.Username });
                }
                
                // Установка refresh token в куки
                SetRefreshTokenCookie(result.RefreshToken);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при авторизации пользователя: {ex.Message}");
                return BadRequest(new { message = "Ошибка авторизации" });
            }
        }

        // НОВЫЙ МЕТОД: Проверка валидности токена
        [HttpGet("validate")]
        public ActionResult<TokenValidationResponse> ValidateToken()
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                if (!User.Identity.IsAuthenticated)
                {
                    return Ok(new TokenValidationResponse { Valid = false });
                }

                // Получаем ID пользователя из токена
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                int userId = 0;
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    return Ok(new TokenValidationResponse { Valid = true, UserId = userId });
                }
                
                return Ok(new TokenValidationResponse { Valid = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке токена: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // НОВЫЙ МЕТОД: Обновление токена
        [HttpPost("refresh")]
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
                
                var result = await _authUseCase.RefreshTokenAsync(refreshToken, ipAddress);
                
                // Устанавливаем новый refresh token в куки
                SetRefreshTokenCookie(result.RefreshToken);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении токена: {ex.Message}");
                return BadRequest(new { message = "Невозможно обновить токен доступа" });
            }
        }

        // Вспомогательные методы для работы с IP-адресом и куки
        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
                
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }
        
        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Secure = true
            };
            
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
    
    // Класс для ответа при проверке валидности токена
    public class TokenValidationResponse
    {
        public bool Valid { get; set; }
        public int? UserId { get; set; }
    }
} 