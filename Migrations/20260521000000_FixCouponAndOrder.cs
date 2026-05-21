// FILE: Migrations/20260521000000_FixCouponAndOrder.cs
// [FIX] Thêm StartDate, MaxUsage, UsageCount vào Coupons
//       Thêm CouponCode, DiscountAmount vào Orders
//       Thêm UNIQUE index trên Coupons.Code

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MilkStore.Migrations
{
    public partial class FixCouponAndOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Coupons ───────────────────────────────────────────
            // [FIX KM14] Thêm StartDate
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Coupons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // [FIX KM15, KM24] Thêm MaxUsage và UsageCount
            migrationBuilder.AddColumn<int>(
                name: "MaxUsage",
                table: "Coupons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Coupons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // [FIX KM02] UNIQUE constraint trên Code
            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            // ── Orders ────────────────────────────────────────────
            // [FIX KM22] Lưu mã coupon đã dùng và số tiền giảm
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Coupons_Code", table: "Coupons");
            migrationBuilder.DropColumn(name: "StartDate", table: "Coupons");
            migrationBuilder.DropColumn(name: "MaxUsage", table: "Coupons");
            migrationBuilder.DropColumn(name: "UsageCount", table: "Coupons");
            migrationBuilder.DropColumn(name: "CouponCode", table: "Orders");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Orders");
        }
    }
}
