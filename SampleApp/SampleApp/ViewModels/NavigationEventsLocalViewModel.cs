using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace SampleApp.ViewModels
{
    public class NavigationEventsLocalViewModel : BindableBase
    {

        ICommand _errorCommand;
        public ICommand ErrorCommand => (_errorCommand ?? (_errorCommand = new DelegateCommand(() => Uri = "Sample3.html")));

        ICommand _reloadCommand;
        public ICommand ReloadCommand => (_reloadCommand ?? (_reloadCommand = new DelegateCommand(() =>
        {
            if (Uri.Equals("Sample.html"))
                Uri = "Sample2.html";
            else
                Uri = "Sample.html";
        })));

        string _uri = "Sample.html";

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
