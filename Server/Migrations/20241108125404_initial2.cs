using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolidGround.Migrations
{
    /// <inheritdoc />
    public partial class initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StringVariable_Executions_ExecutionId",
                table: "StringVariable");

            migrationBuilder.DropForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable");

            migrationBuilder.AddForeignKey(
                name: "FK_StringVariable_Executions_ExecutionId",
                table: "StringVariable",
                column: "ExecutionId",
                principalTable: "Executions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StringVariable_Outputs_OutputId",
                table: "StringVariable",
                column: "OutputId",
                principalTable: "Outputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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
    }
}
