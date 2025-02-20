using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MauiApp2.Classes;
#if ANDROID
using MauiApp2.Services;
#endif
using Microsoft.Maui.Controls;

namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
        private Socket clientSocket;
        private string serverIp;
        private int serverPort = 5000;

#if ANDROID
        //private readonly IDeviceLockService _deviceLockService;
#endif

        public MainPage()
        {
            InitializeComponent();
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

#if ANDROID
            // Resolve the IDeviceLockService using dependency injection
            //_deviceLockService = DependencyService.Get<IDeviceLockService>();
            //Console.WriteLine(_deviceLockService);
            
#endif
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string ipAddress = EntryIpAddress.Text;
            string email = EntryEmail.Text;
            string password = EntryPassword.Text;

            if (IsValidIpAddress(ipAddress) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                serverIp = ipAddress;
                bool isConnected = await ConnectToServerAsync(GetStatusLabel(), email, password, "REGISTER");

                if (isConnected)
                {
                    await DisplayAlert("Success", "Registration successful!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Failed to register.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter valid details.", "OK");
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string ipAddress = EntryIpAddress.Text;
            string email = EntryEmail.Text;
            string password = EntryPassword.Text;

            if (IsValidIpAddress(ipAddress) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                serverIp = ipAddress;
                bool isConnected = await ConnectToServerAsync(GetStatusLabel(), email, password, "LOGIN");

                if (isConnected)
                {
                    await DisplayAlert("Success", "Login successful!", "OK");

                    // Start listening for messages from the server
                    _ = Task.Run(() => ListenForServerMessages());
                }
                else
                {
                    await DisplayAlert("Error", "Failed to login.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter valid details.", "OK");
            }
        }

        private async Task ListenForServerMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (clientSocket != null && clientSocket.Connected)
                {
                    int received = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                    if (received > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, received);
                        Console.WriteLine($"[CLIENT] Received: {message}");

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            GetStatusLabel().Text += $"\nServer: {message}";
                        });

                        if (message == "Your account has been locked.")
                        {
                            Console.WriteLine("[CLIENT] LOCK command received! Locking device...");

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                GetStatusLabel().Text += "\nYour device has been locked!";
#if ANDROID
                                var deviceLockService = MauiProgram.Services.GetService<IDeviceLockService>(); 
                                Console.WriteLine(deviceLockService);
                                deviceLockService?.LockScreen();
#endif
                            });

                            // Optional: Disable UI or take other actions
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[CLIENT] Connection lost: {ex.Message}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GetStatusLabel().Text += $"\nConnection lost: {ex.Message}";
                });
            }
        }

        private bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }

        private Label GetStatusLabel()
        {
            return StatusLabel;
        }

        private async Task<bool> ConnectToServerAsync(Label statusLabel, string email, string password, string action)
        {
            try
            {
                await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
                statusLabel.Text = $"Connected to server at {serverIp}:{serverPort}";

                // Prepare data to send
                string data = $"{action}|{email}|{password}";
                byte[] commandBytes = Encoding.UTF8.GetBytes(data);
                await clientSocket.SendAsync(new ArraySegment<byte>(commandBytes), SocketFlags.None);

                // Receive response from server
                byte[] buffer = new byte[1024];
                int receivedBytes = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (receivedBytes > 0)
                {
                    string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    statusLabel.Text = $"Server Response: {response}";

                    return response.Contains("successful");
                }

                return false;
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                return false;
            }
        }
    }
}