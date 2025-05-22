using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

class Program
{
    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "image_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var task = JsonSerializer.Deserialize<TaskData>(message);

            Console.WriteLine($" [x] Received task with operation: {task.Operation}");

            byte[] imageBytes = Convert.FromBase64String(task.Base64);
            string tempInputPath = "input.bmp";
            File.WriteAllBytes(tempInputPath, imageBytes);

            var outputPath = CudaWrapper.ProcessImage(tempInputPath, task.Operation);

            Console.WriteLine($" [x] Saved to: {outputPath}");
        };

        channel.BasicConsume(queue: "image_tasks", autoAck: true, consumer: consumer);

        Console.WriteLine(" [*] Waiting for messages. Press [Enter] to exit.");
        Console.ReadLine();
    }
}