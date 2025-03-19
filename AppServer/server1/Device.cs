
using System.Net.Sockets;
using System.Net;

class Device
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
