using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ADL.AirportSampling.Utility.Enum;
namespace Extension
{
    public static class Policies
    {
        public static IServiceCollection AppPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(auth =>
            {
                auth.AddPolicy(AuthorizationPolicies.Admin_Authenticated, options =>
                {
                    options.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    options.RequireAuthenticatedUser();
                    options.RequireRole(RoleType.Admin.ToString(), RoleType.Viewer.ToString(), RoleType.Manager.ToString());
                    options.Build();
                });
                auth.AddPolicy(AuthorizationPolicies.Admin_RoleType, options =>
                {
                    options.RequireRole(RoleType.Admin.ToString());
                    options.Build();
                });
                auth.AddPolicy(AuthorizationPolicies.Viewer_RoleType, options =>
                {
                    options.RequireRole(RoleType.Viewer.ToString());
                    options.Build();
                });
            });
            return services;
        }
    }
}
