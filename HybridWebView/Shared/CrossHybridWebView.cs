﻿using System;

namespace Plugin.HybridWebView.Shared
{
    /// <summary>
    /// Cross HybridWebView
    /// </summary>
    public static class CrossHybridWebView
    {
        private static Lazy<IHybridWebView> _implementation = new Lazy<IHybridWebView>(() => CreateHybridWebView(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Gets if the plugin is supported on the current platform.
        /// </summary>
        public static bool IsSupported => _implementation.Value == null ? false : true;

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static IHybridWebView Current
        {
            get
            {
                var ret = _implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        private static IHybridWebView CreateHybridWebView()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
#pragma warning disable IDE0022 // Use expression body for methods
            return new HybridWebViewControl();
#pragma warning restore IDE0022 // Use expression body for methods
#endif

            //TODO: Implement all improvements from https://github.com/KristofferBerge/Xam.Plugin.Webview/blob/unofficial-release/Xam.Plugin.WebView.Abstractions/FormsWebView.Static.cs
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    }
}
