using System.Security.Claims;
using AgentPortal.Controllers;
using AgentPortal.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgentPortal.Tests;

public class CheckoutTests : IDisposable
{
    readonly SqliteConnection connection = new("Data Source=:memory:");
    readonly WholesaleDb db;
    readonly PortalController portal;

    public CheckoutTests()
    {
        connection.Open();
        connection.CreateFunction("SYSDATETIME", () => "2026-11-22 10:00:00");
        db = new WholesaleDb(new DbContextOptionsBuilder<WholesaleDb>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Add(new Agent { AgentID = 1, AgentName = "Test", Address = "A", Phone = "123" });
        db.AddRange(Product(1, 10), Product(2, 5));
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var context = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("AgentID","1") }, "Test"))
        };
        portal = new PortalController(db) {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new TestTempDataProvider())
        };
    }

    static Item Product(int id, int stock) => new Item { ItemID = id, ItemName = "Item " + id,
        Size = "Box", Price = 12.50m, StockQuantity = stock };

    [Fact]
    public async Task CheckoutCommitsMasterDetailsAndAllowsExactRemainingStock()
    {
        var result = await portal.Place(new() { [1] = 4, [2] = 5 });
        Assert.Equal("History", Assert.IsType<RedirectToActionResult>(result).ActionName);
        db.ChangeTracker.Clear();
        var order = await db.Orders.Include(o => o.Lines).SingleAsync();
        Assert.Equal(1, order.AgentID);
        Assert.Equal("Pending", order.OrderStatus);
        Assert.Equal(new[] { (1,4,12.50m), (2,5,12.50m) }, order.Lines.OrderBy(d=>d.ItemID)
            .Select(d=>(d.ItemID,d.Quantity,d.UnitAmount)).ToArray());
        Assert.Equal(new[] {6,0}, await Stocks());
    }

    [Theory]
    [InlineData(2,6)] // Second product has only five units.
    [InlineData(99,1)] // Second product does not exist.
    public async Task FailureOnLaterLineRestoresFirstItemAndLeavesNoOrder(int id, int qty)
    {
        var result = await portal.Place(new() { [1] = 3, [id] = qty });
        Assert.Equal("Catalog", Assert.IsType<RedirectToActionResult>(result).ActionName);
        await AssertUnchanged();
    }

    [Theory]
    [InlineData(-1,2)]
    [InlineData(0,0)]
    public async Task InvalidQuantitiesLeaveDatabaseUntouched(int first, int second)
    {
        var result = await portal.Place(new() { [1] = first, [2] = second });
        Assert.Equal("Catalog", Assert.IsType<RedirectToActionResult>(result).ActionName);
        await AssertUnchanged();
    }

    Task<int[]> Stocks() => db.Items.AsNoTracking().OrderBy(i=>i.ItemID)
        .Select(i=>i.StockQuantity).ToArrayAsync();

    async Task AssertUnchanged()
    {
        db.ChangeTracker.Clear();
        Assert.Equal(new[] {10,5}, await Stocks());
        Assert.False(await db.Orders.AnyAsync());
        Assert.False(await db.Set<OrderDetail>().AnyAsync());
    }

    public void Dispose() { db.Dispose(); connection.Dispose(); }

    sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string,object> LoadTempData(HttpContext context) => new Dictionary<string,object>();
        public void SaveTempData(HttpContext context, IDictionary<string,object> values) { }
    }
}
