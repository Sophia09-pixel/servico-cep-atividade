using Domain;
using Repository;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Service
{
    public class CepService : ICepService
    {
        private readonly ICepRepository _cepRepository;
        private readonly ICacheService _cacheService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CepService> _logger;

        public CepService(ICepRepository cepRepository, ICacheService cacheService, ILogger<CepService> logger)
        {
            _cepRepository = cepRepository;
            _cacheService = cacheService;
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<Cep> ConsultarCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep)) throw new ArgumentException("CEP inválido.");

            var cepLimpo = cep.Replace("-", "").Trim();
            if (cepLimpo.Length != 8 || !cepLimpo.All(char.IsDigit))
                throw new ArgumentException("CEP deve ter 8 dígitos numéricos.");

            string cacheKey = "get-ceps";

            // 1. Verificar cache
            var cachedJson = await _cacheService.GetAsync(cacheKey);
            if (cachedJson != null)
            {
                _logger.LogInformation("Cache hit para o CEP {Cep}", cepLimpo);
                var cachedCep = JsonSerializer.Deserialize<Cep>(cachedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (cachedCep != null) return cachedCep;
            }

            // 2. Verificar no banco
            var existente = await _cepRepository.GetCepByCodeAsync(cepLimpo);
            if (existente != null)
            {
                _logger.LogInformation("CEP {Cep} encontrado no banco", cepLimpo);
                await _cacheService.SetAsync(cacheKey, JsonSerializer.Serialize(existente), TimeSpan.FromSeconds(20));
                return existente;
            }

            // 3. Consultar ViaCEP
            var url = $"https://viacep.com.br/ws/{cepLimpo}/json/";
            ViaCepResponse viaCep;
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                viaCep = JsonSerializer.Deserialize<ViaCepResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Erro ao desserializar resposta ViaCEP.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar ViaCEP para o CEP {Cep}", cepLimpo);
                throw new Exception("Erro ao consultar ViaCEP.");
            }

            if (viaCep.Erro)
                throw new ArgumentException("CEP não encontrado na ViaCEP.");

            // 4. Criar entidade e salvar
            var novoCep = new Cep
            {
                CepCode = viaCep.Cep.Replace("-", ""),
                Logradouro = viaCep.Logradouro,
                Complemento = viaCep.Complemento,
                Bairro = viaCep.Bairro,
                Localidade = viaCep.Localidade,
                Uf = viaCep.Uf,
                Ibge = viaCep.Ibge,
                Gia = viaCep.Gia,
                Ddd = viaCep.Ddd,
                Siafi = viaCep.Siafi,
                DataConsulta = DateTime.UtcNow
            };

            novoCep.Id = await _cepRepository.AddCepAsync(novoCep);

            // 5. Armazenar no cache por 20 segundos
            await _cacheService.SetAsync(cacheKey, JsonSerializer.Serialize(novoCep), TimeSpan.FromSeconds(20));

            _logger.LogInformation("CEP {Cep} salvo no cache e banco", cepLimpo);

            return novoCep;
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            return await _cepRepository.GetAllCepsAsync();
        }
    }
}
