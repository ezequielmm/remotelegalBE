using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class VerifyUserRepository : BaseRepository<VerifyUser>, IVerifyUserRepository
    {
        public VerifyUserRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }        
    }
}
