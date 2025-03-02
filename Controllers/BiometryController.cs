using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BiometryController : ControllerBase
    {
        private readonly IBiometryService _biometryService;

        public BiometryController(IBiometryService biometryService)
        {
            _biometryService = biometryService;
        }

        // GET: api/Biometry
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BiometryDto>>> GetAll()
        {
            var biometries = await _biometryService.GetAllBiometriesAsync();
            return Ok(biometries);
        }

        // GET: api/Biometry/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BiometryDto>> GetById(int id)
        {
            var biometry = await _biometryService.GetBiometryByIdAsync(id);
            if (biometry == null)
                return NotFound();

            return Ok(biometry);
        }

        // GET: api/Biometry/specimen/5
        [HttpGet("specimen/{specimenId}")]
        public async Task<ActionResult<IEnumerable<BiometryDto>>> GetBySpecimenId(int specimenId)
        {
            var biometries = await _biometryService.GetBiometriesBySpecimenIdAsync(specimenId);
            return Ok(biometries);
        }

        // GET: api/Biometry/daterange?startDate=2023-04-01&endDate=2023-05-31
        [HttpGet("daterange")]
        public async Task<ActionResult<IEnumerable<BiometryDto>>> GetByDateRange(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var biometries = await _biometryService.GetBiometriesByDateRangeAsync(startDate, endDate);
            return Ok(biometries);
        }

        // GET: api/Biometry/specimen/5/latest?count=3
        [HttpGet("specimen/{specimenId}/latest")]
        public async Task<ActionResult<IEnumerable<BiometryDto>>> GetLatestForSpecimen(
            int specimenId, 
            [FromQuery] int count = 1)
        {
            var biometries = await _biometryService.GetLatestBiometriesForSpecimenAsync(specimenId, count);
            return Ok(biometries);
        }

        // POST: api/Biometry
        [HttpPost]
        public async Task<ActionResult<BiometryDto>> Create([FromBody] BiometryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _biometryService.CreateBiometryAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Biometry/5
        [HttpPut("{id}")]
        public async Task<ActionResult<BiometryDto>> Update(int id, [FromBody] BiometryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _biometryService.UpdateBiometryAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Biometry/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _biometryService.DeleteBiometryAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
} 