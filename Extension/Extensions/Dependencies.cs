using DataService;
using Microsoft.Extensions.DependencyInjection;
using QueryService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services;
namespace Extension
{
    public static class Dependencies
    {
        public static IServiceCollection ConfigureDependencies(this IServiceCollection services)
        {
            services.AddScoped<IQueryServices, QueryServices>();

            services.AddScoped<IDataServices, DataServices>();
            services.AddScoped<IUserServices, UserServices>();
            return services;
        }
    }
}
