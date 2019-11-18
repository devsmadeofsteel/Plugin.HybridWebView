using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace SampleApp.ViewModels
{
    public class NavigatingEventViewModel : BindableBase
    {

        string _uri = "https://www.google.co.uk";

        public string Uri
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }

        ICommand _reloadCommand;
        public ICommand ReloadCommand => (_reloadCommand ?? (_reloadCommand = new DelegateCommand(() =>
        {
            if (Uri.Equals("https://www.google.co.uk"))
                Uri = "https://www.xamarin.com";
            else
                Uri = "https://www.google.co.uk";
        })));

    }
}
