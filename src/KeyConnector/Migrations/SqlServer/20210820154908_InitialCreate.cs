using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bit.KeyConnector.Migrations.SqlServer
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SymmetricKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessDate = table.Column<DateTime>(type: "datetime2", nullable: true)
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
