using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServerRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDisplayedSeparately = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsMentionable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerRoles_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerMemberRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerMemberId = table.Column<int>(type: "int", nullable: false),
                    ServerRoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMemberRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerMemberRoles_ServerMembers_ServerMemberId",
                        column: x => x.ServerMemberId,
                        principalTable: "ServerMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServerMemberRoles_ServerRoles_ServerRoleId",
                        column: x => x.ServerRoleId,
                        principalTable: "ServerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerRolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerRoleId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerRolePermissions_ServerRoles_ServerRoleId",
                        column: x => x.ServerRoleId,
                        principalTable: "ServerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerMemberRoles_ServerMemberId_ServerRoleId",
                table: "ServerMemberRoles",
                columns: new[] { "ServerMemberId", "ServerRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerMemberRoles_ServerRoleId",
                table: "ServerMemberRoles",
                column: "ServerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerRolePermissions_ServerRoleId_Permission",
                table: "ServerRolePermissions",
                columns: new[] { "ServerRoleId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerRoles_ServerId",
                table: "ServerRoles",
                column: "ServerId",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ServerRoles_ServerId_Position",
                table: "ServerRoles",
                columns: new[] { "ServerId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerMemberRoles");

            migrationBuilder.DropTable(
                name: "ServerRolePermissions");

            migrationBuilder.DropTable(
                name: "ServerRoles");
        }
    }
}
