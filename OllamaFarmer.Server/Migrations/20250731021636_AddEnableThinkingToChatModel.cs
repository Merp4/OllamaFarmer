using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OllamaFarmer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableThinkingToChatModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableThinking",
                table: "Models",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableThinking",
                table: "Models");
        }
    }
}
