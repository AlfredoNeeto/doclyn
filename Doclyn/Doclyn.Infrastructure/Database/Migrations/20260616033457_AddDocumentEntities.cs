using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doclyn.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "USERS");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "REFRESH_TOKENS");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "USERS",
                newName: "ROLE");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "USERS",
                newName: "NAME");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "USERS",
                newName: "EMAIL");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "USERS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "USERS",
                newName: "UPDATED_AT");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "USERS",
                newName: "PASSWORD_HASH");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "USERS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "USERS",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "USERS",
                newName: "IX_USERS_EMAIL");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "REFRESH_TOKENS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "REFRESH_TOKENS",
                newName: "USER_ID");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "REFRESH_TOKENS",
                newName: "UPDATED_AT");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "REFRESH_TOKENS",
                newName: "TOKEN_HASH");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                table: "REFRESH_TOKENS",
                newName: "REVOKED_AT");

            migrationBuilder.RenameColumn(
                name: "ReplacedByTokenHash",
                table: "REFRESH_TOKENS",
                newName: "REPLACED_BY_TOKEN_HASH");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "REFRESH_TOKENS",
                newName: "EXPIRES_AT");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "REFRESH_TOKENS",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "REFRESH_TOKENS",
                newName: "IX_REFRESH_TOKENS_USER_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "REFRESH_TOKENS",
                newName: "IX_REFRESH_TOKENS_TOKEN_HASH");

            migrationBuilder.AddPrimaryKey(
                name: "PK_USERS",
                table: "USERS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_REFRESH_TOKENS",
                table: "REFRESH_TOKENS",
                column: "ID");

            migrationBuilder.CreateTable(
                name: "DOCUMENTS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    USER_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    FILE_NAME = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FILE_HASH = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    STORAGE_PATH = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DOCUMENT_TYPE = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DOCUMENT_STATUS = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PROCESSED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENTS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DOCUMENTS__USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PASSWORD_RESET_REQUESTS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    USER_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    CODE_HASH = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RESET_TOKEN_HASH = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RESET_TOKEN_EXPIRES_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ATTEMPTS = table.Column<int>(type: "integer", nullable: false),
                    IS_USED = table.Column<bool>(type: "boolean", nullable: false),
                    IS_RESET_TOKEN_USED = table.Column<bool>(type: "boolean", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PASSWORD_RESET_REQUESTS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PASSWORD_RESET_REQUESTS__USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EXTRACTED_DATA",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DATA = table.Column<string>(type: "jsonb", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXTRACTED_DATA", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EXTRACTED_DATA_DOCUMENTS_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "DOCUMENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROCESSING_LOGS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOCUMENT_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    STEP = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MESSAGE = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    STATUS = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROCESSING_LOGS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PROCESSING_LOGS_DOCUMENTS_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "DOCUMENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_REFRESH_TOKENS_EXPIRES_AT",
                table: "REFRESH_TOKENS",
                column: "EXPIRES_AT");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_USER_ID_DOCUMENT_STATUS_DOCUMENT_TYPE_CREATED_AT",
                table: "DOCUMENTS",
                columns: new[] { "USER_ID", "DOCUMENT_STATUS", "DOCUMENT_TYPE", "CREATED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_EXTRACTED_DATA_DOCUMENT_ID",
                table: "EXTRACTED_DATA",
                column: "DOCUMENT_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PASSWORD_RESET_REQUESTS_RESET_TOKEN_HASH",
                table: "PASSWORD_RESET_REQUESTS",
                column: "RESET_TOKEN_HASH",
                unique: true,
                filter: "\"RESET_TOKEN_HASH\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_PASSWORD_RESET_REQUESTS_USER_ID",
                table: "PASSWORD_RESET_REQUESTS",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCESSING_LOGS_DOCUMENT_ID",
                table: "PROCESSING_LOGS",
                column: "DOCUMENT_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_REFRESH_TOKENS__USERS_USER_ID",
                table: "REFRESH_TOKENS",
                column: "USER_ID",
                principalTable: "USERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_REFRESH_TOKENS__USERS_USER_ID",
                table: "REFRESH_TOKENS");

            migrationBuilder.DropTable(
                name: "EXTRACTED_DATA");

            migrationBuilder.DropTable(
                name: "PASSWORD_RESET_REQUESTS");

            migrationBuilder.DropTable(
                name: "PROCESSING_LOGS");

            migrationBuilder.DropTable(
                name: "DOCUMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_USERS",
                table: "USERS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_REFRESH_TOKENS",
                table: "REFRESH_TOKENS");

            migrationBuilder.DropIndex(
                name: "IX_REFRESH_TOKENS_EXPIRES_AT",
                table: "REFRESH_TOKENS");

            migrationBuilder.RenameTable(
                name: "USERS",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "REFRESH_TOKENS",
                newName: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "ROLE",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "NAME",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "EMAIL",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "UPDATED_AT",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "PASSWORD_HASH",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "IS_ACTIVE",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "CREATED_AT",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_USERS_EMAIL",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "RefreshTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "USER_ID",
                table: "RefreshTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "UPDATED_AT",
                table: "RefreshTokens",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "TOKEN_HASH",
                table: "RefreshTokens",
                newName: "TokenHash");

            migrationBuilder.RenameColumn(
                name: "REVOKED_AT",
                table: "RefreshTokens",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "REPLACED_BY_TOKEN_HASH",
                table: "RefreshTokens",
                newName: "ReplacedByTokenHash");

            migrationBuilder.RenameColumn(
                name: "EXPIRES_AT",
                table: "RefreshTokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "CREATED_AT",
                table: "RefreshTokens",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_REFRESH_TOKENS_USER_ID",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_REFRESH_TOKENS_TOKEN_HASH",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_TokenHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
