using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WindbgScriptRunner
{
    public class ScriptRunner
    {
        public static IDebugClient DebugClient { get; private set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        public static void Run(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
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
                        var fileName = module.FileName.ToLowerInvariant();

                        if (fileName.Contains("mscordacwks") || fileName.Contains("mscordaccore"))
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
            }

            return Runtime != null;
        }
    }
}