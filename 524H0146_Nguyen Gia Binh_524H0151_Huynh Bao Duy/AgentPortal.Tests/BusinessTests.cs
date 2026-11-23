using AgentPortal.Controllers;
using AgentPortal.Data;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace AgentPortal.Tests;

public class BusinessTests
{
    [Fact]
    public void TotalUsesSalePricesAcrossLinesEvenWhenCatalogPriceChanges()
    {
        var item = new Item { ItemName = "Giấy A4", Size = "Ram", Price = 999m };
        var order = new Order();
        order.Lines.Add(new OrderDetail { Item = item, Quantity = 3, UnitAmount = 10.25m });
        order.Lines.Add(new OrderDetail { Quantity = 2, UnitAmount = 4.10m });
        Assert.Equal(38.95m, order.TotalAmount);
    }

    [Fact]
    public void EmptyOrderTotalIsZero() => Assert.Equal(0m, new Order().TotalAmount);

    [Theory]
    [InlineData(true, "Agent", 1, "correct", false)]
    [InlineData(false, "Agent", 1, "correct", true)]
    [InlineData(false, "Agent", 1, "wrong", false)]
    [InlineData(false, "Admin", null, "correct", false)]
    [InlineData(false, "Agent", null, "correct", false)]
    public void LoginChecksLockRoleAgentAndPassword(bool locked, string role, int? agent,
        string supplied, bool expected)
    {
        var user = new User { UserName = "tester", Email = "tester@example.com",
            Role = role, Locked = locked, AgentID = agent, PasswordHash = "" };
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "correct");
        Assert.Equal(expected, AgentLogin.Allowed(user, supplied, hasher));
    }
}
