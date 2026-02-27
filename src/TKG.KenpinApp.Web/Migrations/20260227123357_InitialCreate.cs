using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TKG.KenpinApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_APP_LOG",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    log_datetime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    action_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    screen_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    detail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_APP_LOG", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "T_KENPIN_LOCK",
                columns: table => new
                {
                    lock_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seiri_no = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    shukka_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    locked_by = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    locked_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    timeout_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    released_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_KENPIN_LOCK", x => x.lock_id);
                });

            migrationBuilder.CreateTable(
                name: "T_KENPIN_SESSION",
                columns: table => new
                {
                    session_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    seiri_no = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    shukka_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    kenpin_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    user_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    site_code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    warehouse_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    total_items = table.Column<int>(type: "INTEGER", nullable: true),
                    total_qty = table.Column<int>(type: "INTEGER", nullable: true),
                    scanned_qty = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    d365_synced = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_KENPIN_SESSION", x => x.session_id);
                });

            migrationBuilder.CreateTable(
                name: "T_USER_SESSION",
                columns: table => new
                {
                    session_token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    user_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    site_code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    login_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_USER_SESSION", x => x.session_token);
                });

            migrationBuilder.CreateTable(
                name: "T_DENPYO_KENPIN",
                columns: table => new
                {
                    denpyo_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<long>(type: "INTEGER", nullable: false),
                    denpyo_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    verified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    result = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_DENPYO_KENPIN", x => x.denpyo_id);
                    table.ForeignKey(
                        name: "FK_T_DENPYO_KENPIN_T_KENPIN_SESSION_session_id",
                        column: x => x.session_id,
                        principalTable: "T_KENPIN_SESSION",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "T_KENPIN_DETAIL",
                columns: table => new
                {
                    detail_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<long>(type: "INTEGER", nullable: false),
                    item_code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    kenpin_category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    scan_qty = table.Column<int>(type: "INTEGER", nullable: false),
                    scan_datetime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    scan_method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    cancel_flg = table.Column<bool>(type: "INTEGER", nullable: false),
                    cancel_datetime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_KENPIN_DETAIL", x => x.detail_id);
                    table.ForeignKey(
                        name: "FK_T_KENPIN_DETAIL_T_KENPIN_SESSION_session_id",
                        column: x => x.session_id,
                        principalTable: "T_KENPIN_SESSION",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_APP_LOG_log_datetime",
                table: "T_APP_LOG",
                column: "log_datetime");

            migrationBuilder.CreateIndex(
                name: "IX_T_APP_LOG_user_code",
                table: "T_APP_LOG",
                column: "user_code");

            migrationBuilder.CreateIndex(
                name: "IX_T_DENPYO_KENPIN_session_id",
                table: "T_DENPYO_KENPIN",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_T_KENPIN_DETAIL_barcode",
                table: "T_KENPIN_DETAIL",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "IX_T_KENPIN_DETAIL_session_id",
                table: "T_KENPIN_DETAIL",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_T_KENPIN_LOCK_seiri_no_shukka_date",
                table: "T_KENPIN_LOCK",
                columns: new[] { "seiri_no", "shukka_date" });

            migrationBuilder.CreateIndex(
                name: "IX_T_KENPIN_SESSION_seiri_no",
                table: "T_KENPIN_SESSION",
                column: "seiri_no");

            migrationBuilder.CreateIndex(
                name: "IX_T_KENPIN_SESSION_status",
                table: "T_KENPIN_SESSION",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_T_USER_SESSION_user_code",
                table: "T_USER_SESSION",
                column: "user_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_APP_LOG");

            migrationBuilder.DropTable(
                name: "T_DENPYO_KENPIN");

            migrationBuilder.DropTable(
                name: "T_KENPIN_DETAIL");

            migrationBuilder.DropTable(
                name: "T_KENPIN_LOCK");

            migrationBuilder.DropTable(
                name: "T_USER_SESSION");

            migrationBuilder.DropTable(
                name: "T_KENPIN_SESSION");
        }
    }
}
