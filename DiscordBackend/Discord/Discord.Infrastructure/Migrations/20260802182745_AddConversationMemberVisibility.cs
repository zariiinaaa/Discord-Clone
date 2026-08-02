using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationMemberVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleInList",
                table: "ConversationMembers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisibleInList",
                table: "ConversationMembers");
        }
    }
}
