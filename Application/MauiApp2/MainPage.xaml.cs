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
using Org.BouncyCastle.Crypto.Encodings; //BouncyCastle is used for the encryption
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;

namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
        private Socket clientSocket;
        private string serverIp;
        private int serverPort = 5000;

#if ANDROID

#endif

        public MainPage()
        {
            InitializeComponent();
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

#if ANDROID
            
#endif
        }

#if ANDROID
        private async void OnLoginClicked(object sender, EventArgs e) // the function of the login button
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


private AsymmetricCipherKeyPair clientKeyPair;

        private async Task ListenForServerMessages() // The main function- listening to server's messages
        {
            try
            {
                // List all files in the Assets folder (for debugging)
                string[] assetFiles = Android.App.Application.Context.Assets.List("");
                Console.WriteLine("[CLIENT] Files in Assets folder:");
                foreach (var file in assetFiles)
                {
                    Console.WriteLine(file);
                }

                // Load the private key from the Assets folder
                AsymmetricKeyParameter privateKey = await LoadPrivateKeyFromAssets("private_key.pem");

                // Create the RSA decryption engine
                var cipher = new Pkcs1Encoding(new RsaEngine());
                cipher.Init(false, privateKey); // false for decryption

                byte[] buffer = new byte[4096];
                while (clientSocket != null && clientSocket.Connected)
                {
                    int received = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (received > 0)
                    {
                        try
                        {
                            // Extract the encrypted data
                            byte[] encryptedData = new byte[received];
                            Array.Copy(buffer, encryptedData, received);

                            // Decrypt the data
                            byte[] decryptedData = cipher.ProcessBlock(encryptedData, 0, encryptedData.Length);
                            string message = Encoding.UTF8.GetString(decryptedData);

                            Console.WriteLine($"[CLIENT] Decrypted: {message}");

                            if (message == "Your account has been locked.")
                            {
                                // Handle lock message
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    GetStatusLabel().Text += "\nYour device has been locked!";
#if ANDROID
                                    var deviceLockService = MauiProgram.Services.GetService<IDeviceLockService>();
                                    Console.WriteLine(deviceLockService);
                                    deviceLockService?.LockScreen();
#endif
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CLIENT] Decryption error: {ex.Message}");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[CLIENT] Connection lost: {ex.Message}");
            }
        }
#endif
#if ANDROID
        private async Task<AsymmetricKeyParameter> LoadPrivateKeyFromAssets(string fileName)  // Function to load the pem file - encryption 
        {
            try
            {
                // Read the PEM file from the Assets folder
                using (var stream = Android.App.Application.Context.Assets.Open(fileName))
                using (var reader = new StreamReader(stream))
                {
                    string pemContent = await reader.ReadToEndAsync();

                    // Parse the PEM content to extract the private key
                    using (var stringReader = new StringReader(pemContent))
                    {
                        PemReader pemReader = new PemReader(stringReader);
                        object keyObject = pemReader.ReadObject();

                        if (keyObject is AsymmetricCipherKeyPair keyPair)
                        {
                            // Return the private key
                            return keyPair.Private;
                        }
                        else if (keyObject is AsymmetricKeyParameter keyParam && keyParam.IsPrivate)
                        {
                            // Return the private key
                            return keyParam;
                        }
                        else
                        {
                            throw new InvalidOperationException("The PEM file does not contain a valid private key.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Error loading private key: {ex.Message}");
                throw;
            }
        }
#endif
        private bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }

        private Label GetStatusLabel()
        {
            return StatusLabel;
        }

        private async Task<bool> ConnectToServerAsync(Label statusLabel, string email, string password, string action) // connecting the server 
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