using Application.DTO;
using Application.DTO.Exceptions; // Используется для FileValidationException
using Application.Interfaces;
using BGarden.API.DTOs; // Added using for the new DTO
using BGarden.API.Exceptions; // Используется для ResourceNotFoundException через alias
using ApiResourceNotFoundException = BGarden.API.Exceptions.ResourceNotFoundException; // Псевдоним для разрешения неоднозначности
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using BGarden.Domain.Interfaces;
using Microsoft.Extensions.Logging; // Добавлен отсутствующий using для ILogger

namespace BGarden.API.Controllers
{
    [ApiController]
    [Route("api/v1/specimen-images")]
    public class SpecimenImageController : ControllerBase
    {
        private readonly ISpecimenImageService _specimenImageService;
        private readonly ILogger<SpecimenImageController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public SpecimenImageController(
            ISpecimenImageService specimenImageService,
            ILogger<SpecimenImageController> logger,
            IUnitOfWork unitOfWork)
        {
            _specimenImageService = specimenImageService ?? throw new ArgumentNullException(nameof(specimenImageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private string GetBaseApiUrl()
        {
            var pathBase = Request.PathBase.ToUriComponent();
            return $"{Request.Scheme}://{Request.Host}{pathBase}";
        }

        private void PopulateImageUrl(SpecimenImageDto? dto)
        {
            if (dto?.RelativeFilePath != null)
            {
                // Исправлено: одинарный обратный слэш экранируется как '\\'
                dto.ImageUrl = $"{GetBaseApiUrl()}/{dto.RelativeFilePath.TrimStart('/', '\\')}";
            }
        }

        private void PopulateImageUrls(IEnumerable<SpecimenImageDto> dtos)
        {
            var baseApiUrl = GetBaseApiUrl();
            foreach (var dto in dtos)
            {
                if (dto?.RelativeFilePath != null)
                {
                    // Исправлено: одинарный обратный слэш экранируется как '\\'
                    dto.ImageUrl = $"{baseApiUrl}/{dto.RelativeFilePath.TrimStart('/', '\\')}";
                }
            }
        }

        /// <summary>
        /// Получить все изображения для указанного образца
        /// </summary>
        /// <param name="specimenId">Идентификатор образца</param>
        /// <param name="includeImageUrl">Включать ли URL изображения</param>
        [HttpGet("by-specimen/{specimenId}")]
        [ProducesResponseType(typeof(IEnumerable<SpecimenImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SpecimenImageDto>>> GetBySpecimenId(
            int specimenId,
            [FromQuery] bool includeImageUrl = true)
        {
            var images = await _specimenImageService.GetBySpecimenIdAsync(specimenId);
            if (includeImageUrl) PopulateImageUrls(images);
            return Ok(images);
        }

        /// <summary>
        /// Получить основное изображение для указанного образца
        /// </summary>
        /// <param name="specimenId">Идентификатор образца</param>
        [HttpGet("by-specimen/{specimenId}/main")]
        [ProducesResponseType(typeof(SpecimenImageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> GetMainImageBySpecimenId(int specimenId)
        {
            var image = await _specimenImageService.GetMainImageBySpecimenIdAsync(specimenId);
            if (image == null)
            {
                // Исправлено: используется псевдоним для разрешения неоднозначности
                throw new ApiResourceNotFoundException($"Основное изображение для образца с ID {specimenId} не найдено");
            }
            PopulateImageUrl(image);
            return Ok(image);
        }

        /// <summary>
        /// Получить изображение по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SpecimenImageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> GetById(int id)
        {
            var image = await _specimenImageService.GetByIdAsync(id);
            if (image == null)
            {
                // Исправлено: используется псевдоним для разрешения неоднозначности
                throw new ApiResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            PopulateImageUrl(image);
            return Ok(image);
        }

        /// <summary>
        /// Добавить новое изображение (из формы)
        /// </summary>
        /// <param name="specimenId">ID образца</param>
        /// <param name="description">Описание</param>
        /// <param name="isMain">Основное ли</param>
        /// <param name="imageFile">Файл изображения</param>
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SpecimenImageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> UploadAndAddImage(
            [FromForm] UploadSpecimenImageDto form)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var resultDto = await _specimenImageService.UploadAndAddImageAsync(
                    form.SpecimenId, 
                    form.ImageFile, 
                    form.Description, 
                    form.IsMain);
                await _unitOfWork.SaveChangesAsync();

                PopulateImageUrl(resultDto);
                return CreatedAtAction(nameof(GetById), new { id = resultDto.Id }, resultDto);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Ошибка ввода-вывода при загрузке изображения для образца ID {SpecimenId}", form.SpecimenId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Ошибка при сохранении файла: {ex.Message}" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Некорректные аргументы при загрузке изображения для образца ID {SpecimenId}", form.SpecimenId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при загрузке изображения для образца ID {SpecimenId}", form.SpecimenId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Внутренняя ошибка сервера при загрузке изображения." });
            }
        }

        /// <summary>
        /// Обновить существующее изображение (только метаданные)
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        /// <param name="dto">DTO обновления изображения</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(SpecimenImageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpecimenImageDto>> Update(int id, [FromBody] UpdateSpecimenImageDto dto)
        {
            var result = await _specimenImageService.UpdateAsync(id, dto);
            if (result == null)
            {
                // Исправлено: используется псевдоним для разрешения неоднозначности
                throw new ApiResourceNotFoundException($"Изображение с ID {id} не найдено");
            }
            await _unitOfWork.SaveChangesAsync();
            PopulateImageUrl(result);
            return Ok(result);
        }

        /// <summary>
        /// Удалить изображение по идентификатору (включая файл)
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
            bool success = await _specimenImageService.DeleteAsync(id);
            if (!success)
            {
                var imageExists = await _specimenImageService.GetByIdAsync(id, false); // Предполагается, что этот метод не выбрасывает исключение, если не найден
                if (imageExists == null)
                {
                    // Исправлено: используется псевдоним для разрешения неоднозначности
                    throw new ApiResourceNotFoundException($"Изображение с ID {id} не найдено или уже удалено.");
                }

                _logger.LogError("Не удалось полностью завершить операцию удаления для изображения ID {ImageId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Не удалось удалить изображение." });
            }
            await _unitOfWork.SaveChangesAsync();
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
                // Исправлено: используется псевдоним для разрешения неоднозначности
                throw new ApiResourceNotFoundException($"Изображение с ID {id} не найдено или произошла ошибка при установке как основного");
            }
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Загрузить набор изображений для образца растения
        /// </summary>
        /// <param name="batchDto">Данные для пакетной загрузки</param>
        [HttpPost("batch-upload")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(52428800)] // 50MB
        [ProducesResponseType(typeof(BatchSpecimenImageResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BatchSpecimenImageResultDto>> UploadBatch([FromForm] BatchImageUploadDto batchDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (batchDto.Files == null || !batchDto.Files.Any())
            {
                return BadRequest(new { error = "Не предоставлены файлы для загрузки" });
            }
            if (batchDto.SpecimenId <= 0)
            {
                return BadRequest(new { error = "Некорректный ID образца." });
            }

            var overallResult = new BatchSpecimenImageResultDto
            {
                SpecimenId = batchDto.SpecimenId,
                SuccessCount = 0,
                ErrorCount = 0,
                UploadedImageIds = new List<int>(),
                ErrorMessages = new List<string>()
            };

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var maxFileSize = 10 * 1024 * 1024; // 10MB
            bool firstIsMain = batchDto.IsMain;

            foreach (var file in batchDto.Files)
            {
                try
                {
                    if (file.Length == 0)
                    {
                        overallResult.ErrorCount++;
                        overallResult.ErrorMessages.Add($"Файл '{file.FileName}' пуст.");
                        continue;
                    }
                    if (file.Length > maxFileSize)
                    {
                        // FileValidationException из Application.DTO.Exceptions
                        throw new FileValidationException(file.FileName, $"Превышен размер файла (макс. {maxFileSize / 1024 / 1024}MB)");
                    }
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        // FileValidationException из Application.DTO.Exceptions
                        throw new FileValidationException(file.FileName, $"Недопустимое расширение файла. Разрешены: {string.Join(", ", allowedExtensions)}");
                    }

                    var addedImageDto = await _specimenImageService.UploadAndAddImageAsync(
                        batchDto.SpecimenId,
                        file,
                        description: $"Пакетная загрузка: {file.FileName}",
                        isMain: firstIsMain
                    );
                    firstIsMain = false; // Только первый файл может быть основным, если isMain = true

                    overallResult.SuccessCount++;
                    overallResult.UploadedImageIds.Add(addedImageDto.Id);
                }
                catch (FileValidationException ex) // Это исключение из Application.DTO.Exceptions
                {
                    overallResult.ErrorCount++;
                    overallResult.ErrorMessages.Add(ex.Message);
                    _logger.LogWarning(ex, "Ошибка валидации файла при пакетной загрузке: '{FileName}'", file.FileName);
                }
                catch (IOException ex)
                {
                    overallResult.ErrorCount++;
                    overallResult.ErrorMessages.Add($"Ошибка сохранения файла '{file.FileName}': {ex.Message}");
                    _logger.LogError(ex, "Ошибка сохранения файла при пакетной загрузке: '{FileName}'", file.FileName);
                }
                catch (Exception ex)
                {
                    overallResult.ErrorCount++;
                    overallResult.ErrorMessages.Add($"Непредвиденная ошибка при обработке файла '{file.FileName}': {ex.Message}");
                    _logger.LogError(ex, "Непредвиденная ошибка при пакетной загрузке файла: '{FileName}'", file.FileName);
                }
            }

            if (overallResult.SuccessCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            if (overallResult.SuccessCount > 0)
            {
                // Заполняем URL для успешно загруженных изображений, если это необходимо (не требуется по ТЗ, но хорошая практика)
                // var uploadedImages = await _specimenImageService.GetByIdsAsync(overallResult.UploadedImageIds);
                // PopulateImageUrls(uploadedImages.Where(img => img != null)!); 
                // overallResult.UploadedImages = uploadedImages; // Если BatchSpecimenImageResultDto будет содержать полные DTO

                return Created($"api/v1/specimen-images/by-specimen/{batchDto.SpecimenId}", overallResult);
            }
            else if (overallResult.ErrorCount > 0)
            {
                return BadRequest(overallResult);
            }
            else
            {
                // Случай, когда batchDto.Files был пуст (уже обработан выше) или все файлы были пропущены без ошибок (маловероятно)
                return BadRequest(new { message = "Файлы не были обработаны или не предоставлены." });
            }
        }
    }
}