using Application.DTO;
using Application.Interfaces;
using BGarden.API.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BGarden.API.Controllers
{
    [ApiController]
    [Route("api/v1/specimen-images")]
    public class SpecimenImageController : ControllerBase
    {
        private readonly ISpecimenImageService _specimenImageService;
        private readonly ILogger<SpecimenImageController> _logger;

        public SpecimenImageController(
            ISpecimenImageService specimenImageService,
            ILogger<SpecimenImageController> logger)
        {
            _specimenImageService = specimenImageService;
            _logger = logger;
        }

        /// <summary>
        /// Получить все изображения для указанного образца
        /// </summary>
        /// <param name="specimenId">Идентификатор образца</param>
        /// <param name="includeImageData">Включать ли данные изображения</param>
        [HttpGet("by-specimen/{specimenId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SpecimenImageDto>>> GetBySpecimenId(
            int specimenId, 
            [FromQuery] bool includeImageData = false)
        {
            var images = await _specimenImageService.GetBySpecimenIdAsync(specimenId, includeImageData);
            return Ok(images);
        }

        /// <summary>
        /// Получить основное изображение для указанного образца
        /// </summary>
        /// <param name="specimenId">Идентификатор образца</param>
        [HttpGet("by-specimen/{specimenId}/main")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> GetMainImageBySpecimenId(int specimenId)
        {
            var image = await _specimenImageService.GetMainImageBySpecimenIdAsync(specimenId);
            if (image == null)
            {
                throw new ResourceNotFoundException($"Основное изображение для образца с ID {specimenId} не найдено");
            }
            return Ok(image);
        }

        /// <summary>
        /// Получить изображение по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> GetById(int id)
        {
            var image = await _specimenImageService.GetByIdAsync(id);
            if (image == null)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            return Ok(image);
        }

        /// <summary>
        /// Добавить новое изображение
        /// </summary>
        /// <param name="dto">DTO создания изображения</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> Add([FromBody] CreateSpecimenImageDto dto)
        {
            var result = await _specimenImageService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Обновить существующее изображение
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        /// <param name="dto">DTO обновления изображения</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> Update(int id, [FromBody] UpdateSpecimenImageDto dto)
        {
            var result = await _specimenImageService.UpdateAsync(id, dto);
            if (result == null)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            return Ok(result);
        }

        /// <summary>
        /// Удалить изображение по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _specimenImageService.DeleteAsync(id);
            if (!result)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            return NoContent();
        }

        /// <summary>
        /// Установить указанное изображение как основное для образца
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        [HttpPatch("{id}/set-as-main")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetAsMain(int id)
        {
            var result = await _specimenImageService.SetAsMainAsync(id);
            if (!result)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            return NoContent();
        }
    }
} 