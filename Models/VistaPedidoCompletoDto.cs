namespace NorthwindProyecto.Models
{
    public class VistaPedidoCompletoDto
    {
        public int OrderId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaPedido { get; set; }
    }
}
 