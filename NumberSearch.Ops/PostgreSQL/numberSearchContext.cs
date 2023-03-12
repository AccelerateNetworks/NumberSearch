
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using AccelerateNetworks.Operations;

namespace AccelerateNetworks.Operations;

public partial class numberSearchContext : DbContext
{
    public numberSearchContext(DbContextOptions<numberSearchContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
    public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
    public virtual DbSet<Carrier> Carriers { get; set; } = null!;
    public virtual DbSet<Coupon> Coupons { get; set; } = null!;
    public virtual DbSet<EmergencyInformation> EmergencyInformations { get; set; } = null!;
    public virtual DbSet<ExtensionRegistration> ExtensionRegistrations { get; set; } = null!;
    public virtual DbSet<FollowMeRegistration> FollowMeRegistrations { get; set; } = null!;
    public virtual DbSet<Ingest> Ingests { get; set; } = null!;
    public virtual DbSet<Ingest1> Ingests1 { get; set; } = null!;
    public virtual DbSet<IngestCycle> IngestCycles { get; set; } = null!;
    public virtual DbSet<IntercomRegistration> IntercomRegistrations { get; set; } = null!;
    public virtual DbSet<Mvc> Mvcs { get; set; } = null!;
    public virtual DbSet<NewClient> NewClients { get; set; } = null!;
    public virtual DbSet<NumberDescription> NumberDescriptions { get; set; } = null!;
    public virtual DbSet<Order> Orders { get; set; } = null!;
    public virtual DbSet<OwnedPhoneNumber> OwnedPhoneNumbers { get; set; } = null!;
    public virtual DbSet<PhoneMenuOption> PhoneMenuOptions { get; set; } = null!;
    public virtual DbSet<PhoneNumber> PhoneNumbers { get; set; } = null!;
    public virtual DbSet<PhoneNumberLookup> PhoneNumberLookups { get; set; } = null!;
    public virtual DbSet<PortRequest> PortRequests { get; set; } = null!;
    public virtual DbSet<PortedPhoneNumber> PortedPhoneNumbers { get; set; } = null!;
    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<ProductOrder> ProductOrders { get; set; } = null!;
    public virtual DbSet<ProductShipment> ProductShipments { get; set; } = null!;
    public virtual DbSet<ProductItem> ProductItems { get; set; } = null!;
    public virtual DbSet<PurchasedPhoneNumber> PurchasedPhoneNumbers { get; set; } = null!;
    public virtual DbSet<SalesLead> SalesLeads { get; set; } = null!;
    public virtual DbSet<SentEmail> SentEmails { get; set; } = null!;
    public virtual DbSet<Service> Services { get; set; } = null!;
    public virtual DbSet<SpeedDialKey> SpeedDialKeys { get; set; } = null!;
    public virtual DbSet<VerifiedPhoneNumber> VerifiedPhoneNumbers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

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

            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles)
                .WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    l => l.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    r => r.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");

                        j.ToTable("AspNetUserRoles");

                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

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

            entity.HasOne(d => d.User)
                .WithMany(p => p.AspNetUserLogins)
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

        modelBuilder.Entity<Carrier>(entity =>
        {
            entity.Property(e => e.CarrierId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Color).HasColumnType("character varying");

            entity.Property(e => e.LastUpdated).HasColumnType("timestamp without time zone");

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

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.Property(e => e.CouponId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Description).HasColumnType("character varying");

            entity.Property(e => e.Name).HasColumnType("character varying");

            entity.Property(e => e.Type).HasColumnType("character varying");
        });

        modelBuilder.Entity<EmergencyInformation>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("EmergencyInformation");

            entity.Property(e => e.Address).HasColumnType("character varying");

            entity.Property(e => e.AlertGroup).HasColumnType("character varying");

            entity.Property(e => e.City).HasColumnType("character varying");

            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DialedNumber).HasMaxLength(10);

            entity.Property(e => e.EmergencyInformationId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.FullName).HasColumnType("character varying");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.ModifyDate).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Note).HasColumnType("character varying");

            entity.Property(e => e.State).HasColumnType("character varying");

            entity.Property(e => e.TeliId).HasColumnType("character varying");

            entity.Property(e => e.UnitNumber).HasColumnType("character varying");

            entity.Property(e => e.UnitType).HasColumnType("character varying");

            entity.Property(e => e.Zip).HasColumnType("character varying");
        });

        modelBuilder.Entity<ExtensionRegistration>(entity =>
        {
            entity.Property(e => e.ExtensionRegistrationId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Email).HasColumnType("character varying");

            entity.Property(e => e.ModelOfPhone).HasColumnType("character varying");

            entity.Property(e => e.NameOrLocation).HasColumnType("character varying");

            entity.Property(e => e.OutboundCallerId).HasColumnType("character varying");
        });

        modelBuilder.Entity<FollowMeRegistration>(entity =>
        {
            entity.Property(e => e.FollowMeRegistrationId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.CellPhoneNumber).HasColumnType("character varying");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.NumberOrExtension).HasColumnType("character varying");

            entity.Property(e => e.UnreachablePhoneNumber).HasColumnType("character varying");
        });

        modelBuilder.Entity<Ingest>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("Ingest", "Logs");

            entity.Property(e => e.Exception).HasColumnName("exception");

            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");

            entity.Property(e => e.MachineName).HasColumnName("machine_name");

            entity.Property(e => e.Message).HasColumnName("message");

            entity.Property(e => e.MessageTemplate).HasColumnName("message_template");

            entity.Property(e => e.Properties)
                .HasColumnType("jsonb")
                .HasColumnName("properties");

            entity.Property(e => e.PropsTest)
                .HasColumnType("jsonb")
                .HasColumnName("props_test");

            entity.Property(e => e.RaiseDate).HasColumnName("raise_date");
        });

        modelBuilder.Entity<Ingest1>(entity =>
        {
            entity.ToTable("Ingests");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.EndDate).HasColumnType("timestamp without time zone");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.Lock).HasDefaultValueSql("false");

            entity.Property(e => e.Priority).HasDefaultValueSql("false");

            entity.Property(e => e.StartDate).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<IngestCycle>(entity =>
        {
            entity.Property(e => e.IngestCycleId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Enabled).HasDefaultValueSql("false");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.LastUpdate).HasColumnType("timestamp without time zone");

            entity.Property(e => e.RunNow).HasDefaultValueSql("false");
        });

        modelBuilder.Entity<IntercomRegistration>(entity =>
        {
            entity.Property(e => e.IntercomRegistrationId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Mvc>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable("Mvc", "Logs");

            entity.Property(e => e.Exception).HasColumnName("exception");

            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");

            entity.Property(e => e.MachineName).HasColumnName("machine_name");

            entity.Property(e => e.Message).HasColumnName("message");

            entity.Property(e => e.MessageTemplate).HasColumnName("message_template");

            entity.Property(e => e.Properties)
                .HasColumnType("jsonb")
                .HasColumnName("properties");

            entity.Property(e => e.PropsTest)
                .HasColumnType("jsonb")
                .HasColumnName("props_test");

            entity.Property(e => e.RaiseDate).HasColumnName("raise_date");
        });

        modelBuilder.Entity<NewClient>(entity =>
        {
            entity.Property(e => e.NewClientId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.AfterHoursVoicemail).HasColumnType("character varying");

            entity.Property(e => e.BillingClientId).HasColumnType("character varying");

            entity.Property(e => e.BusinessHours).HasColumnType("character varying");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.HoldMusicDescription).HasColumnType("character varying");

            entity.Property(e => e.IntercomDescription).HasColumnType("character varying");

            entity.Property(e => e.OverheadPagingDescription).HasColumnType("character varying");

            entity.Property(e => e.PhoneOfflineInstructions).HasColumnType("character varying");

            entity.Property(e => e.PhonesToRingOrMenuDescription).HasColumnType("character varying");

            entity.Property(e => e.TextingServiceName).HasColumnType("character varying");
        });

        modelBuilder.Entity<NumberDescription>(entity =>
        {
            entity.Property(e => e.NumberDescriptionId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Description).HasColumnType("character varying");

            entity.Property(e => e.PhoneNumber).HasColumnType("character varying");

            entity.Property(e => e.Prefix).HasColumnType("character varying");
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

            entity.Property(e => e.DateSubmitted).HasColumnType("timestamp without time zone");

            entity.Property(e => e.E911ServiceNumber)
                .HasColumnType("character varying")
                .HasColumnName("E911ServiceNumber");

            entity.Property(e => e.Email).HasColumnType("character varying");

            entity.Property(e => e.FirstName).HasColumnType("character varying");

            entity.Property(e => e.InstallDate).HasColumnType("timestamp without time zone");

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
            entity.Property(e => e.BillingClientId).HasColumnType("character varying");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DialedNumber).HasMaxLength(10);

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.Lidbcnam)
                .HasColumnType("character varying")
                .HasColumnName("LIDBCNAM");

            entity.Property(e => e.OwnedBy).HasColumnType("character varying");

            entity.Property(e => e.OwnedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.SPID)
                .HasColumnType("character varying")
                .HasColumnName("SPID");

            entity.Property(e => e.SPIDName)
                .HasColumnType("character varying")
                .HasColumnName("SPIDName");
        });

        modelBuilder.Entity<PhoneMenuOption>(entity =>
        {
            entity.Property(e => e.PhoneMenuOptionId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Description).HasColumnType("character varying");

            entity.Property(e => e.Destination).HasColumnType("character varying");

            entity.Property(e => e.MenuOption).HasColumnType("character varying");
        });

        modelBuilder.Entity<PhoneNumber>(entity =>
        {
            entity.HasKey(e => e.DialedNumber)
                .HasName("PhoneNumbers_pkey");

            entity.Property(e => e.DialedNumber).HasMaxLength(10);

            entity.Property(e => e.City).HasColumnType("character varying");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.NPA).HasColumnName("NPA");

            entity.Property(e => e.NumberType).HasColumnType("character varying");

            entity.Property(e => e.NXX).HasColumnName("NXX");

            entity.Property(e => e.State).HasColumnType("character varying");

            entity.Property(e => e.XXXX).HasColumnName("XXXX");
        });

        modelBuilder.Entity<PhoneNumberLookup>(entity =>
        {
            entity.Property(e => e.PhoneNumberLookupId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.City).HasColumnType("character varying");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DialedNumber).HasMaxLength(11);

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.Jurisdiction).HasColumnType("character varying");

            entity.Property(e => e.LastPorted).HasColumnType("timestamp without time zone");

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

            entity.Property(e => e.DateCompleted).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateSubmitted).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.LocationType).HasColumnType("character varying");

            entity.Property(e => e.PartialPortDescription).HasColumnType("character varying");

            entity.Property(e => e.ProviderAccountNumber).HasColumnType("character varying");

            entity.Property(e => e.ProviderPIN)
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

            entity.Property(e => e.DateFirmOrderCommitment).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.ExternalPortRequestId).HasColumnType("character varying");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.NPA).HasColumnName("NPA");

            entity.Property(e => e.NXX).HasColumnName("NXX");

            entity.Property(e => e.PortedDialedNumber).HasMaxLength(10);

            entity.Property(e => e.RawResponse).HasColumnType("character varying");

            entity.Property(e => e.RequestStatus).HasColumnType("character varying");

            entity.Property(e => e.State).HasColumnType("character varying");

            entity.Property(e => e.Wireless).HasDefaultValueSql("false");

            entity.Property(e => e.XXXX).HasColumnName("XXXX");
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

            entity.Property(e => e.Tags).HasColumnType("character varying");

            entity.Property(e => e.VendorDescription).HasColumnType("character varying");

            entity.Property(e => e.VendorFeatures).HasColumnType("character varying");
        });

        modelBuilder.Entity<ProductOrder>(entity =>
        {
            entity.Property(e => e.ProductOrderId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DialedNumber).HasMaxLength(10);

            entity.Property(e => e.PortedDialedNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<ProductShipment>(entity =>
        {
            entity.Property(e => e.ProductShipmentId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.BillingClientId).HasColumnType("character varying");

            entity.Property(e => e.DateCreated)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.Name).HasColumnType("character varying");

            entity.Property(e => e.PurchasePrice).HasColumnType("money");

            entity.Property(e => e.ShipmentSource).HasColumnType("character varying");

            entity.Property(e => e.ShipmentType).HasColumnType("character varying");
        });

        modelBuilder.Entity<ProductItem>(entity =>
        {
            entity.Property(e => e.ProductItemId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.ProductId);
            entity.Property(e => e.ProductShipmentId);
            entity.Property(e => e.OrderId);

            entity.Property(e => e.SerialNumber).HasColumnType("character varying");

            entity.Property(e => e.MACAddress).HasColumnType("character varying");

            entity.Property(e => e.Condition).HasColumnType("character varying");

            entity.Property(e => e.ExternalOrderId).HasColumnType("character varying");

            entity.Property(e => e.ShipmentTrackingLink).HasColumnType("character varying");

            entity.Property(e => e.DateCreated)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.DateUpdated)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<PurchasedPhoneNumber>(entity =>
        {
            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateOrdered).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DialedNumber).HasMaxLength(10);

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.NPA).HasColumnName("NPA");

            entity.Property(e => e.NumberType).HasColumnType("character varying");

            entity.Property(e => e.NXX).HasColumnName("NXX");

            entity.Property(e => e.OrderResponse).HasColumnType("character varying");

            entity.Property(e => e.Pin)
                .HasColumnType("character varying")
                .HasColumnName("PIN");

            entity.Property(e => e.PurchasedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.XXXX).HasColumnName("XXXX");
        });

        modelBuilder.Entity<SalesLead>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.BusinessName).HasColumnType("character varying");

            entity.Property(e => e.DateSubmitted).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Email).HasColumnType("character varying");

            entity.Property(e => e.FirstName).HasColumnType("character varying");

            entity.Property(e => e.LastName).HasColumnType("character varying");

            entity.Property(e => e.PhoneNumber).HasMaxLength(10);

            entity.Property(e => e.RoleTitle).HasColumnType("character varying");
        });

        modelBuilder.Entity<SentEmail>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CalendarInvite).HasColumnType("character varying");

            entity.Property(e => e.CarbonCopy).HasColumnType("character varying");

            entity.Property(e => e.DateSent).HasColumnType("timestamp without time zone");

            entity.Property(e => e.EmailId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.MessageBody).HasColumnType("character varying");

            entity.Property(e => e.PrimaryEmailAddress).HasColumnType("character varying");

            entity.Property(e => e.SalesEmailAddress).HasColumnType("character varying");

            entity.Property(e => e.Subject).HasColumnType("character varying");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.ServiceId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Description).HasColumnType("character varying");

            entity.Property(e => e.Name).HasColumnType("character varying");

            entity.Property(e => e.Price).HasColumnType("character varying");
        });

        modelBuilder.Entity<SpeedDialKey>(entity =>
        {
            entity.Property(e => e.SpeedDialKeyId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DateUpdated).HasColumnType("timestamp without time zone");

            entity.Property(e => e.LabelOrName).HasColumnType("character varying");

            entity.Property(e => e.NumberOrExtension).HasColumnType("character varying");
        });

        modelBuilder.Entity<VerifiedPhoneNumber>(entity =>
        {
            entity.Property(e => e.VerifiedPhoneNumberId).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Activation).HasColumnType("character varying");

            entity.Property(e => e.City).HasColumnType("character varying");

            entity.Property(e => e.DateIngested).HasColumnType("timestamp without time zone");

            entity.Property(e => e.DateToExpire).HasColumnType("timestamp without time zone");

            entity.Property(e => e.IngestedFrom).HasColumnType("character varying");

            entity.Property(e => e.Jurisdiction).HasColumnType("character varying");

            entity.Property(e => e.LastPorted).HasColumnType("timestamp without time zone");

            entity.Property(e => e.LIDBName)
                .HasColumnType("character varying")
                .HasColumnName("LIDBName");

            entity.Property(e => e.Local).HasColumnType("character varying");

            entity.Property(e => e.LocalAccessTransportArea).HasColumnType("character varying");

            entity.Property(e => e.LocalExchangeCarrier).HasColumnType("character varying");

            entity.Property(e => e.LocalExchangeCarrierType).HasColumnType("character varying");

            entity.Property(e => e.LocalRoutingNumber).HasColumnType("character varying");

            entity.Property(e => e.NPA).HasColumnName("NPA");

            entity.Property(e => e.NumberType).HasColumnType("character varying");

            entity.Property(e => e.NXX).HasColumnName("NXX");

            entity.Property(e => e.OperatingCompanyNumber).HasColumnType("character varying");

            entity.Property(e => e.Province).HasColumnType("character varying");

            entity.Property(e => e.RateCenter).HasColumnType("character varying");

            entity.Property(e => e.ServiceProfileIdentifier).HasColumnType("character varying");

            entity.Property(e => e.VerifiedDialedNumber).HasMaxLength(10);

            entity.Property(e => e.XXXX).HasColumnName("XXXX");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}