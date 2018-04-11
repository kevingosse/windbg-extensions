using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;
using DbgX.Util;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace RunCSharp
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

        public FrameworkElement GetToolWindowView(object _) => new EditorControl { DataContext = this };

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

            var output = await _console.ExecuteCommandAndCaptureOutputAsync($"!compileandrun {file}");

            _console.PrintTextToConsole(output);

            File.Delete(file);
        }
    }
}