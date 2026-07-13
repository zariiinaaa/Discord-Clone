using Discord.Core.DTOs.Servers.Responses;
using Discord.Core.Entities.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Application.Mappings
{
    public static class ServerMappings
    {
        public static ServerResponseDto ToResponseDto(this Server server)
        {
            ArgumentNullException.ThrowIfNull(server);

            return new ServerResponseDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                IconUrl = server.IconUrl,
                BannerUrl = server.BannerUrl,
                IsPublic = server.IsPublic,
                OwnerId = server.OwnerId,
                CreatedAt = server.CreatedAt
            };
        }

        public static ServerDetailsResponseDto ToDetailsResponseDto(
    this Server server)
        {
            ArgumentNullException.ThrowIfNull(server);

            return new ServerDetailsResponseDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                IconUrl = server.IconUrl,
                BannerUrl = server.BannerUrl,
                IsPublic = server.IsPublic,
                OwnerId = server.OwnerId,
                CreatedAt = server.CreatedAt,

                MemberCount = server.Members.Count,

                Channels = server.Channels
                    .OrderBy(channel =>
                        channel.ParentCategoryId ?? channel.Id)
                    .ThenBy(channel =>
                        channel.ParentCategoryId.HasValue ? 1 : 0)
                    .ThenBy(channel => channel.Position)
                    .Select(channel => channel.ToResponseDto())
                    .ToList()
            };
        }
    }
}
