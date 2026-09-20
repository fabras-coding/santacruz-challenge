using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StaCruzChallenge.Domain.Repositories;
using StaCruzChallenge.Infrastructure.Persistence.Dapper;
using StaCruzChallenge.Infrastructure.Persistence.Data;

namespace StaCruzChallenge.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
            
        services.AddSingleton<IPostgresConnectionFactory>(provider => new PostgresConnectionFactory(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
