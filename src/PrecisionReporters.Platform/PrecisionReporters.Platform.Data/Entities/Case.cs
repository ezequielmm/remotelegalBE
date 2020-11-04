using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Case : BaseEntity<Case>
    {
        [Required]
        public string Name { get; set; }

        public string CaseNumber { get; set; }

        public User AddedBy { get; set; }
        public virtual ICollection<Member> Members { get; set; }

        public override void CopyFrom(Case entity)
        {
            Name = entity.Name;
            CreationDate = entity.CreationDate;
        }
    }
}
