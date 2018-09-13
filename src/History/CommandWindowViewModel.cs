using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;

namespace WinDbgExt.History
{
    [NamedPartMetadata("CommandWindow"), Export(typeof(IDbgToolWindow))]
    public class CommandWindowViewModel : IDbgToolWindow
    {
        [Import]
        private IDbgConsole _console;

        [Import]
        private IHistoryManager _historyManager;

        public FrameworkElement GetToolWindowView(object parameter)
        {
            return new CommandWindow(_console, _historyManager, (string)parameter);
        }
    }
}
