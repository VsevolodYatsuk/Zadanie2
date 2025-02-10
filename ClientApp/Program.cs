using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class Client
{
    static void Main()
    {
        Console.Write("Введите логин: ");
        string username = Console.ReadLine();

        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        TcpClient client = new TcpClient("127.0.0.1", 5000);
        NetworkStream stream = client.GetStream();

        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);

        try
        {
            // Отправка логина и пароля (без шифрования)
            writer.WriteLine(username);
            writer.WriteLine(password);

            string authResponse = reader.ReadLine();
            Console.WriteLine(authResponse);

            if (authResponse.Contains("Ошибка"))
            {
                client.Close();
                return;
            }

            // Подключение к шифрованию
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("abcdefghijklmnop");
            aes.IV = Encoding.UTF8.GetBytes("1234567890123456");

            while (true)
            {
                Console.Write("Введите математический запрос (например: 5 + 3) или 'exit' для выхода: ");
                string request = Console.ReadLine();

                if (request == "exit")
                {
                    writer.WriteLine("exit");
                    break;
                }

                string encryptedRequest = Encrypt(request, aes);
                writer.WriteLine(encryptedRequest);

                string response = reader.ReadLine();
                Console.WriteLine($"Ответ: {response}");
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

    static string Encrypt(string plainText, Aes aes)
    {
        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using StreamWriter sw = new StreamWriter(cs);
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }
}
