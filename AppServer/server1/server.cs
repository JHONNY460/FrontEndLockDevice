using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;


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
            _database = database;
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
                    var usersToLock = await _database.GetUsersToLockAsync(connectedDevices);

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
                    else { Console.WriteLine("Not found any users to lock"); }
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

                    if (parts[0] == "LOGIN")
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
