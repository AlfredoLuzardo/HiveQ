using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveQ.Migrations
{
    /// <inheritdoc />
    public partial class AddPartySizeFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueHistories_Queues_QueueId",
                table: "QueueHistories");

            migrationBuilder.AddColumn<int>(
                name: "MaxPartySize",
                table: "Queues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartySize",
                table: "QueueEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Queues",
                keyColumn: "QueueId",
                keyValue: 1,
                column: "MaxPartySize",
                value: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueHistories_Queues_QueueId",
                table: "QueueHistories",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "QueueId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueHistories_Queues_QueueId",
                table: "QueueHistories");

            migrationBuilder.DropColumn(
                name: "MaxPartySize",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "PartySize",
                table: "QueueEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueHistories_Queues_QueueId",
                table: "QueueHistories",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "QueueId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
