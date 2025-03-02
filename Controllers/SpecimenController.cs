using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecimenController : ControllerBase
    {
        private readonly ISpecimenService _specimenService;

        public SpecimenController(ISpecimenService specimenService)
        {
            _specimenService = specimenService;
        }

        // GET: api/Specimen
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetAll()
        {
            var specimens = await _specimenService.GetAllSpecimensAsync();
            return Ok(specimens);
        }

        // GET: api/Specimen/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SpecimenDto>> GetById(int id)
        {
            var specimen = await _specimenService.GetSpecimenByIdAsync(id);
            if (specimen == null)
                return NotFound();

            return Ok(specimen);
        }

        // POST: api/Specimen
        [HttpPost]
        public async Task<ActionResult<SpecimenDto>> Create([FromBody] SpecimenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _specimenService.CreateSpecimenAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Specimen/5
        [HttpPut("{id}")]
        public async Task<ActionResult<SpecimenDto>> Update(int id, [FromBody] SpecimenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _specimenService.UpdateSpecimenAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Specimen/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _specimenService.DeleteSpecimenAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
} 