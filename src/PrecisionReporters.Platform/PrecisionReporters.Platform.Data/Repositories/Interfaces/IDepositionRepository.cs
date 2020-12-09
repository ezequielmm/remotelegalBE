using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IDepositionRepository: IRepository<Deposition>
    {
        Task<List<Deposition>> GetByStatus(Expression<Func<Deposition, object>> orderBy, SortDirection sortDirection,
            Expression<Func<Deposition, bool>> filter = null, string[] include = null);
    }
} 