using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SagaImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var types = LoadVerbs();

            Parser.Default.ParseArguments(args, types)
                  .WithParsed(Run)
                  .WithNotParsed(HandleErrors);
        }

        private static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        private static void HandleErrors(IEnumerable<Error> errs)
        {
            if (errs.IsVersion())
            {
                Console.WriteLine("Version Request");
                return;
            }

            if (errs.IsHelp())
            {
                Console.WriteLine("Help Request");
                return;
            }
            Console.WriteLine("Parser Fail");
        }

        private static void Run(object obj)
        {
            try
            {
                switch (obj)
                {
                    case InputOptions i:
                        var inputProcess = new InputProcessor();
                        inputProcess.Initialize(i);
                        inputProcess.Execute();
                        break;
                    case OutputOptions o:
                        var outputProcess = new OutputProcessor();
                        outputProcess.Initialize(o);
                        outputProcess.Execute();
                        break;
                    case LookupOptions l:
                        //var lookupProcess = new LookupProcessor_GoodReads();
                        var lookupProcess = new LookupProcessor_GoogleBooks();
                        lookupProcess.Initialize(l);
                        lookupProcess.Execute();
                        break;
                    case DumpOptions d:
                        var dumpProcess = new DumpProcessor();
                        dumpProcess.Initialize(d);
                        dumpProcess.Execute();
                        break;
                    case CleanOptions c:
                        var cleanProcess = new CleanProcessor();
                        cleanProcess.Initialize(c);
                        cleanProcess.Execute();
                        break;
                }
            }
            catch (Exception e)
            {
                var st = new StackTrace(e, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                Console.WriteLine($"\nSomething went wrong -> {e.Message}");
                Console.WriteLine($"Filename -> {frame.GetFileName()}");
                Console.WriteLine($"Line Number -> {frame.GetFileLineNumber()}");
                Console.WriteLine($"Stack Trace \n {st.ToString()}");

            }
        }
    }
}