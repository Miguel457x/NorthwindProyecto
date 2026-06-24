namespace NorthwindProyecto.Models
{
    public class VistaLineaDto
    {
        public int OrderId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal TotalLinea { get; set; }
    }
}