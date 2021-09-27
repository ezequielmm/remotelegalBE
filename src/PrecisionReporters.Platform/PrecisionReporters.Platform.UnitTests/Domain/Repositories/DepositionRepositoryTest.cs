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

namespace PrecisionReporters.Platform.UnitTests.Domain.Repositories
{
    public class DepositionRepositoryTest
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly DataAccessContextForTest _dataAccess;

        private DepositionRepository _repository;

        private List<Deposition> _depositions;

        public DepositionRepositoryTest()
        {
            _depositions = new List<Deposition>();

            _configuration = new Mock<IConfiguration>();

            _dataAccess = new DataAccessContextForTest(Guid.NewGuid(), _configuration.Object);
            _dataAccess.Database.EnsureDeleted();
            _dataAccess.Database.EnsureCreated();

            _repository = new DepositionRepository(_dataAccess);
        }

        private async Task SeedDb()
        {
            List<Deposition> depositions = new List<Deposition> {
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

            var upcomingList = _depositions.FindAll(w => w.Requester.EmailAddress == "testUser@mail.com");
            var depostionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsAsyncQueryable());

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByStatus(orderBy, It.IsAny<SortDirection>(), null, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
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

            // act
            var result = await _repository.GetByFilter(null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
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
            Assert.Contains(result, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
        }

        [Fact]
        public async Task GetDeposititionWithGetByFilterPagination_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilterPagination(null, null, null, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.Item2.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.Item2.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
        }

        [Fact]
        public async Task GetDeposititionWithGetByFilterPaginationQueryable_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilterPaginationQueryable(null, null, null, null, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.Item2, d => d.Requester.EmailAddress == "testUser@mail.com");
            Assert.Contains(result.Item2.SelectMany(a => a.Participants).Distinct(), participant => participant.Role == ParticipantType.Witness);
            Assert.Equal(result.Item2.SelectMany(b => b.Participants).Distinct().ToList(), participantThatAreWitness.ToList());
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

            // act
            var result = await _repository.GetFirstOrDefaultByFilter(filter);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, _depositions.FirstOrDefault(x => x.IsOnTheRecord == true));
        }

        [Fact]
        public async Task GetDepositionWithGetByFilterOrderByThen_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            Expression<Func<Deposition, object>> orderBy = x => x.StartDate;
            Expression<Func<Deposition, object>> orderByThen = x => x.CreationDate;

            var participantList = _depositions.SelectMany(b => b.Participants).Distinct();
            var participantThatAreWitness = participantList.Where(p => p.Role == ParticipantType.Witness);

            // act
            var result = await _repository.GetByFilterOrderByThen(orderBy, It.IsAny<SortDirection>(), null, null, orderByThen);

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

            // act
            var result = await _repository.GetById(Guid.Parse("bc627ed1-9e16-4522-93b5-ee96e6d73923"));

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, _depositions.FirstOrDefault(x => x.Id == Guid.Parse("bc627ed1-9e16-4522-93b5-ee96e6d73923")));
        }
    }
}
