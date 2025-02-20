
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
    public static class MauiProgram
    { //
        public static IServiceProvider Services { get; private set; }
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

#if DEBUG
                builder.Logging.AddDebug();
#endif
#if ANDROID
                builder.Services.AddSingleton<IDeviceLockService, DeviceLockService>();
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
