using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using Dapper;
using System.Data;
using StaCruzChallenge.Infrastructure.Persistence.Data;

namespace StaCruzChallenge.Infrastructure.Persistence.Dapper
{
    public class ProductRepository : IProductRepository
    {
        private readonly IPostgresConnectionFactory _factory;
        public ProductRepository(IPostgresConnectionFactory factory)
        {
            _factory = factory;
        }
        public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = @"
            SELECT p_id AS Id, p_name AS Name, p_description AS Description,
                   p_price AS Price, created_at AS CreatedAt
            FROM products;
        ";
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync(cancellationToken: ct);
            return await conn.QueryAsync<Product>(new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {

            const string sql = @"
            SELECT p_id AS Id, p_name AS Name, p_description AS Description,
                   p_price AS Price, created_at AS CreatedAt
            FROM products
            WHERE p_id = @Id;
        ";

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            return await conn.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        }
    }
}