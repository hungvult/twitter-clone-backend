using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.Entities;
using Xunit;

// Note: Integration tests require proper Program class setup
// For now, these are placeholder tests that demonstrate the structure
namespace TwitterClone.Api.Tests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    // Note: Full integration tests require WebApplicationFactory setup
    // These tests demonstrate the structure but need proper Program class configuration
    // For now, we focus on unit tests for services which are more straightforward

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}


