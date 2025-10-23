using Domain;
using System.Runtime.ConstrainedExecution;

namespace Service
{
    public interface ICepService
    {
        Task<Cep> ConsultarCepAsync(string cep);
        Task<IEnumerable<Cep>> GetAllCepsAsync();
    }
}
