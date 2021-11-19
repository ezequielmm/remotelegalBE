using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public abstract class BaseEntity<T>
    {
        [Key]
        [Column(TypeName = "char(36)")]
        public Guid Id { get; set; }

        public DateTime CreationDate { get; set; }

        public abstract void CopyFrom(T entity);

        // TODO: add protected method for copying Id and Creation date to be called on each implementation of CopyFrom
    }
}
