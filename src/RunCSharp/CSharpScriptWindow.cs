using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;
using DbgX.Interfaces.UI;
using DbgX.Util;
using ICSharpCode.AvalonEdit.Document;

namespace WinDbgExt.RunCSharp
{
    [NamedPartMetadata("CSharpScriptWindow"), Export(typeof(IDbgToolWindow))]
    public class CSharpScriptWindow : IDbgToolWindow
    {
        [Import]
        private IDbgConsole _console;

        public CSharpScriptWindow()
        {
            EditorDocument = new TextDocument();
            RunCommand = new DelegateCommand(Run);
        }

        public DelegateCommand RunCommand { get; }

        public TextDocument EditorDocument { get; set; }

        public FrameworkElement GetToolWindowView(object _)
        {
            var control = new EditorControl { DataContext = this };

            ToolWindowView.SetTabTitle(control, new ToolWindowTitle("Run C# script"));

            return control;
        }

        private async void Run()
        {
            var code = $@"
using System;
using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;

namespace Test
{{
    public static class Program
    {{
        public static void Run(ClrHeap heap)
        {{
            {EditorDocument.Text}
        }}
    }}
}}";

            var file = Path.GetTempFileName();

            File.WriteAllText(file, code);

            _console.PrintTextToConsole("Executing script..." + Environment.NewLine);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var output = await _console.ExecuteCommandAndCaptureOutputAsync($"!compileandrun {file}");

                _console.PrintTextToConsole(output);

                _console.PrintTextToConsole($"Execution done in {stopwatch.Elapsed.TotalSeconds} seconds" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _console.PrintTextToConsole("Execution failed with error: " + ex + Environment.NewLine);
            }

            File.Delete(file);
        }
    }
}