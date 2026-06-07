using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBanHang.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsageCount",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerCustomer",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PromotionUsages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PromotionId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionUsages_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_PromotionId",
                table: "PromotionUsages",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "MaxUsageCount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerCustomer",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "Promotions");
        }
    }
}
