using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    public partial class AddChannelToServerInvites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServerInvites_ServerId",
                table: "ServerInvites");

            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "ServerInvites",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE invites
                SET invites.ChannelId = selectedChannel.Id
                FROM ServerInvites AS invites
                CROSS APPLY
                (
                    SELECT TOP 1 channels.Id
                    FROM Channels AS channels
                    WHERE channels.ServerId = invites.ServerId
                      AND channels.[Type] <> 2
                    ORDER BY
                        CASE WHEN channels.IsPrivate = 0 THEN 0 ELSE 1 END,
                        CASE WHEN channels.[Type] = 0 THEN 0 ELSE 1 END,
                        channels.Position,
                        channels.Id
                ) AS selectedChannel;
                """);

            migrationBuilder.Sql("""
                DELETE FROM ServerInvites
                WHERE ChannelId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ChannelId",
                table: "ServerInvites",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerInvites_ChannelId",
                table: "ServerInvites",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerInvites_ServerId_ChannelId",
                table: "ServerInvites",
                columns: new[] { "ServerId", "ChannelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServerInvites_Channels_ChannelId",
                table: "ServerInvites",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerInvites_Channels_ChannelId",
                table: "ServerInvites");

            migrationBuilder.DropIndex(
                name: "IX_ServerInvites_ChannelId",
                table: "ServerInvites");

            migrationBuilder.DropIndex(
                name: "IX_ServerInvites_ServerId_ChannelId",
                table: "ServerInvites");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "ServerInvites");

            migrationBuilder.CreateIndex(
                name: "IX_ServerInvites_ServerId",
                table: "ServerInvites",
                column: "ServerId");
        }
    }
}