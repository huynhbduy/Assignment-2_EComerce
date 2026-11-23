using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AgentPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentPortal.Controllers;

public class LoginInput {
    [Required, StringLength(80)] public string UserName { get; set; } = "";
    [Required, StringLength(512)] public string Password { get; set; } = "";
}
public class AccountController(WholesaleDb db, IPasswordHasher<User> hasher) : Controller {
    [AllowAnonymous,HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Catalog","Portal") : View(new LoginInput());

    [AllowAnonymous,HttpPost]
    public async Task<IActionResult> Login(LoginInput input) {
        if (ModelState.IsValid) {
            var u=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.UserName==input.UserName.Trim());
            if (u != null && AgentLogin.Allowed(u, input.Password, hasher)) {
                var claims=new[] { new Claim(ClaimTypes.NameIdentifier,u.UserID.ToString()),
                    new Claim(ClaimTypes.Name,u.UserName),new Claim(ClaimTypes.Role,"Agent"),
                    new Claim("AgentID",u.AgentID.Value.ToString()) };
                var identity=new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
                return RedirectToAction("Catalog","Portal");
            }
        }
        ModelState.Clear();
        ModelState.AddModelError("","Đăng nhập không thành công. Kiểm tra tài khoản đại lý và mật khẩu.");
        return View(new LoginInput {UserName=input.UserName ?? ""});
    }
    [HttpPost] public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login));
    }
}

public static class AgentLogin
{
    public static bool Allowed(User user, string password, IPasswordHasher<User> hasher) =>
        user is { Role: "Agent", Locked: false, AgentID: not null } &&
        hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
