using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Blackhole.Server.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Helo = table.Column<string>(type: "text", nullable: true),
                    MailFrom = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    HeaderFrom = table.Column<string>(type: "text", nullable: true),
                    HeaderTo = table.Column<string>(type: "text", nullable: true),
                    TextBody = table.Column<string>(type: "text", nullable: true),
                    HtmlBody = table.Column<string>(type: "text", nullable: true),
                    Raw = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_recipients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_recipients_emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_recipients_Address",
                table: "email_recipients",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_email_recipients_EmailId",
                table: "email_recipients",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_emails_MailFrom",
                table: "emails",
                column: "MailFrom");

            migrationBuilder.CreateIndex(
                name: "IX_emails_ReceivedAtUtc",
                table: "emails",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_emails_Subject",
                table: "emails",
                column: "Subject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_recipients");

            migrationBuilder.DropTable(
                name: "emails");
        }
    }
}
