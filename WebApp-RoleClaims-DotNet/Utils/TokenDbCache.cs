using Microsoft.IdentityModel.Clients.ActiveDirectory;
//using Microsoft.IdentityModel.Clients.ActiveDirectory;
//using WebApp_RoleClaims_DotNet.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebApp_RoleClaims_DotNet.DAL;
using WebApp_RoleClaims_DotNet.Models;

namespace WebApp_RoleClaims_DotNet.Utils
{
    public class TokenDbCache : TokenCache
    {
        private RoleClaimContext db = new RoleClaimContext();
        string userObjId;
        TokenCacheEntry Cache;

        public TokenDbCache(string userObjectId)
        {
           // associate the cache to the current user of the web app
            userObjId = userObjectId;
            
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.BeforeWrite = BeforeWriteNotification;

            // look up the entry in the DB
            Cache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
            // place the entry in memory
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }

        // clean the db of all tokens associated with the user.
        public override void Clear()
        {
            base.Clear();

            var tokens = from e in db.TokenCacheEntries
                         where (e.userObjId == userObjId)
                         select e;

            foreach (var cacheEntry in tokens.ToList())
                db.TokenCacheEntries.Remove(cacheEntry);
            db.SaveChanges();
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (Cache == null)
            {
                // first time access
                Cache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
            }
            else
            {   // retrieve last write from the DB
                var status = from e in db.TokenCacheEntries
                             where (e.userObjId == userObjId)
                             select new
                             {
                                 LastWrite = e.LastWrite
                             };
                // if the in-memory copy is older than the persistent copy
                if (status.First().LastWrite > Cache.LastWrite)
                //// read from from storage, update in-memory copy
                {
                    Cache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
                }
            }
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }
        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (this.HasStateChanged)
            {
                Cache = new TokenCacheEntry
                {
                    userObjId = userObjId,
                    cacheBits = this.Serialize(),
                    LastWrite = DateTime.Now
                };
                //// update the DB and the lastwrite                
                db.Entry(Cache).State = Cache.TokenCacheEntryID == 0 ? EntityState.Added : EntityState.Modified;                
                db.SaveChanges();
                this.HasStateChanged = false;
            }
        }
        void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }
    }
}