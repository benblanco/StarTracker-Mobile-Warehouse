using Android;
using Android.Annotation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using AlertDialog = Android.App.AlertDialog;

namespace StarTracker_Mobile_Warehouse
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public static IValueCallback mUploadMessage;

        public static int FILECHOOSER_RESULTCODE = 1;
        public static MainActivity Instance;

        WebView webView;

        //Button btnHome;
        //Button btnBack;
        //Button btnForward;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.WebViewLayout);
            webView = FindViewById<WebView>(2131230955);

            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            webView.Settings.DomStorageEnabled = true;
            webView.Settings.UseWideViewPort = true;
            webView.Settings.LoadWithOverviewMode = true;
            webView.Settings.DefaultTextEncodingName = "UTF-8";

            webView.Settings.AllowFileAccess = true;
            webView.Settings.AllowContentAccess = true;
            
            webView.Settings.SetPluginState(WebSettings.PluginState.On);
            webView.ClearCache(true);
            webView.Settings.MediaPlaybackRequiresUserGesture = false;
            webView.Settings.BuiltInZoomControls = true;
            webView.Settings.SetSupportZoom(true);
            webView.ScrollBarStyle = ScrollbarStyles.OutsideOverlay;
            webView.ScrollbarFadingEnabled = false;

            webView.SetWebChromeClient(new STChromeClient(this));
            webView.SetWebViewClient(new STWebViewClient(this));

            webView.LoadUrl("https://forums.xamarin.com/");
            Instance = this;

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted)
            {
                RequestPermissions(new string[] { Manifest.Permission.Camera, Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, 0);
            }
            //btnHome = FindViewById<Button>(Resource.Id.btnHome);
            //btnBack = FindViewById<Button>(Resource.Id.btnBack);
            //btnForward = FindViewById<Button>(Resource.Id.btnForward);
            //btnHome.Click += BtnHome_Click;
            //btnBack.Click += BtnBack_Click;
            //btnForward.Click += BtnForward_Click;            
    }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            if (requestCode == FILECHOOSER_RESULTCODE)
            {
                if (null == mUploadMessage) return;
                Android.Net.Uri result = intent.Data;
                var l = new Uri(result.ToString());              
                mUploadMessage.OnReceiveValue(result);  //new Uri[] { result }
                mUploadMessage = null;
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        public override bool OnKeyDown(Android.Views.Keycode keyCode, Android.Views.KeyEvent e)
        {
            if (keyCode == Keycode.Back && webView.CanGoBack())
            {
                webView.GoBack();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
        private void BtnHome_Click(object sender, System.EventArgs e)
        {
            webView.LoadUrl("https://forums.xamarin.com/");
        }
        private void BtnBack_Click(object sender, System.EventArgs e)
        {
            webView.GoBack();
        }
        private void BtnForward_Click(object sender, System.EventArgs e)
        {
            webView.GoForward();
        }
        public class STWebViewClient : WebViewClient
        {
            private Activity _activity;
            public STWebViewClient(Activity activity)
            {
                _activity = activity;
            }
            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                view.LoadUrl(request.Url.ToString());
                return false;
            }
            [Obsolete("deprecated")]
            public override void OnTooManyRedirects(WebView view, Message cancelMsg, Message continueMsg)
            {
                view.LoadUrl("https://forums.xamarin.com/");
                base.OnTooManyRedirects(view, cancelMsg, continueMsg);
            }

            public override void OnReceivedError(WebView view, IWebResourceRequest request, WebResourceError error)
            {
                Toast.MakeText(view.Context, error.Description, ToastLength.Long).Show();
                base.OnReceivedError(view, request, error);
            }
        }
        public class STChromeClient : WebChromeClient 
        {
            private Activity _activity;
            public STChromeClient(Activity activity)
            {
                _activity = activity;
            }
            public override void OnPermissionRequest(PermissionRequest request)
            {
                request.Grant(request.GetResources());
            }
            string _photoPath;
            public override bool OnShowFileChooser(Android.Webkit.WebView webView, Android.Webkit.IValueCallback filePathCallback, FileChooserParams fileChooserParams)
            {

                AlertDialog.Builder alertDialog = new AlertDialog.Builder(MainActivity.Instance);
                alertDialog.SetTitle("Take picture or choose a file");
                alertDialog.SetNeutralButton("Take picture", async (sender, alertArgs) =>
                {
                    try
                    {
                        var photo = await MediaPicker.CapturePhotoAsync();
                        var uri = await LoadPhotoAsync(photo);
                        filePathCallback.OnReceiveValue(uri);
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
                    }
                });
                alertDialog.SetNegativeButton("Choose picture", async (sender, alertArgs) =>
                {
                    try
                    {
                        var photo = await MediaPicker.PickPhotoAsync();
                        var uri = await LoadPhotoAsync(photo);
                        filePathCallback.OnReceiveValue(uri);
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"PickPhotoAsync THREW: {ex.Message}");
                    }
                });
                alertDialog.SetPositiveButton("Cancel", (sender, alertArgs) =>
                {
                    filePathCallback.OnReceiveValue(null);
                });
                Dialog dialog = alertDialog.Create();
                dialog.Show();
                return true;
            }

            async Task<Android.Net.Uri[]> LoadPhotoAsync(FileResult photo)
            {
                // cancelled
                if (photo == null)
                {
                    _photoPath = null;
                    return null;
                }
                // save the file into local storage
                var newFile = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using (var stream = await photo.OpenReadAsync())
                using (var newStream = System.IO.File.OpenWrite(newFile))
                    await stream.CopyToAsync(newStream);
                _photoPath = newFile;
                Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(_photoPath));
                return new Android.Net.Uri[] { uri };
            }
        }
    }
}
