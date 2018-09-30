using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using RGiesecke.DllExport;

namespace WindbgScriptRunner
{
    public class Runner
    {
        public static AppDomain ChildDomain;

        [DllExport("compileandrun")]
        public static void CompileAndRun(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            if (ChildDomain == null)
            {
                ChildDomain = AppDomain.CreateDomain("CSharpRunner", AppDomain.CurrentDomain.Evidence, GetAssemblyPath(), ".", false);
            }

            var invoker = new ScriptInvoker(client, args);

            ChildDomain.DoCallBack(invoker.Invoke);
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

        private static uint DEBUG_EXTENSION_VERSION(uint Major, uint Minor) => ((Major & 0xffff) << 16) | (Minor & 0xffff);

        private static string GetAssemblyPath()
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
