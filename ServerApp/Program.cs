using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class Server
{
    private static readonly Dictionary<string, string> users = new Dictionary<string, string>()
    {
        { "admin", "password123" },
        { "user", "1234" }
    };

    private static readonly string dataFile = "server_data.txt";

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Сервер запущен...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Клиент подключен.");
            HandleClient(client);
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            // Авторизация (без шифрования)
            string username = reader.ReadLine();
            string password = reader.ReadLine();

            if (!users.ContainsKey(username) || users[username] != password)
            {
                writer.WriteLine("Ошибка: неверный логин или пароль.");
                client.Close();
                return;
            }

            writer.WriteLine("Авторизация успешна!");

            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("abcdefghijklmnop");
            aes.IV = Encoding.UTF8.GetBytes("1234567890123456");

            while (true)
            {
                string encryptedRequest = reader.ReadLine();
                if (encryptedRequest == "exit") break;

                string request = Decrypt(encryptedRequest, aes);
                string response = ProcessRequest(request);
                writer.WriteLine(response);

                File.AppendAllText(dataFile, $"{DateTime.Now}: {username} -> {request} = {response}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
        finally
        {
            client.Close();
        }
    }

    static string Decrypt(string encryptedText, Aes aes)
    {
        byte[] buffer = Convert.FromBase64String(encryptedText);
        using MemoryStream ms = new MemoryStream(buffer);
        using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    static string ProcessRequest(string input)
    {
        try
        {
            string[] parts = input.Split();
            double num1 = double.Parse(parts[0]);
            string op = parts[1];
            double num2 = double.Parse(parts[2]);

            return op switch
            {
                "+" => (num1 + num2).ToString(),
                "-" => (num1 - num2).ToString(),
                "*" => (num1 * num2).ToString(),
                "/" => num2 != 0 ? (num1 / num2).ToString() : "Ошибка: деление на 0",
                _ => "Ошибка: неизвестная операция"
            };
        }
        catch
        {
            return "Ошибка: неверный формат запроса";
        }
    }
}
