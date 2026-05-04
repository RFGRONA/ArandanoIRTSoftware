using System;
using ArandanoIRT.Web._0_Domain.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArandanoIRT.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToEmpiricalBaselines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "baseline_twet",
                table: "analysis_results",
                newName: "baseline_ll");

            migrationBuilder.RenameColumn(
                name: "baseline_tdry",
                table: "analysis_results",
                newName: "baseline_ul");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "baseline_ll",
                table: "analysis_results",
                newName: "baseline_twet");

            migrationBuilder.RenameColumn(
                name: "baseline_ul",
                table: "analysis_results",
                newName: "baseline_tdry");
        }
    }
}
