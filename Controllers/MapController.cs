using Application.Interfaces.Map;
using Microsoft.AspNetCore.Mvc;
using BGarden.DB.Application.DTO;
using BGarden.DB.Domain.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;

        public MapController(IMapService mapService)
        {
            _mapService = mapService;
        }

        #region PlantMarkers

        // GET: api/Map/markers
        [HttpGet("markers")]
        public async Task<ActionResult<IEnumerable<MapMarkerDto>>> GetAllMarkers()
        {
            var markers = await _mapService.GetAllMarkersAsync();
            return Ok(markers);
        }

        // GET: api/Map/markers/{id}
        [HttpGet("markers/{id}")]
        public async Task<ActionResult<MapMarkerDto>> GetMarkerById(int id)
        {
            var marker = await _mapService.GetMarkerByIdAsync(id);
            if (marker == null)
                return NotFound();

            return Ok(marker);
        }

        // GET: api/Map/markers/specimen/{specimenId}
        [HttpGet("markers/specimen/{specimenId}")]
        public async Task<ActionResult<IEnumerable<MapMarkerDto>>> GetMarkersBySpecimenId(int specimenId)
        {
            var markers = await _mapService.GetMarkersBySpecimenIdAsync(specimenId);
            return Ok(markers);
        }

        // POST: api/Map/markers
        [HttpPost("markers")]
        public async Task<ActionResult<MapMarkerDto>> CreateMarker([FromBody] CreateMapMarkerDto markerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _mapService.CreateMarkerAsync(markerDto);
            return CreatedAtAction(nameof(GetMarkerById), new { id = created.Id }, created);
        }

        // PUT: api/Map/markers
        [HttpPut("markers")]
        public async Task<ActionResult<MapMarkerDto>> UpdateMarker([FromBody] UpdateMapMarkerDto markerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _mapService.UpdateMarkerAsync(markerDto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Map/markers/{id}
        [HttpDelete("markers/{id}")]
        public async Task<ActionResult> DeleteMarker(int id)
        {
            var result = await _mapService.DeleteMarkerAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        // GET: api/Map/markers/nearby
        [HttpGet("markers/nearby")]
        public async Task<ActionResult<IEnumerable<MapMarkerDto>>> FindNearbyMarkers(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusInMeters = 100)
        {
            var markers = await _mapService.FindNearbyMarkersAsync(latitude, longitude, radiusInMeters);
            return Ok(markers);
        }

        #endregion

        #region MapAreas

        // GET: api/Map/areas
        [HttpGet("areas")]
        public async Task<ActionResult<IEnumerable<MapAreaDto>>> GetAllAreas()
        {
            var areas = await _mapService.GetAllAreasAsync();
            return Ok(areas);
        }

        // GET: api/Map/areas/{id}
        [HttpGet("areas/{id}")]
        public async Task<ActionResult<MapAreaDto>> GetAreaById(int id)
        {
            var area = await _mapService.GetAreaByIdAsync(id);
            if (area == null)
                return NotFound();

            return Ok(area);
        }

        // POST: api/Map/areas
        [HttpPost("areas")]
        public async Task<ActionResult<MapAreaDto>> CreateArea([FromBody] CreateMapAreaDto areaDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _mapService.CreateAreaAsync(areaDto);
            return CreatedAtAction(nameof(GetAreaById), new { id = created.Id }, created);
        }

        // PUT: api/Map/areas
        [HttpPut("areas")]
        public async Task<ActionResult<MapAreaDto>> UpdateArea([FromBody] UpdateMapAreaDto areaDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _mapService.UpdateAreaAsync(areaDto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Map/areas/{id}
        [HttpDelete("areas/{id}")]
        public async Task<ActionResult> DeleteArea(int id)
        {
            var result = await _mapService.DeleteAreaAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        #endregion

        #region MapOptions

        // GET: api/Map/options
        [HttpGet("options")]
        public async Task<ActionResult<IEnumerable<MapOptionsDto>>> GetAllOptions()
        {
            var options = await _mapService.GetAllOptionsAsync();
            return Ok(options);
        }

        // GET: api/Map/options/{id}
        [HttpGet("options/{id}")]
        public async Task<ActionResult<MapOptionsDto>> GetOptionsById(int id)
        {
            var options = await _mapService.GetOptionsByIdAsync(id);
            if (options == null)
                return NotFound();

            return Ok(options);
        }

        // GET: api/Map/options/default
        [HttpGet("options/default")]
        public async Task<ActionResult<MapOptionsDto>> GetDefaultOptions()
        {
            try
            {
                var options = await _mapService.GetDefaultOptionsAsync();
                return Ok(options);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST: api/Map/options
        [HttpPost("options")]
        public async Task<ActionResult<MapOptionsDto>> CreateOptions([FromBody] CreateMapOptionsDto optionsDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _mapService.CreateOptionsAsync(optionsDto);
            return CreatedAtAction(nameof(GetOptionsById), new { id = created.Id }, created);
        }

        // PUT: api/Map/options
        [HttpPut("options")]
        public async Task<ActionResult<MapOptionsDto>> UpdateOptions([FromBody] UpdateMapOptionsDto optionsDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _mapService.UpdateOptionsAsync(optionsDto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/Map/options/{id}
        [HttpDelete("options/{id}")]
        public async Task<ActionResult> DeleteOptions(int id)
        {
            try
            {
                var result = await _mapService.DeleteOptionsAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region CustomMapScheme

        // POST: api/Map/customscheme
        [HttpPost("customscheme")]
        public async Task<ActionResult<bool>> UploadCustomMapScheme(IFormFile mapScheme)
        {
            if (mapScheme == null || mapScheme.Length == 0)
                return BadRequest("No file uploaded");

            // здесь должен быть вызов сервиса полезной нагрузки для загрузки пользовательской схемы
            // var result = await _mapService.UploadCustomMapSchemeAsync(mapScheme);

            // временная заглушка
            return Ok(true);
        }

        #endregion
    }
}