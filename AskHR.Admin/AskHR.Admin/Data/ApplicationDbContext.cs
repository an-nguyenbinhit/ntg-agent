using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Admin.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = new Guid("d5147680-87f5-41dc-aff2-e041959c2fa1").ToString(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "c3a91a6b-a975-4542-af12-321515222481"
            });

            builder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = new Guid("e0afe23f-b53c-4ad8-b718-cb4ff5bb9f71").ToString(),
                UserName = "admin@askhr.com",
                Email = "admin@askhr.com",
                NormalizedEmail = "ADMIN@ASKHR.COM",
                NormalizedUserName = "ADMIN@ASKHR.COM",
                AccessFailedCount = 0, 
                ConcurrencyStamp = "101cd6ae-a8ef-4a37-97fd-04ac2dd630e4", 
                EmailConfirmed = true,
                LockoutEnabled = true, 
                PasswordHash = "AQAAAAIAAYagAAAAEF2jKeeVxIQVMpGg1sRF7CJLB36wu7m3gsfFWm8b0MkC0DbCqUA9hzzm+G0t2Jr+dw==", //AskHR@123
                PhoneNumberConfirmed = false, 
                SecurityStamp = "a9565acb-cee6-425f-9833-419a793f5fba", 
                TwoFactorEnabled = false,
            });

            builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                UserId = "e0afe23f-b53c-4ad8-b718-cb4ff5bb9f71",
                RoleId = "d5147680-87f5-41dc-aff2-e041959c2fa1"
            });

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = new Guid("3dc04c42-9b42-4920-b7f2-29dfc2c5d169").ToString(),
                Name = "Anonymous",
                NormalizedName = "ANONYMOUS",
                ConcurrencyStamp = "94602b5b-18d2-4043-9761-c64818c856cd"
            });
        }
    }
}
