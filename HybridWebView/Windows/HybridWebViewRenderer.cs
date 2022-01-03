using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Compatibility.Platform.UWP;
using Microsoft.Maui.Controls.Platform;
using Plugin.HybridWebView.Shared;
using Plugin.HybridWebView.Shared.Enumerations;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Plugin.HybridWebView.Windows
{
    /// <summary>
    /// Interface for HybridWebView
    /// </summary>
    public class HybridWebViewRenderer : ViewRenderer<HybridWebViewControl, WebView2>
    {
        private static string defaultUserAgent;

        public static event EventHandler<WebView2> OnControlChanged;

        public static string BaseUrl { get; set; } = "ms-appx:///";

        public static void Initialize()
        {
            // ReSharper disable once UnusedVariable
            var dt = DateTime.Now;
        }

        protected override async void OnElementChanged(ElementChangedEventArgs<HybridWebViewControl> e)
        {
            base.OnElementChanged(e);

            if (Control == null && Element != null)
                await SetupControlAsync();

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
            element.OnUserAgentChanged += SetUserAgent;

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
            element.OnUserAgentChanged -= SetUserAgent;

            element.Dispose();
        }

        private async Task SetupControlAsync()
        {
            var control = new WebView2();

            SetNativeControl(control);

            HybridWebViewControl.CallbackAdded += OnCallbackAdded;
            Control.NavigationStarting += OnNavigationStarting;
            Control.NavigationCompleted += OnNavigationCompleted;
            Control.WebMessageReceived += OnWebMessageReceived;
            Control.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;

            await Control.EnsureCoreWebView2Async();
            Control.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
            Control.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
            Control.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            defaultUserAgent = Control.CoreWebView2.Settings.UserAgent;
            SetUserAgent();

            OnControlChanged?.Invoke(this, control);
        }

        private void OnRefreshRequested(object sender, EventArgs e) => SetSource();

        private void OnForwardRequested(object sender, EventArgs e)
        {
            if (Control == null)
                return;

            if (Control.CanGoForward)
                Control.GoForward();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            if (Control == null)
                return;

            if (Control.CanGoBack)
                Control.GoBack();
        }

        private void OnWebViewPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HybridWebViewControl.Source):
                    SetSource();
                    break;
            }
        }

        private void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (Element == null)
                return;

            Element.Navigating = true;
            var handler = Element.HandleNavigationStartRequest(args.Uri ?? Element.Source);
            args.Cancel = handler.Cancel;

            Device.BeginInvokeOnMainThread(() => Element.CurrentUrl = args.Uri);
        }

        private void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            // Add Local Headers
            foreach (var header in Element.LocalRegisteredHeaders)
            {
                if (!args.Request.Headers.Contains(header.Key))
                    args.Request.Headers.SetHeader(header.Key, header.Value);
            }

            // Add Global Headers
            if (Element.EnableGlobalHeaders)
            {
                foreach (var header in HybridWebViewControl.GlobalRegisteredHeaders)
                {
                    if (!args.Request.Headers.Contains(header.Key))
                        args.Request.Headers.SetHeader(header.Key, header.Value);
                }
            }
        }

        private void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (Element == null)
                return;

            if (!args.IsSuccess)
                Element.HandleNavigationError((int)args.WebErrorStatus);

            Element.CanGoBack = Control.CanGoBack;
            Element.CanGoForward = Control.CanGoForward;

            Element.Navigating = false;
            Element.HandleNavigationCompleted(Control.Source.ToString());
        }

        private async void OnDOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            // Add Injection Function
            await Control.ExecuteScriptAsync(HybridWebViewControl.InjectedFunction);

            // Add Global Callbacks
            if (Element.EnableGlobalCallbacks)
            {
                foreach (var callback in HybridWebViewControl.GlobalRegisteredCallbacks)
                    await Control.ExecuteScriptAsync(HybridWebViewControl.GenerateFunctionScript(callback.Key));
            }

            // Add Local Callbacks
            foreach (var callback in Element.LocalRegisteredCallbacks)
                await Control.ExecuteScriptAsync(HybridWebViewControl.GenerateFunctionScript(callback.Key));

            Element.HandleContentLoaded();
        }

        private async void OnCallbackAdded(object sender, string e)
        {
            if (Element == null || String.IsNullOrWhiteSpace(e))
                return;

            if ((sender == null && Element.EnableGlobalCallbacks) || sender != null)
                await OnJavascriptInjectionRequestAsync(HybridWebViewControl.GenerateFunctionScript(e));
        }


        private void OnWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            if (Element == null)
                return;
            Element.HandleScriptReceived(args.TryGetWebMessageAsString());
        }

        private Task OnClearCookiesRequest()
        {
            if (Control == null || Element == null)
                return Task.CompletedTask;

            var cookieManager = Control.CoreWebView2.CookieManager;
            cookieManager.DeleteAllCookies();

            return Task.CompletedTask;
        }

        private Task<string> OnGetAllCookieRequestAsync()
        {
            if (Control == null || Element == null)
                return Task.FromResult(String.Empty);

            var cookieManager = Control.CoreWebView2.CookieManager;
            var cookieList = await cookieManager.GetCookiesAsync(Element.Source);

            var cookie = String.Empty;
            foreach (var currentCookie in cookieList)
            {
                cookie += currentCookie.Name + "=" + currentCookie.Value + "; ";
            }

            if (cookie.Length > 2)
            {
                cookie = cookie.Remove(cookie.Length - 2);
            }

            return Task.FromResult(cookie);
        }

        private Task<string> OnGetCookieRequestAsync(string key)
        {
            if (Control == null || Element == null)
                return Task.FromResult(String.Empty);

            var cookieManager = Control.CoreWebView2.CookieManager;
            var cookieList = await cookieManager.GetCookiesAsync(Element.Source);

            var cookie = String.Empty;
            foreach (var currentCookie in cookieList)
            {
                if (key == currentCookie.Name)
                {
                    cookie = currentCookie.Value;
                    break;
                }
            }

            return Task.FromResult(cookie);
        }

        private Task<string> OnSetCookieRequestAsync(Cookie cookie)
        {
            if (Control == null || Element == null)
                return Task.FromResult(String.Empty);
            var cookieManager = Control.CoreWebView2.CookieManager;
            var webViewCookie = cookieManager.CreateCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);
            webViewCookie.IsHttpOnly = cookie.HttpOnly;
            webViewCookie.IsSecure = cookie.Secure;
            webViewCookie.Expires = new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds();
            cookieManager.AddOrUpdateCookie(webViewCookie);
            return OnGetCookieRequestAsync(webViewCookie.Name);
        }

        private async Task<string> OnJavascriptInjectionRequestAsync(string js)
        {
            if (Control == null)
                return String.Empty;
            var result = await Control.ExecuteScriptAsync(js);
            return result;
        }

        private void SetSource()
        {
            if (Element == null || Control == null || String.IsNullOrWhiteSpace(Element.Source))
                return;

            switch (Element.ContentType)
            {
                case WebViewContentType.Internet:
                    LoadUrl(Element.Source);
                    break;
                case WebViewContentType.StringData:
                    LoadStringData(Element.Source);
                    break;
                case WebViewContentType.LocalFile:
                    LoadUrl(Element.Source);
                    break;
            }
        }

        private void LoadUrl(string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri((Element?.BaseUrl ?? BaseUrl) + url, UriKind.RelativeOrAbsolute);

            Control.Source = uri;
        }

        private void LoadStringData(string source) => Control.NavigateToString(source);

        private void SetUserAgent(object sender = null, EventArgs e = null)
        {
            if (Control != null && Element.UserAgent != null && Element.UserAgent.Length > 0)
            {
                switch (Element.UserAgentMode)
                {
                    case UserAgentMode.Replace:
                        Control.CoreWebView2.Settings.UserAgent = Element.UserAgent;
                        break;
                    case UserAgentMode.Append:
                        Control.CoreWebView2.Settings.UserAgent = $"{defaultUserAgent} {Element.UserAgent}";
                        break;
                    case UserAgentMode.Prepend:
                        Control.CoreWebView2.Settings.UserAgent = $"{Element.UserAgent} {defaultUserAgent}";
                        break;
                }
            }
        }
    }
}