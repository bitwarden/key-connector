using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Bit.KeyConnector.Migrations.PostgreSql
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymmetricKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevisionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastAccessDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKeys", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDatas");

            migrationBuilder.DropTable(
                name: "UserKeys");
        }
    }
}
