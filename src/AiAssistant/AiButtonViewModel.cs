using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Util;

namespace WinDbgExt.AiAssistant
{
    [RibbonTabGroupExtensionMetadata("ViewRibbonTab", "Windows", 5), Export(typeof(IDbgRibbonTabGroupExtension))]
    public class AiButtonViewModel : IDbgRibbonTabGroupExtension
    {
        [Import]
        private IDbgToolWindowManager _toolWindowManager;

        public AiButtonViewModel()
        {
            ShowCommand = new DelegateCommand(Show);
        }

        public DelegateCommand ShowCommand { get; }

        public IEnumerable<FrameworkElement> Controls => new FrameworkElement[] { new AiButton { DataContext = this } };

        private void Show()
        {
            _toolWindowManager.OpenToolWindow("AiAssistant");
        }
    }
}