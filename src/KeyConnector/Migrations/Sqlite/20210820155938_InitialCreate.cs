using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bit.KeyConnector.Migrations.Sqlite
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SymmetricKey = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevisionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessDate = table.Column<DateTime>(type: "TEXT", nullable: true)
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
