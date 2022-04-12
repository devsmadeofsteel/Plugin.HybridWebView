﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using Plugin.HybridWebView.iOS;
using Plugin.HybridWebView.Shared;
using Plugin.HybridWebView.Shared.Enumerations;
using WebKit;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Xamarin.Forms;
using System.Net;

[assembly: ExportRenderer(typeof(HybridWebViewControl), typeof(HybridWebViewRenderer))]
namespace Plugin.HybridWebView.iOS
{
    /// <summary>
    /// Interface for HybridWebView
    /// </summary>
    public class HybridWebViewRenderer : ViewRenderer<HybridWebViewControl, WKWebView>, IWKScriptMessageHandler, IWKUIDelegate
    {
        public static event EventHandler<WKWebView> OnControlChanged;

        public static string BaseUrl { get; set; } = $"{NSBundle.MainBundle.BundlePath}/";

        private HybridWebViewNavigationDelegate _navigationDelegate;

        private WKWebViewConfiguration _configuration;

        private WKUserContentController _contentController;

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
        }

        private void SetupElement(HybridWebViewControl element)
        {
            NSHttpCookieStorage.SharedStorage.AcceptPolicy = NSHttpCookieAcceptPolicy.Always;
            element.PropertyChanged += OnPropertyChanged;
            element.OnJavascriptInjectionRequest += OnJavascriptInjectionRequest;
            element.OnClearCookiesRequested += OnClearCookiesRequest;
            element.OnGetAllCookiesRequestedAsync += OnGetAllCookiesRequestAsync;
            element.OnGetCookieRequestedAsync += OnGetCookieRequestAsync;
            element.OnSetCookieRequestedAsync += OnSetCookieRequestAsync;
            element.OnBackRequested += OnBackRequested;
            element.OnForwardRequested += OnForwardRequested;
            element.OnRefreshRequested += OnRefreshRequested;
            element.OnUserAgentChanged += SetUserAgent;

            SetSource();
        }

        private void DestroyElement(HybridWebViewControl element)
        {
            element.PropertyChanged -= OnPropertyChanged;
            element.OnJavascriptInjectionRequest -= OnJavascriptInjectionRequest;
            element.OnClearCookiesRequested -= OnClearCookiesRequest;
            element.OnGetAllCookiesRequestedAsync += OnGetAllCookiesRequestAsync;
            element.OnGetCookieRequestedAsync += OnGetCookieRequestAsync;
            element.OnSetCookieRequestedAsync += OnSetCookieRequestAsync;
            element.OnBackRequested -= OnBackRequested;
            element.OnForwardRequested -= OnForwardRequested;
            element.OnRefreshRequested -= OnRefreshRequested;
            element.OnUserAgentChanged -= SetUserAgent;

            element.Dispose();
        }

        private void SetupControl()
        {
            _navigationDelegate = new HybridWebViewNavigationDelegate(this);
            _contentController = new WKUserContentController();
            _contentController.AddScriptMessageHandler(this, "invokeAction");
            _configuration = new WKWebViewConfiguration
            {
                UserContentController = _contentController,
                AllowsInlineMediaPlayback = true
            };


            var wkWebView = new WKWebView(Frame, _configuration)
            {
                Opaque = false,
                UIDelegate = this,
                NavigationDelegate = _navigationDelegate,
            };


            HybridWebViewControl.CallbackAdded += OnCallbackAdded;

            SetNativeControl(wkWebView);
            SetUserAgent();
            OnControlChanged?.Invoke(this, wkWebView);
        }

        private async void OnCallbackAdded(object sender, string e)
        {
            if (Element == null || string.IsNullOrWhiteSpace(e)) return;

            if ((sender == null && Element.EnableGlobalCallbacks) || sender != null)
                await OnJavascriptInjectionRequest(HybridWebViewControl.GenerateFunctionScript(e));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
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
            if (Control == null) return;

            var store = _configuration.WebsiteDataStore.HttpCookieStore;

            var cookies = await store.GetAllCookiesAsync();
            foreach (var c in cookies)
            {
                await store.DeleteCookieAsync(c);
            }

            if (Uri.TryCreate(Element.Source, UriKind.RelativeOrAbsolute, out var url))
            {
                var sharedCookies = NSHttpCookieStorage.SharedStorage.CookiesForUrl(url);
                foreach (var c in sharedCookies)
                {
                    NSHttpCookieStorage.SharedStorage.DeleteCookie(c);
                }
            }
        }

        private async Task<string> OnSetCookieRequestAsync(Cookie cookie)
        {
            if (Control == null) return string.Empty;
            var toReturn = string.Empty;
            try
            {
                var domain = Control.Url.Host;
                var newCookie = new NSHttpCookie(cookie);

                NSHttpCookieStorage.SharedStorage.SetCookie(newCookie);

                var store = _configuration.WebsiteDataStore.HttpCookieStore;
                store.SetCookie(newCookie, () => { });

                toReturn = await OnGetCookieRequestAsync(cookie.Name);

            }
            catch (Exception e)
            {
                Console.WriteLine("We had a crash " + e);
                toReturn = string.Empty;
            }
            return toReturn;
        }

        private async Task<string> OnGetAllCookiesRequestAsync()
        {
            if (Control == null || Element == null)
            {
                return string.Empty;
            }
            var cookieCollection = string.Empty;
            var url = Control.Url;

            var sharedCookies = NSHttpCookieStorage.SharedStorage.CookiesForUrl(url);
            foreach (var c in sharedCookies)
            {
                if (c.Domain == url.Host)
                {
                    cookieCollection += c.Name + "=" + c.Value + "; ";
                }
            }

            var store = _configuration.WebsiteDataStore.HttpCookieStore;

            var cookies = await store.GetAllCookiesAsync();

            foreach (var c in cookies)
            {
                if (url.Host.Contains(c.Domain))
                {
                    cookieCollection += c.Name + "=" + c.Value + "; ";
                }
            }

            if (cookieCollection.Length > 0)
            {
                cookieCollection = cookieCollection.Remove(cookieCollection.Length - 2);
            }

            return cookieCollection;
        }

        private async Task<string> OnGetCookieRequestAsync(string key)
        {
            if (Control?.Url == null || Element == null) return string.Empty;
            var url = Control.Url;
            var toReturn = string.Empty;

            var store = _configuration.WebsiteDataStore.HttpCookieStore;

            var cookies = await store.GetAllCookiesAsync();
            foreach (var c in cookies)
            {
                if (c.Name == key && c.Domain == url.Host)
                    return c.Value;
            }

            var sharedCookies = NSHttpCookieStorage.SharedStorage.CookiesForUrl(url);
            foreach (var c in sharedCookies)
            {
                if (c.Name == key && c.Domain == url.Host)
                {
                    return c.Value;
                }
            }

            return string.Empty;
        }

        internal async Task<string> OnJavascriptInjectionRequest(string js)
        {
            if (Control == null || Element == null) return string.Empty;

            var response = string.Empty;

            try
            {
                var obj = await Control.EvaluateJavaScriptAsync(js).ConfigureAwait(true);
                if (obj != null)
                    response = obj.ToString();
            }

            catch (Exception) { /* The Webview might not be ready... */ }
            return response;
        }

        private void SetSource()
        {
            if (Element == null || Control == null || string.IsNullOrWhiteSpace(Element.Source)) return;

            switch (Element.ContentType)
            {
                case WebViewContentType.Internet:
                    LoadInternetContent();
                    break;

                case WebViewContentType.LocalFile:
                    LoadLocalFile();
                    break;

                case WebViewContentType.StringData:
                    LoadStringData();
                    break;
            }
        }

        private void LoadStringData()
        {
            if (Control == null || Element == null) return;

            var nsBaseUri = new NSUrl($"file://{Element.BaseUrl ?? BaseUrl}");
            try
            {
                string iOSHtmlWithScaling = Element.Source.Insert(0,
                    "<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1' />");
                Control.LoadHtmlString(iOSHtmlWithScaling, nsBaseUri);
            }
            catch (Exception ex)
            {
                Control.LoadHtmlString(Element.Source, nsBaseUri);
            }
        }

        private void LoadLocalFile()
        {
            if (Control == null || Element == null) return;

            var path = Path.Combine(Element.BaseUrl ?? BaseUrl, Element.Source);
            var nsFileUri = new NSUrl($"file://{path}");
            var nsBaseUri = new NSUrl($"file://{Element.BaseUrl ?? BaseUrl}");

            Control.LoadFileUrl(nsFileUri, nsBaseUri);
        }

        private void LoadInternetContent()
        {
            if (Control == null || Element == null) return;

            var headers = new NSMutableDictionary();

            foreach (var header in Element.LocalRegisteredHeaders)
            {
                var key = new NSString(header.Key);
                if (!headers.ContainsKey(key))
                    headers.Add(key, new NSString(header.Value));
            }

            if (Element.EnableGlobalHeaders)
            {
                foreach (var header in HybridWebViewControl.GlobalRegisteredHeaders)
                {
                    var key = new NSString(header.Key);
                    if (!headers.ContainsKey(key))
                        headers.Add(key, new NSString(header.Value));
                }
            }

            var url = new NSUrl(Element.Source);
            var request = new NSMutableUrlRequest(url)
            {
                Headers = headers
            };

            Control.LoadRequest(request);
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            if (Element == null || message == null || message.Body == null) return;
            Element.HandleScriptReceived(message.Body.ToString());
        }

        private void OnRefreshRequested(object sender, EventArgs e)
        {
            if (Control == null) return;
            Control.ReloadFromOrigin();
        }

        private void OnForwardRequested(object sender, EventArgs e)
        {
            if (Control == null || Element == null) return;

            if (Control.CanGoForward)
                Control.GoForward();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            if (Control == null || Element == null) return;

            if (Control.CanGoBack)
                Control.GoBack();
        }

        /**
         * UI Delegate methods from: https://developer.xamarin.com/recipes/ios/content_controls/web_view/handle_javascript_alerts/
         */

        [Export("webView:runJavaScriptAlertPanelWithMessage:initiatedByFrame:completionHandler:")]
        public void RunJavaScriptAlertPanel(WebKit.WKWebView webView, string message, WKFrameInfo frame, Action completionHandler)
        {
            var alertController = UIAlertController.Create(null, message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, null));
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);

            completionHandler();
        }



        [Export("webView:runJavaScriptConfirmPanelWithMessage:initiatedByFrame:completionHandler:")]
        public void RunJavaScriptConfirmPanel(WKWebView webView, string message, WKFrameInfo frame, Action<bool> completionHandler)
        {
            var alertController = UIAlertController.Create(null, message, UIAlertControllerStyle.Alert);

            alertController.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, okAction =>
            {

                completionHandler(true);

            }));

            alertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Default, cancelAction =>
            {

                completionHandler(false);

            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);
        }



        [Export("webView:runJavaScriptTextInputPanelWithPrompt:defaultText:initiatedByFrame:completionHandler:")]
        public void RunJavaScriptTextInputPanel(WKWebView webView, string prompt, string defaultText, WebKit.WKFrameInfo frame, System.Action<string> completionHandler)
        {
            var alertController = UIAlertController.Create(null, prompt, UIAlertControllerStyle.Alert);

            UITextField alertTextField = null;
            alertController.AddTextField(textField =>
            {
                textField.Placeholder = defaultText;
                alertTextField = textField;
            });

            alertController.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, okAction =>
            {

                completionHandler(alertTextField.Text);

            }));

            alertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Default, cancelAction =>
            {

                completionHandler(null);

            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);
        }
        private void SetUserAgent(object sender = null, EventArgs e = null)
        {
            if (Control != null && Element.UserAgent != null && Element.UserAgent.Length > 0)
            {
                Control.CustomUserAgent = Element.UserAgent;
            }
        }
    }
}
