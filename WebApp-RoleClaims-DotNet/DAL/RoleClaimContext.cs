using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebApp_RoleClaims_DotNet.Models;

namespace WebApp_RoleClaims_DotNet.DAL
{
    public class RoleClaimContext : DbContext
    {
        public RoleClaimContext() : base("RoleClaimContext") { }

        public DbSet<Task> Tasks { get; set; }
        public DbSet<TokenCacheEntry> TokenCacheEntries { get; set; }
    }
}