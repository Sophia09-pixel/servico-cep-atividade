using Domain;

namespace Repository
{
    public interface ICepRepository
    {
        Task<int> AddCepAsync(Cep cep);
        Task<IEnumerable<Cep>> GetAllCepsAsync();
        Task<Cep?> GetCepByCodeAsync(string cep);
    }
}
