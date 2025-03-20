using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegionController : ControllerBase
    {
        private readonly IRegionService _regionService;

        public RegionController(IRegionService regionService)
        {
            _regionService = regionService;
        }

        // GET: api/Region
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RegionDto>>> GetAll()
        {
            var regions = await _regionService.GetAllRegionsAsync();
            return Ok(regions);
        }

        // GET: api/Region/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RegionDto>> GetById(int id)
        {
            var region = await _regionService.GetRegionByIdAsync(id);
            if (region == null)
                return NotFound();

            return Ok(region);
        }

        // GET: api/Region/5/specimens
        [HttpGet("{id}/specimens")]
        public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetSpecimensInRegion(int id)
        {
            var specimens = await _regionService.GetSpecimensInRegionAsync(id);
            return Ok(specimens);
        }

        // POST: api/Region
        [HttpPost]
        public async Task<ActionResult<RegionDto>> Create([FromBody] RegionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _regionService.CreateRegionAsync(dto);
            // Возвращаем 201 Created + ссылку на новый ресурс
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Region/5
        [HttpPut("{id}")]
        public async Task<ActionResult<RegionDto>> Update(int id, [FromBody] RegionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _regionService.UpdateRegionAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Region/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _regionService.DeleteRegionAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // PUT: api/Region/{id}/increment-specimens
        [HttpPut("{id}/increment-specimens")]
        public async Task<IActionResult> IncrementSpecimensCount(int id)
        {
            try
            {
                if (!await _regionService.UpdateSpecimensCountAsync(id, true))
                    return NotFound();
                    
                return NoContent();
            }
            catch (Exception error)
            {
                // Можно использовать логирование через встроенные средства или другой подход
                // logger.LogError("Ошибка при увеличении количества образцов в области:", error);
                return StatusCode(500, "Ошибка сервера при обновлении количества образцов.");
            }
        }

        // PUT: api/Region/{id}/decrement-specimens
        [HttpPut("{id}/decrement-specimens")]
        public async Task<IActionResult> DecrementSpecimensCount(int id)
        {
            try
            {
                if (!await _regionService.UpdateSpecimensCountAsync(id, false))
                    return NotFound();
                    
                return NoContent();
            }
            catch (Exception error)
            {
                // logger.LogError("Ошибка при уменьшении количества образцов в области:", error);
                return StatusCode(500, "Ошибка сервера при обновлении количества образцов.");
            }
        }
    }
} 