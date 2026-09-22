using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StaCruzChallenge.Api.Middleware;
using StaCruzChallenge.Api.Security;
using StaCruzChallenge.Application.Interfaces;
using StaCruzChallenge.Application.Interfaces.ExternalServices;
using StaCruzChallenge.Application.Interfaces.Messaging;
using StaCruzChallenge.Application.Interfaces.Security;
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
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid Keycloak access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.Audience = builder.Configuration["Keycloak:Audience"];
    options.RequireHttpsMetadata =
        builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
 {
     Console.WriteLine(
         $"JWT challenge: error={context.Error}, " +
         $"description={context.ErrorDescription}, " +
         $"uri={context.ErrorUri}");

     return Task.CompletedTask;
 },
        OnMessageReceived = context =>
       {
           Console.WriteLine(
               $"Authorization header present: {!string.IsNullOrWhiteSpace(context.Token)}");

           return Task.CompletedTask;
       }
    };
});








builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IExternalOrderCaller, FakeExternalOrderCaller>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
