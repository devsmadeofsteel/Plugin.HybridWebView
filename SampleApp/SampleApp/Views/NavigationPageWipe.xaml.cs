using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NavigationPageWipe : ContentPage
    {
        public NavigationPageWipe()
        {
            InitializeComponent();
        }

        void Button_Clicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new InternetSample();
        }
    }
}