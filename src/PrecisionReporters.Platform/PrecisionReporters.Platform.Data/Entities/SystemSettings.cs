using PrecisionReporters.Platform.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class SystemSettings : BaseEntity<SystemSettings>
    {
        [Required]
        public SystemSettingsName Name { get; set; }
        [Required]
        public string Value { get; set; }

        public override void CopyFrom(SystemSettings entity)
        {
            Name = entity.Name;
            Value = entity.Value;
        }
    }
}
