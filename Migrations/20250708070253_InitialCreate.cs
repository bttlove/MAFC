using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pviBase.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoanType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustBirthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustGender = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    CustIdNo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CustAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustPhone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoanAmount = table.Column<long>(type: "bigint", nullable: false),
                    LoanTerm = table.Column<int>(type: "int", nullable: false),
                    InsRate = table.Column<double>(type: "float", nullable: false),
                    DisbursementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceContracts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceContracts");
        }
    }
}
