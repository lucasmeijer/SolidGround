using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolidGround.Migrations
{
    /// <inheritdoc />
    public partial class add_is_reference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReference",
                table: "Executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReference",
                table: "Executions");
        }
    }
}
