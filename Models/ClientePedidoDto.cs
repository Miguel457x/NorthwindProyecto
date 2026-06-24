namespace NorthwindProyecto.Models
{
    public class ClientePedidoDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}