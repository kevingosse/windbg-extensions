using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using RGiesecke.DllExport;

namespace WindbgScriptRunner
{
    public class Runner
    {
        public static IDebugClient DebugClient { get; private set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        public static AppDomain _childDomain;

        [DllExport("compileandrun")]
        public static void Script(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            if (_childDomain == null)
            {
                _childDomain = AppDomain.CreateDomain("CSharpRunner", AppDomain.CurrentDomain.Evidence, GetCurrentAssemblyPath(), ".", false);
            }

            var invoker = new ScriptInvoker(client, args);

            _childDomain.DoCallBack(invoker.Invoke);
        }

        public static bool InitApi(IntPtr ptrClient)
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

        private static string GetCurrentAssemblyPath()
        {
            string basePath;
            
            try
            {
                var suffix = Environment.Is64BitProcess ? "x64" : "x86";

                basePath = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Packages\\" + Windows.ApplicationModel.Package.Current.Id.FamilyName);
                basePath = Path.Combine(basePath, $@"LocalCache\Local\DBG\UIExtensions\CsharpScriptRunner-{suffix}");
            }
            catch (Exception) // This will throw if running without the Windows Bridge	
            {
                basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            return basePath;
        }
    }
}
