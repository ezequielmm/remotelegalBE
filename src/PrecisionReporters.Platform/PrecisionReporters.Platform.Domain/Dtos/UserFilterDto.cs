using System;
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
        [FromQuery(Name = "page")]
        public int Page { get; set; }
        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; }
    }
}