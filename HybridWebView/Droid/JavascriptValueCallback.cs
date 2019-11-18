using System;
using Android.Webkit;

namespace Plugin.HybridWebView.Droid
{
    public class JavascriptValueCallback : Java.Lang.Object, IValueCallback
    {

        public Java.Lang.Object Value { get; private set; }

        readonly WeakReference<HybridWebViewRenderer> Reference;

        public JavascriptValueCallback(HybridWebViewRenderer renderer)
        {
            Reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        public void OnReceiveValue(Java.Lang.Object value)
        {
            if (Reference == null || !Reference.TryGetTarget(out HybridWebViewRenderer renderer)) return;
            Value = value;
        }

        public void Reset()
        {
            Value = null;
        }
    }
}
