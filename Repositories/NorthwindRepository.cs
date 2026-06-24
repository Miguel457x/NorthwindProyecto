using Dapper;
using NorthwindProyecto.Models;
using System.Data;
using System.Data.SqlClient;


namespace NorthwindProyecto.Repositories
{
    public interface INorthwindRepository
    {
        Task<IEnumerable<AnaliticaDto>> GetVentasAnalyticsAsync(string agruparPor, int anio);
        Task<int> InsertarClientePedidoAsync(ClientePedidoDto p);
        Task<bool> ActualizarClienteAsync(string id, ActualizarClienteDto p);
        Task<bool> EliminarPedidoAsync(int orderId);
        Task<IEnumerable<VistaRegionDto>> GetVistaRegionesAsync();
        Task<IEnumerable<CatalogoClienteDto>> GetCatClientesAsync();
        Task<IEnumerable<CatalogoProductoDto>> GetCatProductosAsync();
        Task<IEnumerable<VistaPedidoCompletoDto>> GetPedidosCompletosAsync();
        Task<IEnumerable<VistaLineaDto>> GetVistaLineasAsync();
        Task<IEnumerable<SpHistorialDto>> GetHistorialClienteAsync(string customerId);
        Task<bool> RegistrarPedidoCompletoAsync(SpRegistrarPedidoCompletoDto p);
    }

    public class NorthwindRepository : INorthwindRepository
    {
        private readonly string _conexion;

        public NorthwindRepository(IConfiguration config)
        {
            _conexion = config.GetConnectionString("NorthwindConnection")
                        ?? throw new Exception("Cadena de conexión no encontrada.");
        }

        private IDbConnection GetConnection() => new SqlConnection(_conexion);

        //ANALISIS
        public async Task<IEnumerable<AnaliticaDto>> GetVentasAnalyticsAsync(string agruparPor, int anio)
        {
            string sql = agruparPor switch
            {
                "cliente" => @"SELECT c.CompanyName AS Nombre, COUNT(DISTINCT o.OrderID) AS TotalPedidos, 
                               CAST(SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TotalVendido,
                               CAST(AVG(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TicketPromedio
                               FROM Customers c INNER JOIN Orders o ON c.CustomerID = o.CustomerID
                               INNER JOIN [Order Details] od ON o.OrderID = od.OrderID WHERE YEAR(o.OrderDate) = @Anio GROUP BY c.CompanyName",
                "empleado" => @"SELECT (e.FirstName + ' ' + e.LastName) AS Nombre, COUNT(DISTINCT o.OrderID) AS TotalPedidos, 
                                CAST(SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TotalVendido,
                                CAST(AVG(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TicketPromedio
                                FROM Employees e INNER JOIN Orders o ON e.EmployeeID = o.EmployeeID
                                INNER JOIN [Order Details] od ON o.OrderID = od.OrderID WHERE YEAR(o.OrderDate) = @Anio GROUP BY e.FirstName, e.LastName",
                _ => @"SELECT p.ProductName AS Nombre, COUNT(DISTINCT od.OrderID) AS TotalPedidos, 
                       CAST(SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TotalVendido,
                       CAST(AVG(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TicketPromedio
                       FROM Products p INNER JOIN [Order Details] od ON p.ProductID = od.ProductID
                       INNER JOIN Orders o ON od.OrderID = o.OrderID WHERE YEAR(o.OrderDate) = @Anio GROUP BY p.ProductName"
            };

            using var db = GetConnection();
            return await db.QueryAsync<AnaliticaDto>(sql, new { Anio = anio });
        }
        //INSERCIÓN ACID 
        public async Task<int> InsertarClientePedidoAsync(ClientePedidoDto p)
        {
            string sqlScript = @"
                BEGIN TRANSACTION;
                BEGIN TRY
                    IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerID = @Id)
                        INSERT INTO Customers (CustomerID, CompanyName, Phone) VALUES (@Id, @Comp, @Phone);

                    INSERT INTO Orders (CustomerID, OrderDate) VALUES (@Id, GETDATE());
                    DECLARE @NewId INT = SCOPE_IDENTITY();

                    DECLARE @Price DECIMAL(18,2) = COALESCE((SELECT UnitPrice FROM Products WHERE ProductID = @ProdId), 10.0);
                    INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount) VALUES (@NewId, @ProdId, @Price, @Qty, 0);

                    COMMIT TRANSACTION;
                    SELECT @NewId; 
                END TRY
                BEGIN CATCH
                    ROLLBACK TRANSACTION;
                    THROW;
                END CATCH";

            using var db = GetConnection();
            return await db.QuerySingleAsync<int>(sqlScript, new
            {
                Id = p.CustomerId,
                Comp = p.CompanyName,
                Phone = p.Phone,
                ProdId = p.ProductId,
                Qty = p.Quantity
            });
        }

        // ACTUALIZAR
        public async Task<bool> ActualizarClienteAsync(string id, ActualizarClienteDto p)
        {
            using var db = GetConnection();
            string sql = "UPDATE Customers SET Phone = @Phone, Address = @Address WHERE CustomerID = @Id";
            var filas = await db.ExecuteAsync(sql, new { p.Phone, p.Address, Id = id });
            return filas > 0;
        }

        // ELIMINAR

        public async Task<bool> EliminarPedidoAsync(int orderId)
        {
            using var db = GetConnection();
            string sql = "DELETE FROM [Order Details] WHERE OrderID = @Ord; DELETE FROM Orders WHERE OrderID = @Ord;";
            var filas = await db.ExecuteAsync(sql, new { Ord = orderId });
            return filas > 0;
        }

        // REGIONES

        public async Task<IEnumerable<VistaRegionDto>> GetVistaRegionesAsync()
        {
            using var db = GetConnection();
            string sql = @"SELECT COALESCE(r.RegionDescription, 'Internacional') AS Region, COUNT(DISTINCT c.CustomerID) AS Clientes,
                           CAST(SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS DECIMAL(18,2)) AS TotalVentas
                           FROM Customers c INNER JOIN Orders o ON c.CustomerID = o.CustomerID
                           INNER JOIN [Order Details] od ON o.OrderID = od.OrderID INNER JOIN Employees e ON o.EmployeeID = e.EmployeeID
                           INNER JOIN EmployeeTerritories et ON e.EmployeeID = et.EmployeeID INNER JOIN Territories t ON et.TerritoryID = t.TerritoryID
                           INNER JOIN Region r ON t.RegionID = r.RegionID GROUP BY r.RegionDescription";

            return await db.QueryAsync<VistaRegionDto>(sql);
        }

        // CATÁLOGOS 

        public async Task<IEnumerable<CatalogoClienteDto>> GetCatClientesAsync()
        {
            using var db = GetConnection();
            string sql = "SELECT CustomerID, CompanyName FROM Customers ORDER BY CompanyName";
            return await db.QueryAsync<CatalogoClienteDto>(sql);
        }

        public async Task<IEnumerable<CatalogoProductoDto>> GetCatProductosAsync()
        {
            using var db = GetConnection();
            string sql = "SELECT ProductID, ProductName FROM Products ORDER BY ProductName";
            return await db.QueryAsync<CatalogoProductoDto>(sql);
        }

        // MULTI-JOIN

        public async Task<IEnumerable<VistaPedidoCompletoDto>> GetPedidosCompletosAsync()
        {
            using var db = GetConnection();
            string sql = @"
        SELECT 
            o.OrderID,
            c.CompanyName AS Cliente,
            p.ProductName AS Producto,
            od.UnitPrice AS PrecioVenta,
            od.Quantity AS Cantidad,
            o.OrderDate AS FechaPedido
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerID = c.CustomerID
        INNER JOIN [Order Details] od ON o.OrderID = od.OrderID
        INNER JOIN Products p ON od.ProductID = p.ProductID
        ORDER BY o.OrderID DESC";
            return await db.QueryAsync<VistaPedidoCompletoDto>(sql);
        }

        // VISTA PEDIDOS CON TOTAL

        public async Task<IEnumerable<VistaLineaDto>> GetVistaLineasAsync()
        {
            using var db = GetConnection();
            string sql = "SELECT OrderId, Producto, Cantidad, TotalLinea FROM vw_PedidosConTotalLinea";
            return await db.QueryAsync<VistaLineaDto>(sql);
        }

        //HISTORIAL DE PEDIDOS POR CLIENTE

        public async Task<IEnumerable<SpHistorialDto>> GetHistorialClienteAsync(string customerId)
        {
            using var db = GetConnection();
            return await db.QueryAsync<SpHistorialDto>(
                "sp_HistorialPedidosCliente",
                new { CustomerId = customerId },
                commandType: CommandType.StoredProcedure
            );
        }

        // REGISTRAR PEDIDO COMPLETO
        public async Task<bool> RegistrarPedidoCompletoAsync(SpRegistrarPedidoCompletoDto p)
        {
            using var db = GetConnection();
            {
                var parametros = new
                {
                    CustomerID = p.CustomerID,
                    EmployeeID = p.EmployeeID,
                    ProductID = p.ProductID,
                    Quantity = p.Quantity
                };
                await db.ExecuteAsync(
                    "sp_RegistrarPedidoCompleto",
                    parametros,
                    commandType: CommandType.StoredProcedure
                );

                return true;
            }
        }
    }
}
