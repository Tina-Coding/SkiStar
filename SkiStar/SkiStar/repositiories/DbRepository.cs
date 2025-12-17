using Microsoft.Extensions.Configuration;
using Npgsql;
using SkiStar.SkistarData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SkiStar.repositiories
{
    public class DbRepository
    {
        private readonly string _connectionString;
        public DbRepository()
        {
            var config = new ConfigurationBuilder()
                         .AddUserSecrets<DbRepository>()
                         .Build();

            _connectionString = config.GetConnectionString("DefaultConnection"); // Hämtar hemlig information från jsonfil
                
        }

        public async Task<Skier> GetSKierByNameAsync(int id)
        {
            Skier skier = null;
            using var conn = new NpgsqlConnection(_connectionString); // sql-connection
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("select id, firstname, lastname, email from skier where id=1", conn); // sql-frågan

            // Läser av svaret i databasen från sql-koden tills det inte finns någon mer data att läsa
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    skier = new Skier
                    {
                        Id = (int)reader["id"],
                        Firstname = reader["firstname"].ToString(),
                        Lastname = reader["lastname"].ToString()
                    };
                  
                }
            }

            return skier;
        }
    }
}
