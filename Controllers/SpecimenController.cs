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

        // POST: api/Specimen/with-images
        [HttpPost("with-images")]
        [RequestSizeLimit(50 * 1024 * 1024)] // Ограничение размера запроса в 50 МБ
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)] // Ограничение размера формы в 50 МБ
        public async Task<ActionResult<SpecimenDto>> CreateWithImages(
            [FromForm] SpecimenDto dto,
            [FromForm] IFormFileCollection images)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (images == null || images.Count == 0)
                    return BadRequest("Необходимо загрузить хотя бы одно изображение");

                // Создаем список DTO изображений
                var imageDtos = new List<CreateSpecimenImageBinaryDto>();

                foreach (var image in images)
                {
                    if (image.Length == 0)
                        continue;

                    // Читаем бинарные данные из файла
                    using var memoryStream = new MemoryStream();
                    await image.CopyToAsync(memoryStream);

                    // Создаем DTO для изображения
                    var imageDto = new CreateSpecimenImageBinaryDto
                    {
                        ImageData = memoryStream.ToArray(),
                        ContentType = image.ContentType,
                        Description = image.FileName,
                        IsMain = imageDtos.Count == 0 // Первое изображение считаем основным
                    };

                    imageDtos.Add(imageDto);
                }

                // Получаем UseCase из DI
                var createSpecimenWithImagesUseCase = HttpContext.RequestServices
                    .GetRequiredService<CreateSpecimenWithImagesUseCase>();

                // Выполняем UseCase
                var (createdSpecimen, imageIds) = await createSpecimenWithImagesUseCase
                    .ExecuteAsync(dto, imageDtos);

                // Возвращаем результат
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdSpecimen.Id },
                    new { specimen = createdSpecimen, imageIds });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
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
