using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TKG.KenpinApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddD365SyncQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_D365_SYNC_QUEUE",
                columns: table => new
                {
                    queue_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<long>(type: "INTEGER", nullable: false),
                    sync_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false),
                    max_retries = table.Column<int>(type: "INTEGER", nullable: false),
                    next_retry_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_D365_SYNC_QUEUE", x => x.queue_id);
                    table.ForeignKey(
                        name: "FK_T_D365_SYNC_QUEUE_T_KENPIN_SESSION_session_id",
                        column: x => x.session_id,
                        principalTable: "T_KENPIN_SESSION",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_D365_SYNC_QUEUE_next_retry_at",
                table: "T_D365_SYNC_QUEUE",
                column: "next_retry_at");

            migrationBuilder.CreateIndex(
                name: "IX_T_D365_SYNC_QUEUE_session_id",
                table: "T_D365_SYNC_QUEUE",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_T_D365_SYNC_QUEUE_status",
                table: "T_D365_SYNC_QUEUE",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_D365_SYNC_QUEUE");
        }
    }
}
