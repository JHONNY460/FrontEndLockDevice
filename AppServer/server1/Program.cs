using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

class Device  //
{
    public string IP { get; private set; }
    public string Username { get; private set; }
    public Socket Connection { get; private set; }

    public Device(Socket socket)
    {
        Connection = socket;
        IP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        Username = "Unknown";
    }

    public void SetUsername(string username)
    {
        Username = username;
    }
}

class Database
{
    private readonly string _connectionString;

    public Database(string server, string database, string userId, string password)
    {
        _connectionString = $"Server={server};Database={database};User Id={userId};Password={password};"; // 
    }

    public async Task<bool> RegisterUserAsync(string username, string email, string password)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO users (username, email, password) VALUES (@username, @email, @password)";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@password", password);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
    }

    public async Task<(bool isAuthenticated, string username)> AuthenticateUserAsync(string email, string password)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = "SELECT username FROM users WHERE email = @email AND password = @password";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@password", password);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // Check if there is a matching row
                    {
                        string username = reader.GetString(0); // Get username from the first column
                        return (true, username);
                    }
                }
            }
        }

        return (false, string.Empty); // Return false with empty username if authentication fails
    }

    private void SendLockNotification(Device device)
    {
        try
        {
            string message = "Your account has been locked.";
            byte[] data = Encoding.UTF8.GetBytes(message);
            device.Connection.Send(data);
            Console.WriteLine($"Sent lock notification to {device.Username} ({device.IP})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending lock notification to {device.Username}: {ex.Message}");
        }
    }

    public async Task<List<string>> GetUsersToLockAsync(List<Device> connectedDevices)
    {
        Console.WriteLine($"Test lock is running?");
        var usersToLock = new List<string>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // שליפת המשתמשים שצריך לנעול
            var query = "SELECT email, username FROM users WHERE should_lock = 1";
            using (var command = new MySqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    int emailIndex = reader.GetOrdinal("email");
                    int usernameIndex = reader.GetOrdinal("username");

                    while (await reader.ReadAsync())
                    {
                        string email = reader.GetString(emailIndex);
                        string username = reader.GetString(usernameIndex);
                        usersToLock.Add(email);

                        // שליחת הודעה למכשיר של המשתמש אם קיים ברשימת המכשירים המחוברים
                        var device = connectedDevices.FirstOrDefault(d => d.Username == username);
                        if (device != null)
                        {
                            Console.WriteLine("Trying to lock device.");
                            SendLockNotification(device);
                        }
                    }
                }
            }



            // עדכון המשתמשים שנמצאו ל-should_lock = FALSE
            if (usersToLock.Any())
            {
                var updateQuery = "UPDATE users SET should_lock = FALSE WHERE email = @email";
                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    foreach (var email in usersToLock)
                    {
                        updateCommand.Parameters.Clear();
                        updateCommand.Parameters.AddWithValue("@email", email);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        return usersToLock;
    }
}

    class Server
{
    private List<Device> connectedDevices = new List<Device>();
    private Socket serverSocket;
    private Database _database;

    public Server(int port, Database database)
    {
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        serverSocket.Listen(10);
        _database = database; // הוא לא באמת יוצר חיבור רק הגדיר
        Console.WriteLine($"Server listening on port {port}...");
    }

    public async Task StartAsync()
    {
        // התחל משימה תקופתית לבדיקת נעילה
        _ = CheckForLockedUsersPeriodically();

        while (true)
        {
            Socket clientSocket = await serverSocket.AcceptAsync();
            Device newDevice = new Device(clientSocket);
            connectedDevices.Add(newDevice);
            Console.WriteLine($"New device connected: {newDevice.IP}");

            _ = HandleClientAsync(newDevice);
        }
    }

        

        private async Task CheckForLockedUsersPeriodically()
    {
        while (true)
        {
            try
            {
                // המתן 10 שניות בין בדיקה לבדיקה
                await Task.Delay(10000);

                // קבל את רשימת המשתמשים שצריך לנעול
                var usersToLock = await _database.GetUsersToLockAsync(connectedDevices); // פונקציה המחפשת יוזרים לנעילה מהדטא בייס- אם מצאה שולחת הודעה נעילה דרך הסוקט לפי שם משתמש

                if (usersToLock.Any())
                {
                    Console.WriteLine($"Found {usersToLock.Count} users to lock.");

                    // נעל את המכשירים של המשתמשים האלה
                    foreach (var email in usersToLock)
                    {
                        var deviceToLock = connectedDevices.FirstOrDefault(d => d.Username == email);
                        if (deviceToLock != null)
                        {
                            Console.WriteLine($"Locking device with IP: {deviceToLock.IP}");
                            deviceToLock.Connection.Close();
                            connectedDevices.Remove(deviceToLock);
                        }
                    }
                }
                else { Console.WriteLine("Not found any users to lock"); };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for locked users: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(Device device)
    {
        Console.WriteLine($"Device {device.IP} was trying to connect.");
        try
        {
            byte[] buffer = new byte[1024];
            int received = await device.Connection.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

            if (received > 0)
            {
                string data = Encoding.UTF8.GetString(buffer, 0, received);
                var parts = data.Split('|');
                if (parts[0] == "REGISTER")
                {
                    var username = parts[1];
                    var email = parts[2];
                    var password = parts[3];
                    var result = await _database.RegisterUserAsync(username, email, password);
                    if (result)
                    {
                        await device.Connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Registration successful")), SocketFlags.None);
                    }
                    else
                    {
                        await device.Connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Registration failed")), SocketFlags.None);
                    }
                }
                else if (parts[0] == "LOGIN")
                {
                    Console.WriteLine($"Device {device.IP} was trying to Login.");
                    Console.WriteLine($"Email: {parts[1]} ");
                    Console.WriteLine($"Password: {parts[2]} ");

                    var email = parts[1];
                    var password = parts[2];

                    var (isAuthenticated, username) = await _database.AuthenticateUserAsync(email, password);

                    if (isAuthenticated)
                    {
                        Console.WriteLine($"Login successful for {username}");

                        // Send username along with success message
                        string successMessage = $"Login successful|{username}";
                        device.SetUsername(username);
                        await device.Connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(successMessage)), SocketFlags.None);
                    }
                    else
                    {
                        Console.WriteLine("Login failed");
                        await device.Connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Login failed")), SocketFlags.None);
                    }
                }

            }
        }
        catch
        {
            Console.WriteLine($"Device {device.IP} disconnected.");
            connectedDevices.Remove(device);
        }
    }
}




class Program
{
    static async Task Main()
    {
        var database = new Database("127.0.0.1", "project", "JohnServer", "Jhony2007"); // the pass
        Server server = new Server(5000, database);
        await server.StartAsync(); //פה
        
    }
}