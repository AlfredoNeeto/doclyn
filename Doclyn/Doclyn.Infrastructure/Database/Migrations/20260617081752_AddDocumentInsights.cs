using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doclyn.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DOCUMENT_INSIGHTS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    TYPE = table.Column<int>(type: "integer", nullable: false),
                    SEVERITY = table.Column<int>(type: "integer", nullable: false),
                    TITLE = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MESSAGE = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CONFIDENCE = table.Column<decimal>(type: "numeric", nullable: false),
                    SOURCE = table.Column<int>(type: "integer", nullable: false),
                    RELATED_FIELD_NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_INSIGHTS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_INSIGHTS_DOCUMENTS_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "DOCUMENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_INSIGHTS_CREATED_AT",
                table: "DOCUMENT_INSIGHTS",
                column: "CREATED_AT");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_INSIGHTS_DOCUMENT_ID",
                table: "DOCUMENT_INSIGHTS",
                column: "DOCUMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_INSIGHTS_SEVERITY",
                table: "DOCUMENT_INSIGHTS",
                column: "SEVERITY");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_INSIGHTS_TYPE",
                table: "DOCUMENT_INSIGHTS",
                column: "TYPE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOCUMENT_INSIGHTS");
        }
    }
}
