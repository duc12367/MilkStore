// ============================================================
// FILE: Migrations/20260521000001_AddIsBlockedToUsers.cs
// MỤC ĐÍCH: Thêm cột IsBlocked vào bảng Users
// FIX: TC10 (Khóa tài khoản), TC11 (Mở khóa tài khoản)
//
// Chạy: dotnet ef database update
// ============================================================

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MilkStore.Migrations;

/// <inheritdoc />
public partial class AddIsBlockedToUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsBlocked",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsBlocked",
            table: "Users");
    }
}