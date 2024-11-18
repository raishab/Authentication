using Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Extension.MiddleWare;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var authTokenOptions = SecurityUtils.CreateAuthTokenOptions(configuration);
builder.Services.AddAuthentication(authTokenOptions);
builder.Services.AddAuthorization();
builder.Services.ConfigureDependencies();
builder.Services.ConfigureOptions(configuration);
builder.Services.AppPolicies();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(Options =>
{
    Options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    Options.AddSecurityRequirement(new OpenApiSecurityRequirement(){
                    {
                        new OpenApiSecurityScheme
                        {
                            Scheme = "oauth2",
                            Name = "Bearer",
                            Reference = new OpenApiReference{
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"},
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });

    Options.AddSecurityRequirement(new OpenApiSecurityRequirement(){
                    {
                        new OpenApiSecurityScheme
                        {
                            Scheme = "oauth2",
                            Name = "Bearer",
                            Reference = new OpenApiReference{
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"},
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandlers();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
