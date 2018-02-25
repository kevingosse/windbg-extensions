using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace RunCSharp
{
    [NamedPartMetadata("CSharpScriptWindow"), Export(typeof(IDbgToolWindow))]
    public class CSharpScriptWindow : IDbgToolWindow
    {
        [Import]
        private IDbgConsole _console;

        public FrameworkElement GetToolWindowView(object parameter)
        {
            var contentControl = new ContentControl();

            var textViewHost = new TextEditor
            {
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#")
            };

            var panel = new Grid();

            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition());

            var runButton = new Button { Content = "Run", Tag = textViewHost };

            runButton.Click += RunButton_Click;

            Grid.SetRow(runButton, 0);
            Grid.SetRow(textViewHost, 1);

            panel.Children.Add(runButton);
            panel.Children.Add(textViewHost);

            contentControl.Content = panel;
            
            return contentControl;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var textViewHost = (TextEditor)((Button)sender).Tag;

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
            {textViewHost.Text}
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