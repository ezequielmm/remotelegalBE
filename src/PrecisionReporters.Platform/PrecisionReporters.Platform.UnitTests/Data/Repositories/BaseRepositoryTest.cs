using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public abstract class BaseRepositoryTest<T> where T : BaseEntity<T>
    {
        //TODO: Add classes for others Repositories and they will be similar to BreakRoomRepositoryTest and CaseRepositoryTest

        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<DataAccessContextForTest> _dataAccessMock;
        private readonly BaseRepository<T> _repository;
        private readonly DataAccessContextForTest _dataAccess;
        public BaseRepositoryTest()
        {
            _configuration = new Mock<IConfiguration>();
            _dataAccessMock = new Mock<DataAccessContextForTest>(Guid.NewGuid(), _configuration.Object);
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid(), _configuration.Object);
            _repository = new BaseRepository<T>(_dataAccessMock.Object);
        }

        [Fact]
        public async Task Create()
        {
            // Arrange
            var objTest = (T)Activator.CreateInstance(typeof(T), new object[] { });
            objTest.CreationDate = DateTime.Now;
            var dbSetMock = new Mock<DbSet<T>>();
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            dbSetMock.Setup(x => x.Add(It.IsAny<T>())).Returns(_dataAccessMock.Object.Entry(objTest));

            // Act
            var result = await _repository.Create(objTest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateRange()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            var testlist = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };

            // Act
            var result = await _repository.CreateRange(testlist);

            //Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            dbSetMock.Verify(x => x.AddRangeAsync(It.IsAny<List<T>>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Update()
        {
            //Arrange
            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToUpdate = testList.FirstOrDefault();
            itemToUpdate.CreationDate = DateTime.Now;

            // Act
            var result = await repository.Update(itemToUpdate);

            //Assert
            Assert.NotNull(itemToUpdate);
            Assert.Equal(itemToUpdate, result);
        }

        [Fact]
        public async Task Remove()
        {
            //Arrange           
            var dbSetMock = new Mock<DbSet<T>>();
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            var testlist = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testlist.ForEach(x => x.Id = Guid.NewGuid());
            await _repository.CreateRange(testlist);
            var itemToRemove = testlist.FirstOrDefault();
            itemToRemove.CreationDate = DateTime.Now;
            dbSetMock.Setup(x => x.Remove(It.IsAny<T>())).Returns(_dataAccessMock.Object.Entry(itemToRemove));

            // Act
            await _repository.Remove(itemToRemove);

            //Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            dbSetMock.Verify(x => x.Remove(It.Is<T>(y => y == itemToRemove)));
        }

        [Fact]
        public async Task RemoveRange()
        {
            //Arrange           
            var dbSetMock = new Mock<DbSet<T>>();
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            var testlist = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testlist.ForEach(x => x.Id = Guid.NewGuid());
            await _repository.CreateRange(testlist);
            dbSetMock.Setup(x => x.RemoveRange(It.IsAny<T>()));

            // Act
            await _repository.RemoveRange(testlist);

            //Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            _dataAccessMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()));
            dbSetMock.Verify(x => x.RemoveRange(It.Is<List<T>>(y => y == testlist)));
        }

        [Fact]
        public async Task GetByFilter_Null_Include()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            var testObject = (T)Activator.CreateInstance(typeof(T), new object[] { });
            testObject.Id = Guid.NewGuid();
            var testList = new List<T>() { testObject }.AsQueryable();

            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(testList.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(testList.Provider));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(testList.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(testList.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => testList.GetEnumerator());
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);

            // Act
            var result = await _repository.GetByFilter(x => x.Id == testObject.Id);

            // Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            Assert.Equal(testObject, result.FirstOrDefault());
        }

        [Fact]
        public async Task GetByFilter_NotNull_Include()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            var testObject = (T)Activator.CreateInstance(typeof(T), new object[] { });
            testObject.Id = Guid.NewGuid();
            var testList = new List<T>() { testObject }.AsQueryable();

            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(testList.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(testList.Provider));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(testList.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(testList.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => testList.GetEnumerator());
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);

            // Act
            var result = await _repository.GetByFilter(x => x.Id == testObject.Id, new[] { "test" });

            // Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            Assert.Equal(testObject, result.FirstOrDefault());
        }

        [Fact]
        public async Task GetByFilter_With_AllParameters()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            var testObject = (T)Activator.CreateInstance(typeof(T), new object[] { });
            testObject.Id = Guid.NewGuid();
            var testList = new List<T>() { testObject }.AsQueryable();

            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(testList.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(testList.Provider));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(testList.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(testList.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => testList.GetEnumerator());
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);

            // Act
            var result = await _repository.GetByFilter(x => x.CreationDate, SortDirection.Ascend, x => x.Id == testObject.Id, new[] { "test" });

            // Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            Assert.Equal(testObject, result.FirstOrDefault());
        }

        [Fact]
        public async Task GetByFilterPaginationQueryable()
        {

            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToTest = testList.FirstOrDefault();
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderByQuery = null;
            orderByQuery = d => d.OrderBy(x => x.CreationDate);

            // Act
            var result = await repository.GetByFilterPaginationQueryable(x => x.Id == itemToTest.Id, orderByQuery.Compile(), null, 1, 1);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result.Item2.First(), itemToTest);
        }

        [Fact]
        public async Task GetByFilterPagination()
        {

            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToTest = testList.FirstOrDefault();
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderByQuery = null;
            orderByQuery = d => d.OrderBy(x => x.CreationDate);

            // Act
            var result = await repository.GetByFilterPagination(x => x.Id == itemToTest.Id, orderByQuery.Compile(), null, 1, 1);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result.Item2.First(), itemToTest);
        }

        [Fact]
        public async Task GetCountByFilter()
        {

            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToTest = testList.FirstOrDefault();

            // Act
            var result = await repository.GetCountByFilter(x => x.Id == itemToTest.Id);

            //Assert
            Assert.True(result == 1);
        }

        [Fact]
        public async Task GetByFilterOrderByThen()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            var testObject = (T)Activator.CreateInstance(typeof(T), new object[] { });
            testObject.Id = Guid.NewGuid();
            var testList = new List<T>() { testObject };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(testList.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(testList.AsQueryable().Provider));
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(testList.AsQueryable().Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(testList.AsQueryable().ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => testList.GetEnumerator());
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            await _repository.CreateRange(testList);
            _dataAccessMock.Object.SaveChanges();
            // Act
            var result = await _repository.GetByFilterOrderByThen(x => x.CreationDate, SortDirection.Descend, x => x.Id == testObject.Id, new[] { "test" }, x => x.CreationDate);

            // Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            Assert.Equal(testObject, result.FirstOrDefault());
        }

        [Fact]
        public async Task GetByFilterOrderByThen_NullOrderByThen()
        {
            // Arrange
            var dbSetMock = new Mock<DbSet<T>>();
            var testObject = (T)Activator.CreateInstance(typeof(T), new object[] { });
            testObject.Id = Guid.NewGuid();
            var testList = new List<T>() { testObject };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(testList.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(testList.AsQueryable().Provider));
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(testList.AsQueryable().Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(testList.AsQueryable().ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => testList.GetEnumerator());
            _dataAccessMock.Setup(x => x.Set<T>()).Returns(dbSetMock.Object);
            await _repository.CreateRange(testList);
            _dataAccessMock.Object.SaveChanges();
            // Act
            var result = await _repository.GetByFilterOrderByThen(x => x.CreationDate, SortDirection.Descend, x => x.Id == testObject.Id, new[] { "test" });

            // Assert
            _dataAccessMock.Verify(x => x.Set<T>());
            Assert.Equal(testObject, result.FirstOrDefault());
        }

        [Fact]
        public async Task GetFirstOrDefaultByFilter()
        {
            //Arrange
            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToTest = testList.FirstOrDefault();
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderByQuery = null;
            orderByQuery = d => d.OrderBy(x => x.CreationDate);

            // Act
            var result = await repository.GetFirstOrDefaultByFilter(x => x.Id == itemToTest.Id, null, true);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result, itemToTest);
        }

        [Fact]
        public async Task GetById()
        {
            //Arrange
            var testList = new List<T> {
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { }),
                (T)Activator.CreateInstance(typeof(T), new object[] { })
            };
            testList.ForEach(x => x.Id = Guid.NewGuid());
            var repository = new BaseRepository<T>(_dataAccess);
            await repository.CreateRange(testList);
            var itemToTest = testList.FirstOrDefault();
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderByQuery = null;
            orderByQuery = d => d.OrderBy(x => x.CreationDate);

            // Act
            var result = await repository.GetById(itemToTest.Id, null);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result, itemToTest);
        }
    }
}
