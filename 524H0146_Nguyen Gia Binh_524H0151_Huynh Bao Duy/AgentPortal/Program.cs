using AgentPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var b = WebApplication.CreateBuilder(args);
b.Services.AddDbContext<WholesaleDb>(o=>o.UseSqlServer(b.Configuration.GetConnectionString("Wholesale")));
b.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();
b.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
b.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
    o.Cookie.Name="AgentPortal.Session"; o.Cookie.HttpOnly=true; o.Cookie.SecurePolicy=CookieSecurePolicy.Always;
    o.LoginPath="/Account/Login"; o.ExpireTimeSpan=TimeSpan.FromMinutes(30);
    o.Events.OnValidatePrincipal=async ctx=>{
        var p=ctx.Principal!;
        var db=ctx.HttpContext.RequestServices.GetRequiredService<WholesaleDb>();
        if (!int.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out int uid) ||
            !int.TryParse(p.FindFirstValue("AgentID"),out int aid) ||
            !await db.Users.AnyAsync(u=>u.UserID==uid && u.AgentID==aid && u.Role=="Agent" && !u.Locked)) {
            ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync();
        }
    };
});
b.Services.AddAuthorization(o=>o.FallbackPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Agent").Build());
var app=b.Build();
if (args.Contains("--init-demo")) {
    using var scope=app.Services.CreateScope();
    var db=scope.ServiceProvider.GetRequiredService<WholesaleDb>();
    if (await db.Database.EnsureCreatedAsync()) {
        var sql=await File.ReadAllTextAsync(Path.Combine(app.Environment.ContentRootPath,"Data","DemoSeed.sql"));
        using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(sql);
        await tx.CommitAsync();
        Console.WriteLine("Đã tạo database và dữ liệu mẫu.");
    } else Console.WriteLine("Database đã có bảng. Bỏ qua khởi tạo.");
    return;
}
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute("default","{controller=Portal}/{action=Catalog}/{id?}");
app.Run();
