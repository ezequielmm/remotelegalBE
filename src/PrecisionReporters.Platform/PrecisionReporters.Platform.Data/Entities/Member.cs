using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Member : BaseEntity<Member>
    {
        [ForeignKey(nameof(User))]
        [Column(TypeName = "char(36)")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Case))]
        [Column(TypeName = "char(36)")]
        public Guid CaseId { get; set; }

        public User User { get; set; }
        public Case Case { get; set; }

        public override void CopyFrom(Member entity)
        {
            User.CopyFrom(entity.User);
            Case.CopyFrom(entity.Case);
        }
    }
}
