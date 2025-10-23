using Dapper;
using Domain;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Repository
{
    public class CepRepository : ICepRepository
    {
        private readonly string _connectionString;

        public CepRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new ArgumentNullException("DefaultConnection", "Connection string não encontrada");
        }

        public async Task<int> AddCepAsync(Cep cep)
        {
            if (cep == null) throw new ArgumentNullException(nameof(cep));

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Ceps (CepCode, Logradouro, Complemento, Bairro, Localidade, Uf, Ibge, Gia, Ddd, Siafi, DataConsulta)
                VALUES (@CepCode, @Logradouro, @Complemento, @Bairro, @Localidade, @Uf, @Ibge, @Gia, @Ddd, @Siafi, @DataConsulta);
                SELECT LAST_INSERT_ID();
            ";

            var id = await connection.ExecuteScalarAsync<int>(sql, cep);
            return id;
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Ceps;";
            var ceps = await connection.QueryAsync<Cep>(sql);
            return ceps;
        }

        public async Task<Cep?> GetCepByCodeAsync(string cep)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM Ceps WHERE CepCode = @Cep LIMIT 1;";
            var result = await connection.QueryFirstOrDefaultAsync<Cep>(sql, new { Cep = cep });
            return result;
        }
    }
}
