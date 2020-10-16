using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Case
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
