using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MauiApp2.Classes
{
    public class Client 
    {
        private Socket clientSocket;
        private string serverIP;
        private int serverPort;
        private bool isListening = true;

        public Client(string ip, int port)
        {
            serverIP = ip;
            serverPort = port;
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        
        public async Task ConnectAsync(Label statusLabel, Entry usernameEntry)
        {
            try
            {
                await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
        
                string username = usernameEntry.Text;
                if (!string.IsNullOrEmpty(username))
                {
                    byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
                    await clientSocket.SendAsync(new ArraySegment<byte>(usernameBytes), SocketFlags.None);
                    statusLabel.Text = "Connected and username sent.";
        
                    // Start listening for incoming messages from the server
                    _ = Task.Run(() => ListenForMessages(statusLabel));
                }
                else
                {
                    statusLabel.Text = "Username cannot be empty.";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }
        
        private async Task ListenForMessages(Label statusLabel) 
        {
            try
            {
                byte[] buffer = new byte[1024];
        
                while (isListening)
                {
                    int received = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        
                    if (received > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, received);
                        Console.WriteLine($"Message from server: {message}");
        
                        // Update UI - run on the main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            statusLabel.Text += $"\nServer: {message}";
                        });
        
                        if (message == "Your account has been locked.")
                        {
                            // Handle lock event (e.g., disable UI, show alert)
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                statusLabel.Text += "\nAccount locked. Please contact support.";
                            });
        
                            isListening = false; // Stop listening if locked
                            clientSocket.Close(); // Close socket
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    statusLabel.Text += $"\nError receiving message: {ex.Message}";
                });
            }
        }
    }
}
