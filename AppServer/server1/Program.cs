
using System.Threading.Tasks;



class Program
{
    static async Task Main()
    {
        var database = new Database("127.0.0.1", "project", "JohnServer", "Jhony2007"); // the pass
        Server server = new Server(5000, database);
        await server.StartAsync(); //פה
        
    }
}