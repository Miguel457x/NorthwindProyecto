namespace NorthwindProyecto.Models
{
    public class SpRegistrarPedidoCompletoDto
    {
        public string CustomerID { get; set; } = string.Empty;
        public int EmployeeID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }
}