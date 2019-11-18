using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace SampleApp.ViewModels
{
    public class NavigationEventsViewModel : BindableBase
    {
        ICommand _errorCommand;
        public ICommand ErrorCommand => (_errorCommand ?? (_errorCommand = new DelegateCommand(() => Uri = "http://www.google.co.yk")));

        private ICommand _reloadCommand;
        public ICommand ReloadCommand => (_reloadCommand ?? (_reloadCommand = new DelegateCommand(() =>
        {
            if (Uri.Equals("https://www.google.co.uk"))
                Uri = "https://www.xamarin.com";
            else
                Uri = "https://www.google.co.uk";
        })));

        string _uri = "https://www.google.co.uk";

        public string Uri
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }

        bool _isCancelled;

        public bool IsCancelled
        {
            get => _isCancelled;
            set => SetProperty(ref _isCancelled, value);
        }
    }
}
