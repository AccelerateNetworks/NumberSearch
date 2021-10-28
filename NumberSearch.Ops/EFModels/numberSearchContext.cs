
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class numberSearchContext : DbContext
    {
        public numberSearchContext()
        {
        }

        public numberSearchContext(DbContextOptions<numberSearchContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRole> AspNetRoles { get; set; }
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRole> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }
        public virtual DbSet<Coupon> Coupons { get; set; }
        public virtual DbSet<EmergencyInformation> EmergencyInformations { get; set; }
        public virtual DbSet<Ingest> Ingests { get; set; }
        public virtual DbSet<IngestCycle> IngestCycles { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OwnedPhoneNumber> OwnedPhoneNumbers { get; set; }
        public virtual DbSet<PhoneNumber> PhoneNumbers { get; set; }
        public virtual DbSet<PortRequest> PortRequests { get; set; }
        public virtual DbSet<PortedPhoneNumber> PortedPhoneNumbers { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductOrder> ProductOrders { get; set; }
        public virtual DbSet<ProductShipment> ProductShipments { get; set; }
        public virtual DbSet<PurchasedPhoneNumber> PurchasedPhoneNumbers { get; set; }
        public virtual DbSet<SalesLead> SalesLeads { get; set; }
        public virtual DbSet<SentEmail> SentEmails { get; set; }
        public virtual DbSet<Service> Services { get; set; }
        public virtual DbSet<VerifiedPhoneNumber> VerifiedPhoneNumbers { get; set; }
        public virtual DbSet<Carrier> Carriers { get; set; }
        public virtual DbSet<PhoneNumberLookup> PhoneNumberLookups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp")
                .HasAnnotation("Relational:Collation", "en_US.UTF-8");

            modelBuilder.Entity<AspNetRole>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                    .IsUnique();

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetRoleClaim>(entity =>
            {
                entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

                entity.Property(e => e.RoleId).IsRequired();

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetUser>(entity =>
            {
                entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                    .IsUnique();

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.LockoutEnd).HasColumnType("timestamp with time zone");

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.UserName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaim>(entity =>
            {
                entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogin>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

                entity.Property(e => e.LoginProvider).HasMaxLength(128);

                entity.Property(e => e.ProviderKey).HasMaxLength(128);

                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasIndex(e => e.RoleId, "IX_AspNetUserRoles_RoleId");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserToken>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.Property(e => e.LoginProvider).HasMaxLength(128);

                entity.Property(e => e.Name).HasMaxLength(128);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.Property(e => e.CouponId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Description).HasColumnType("character varying");

                entity.Property(e => e.Name).HasColumnType("character varying");
            });

            modelBuilder.Entity<EmergencyInformation>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("EmergencyInformation");

                entity.Property(e => e.Address).HasColumnType("character varying");

                entity.Property(e => e.AlertGroup).HasColumnType("character varying");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.DialedNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.EmergencyInformationId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.FullName).HasColumnType("character varying");

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Note).HasColumnType("character varying");

                entity.Property(e => e.State).HasColumnType("character varying");

                entity.Property(e => e.TeliId).HasColumnType("character varying");

                entity.Property(e => e.UnitNumber).HasColumnType("character varying");

                entity.Property(e => e.UnitType).HasColumnType("character varying");

                entity.Property(e => e.Zip).HasColumnType("character varying");
            });

            modelBuilder.Entity<Ingest>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

                entity.Property(e => e.Lock).HasDefaultValueSql("false");

                entity.Property(e => e.Priority).HasDefaultValueSql("false");
            });

            modelBuilder.Entity<IngestCycle>(entity =>
            {
                entity.Property(e => e.IngestCycleId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Enabled).HasDefaultValueSql("false");

                entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

                entity.Property(e => e.RunNow).HasDefaultValueSql("false");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.OrderId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Address).HasColumnType("character varying");

                entity.Property(e => e.Address2).HasColumnType("character varying");

                entity.Property(e => e.AddressUnitNumber).HasColumnType("character varying");

                entity.Property(e => e.AddressUnitType).HasColumnType("character varying");

                entity.Property(e => e.BillingClientId).HasColumnType("character varying");

                entity.Property(e => e.BillingInvoiceId).HasColumnType("character varying");

                entity.Property(e => e.BillingInvoiceReoccuringId).HasColumnType("character varying");

                entity.Property(e => e.BusinessName).HasColumnType("character varying");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.Email).HasColumnType("character varying");

                entity.Property(e => e.FirstName).HasColumnType("character varying");

                entity.Property(e => e.LastName).HasColumnType("character varying");

                entity.Property(e => e.ReoccuringInvoiceLink).HasColumnType("character varying");

                entity.Property(e => e.SalesEmail).HasColumnType("character varying");

                entity.Property(e => e.State).HasColumnType("character varying");

                entity.Property(e => e.UnparsedAddress).HasColumnType("character varying");

                entity.Property(e => e.UpfrontInvoiceLink).HasColumnType("character varying");

                entity.Property(e => e.Zip).HasColumnType("character varying");
            });

            modelBuilder.Entity<OwnedPhoneNumber>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.BillingClientId).HasColumnType("character varying");

                entity.Property(e => e.DialedNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Lidbcnam)
                    .HasColumnType("character varying")
                    .HasColumnName("LIDBCNAM");

                entity.Property(e => e.OwnedBy).HasColumnType("character varying");

                entity.Property(e => e.OwnedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Spid)
                    .HasColumnType("character varying")
                    .HasColumnName("SPID");

                entity.Property(e => e.Spidname)
                    .HasColumnType("character varying")
                    .HasColumnName("SPIDName");
            });

            modelBuilder.Entity<PhoneNumber>(entity =>
            {
                entity.HasKey(e => e.DialedNumber)
                    .HasName("PhoneNumbers_pkey");

                entity.Property(e => e.DialedNumber).HasMaxLength(10);

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Npa).HasColumnName("NPA");

                entity.Property(e => e.NumberType).HasColumnType("character varying");

                entity.Property(e => e.Nxx).HasColumnName("NXX");

                entity.Property(e => e.State).HasColumnType("character varying");

                entity.Property(e => e.Xxxx).HasColumnName("XXXX");
            });

            modelBuilder.Entity<PortRequest>(entity =>
            {
                entity.Property(e => e.PortRequestId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Address).HasColumnType("character varying");

                entity.Property(e => e.Address2).HasColumnType("character varying");

                entity.Property(e => e.BillImageFileType).HasColumnType("character varying");

                entity.Property(e => e.BillImagePath).HasColumnType("character varying");

                entity.Property(e => e.BillingPhone).HasColumnType("character varying");

                entity.Property(e => e.BusinessContact).HasColumnType("character varying");

                entity.Property(e => e.BusinessName).HasColumnType("character varying");

                entity.Property(e => e.CallerId).HasColumnType("character varying");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.LocationType).HasColumnType("character varying");

                entity.Property(e => e.PartialPortDescription).HasColumnType("character varying");

                entity.Property(e => e.ProviderAccountNumber).HasColumnType("character varying");

                entity.Property(e => e.ProviderPin)
                    .HasColumnType("character varying")
                    .HasColumnName("ProviderPIN");

                entity.Property(e => e.RequestStatus).HasColumnType("character varying");

                entity.Property(e => e.ResidentialFirstName).HasColumnType("character varying");

                entity.Property(e => e.ResidentialLastName).HasColumnType("character varying");

                entity.Property(e => e.State).HasColumnType("character varying");

                entity.Property(e => e.TeliId).HasColumnType("character varying");

                entity.Property(e => e.VendorSubmittedTo).HasColumnType("character varying");

                entity.Property(e => e.Zip).HasColumnType("character varying");
            });

            modelBuilder.Entity<PortedPhoneNumber>(entity =>
            {
                entity.Property(e => e.PortedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.ExternalPortRequestId).HasColumnType("character varying");

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Npa).HasColumnName("NPA");

                entity.Property(e => e.Nxx).HasColumnName("NXX");

                entity.Property(e => e.PortedDialedNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.RawResponse).HasColumnType("character varying");

                entity.Property(e => e.RequestStatus).HasColumnType("character varying");

                entity.Property(e => e.State).HasColumnType("character varying");

                entity.Property(e => e.Wireless).HasDefaultValueSql("false");

                entity.Property(e => e.Xxxx).HasColumnName("XXXX");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.ProductId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Description).HasColumnType("character varying");

                entity.Property(e => e.Image).HasColumnType("character varying");

                entity.Property(e => e.Name).HasColumnType("character varying");

                entity.Property(e => e.Price).HasColumnType("character varying");

                entity.Property(e => e.SupportLink).HasColumnType("character varying");

                entity.Property(e => e.Type).HasColumnType("character varying");

                entity.Property(e => e.VendorPartNumber).HasColumnType("character varying");
            });

            modelBuilder.Entity<ProductOrder>(entity =>
            {
                entity.Property(e => e.ProductOrderId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.DialedNumber).HasMaxLength(10);

                entity.Property(e => e.PortedDialedNumber).HasMaxLength(10);
            });

            modelBuilder.Entity<ProductShipment>(entity =>
            {
                entity.Property(e => e.ProductShipmentId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.BillingClientId).HasColumnType("character varying");

                entity.Property(e => e.DateCreated).HasDefaultValueSql("now()");

                entity.Property(e => e.Name).HasColumnType("character varying");

                entity.Property(e => e.PurchasePrice).HasColumnType("money");

                entity.Property(e => e.ShipmentSource).HasColumnType("character varying");

                entity.Property(e => e.ShipmentType).HasColumnType("character varying");
            });

            modelBuilder.Entity<PurchasedPhoneNumber>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.DialedNumber).HasMaxLength(10);

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Npa).HasColumnName("NPA");

                entity.Property(e => e.NumberType).HasColumnType("character varying");

                entity.Property(e => e.Nxx).HasColumnName("NXX");

                entity.Property(e => e.OrderResponse).HasColumnType("character varying");

                entity.Property(e => e.Pin)
                    .HasColumnType("character varying")
                    .HasColumnName("PIN");

                entity.Property(e => e.PurchasedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Xxxx).HasColumnName("XXXX");
            });

            modelBuilder.Entity<SalesLead>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.BusinessName).HasColumnType("character varying");

                entity.Property(e => e.Email).HasColumnType("character varying");

                entity.Property(e => e.FirstName).HasColumnType("character varying");

                entity.Property(e => e.LastName).HasColumnType("character varying");

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.RoleTitle).HasColumnType("character varying");
            });

            modelBuilder.Entity<SentEmail>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.CalendarInvite).HasColumnType("character varying");

                entity.Property(e => e.CarbonCopy)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.EmailId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.MessageBody)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.PrimaryEmailAddress)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.SalesEmailAddress).HasColumnType("character varying");

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasColumnType("character varying");
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.Property(e => e.ServiceId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Description).HasColumnType("character varying");

                entity.Property(e => e.Name).HasColumnType("character varying");

                entity.Property(e => e.Price).HasColumnType("character varying");
            });

            modelBuilder.Entity<VerifiedPhoneNumber>(entity =>
            {
                entity.Property(e => e.VerifiedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Activation).HasColumnType("character varying");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.IngestedFrom)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.Jurisdiction).HasColumnType("character varying");

                entity.Property(e => e.Lidbname)
                    .HasColumnType("character varying")
                    .HasColumnName("LIDBName");

                entity.Property(e => e.Local).HasColumnType("character varying");

                entity.Property(e => e.LocalAccessTransportArea).HasColumnType("character varying");

                entity.Property(e => e.LocalExchangeCarrier).HasColumnType("character varying");

                entity.Property(e => e.LocalExchangeCarrierType).HasColumnType("character varying");

                entity.Property(e => e.LocalRoutingNumber).HasColumnType("character varying");

                entity.Property(e => e.Npa).HasColumnName("NPA");

                entity.Property(e => e.NumberType).HasColumnType("character varying");

                entity.Property(e => e.Nxx).HasColumnName("NXX");

                entity.Property(e => e.OperatingCompanyNumber).HasColumnType("character varying");

                entity.Property(e => e.Province).HasColumnType("character varying");

                entity.Property(e => e.RateCenter).HasColumnType("character varying");

                entity.Property(e => e.ServiceProfileIdentifier).HasColumnType("character varying");

                entity.Property(e => e.VerifiedDialedNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Xxxx).HasColumnName("XXXX");
            });

            modelBuilder.Entity<Carrier>(entity =>
            {
                entity.Property(e => e.CarrierId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Color).HasColumnType("character varying");

                entity.Property(e => e.Lec)
                    .HasColumnType("character varying")
                    .HasColumnName("LEC");

                entity.Property(e => e.Lectype)
                    .HasColumnType("character varying")
                    .HasColumnName("LECType");

                entity.Property(e => e.LogoLink).HasColumnType("character varying");

                entity.Property(e => e.Name).HasColumnType("character varying");

                entity.Property(e => e.Ocn)
                    .HasColumnType("character varying")
                    .HasColumnName("OCN");

                entity.Property(e => e.Ratecenter).HasColumnType("character varying");

                entity.Property(e => e.Spid)
                    .HasColumnType("character varying")
                    .HasColumnName("SPID");

                entity.Property(e => e.Type).HasColumnType("character varying");
            });

            modelBuilder.Entity<PhoneNumberLookup>(entity =>
            {
                entity.Property(e => e.PhoneNumberLookupId).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.City).HasColumnType("character varying");

                entity.Property(e => e.DialedNumber)
                    .IsRequired()
                    .HasMaxLength(11);

                entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

                entity.Property(e => e.Jurisdiction).HasColumnType("character varying");

                entity.Property(e => e.Lata)
                    .HasColumnType("character varying")
                    .HasColumnName("LATA");

                entity.Property(e => e.Lec)
                    .HasColumnType("character varying")
                    .HasColumnName("LEC");

                entity.Property(e => e.Lectype)
                    .HasColumnType("character varying")
                    .HasColumnName("LECType");

                entity.Property(e => e.Lidbname)
                    .HasColumnType("character varying")
                    .HasColumnName("LIDBName");

                entity.Property(e => e.Lrn)
                    .HasColumnType("character varying")
                    .HasColumnName("LRN");

                entity.Property(e => e.Ocn)
                    .HasColumnType("character varying")
                    .HasColumnName("OCN");

                entity.Property(e => e.Ratecenter).HasColumnType("character varying");

                entity.Property(e => e.Spid)
                    .HasColumnType("character varying")
                    .HasColumnName("SPID");

                entity.Property(e => e.State).HasColumnType("character varying");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
