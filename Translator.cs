using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZCM
{
    class Translator
    {
        static List<int> MEM = new List<int>(1024);

        enum STATE
        {
            UNKNOWN      = 0,
            SKIPPING        ,
            TOKEN           ,
            STRING          ,
            COMMENT         ,
            MULTILINECOMMENT,
        }

        static STATE cstate = STATE.UNKNOWN;

        static int cidx;
        static char cc;
        static char nc;

        static List<string> tokens = new List<string>();

        public static int[] Translate(string text) {

            Stopwatch sw = new Stopwatch(); sw.Restart();

            while (true) {
                if (cidx >= text.Length) { break; }

                cc =                            text[cidx    ];
                nc = (cidx < text.Length - 1) ? text[cidx + 1] : (char)0;
                
                switch (cstate) {
                    case STATE.UNKNOWN         : StateUnknown  (); break;
                    case STATE.SKIPPING        : StateSkipping (); break;
                    case STATE.TOKEN           : StateToken    (); break;
                    case STATE.STRING          : StateString   (); break;
                    case STATE.COMMENT         : StateComment  (); break;
                    case STATE.MULTILINECOMMENT: StateMcomment (); break;
                }
            }

            // foreach (string s in tokens) { Console.WriteLine(s); }

            while (T < tokens.Count) {

                if (((tokens.Count - T) >= 2) &&
                    tokens[T    ] == "PUSH"   &&
                    tokens[T + 1] == "POP"
                    ) {
                    T += 2; continue;
                }

                bool found = false;
                foreach (COMMAND c in cmds) {
                    if (string.Equals(tokens[T], c.Name, StringComparison.OrdinalIgnoreCase)) {
                        c.Cmd();
                        found = true;
                        break;
                    }
                }
                if (!found) { Console.WriteLine("ERROR : {0}", tokens[T]); break; }
            }

            foreach (Label plc in places) { ResolvePlace(plc); }

            sw.Stop(); Console.WriteLine("TRANSLATOR : {0} ms", sw.ElapsedMilliseconds);
            Console.WriteLine("{0} words binary ({1} bytes)", MEM.Count, MEM.Count * sizeof(int));

            //Console.WriteLine("=============== LABELS ===============");
            //foreach (Label l in labels) { Console.WriteLine("{0} | {1}", l.Name, l.Address); }
            //Console.WriteLine("=============== PLACES ===============");
            //foreach (Label p in places) { Console.WriteLine("{0} | {1}", p.Name, p.Address); }

            return MEM.ToArray();
        }
        
        static string token = "";
        static bool lastPush = false;

        static void StateToken() {
            if (char.IsWhiteSpace(cc)) {
                tokens.Add(token);
                token = "";
                cstate = STATE.UNKNOWN;
                cidx++;
            } else {
                token += cc; cidx++;
            }
        }

        static void StateUnknown() {
                 if (cc == '/'  && nc == '*' ) { cstate = STATE.MULTILINECOMMENT; cidx += 2; }
            else if (cc == '/'  && nc == '/' ) { cstate = STATE.COMMENT         ; cidx += 2; }
            else if (cc == '\"'              ) { cstate = STATE.STRING          ; cidx += 1; }
            else if (char.IsWhiteSpace(cc)   ) { cstate = STATE.SKIPPING        ;            }
            else                               { cstate = STATE.TOKEN           ;            }
        }                                                                          

        static void StateSkipping() {
            if (!char.IsWhiteSpace(cc)  ) { cstate = STATE.UNKNOWN; }
            else                          { cidx++;                 }
        }

        static void StateString() {
            if (cc == '\"') { tokens.Add("\0\0\0" + token); token = ""; cstate = STATE.UNKNOWN; cidx += 1; }
            else            { token += cc;                                                      cidx += 1; }
        }                                      

        static void StateComment() {
            if (cc == '\r' || cc == '\n') { cstate = STATE.UNKNOWN; }
            else                          { cidx++;                 }
        }

        static void StateMcomment() {
            if (cc == '*' && nc == '/'  ) { cstate = STATE.UNKNOWN; cidx += 2; }
            else                          {                         cidx += 1; }
        }                                                                     

        delegate void CMD();
        static int T = 0;

        struct Label
        {
            public string Name;
            public int    Address;
            public Label(string name, int address) { Name = name; Address = address; }
        }

        static List<Label> labels = new List<Label>(); static void AddLabel(string name, int address) { labels.Add(new Label(name, address)); }
        static List<Label> places = new List<Label>(); static void AddPlace(string name, int address) { places.Add(new Label(name, address)); }
        
        static bool ResolvePlace(Label plc) {
            foreach (Label l in labels) {
                if (string.Equals(plc.Name, l.Name, StringComparison.OrdinalIgnoreCase)) { MEM[plc.Address] = l.Address; return true; }
            }
            return false;
        }

        static int caddr = 0;

        struct COMMAND
        {
            public string Name;
            public CMD Cmd;
            public COMMAND(string name, CMD cmd) { Name = name; Cmd = cmd; }
        }

        static COMMAND[] cmds = new COMMAND[] {

            new COMMAND("LBL", () => {
                AddLabel(tokens[T + 1], caddr); T += 2;
            }),

            new COMMAND("VAR", () => {
                AddLabel(tokens[T + 1], caddr);

                if (tokens[T+2].Length >= 3 && tokens[T+2].Substring(0, 3) == "\0\0\0") { // string
                    for (int i = 3; i < tokens[T + 2].Length; i++) { MEM.Add(tokens[T + 2][i]); }
                    MEM.Add(0);
                    caddr += tokens[T + 2].Length - 3 + 1;
                } else { // int
                    MEM.Add(int.Parse(tokens[T + 2]));
                    caddr++;
                }

                T += 3;
            }),

            new COMMAND("RES", () => {
                AddLabel(tokens[T + 1], caddr);
                int count = int.Parse(tokens[T + 2]);
                for (int i = 0; i < count; i++) { MEM.Add(0); }
                caddr += count;
                T += 3;
            }),

            // *** *** *** *** *** *** *** *** ***

            new COMMAND("HLT"    , () => { MEM.Add( 0); caddr++; T++; } ),
            new COMMAND("NOP"    , () => { MEM.Add( 1); caddr++; T++; } ),
            new COMMAND("PUTC"   , () => { MEM.Add( 2); caddr++; T++; } ),
            new COMMAND("PRINT"  , () => { MEM.Add( 3); caddr++; T++; } ),                  
            new COMMAND("READC"  , () => { MEM.Add( 4); caddr++; T++; } ),
            new COMMAND("READI"  , () => { MEM.Add( 5); caddr++; T++; } ),
            new COMMAND("SLEEP"  , () => { MEM.Add( 6); caddr++; T++; } ),
            new COMMAND("TIM"    , () => { MEM.Add( 7); caddr++; T++; } ),
            new COMMAND("MUL"    , () => { MEM.Add( 8); caddr++; T++; } ),
            new COMMAND("DIV"    , () => { MEM.Add( 9); caddr++; T++; } ),
            new COMMAND("REM"    , () => { MEM.Add(10); caddr++; T++; } ),
            new COMMAND("ADD"    , () => { MEM.Add(11); caddr++; T++; } ),
            new COMMAND("SUB"    , () => { MEM.Add(12); caddr++; T++; } ),
            new COMMAND("LESS"   , () => { MEM.Add(13); caddr++; T++; } ),
            new COMMAND("LESSEQ" , () => { MEM.Add(14); caddr++; T++; } ),
            new COMMAND("GREAT"  , () => { MEM.Add(15); caddr++; T++; } ),
            new COMMAND("GREATEQ", () => { MEM.Add(16); caddr++; T++; } ),
            new COMMAND("EQU"    , () => { MEM.Add(17); caddr++; T++; } ),
            new COMMAND("NEQ"    , () => { MEM.Add(18); caddr++; T++; } ),
            new COMMAND("AND"    , () => { MEM.Add(19); caddr++; T++; } ),
            new COMMAND("OR"     , () => { MEM.Add(20); caddr++; T++; } ),
            new COMMAND("NOT"    , () => { MEM.Add(21); caddr++; T++; } ),
            new COMMAND("SSP"    , () => { MEM.Add(22); caddr++; T++; } ),
            new COMMAND("LFS"    , () => { MEM.Add(23); caddr++; MEM.Add(int.Parse(tokens[T + 1])); caddr++; T += 2; } ),
            new COMMAND("STS"    , () => { MEM.Add(24); caddr++; MEM.Add(int.Parse(tokens[T + 1])); caddr++; T += 2; } ),
            new COMMAND("PUSH"   , () => { MEM.Add(25); caddr++; T++; } ),
            new COMMAND("POP"    , () => { MEM.Add(26); caddr++; T++; } ),
            new COMMAND("LDR"    , () => { MEM.Add(27); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("STR"    , () => { MEM.Add(28); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("RB"     , () => { MEM.Add(29); caddr++; T++; } ),
            new COMMAND("BR"     , () => { MEM.Add(30); caddr++; T++; } ),
            new COMMAND("LDRA"   , () => { MEM.Add(31); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("LDBA"   , () => { MEM.Add(32); caddr++; T++; } ),
            new COMMAND("STBA"   , () => { MEM.Add(33); caddr++; T++; } ),
            new COMMAND("JMP"    , () => { MEM.Add(34); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("JZ"     , () => { MEM.Add(35); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("JNZ"    , () => { MEM.Add(36); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("CALL"   , () => { MEM.Add(37); caddr++; MEM.Add(0); AddPlace(tokens[T + 1], caddr); caddr++; T += 2; } ),
            new COMMAND("CLS"    , () => { MEM.Add(38); caddr++; MEM.Add(int.Parse(tokens[T + 1])); caddr++; T += 2; } ), 
            new COMMAND("RET"    , () => { MEM.Add(39); caddr++; T++; } ),
        };
    }
}
