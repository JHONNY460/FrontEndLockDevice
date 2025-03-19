
//using MauiApp2.Droid;
using Microsoft.Extensions.Logging;
using MauiApp2.Classes;
#if ANDROID
using MauiApp2.Services;
//using Android.OS;
//using MauiApp2.Services;
#endif
namespace MauiApp2
{
    public static class MauiProgram // our heart of the program - the main class
    { 
        public static IServiceProvider Services { get; private set; } // Services manager - used to access microservices like lock screen
        public static MauiApp CreateMauiApp()
        {
            try
            {
                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    });

//#if DEBUG
//                builder.Logging.AddDebug();
//#endif
#if ANDROID
                builder.Services.AddSingleton<IDeviceLockService, DeviceLockService>(); //our device lock microservice
#endif
                
                var app = builder.Build();
                Services = app.Services;
                return app;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }


    }
}
