using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaymentsService.Controllers;
using PaymentsService.Data;
using PaymentsService.Models;
using Xunit;

namespace PaymentsService.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly PaymentsDbContext _context;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PaymentsDbContext(options);
        _context.Database.EnsureCreated(); 
        _controller = new PaymentsController(_context);
    }

    [Fact]
    public async Task CreateAccount_NewUser_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.CreateAccount(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var account = Assert.IsType<Account>(okResult.Value);
        Assert.Equal(userId, account.UserId);
        Assert.Equal(0, account.Balance);

        var savedAccount = await _context.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(savedAccount);
        Assert.Equal(0, savedAccount.Balance);

        var outboxMessage = await _context.OutboxMessages
            .SingleOrDefaultAsync(m => m.Type == "AccountCreated");
        Assert.NotNull(outboxMessage);
    }

    [Fact]
    public async Task CreateAccount_DuplicateUser_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingAccount = new Account 
        { 
            UserId = userId, 
            Balance = 0
        };
        _context.Accounts.Add(existingAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.CreateAccount(userId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Deposit_ExistingAccount_UpdatesBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account 
        { 
            UserId = userId, 
            Balance = 50
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var request = new PaymentsController.DepositRequest
        {
            UserId = userId,
            Amount = 100
        };

        // Act
        var result = await _controller.Deposit(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var depositResult = Assert.IsType<PaymentsController.DepositResult>(okResult.Value);
        Assert.Equal(userId, depositResult.UserId);
        Assert.Equal(150m, depositResult.Balance);

        var updatedAccount = await _context.Accounts.SingleAsync(a => a.UserId == userId);
        Assert.Equal(150m, updatedAccount.Balance);

        var outboxMessage = await _context.OutboxMessages
            .SingleOrDefaultAsync(m => m.Type == "PaymentProcessed");
        Assert.NotNull(outboxMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Deposit_InvalidAmount_ReturnsBadRequest(decimal amount)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account 
        { 
            UserId = userId, 
            Balance = 0
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var request = new PaymentsController.DepositRequest
        {
            UserId = userId,
            Amount = amount
        };

        // Act
        var result = await _controller.Deposit(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
    [Fact]
    public async Task GetBalance_ExistingAccount_ReturnsBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            UserId = userId,
            Balance = 500
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetBalance(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var balanceInfo = Assert.IsAssignableFrom<object>(okResult.Value);
        var jsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(
            System.Text.Json.JsonSerializer.Serialize(balanceInfo));
    
        Assert.Equal(userId, jsonElement.GetProperty("userId").GetGuid());
        Assert.Equal(500m, jsonElement.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task GetBalance_NonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.GetBalance(userId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Deposit_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new PaymentsController.DepositRequest
        {
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _controller.Deposit(request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Deposit_ZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            UserId = userId,
            Balance = 100
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var request = new PaymentsController.DepositRequest
        {
            UserId = userId,
            Amount = 0
        };

        // Act
        var result = await _controller.Deposit(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}