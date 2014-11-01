using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebApp_RoleClaims_DotNet.Utils;

[assembly: OwinStartup(typeof(WebApp_RoleClaims_DotNet.Startup))]

namespace WebApp_RoleClaims_DotNet
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
