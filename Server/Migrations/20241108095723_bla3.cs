using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class bla3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable");

            migrationBuilder.AlterColumn<int>(
                name: "OutputId",
                table: "StringVariable",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ExecutionId",
                table: "StringVariable",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputId",
                table: "Executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SolidGroundInitiated",
                table: "Executions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StringVariable_ExecutionId",
                table: "StringVariable",
                column: "ExecutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StringVariable_Executions_ExecutionId",
                table: "StringVariable",
                column: "ExecutionId",
                principalTable: "Executions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable",
                column: "OutputId",
                principalTable: "Outputs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StringVariable_Executions_ExecutionId",
                table: "StringVariable");

            migrationBuilder.DropForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable");

            migrationBuilder.DropIndex(
                name: "IX_StringVariable_ExecutionId",
                table: "StringVariable");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "StringVariable");

            migrationBuilder.DropColumn(
                name: "InputId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "SolidGroundInitiated",
                table: "Executions");

            migrationBuilder.AlterColumn<int>(
                name: "OutputId",
                table: "StringVariable",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable",
                column: "OutputId",
                principalTable: "Outputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
