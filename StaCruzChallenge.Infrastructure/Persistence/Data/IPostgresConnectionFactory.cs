using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;


namespace StaCruzChallenge.Infrastructure.Persistence.Data
{
    public interface IPostgresConnectionFactory
    {
        DbConnection CreateConnection();
    }
}