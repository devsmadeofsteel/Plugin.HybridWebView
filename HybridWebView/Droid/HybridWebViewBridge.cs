using Android.Webkit;
using Java.Interop;
using System;

namespace Plugin.HybridWebView.Droid
{
    public class HybridWebViewBridge : Java.Lang.Object
    {
        private readonly WeakReference<HybridWebViewRenderer> _reference;

        public HybridWebViewBridge(HybridWebViewRenderer renderer)
        {
            _reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        [JavascriptInterface]
        [Export("invokeAction")]
        public void InvokeAction(string data)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;

            renderer.Element?.HandleScriptReceived(data);
        }
    }
}
