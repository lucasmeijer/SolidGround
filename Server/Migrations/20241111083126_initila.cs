using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolidGround.Migrations
{
    /// <inheritdoc />
    public partial class initila : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsReference = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SolidGroundInitiated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OriginalRequest_Route = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OriginalRequest_QueryString = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalRequest_ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OriginalRequest_Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    OriginalRequest_Body = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inputs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InputFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InputId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Bytes = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputFiles_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InputStrings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InputId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputStrings_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExecutionId = table.Column<int>(type: "INTEGER", nullable: false),
                    InputId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outputs_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Outputs_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InputSetInputs",
                columns: table => new
                {
                    InputsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputSetInputs", x => new { x.InputsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_InputSetInputs_Inputs_InputsId",
                        column: x => x.InputsId,
                        principalTable: "Inputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputSetInputs_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutputId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutputComponents_Outputs_OutputId",
                        column: x => x.OutputId,
                        principalTable: "Outputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StringVariable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutputId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExecutionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StringVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StringVariable_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StringVariable_Outputs_OutputId",
                        column: x => x.OutputId,
                        principalTable: "Outputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InputFiles_InputId",
                table: "InputFiles",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_InputSetInputs_TagsId",
                table: "InputSetInputs",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_InputStrings_InputId",
                table: "InputStrings",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputComponents_OutputId",
                table: "OutputComponents",
                column: "OutputId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_ExecutionId",
                table: "Outputs",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_InputId",
                table: "Outputs",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_StringVariable_ExecutionId",
                table: "StringVariable",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StringVariable_OutputId",
                table: "StringVariable",
                column: "OutputId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputFiles");

            migrationBuilder.DropTable(
                name: "InputSetInputs");

            migrationBuilder.DropTable(
                name: "InputStrings");

            migrationBuilder.DropTable(
                name: "OutputComponents");

            migrationBuilder.DropTable(
                name: "StringVariable");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "Inputs");
        }
    }
}
