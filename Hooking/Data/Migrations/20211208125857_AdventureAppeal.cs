﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hooking.Data.Migrations
{
    public partial class AdventureAppeal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdventureAppeal",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RowVersion = table.Column<byte[]>(rowVersion: true, nullable: true),
                    AdventureId = table.Column<string>(nullable: true),
                    AppealContent = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureAppeal", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdventureAppeal");
        }
    }
}
