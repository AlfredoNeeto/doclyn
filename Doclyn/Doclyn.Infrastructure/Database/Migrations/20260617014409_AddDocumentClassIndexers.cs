using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doclyn.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentClassIndexers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DOCUMENT_CLASS_INDEXERS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_CLASS_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DISPLAY_NAME = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DATA_TYPE = table.Column<int>(type: "integer", nullable: false),
                    IS_REQUIRED = table.Column<bool>(type: "boolean", nullable: false),
                    IS_MULTIPLE = table.Column<bool>(type: "boolean", nullable: false),
                    EXTRACTION_HINT = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    REGEX_PATTERN = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_CLASS_INDEXERS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_CLASS_INDEXERS_DOCUMENT_CLASSES_DOCUMENT_CLASS_ID",
                        column: x => x.DOCUMENT_CLASS_ID,
                        principalTable: "DOCUMENT_CLASSES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_INDEXERS_DOCUMENT_CLASS_ID",
                table: "DOCUMENT_CLASS_INDEXERS",
                column: "DOCUMENT_CLASS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_INDEXERS_DOCUMENT_CLASS_ID_NAME_IS_ACTIVE",
                table: "DOCUMENT_CLASS_INDEXERS",
                columns: new[] { "DOCUMENT_CLASS_ID", "NAME", "IS_ACTIVE" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_INDEXERS_IS_ACTIVE",
                table: "DOCUMENT_CLASS_INDEXERS",
                column: "IS_ACTIVE");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_CLASS_INDEXERS_NAME",
                table: "DOCUMENT_CLASS_INDEXERS",
                column: "NAME");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOCUMENT_CLASS_INDEXERS");
        }
    }
}
