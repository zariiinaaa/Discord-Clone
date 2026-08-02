using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "ConversationMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReadMessageId",
                table: "ConversationMembers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "ConversationMembers");

            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "ConversationMembers");
        }
    }
}
