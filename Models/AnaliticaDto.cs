namespace NorthwindProyecto.Models
{
    public class AnaliticaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal TotalVendido { get; set; }
        public decimal TicketPromedio { get; set; }
    }
}
