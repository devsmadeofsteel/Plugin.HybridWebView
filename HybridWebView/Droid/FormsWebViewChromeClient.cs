using System;
using Android.Webkit;

namespace Plugin.HybridWebView.Droid
{
    public class FormsWebViewChromeClient : WebChromeClient
    {

        readonly WeakReference<HybridWebViewRenderer> Reference;

        public FormsWebViewChromeClient(HybridWebViewRenderer renderer)
        {
            Reference = new WeakReference<HybridWebViewRenderer>(renderer);
        }

    }
}
