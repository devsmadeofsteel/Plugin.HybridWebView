using Plugin.HybridWebView.Shared;
using Plugin.HybridWebView.Shared.Enumerations;
using Plugin.HybridWebView.UWP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;


[assembly: ExportRenderer(typeof(HybridWebViewControl), typeof(HybridWebViewRenderer))]
namespace Plugin.HybridWebView.UWP
{
    /// <summary>
    /// Interface for HybridWebView
    /// </summary>
    public class HybridWebViewRenderer : ViewRenderer<Shared.HybridWebViewControl, Windows.UI.Xaml.Controls.WebView>
    {
        public static event EventHandler<Windows.UI.Xaml.Controls.WebView> OnControlChanged;

        public static string BaseUrl { get; set; } = "ms-appx:///";
        private LocalFileStreamResolver _resolver;

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
                SetupNewElement(e.NewElement);

            if (e.OldElement != null)
                DestroyOldElement(e.OldElement);
        }

        private void SetupNewElement(HybridWebViewControl element)
        {
            element.PropertyChanged += OnWebViewPropertyChanged;
            element.OnJavascriptInjectionRequest += OnJavascriptInjectionRequestAsync;
            element.OnClearCookiesRequested += OnClearCookiesRequest;
            element.OnGetAllCookiesRequestedAsync += OnGetAllCookieRequestAsync;
            element.OnGetCookieRequestedAsync += OnGetCookieRequestAsync;
            element.OnSetCookieRequestedAsync += OnSetCookieRequestAsync;
            element.OnBackRequested += OnBackRequested;
            element.OnForwardRequested += OnForwardRequested;
            element.OnRefreshRequested += OnRefreshRequested;

            SetSource();
        }

        private void DestroyOldElement(HybridWebViewControl element)
        {
            element.PropertyChanged -= OnWebViewPropertyChanged;
            element.OnJavascriptInjectionRequest -= OnJavascriptInjectionRequestAsync;
            element.OnClearCookiesRequested -= OnClearCookiesRequest;
            element.OnBackRequested -= OnBackRequested;
            element.OnGetAllCookiesRequestedAsync -= OnGetAllCookieRequestAsync;
            element.OnGetCookieRequestedAsync -= OnGetCookieRequestAsync;
            element.OnSetCookieRequestedAsync -= OnSetCookieRequestAsync;
            element.OnForwardRequested -= OnForwardRequested;
            element.OnRefreshRequested -= OnRefreshRequested;

            element.Dispose();
        }

        private void SetupControl()
        {
            var control = new Windows.UI.Xaml.Controls.WebView();
            _resolver = new LocalFileStreamResolver(this);

            SetNativeControl(control);

            HybridWebViewControl.CallbackAdded += OnCallbackAdded;
            Control.NavigationStarting += OnNavigationStarting;
            Control.NavigationCompleted += OnNavigationCompleted;
            Control.DOMContentLoaded += OnDOMContentLoaded;
            Control.ScriptNotify += OnScriptNotify;
            Control.LoadCompleted += SetCurrentUrl;
            Control.DefaultBackgroundColor = Windows.UI.Colors.Transparent;

            OnControlChanged?.Invoke(this, control);
        }

        private void OnRefreshRequested(object sender, EventArgs e)
        {
            SetSource();
        }

        private void OnForwardRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoForward)
                Control.GoForward();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            if (Control == null) return;

            if (Control.CanGoBack)
                Control.GoBack();
        }

        private void OnWebViewPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Source":
                    SetSource();
                    break;
            }
        }

        private void OnNavigationStarting(Windows.UI.Xaml.Controls.WebView sender, WebViewNavigationStartingEventArgs args)
        {

            if (Element == null) return;


            Element.Navigating = true;
            var handler = Element.HandleNavigationStartRequest(args.Uri != null ? args.Uri.AbsoluteUri : Element.Source);
            args.Cancel = handler.Cancel;

            // Try to handle cases with custom user agent. This is kinda not supported by the UWP web-view
            // https://stackoverflow.com/questions/39490430/change-default-user-agent-in-webview-uwp
            if (Element.UserAgent != null && Element.UserAgent.Length > 0)
            {
                // Unsubscribe to avoid eternal loop
                Control.NavigationStarting -= OnNavigationStarting;
                // Cancel navigation, we need to start a new custom one to add the user agent
                args.Cancel = true;
                NavigateWithCustomUserAgent(args, Element.UserAgent);
            }
        }

        private void NavigateWithCustomUserAgent(WebViewNavigationStartingEventArgs args, string userAgent)
        {
            try
            {
                // Create new request with custom user agent
                var requestMsg = new Windows.Web.Http.HttpRequestMessage(HttpMethod.Get, args.Uri);
                requestMsg.Headers.Add("User-Agent", userAgent);
                Control.NavigateWithHttpRequestMessage(requestMsg);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                // Re-subscribe after navigating
                Control.NavigationStarting += OnNavigationStarting;
            }
        }

        private void OnNavigationCompleted(Windows.UI.Xaml.Controls.WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (Element == null) return;

            if (!args.IsSuccess)
                Element.HandleNavigationError((int)args.WebErrorStatus);

            Element.CanGoBack = Control.CanGoBack;
            Element.CanGoForward = Control.CanGoForward;

            Element.Navigating = false;
            Element.HandleNavigationCompleted(args.Uri.ToString());
        }

        private async void OnDOMContentLoaded(Windows.UI.Xaml.Controls.WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            if (Element == null) return;

            // Add Injection Function
            await Control.InvokeScriptAsync("eval", new[] { HybridWebViewControl.InjectedFunction });

            // Add Global Callbacks
            if (Element.EnableGlobalCallbacks)
                foreach (var callback in HybridWebViewControl.GlobalRegisteredCallbacks)
                    await Control.InvokeScriptAsync("eval", new[] { HybridWebViewControl.GenerateFunctionScript(callback.Key) });

            // Add Local Callbacks
            foreach (var callback in Element.LocalRegisteredCallbacks)
                await Control.InvokeScriptAsync("eval", new[] { HybridWebViewControl.GenerateFunctionScript(callback.Key) });

            Element.HandleContentLoaded();
        }

        private async void OnCallbackAdded(object sender, string e)
        {
            if (Element == null || string.IsNullOrWhiteSpace(e)) return;

            if ((sender == null && Element.EnableGlobalCallbacks) || sender != null)
                await OnJavascriptInjectionRequestAsync(HybridWebViewControl.GenerateFunctionScript(e));
        }

        private void OnScriptNotify(object sender, NotifyEventArgs e)
        {
            if (Element == null) return;
            Element.HandleScriptReceived(e.Value);
        }

        private async Task OnClearCookiesRequest()
        {
            if (Control == null) return;


            // This clears all tmp. data. Not only cookies
            await Windows.UI.Xaml.Controls.WebView.ClearTemporaryWebDataAsync();
        }

        private async Task<string> OnGetAllCookieRequestAsync()
        {
            if (Control == null || Element == null) return string.Empty;
            var domain = (new Uri(Element.Source)).Host;
            var cookie = string.Empty;
            var url = new Uri(Element.Source);

            var filter = new HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;
            var cookieCollection = cookieManager.GetCookies(url);

            foreach (var currentCookie in cookieCollection)
            {
                cookie += currentCookie.Name + "=" + currentCookie.Value + "; ";
            }

            if (cookie.Length > 2)
            {
                cookie = cookie.Remove(cookie.Length - 2);
            }
            return cookie;
        }

        private async Task<string> OnGetCookieRequestAsync(string key)
        {
            if (Control == null || Element == null) return string.Empty;
            var url = new Uri(Element.Source);
            var domain = (new Uri(Element.Source)).Host;
            var cookie = string.Empty;

            var filter = new HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;
            var cookieCollection = cookieManager.GetCookies(url);

            foreach (var currentCookie in cookieCollection)
            {
                if (key == currentCookie.Name)
                {
                    cookie = currentCookie.Value;
                    break;
                }
            }

            return cookie;
        }

        private async Task<string> OnSetCookieRequestAsync(Cookie cookie)
        {
            if (Control == null || Element == null) return string.Empty;
            var url = new Uri(Element.Source);
            var newCookie = new HttpCookie(cookie.Name, cookie.Domain, cookie.Path);
            newCookie.Value = cookie.Value;
            newCookie.HttpOnly = cookie.HttpOnly;
            newCookie.Secure = cookie.Secure;
            newCookie.Expires = cookie.Expires;

            var cookieCollection = new List<HttpCookie>();
            var filter = new HttpBaseProtocolFilter();
            HttpClient httpClient;
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            foreach (var knownCookie in cookieCollection)
            {
                filter.CookieManager.SetCookie(knownCookie);
            }

            filter.CookieManager.SetCookie(newCookie);
            httpClient = new HttpClient(filter);

            return await OnGetCookieRequestAsync(cookie.Name);

        }

        private async Task<string> OnJavascriptInjectionRequestAsync(string js)
        {
            if (Control == null) return string.Empty;
            var result = await Control.InvokeScriptAsync("eval", new[] { js });
            return result;
        }

        private void SetSource()
        {
            if (Element == null || Control == null || string.IsNullOrWhiteSpace(Element.Source)) return;

            switch (Element.ContentType)
            {
                case WebViewContentType.Internet:
                    NavigateWithHttpRequest(new Uri(Element.Source));
                    break;
                case WebViewContentType.StringData:
                    LoadStringData(Element.Source);
                    break;
                case WebViewContentType.LocalFile:
                    LoadLocalFile(Element.Source);
                    break;
            }
        }

        private void NavigateWithHttpRequest(Uri uri)
        {
            if (Element == null || Control == null) return;

            var requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);

            // Add Local Headers
            foreach (var header in Element.LocalRegisteredHeaders)
            {
                if (!requestMsg.Headers.ContainsKey(header.Key))
                    requestMsg.Headers.Add(header.Key, header.Value);
            }

            // Add Global Headers
            if (Element.EnableGlobalHeaders)
            {
                foreach (var header in HybridWebViewControl.GlobalRegisteredHeaders)
                {
                    if (!requestMsg.Headers.ContainsKey(header.Key))
                        requestMsg.Headers.Add(header.Key, header.Value);
                }
            }

            // Navigate
            Control.NavigateWithHttpRequestMessage(requestMsg);
        }

        private void LoadLocalFile(string source)
        {
            Control.NavigateToLocalStreamUri(Control.BuildLocalStreamUri("/", source), _resolver);
        }

        private void LoadStringData(string source)
        {
            Control.NavigateToString(source);
        }

        internal string GetBaseUrl()
        {
            return Element?.BaseUrl ?? BaseUrl;
        }

        private Windows.UI.Color ToWindowsColor(Xamarin.Forms.Color color)
        {
            // Make colour safe for Windows
            if (color.A == -1 || color.R == -1 || color.G == -1 || color.B == -1)
                color = Xamarin.Forms.Color.Transparent;

            return Windows.UI.Color.FromArgb(Convert.ToByte(color.A * 255), Convert.ToByte(color.R * 255), Convert.ToByte(color.G * 255), Convert.ToByte(color.B * 255));
        }
        private void SetCurrentUrl(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Element.CurrentUrl = e.Uri.ToString();
            });
        }
    }
}
