using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhenologyController : ControllerBase
    {
        private readonly IPhenologyService _phenologyService;

        public PhenologyController(IPhenologyService phenologyService)
        {
            _phenologyService = phenologyService;
        }

        // GET: api/Phenology
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhenologyDto>>> GetAll()
        {
            var phenologies = await _phenologyService.GetAllPhenologiesAsync();
            return Ok(phenologies);
        }

        // GET: api/Phenology/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PhenologyDto>> GetById(int id)
        {
            var phenology = await _phenologyService.GetPhenologyByIdAsync(id);
            if (phenology == null)
                return NotFound();

            return Ok(phenology);
        }

        // GET: api/Phenology/specimen/5
        [HttpGet("specimen/{specimenId}")]
        public async Task<ActionResult<IEnumerable<PhenologyDto>>> GetBySpecimenId(int specimenId)
        {
            var phenologies = await _phenologyService.GetPhenologiesBySpecimenIdAsync(specimenId);
            return Ok(phenologies);
        }

        // GET: api/Phenology/year/2023
        [HttpGet("year/{year}")]
        public async Task<ActionResult<IEnumerable<PhenologyDto>>> GetByYear(int year)
        {
            var phenologies = await _phenologyService.GetPhenologiesByYearAsync(year);
            return Ok(phenologies);
        }

        // GET: api/Phenology/specimen/5/year/2023
        [HttpGet("specimen/{specimenId}/year/{year}")]
        public async Task<ActionResult<PhenologyDto>> GetBySpecimenAndYear(int specimenId, int year)
        {
            var phenology = await _phenologyService.GetPhenologyBySpecimenAndYearAsync(specimenId, year);
            if (phenology == null)
                return NotFound();

            return Ok(phenology);
        }

        // GET: api/Phenology/flowering?startDate=2023-04-01&endDate=2023-05-31
        [HttpGet("flowering")]
        public async Task<ActionResult<IEnumerable<PhenologyDto>>> GetByFloweringPeriod([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var phenologies = await _phenologyService.GetPhenologiesByFloweringPeriodAsync(startDate, endDate);
            return Ok(phenologies);
        }

        // POST: api/Phenology
        [HttpPost]
        public async Task<ActionResult<PhenologyDto>> Create([FromBody] PhenologyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _phenologyService.CreatePhenologyAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Phenology/5
        [HttpPut("{id}")]
        public async Task<ActionResult<PhenologyDto>> Update(int id, [FromBody] PhenologyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _phenologyService.UpdatePhenologyAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Phenology/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _phenologyService.DeletePhenologyAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
} 