using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolidGround.Migrations
{
    /// <inheritdoc />
    public partial class TagPreventAutoDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PreventAutoDelete",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreventAutoDelete",
                table: "Tags");
        }
    }
}
