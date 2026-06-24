using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using NorthwindProyecto.Models;
using NorthwindProyecto.Repositories;
using System.Net;

namespace NorthwindProyecto.Controllers
{
    [ApiController]
    [Route("api")]
    public class NorthwindApiController : ControllerBase
    {
        private readonly INorthwindRepository _repo;

        public NorthwindApiController(INorthwindRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("ventas")]
        public async Task<IActionResult> GetVentas([FromQuery] string agruparPor, [FromQuery] int anio)
        {
            var data = await _repo.GetVentasAnalyticsAsync(agruparPor, anio);
            return Ok(data);
        }

        [HttpPost("clientes")]
        public async Task<IActionResult> Insertar([FromBody] ClientePedidoDto p)
        {
            try
            {
                var orderId = await _repo.InsertarClientePedidoAsync(p);
                return Ok(new { mensaje = "Estructura inyectada", orderId });
            }
            catch { return BadRequest("Error de restricciones en la base de datos."); }
        }

        [HttpPut("clientes/{id}")]
        public async Task<IActionResult> Actualizar(string id, [FromBody] ActualizarClienteDto p)
        {
            var exito = await _repo.ActualizarClienteAsync(id, p);
            return exito ? Ok("Modificación completada.") : NotFound("Cliente no registrado.");
        }

        [HttpDelete("pedidos/{orderId}")]
        public async Task<IActionResult> Eliminar(int orderId)
        {
            var exito = await _repo.EliminarPedidoAsync(orderId);
            return exito ? Ok("Orden limpia.") : BadRequest("La orden no existe.");
        }

        [HttpGet("vistas/ventas-por-region")]
        public async Task<IActionResult> GetRegiones()
        {
            var data = await _repo.GetVistaRegionesAsync();
            return Ok(data);
        }

        [HttpGet("catalogos/clientes")]
        public async Task<IActionResult> GetCatClientes()
        {
            var data = await _repo.GetCatClientesAsync();
            return Ok(data);
        }

        [HttpGet("catalogos/productos")]
        public async Task<IActionResult> GetCatProductos()
        {
            var data = await _repo.GetCatProductosAsync();
            return Ok(data);
        }

        [HttpGet("vistas/pedidos-completos")]
        public async Task<IActionResult> GetPedidosCompletos()
        {
            var data = await _repo.GetPedidosCompletosAsync();
            return Ok(data);
        }
        [HttpGet("vistas/pedidos-con-total-linea")]
        public async Task<IActionResult> GetLineas()
        {
            var data = await _repo.GetVistaLineasAsync();
            return Ok(data);
        }

        [HttpGet("procedimientos/historial/{customerId}")]
        public async Task<IActionResult> GetHistorial(string customerId)
        {
            var data = await _repo.GetHistorialClienteAsync(customerId);
            return Ok(data);
        }

        [HttpPost("procedimientos/registrar-pedido")]
        public async Task<IActionResult> RegistrarPedidoCompleto([FromBody] SpRegistrarPedidoCompletoDto param)
        {
            if (param == null) return BadRequest("Los parámetros no pueden ser nulos.");

            try
            {
                var exito = await _repo.RegistrarPedidoCompletoAsync(param);
                if (exito)
                {
                    return Ok(new { ok = true, mensaje = "SP 'RegistrarPedidoCompleto' ejecutado con éxito." });
                }
                return BadRequest("No se pudo completar el registro en la base de datos.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}