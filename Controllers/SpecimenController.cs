using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BGarden.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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

        // GET: api/Specimen/all
        // Получение всех образцов независимо от сектора
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetAll()
        {
            var specimens = await _specimenService.GetAllSpecimensAsync();
            if (specimens == null || !specimens.Any())
                return NotFound();

            return Ok(specimens);
        }

        // GET: api/Specimen/filter?name=value&familyId=1&regionId=2
        // Получение образцов с фильтрацией по параметрам
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetFiltered(
            [FromQuery] string? name = null, 
            [FromQuery] int? familyId = null, 
            [FromQuery] int? regionId = null)
        {
            var specimens = await _specimenService.GetFilteredSpecimensAsync(name, familyId, regionId);
            if (specimens == null || !specimens.Any())
                return NotFound();

            return Ok(specimens);
        }

        // GET: api/Specimen/{id}
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
            // Возвращаем 201 Created с данными созданного образца
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Specimen/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<SpecimenDto>> Update(int id, [FromBody] SpecimenDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id в URL не соответствует Id в теле запроса");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _specimenService.UpdateSpecimenAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Specimen/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _specimenService.DeleteSpecimenAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
