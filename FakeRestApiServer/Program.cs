using FakeRestApiServer.DataAccess;
using FakeRestApiServer.Dtos;
using FakeRestApiServer.Entities;
using FakeRestApiServer.Repository;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeRestApiServer
{
    public class HttpRequest
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Body { get; set; }

        public static HttpRequest Parse(string raw)
        {
            var lines = raw.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            var firstLine = lines[0].Trim();
            var parts = firstLine.Split(' ');

            var body = lines.Length > 1 ? string.Join('\n', lines.Skip(1)) : "";

            return new HttpRequest
            {
                Method = parts[0].Trim().ToUpper(),
                Path = parts[1].Trim(),
                Body = body.Trim(),
            };
        }
    }

    public static class HttpResponse
    {
        public static string Json(object data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public static class Router
    {
        public static async Task<string> HandleAsync(HttpRequest request)
        {
            var context = new FakeApiDbContext();
            var repo = new ProductRepository(context);

            if (request.Method == "GET" && request.Path == "/products")
            {
                var products = await repo.GetAllProduct();
                return HttpResponse.Json(products);
            }
            else if (request.Method == "GET" && request.Path.StartsWith("/products/"))
            {
                var idText = request.Path.Replace("/products/", "");
                if (!int.TryParse(idText, out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id.." });
                }
                var product = await repo.GetProduct(id);
                if (product is null)
                {
                    return HttpResponse.Json(new { message = "Product not found.." });
                }
                return HttpResponse.Json(product);
            }
            else if (request.Method == "POST" && request.Path == "/products")
            {
                var dto = JsonSerializer.Deserialize<CreateProductDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return HttpResponse.Json(new { message = "Invalid product data.." });
                }
                var product = new Product
                {
                    Name = dto.Name,
                    Price = dto.Price
                };
                repo.Add(product);
                return HttpResponse.Json(product);
            }
            else if (request.Method == "PUT" && request.Path.StartsWith("/products/"))
            {
                var idText = request.Path.Replace("/products/", "");
                if (!int.TryParse(idText, out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id.." });
                }
                var product = await repo.GetProduct(id);
                if (product is null)
                {
                    return HttpResponse.Json(new { message = "Product not found.." });
                }
                var dto = JsonSerializer.Deserialize<UpdateProductDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return HttpResponse.Json(new { message = "Invalid product data.." });
                }
                product.Name = dto.Name;
                product.Price = dto.Price;
                repo.Update(product);
                return HttpResponse.Json(product);
            }
            else if (request.Method == "DELETE" && request.Path.StartsWith("/products/"))
            {
                var idText = request.Path.Replace("/products/", "");
                if (!int.TryParse(idText, out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id.." });
                }
                var product = await repo.GetProduct(id);
                if (product is null)
                {
                    return HttpResponse.Json(new { message = "Product not found.." });
                }
                repo.Delete(id);
                return HttpResponse.Json(new { message = "Product deleted successfully.." });
            }

            return HttpResponse.Json(null);
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            FakeApiDbContext context = new FakeApiDbContext();
            var repo = new ProductRepository(context);

            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 27001);
            listener.Start();

            Console.WriteLine("Fake Api Server started...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    HandleClientAsync(client);
                });
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                await using (var stream = client.GetStream())
                {
                    var buffer = new byte[client.ReceiveBufferSize];
                    var count = await stream.ReadAsync(buffer);
                    var rawRequest = Encoding.UTF8.GetString(buffer, 0, count);

                    Console.WriteLine("---------------------REQUEST------------------");
                    Console.WriteLine(rawRequest);

                    var request = HttpRequest.Parse(rawRequest);
                    var response = await Router.HandleAsync(request);

                    var bytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(bytes);
                }
            }
        }
    }
}
