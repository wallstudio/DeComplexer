using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeComplexer
{
    class Program
    {
        static readonly object CONSOL_LOCK = new object();  

        static void Main(string[] args)
        {
            args = new []
            {
                "sample_data/64.asm.txt",
                "sample_data/script.py",
                "sample_data/64.hint.asm.txt",
            };

            var asm = args[0];
            var meta = args[1];
            var hint = args[2];

            var metaLines = File.ReadAllLines(meta);
            var pattern = new Regex(@"^SetName\(0x(.+), \'(.+)\'\)$");
            var methods = metaLines
                .SkipWhile(l => l != "print('Making method name...')").Skip(1)
                .TakeWhile(l => l != "print('Make method name done')").SkipLast(1)
                .Select(l => pattern.Match(l).Groups)
                .Select(g => new {addr = g[1].Value.ToLower(), name = g[2].Value})
                .ToList();

            Console.WriteLine($"{methods.Count} methods");

            var asmLines = File.ReadLines(asm).ToArray();
            Console.WriteLine($"Load asm {asmLines.Length}");

            var progress = 0;
            Parallel.For(0, asmLines.Length, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 64,
            }, (i, s) =>
            {
                var line = asmLines[i];

                // var match = labelPattern.Match(line);
                // if(match.Success)
                // {
                //     line = line.Replace(match.Groups[1].Value, "");
                // }

                var hints = methods.Where(m => line.Contains(m.addr))
                    .Select(m => m.name)
                    .ToArray();
                if(hints.Length > 0)
                {
                    var hintLine = " @HINTS: " + string.Join(", ", hints);
                    line = hintLine + "\r\n" + line;
                }
                asmLines[i] = line;

                if(i % 100 == 0)
                {
                    lock(CONSOL_LOCK)
                    {
                        progress += 100;
                        Console.CursorLeft = 0;
                        Console.Write($"Proc {progress}/{asmLines.Length}");
                    }
                }
            });

            File.WriteAllLines(hint, asmLines);
        }
    }
}
