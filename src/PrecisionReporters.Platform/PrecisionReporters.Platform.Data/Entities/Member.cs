using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Member : BaseEntity<Member>
    {
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(Case))]
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
