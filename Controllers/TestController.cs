using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // GET: api/Test/ping
        [HttpGet("ping")]
        public ActionResult<string> Ping()
        {
            return Ok("Пинг");
        }

        // GET: api/Test/pong
        [HttpGet("pong")]
        public ActionResult<string> Pong()
        {
            return Ok("Понг");
        }
    }
} 