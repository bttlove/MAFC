using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pviBase.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToLoanNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InsuranceContracts_LoanNo",
                table: "InsuranceContracts",
                column: "LoanNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InsuranceContracts_LoanNo",
                table: "InsuranceContracts");
        }
    }
}
