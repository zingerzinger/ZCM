using System;
using System.Collections.Generic;
using System.IO;

namespace ZCM
{
    class Program
    {
        const int MEM_SIZE = 1024;
        
        static void Main() {
            Console.WriteLine("===============");
            List<TOKEN> tokens = Lexer.Process(File.ReadAllText("prog.txt"));
            Console.WriteLine("===============");
            
            for (int i = 0; i < tokens.Count; i++) {
                TOKEN t = tokens[i];
                Console.WriteLine("{0} : {1} : {2} | {3} : {4}",
                    i.ToString().PadLeft(3, ' ') ,
                    (t.Type).ToString().PadLeft(10, ' '),
                    t.Value.PadRight(20, ' '),
                    t.line.ToString().PadLeft(4, ' '),
                    t.col .ToString().PadLeft(4, ' '));
            } Console.WriteLine("===============");

            Node program = Parser.Process(tokens);
            Console.WriteLine("===============");

            string asm = Emitter.Process(program, MEM_SIZE);
            File.WriteAllText("casm.txt", asm);
            Console.WriteLine("===============");
            Console.WriteLine(asm);
            Console.WriteLine("===============");
            
            int[] binary = Translator.Translate(asm /*File.ReadAllText("asm.txt")*/);
            Console.WriteLine("{0}% MEM", (int)(binary.Length / (float)MEM_SIZE * 100.0f));
            Console.WriteLine("===============");

            string sbin = "";
            for (int i = 0; i < binary.Length; i++) { sbin += string.Format("MEM[{0}] = {1}; ", i, binary[i]); }
            File.WriteAllText("sbin.txt", sbin);

            Core.Run(binary, MEM_SIZE);
            Console.ReadKey();
        }
    }
}
