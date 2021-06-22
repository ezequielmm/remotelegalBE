using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class UserFilterDto
    {
        [FromQuery(Name = "sortedField")]
        public UserSortField? SortedField { get; set; }
        [FromQuery(Name = "sortDirection")]
        public SortDirection? SortDirection { get; set; }
        [Required]
        [FromQuery(Name = "page")]
        public int Page { get; set; }
        [Required]
        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; }
    }
}