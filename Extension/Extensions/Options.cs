using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using APIModels.Common;
namespace Extension
{
    public static class Options
    {
        public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<ConnectionStringModel>(configuration.GetSection("ConnectionStrings"));
            services.Configure<JwtConfiguration>(configuration.GetSection("JwtSecurityOptions"));
            return services;
        }
    }
}
