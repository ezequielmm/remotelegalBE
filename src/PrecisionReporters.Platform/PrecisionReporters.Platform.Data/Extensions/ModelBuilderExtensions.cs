using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Linq;

namespace PrecisionReporters.Platform.Data.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void SetBaseEntityCreationDateProperties(this ModelBuilder modelBuilder, string columnType, string defaultValueSql)
        {
            var entitiesTypes = modelBuilder.Model
                .GetEntityTypes()
                .ToList();
            foreach (var entityType in entitiesTypes)
            {
                if (IsSubclassOfRawGeneric(typeof(BaseEntity<>), entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>(nameof(BaseEntity<object>.CreationDate))
                        .HasColumnType(columnType)
                        .HasDefaultValueSql(defaultValueSql);
                }
            }
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
