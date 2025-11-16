using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HiveQ.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompanyDescription = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CompanyCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    QRCodeData = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    QRCodeImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedWaitTimePerPerson = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentQueueSize = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalServedToday = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_Queues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    QueueEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ServedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedWaitTime = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationPreference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.QueueEntryId);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueueEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "QueueEntryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QueueHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueEntryId = table.Column<int>(type: "INTEGER", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ServedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WaitTime = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_QueueHistories_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "QueueEntryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QueueHistories_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CompanyAddress", "CompanyCategory", "CompanyDescription", "CompanyName", "CreatedAt", "Email", "FirstName", "IsActive", "IsVerified", "LastName", "LogoUrl", "PasswordHash", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, "123 Main St, City, State 12345", "Food & Beverage", "A cozy coffee shop in the heart of the city", "Sample Coffee Shop", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "owner@coffeeshop.com", "Sarah", true, true, "Johnson", null, "hashed_password_here", "5551234567" },
                    { 2, null, null, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "customer@example.com", "John", true, false, "Doe", null, "hashed_password_here", "5559876543" }
                });

            migrationBuilder.InsertData(
                table: "Queues",
                columns: new[] { "QueueId", "CreatedAt", "CurrentQueueSize", "Description", "EstimatedWaitTimePerPerson", "IsActive", "MaxCapacity", "QRCodeData", "QRCodeImageUrl", "QueueName", "Status", "TotalServedToday", "UpdatedAt", "UserId" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, "Main service queue for morning hours", 5, true, 50, "HIVEQ_QUEUE_1", null, "Morning Service", "Active", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_QueueEntryId",
                table: "Notifications",
                column: "QueueEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueId",
                table: "QueueEntries",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_UserId",
                table: "QueueEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueHistories_QueueEntryId",
                table: "QueueHistories",
                column: "QueueEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueHistories_QueueId",
                table: "QueueHistories",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueHistories_UserId",
                table: "QueueHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_QRCodeData",
                table: "Queues",
                column: "QRCodeData",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queues_UserId",
                table: "Queues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "QueueHistories");

            migrationBuilder.DropTable(
                name: "QueueEntries");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
