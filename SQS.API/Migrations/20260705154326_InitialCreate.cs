using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace SQS.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "counters",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counters", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "daily_sequence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    seq_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_number = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_sequence", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "counter_services",
                columns: table => new
                {
                    counter_id = table.Column<int>(type: "int", nullable: false),
                    service_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counter_services", x => new { x.counter_id, x.service_id });
                    table.ForeignKey(
                        name: "FK_counter_services_counters_counter_id",
                        column: x => x.counter_id,
                        principalTable: "counters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_counter_services_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_admins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_customers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "staffs",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "Nhân viên"),
                    kpi = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staffs", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_staffs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ticket_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    id_customer = table.Column<int>(type: "int", nullable: true),
                    guest_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    id_service = table.Column<int>(type: "int", nullable: false),
                    id_counter = table.Column<int>(type: "int", nullable: true),
                    id_staff = table.Column<int>(type: "int", nullable: true),
                    ticket_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    called_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Waiting")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                    table.ForeignKey(
                        name: "FK_tickets_counters_id_counter",
                        column: x => x.id_counter,
                        principalTable: "counters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_customers_id_customer",
                        column: x => x.id_customer,
                        principalTable: "customers",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tickets_services_id_service",
                        column: x => x.id_service,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_staffs_id_staff",
                        column: x => x.id_staff,
                        principalTable: "staffs",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_counter_services_service_id",
                table: "counter_services",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_sequence_seq_date",
                table: "daily_sequence",
                column: "seq_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_services_code",
                table: "services",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tickets_customer_date",
                table: "tickets",
                columns: new[] { "id_customer", "ticket_date" });

            migrationBuilder.CreateIndex(
                name: "idx_tickets_date_status",
                table: "tickets",
                columns: new[] { "ticket_date", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_tickets_service_status_created",
                table: "tickets",
                columns: new[] { "id_service", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_tickets_status_counter",
                table: "tickets",
                columns: new[] { "status", "id_counter" });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_counter",
                table: "tickets",
                column: "id_counter");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_id_staff",
                table: "tickets",
                column: "id_staff");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropTable(
                name: "counter_services");

            migrationBuilder.DropTable(
                name: "daily_sequence");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "counters");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "staffs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
