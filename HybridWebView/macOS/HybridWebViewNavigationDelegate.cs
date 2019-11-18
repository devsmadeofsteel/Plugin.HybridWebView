using System;
using Foundation;
using Plugin.HybridWebView.Shared;
using WebKit;
using Xamarin.Forms;

namespace Plugin.HybridWebView.macOS
{
    public class HybridWebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly WeakReference<HybridWebViewRenderer> Reference;

        public HybridWebViewNavigationDelegate(HybridWebViewRenderer renderer)
        {
            Reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        [Export("webView:decidePolicyForNavigationAction:decisionHandler:")]
        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            if (Reference == null || !Reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;

            var response = renderer.Element.HandleNavigationStartRequest(navigationAction.Request.Url.ToString());

            if (response.Cancel)
            {
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
            if (Reference == null || !Reference.TryGetTarget(out var renderer)) return;
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
            if (Reference == null || !Reference.TryGetTarget(out var renderer)) return;
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
            if (Reference == null || !Reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;
            Device.BeginInvokeOnMainThread(() =>
            {
                renderer.Element.CurrentUrl = webView.Url.ToString();
            });
        }
    }
}
