using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BGarden.API.DTOs
{
    // DTO for single upload
    public class UploadSpecimenImageDto
    {
        [Required]
        public int SpecimenId { get; set; }

        public string? Description { get; set; }

        public bool IsMain { get; set; }

        [Required]
        public IFormFile ImageFile { get; set; } = default!;
    }
} 