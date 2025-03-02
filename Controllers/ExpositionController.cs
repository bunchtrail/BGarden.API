using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpositionController : ControllerBase
    {
        private readonly IExpositionService _expositionService;

        public ExpositionController(IExpositionService expositionService)
        {
            _expositionService = expositionService;
        }

        // GET: api/Exposition
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpositionDto>>> GetAll()
        {
            var expositions = await _expositionService.GetAllExpositionsAsync();
            return Ok(expositions);
        }

        // GET: api/Exposition/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpositionDto>> GetById(int id)
        {
            var exposition = await _expositionService.GetExpositionByIdAsync(id);
            if (exposition == null)
                return NotFound();

            return Ok(exposition);
        }

        // GET: api/Exposition/name/Альпинарий
        [HttpGet("name/{name}")]
        public async Task<ActionResult<ExpositionDto>> GetByName(string name)
        {
            var exposition = await _expositionService.GetExpositionByNameAsync(name);
            if (exposition == null)
                return NotFound();

            return Ok(exposition);
        }

        // POST: api/Exposition
        [HttpPost]
        public async Task<ActionResult<ExpositionDto>> Create([FromBody] ExpositionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _expositionService.CreateExpositionAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Exposition/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ExpositionDto>> Update(int id, [FromBody] ExpositionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _expositionService.UpdateExpositionAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Exposition/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _expositionService.DeleteExpositionAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
} 