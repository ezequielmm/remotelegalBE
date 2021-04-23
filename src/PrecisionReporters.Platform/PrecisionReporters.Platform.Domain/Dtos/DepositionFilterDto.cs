using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionFilterDto
    {
        [FromQuery(Name = "MaxDate")]
        public DateTimeOffset? MaxDate { get; set; }

        [FromQuery(Name = "MinDate")]
        public DateTimeOffset? MinDate { get; set; }

        [FromQuery(Name = "status")]
        public DepositionStatus? Status { get; set; }

        [FromQuery(Name = "sortedField")]
        public DepositionSortField? SortedField { get; set; }

        [FromQuery(Name = "sortDirection")]
        public SortDirection? SortDirection { get; set; }
        [FromQuery(Name = "page")]
        public int Page { get; set; }
        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; }
        [FromQuery(Name = "pastDepositions")]
        public bool PastDepositions { get; set; }
    }
}