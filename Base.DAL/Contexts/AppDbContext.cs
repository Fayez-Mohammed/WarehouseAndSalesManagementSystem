using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels; // Import the new models
using Base.Shared.Responses; // Assuming this exists based on your imports
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims; // Needed for simpler User ID access
using System.Threading.Tasks;

namespace Base.DAL.Contexts
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Applies all configurations (ProductConfig, OrderConfig, etc.)
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Optional: Global Query Filter to hide "Deleted" items automatically
            // builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. Get Current User ID (Optimized)
            // Instead of opening a new scope and querying the DB, we get the ID directly from the Token Claims.
            string? _userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Handle Soft Delete (Intercept .Remove())
            var deletedEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && e.State == EntityState.Deleted);

            foreach (var entry in deletedEntries)
            {
                // Stop the physical delete
                entry.State = EntityState.Modified;

                // Set flags
                var entity = (BaseEntity)entry.Entity;
                entity.IsDeleted = true;
                entity.DateOfDeletion = DateTime.UtcNow;
                entity.DeletedById = _userId;
            }

            // 3. Handle Audit (Added/Modified)
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                // Always update the modified date
                entity.DateOfUpdate = DateTime.UtcNow;
                entity.UpdatedById = _userId;

                if (entityEntry.State == EntityState.Added)
                {
                    // Fix: Updated property name to 'DateOfCreation'
                    entity.DateOfCreation = DateTime.UtcNow;
                    entity.CreatedById = _userId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        #region Auth DbSets
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<OtpEntry> OtpEntries { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
        #endregion

        #region User Profiles (Role Specific)
        public DbSet<SalesRepProfile> SalesRepProfiles { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
        #endregion

        #region Warehouse Module DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        #endregion
    }
}