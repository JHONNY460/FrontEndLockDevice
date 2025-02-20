#if ANDROID
using Android.App;
using Android.Content;
using MauiApp2.Classes;
//using MauiApp2.Droid;
using MauiApp2.Services;

namespace MauiApp2.Services
{
    public interface IDeviceLockService
    {
        void LockScreen();
    }
}


#endif