using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OllamaFarmer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Consultants",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Consultants_ChatServerId_Name",
                table: "Consultants",
                columns: new[] { "ChatServerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Consultants_ChatServerId_Name",
                table: "Consultants");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Consultants");
        }
    }
}
