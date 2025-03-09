using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BGarden.Application.DTO;
using BGarden.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BGarden.API.Controllers
{
    /// <summary>
    /// Контроллер для работы с картами ботанического сада
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;
        private readonly ILogger<MapController> _logger;

        public MapController(IMapService mapService, ILogger<MapController> logger)
        {
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получить все карты
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MapDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MapDto>>> GetAllMaps()
        {
            _logger.LogInformation("Запрос на получение всех карт");
            var maps = await _mapService.GetAllMapsAsync();
            return Ok(maps);
        }

        /// <summary>
        /// Получить только активные карты
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(typeof(IEnumerable<MapDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MapDto>>> GetActiveMaps()
        {
            _logger.LogInformation("Запрос на получение активных карт");
            var maps = await _mapService.GetActiveMapsAsync();
            return Ok(maps);
        }

        /// <summary>
        /// Получить карту по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор карты</param>
        /// <returns>Информация о карте</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MapDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MapDto>> GetMapById(int id)
        {
            _logger.LogInformation("Запрос на получение карты с ID: {Id}", id);
            var map = await _mapService.GetMapByIdAsync(id);
            
            if (map == null)
            {
                _logger.LogWarning("Карта с ID: {Id} не найдена", id);
                return NotFound();
            }
            
            return Ok(map);
        }

        /// <summary>
        /// Получить карту вместе с растениями
        /// </summary>
        /// <param name="id">Идентификатор карты</param>
        /// <returns>Информация о карте со списком размещенных на ней растений</returns>
        [HttpGet("{id}/specimens")]
        [ProducesResponseType(typeof(MapDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MapDto>> GetMapWithSpecimens(int id)
        {
            _logger.LogInformation("Запрос на получение карты с растениями для ID: {Id}", id);
            var map = await _mapService.GetMapWithSpecimensAsync(id);
            
            if (map == null)
            {
                _logger.LogWarning("Карта с ID: {Id} не найдена", id);
                return NotFound();
            }
            
            return Ok(map);
        }

        /// <summary>
        /// Создать новую карту
        /// </summary>
        /// <param name="mapDto">Данные новой карты</param>
        /// <returns>Созданная карта с присвоенным идентификатором</returns>
        [HttpPost]
        [ProducesResponseType(typeof(MapDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MapDto>> CreateMap([FromBody] CreateMapDto mapDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                _logger.LogInformation("Запрос на создание новой карты: {MapName}", mapDto.Name);
                var createdMap = await _mapService.CreateMapAsync(mapDto);
                return CreatedAtAction(nameof(GetMapById), new { id = createdMap.Id }, createdMap);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка при создании карты");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Обновить существующую карту
        /// </summary>
        /// <param name="id">Идентификатор карты</param>
        /// <param name="mapDto">Данные для обновления</param>
        /// <returns>Обновленная информация о карте</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MapDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MapDto>> UpdateMap(int id, [FromBody] UpdateMapDto mapDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                _logger.LogInformation("Запрос на обновление карты с ID: {Id}", id);
                var updatedMap = await _mapService.UpdateMapAsync(id, mapDto);
                
                if (updatedMap == null)
                {
                    _logger.LogWarning("Карта с ID: {Id} не найдена при обновлении", id);
                    return NotFound();
                }
                
                return Ok(updatedMap);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка при обновлении карты с ID: {Id}", id);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Загрузить файл карты
        /// </summary>
        /// <param name="id">Идентификатор карты</param>
        /// <param name="file">Файл карты (изображение)</param>
        /// <returns>Обновленная информация о карте</returns>
        [HttpPost("{id}/upload")]
        [ProducesResponseType(typeof(MapDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MapDto>> UploadMapFile(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не выбран или пустой");
            }
            
            try
            {
                _logger.LogInformation("Запрос на загрузку файла для карты с ID: {Id}", id);
                var updatedMap = await _mapService.UploadMapFileAsync(id, file);
                
                if (updatedMap == null)
                {
                    _logger.LogWarning("Карта с ID: {Id} не найдена при загрузке файла", id);
                    return NotFound();
                }
                
                return Ok(updatedMap);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка при загрузке файла для карты с ID: {Id}", id);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Удалить карту
        /// </summary>
        /// <param name="id">Идентификатор карты</param>
        /// <returns>Результат операции удаления</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMap(int id)
        {
            try
            {
                _logger.LogInformation("Запрос на удаление карты с ID: {Id}", id);
                var map = await _mapService.GetMapByIdAsync(id);
                
                if (map == null)
                {
                    _logger.LogWarning("Карта с ID: {Id} не найдена при удалении", id);
                    return NotFound();
                }
                
                await _mapService.DeleteMapAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка при удалении карты с ID: {Id}", id);
                return BadRequest(ex.Message);
            }
        }
    }
} 