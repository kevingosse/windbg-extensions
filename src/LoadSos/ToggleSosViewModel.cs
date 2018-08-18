using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.Enums;
using DbgX.Interfaces.Listeners;
using DbgX.Interfaces.Services;
using DbgX.Util;

namespace WinDbgExt.LoadSos
{
    [RibbonTabGroupExtensionMetadata("HomeRibbonTab", "Help", 0), Export(typeof(IDbgRibbonTabGroupExtension))]
    [Export(typeof(IDbgCommandExecutionListener))]
    [Export(typeof(IDbgRefreshListener))]
    public class ToggleSosViewModel : IDbgRibbonTabGroupExtension, IDbgCommandExecutionListener, INotifyPropertyChanged, IDbgRefreshListener
    {
        private bool _engineLoaded;

        [Import]
        private IDbgConsole _console;

        [Import]
        private IDbgEngineControl _engineControl;

        [Import]
        private IDbgEngineState _engineState;

        private bool _isLoaded;

        public ToggleSosViewModel()
        {
            LoadSosCommand = new AsyncDelegateCommand(LoadSos, CanLoadSos);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsLoaded
        {
            get => _isLoaded;

            set
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public AsyncDelegateCommand LoadSosCommand { get; }

        public IEnumerable<FrameworkElement> Controls => new[] { new LoadSosButton(this) };

        private bool CanLoadSos()
        {
            return _engineState.ConnectionState != EngineConnectionState.NoSession && !IsLoaded;
        }

        public void OnCommandExecuted(string command)
        {
            if (command.StartsWith(".loadby sos"))
            {
                IsLoaded = true;
            }
        }

        public void OnRefreshRequested()
        {
            if (!_engineLoaded)
            {
                _engineState.PropertyChanged += EngineStatePropertyChanged;
                _engineLoaded = true;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task LoadSos()
        {
            _console.PrintTextToConsole(await _console.ExecuteLocalCommandAndCaptureOutputAsync(".loadby sos clr"));
            _console.PrintTextToConsole(await _console.ExecuteLocalCommandAndCaptureOutputAsync(".cordll -l -e"));
        }

        private void EngineStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ConnectionState")
            {
                if (_engineState.ConnectionState == EngineConnectionState.NoSession)
                {
                    IsLoaded = false;
                }

                LoadSosCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
