using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using PrecisionReporters.Platform.Data;
using System;

public class DataAccessContextForTest : ApplicationDbContext
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// This is the base that must be injected to use InMemory Testing
    /// </summary>
    /// <param name="dbGuid"></param>
    /// <param name="configuration"></param>
    public DataAccessContextForTest(Guid dbGuid, IConfiguration configuration) : base( new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: "RemoteLegalInMemoryDB")
        .Options)
    {
        _configuration = configuration;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }
}