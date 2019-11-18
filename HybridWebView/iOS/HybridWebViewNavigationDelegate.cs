using System;
using Foundation;
using Plugin.HybridWebView.Shared;
using WebKit;
using UIKit;
using Xamarin.Forms;

namespace Plugin.HybridWebView.iOS
{
    public class HybridWebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly WeakReference<HybridWebViewRenderer> _reference;

        public HybridWebViewNavigationDelegate(HybridWebViewRenderer renderer)
        {
            _reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        public bool AttemptOpenCustomUrlScheme(NSUrl url)
        {
            var app = UIApplication.SharedApplication;

            if (app.CanOpenUrl(url))
                return app.OpenUrl(url);

            return false;
        }

        [Export("webView:decidePolicyForNavigationAction:decisionHandler:")]
        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;

            // If navigation target frame is null, this can mean that the link contains target="_blank". Start loadrequest to perform the navigation
            if (navigationAction.TargetFrame == null)
            {
                webView.LoadRequest(navigationAction.Request);
                return;
            }
            // If the navigation event originates from another frame than main (iframe?) it's not a navigation event we care about
            if (!navigationAction.TargetFrame.MainFrame)
            {
                decisionHandler(WKNavigationActionPolicy.Allow);
                return;
            }


            var response = renderer.Element.HandleNavigationStartRequest(navigationAction.Request.Url.ToString());

            if (response.Cancel || response.OffloadOntoDevice)
            {
                if (response.OffloadOntoDevice)
                    AttemptOpenCustomUrlScheme(navigationAction.Request.Url);

                decisionHandler(WKNavigationActionPolicy.Cancel);
            }

            else
            {
                decisionHandler(WKNavigationActionPolicy.Allow);
                renderer.Element.Navigating = true;
            }
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;

            if (navigationResponse.Response is NSHttpUrlResponse)
            {
                var code = ((NSHttpUrlResponse)navigationResponse.Response).StatusCode;
                if (code >= 400)
                {
                    renderer.Element.Navigating = false;
                    renderer.Element.HandleNavigationError((int)code);
                    decisionHandler(WKNavigationResponsePolicy.Cancel);
                    return;
                }
            }

            decisionHandler(WKNavigationResponsePolicy.Allow);
        }

        [Export("webView:didFinishNavigation:")]
        public async override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;

            renderer.Element.HandleNavigationCompleted(webView.Url.ToString());
            await renderer.OnJavascriptInjectionRequest(HybridWebViewControl.InjectedFunction);

            if (renderer.Element.EnableGlobalCallbacks)
                foreach (var function in HybridWebViewControl.GlobalRegisteredCallbacks)
                    await renderer.OnJavascriptInjectionRequest(HybridWebViewControl.GenerateFunctionScript(function.Key));

            foreach (var function in renderer.Element.LocalRegisteredCallbacks)
                await renderer.OnJavascriptInjectionRequest(HybridWebViewControl.GenerateFunctionScript(function.Key));

            renderer.Element.CanGoBack = webView.CanGoBack;
            renderer.Element.CanGoForward = webView.CanGoForward;
            renderer.Element.Navigating = false;
            renderer.Element.HandleContentLoaded();
        }

        [Foundation.Export("webView:didStartProvisionalNavigation:")]
        [ObjCRuntime.BindingImpl(ObjCRuntime.BindingImplOptions.GeneratedCode | ObjCRuntime.BindingImplOptions.Optimizable)]
        public virtual void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;
            Device.BeginInvokeOnMainThread(() =>
            {
                renderer.Element.CurrentUrl = webView.Url.ToString();
            });
        }

    }
}
