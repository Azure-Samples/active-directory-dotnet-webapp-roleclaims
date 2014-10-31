namespace WebApp_RoleClaims_DotNet.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tasks",
                c => new
                    {
                        TaskID = c.Int(nullable: false, identity: true),
                        TaskText = c.String(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.TaskID);
            
            CreateTable(
                "dbo.TokenCacheEntries",
                c => new
                    {
                        TokenCacheEntryID = c.Int(nullable: false, identity: true),
                        userObjId = c.String(),
                        cacheBits = c.Binary(),
                        LastWrite = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.TokenCacheEntryID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TokenCacheEntries");
            DropTable("dbo.Tasks");
        }
    }
}
