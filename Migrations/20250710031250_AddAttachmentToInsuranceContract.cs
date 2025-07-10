using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pviBase.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentToInsuranceContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                table: "InsuranceContracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AttachmentData",
                table: "InsuranceContracts",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "InsuranceContracts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                table: "InsuranceContracts");

            migrationBuilder.DropColumn(
                name: "AttachmentData",
                table: "InsuranceContracts");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "InsuranceContracts");
        }
    }
}
