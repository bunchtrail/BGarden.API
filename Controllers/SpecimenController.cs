using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BGarden.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Application.UseCases;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using BGarden.Domain.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecimenController : ControllerBase
    {
        private readonly ISpecimenService _specimenService;
        private readonly CreateSpecimenWithImagesUseCase _createSpecimenWithImagesUseCase;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SpecimenController> _logger;

        public SpecimenController(
            ISpecimenService specimenService,
            CreateSpecimenWithImagesUseCase createSpecimenWithImagesUseCase,
            IUnitOfWork unitOfWork,
            ILogger<SpecimenController> logger)
        {
            _specimenService = specimenService ?? throw new ArgumentNullException(nameof(specimenService));
            _createSpecimenWithImagesUseCase = createSpecimenWithImagesUseCase ?? throw new ArgumentNullException(nameof(createSpecimenWithImagesUseCase));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        [Authorize]
        public async Task<ActionResult<SpecimenDto>> Create([FromBody] SpecimenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdSpecimen = await _specimenService.CreateSpecimenAsync(dto);
                await _unitOfWork.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = createdSpecimen.Id }, createdSpecimen);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating specimen");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // PUT: api/Specimen/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] SpecimenDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedSpecimen = await _specimenService.UpdateSpecimenAsync(id, dto);
                if (updatedSpecimen == null)
                    return NotFound();
                
                await _unitOfWork.SaveChangesAsync();
                return Ok(updatedSpecimen);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating specimen with id {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // DELETE: api/Specimen/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _specimenService.DeleteSpecimenAsync(id);
            if (!result)
                return NotFound();

            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Specimen/with-images
        [HttpPost("with-images")]
        [Authorize]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> CreateWithImages(
            [FromForm] SpecimenDto dto,
            [FromForm] IFormFileCollection images)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (images == null || !images.Any())
                    return BadRequest(new { message = "Необходимо загрузить хотя бы одно изображение" });

                var (createdSpecimen, imageIds) = await _createSpecimenWithImagesUseCase.ExecuteAsync(dto, images.ToList());

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdSpecimen.Id },
                    new { specimen = createdSpecimen, imageIds = imageIds });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (IOException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Ошибка при работе с файлами: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating specimen with images.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }

        /// <summary>
        /// Обновляет географические координаты образца
        /// </summary>
        /// <remarks>
        /// Устаревший метод, рекомендуется использовать PUT /location с указанием типа координат
        /// </remarks>
        [HttpPut("{id}/geo-location")]
        public async Task<ActionResult<SpecimenDto>> UpdateGeoLocation(int id, [FromBody] GeoLocationDto locationDto)
        {
            var result = await _specimenService.UpdateSpecimenLocationAsync(id, locationDto.Latitude, locationDto.Longitude);
            
            if (result == null)
                return NotFound($"Образец с ID {id} не найден");
                
            return Ok(result);
        }
        
        /// <summary>
        /// Обновляет местоположение образца с учетом типа координат
        /// </summary>
        [HttpPut("{id}/location")]
        public async Task<ActionResult<SpecimenDto>> UpdateLocation(int id, [FromBody] LocationUpdateDto locationDto)
        {
            // Создаем DTO с минимальным набором свойств для обновления местоположения
            var updateDto = new SpecimenDto
            {
                LocationType = locationDto.LocationType,
                Latitude = locationDto.Latitude,
                Longitude = locationDto.Longitude,
                MapId = locationDto.MapId,
                MapX = locationDto.MapX,
                MapY = locationDto.MapY
            };
            
            var result = await _specimenService.UpdateSpecimenLocationAsync(id, updateDto);
            
            if (result == null)
                return NotFound($"Образец с ID {id} не найден");
                
            return Ok(result);
        }
    }
    
    /// <summary>
    /// DTO для обновления географических координат (устаревший метод)
    /// </summary>
    public class GeoLocationDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
    
    /// <summary>
    /// DTO для обновления местоположения с учетом типа координат
    /// </summary>
    public class LocationUpdateDto
    {
        /// <summary>
        /// Тип используемых координат
        /// </summary>
        public BGarden.Domain.Enums.LocationType LocationType { get; set; }
        
        /// <summary>
        /// Географическая широта (для LocationType.Geographic)
        /// </summary>
        public decimal? Latitude { get; set; }
        
        /// <summary>
        /// Географическая долгота (для LocationType.Geographic)
        /// </summary>
        public decimal? Longitude { get; set; }
        
        /// <summary>
        /// Идентификатор карты (для LocationType.SchematicMap)
        /// </summary>
        public int? MapId { get; set; }
        
        /// <summary>
        /// Координата X на карте (для LocationType.SchematicMap)
        /// </summary>
        public decimal? MapX { get; set; }
        
        /// <summary>
        /// Координата Y на карте (для LocationType.SchematicMap)
        /// </summary>
        public decimal? MapY { get; set; }
    }
}
