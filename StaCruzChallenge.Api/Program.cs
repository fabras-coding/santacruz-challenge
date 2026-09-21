using StaCruzChallenge.Api.Middleware;
using StaCruzChallenge.Application.Interfaces;
using StaCruzChallenge.Application.Interfaces.ExternalServices;
using StaCruzChallenge.Application.Interfaces.Messaging;
using StaCruzChallenge.Application.Services;
using StaCruzChallenge.Infrastructure.Extensions;
using StaCruzChallenge.Infrastructure.ExternalServices;
using StaCruzChallenge.Infrastructure.Messaging;
using StaCruzChallenge.Infrastructure.Persistence.Data;
using StaCruzChallenge.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IExternalOrderCaller, FakeExternalOrderCaller>();

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>(); 

builder.Services.AddHostedService<OrderProcessorWorker>();
builder.Services.AddHostedService<OutboxOrderPublishWorker>();


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
