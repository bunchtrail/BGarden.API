using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FamilyController : ControllerBase
    {
        private readonly IFamilyService _familyService;

        public FamilyController(IFamilyService familyService)
        {
            _familyService = familyService;
        }

        // GET: api/Family
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FamilyDto>>> GetAll()
        {
            var families = await _familyService.GetAllFamiliesAsync();
            return Ok(families);
        }

        // GET: api/Family/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FamilyDto>> GetById(int id)
        {
            var family = await _familyService.GetFamilyByIdAsync(id);
            if (family == null)
                return NotFound();

            return Ok(family);
        }

        // GET: api/Family/name/Розовые
        [HttpGet("name/{name}")]
        public async Task<ActionResult<FamilyDto>> GetByName(string name)
        {
            var family = await _familyService.GetFamilyByNameAsync(name);
            if (family == null)
                return NotFound();

            return Ok(family);
        }

        // POST: api/Family
        [HttpPost]
        public async Task<ActionResult<FamilyDto>> Create([FromBody] FamilyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _familyService.CreateFamilyAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Family/5
        [HttpPut("{id}")]
        public async Task<ActionResult<FamilyDto>> Update(int id, [FromBody] FamilyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _familyService.UpdateFamilyAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Family/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _familyService.DeleteFamilyAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
} 