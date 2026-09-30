using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class ehioruih : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Home = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ServeLocation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FloorZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TopZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientID);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyLogo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InchargeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InchargePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefaultPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PasswordResetExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    TechnicianID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    TechnicianCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Photo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDefaultPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PasswordResetExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LiveStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TotalJobsSolved = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateOfJoining = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceAreas = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Skills = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LicenseCertification = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.TechnicianID);
                    table.ForeignKey(
                        name: "FK_Technicians_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyWorkReports",
                columns: table => new
                {
                    DWRID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    DutyIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DutyOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingJobs = table.Column<int>(type: "int", nullable: true),
                    RepeatJobs = table.Column<int>(type: "int", nullable: true),
                    CompletedJobs = table.Column<int>(type: "int", nullable: true),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ToolBox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpareParts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Record = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkReports", x => x.DWRID);
                    table.ForeignKey(
                        name: "FK_DailyWorkReports_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkReports_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: true),
                    TechnicianID = table.Column<int>(type: "int", nullable: true),
                    CRNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CRID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientRequest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BidOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BroadcastZoneLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobID);
                    table.ForeignKey(
                        name: "FK_Jobs_Clients_ClientID",
                        column: x => x.ClientID,
                        principalTable: "Clients",
                        principalColumn: "ClientID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jobs_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jobs_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RelatedRecordId = table.Column<int>(type: "int", nullable: true),
                    RelatedModule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianLocations",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianLocations", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_TechnicianLocations_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchBids",
                columns: table => new
                {
                    BidID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    EstimatedArrival = table.Column<int>(type: "int", nullable: false),
                    BidTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsWinner = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchBids", x => x.BidID);
                    table.ForeignKey(
                        name: "FK_DispatchBids_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchBids_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobMessages",
                columns: table => new
                {
                    MessageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    SenderType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SenderID = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobMessages", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_JobMessages_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobStuckHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    StuckReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StuckRemarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GpsLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStuckHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStuckHistories_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobTracking",
                columns: table => new
                {
                    TrackingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    OnWayTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitRegistrationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FIRTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuoteTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FCRTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobStatusTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobClosureTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentStep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ETA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTracking", x => x.TrackingID);
                    table.ForeignKey(
                        name: "FK_JobTracking_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasterCards",
                columns: table => new
                {
                    MasterCardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    ClientID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCards", x => x.MasterCardID);
                    table.ForeignKey(
                        name: "FK_MasterCards_Clients_ClientID",
                        column: x => x.ClientID,
                        principalTable: "Clients",
                        principalColumn: "ClientID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterCards_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterCards_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterCards_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitRegistrations",
                columns: table => new
                {
                    UnitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    ClientID = table.Column<int>(type: "int", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModelNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AdminRemarks = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitRegistrations", x => x.UnitID);
                    table.ForeignKey(
                        name: "FK_UnitRegistrations_Clients_ClientID",
                        column: x => x.ClientID,
                        principalTable: "Clients",
                        principalColumn: "ClientID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitRegistrations_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitRegistrations_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProformaInvoices",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasterCardID = table.Column<int>(type: "int", nullable: false),
                    FIRNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncidentDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChatBox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicianPhoto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CircuitAHighPressure = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitALowPressure = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitAGT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitART = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitAPower = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitAAmpere = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBHighPressure = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBLowPressure = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBGT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBRT = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBPower = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CircuitBAmpere = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    VoucherNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoucherType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VoucherExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaInvoices", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_ProformaInvoices_MasterCards_MasterCardID",
                        column: x => x.MasterCardID,
                        principalTable: "MasterCards",
                        principalColumn: "MasterCardID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitRegistrationMedia",
                columns: table => new
                {
                    MediaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitID = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitRegistrationMedia", x => x.MediaID);
                    table.ForeignKey(
                        name: "FK_UnitRegistrationMedia_UnitRegistrations_UnitID",
                        column: x => x.UnitID,
                        principalTable: "UnitRegistrations",
                        principalColumn: "UnitID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProformaInvoiceItems",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaInvoiceItems", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceItems_ProformaInvoices_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "ProformaInvoices",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkReports_CompanyID",
                table: "DailyWorkReports",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkReports_TechnicianID",
                table: "DailyWorkReports",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBids_CompanyID",
                table: "DispatchBids",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBids_JobID",
                table: "DispatchBids",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_JobID",
                table: "Invoices",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_JobMessages_JobID",
                table: "JobMessages",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ClientID",
                table: "Jobs",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CompanyID",
                table: "Jobs",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_TechnicianID",
                table: "Jobs",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_JobStuckHistories_JobID",
                table: "JobStuckHistories",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_JobTracking_JobID",
                table: "JobTracking",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCards_ClientID",
                table: "MasterCards",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCards_CompanyID",
                table: "MasterCards",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCards_JobID",
                table: "MasterCards",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCards_TechnicianID",
                table: "MasterCards",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TechnicianId",
                table: "Notifications",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceItems_InvoiceID",
                table: "ProformaInvoiceItems",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_MasterCardID",
                table: "ProformaInvoices",
                column: "MasterCardID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianLocations_TechnicianID",
                table: "TechnicianLocations",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_CompanyID",
                table: "Technicians",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitRegistrationMedia_UnitID",
                table: "UnitRegistrationMedia",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitRegistrations_ClientID",
                table: "UnitRegistrations",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitRegistrations_JobID",
                table: "UnitRegistrations",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitRegistrations_TechnicianID",
                table: "UnitRegistrations",
                column: "TechnicianID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyWorkReports");

            migrationBuilder.DropTable(
                name: "DispatchBids");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "JobMessages");

            migrationBuilder.DropTable(
                name: "JobStuckHistories");

            migrationBuilder.DropTable(
                name: "JobTracking");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "ProformaInvoiceItems");

            migrationBuilder.DropTable(
                name: "TechnicianLocations");

            migrationBuilder.DropTable(
                name: "UnitRegistrationMedia");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "ProformaInvoices");

            migrationBuilder.DropTable(
                name: "UnitRegistrations");

            migrationBuilder.DropTable(
                name: "MasterCards");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
