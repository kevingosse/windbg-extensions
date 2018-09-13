using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Util;

namespace WinDbgExt.History
{
    [RibbonTabGroupExtensionMetadata("ViewRibbonTab", "Windows", 5), Export(typeof(IDbgRibbonTabGroupExtension))]
    public class HistoryButtonViewModel : IDbgRibbonTabGroupExtension
    {
        [Import]
        private IDbgToolWindowManager _toolWindowManager;

        public HistoryButtonViewModel()
        {
            ShowCommand = new DelegateCommand(Show);
        }

        public DelegateCommand ShowCommand { get; }

        public IEnumerable<FrameworkElement> Controls => new[] { new HistoryButton(this) };

        private void Show()
        {
            _toolWindowManager.OpenToolWindow("HistoryWindow");
        }
    }
}