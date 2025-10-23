using Domain;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CepController : ControllerBase
    {
        private readonly ICepService _cepService;

        public CepController(ICepService cepService)
        {
            _cepService = cepService;
        }

        // POST /api/cep
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CepRequest request)
        {
            try
            {
                var resultado = await _cepService.ConsultarCepAsync(request.Cep);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        // GET /api/cep
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var ceps = await _cepService.GetAllCepsAsync();
            return Ok(ceps);
        }
    }
}
