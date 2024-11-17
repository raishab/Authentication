using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension
{
    public static class Security
    {
        public static IServiceCollection AddAuthentication(this IServiceCollection services, AuthTokenOptions tokenOptions)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(Options =>
            {
                Options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidAudiences = tokenOptions.Audiences,
                    ValidIssuer = tokenOptions.Issuer,
                    IssuerSigningKey = tokenOptions.RsaPublicSecurityKey,
                    ClockSkew = TimeSpan.Zero
                };
                Options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if(context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context?.Response?.Headers?.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
                Options.RequireHttpsMetadata = false;
                Options.SaveToken = true;
            });
            AddAuthorizationTokenOptions(services, tokenOptions);
            return services;
        }
        private static void AddAuthorizationTokenOptions(this IServiceCollection services, AuthTokenOptions tokenOptions)
        {
            services.AddSingleton(tokenOptions);
        }
    }
}
