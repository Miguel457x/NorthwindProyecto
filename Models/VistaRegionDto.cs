namespace NorthwindProyecto.Models
{
    public class VistaRegionDto
    {
        public string Region { get; set; } = string.Empty;
        public int Clientes { get; set; }
        public decimal TotalVentas { get; set; }
    }
}