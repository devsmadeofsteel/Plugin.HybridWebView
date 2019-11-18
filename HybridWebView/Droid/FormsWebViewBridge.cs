using Android.Webkit;
using Java.Interop;
using System;

namespace Plugin.HybridWebView.Droid
{
    public class FormsWebViewBridge : Java.Lang.Object
    {

        readonly WeakReference<HybridWebViewRenderer> Reference;

        public FormsWebViewBridge(HybridWebViewRenderer renderer)
        {
            Reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        [JavascriptInterface]
        [Export("invokeAction")]
        public void InvokeAction(string data)
        {
            if (Reference == null || !Reference.TryGetTarget(out HybridWebViewRenderer renderer)) return;
            if (renderer.Element == null) return;

            renderer.Element.HandleScriptReceived(data);
        }
    }
}
