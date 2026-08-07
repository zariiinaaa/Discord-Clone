using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelPermissionOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMemberPermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMemberPermissionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMemberPermissionOverrides_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelMemberPermissionOverrides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChannelRolePermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelRolePermissionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelRolePermissionOverrides_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelRolePermissionOverrides_ServerRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ServerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberPermissionOverrides_ChannelId_UserId_Permission",
                table: "ChannelMemberPermissionOverrides",
                columns: new[] { "ChannelId", "UserId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberPermissionOverrides_UserId",
                table: "ChannelMemberPermissionOverrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRolePermissionOverrides_ChannelId_RoleId_Permission",
                table: "ChannelRolePermissionOverrides",
                columns: new[] { "ChannelId", "RoleId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRolePermissionOverrides_RoleId",
                table: "ChannelRolePermissionOverrides",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMemberPermissionOverrides");

            migrationBuilder.DropTable(
                name: "ChannelRolePermissionOverrides");
        }
    }
}
