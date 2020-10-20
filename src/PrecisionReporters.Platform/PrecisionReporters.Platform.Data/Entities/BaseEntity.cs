using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public abstract class BaseEntity<T>
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreationDate { get; set; }


        public abstract void CopyFrom(T entity);
    }
}
