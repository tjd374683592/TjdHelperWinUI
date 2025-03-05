
using System;
using System.ComponentModel;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public ICommand OnControlsSearchBoxTextChangedCommand { get; set; }

        private string _strTest;

        public string StrTest
        {
            get { return _strTest; }
            set
            {
                if (_strTest != value)
                {
                    _strTest = value;
                    OnPropertyChanged(nameof(StrTest));
                }
            }
        }

        public MainWindowViewModel()
        {
            OnControlsSearchBoxTextChangedCommand = new RelayCommand(OnControlsSearchBoxTextChangedCommandExecute);
        }

        private void OnControlsSearchBoxTextChangedCommandExecute(object obj)
        {
            throw new NotImplementedException();
        }
    }
}