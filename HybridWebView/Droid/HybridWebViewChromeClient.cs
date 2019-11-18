using System;
using Android.Webkit;

namespace Plugin.HybridWebView.Droid
{
    public class HybridWebViewChromeClient : WebChromeClient
    {
        private readonly WeakReference<HybridWebViewRenderer> _reference;

        public HybridWebViewChromeClient(HybridWebViewRenderer renderer)
        {
            _reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }
    }
}
