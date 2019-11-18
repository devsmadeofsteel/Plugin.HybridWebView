using System;
using Android.Webkit;

namespace Plugin.HybridWebView.Droid
{
    public class JavascriptValueCallback : Java.Lang.Object, IValueCallback
    {
        public Java.Lang.Object Value { get; private set; }

        private readonly WeakReference<HybridWebViewRenderer> _reference;

        public JavascriptValueCallback(HybridWebViewRenderer renderer)
        {
            _reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

        public void OnReceiveValue(Java.Lang.Object value)
        {
            if (_reference == null || !_reference.TryGetTarget(out _)) return;
            Value = value;
        }

        public void Reset()
        {
            Value = null;
        }
    }
}
