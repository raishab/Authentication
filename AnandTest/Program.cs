using Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Extension.MiddleWare;
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
builder.Services.AddSwaggerGen();

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
