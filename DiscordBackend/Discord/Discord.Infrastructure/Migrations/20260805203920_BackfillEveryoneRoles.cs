using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillEveryoneRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
        DECLARE @CreatedRoles TABLE
        (
            [Id] int NOT NULL
        );

        INSERT INTO [ServerRoles]
        (
            [Name],
            [ColorHex],
            [Position],
            [IsDefault],
            [IsDisplayedSeparately],
            [IsMentionable],
            [ServerId],
            [CreatedAt],
            [UpdatedAt]
        )
        OUTPUT INSERTED.[Id]
            INTO @CreatedRoles ([Id])
        SELECT
            N'@everyone',
            NULL,
            0,
            1,
            0,
            0,
            server.[Id],
            SYSUTCDATETIME(),
            NULL
        FROM [Servers] AS server
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [ServerRoles] AS role
            WHERE role.[ServerId] = server.[Id]
              AND role.[IsDefault] = 1
        );

        INSERT INTO [ServerRolePermissions]
        (
            [ServerRoleId],
            [Permission],
            [CreatedAt],
            [UpdatedAt]
        )
        SELECT
            createdRole.[Id],
            permissionValue.[Permission],
            SYSUTCDATETIME(),
            NULL
        FROM @CreatedRoles AS createdRole
        CROSS JOIN
        (
            VALUES
                (1),
                (5),
                (9),
                (11),
                (13),
                (14),
                (15),
                (16),
                (18),
                (19)
        ) AS permissionValue ([Permission]);
        """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
