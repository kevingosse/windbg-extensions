using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using RGiesecke.DllExport;

namespace WindbgScriptRunner
{
    public class Runner
    {
        static Runner()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static readonly string[] References =
        {
            "Microsoft.Diagnostics.Runtime",
            "DynaMD"
        };

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            if (References.Any(r => string.Equals(assemblyName.Name, r, StringComparison.OrdinalIgnoreCase)))
            {
                string codebase = Assembly.GetExecutingAssembly().CodeBase;

                if (codebase.StartsWith("file://"))
                    codebase = codebase.Substring(8).Replace('/', '\\');

                string directory = Path.GetDirectoryName(codebase);
                string path = Path.Combine(directory, assemblyName.Name) + ".dll";
                return Assembly.LoadFile(path);
            }

            return null;
        }

        public static IDebugClient DebugClient { get; private set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        [DllExport("compileandrun")]
        public static void Script(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            if (!InitApi(client))
            {
                return;
            }

            try
            {
                var code = File.ReadAllText(args);

                var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                using (var codeProvider = new CSharpCodeProvider())
                {
                    var parameters = new CompilerParameters(new[]
                    {
                        "mscorlib.dll",
                        "System.dll",
                        "System.Core.dll",
                        "Microsoft.CSharp.dll",
                        Path.Combine(basePath, "Microsoft.Diagnostics.Runtime.dll"),
                        Path.Combine(basePath, "DynaMD.dll")
                    });
                    parameters.GenerateInMemory = true;

                    var results = codeProvider.CompileAssemblyFromSource(parameters, code);

                    if (results.Errors.Count > 0)
                    {
                        Console.WriteLine(string.Join("\r\n", results.Errors.Cast<CompilerError>().Select(l => l.ErrorText)));
                        return;
                    }

                    var type = results.CompiledAssembly.GetType("Test.Program");

                    var method = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);

                    method.Invoke(null, new object[] { Runtime.Heap });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured: " + ex);
            }
        }

        private static bool InitApi(IntPtr ptrClient)
        {
            // On our first call to the API:
            //   1. Store a copy of IDebugClient in DebugClient.
            //   2. Replace Console's output stream to be the debugger window.
            //   3. Create an instance of DataTarget using the IDebugClient.
            if (DebugClient == null)
            {
                object client = Marshal.GetUniqueObjectForIUnknown(ptrClient);
                DebugClient = (IDebugClient)client;

                var stream = new StreamWriter(new DebugEngineStream(DebugClient));
                stream.AutoFlush = true;
                Console.SetOut(stream);

                DataTarget = DataTarget.CreateFromDebuggerInterface(DebugClient);
            }

            // If our ClrRuntime instance is null, it means that this is our first call, or
            // that the dac wasn't loaded on any previous call.  Find the dac loaded in the
            // process (the user must use .cordll), then construct our runtime from it.
            if (Runtime == null)
            {
                // Just find a module named mscordacwks and assume it's the one the user
                // loaded into windbg.
                using (var process = Process.GetCurrentProcess())
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (module.FileName.ToLower().Contains("mscordacwks"))
                        {
                            // TODO:  This does not support side-by-side CLRs.
                            Runtime = DataTarget.ClrVersions.Single().CreateRuntime(module.FileName);
                            break;
                        }
                    }
                }

                // Otherwise, the user didn't run .cordll.
                if (Runtime == null)
                {
                    Console.WriteLine("Mscordacwks.dll not loaded into the debugger.");
                    Console.WriteLine("Run .cordll to load the dac before running this command.");
                }
            }
            else
            {
                // If we already had a runtime, flush it for this use.  This is ONLY required
                // for a live process or iDNA trace.  If you use the IDebug* apis to detect
                // that we are debugging a crash dump you may skip this call for better perf.
                Runtime.Flush();
                
                // Temporary workaround for a bug in ClrMD
                // To be removed when https://github.com/Microsoft/clrmd/pull/94 get published on NuGet
                var type = Type.GetType("Microsoft.Diagnostics.Runtime.Desktop.DesktopRuntimeBase, Microsoft.Diagnostics.Runtime");

                var modules = type.GetField("_modules", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Runtime)
                    as IDictionary;

                modules?.Clear();

                var moduleFiles = type.GetField("_moduleFiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Runtime)
                    as IDictionary;

                moduleFiles?.Clear();
            }

            return Runtime != null;
        }

        [DllExport("DebugExtensionInitialize")]
        public static int DebugExtensionInitialize(ref uint version, ref uint flags)
        {
            // Set the extension version to 1, which expects exports with this signature:
            //      void _stdcall function(IDebugClient *client, const char *args)
            version = DEBUG_EXTENSION_VERSION(1, 0);
            flags = 0;
            return 0;
        }

        static uint DEBUG_EXTENSION_VERSION(uint Major, uint Minor)
        {
            return ((((Major) & 0xffff) << 16) | ((Minor) & 0xffff));
        }
    }
}
