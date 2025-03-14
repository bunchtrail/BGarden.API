using BGarden.Application.DTO;
using BGarden.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BGarden.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
                // Убираем избыточные логи
                // Console.WriteLine($"User.Identity.Name: {username}");
                // Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
                // Console.WriteLine($"AuthenticationType: {User.Identity?.AuthenticationType}");

                // Проверим все клеймы в токене - убираем логирование всех клеймов
                // if (User.Claims.Any())
                // {
                //     Console.WriteLine("Доступные claims в токене:");
                //     foreach (var claim in User.Claims)
                //     {
                //         Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                //     }
                // }
                // else
                // {
                //     Console.WriteLine("Клеймы отсутствуют в токене");
                // }

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
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.AuthenticateAsync(loginDto);
            if (user == null)
                return Unauthorized();

            return Ok(user);
        }
    }
} 