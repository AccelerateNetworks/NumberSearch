using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccelerateNetworks.Operations.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Logs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OCN = table.Column<string>(type: "character varying", nullable: true),
                    LEC = table.Column<string>(type: "character varying", nullable: true),
                    LECType = table.Column<string>(type: "character varying", nullable: true),
                    SPID = table.Column<string>(type: "character varying", nullable: true),
                    Name = table.Column<string>(type: "character varying", nullable: true),
                    Type = table.Column<string>(type: "character varying", nullable: true),
                    Ratecenter = table.Column<string>(type: "character varying", nullable: true),
                    Color = table.Column<string>(type: "character varying", nullable: true),
                    LogoLink = table.Column<string>(type: "character varying", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.CarrierId);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "character varying", nullable: true),
                    Description = table.Column<string>(type: "character varying", nullable: true),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying", nullable: true),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.CouponId);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyInformation",
                columns: table => new
                {
                    EmergencyInformationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    DialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TeliId = table.Column<string>(type: "character varying", nullable: true),
                    FullName = table.Column<string>(type: "character varying", nullable: true),
                    Address = table.Column<string>(type: "character varying", nullable: true),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    Zip = table.Column<string>(type: "character varying", nullable: true),
                    UnitType = table.Column<string>(type: "character varying", nullable: true),
                    UnitNumber = table.Column<string>(type: "character varying", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AlertGroup = table.Column<string>(type: "character varying", nullable: true),
                    Note = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ExtensionRegistrations",
                columns: table => new
                {
                    ExtensionRegistrationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtensionNumber = table.Column<int>(type: "integer", nullable: true),
                    NameOrLocation = table.Column<string>(type: "character varying", nullable: true),
                    Email = table.Column<string>(type: "character varying", nullable: true),
                    ModelOfPhone = table.Column<string>(type: "character varying", nullable: true),
                    OutboundCallerId = table.Column<string>(type: "character varying", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionRegistrations", x => x.ExtensionRegistrationId);
                });

            migrationBuilder.CreateTable(
                name: "FollowMeRegistrations",
                columns: table => new
                {
                    FollowMeRegistrationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumberOrExtension = table.Column<string>(type: "character varying", nullable: true),
                    CellPhoneNumber = table.Column<string>(type: "character varying", nullable: true),
                    UnreachablePhoneNumber = table.Column<string>(type: "character varying", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowMeRegistrations", x => x.FollowMeRegistrationId);
                });

            migrationBuilder.CreateTable(
                name: "Ingest",
                schema: "Logs",
                columns: table => new
                {
                    message = table.Column<string>(type: "text", nullable: true),
                    message_template = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    raise_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exception = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    props_test = table.Column<string>(type: "jsonb", nullable: true),
                    machine_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "IngestCycles",
                columns: table => new
                {
                    IngestCycleId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: true),
                    CycleTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false"),
                    RunNow = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestCycles", x => x.IngestCycleId);
                });

            migrationBuilder.CreateTable(
                name: "Ingests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NumbersRetrived = table.Column<long>(type: "bigint", nullable: false),
                    IngestedNew = table.Column<long>(type: "bigint", nullable: false),
                    FailedToIngest = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedExisting = table.Column<long>(type: "bigint", nullable: false),
                    Unchanged = table.Column<long>(type: "bigint", nullable: false),
                    Removed = table.Column<long>(type: "bigint", nullable: false),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Lock = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false"),
                    Priority = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntercomRegistrations",
                columns: table => new
                {
                    IntercomRegistrationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtensionSendingIntercom = table.Column<int>(type: "integer", nullable: true),
                    ExtensionRecievingIntercom = table.Column<int>(type: "integer", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntercomRegistrations", x => x.IntercomRegistrationId);
                });

            migrationBuilder.CreateTable(
                name: "Mvc",
                schema: "Logs",
                columns: table => new
                {
                    message = table.Column<string>(type: "text", nullable: true),
                    message_template = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    raise_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exception = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    props_test = table.Column<string>(type: "jsonb", nullable: true),
                    machine_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "NewClients",
                columns: table => new
                {
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingClientId = table.Column<string>(type: "character varying", nullable: true),
                    PhoneMenu = table.Column<bool>(type: "boolean", nullable: false),
                    PhonesToRingOrMenuDescription = table.Column<string>(type: "character varying", nullable: true),
                    BusinessHours = table.Column<string>(type: "character varying", nullable: true),
                    AfterHoursVoicemail = table.Column<string>(type: "character varying", nullable: true),
                    TextingService = table.Column<bool>(type: "boolean", nullable: false),
                    TextingServiceName = table.Column<string>(type: "character varying", nullable: true),
                    OverheadPaging = table.Column<bool>(type: "boolean", nullable: false),
                    OverheadPagingDescription = table.Column<string>(type: "character varying", nullable: true),
                    Intercom = table.Column<bool>(type: "boolean", nullable: false),
                    CustomHoldMusic = table.Column<bool>(type: "boolean", nullable: false),
                    HoldMusicDescription = table.Column<string>(type: "character varying", nullable: true),
                    PhoneOfflineInstructions = table.Column<string>(type: "character varying", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SpeedDial = table.Column<bool>(type: "boolean", nullable: false),
                    IntercomDescription = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewClients", x => x.NewClientId);
                });

            migrationBuilder.CreateTable(
                name: "NumberDescriptions",
                columns: table => new
                {
                    NumberDescriptionId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying", nullable: true),
                    Description = table.Column<string>(type: "character varying", nullable: true),
                    Prefix = table.Column<string>(type: "character varying", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberDescriptions", x => x.NumberDescriptionId);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FirstName = table.Column<string>(type: "character varying", nullable: true),
                    LastName = table.Column<string>(type: "character varying", nullable: true),
                    Email = table.Column<string>(type: "character varying", nullable: true),
                    Address = table.Column<string>(type: "character varying", nullable: true),
                    Address2 = table.Column<string>(type: "character varying", nullable: true),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    Zip = table.Column<string>(type: "character varying", nullable: true),
                    DateSubmitted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    BillingClientId = table.Column<string>(type: "character varying", nullable: true),
                    BillingInvoiceId = table.Column<string>(type: "character varying", nullable: true),
                    Quote = table.Column<bool>(type: "boolean", nullable: true),
                    BillingInvoiceReoccuringId = table.Column<string>(type: "character varying", nullable: true),
                    SalesEmail = table.Column<string>(type: "character varying", nullable: true),
                    BackgroundWorkCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    InstallDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpfrontInvoiceLink = table.Column<string>(type: "character varying", nullable: true),
                    ReoccuringInvoiceLink = table.Column<string>(type: "character varying", nullable: true),
                    OnsiteInstallation = table.Column<bool>(type: "boolean", nullable: false),
                    AddressUnitType = table.Column<string>(type: "character varying", nullable: true),
                    AddressUnitNumber = table.Column<string>(type: "character varying", nullable: true),
                    UnparsedAddress = table.Column<string>(type: "character varying", nullable: true),
                    MergedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    E911ServiceNumber = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "OwnedPhoneNumbers",
                columns: table => new
                {
                    OwnedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    DialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    BillingClientId = table.Column<string>(type: "character varying", nullable: true),
                    OwnedBy = table.Column<string>(type: "character varying", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SPID = table.Column<string>(type: "character varying", nullable: true),
                    SPIDName = table.Column<string>(type: "character varying", nullable: true),
                    LIDBCNAM = table.Column<string>(type: "character varying", nullable: true),
                    EmergencyInformationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "PhoneMenuOptions",
                columns: table => new
                {
                    PhoneMenuOptionId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuOption = table.Column<string>(type: "character varying", nullable: false),
                    Destination = table.Column<string>(type: "character varying", nullable: false),
                    Description = table.Column<string>(type: "character varying", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneMenuOptions", x => x.PhoneMenuOptionId);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumberLookups",
                columns: table => new
                {
                    PhoneNumberLookupId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    DialedNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    LRN = table.Column<string>(type: "character varying", nullable: true),
                    OCN = table.Column<string>(type: "character varying", nullable: true),
                    LATA = table.Column<string>(type: "character varying", nullable: true),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    Ratecenter = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    Jurisdiction = table.Column<string>(type: "character varying", nullable: true),
                    Local = table.Column<bool>(type: "boolean", nullable: false),
                    LEC = table.Column<string>(type: "character varying", nullable: true),
                    LECType = table.Column<string>(type: "character varying", nullable: true),
                    SPID = table.Column<string>(type: "character varying", nullable: true),
                    LIDBName = table.Column<string>(type: "character varying", nullable: true),
                    LastPorted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: true),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumberLookups", x => x.PhoneNumberLookupId);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumbers",
                columns: table => new
                {
                    DialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NPA = table.Column<int>(type: "integer", nullable: false),
                    NXX = table.Column<int>(type: "integer", nullable: false),
                    XXXX = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NumberType = table.Column<string>(type: "character varying", nullable: true),
                    Purchased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PhoneNumbers_pkey", x => x.DialedNumber);
                });

            migrationBuilder.CreateTable(
                name: "PortedPhoneNumbers",
                columns: table => new
                {
                    PortedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    PortedDialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NPA = table.Column<int>(type: "integer", nullable: false),
                    NXX = table.Column<int>(type: "integer", nullable: false),
                    XXXX = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PortRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Wireless = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false"),
                    RequestStatus = table.Column<string>(type: "character varying", nullable: true),
                    DateFirmOrderCommitment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExternalPortRequestId = table.Column<string>(type: "character varying", nullable: true),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    RawResponse = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortedPhoneNumbers", x => x.PortedPhoneNumberId);
                });

            migrationBuilder.CreateTable(
                name: "PortRequests",
                columns: table => new
                {
                    PortRequestId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying", nullable: true),
                    Address2 = table.Column<string>(type: "character varying", nullable: true),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    State = table.Column<string>(type: "character varying", nullable: true),
                    Zip = table.Column<string>(type: "character varying", nullable: true),
                    BillingPhone = table.Column<string>(type: "character varying", nullable: true),
                    LocationType = table.Column<string>(type: "character varying", nullable: true),
                    BusinessContact = table.Column<string>(type: "character varying", nullable: true),
                    BusinessName = table.Column<string>(type: "character varying", nullable: true),
                    ProviderAccountNumber = table.Column<string>(type: "character varying", nullable: true),
                    ProviderPIN = table.Column<string>(type: "character varying", nullable: true),
                    PartialPort = table.Column<bool>(type: "boolean", nullable: false),
                    PartialPortDescription = table.Column<string>(type: "character varying", nullable: true),
                    WirelessNumber = table.Column<bool>(type: "boolean", nullable: false),
                    CallerId = table.Column<string>(type: "character varying", nullable: true),
                    BillImagePath = table.Column<string>(type: "character varying", nullable: true),
                    BillImageFileType = table.Column<string>(type: "character varying", nullable: true),
                    DateSubmitted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResidentialFirstName = table.Column<string>(type: "character varying", nullable: true),
                    ResidentialLastName = table.Column<string>(type: "character varying", nullable: true),
                    TeliId = table.Column<string>(type: "character varying", nullable: true),
                    RequestStatus = table.Column<string>(type: "character varying", nullable: true),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VendorSubmittedTo = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortRequests", x => x.PortRequestId);
                });

            migrationBuilder.CreateTable(
                name: "ProductOrders",
                columns: table => new
                {
                    ProductOrderId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PortedDialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PortedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOrders", x => x.ProductOrderId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "character varying", nullable: true),
                    Price = table.Column<string>(type: "character varying", nullable: true),
                    Description = table.Column<string>(type: "character varying", nullable: true),
                    Image = table.Column<string>(type: "character varying", nullable: true),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: false),
                    SupportLink = table.Column<string>(type: "character varying", nullable: true),
                    DisplayPriority = table.Column<int>(type: "integer", nullable: true),
                    VendorPartNumber = table.Column<string>(type: "character varying", nullable: true),
                    Type = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "ProductShipments",
                columns: table => new
                {
                    ProductShipmentId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingClientId = table.Column<string>(type: "character varying", nullable: true),
                    Name = table.Column<string>(type: "character varying", nullable: true),
                    ShipmentSource = table.Column<string>(type: "character varying", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "money", nullable: true),
                    ShipmentType = table.Column<string>(type: "character varying", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductShipments", x => x.ProductShipmentId);
                });

            migrationBuilder.CreateTable(
                name: "PurchasedPhoneNumbers",
                columns: table => new
                {
                    PurchasedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateOrdered = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrderResponse = table.Column<string>(type: "character varying", nullable: true),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    NPA = table.Column<int>(type: "integer", nullable: true),
                    NXX = table.Column<int>(type: "integer", nullable: true),
                    XXXX = table.Column<int>(type: "integer", nullable: true),
                    NumberType = table.Column<string>(type: "character varying", nullable: true),
                    PIN = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SalesLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    BusinessName = table.Column<string>(type: "character varying", nullable: true),
                    RoleTitle = table.Column<string>(type: "character varying", nullable: true),
                    FirstName = table.Column<string>(type: "character varying", nullable: true),
                    LastName = table.Column<string>(type: "character varying", nullable: true),
                    Email = table.Column<string>(type: "character varying", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DateSubmitted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SentEmails",
                columns: table => new
                {
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryEmailAddress = table.Column<string>(type: "character varying", nullable: false),
                    CarbonCopy = table.Column<string>(type: "character varying", nullable: false),
                    Subject = table.Column<string>(type: "character varying", nullable: false),
                    MessageBody = table.Column<string>(type: "character varying", nullable: false),
                    DateSent = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    SalesEmailAddress = table.Column<string>(type: "character varying", nullable: true),
                    CalendarInvite = table.Column<string>(type: "character varying", nullable: true),
                    DoNotSend = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "character varying", nullable: true),
                    Price = table.Column<string>(type: "character varying", nullable: true),
                    Description = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "SpeedDialKeys",
                columns: table => new
                {
                    SpeedDialKeyId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    NewClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumberOrExtension = table.Column<string>(type: "character varying", nullable: true),
                    LabelOrName = table.Column<string>(type: "character varying", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeedDialKeys", x => x.SpeedDialKeyId);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedPhoneNumbers",
                columns: table => new
                {
                    VerifiedPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    VerifiedDialedNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NPA = table.Column<int>(type: "integer", nullable: false),
                    NXX = table.Column<int>(type: "integer", nullable: false),
                    XXXX = table.Column<int>(type: "integer", nullable: false),
                    IngestedFrom = table.Column<string>(type: "character varying", nullable: false),
                    DateIngested = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Wireless = table.Column<bool>(type: "boolean", nullable: false),
                    NumberType = table.Column<string>(type: "character varying", nullable: true),
                    LocalRoutingNumber = table.Column<string>(type: "character varying", nullable: true),
                    OperatingCompanyNumber = table.Column<string>(type: "character varying", nullable: true),
                    City = table.Column<string>(type: "character varying", nullable: true),
                    LocalAccessTransportArea = table.Column<string>(type: "character varying", nullable: true),
                    RateCenter = table.Column<string>(type: "character varying", nullable: true),
                    Province = table.Column<string>(type: "character varying", nullable: true),
                    Jurisdiction = table.Column<string>(type: "character varying", nullable: true),
                    Local = table.Column<string>(type: "character varying", nullable: true),
                    LocalExchangeCarrier = table.Column<string>(type: "character varying", nullable: true),
                    LocalExchangeCarrierType = table.Column<string>(type: "character varying", nullable: true),
                    ServiceProfileIdentifier = table.Column<string>(type: "character varying", nullable: true),
                    Activation = table.Column<string>(type: "character varying", nullable: true),
                    LIDBName = table.Column<string>(type: "character varying", nullable: true),
                    LastPorted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateToExpire = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedPhoneNumbers", x => x.VerifiedPhoneNumberId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "EmergencyInformation");

            migrationBuilder.DropTable(
                name: "ExtensionRegistrations");

            migrationBuilder.DropTable(
                name: "FollowMeRegistrations");

            migrationBuilder.DropTable(
                name: "Ingest",
                schema: "Logs");

            migrationBuilder.DropTable(
                name: "IngestCycles");

            migrationBuilder.DropTable(
                name: "Ingests");

            migrationBuilder.DropTable(
                name: "IntercomRegistrations");

            migrationBuilder.DropTable(
                name: "Mvc",
                schema: "Logs");

            migrationBuilder.DropTable(
                name: "NewClients");

            migrationBuilder.DropTable(
                name: "NumberDescriptions");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "OwnedPhoneNumbers");

            migrationBuilder.DropTable(
                name: "PhoneMenuOptions");

            migrationBuilder.DropTable(
                name: "PhoneNumberLookups");

            migrationBuilder.DropTable(
                name: "PhoneNumbers");

            migrationBuilder.DropTable(
                name: "PortedPhoneNumbers");

            migrationBuilder.DropTable(
                name: "PortRequests");

            migrationBuilder.DropTable(
                name: "ProductOrders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductShipments");

            migrationBuilder.DropTable(
                name: "PurchasedPhoneNumbers");

            migrationBuilder.DropTable(
                name: "SalesLeads");

            migrationBuilder.DropTable(
                name: "SentEmails");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "SpeedDialKeys");

            migrationBuilder.DropTable(
                name: "VerifiedPhoneNumbers");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
