using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.OS;
using Android.Webkit;
using Plugin.HybridWebView.Droid;
using Plugin.HybridWebView.Shared;
using Plugin.HybridWebView.Shared.Delegates;
using Plugin.HybridWebView.Shared.Enumerations;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(HybridWebViewControl), typeof(HybridWebViewRenderer))]
namespace Plugin.HybridWebView.Droid
{
    /// <summary>
    /// Interface for HybridWebView
    /// </summary>
    public class HybridWebViewRenderer : ViewRenderer<HybridWebViewControl, Android.Webkit.WebView>
    {
        public static string MimeType = "text/html";

        public static string EncodingType = "UTF-8";

        public static string HistoryUri = "";

        public static string BaseUrl { get; set; } = "file:///android_asset/";

        public static bool IgnoreSslGlobally { get; set; }

        public static event EventHandler<Android.Webkit.WebView> OnControlChanged;

        private JavascriptValueCallback _callback;

        public static void Initialize()
        {
            // ReSharper disable once UnusedVariable
            var dt = DateTime.Now;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<HybridWebViewControl> e)
        {
            base.OnElementChanged(e);

            if (Control == null && Element != null)
                SetupControl();

            if (e.NewElement != null)
                SetupElement(e.NewElement);

            if (e.OldElement != null)
                DestroyElement(e.OldElement);

            if (Element != null && Element.UseWideViewPort)
            {
                if (Control != null)
                {
                    Control.Settings.LoadWithOverviewMode = true;
                    Control.Settings.UseWideViewPort = true;
                    Control.Settings.MediaPlaybackRequiresUserGesture = !Element.AllowMediaAutoplay;
                }
            }

            if (Control != null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    // chromium, enable hardware acceleration
                    Control.SetLayerType(Android.Views.LayerType.Hardware, null);
                }
                else
                {
                    // older android version, disable hardware acceleration
                    Control.SetLayerType(Android.Views.LayerType.Software, null);
                }
            }
        }

        private void SetupElement(HybridWebViewControl element)
        {
            element.PropertyChanged += OnPropertyChanged;
            element.OnJavascriptInjectionRequest += OnJavascriptInjectionRequest;
            element.OnGetCookieRequestedAsync += OnGetCookieRequestAsync;
            element.OnGetAllCookiesRequestedAsync += OnGetAllCookieRequestAsync;
            element.OnSetCookieRequestedAsync += OnSetCookieRequestAsync;
            element.OnClearCookiesRequested += OnClearCookiesRequest;
            element.OnBackRequested += OnBackRequested;
            element.OnForwardRequested += OnForwardRequested;
            element.OnRefreshRequested += OnRefreshRequested;
            element.OnNavigationStarted += SetCurrentUrl;
            element.OnUserAgentChanged += SetUserAgent;

            SetSource();
        }

        private void DestroyElement(HybridWebViewControl element)
        {
            element.PropertyChanged -= OnPropertyChanged;
            element.OnJavascriptInjectionRequest -= OnJavascriptInjectionRequest;
            element.OnClearCookiesRequested -= OnClearCookiesRequest;
            element.OnGetAllCookiesRequestedAsync -= OnGetAllCookieRequestAsync;
            element.OnGetCookieRequestedAsync -= OnGetCookieRequestAsync;
            element.OnSetCookieRequestedAsync -= OnSetCookieRequestAsync;
            element.OnBackRequested -= OnBackRequested;
            element.OnForwardRequested -= OnForwardRequested;
            element.OnRefreshRequested -= OnRefreshRequested;
            element.OnNavigationStarted -= SetCurrentUrl;
            element.OnUserAgentChanged -= SetUserAgent;

            element.Dispose();
        }

        private void SetupControl()
        {
            var webView = new Android.Webkit.WebView(Forms.Context);
            _callback = new JavascriptValueCallback(this);

            // https://github.com/SKLn-Rad/Xam.Plugin.WebView.Webview/issues/11
            webView.LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

            // Defaults
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;


            webView.AddJavascriptInterface(new HybridWebViewBridge(this), "bridge");
            webView.SetWebViewClient(new HybridWebViewClient(this));
            webView.SetWebChromeClient(new HybridWebViewChromeClient(this));
            webView.SetBackgroundColor(Android.Graphics.Color.Transparent);

            HybridWebViewControl.CallbackAdded += OnCallbackAdded;
            SetNativeControl(webView);
            SetUserAgent();
            OnControlChanged?.Invoke(this, webView);
        }

        private async void OnCallbackAdded(object sender, string e)
        {
            if (Element == null || string.IsNullOrWhiteSpace(e)) return;

            if ((sender == null && Element.EnableGlobalCallbacks) || sender != null)
                await OnJavascriptInjectionRequest(HybridWebViewControl.GenerateFunctionScript(e));
        }

        private void OnForwardRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoForward())
                Control.GoForward();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoBack())
                Control.GoBack();
        }

        private void OnRefreshRequested(object sender, EventArgs e)
        {
            Control?.Reload();
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Source":
                    SetSource();
                    break;
                case nameof(HybridWebViewControl.AllowUniversalAccessFromFileURLs):
                    if (Control != null && Element != null)
                        Control.Settings.AllowUniversalAccessFromFileURLs = Element.AllowUniversalAccessFromFileURLs;
                    break;
                case "AllowMediaAutoplay":
                    SetMediaAutoplay();
                    break;
            }
        }

        private Task OnClearCookiesRequest()
        {
            if (Control == null) return Task.CompletedTask;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
            {
                CookieManager.Instance.RemoveAllCookies(null);
                CookieManager.Instance.Flush();
            }
            else
            {
                //CookieSyncManager cookieSyncMngr = CookieSyncManager.createInstance(context);
                var cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                cookieSyncMngr.StartSync();
                var cookieManager = CookieManager.Instance;
                cookieManager.RemoveAllCookie();
                cookieManager.RemoveSessionCookie();
                cookieSyncMngr.StopSync();
                cookieSyncMngr.Sync();
            }
            
            return Task.CompletedTask;
        }


        private Task<string> OnGetAllCookieRequestAsync()
        {
            if (Control == null || Element == null) return Task.FromResult(string.Empty);
            var cookies = string.Empty;

            if (Control != null && Element != null)
            {
                var url = string.Empty;
                try
                {
                    url = Control.Url;
                }
                catch (Exception)
                {
                    url = Element.BaseUrl;
                }
                if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
                {
                    CookieManager.Instance.Flush();
                    cookies = CookieManager.Instance.GetCookie(url);
                }
                else
                {
                    var cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                    cookieSyncMngr.StartSync();
                    var cookieManager = CookieManager.Instance;
                    cookies = cookieManager.GetCookie(url);
                }
            }

            return Task.FromResult(cookies);
        }

        private async Task<string> OnSetCookieRequestAsync(Cookie cookie)
        {
            if (Control != null && Element != null)
            {
                var url = new Uri(Control.Url).Host;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
                {

                    CookieManager.Instance.SetCookie(url, cookie.ToString());
                    CookieManager.Instance.Flush();
                }
                else
                {
                    var cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                    cookieSyncMngr.StartSync();
                    var cookieManager = CookieManager.Instance;
                    cookieManager.SetCookie(url, cookie.ToString());
                    cookieManager.Flush();
                }
            }

            var toReturn = await OnGetCookieRequestAsync(cookie.Name);

            return toReturn;
        }



        private async Task<string> OnGetCookieRequestAsync(string key)
        {

            return await Task.Run(() =>
            {
                var cookie = default(string);

                if (Control != null && Element != null)
                {
                    var url = string.Empty;
                    try
                    {
                        url = Control.Url;
                    }
                    catch (Exception e)
                    {
                        url = Element.BaseUrl;
                    }

                    string cookieCollectionString;
                    string[] cookieCollection;

                    try
                    {
                        if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                        {
                            CookieManager.Instance.Flush();
                            cookieCollectionString = CookieManager.Instance.GetCookie(url);

                        }
                        else
                        {
                            var cookieSyncMngr = CookieSyncManager.CreateInstance(Context);
                            cookieSyncMngr.StartSync();
                            var cookieManager = CookieManager.Instance;
                            cookieCollectionString = cookieManager.GetCookie(url);
                        }

                        cookieCollection = cookieCollectionString.Split(new string[] {"; "}, StringSplitOptions.None);

                        foreach (var c in cookieCollection)
                        {
                            var keyValue = c.Split(new[] {'='}, 2);
                            if (keyValue.Length > 1 && keyValue[0] == key)
                            {
                                cookie = keyValue[1];
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                return cookie;
            });
        }


        internal async Task<string> OnJavascriptInjectionRequest(string js)
        {
            if (Element == null || Control == null) return string.Empty;

            // fire!
            _callback.Reset();

            var response = string.Empty;

            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Control.EvaluateJavascript(js, _callback);
                }
                catch (Exception)
                {
                    //ignore
                }
            });

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

            SetCurrentUrl();
        }

        private void SetMediaAutoplay()
        {
            if (Element == null || Control == null || Control.Settings == null) return;
            Control.Settings.MediaPlaybackRequiresUserGesture = !Element.AllowMediaAutoplay;
        }

        private void LoadFromString()
        {
            if (Element == null || Control == null || Element.Source == null) return;

            // Check cancellation
            var handler = Element.HandleNavigationStartRequest(Element.Source);
            if (handler.Cancel) return;

            // Load
            Control.LoadDataWithBaseURL(Element.BaseUrl ?? BaseUrl, Element.Source, MimeType, EncodingType, HistoryUri);
        }

        private void LoadFromFile()
        {
            if (Element == null || Control == null || Element.Source == null) return;

            Control.LoadUrl(Path.Combine(Element.BaseUrl ?? BaseUrl, Element.Source));
        }

        private void LoadFromInternet()
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
                foreach (var header in HybridWebViewControl.GlobalRegisteredHeaders)
                {
                    if (!headers.ContainsKey(header.Key))
                        headers.Add(header.Key, header.Value);
                }
            }

            Control.LoadUrl(Element.Source, headers);
        }


        private void SetCurrentUrl(object sender, DecisionHandlerDelegate e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (Element != null)
                {
                    Element.CurrentUrl = e.Uri;
                }
            });
        }

        private void SetCurrentUrl()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Element.CurrentUrl = Control.Url;
            });
        }

        private void SetUserAgent(object sender = null, EventArgs e = null)
        {
            if (Control != null && Element.UserAgent != null && Element.UserAgent.Length > 0)
            {
                Control.Settings.UserAgentString = Element.UserAgent;
            }
        }
    }
}
