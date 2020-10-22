using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public abstract class BaseEntity<T>
    {
        [Key]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreationDate { get; set; }

        public abstract void CopyFrom(T entity);
    }
}
