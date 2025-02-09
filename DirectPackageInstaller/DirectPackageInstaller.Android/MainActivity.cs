using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Avalonia.Android;
using Avalonia;
using Avalonia.ReactiveUI;
using Java.Lang;
using Application = Android.App.Application;
using File = Java.IO.File;
using System.Linq;
using AndroidX.Core.Content;
using Android.Util;
using Java.Net;
using Android.Systems;
using System.Net;
using AndroidX.Core.App;
using DynamicData;

[assembly: Application(UsesCleartextTraffic = true)]

namespace DirectPackageInstaller.Android
{
    [Activity(Label = "DirectPackageInstaller.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        public static int Instances = 0;
        public ClipboardManager? ClipboardManager;

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            SetupEnv();

            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
        
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Instances++ == 0)
            {

                List<string> Permissions = new () {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage,
                    Manifest.Permission.ManageExternalStorage
                };


                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    Permissions.Add(Manifest.Permission.PostNotifications);
                }

                var MissingPermissions = Permissions.Where(x => CheckSelfPermission(x) != Permission.Granted);

                if (MissingPermissions.Any())
                    RequestPermissions(MissingPermissions.ToArray(), 1);

                await IgnoreBatteryOptimizations();

                App.InstallApk = (Path) =>
                {
                    var Install = new Intent(Intent.ActionView);
                    var ApkFile = FileProvider.GetUriForFile(Application.Context, "com.marcussacana.DirectPackageInstaller.provider", new File(Path));
                    Install.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    Install = Install.SetDataAndType(ApkFile,"application/vnd.android.package-archive");
                    StartActivity(Install);
                };

                App.GetIPAddresses = () =>
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    {
                        using ConnectivityManager cm = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);

                        if (cm != null) {
                            Network[] networks = cm.GetAllNetworks();
                            var Props = networks.Select(x => cm.GetLinkProperties(x));

                            if (Props != null)
                                return Props
                                .SelectMany(x => x!.LinkAddresses.ToArray())
                                .Where(x=> x.Address is Inet4Address)
                                .Select(x=> (x.Address as Inet4Address).HostAddress)
                                .ToArray();
                            
                        }
                    }

                    return new string[0];
                };

                App.GetBroadcastAddress = (string IP) =>
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    {
                        using ConnectivityManager cm = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);

                        if (cm != null)
                        {
                            var address = Inet4Address.GetByName(IP);
                            var netInterface = NetworkInterface.GetByInetAddress(address);
                            var addresses = netInterface.InterfaceAddresses;

                            var entries = addresses.Select(x => x.Broadcast).ToArray();

                            var results = entries
                            .Where(x => x != null)
                            .Select(x => x.HostAddress).ToArray();

                            if (results.Any())
                            {
                                return results.First();
                            }

                            return IPAddress.Broadcast.ToString();
                        }
                    }

                    return string.Empty;
                };


                ForegroundService.StartService(this, null);
            }
        }

        private void SetupEnv()
        {
            if (ClipboardManager != null)
                return;

            ClipboardManager = (ClipboardManager)GetSystemService(ClipboardService);

            App.GetClipboardText = () =>
            {
                if (ClipboardManager == null)
                    return null;

                var PrimaryClip = ClipboardManager.PrimaryClip;

                if (PrimaryClip == null)
                    return null;

                if (PrimaryClip.ItemCount != 1)
                    return null;

                var ClipItem = PrimaryClip.GetItemAt(0);

                if (ClipItem == null)
                    return null;

                return ClipItem.CoerceToText(null);
            };

            if (Application == null)
                throw new NullPointerException("Failed to get the application context");

            //https://stackoverflow.com/a/77948201
            System.AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

            var CacheDirs = Application.GetExternalCacheDirs();

            var ExtCacheDir = CacheDirs?.First() ?? Application.CacheDir;
            var SDCardDir = CacheDirs?.Length > 1 ? CacheDirs.Skip(1).MaxBy(x => x.FreeSpace) : null;

            var BaseDir = Application.GetExternalFilesDir(null);

            App._IsAndroid = true;
            App._WorkingDir = BaseDir?.AbsolutePath;
            App.AndroidCacheDir = ExtCacheDir?.AbsolutePath;
            App.AndroidSDCacheDir = SDCardDir?.AbsolutePath;
            App.GetRootDirPermission = () => GetStorageAccess();
            App.AndroidRootInternalDir = Environment.ExternalStorageDirectory?.AbsolutePath;
            //App.GetFreeStorageSpace = () => BaseDir.UsableSpace;

            if (string.IsNullOrWhiteSpace(App.AndroidCacheDir))
                throw new FileNotFoundException("Failed to find the cache directory");

            TempHelper.Clear();
        }

        public async Task GetStorageAccess()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                if (!Environment.IsExternalStorageManager)
                {
                    try
                    {

                        Uri? uri = Uri.Parse("package:" + PackageName);
                        Intent intent = new Intent(global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, uri);
                        await StartActivityAndWait(intent);
                    }
                    catch (Exception ex)
                    {
                        Intent intent = new Intent();
                        intent.SetAction(global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                        await StartActivityAndWait(intent);
                    }
                }
            }
        }

        private Dictionary<int, TaskCompletionSource> Tasks = new();
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (Tasks.ContainsKey(requestCode))
                Tasks[requestCode].SetResult();
        }

        protected override async void OnPause()
        {
            base.OnPause();
            await IgnoreBatteryOptimizations();
        }

        public async Task StartActivityAndWait(Intent? Activity)
        {
            TaskCompletionSource Source = new TaskCompletionSource();
            int ID = Tasks.Count;
            Tasks[ID] = Source;
            
            StartActivityForResult(Activity, Tasks.Count - 1);

            await Source.Task;
        }

        public async Task IgnoreBatteryOptimizations()
        {
            PowerManager? PowerMan = (PowerManager?)GetSystemService(PowerService);
            if (!PowerMan?.IsIgnoringBatteryOptimizations(PackageName) ?? true)
            {
                Intent intent = new Intent();
                intent.SetAction(global::Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(Uri.Parse("package:" + PackageName));
                await StartActivityAndWait(intent);
                
                PowerMan = (PowerManager?)GetSystemService(PowerService);
            }
            
            var Wakelock = PowerMan?.NewWakeLock(WakeLockFlags.Partial, "PartialWakeLock");
            if (Wakelock != null)
            {
                Wakelock.SetReferenceCounted(false);
                Wakelock.Acquire();
            }

            var WifiMan = (WifiManager?) GetSystemService(WifiService);
            if (WifiMan != null)
            {
                var WifiLock = WifiMan.CreateWifiLock(WifiMode.FullHighPerf, "FullHighWifiLock");
                WifiLock?.SetReferenceCounted(false);
                WifiLock?.Acquire();

                var WifiBroadcastLock = WifiMan.CreateMulticastLock("MulticastWifiLock");
                WifiBroadcastLock?.SetReferenceCounted(false);
                WifiBroadcastLock?.Acquire();
            }


            //Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
            }
            catch (Exception ex)
            {
                LogFatalError(ex);
            }
        }

        public static void LogFatalError(Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText(Path.Combine(App.WorkingDirectory, "DPI-AndroidCrash.log"), $"SDK: {Build.VERSION.SdkInt}\n" + ex.ToString());
            }
            catch
            {
            }
        }

        protected override void OnDestroy()
        {
            Instances--;
            
            if (Instances <= 0)
            {
                App.SaveSettings();
                TempHelper.Clear();
            }

            base.OnDestroy();
        }
    }
}