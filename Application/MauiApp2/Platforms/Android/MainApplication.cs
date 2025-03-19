using Android.App;
using Android.Runtime;
using MauiApp2.Classes;

namespace MauiApp2
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp(); // here we tell the app - if its android- run CreateMauiApp - our main function

        //public void StartClient()
        //{
        //   // Client client = new Client("192.168.1.1", 5000); // Example IP & Port
        //   //// _ = client.ConnectAsync();
        //}
    }
}
