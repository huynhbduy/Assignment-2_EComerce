using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPortal.Data;

public class Item {
    public int ItemID { get; set; }
    [MaxLength(120)] public required string ItemName { get; set; }
    [MaxLength(50)] public required string Size { get; set; }
    [Precision(18,2)] public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
public class Agent {
    public int AgentID { get; set; }
    [MaxLength(120)] public required string AgentName { get; set; }
    [MaxLength(240)] public required string Address { get; set; }
    [MaxLength(20),Unicode(false)] public required string Phone { get; set; }
}
public class Order {
    public int OrderID { get; set; }
    [Precision(0)] public DateTime OrderDate { get; set; }
    public int AgentID { get; set; }
    [MaxLength(10),Unicode(false)] public string OrderStatus { get; set; } = "Pending";
    [NotMapped] public decimal TotalAmount => Lines.Sum(line => line.Quantity * line.UnitAmount);
    public List<OrderDetail> Lines { get; set; } = new();
}
public class OrderDetail {
    public int ID { get; set; }
    public int OrderID { get; set; }
    public int ItemID { get; set; }
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; }
    [Precision(18,2)] public decimal UnitAmount { get; set; }
}
public class User {
    public int UserID { get; set; }
    [MaxLength(80)] public required string UserName { get; set; }
    [Column("email"),MaxLength(254),Unicode(false)] public required string Email { get; set; }
    [Column("password"),MaxLength(512),Unicode(false)] public required string PasswordHash { get; set; }
    [Column("lock")] public bool Locked { get; set; }
    [MaxLength(10),Unicode(false)] public required string Role { get; set; }
    public int? AgentID { get; set; }
}
public class WholesaleDb(DbContextOptions<WholesaleDb> options) : DbContext(options) {
    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    protected override void OnModelCreating(ModelBuilder m) {
        m.Entity<Agent>().ToTable("Agent");
        m.Entity<Item>().ToTable("Item", t => t.HasCheckConstraint("CK_Item_Value","Price > 0 AND StockQuantity >= 0"));
        m.Entity<Order>().ToTable("Order", t => t.HasCheckConstraint("CK_Order_Status","OrderStatus IN ('Pending','Shipped')"));
        m.Entity<Order>().Property(o=>o.OrderStatus).HasDefaultValue("Pending");
        m.Entity<Order>().Property(o=>o.OrderDate).HasDefaultValueSql("SYSDATETIME()");
        m.Entity<Order>().HasOne<Agent>().WithMany().HasForeignKey(o=>o.AgentID).OnDelete(DeleteBehavior.Restrict);
        m.Entity<OrderDetail>().ToTable("OrderDetail", t => t.HasCheckConstraint("CK_Line_Value","Quantity > 0 AND UnitAmount > 0"));
        m.Entity<OrderDetail>().HasIndex(d=>new {d.OrderID,d.ItemID}).IsUnique();
        m.Entity<Order>().HasMany(o=>o.Lines).WithOne().HasForeignKey(d=>d.OrderID).OnDelete(DeleteBehavior.Cascade);
        m.Entity<OrderDetail>().HasOne(d=>d.Item).WithMany().HasForeignKey(d=>d.ItemID).OnDelete(DeleteBehavior.Restrict);
        m.Entity<User>().ToTable("Users",t=>t.HasCheckConstraint("CK_User_Role","(Role='Admin' AND AgentID IS NULL) OR (Role='Agent' AND AgentID IS NOT NULL)"));
        m.Entity<User>().HasIndex(u=>u.UserName).IsUnique();
        m.Entity<User>().HasIndex(u=>u.Email).IsUnique();
        m.Entity<User>().HasOne<Agent>().WithMany().HasForeignKey(u=>u.AgentID).OnDelete(DeleteBehavior.Restrict);
    }
}
