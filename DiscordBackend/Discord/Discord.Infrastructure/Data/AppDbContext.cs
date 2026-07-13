using Discord.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Discord.Core.Entities.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data
{
    public class AppDbContext: DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Server> Servers => Set<Server>();

        public DbSet<ServerMember> ServerMembers =>Set<ServerMember>();

        public DbSet<Channel> Channels => Set<Channel>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
