using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        string path = "input.bmp";
        string operation = "invert";

        Klient.SendTask(path, operation);
    }
}

public static class Klient
{
    public static void SendTask(string path, string operation)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "image_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var imageBytes = File.ReadAllBytes(path);
        string base64 = Convert.ToBase64String(imageBytes);

        var task = new TaskData { Base64 = base64, Operation = operation };
        var json = JsonSerializer.Serialize(task);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "", routingKey: "image_tasks", basicProperties: null, body: body);

        Console.WriteLine($" [x] Sent: {json}");
    }
}
