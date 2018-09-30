using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;

namespace WindbgScriptRunner
{
    public class ScriptRunner
    {
        public static void Run(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            if (!Runner.InitApi(client))
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

                    method.Invoke(null, new object[] { Runner.Runtime.Heap });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured: " + ex);
            }
        }
    }
}