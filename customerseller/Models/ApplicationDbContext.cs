using Microsoft.EntityFrameworkCore;

namespace customerseller.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<TrackingStep> TrackingSteps { get; set; }

        public DbSet<AdminSetting> AdminSettings { get; set; }

        public DbSet<ModeratorSetting> ModeratorSettings { get; set; }

        public DbSet<CourierCompany> CourierCompanies { get; set; }

        public DbSet<SellerShop> SellerShops { get; set; }
        public DbSet<Complaint> Complaints { get; set; }

        public DbSet<SubCategoryVideo> SubCategoryVideos { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
        public DbSet<PaymentSubmission> PaymentSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.Total).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CartItem>().Property(c => c.Price).HasColumnType("decimal(18,2)");
        }

       
    }
}