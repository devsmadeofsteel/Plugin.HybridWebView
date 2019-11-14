using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.OS;
using Android.Webkit;
using Plugin.HybridWebView.Droid;
using Plugin.HybridWebView.Shared;
using Plugin.HybridWebView.Shared.Enumerations;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(HybridWebViewControl), typeof(HybridWebViewRenderer))]
namespace Plugin.HybridWebView.Droid
{
    /// <summary>
    /// Interface for HybridWebView
    /// </summary>
    public class HybridWebViewRenderer : ViewRenderer<Shared.HybridWebViewControl, Android.Webkit.WebView>
    {
        public static string MimeType = "text/html";

        public static string EncodingType = "UTF-8";

        public static string HistoryUri = "";

        public static string BaseUrl { get; set; } = "file:///android_asset/";

        public static bool IgnoreSSLGlobally { get; set; }

        public static event EventHandler<Android.Webkit.WebView> OnControlChanged;

        JavascriptValueCallback _callback;

        public static void Initialize()
        {
            var dt = DateTime.Now;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Shared.HybridWebViewControl> e)
        {
            base.OnElementChanged(e);

            if (Control == null && Element != null)
                SetupControl();

            if (e.NewElement != null)
                SetupElement(e.NewElement);

            if (e.OldElement != null)
                DestroyElement(e.OldElement);

            if (Element.UseWideViewPort)
            {
                Control.Settings.LoadWithOverviewMode = true;
                Control.Settings.UseWideViewPort = true;
            }
        }
        void SetupElement(Shared.HybridWebViewControl element)
        {
            element.PropertyChanged += OnPropertyChanged;
            element.OnJavascriptInjectionRequest += OnJavascriptInjectionRequest;
            element.OnGetCookieValueRequested += OnGetCookieValueRequest;
            element.OnGetAllCookiesRequested += OnGetAllCookieRequest;
            element.OnSetCookieValueRequested += OnSetCookieValueRequest;
            element.OnClearCookiesRequested += OnClearCookiesRequest;
            element.OnBackRequested += OnBackRequested;
            element.OnForwardRequested += OnForwardRequested;
            element.OnRefreshRequested += OnRefreshRequested;

            SetSource();
        }

        void DestroyElement(Shared.HybridWebViewControl element)
        {
            element.PropertyChanged -= OnPropertyChanged;
            element.OnJavascriptInjectionRequest -= OnJavascriptInjectionRequest;
            element.OnGetAllCookiesRequested -= OnGetAllCookieRequest;
            element.OnGetCookieValueRequested -= OnGetCookieValueRequest;
            element.OnSetCookieValueRequested -= OnSetCookieValueRequest;
            element.OnClearCookiesRequested -= OnClearCookiesRequest;
            element.OnBackRequested -= OnBackRequested;
            element.OnForwardRequested -= OnForwardRequested;
            element.OnRefreshRequested -= OnRefreshRequested;

            element.Dispose();
        }

        void SetupControl()
        {
            var webView = new Android.Webkit.WebView(Forms.Context);
            _callback = new JavascriptValueCallback(this);

            // https://github.com/SKLn-Rad/Xam.Plugin.WebView.Webview/issues/11
            webView.LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

            // Defaults
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.AddJavascriptInterface(new FormsWebViewBridge(this), "bridge");
            webView.SetWebViewClient(new FormsWebViewClient(this));
            webView.SetWebChromeClient(new FormsWebViewChromeClient(this));
            webView.SetBackgroundColor(Android.Graphics.Color.Transparent);

            Shared.HybridWebViewControl.CallbackAdded += OnCallbackAdded;

            SetNativeControl(webView);
            OnControlChanged?.Invoke(this, webView);
        }

        async void OnCallbackAdded(object sender, string e)
        {
            if (Element == null || string.IsNullOrWhiteSpace(e)) return;

            if ((sender == null && Element.EnableGlobalCallbacks) || sender != null)
                await OnJavascriptInjectionRequest(Shared.HybridWebViewControl.GenerateFunctionScript(e));
        }

        void OnForwardRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoForward())
                Control.GoForward();
        }

        void OnBackRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoBack())
                Control.GoBack();
        }

        void OnRefreshRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            Control.Reload();
        }

        void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Source":
                    SetSource();
                    break;
            }
        }

        private async Task OnClearCookiesRequest()
        {
            await Task.Run(() =>
            {
                if (Control == null) return;

                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                {
                    CookieManager.Instance.RemoveAllCookies(null);
                    CookieManager.Instance.Flush();
                }
                else
                {
                    //CookieSyncManager cookieSyncMngr = CookieSyncManager.createInstance(context);
                    CookieSyncManager cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                    cookieSyncMngr.StartSync();
                    CookieManager cookieManager = CookieManager.Instance;
                    cookieManager.RemoveAllCookie();
                    cookieManager.RemoveSessionCookie();
                    cookieSyncMngr.StopSync();
                    cookieSyncMngr.Sync();
                }
            });
        }

        /* Returns all cookies for the current page */

        private async Task<string> OnGetAllCookieRequest()
        {
            if (Control == null || Element == null) return string.Empty;
            var cookies = string.Empty;
            await Task.Run(() =>
            {
                if (Control != null && Element != null)
                {
                    var url = Element.Source;
                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                    {
                        CookieManager.Instance.Flush();
                        cookies = CookieManager.Instance.GetCookie(url);
                    }
                    else
                    {
                        //CookieSyncManager cookieSyncMngr = CookieSyncManager.createInstance(context);
                        CookieSyncManager cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                        cookieSyncMngr.StartSync();
                        CookieManager cookieManager = CookieManager.Instance;
                        cookies = cookieManager.GetCookie(url);
                    }
                }

            });

            return cookies;
        }

        /* Sets cookie value based on cookiename. */

        private async Task OnSetCookieValueRequest(string cookieName, string cookieValue, long? duration = null)
        {
            // wait!
            await Task.Run(() =>
            {
                if (Control != null && Element != null)
                {
                    var url = Element.Source;
                    //Console.WriteLine(Control.)
                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                    {

                        CookieManager.Instance.SetCookie(url, cookieName + "=" + cookieValue);
                        CookieManager.Instance.Flush();
                    }
                    else
                    {
                        CookieSyncManager cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                        cookieSyncMngr.StartSync();
                        CookieManager cookieManager = CookieManager.Instance;
                        cookieManager.SetCookie(url, cookieName + "=" + cookieValue);
                        cookieManager.Flush();
                    }
                }

            });
        }

        /* Gets cookie value based on cookiename. */

        private async Task<string> OnGetCookieValueRequest(string cookieName)
        {

            var cookie = default(string);
            // wait!
            await Task.Run(() =>
            {
                if (Control != null && Element != null)
                {
                    var url = Element.Source;
                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                    {
                        CookieManager.Instance.Flush();
                        string[] cookieCollection = CookieManager.Instance.GetCookie(url).Split(new string[] { "; " }, StringSplitOptions.None);

                        foreach (var c in cookieCollection)
                        {
                            var keyValue = c.Split(new[] { '=' }, 2);
                            if (keyValue[0] == cookieName)
                            {
                                cookie = keyValue[1];
                                break;
                            }
                        }
                    }
                    else
                    {
                        //CookieSyncManager cookieSyncMngr = CookieSyncManager.createInstance(context);
                        CookieSyncManager cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                        cookieSyncMngr.StartSync();
                        CookieManager cookieManager = CookieManager.Instance;
                        string[] cookieCollection = cookieManager.GetCookie(url).Split(new string[] { "; " }, StringSplitOptions.None);
                        foreach (var c in cookieCollection)
                        {
                            var keyValue = c.Split(new[] { '=' }, 2);
                            if (keyValue[0] == cookieName)
                            {
                                cookie = keyValue[1];
                                break;
                            }
                        }
                    }
                }

            });

            return cookie;
        }

        internal async Task<string> OnJavascriptInjectionRequest(string js)
        {
            if (Element == null || Control == null) return string.Empty;

            // fire!
            _callback.Reset();

            var response = string.Empty;

            Device.BeginInvokeOnMainThread(() => Control.EvaluateJavascript(js, _callback));

            // wait!
            await Task.Run(() =>
            {
                while (_callback.Value == null) { }

                // Get the string and strip off the quotes
                if (_callback.Value is Java.Lang.String)
                {
                    // Unescape that damn Unicode Java bull.
                    response = Regex.Replace(_callback.Value.ToString(), @"\\[Uu]([0-9A-Fa-f]{4})", m => char.ToString((char)ushort.Parse(m.Groups[1].Value, NumberStyles.AllowHexSpecifier)));
                    response = Regex.Unescape(response);

                    if (response.Equals("\"null\""))
                        response = null;

                    else if (response.StartsWith("\"") && response.EndsWith("\""))
                        response = response.Substring(1, response.Length - 2);
                }

            });

            // return
            return response;
        }

        internal void SetSource()
        {
            if (Element == null || Control == null || string.IsNullOrWhiteSpace(Element.Source)) return;

            switch (Element.ContentType)
            {
                case WebViewContentType.Internet:
                    LoadFromInternet();
                    break;

                case WebViewContentType.LocalFile:
                    LoadFromFile();
                    break;

                case WebViewContentType.StringData:
                    LoadFromString();
                    break;
            }
        }

        void LoadFromString()
        {
            if (Element == null || Control == null || Element.Source == null) return;

            // Check cancellation
            var handler = Element.HandleNavigationStartRequest(Element.Source);
            if (handler.Cancel) return;

            // Load
            Control.LoadDataWithBaseURL(Element.BaseUrl ?? BaseUrl, Element.Source, MimeType, EncodingType, HistoryUri);
        }

        void LoadFromFile()
        {
            if (Element == null || Control == null || Element.Source == null) return;

            Control.LoadUrl(Path.Combine(Element.BaseUrl ?? BaseUrl, Element.Source));
        }

        void LoadFromInternet()
        {
            if (Element == null || Control == null || Element.Source == null) return;

            var headers = new Dictionary<string, string>();

            // Add Local Headers
            foreach (var header in Element.LocalRegisteredHeaders)
            {
                if (!headers.ContainsKey(header.Key))
                    headers.Add(header.Key, header.Value);
            }

            // Add Global Headers
            if (Element.EnableGlobalHeaders)
            {
                foreach (var header in Shared.HybridWebViewControl.GlobalRegisteredHeaders)
                {
                    if (!headers.ContainsKey(header.Key))
                        headers.Add(header.Key, header.Value);
                }
            }

            Control.LoadUrl(Element.Source, headers);
        }
    }
}
