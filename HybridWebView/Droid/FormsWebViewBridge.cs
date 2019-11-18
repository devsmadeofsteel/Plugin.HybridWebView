using Android.Webkit;
using Java.Interop;
using System;

namespace Plugin.HybridWebView.Droid
{
    public class FormsWebViewBridge : Java.Lang.Object
    {
        private readonly WeakReference<HybridWebViewRenderer> _reference;

        public FormsWebViewBridge(HybridWebViewRenderer renderer)
        {
            _reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        [JavascriptInterface]
        [Export("invokeAction")]
        public void InvokeAction(string data)
        {
            if (_reference == null || !_reference.TryGetTarget(out var renderer)) return;
            if (renderer.Element == null) return;

            renderer.Element.HandleScriptReceived(data);
        }
    }
}
