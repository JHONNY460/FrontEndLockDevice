
#if ANDROID
using Android.App;
using Android.Content;
using MauiApp2.Services;
using Microsoft.Maui.Controls.PlatformConfiguration;

[assembly: Dependency(typeof(DeviceLockService))]
namespace MauiApp2.Services
{
    public class DeviceLockService : IDeviceLockService  //our class that incharge of set the lock screen - validating there is a lock screen on device.
    {
        public DeviceLockService()
        {
            Console.WriteLine("DeviceLockService constructor called.");
        }

        public void LockScreen()
{
    Console.WriteLine("LockScreen method called.");

    var keyguardManager = (KeyguardManager)Android.App.Application.Context.GetSystemService(Context.KeyguardService);
    if (keyguardManager == null)
    {
        Console.WriteLine("KeyguardManager is null.");
        return;
    }

            // Check if a secure lock method (PIN, Pattern, Password) is set
            if (!keyguardManager.IsDeviceSecure)
            {
                Console.WriteLine("Device is not secured with a PIN, Pattern, or Password.");
                return;
            }

            var lockIntent = keyguardManager.CreateConfirmDeviceCredentialIntent("Lock", "Your device has been locked by LockerApp.");
    if (lockIntent != null)
    {
        lockIntent.AddFlags(ActivityFlags.NewTask);
        Android.App.Application.Context.StartActivity(lockIntent);
        Console.WriteLine("lockIntent started successfully.");
    }
    else
    {
        Console.WriteLine("lockIntent is null. Ensure device has a PIN, Pattern, or Password set.");
    }
}
    }
}
#endif