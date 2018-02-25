using DbgX.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using DbgX.Interfaces.Services;
using DbgX.Util;

namespace RunCSharp
{
    [RibbonTabGroupExtensionMetadata("ViewRibbonTab", "Windows", 5), Export(typeof(IDbgRibbonTabGroupExtension))]
    public class EditorViewModel : IDbgRibbonTabGroupExtension
    {
        private const string RunnerAssembly = "WindbgScriptRunner.dll";

        private bool _isRunnerLoaded;

        [Import]
        private IDbgToolWindowManager _toolWindowManager;

        [Import]
        private IDbgConsole _console;

        public EditorViewModel()
        {
            ShowCommand = new DelegateCommand(Show);
        }

        public DelegateCommand ShowCommand { get; }

        public IEnumerable<FrameworkElement> Controls => new[] { new OpenEditorButton(this) };

        private async void Show()
        {
            if (!_isRunnerLoaded)
            {
                await PrepareRunner();
            }

            _toolWindowManager.OpenToolWindow("CSharpScriptWindow");
        }

        private async Task PrepareRunner()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var baseFolder = Path.GetDirectoryName(assembly.Location);

            var destinationFolder = Path.Combine(baseFolder, "CsharpScriptRunner");

            if (Directory.Exists(destinationFolder))
            {
                var runnerPath = Path.Combine(destinationFolder, RunnerAssembly);

                if (File.Exists(runnerPath))
                {
                    string version;

                    using (var stream = new StreamReader(assembly.GetManifestResourceStream("RunCSharp.version.txt")))
                    {
                        version = stream.ReadLine();
                    }

                    var installedVersion = AssemblyName.GetAssemblyName(runnerPath).Version.ToString();

                    if (installedVersion == version)
                    {
                        // This version is already installed, nothing to do
                        return;
                    }
                }
                
                Directory.Delete(destinationFolder, true);
            }

            Directory.CreateDirectory(destinationFolder);

            var tempFile = Path.GetTempFileName();

            using (var stream = assembly.GetManifestResourceStream("RunCSharp.runner.zip"))
            {
                using (var destinationStream = File.OpenWrite(tempFile))
                {
                    stream.CopyTo(destinationStream);
                }
            }

            ZipFile.ExtractToDirectory(tempFile, destinationFolder);

            File.Delete(tempFile);

            await _console.ExecuteCommandAsync($".load {Path.Combine(destinationFolder, "WindbgScriptRunner.dll")}");

            _isRunnerLoaded = true;
        }
    }
}
