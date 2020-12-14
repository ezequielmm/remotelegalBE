using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Handlers;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests._03_Data.Handlers
{
    public class TransactionHandlerTests
    {
        private readonly Mock<IDatabaseTransactionProvider> _databaseTransactionProviderMock;
        private readonly Mock<IDbContextTransaction> _transactionMock;

        private readonly TransactionHandler _transactionHandler;
        private readonly VerifiableExecutionStrategy _executionStrategyMock;

        public TransactionHandlerTests()
        {
            _executionStrategyMock = new VerifiableExecutionStrategy();
            
            _databaseTransactionProviderMock = new Mock<IDatabaseTransactionProvider>();
            _databaseTransactionProviderMock
                .Setup(db => db.CreateExecutionStrategy())
                .Returns(_executionStrategyMock);

            _transactionMock = new Mock<IDbContextTransaction>();
            _databaseTransactionProviderMock
                .Setup(db => db.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);
            
            var loggerMock = new Mock<ILogger<TransactionHandler>>();
            _transactionHandler = new TransactionHandler(_databaseTransactionProviderMock.Object, loggerMock.Object);

            Assert.NotNull(_transactionHandler);
        }
        
        [Fact]
        public async Task RunAsync_WhenTransactionWasInPlace_ExecutesAction()
        {
            var verifiableMethod = new VerifiableMethod();

            var fakeTransaction = Mock.Of<IDbContextTransaction>();
            _databaseTransactionProviderMock
                .Setup(db => db.CurrentTransaction)
                .Returns(fakeTransaction);
            await _transactionHandler.RunAsync(verifiableMethod.ActionMethod);
            
            verifiableMethod.VerifyWasCalled();
        }
        
        [Fact]
        public async Task RunAsync_WhenNoTransaction_CommitsNewTransaction()
        {
            var verifiableMethod = new VerifiableMethod();
            var result = await _transactionHandler.RunAsync(verifiableMethod.ActionMethod);
            
            Assert.True(result.IsSuccess);
            _databaseTransactionProviderMock.Verify(db => db.CreateExecutionStrategy());
            _databaseTransactionProviderMock.Verify(db => db.BeginTransactionAsync());
            verifiableMethod.VerifyWasCalled();
            _executionStrategyMock.VerifyExecute();
            _transactionMock.Verify(tran => tran.CommitAsync(It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task RunAsync_WhenNoTransaction_AndOperationThrows_RollsBackNewTransaction()
        {
            var result = await _transactionHandler.RunAsync(() => throw new FakeException());
            
            Assert.True(result.IsFailed);
            _databaseTransactionProviderMock.Verify(db => db.BeginTransactionAsync());
            _executionStrategyMock.VerifyExecute();
            _transactionMock.Verify(tran => tran.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            _transactionMock.Verify(tran => tran.RollbackAsync(It.IsAny<CancellationToken>()));
        }

        private class VerifiableMethod
        {
            private bool _wasCalled = false;

            public Task ActionMethod()
            {
                _wasCalled = true;
                return Task.CompletedTask;
            }

            public void VerifyWasCalled()
            {
                Assert.True(_wasCalled);
            }
        }

        private class VerifiableExecutionStrategy : IExecutionStrategy
        {
            private bool _executeWasCalled = false;
            public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded)
            {
                _executeWasCalled = true;
                return operation(null, state);
            }

            public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public bool RetriesOnFailure { get; }

            public void VerifyExecute()
            {
                Assert.True(_executeWasCalled);
            }
        }

        private class FakeException : Exception
        {
            
        }
    }
}