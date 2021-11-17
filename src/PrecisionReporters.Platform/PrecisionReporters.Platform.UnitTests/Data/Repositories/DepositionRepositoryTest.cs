using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DepositionRepositoryTest
    {
        private readonly DataAccessContextForTest _dataAccess;

        private DepositionRepository _repository;

        private List<Deposition> depositions;

        private List<Deposition> _depositions;

        public DepositionRepositoryTest()
        {
            _depositions = new List<Deposition>();

            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _dataAccess.Database.EnsureDeleted();
            _dataAccess.Database.EnsureCreated();

            _repository = new DepositionRepository(_dataAccess);
        }

        private async Task SeedDb()
        {
            depositions = new List<Deposition> {
                new Deposition
                {
                    Id = Guid.Parse("6d5879aa-32ce-40a3-976d-fcc927e6487f"),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                },
                new Deposition
                {
                    Id = Guid.Parse("ecd125d5-cb5e-4b8a-91c3-830a8ea7270f"),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                },
                new Deposition
                {
                    Id = Guid.Parse("c83fee95-55c5-459d-95f6-b7ab1921b7f1"),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                },
                new Deposition
                {
                    Id = Guid.Parse("bc627ed1-9e16-4522-93b5-ee96e6d73923"),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                }
            };

            _depositions.AddRange(depositions);

            await _repository.CreateRange(_depositions);
        }

        [Fact]
        public async Task GetByStatus_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;
            Expression<Func<Deposition, object>> orderByThen = x => x.CreationDate;
            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;

            string[] include = new[] { nameof(Deposition.Participants) };


            var upcomingList = _depositions.FindAll(w => w.Requester.EmailAddress == "testUser@mail.com");
            var depostionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsAsyncQueryable());

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByStatus(orderBy, SortDirection.Ascend, filter, include, orderByThen);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
        }

        [Fact]
        public async Task GetByStatusOrderByThenNull_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;
            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;

            string[] include = new[] { nameof(Deposition.Participants) };


            var upcomingList = _depositions.FindAll(w => w.Requester.EmailAddress == "testUser@mail.com");
            var depostionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsAsyncQueryable());

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByStatus(orderBy, SortDirection.Ascend, filter, include, null);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
        }

        [Fact]
        public async Task GetDepositionWithAdmittedParticipant_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var upcomingList = _depositions.FindAll(w => w.Requester.EmailAddress == "testUser@mail.com");
            var depostionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsAsyncQueryable());

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetDepositionWithAdmittedParticipant(depostionsResult.Item2);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
        }

        [Fact]
        public async Task GetDepositionWithGetByFilter_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };

            // act
            var result = await _repository.GetByFilter(filter, include);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
        }

        [Fact]
        public async Task GetDepositionWithGetByFilterComplex_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;
            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };

            // act
            var result = await _repository.GetByFilter(orderBy, It.IsAny<SortDirection>(), filter, include);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.ToString(), _depositions.Where(x => x.IsOnTheRecord).ToList().ToString());
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
        }

        [Fact]
        public async Task GetDepositionWithGetByFilterOrderBy_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilter(orderBy, It.IsAny<SortDirection>(), null, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
        }

        [Fact]
        public async Task GetDeposititionWithGetByFilterPagination_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = await _depositions.AsAsyncQueryable().SelectMany(b => b.Participants).Distinct().ToListAsync();

            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };
            int page = 1;
            int pageSize = 4;

            // act
            var result = await _repository.GetByFilterPagination(filter, It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(), include, page, pageSize);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.Item2.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantList.ToList().ToString());
        }

        [Fact]
        public async Task GetDeposititionWithGetByFilterPaginationQueryable_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };
            int page = 1;
            int pageSize = 4;

            // act
            var result = await _repository.GetByFilterPaginationQueryable(filter, It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(), include, page, pageSize);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.Item2.SelectMany(a => a.Participants).Distinct().ToList().ToString(), _depositions.SelectMany(x => x.Participants).ToList().ToString());
            Assert.Equal(result.Item2.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
        }

        [Fact]
        public async Task GetDeposititionWithGetCountByFilter_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;

            // act
            var result = await _repository.GetCountByFilter(filter);

            // assert
            Assert.Equal(4, result);
        }

        [Fact]
        public async Task GetDeposititionWithGetFirstOrDefaultByFilter_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };
            bool tracking = false;
            // act
            var result = await _repository.GetFirstOrDefaultByFilter(filter, include, tracking);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.ToString(), _depositions.FirstOrDefault(x => x.IsOnTheRecord == true).ToString());
        }

        [Fact]
        public async Task GetDepositionWithGetByFilterOrderByThen_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;
            Expression<Func<Deposition, object>> orderByThen = x => x.CreationDate;
            Expression<Func<Deposition, bool>> filter = x => x.IsOnTheRecord;
            string[] include = new[] { nameof(Deposition.Participants) };


            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilterOrderByThen(orderBy, It.IsAny<SortDirection>(), filter, include, orderByThen);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList().ToString(), participantThatAreWitness.ToList().ToString());
        }

        [Fact]
        public async Task GetDepositionWithGetByFilterOrderByThenNull_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilterOrderByThen(orderBy, It.IsAny<SortDirection>(), null, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
        }

        [Fact]
        public async Task GetDeposititionWithGetById_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            string[] include = new[] { nameof(Deposition.Participants) };

            // act
            var result = await _repository.GetById(Guid.Parse("bc627ed1-9e16-4522-93b5-ee96e6d73923"), include);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.ToString(), _depositions.FirstOrDefault(x => x.Id == Guid.Parse("bc627ed1-9e16-4522-93b5-ee96e6d73923")).ToString());
        }

        [Fact]
        public async Task Create_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var newDeposition = new Deposition
            {
                Id = Guid.Parse("ea686a4d-4c0d-429f-aaf6-55ddec8f6d97"),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                CreationDate = DateTime.UtcNow,
                Requester = new User() { EmailAddress = "testUser@mail.com" },
                IsOnTheRecord = true,
            };

            // act
            var result = await _repository.Create(newDeposition);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, newDeposition);
        }

        [Fact]
        public async Task DepositionUpdate_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var newDeposition = new Deposition
            {
                Id = Guid.Parse("6d5879aa-32ce-40a3-976d-fcc927e6487f"),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                CreationDate = DateTime.UtcNow,
                Requester = new User() { EmailAddress = "testUser2@mail.com" },
                IsOnTheRecord = true,
            };

            // act
            var result = await _repository.Update(newDeposition);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.ToString(), newDeposition.ToString());

        }

        [Fact]
        public async Task Remove_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            // act
            var result = _repository.Remove(depositions.FirstOrDefault(x => x.Id == Guid.Parse("6d5879aa-32ce-40a3-976d-fcc927e6487f")));

            // assert
            Assert.Equal(Task.CompletedTask.IsCompleted, result.IsCompleted);

        }

        [Fact]
        public async Task RemoveRange_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            // act
            var result = _repository.RemoveRange(_depositions);

            // assert
            Assert.Equal(Task.CompletedTask.IsCompleted, result.IsCompleted);
        }
    }
}
