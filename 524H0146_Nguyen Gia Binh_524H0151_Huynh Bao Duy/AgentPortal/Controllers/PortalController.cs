using System.Security.Claims;
using AgentPortal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentPortal.Controllers;

[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class PortalController(WholesaleDb db) : Controller {
    int AgentId => int.Parse(User.FindFirstValue("AgentID")!);
    public async Task<IActionResult> Catalog() => View(await db.Items.AsNoTracking().OrderBy(i=>i.ItemName).ToListAsync());
    public async Task<IActionResult> History() => View(await db.Orders.AsNoTracking()
        .Where(o=>o.AgentID==AgentId).Include(o=>o.Lines).ThenInclude(d=>d.Item)
        .OrderByDescending(o=>o.OrderID).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Lines).ThenInclude(line => line.Item)
            .SingleOrDefaultAsync(o => o.OrderID == id && o.AgentID == AgentId);
        if (order is null) return NotFound();

        var buyer = await db.Set<Agent>().AsNoTracking()
            .SingleAsync(a => a.AgentID == order.AgentID);
        return View(new InvoiceView(order, buyer));
    }

    [HttpPost]
    public async Task<IActionResult> Place(Dictionary<int,int> quantities) {
        var selected=quantities.Where(q=>q.Value>0).OrderBy(q=>q.Key).ToList();
        if (!ModelState.IsValid || quantities.Any(q=>q.Value<0) || selected.Count==0) {
            TempData["Error"]="Hãy nhập số lượng nguyên dương cho ít nhất một mặt hàng.";
            return RedirectToAction(nameof(Catalog));
        }
        await using var tx=await db.Database.BeginTransactionAsync();
        var order=new Order { AgentID=AgentId };
        foreach (var (id,qty) in selected) {
            var item=db.Items.Where(i=>i.ItemID==id);
            var updated=await item.Where(i=>i.StockQuantity>=qty)
                .ExecuteUpdateAsync(s=>s.SetProperty(i=>i.StockQuantity,i=>i.StockQuantity-qty));
            if (updated!=1) {
                await tx.RollbackAsync();
                TempData["Error"]=$"Mặt hàng #{id} đã hết hoặc không đủ tồn kho. Toàn bộ đơn đã được hủy.";
                return RedirectToAction(nameof(Catalog));
            }
            var price=await item.Select(i=>i.Price).SingleAsync();
            order.Lines.Add(new OrderDetail { ItemID=id,Quantity=qty,UnitAmount=price });
        }
        db.Orders.Add(order); await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"]=$"Đã gửi đơn #{order.OrderID} đến kho.";
        return RedirectToAction(nameof(History));
    }
}

public record InvoiceView(Order Order, Agent Buyer);
