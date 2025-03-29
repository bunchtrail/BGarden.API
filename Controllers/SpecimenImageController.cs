using Application.DTO;
using Application.DTO.Exceptions;
using Application.Interfaces;
using BGarden.API.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using ResourceNotFoundException = BGarden.API.Exceptions.ResourceNotFoundException;

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
            // Получаем изображение для удаления
            var imageToDelete = await _specimenImageService.GetByIdAsync(id);
            if (imageToDelete == null)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }

            // Удаляем изображение
            var result = await _specimenImageService.DeleteAsync(id);
            if (!result)
            {
                throw new ResourceNotFoundException($"Изображение с ID {id} не найдено");
            }

            // Если удалённое изображение было основным, пытаемся установить новое
            if (imageToDelete.IsMain)
            {
                var remainingImages = await _specimenImageService.GetBySpecimenIdAsync(imageToDelete.SpecimenId, includeImageData: false);
                var newMainImage = remainingImages.FirstOrDefault();
                if (newMainImage != null)
                {
                    // Устанавливаем выбранное изображение как основное
                    await _specimenImageService.SetAsMainAsync(newMainImage.Id);
                }
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

        /// <summary>
        /// Загрузить набор изображений для образца растения
        /// </summary>
        /// <param name="dto">DTO массовой загрузки</param>
        [HttpPost("batch-upload")]
        [Authorize]
        [RequestSizeLimit(52428800)] // 50MB
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BatchSpecimenImageResultDto>> UploadBatch([FromForm] BatchImageUploadDto dto)
        {
            if (dto.Files == null || !dto.Files.Any())
            {
                return BadRequest(new { error = "Не предоставлены файлы для загрузки" });
            }

            var result = new BatchSpecimenImageResultDto
            {
                SpecimenId = dto.SpecimenId,
                SuccessCount = 0,
                ErrorCount = 0,
                UploadedImageIds = new List<int>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                var binaryDtos = new List<CreateSpecimenImageBinaryDto>();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var maxFileSize = 10 * 1024 * 1024; // 10MB

                foreach (var file in dto.Files)
                {
                    try
                    {
                        // Проверка размера файла
                        if (file.Length > maxFileSize)
                        {
                            throw new FileValidationException(file.FileName, $"Превышен размер файла (макс. {maxFileSize / 1024 / 1024}MB)");
                        }

                        // Проверка расширения файла
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                        {
                            throw new FileValidationException(file.FileName, $"Недопустимое расширение файла. Разрешены: {string.Join(", ", allowedExtensions)}");
                        }

                        // Преобразование в DTO с бинарными данными
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);

                        binaryDtos.Add(new CreateSpecimenImageBinaryDto
                        {
                            SpecimenId = dto.SpecimenId,
                            ImageData = memoryStream.ToArray(),
                            ContentType = file.ContentType,
                            Description = $"Загружено {DateTime.UtcNow}",
                            IsMain = dto.IsMain && binaryDtos.Count == 0 // Только первый файл может быть основным, если IsMain == true
                        });
                    }
                    catch (FileValidationException ex)
                    {
                        result.ErrorCount++;
                        result.ErrorMessages.Add(ex.Message);
                        _logger.LogWarning(ex, "Ошибка валидации файла '{FileName}'", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.ErrorMessages.Add($"Ошибка при обработке файла '{file.FileName}': {ex.Message}");
                        _logger.LogError(ex, "Ошибка при обработке файла '{FileName}'", file.FileName);
                    }
                }

                if (binaryDtos.Any())
                {
                    var uploadedImages = await _specimenImageService.AddMultipleAsync(binaryDtos);
                    result.SuccessCount = uploadedImages.Count();
                    result.UploadedImageIds = uploadedImages.Select(img => img.Id).ToList();
                }
                
                return Created($"api/v1/specimen-images/by-specimen/{dto.SpecimenId}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при массовой загрузке изображений для образца с ID {SpecimenId}", dto.SpecimenId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при загрузке изображений" });
            }
        }
    }
} 