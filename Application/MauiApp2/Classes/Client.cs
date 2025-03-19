using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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

        
    }
}
