using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Database.Repositories;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;
using Refacto.DotNet.Controllers.Services.Impl.ProductHandlers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseInMemoryDatabase($"InMemoryDb");
});
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProductHandler, NormalProductHandler>();
builder.Services.AddScoped<IProductHandler, SeasonalProductHandler>();
builder.Services.AddScoped<IProductHandler, ExpirableProductHandler>();
builder.Services.AddScoped<IOrderService, OrderService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }