using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doclyn.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DOCUMENT_CLASSES",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DISPLAY_NAME = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GROUP_NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SUB_GROUP = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IS_SYSTEM_DEFINED = table.Column<bool>(type: "boolean", nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_CLASSES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_CLASS_EXAMPLES",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_CLASS_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    CONFIDENCE = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_CLASS_EXAMPLES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_CLASS_EXAMPLES_DOCUMENTS_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "DOCUMENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_CLASS_EXAMPLES_DOCUMENT_CLASSES_DOCUMENT_CLASS_ID",
                        column: x => x.DOCUMENT_CLASS_ID,
                        principalTable: "DOCUMENT_CLASSES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_EXAMPLES_DOCUMENT_CLASS_ID",
                table: "DOCUMENT_CLASS_EXAMPLES",
                column: "DOCUMENT_CLASS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_EXAMPLES_DOCUMENT_ID",
                table: "DOCUMENT_CLASS_EXAMPLES",
                column: "DOCUMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASSES_GROUP_NAME",
                table: "DOCUMENT_CLASSES",
                column: "GROUP_NAME");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASSES_IS_ACTIVE",
                table: "DOCUMENT_CLASSES",
                column: "IS_ACTIVE");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASSES_NAME",
                table: "DOCUMENT_CLASSES",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASSES_SUB_GROUP",
                table: "DOCUMENT_CLASSES",
                column: "SUB_GROUP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOCUMENT_CLASS_EXAMPLES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_CLASSES");
        }
    }
}
