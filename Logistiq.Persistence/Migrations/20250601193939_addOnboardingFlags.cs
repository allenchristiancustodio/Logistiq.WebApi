using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistiq.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addOnboardingFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedSetup",
                table: "Organizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SetupCompletedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedOnboarding",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAt",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCompletedSetup",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SetupCompletedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "HasCompletedOnboarding",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "ApplicationUsers");
        }
    }
}
