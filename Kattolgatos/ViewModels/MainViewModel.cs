using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattolgatos.ViewModels
{
    internal class MainViewModel : ObservableObject
    {

        private ObservableObject _selectedViewModel;
        public IRelayCommand<object> UpdateViewCommand { get; set; }
        public ObservableObject SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                if (SelectedViewModel != value)
                {
                    SetProperty(ref _selectedViewModel, value);
                    OnPropertyChanged("SelectedViewModel");
                }
            }
        }

        public MainViewModel() 
        {
            UpdateViewCommand = new RelayCommand<object>(e => Execute(e));
            SelectedViewModel = App.Current.Services.GetRequiredService<PecaViewmodel>();
        }

        private void Execute(object p)
        {
            switch (p.ToString())
            {
                case "Peca":
                    SelectedViewModel = App.Current.Services.GetRequiredService<PecaViewmodel>();
                    break;
                case "Energia":
                    SelectedViewModel = App.Current.Services.GetRequiredService<EnergiaFarmViewModel>();
                    break;
            }

        }
    }
}
