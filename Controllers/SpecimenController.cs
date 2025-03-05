using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BGarden.Domain.Enums;

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

        // GET: api/Specimen/sector/{sectorType}
        // Пример: api/Specimen/sector/0 - для дендрологии,
        //          api/Specimen/sector/1 - для флоры,
        //          api/Specimen/sector/2 - для цветоводства.
        [HttpGet("sector/{sectorType}")]
        public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetBySectorType(SectorType sectorType)
        {
            var specimens = await _specimenService.GetSpecimensBySectorTypeAsync(sectorType);
            if (specimens == null || !specimens.Any())
                return NotFound();

            return Ok(specimens);
        }

        // POST: api/Specimen
        [HttpPost]
        public async Task<ActionResult<SpecimenDto>> Create([FromBody] SpecimenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _specimenService.CreateSpecimenAsync(dto);
            // Возвращаем 201 Created с данными созданного образца
            return CreatedAtAction(nameof(Create), new { id = created.Id }, created);
        }
    }
}
