using System;
using System.Threading;
using System.Diagnostics;

namespace ZCM
{
    static class Core
    {
        static int[] MEM = null;
        static Stopwatch TIMER = new Stopwatch();
		static int IP = 0, R = 0, B = 0;
        static int SP = 0;

        public static void Run(int[] binary, int memSize) {
            MEM = new int[memSize];
            Buffer.BlockCopy(binary, 0, MEM, 0, binary.Length * 4);

            int opcode = 0;
            TIMER.Restart();

			while (true) {
                opcode = MEM[IP];
                if (opcode != 0) { instructions[opcode]();         }
                else             { instructions[opcode](); return; }
            }
        }
        
        delegate void INSTRUCTION();

        // *** *** ***

        static INSTRUCTION[] instructions = new INSTRUCTION[] {
            /*  0: HLT        */() => { Console.WriteLine("DONE"); },
			/*  1: NOP        */() => { IP++; },
			/*  2: PUTC       */() => { 
				if (R == 0) { Console.WriteLine();    }
				else        { Console.Write((char)R); }
				IP++;
			},
			/*  3 : PRINT     */() => { Console.WriteLine("{0}", R); IP++; },
			/*  4 : READC     */() => { R = Console.ReadKey().KeyChar; IP++; },
			/*  5 : READI     */() => { R = int.Parse(Console.ReadLine()); IP++; },
			/*  6 : SLEEP     */() => { Thread.Sleep(R); IP++; },
			/*  7 : TIM       */() => { R = (int)TIMER.ElapsedMilliseconds; IP++; },
				  		      
			/*  8 : MUL       */() => { R = MEM[SP+2] * MEM[SP+1]; SP += 2; MEM[SP] = R; SP--; IP++; },
			/*  9 : DIV       */() => { R = MEM[SP+2] / MEM[SP+1]; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 10 : REM       */() => { R = MEM[SP+2] % MEM[SP+1]; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 11 : ADD       */() => { R = MEM[SP+2] + MEM[SP+1]; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 12 : SUB       */() => { R = MEM[SP+2] - MEM[SP+1]; SP += 2; MEM[SP] = R; SP--; IP++; },
															
			/* 13 : LESS      */() => { R = ( MEM[SP+2]       <   MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 14 : LESSEQ    */() => { R = ( MEM[SP+2]       <=  MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 15 : GREAT     */() => { R = ( MEM[SP+2]       >   MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 16 : GREATEQ   */() => { R = ( MEM[SP+2]       >=  MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 17 : EQU       */() => { R = ( MEM[SP+2]       ==  MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 18 : NEQ       */() => { R = ( MEM[SP+2]       !=  MEM[SP+1])       ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 19 : AND       */() => { R = ((MEM[SP+2] != 0) && (MEM[SP+1] != 0)) ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 20 : OR        */() => { R = ((MEM[SP+2] != 0) || (MEM[SP+1] != 0)) ? 1 : 0; SP += 2; MEM[SP] = R; SP--; IP++; },
			/* 21 : NOT       */() => { R = ( MEM[SP+1] == 0)                      ? 1 : 0; SP += 1; MEM[SP] = R; SP--; IP++; },
							  
			/* 22 : SSP       */() => { SP = R;                      IP++;    },
			/* 23 : LFS N     */() => { R = MEM[SP + MEM[IP+1] + 1]; IP += 2; },
			/* 24 : STS N     */() => { MEM[SP + MEM[IP+1] + 1] = R; IP += 2; },
			/* 25 : PUSH      */() => { MEM[SP] = R; SP--;           IP++;    },
			/* 26 : POP       */() => { SP++; R = MEM[SP];           IP++;    },
	
			/* 27 : LDR ADDR  */() => { R = MEM[MEM[IP+1]]; IP += 2; },
			/* 28 : STR ADDR  */() => { MEM[MEM[IP+1]] = R; IP += 2; },
			/* 29 : RB        */() => { R = B;              IP++;    },
			/* 30 : BR        */() => { B = R;              IP++;    },
			/* 31 : LDRA ADDR */() => { R = MEM[IP+1];      IP += 2; },							 
			/* 32 : LDBA      */() => { R = MEM[B];         IP++;    },
			/* 33 : STBA      */() => { MEM[B] = R;         IP++;    },

			/* 34 : JMP ADDR  */() => { IP = MEM[IP+1]; },
			/* 35 : JZ  ADDR  */() => { if (R == 0) { IP = MEM[IP+1]; } else { IP += 2; } },
			/* 36 : JNZ       */() => { if (R != 0) { IP = MEM[IP+1]; } else { IP += 2; } },
			
			/* 37 : CALL ADDR      */() => { MEM[SP] = IP + 2; SP--; IP = MEM[IP + 1]; },
			/* 38 : CLS NUM_PARAMS */() => { SP += MEM[IP + 1]; IP += 2; },
			/* 39 : RET            */() => { SP++; IP = MEM[SP]; },

        };
    }
}
