using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZCM
{
    // ******************************************************
    // ******************************************************
    // ******************************************************
    #region NODES

    partial class Node
    {
        public static int MEMORY_SIZE = 1024;
        private static string asmVars    = "";
        private static string asmProgram = "";
        
        public static string GetProgramText() { return "JMP FUNC_main\n" + asmVars + "//---------\n" + asmProgram + "//-EOF-\n"; }

        protected static void em  (string format, params object[] args) { asmProgram += string.Format(format, args) + "\n"; }
        protected static void emGV(VarDecl vd) { gvars.Add(vd); } // const | string | array

        // *** *** ***

                  static List<VarDecl> gvars  = new List<VarDecl>();
        protected static List<VarDecl> glVars = new List<VarDecl>();
        protected static int lblNum = 0;
        protected static FuncDecl cfunc = null;
        
        protected static int nextLbl() { int result = lblNum; lblNum++; return result; }

        // *** *** ***

        private bool varFound(VarDecl vd) { 
            foreach (VarDecl v in glVars) { if (vd.Name == v.Name) { return true; }  }
            return false;
        }

        public virtual void Emit() {

            VarDecl ss = new VarDecl();
            ss.Name  = "SYS_STACK_START";
            ss.Type  = "const";
            ss.Value = (MEMORY_SIZE-1).ToString();
            emGV(ss);

            foreach (Node n in chld) { n.Emit(); }

            // filter unique variables/constants
            foreach (VarDecl vd in gvars) { if (!varFound(vd)) { glVars.Add(vd); } }
            
            foreach (VarDecl vd in glVars) {
                     if (vd.Type == "const" ) { asmVars += string.Format("VAR {0} {1}\n"    , vd.Name, vd.Value); }
                else if (vd.Type == "string") { asmVars += string.Format("VAR {0} \"{1}\"\n", vd.Name, vd.Value); }
                else if (vd.Type == "array" ) { asmVars += string.Format("RES {0} {1}\n"    , vd.Name, vd.Value); }
            }
        }
    }

    partial class VarDecl : Node
    {
        public override void Emit() { emGV(this); }
    }

    partial class FuncDecl : Node
    {
        public  int STO = 0; // operational stack offset
        
        public int getLocalVarOffset(string name) {

            bool found = false;
            int result = 0;
            for (int i = 0; i < numLocalVars; i++) {
                if (name == varList[i].Name) {
                    result = (numLocalVars - i - 1) + STO;
                    found = true;
                    break;
                }
            }

            if (!found) { return -1; }
            return result;
        }

        public int getLocalParamOffset(string name) {

            bool found = false;
            int result = 0;
            for (int i = 0; i < numArgs; i++) {
                if (name == argList[i]) {
                    result = (numArgs - i - 1) + numLocalVars + STO + 1 /* + return address */;
                    found = true;
                    break;
                }
            }

            if (!found) { return -1; }
            return result;
        }

        public override void Emit() {

            cfunc = this;

             if (Name == "main") { STO--; /* dirty hack! */ }

            em("");
            em("//--- {0} : {1} ---", Name, numArgs);
            em("LBL FUNC_{0}", Name);

            if (Name == "main") {
                em("LDR SYS_STACK_START");
                em("SSP");
            }

            // local vars
            foreach (VarDecl vd in varList) {
                       if (vd.Type == "const" ) {

                    string constName = "__CONST_" + vd.Value;
                    VarDecl cst = new VarDecl();
                    cst.Name = constName;
                    cst.Type = "const";
                    cst.Value = vd.Value;
                    emGV(cst);
                    
                    em("LDR {0}", cst.Name);
                    em("PUSH");
                } else if (vd.Type == "string") {
                    // TODO
                } else if (vd.Type == "array" ) {
                    // TODO
                }
            }

            foreach (Node n in chld) { n.Emit(); } // function body

            if (numLocalVars > 0) { em("CLS {0}", numLocalVars); } // TODO : arrays?
            em(Name == "main" ? "HLT" : "RET");
        }
    }

    partial class Expression : Node
    {
        public override void Emit() {
            foreach (Node n in chld) {
                n.Emit();
                if (n.GetType() == typeof(FuncCall)) { em("PUSH"); }
                if (n.GetType() != typeof(Operator)) { cfunc.STO++; }
            }
        }
    }

    partial class Operator : Node
    {
        public override void Emit() {

            switch (Value) {
                case "*" : em("MUL"    ); cfunc.STO--; break;
                case "/" : em("DIV"    ); cfunc.STO--; break;
                case "%" : em("REM"    ); cfunc.STO--; break;
                case "+" : em("ADD"    ); cfunc.STO--; break;
                case "-" : em("SUB"    ); cfunc.STO--; break;
                case "<" : em("LESS"   ); cfunc.STO--; break;
                case "<=": em("LESSEQ" ); cfunc.STO--; break;
                case ">" : em("GREAT"  ); cfunc.STO--; break;
                case ">=": em("GREATEQ"); cfunc.STO--; break;
                case "==": em("EQU"    ); cfunc.STO--; break;
                case "!=": em("NEQ"    ); cfunc.STO--; break;
                case "&&": em("AND"    ); cfunc.STO--; break;
                case "||": em("OR"     ); cfunc.STO--; break;
                case "!" : em("NOT"    ); /* no STO change */ break;
                default  : em("HLT"    ); /* no STO change */ break;
            }                                         
        }
    }

    partial class Var : Node
    {
        public override void Emit() {

            int paramIdx = cfunc.getLocalParamOffset(Name);

            if (paramIdx >= 0) { // local param
                em("LFS {0}", paramIdx);
                em("PUSH");
                return;
            }

            int localVarIdx = cfunc.getLocalVarOffset(Name);

            if (localVarIdx >= 0) { // local var
                em("LFS {0}", localVarIdx);
                em("PUSH");
                return;
            }

            // global var
            em("LDR {0}", Name);
            em("PUSH");
        }
    }

    partial class FuncCall : Node
    {
        public override void Emit() {

            if (Name == "HLT"   ||
                Name == "NOP"   ||
                Name == "PUTC"  ||
                Name == "PRINT" ||
                Name == "READC" ||
                Name == "READI" ||
                Name == "SLEEP" ||
                Name == "TIM") {

                if (chld.Count > 0) {
                    (chld[0]).Emit();
                    em("POP"); cfunc.STO--;
                }

                em(Name); return;
            }
            
            int numParams = 0;
            foreach (Node n in chld) { n.Emit(); numParams++; }
            em("CALL FUNC_{0}", Name);
            if (numParams > 0) { em("CLS {0}", numParams); }
        }
    }

    partial class Indexer : Node
    {
        public override void Emit() {

            ex.Emit(); // calc expression

            int paramIdx = cfunc.getLocalParamOffset(var.Name);

            if (paramIdx >= 0) { // local param
                em("LFS {0}", paramIdx);
                em("PUSH");
                em("ADD"); // calc new address (newAddr = addr + expressionResult)
                em("POP"); em("BR"); em("LDBA"); em("PUSH"); cfunc.STO--;
                return;
            }

            int localVarIdx = cfunc.getLocalVarOffset(var.Name);

            if (localVarIdx >= 0) { // local var
                em("LFS {0}", localVarIdx);
                em("PUSH");
                em("ADD"); // calc new address (newAddr = addr + expressionResult)
                em("POP"); em("BR"); em("LDBA"); em("PUSH"); cfunc.STO--;
                return;
            }

            // global var
            
            em("LDRA {0}", var.Name); // get var addr
            em("PUSH");
            
            em("ADD"); // calc new address (newAddr = addr + expressionResult)

            em("POP" ); // PUSH MEM[newAddr]
            em("BR"  );
            em("LDBA");
            em("PUSH"); cfunc.STO--;
        }
    }

    partial class Const : Node
    {
        public override void Emit() {
            string constName = "__CONST_" + Value.ToString();
            VarDecl vd = new VarDecl();
            vd.Name  = constName;
            vd.Type  = "const";
            vd.Value = Value;
            emGV(vd);

            em("LDR {0}", constName);
            em("PUSH");
        }
    }

    partial class GetAddr : Node
    {
        public override void Emit() {
            em("LDRA {0}", var.Name);
            em("PUSH");
        }
    }

    partial class VarAssign : Node
    {
        public override void Emit() {
            
            rex.Emit();

            Type lexType = (lex.chld[0]).GetType();

                   if (lexType == typeof(Var    )) {
                
                int paramIdx = cfunc.getLocalParamOffset(((Var)lex.chld[0]).Name);

                if (paramIdx >= 0) { // local param
                    em("POP"); cfunc.STO--;
                    em("STS {0}", cfunc.getLocalParamOffset(((Var)lex.chld[0]).Name));
                    return;
                }

                int localVarIdx = cfunc.getLocalVarOffset(((Var)lex.chld[0]).Name);

                if (localVarIdx >= 0) { // local var
                    em("POP"); cfunc.STO--;
                    em("STS {0}", cfunc.getLocalVarOffset(((Var)lex.chld[0]).Name));
                    return;
                }

                // global var
                em("POP"); cfunc.STO--;
                em("STR {0}", ((Var)lex.chld[0]).Name);
            } else if (lexType == typeof(Indexer)) {
                Indexer ind = ((Indexer)lex.chld[0]);
                ind.ex.Emit();

                int paramIdx = cfunc.getLocalParamOffset(ind.var.Name);

                if (paramIdx >= 0) { // local param
                    em("LFS {0}", cfunc.getLocalParamOffset(ind.var.Name));
                    em("PUSH");
                    em("ADD"); // calc new address (newAddr = addr + expressionResult)
                    em("POP"); cfunc.STO--; em("BR"); em("POP"); cfunc.STO--; em("STBA");
                    return;
                }

                int localVarIdx = cfunc.getLocalVarOffset(ind.var.Name);

                if (localVarIdx >= 0) { // local var
                    em("LFS {0}", cfunc.getLocalVarOffset(ind.var.Name));
                    em("PUSH");
                    em("ADD"); // calc new address (newAddr = addr + expressionResult)
                    em("POP"); cfunc.STO--; em("BR"); em("POP"); cfunc.STO--; em("STBA");
                    return;
                }

                // global var
                em("LDRA {0}", ind.var.Name); // get var addr
                em("PUSH");
                
                em("ADD"); // calc new address (newAddr = addr + expressionResult)
                
                em("POP" ); cfunc.STO--; // R = new address
                em("BR"  ); // B = new address
                em("POP" ); cfunc.STO--; // R = rex result
                em("STBA"); // MEM[B] = R
            }

        }
    }

    partial class FuncRet : Node
    {
        public override void Emit() {
            if (ex.chld.Count > 0) {
                ex.Emit();
                em("POP"); cfunc.STO--;
            }

            if (cfunc.numLocalVars > 0) { em("CLS {0}", cfunc.numLocalVars); }
            em("RET");
        }
    }

    partial class While : Node
    {
        public override void Emit() {
            
            int loopLbl        = nextLbl();
            int finishBlockLbl = nextLbl();
            
            em("LBL LBL_{0}", loopLbl);
            condition.Emit(); em("POP"); cfunc.STO--;
            em("JZ LBL_{0}", finishBlockLbl);
            foreach (Node n in chld) { n.Emit(); }
            em("JMP LBL_{0}", loopLbl);
            em("LBL LBL_{0}", finishBlockLbl);
        }
    }

    partial class IF : Node
    {
        public override void Emit() {

            condition.Emit(); em("POP"); cfunc.STO--;

            if (branchFalse.Count == 0) {
                int clbl = nextLbl();
                em("JZ LBL_{0}", clbl);
                foreach (Node n in branchTrue) { n.Emit(); }
                em("LBL LBL_{0}", clbl);
            } else {
                int lblFalse       = nextLbl();
                int lblFinishBlock = nextLbl();
                
                em("JZ LBL_{0}", lblFalse);
                foreach (Node n in branchTrue) { n.Emit(); }
                em("JMP LBL_{0}", lblFinishBlock);
                em("LBL LBL_{0}", lblFalse);
                foreach (Node n in branchFalse) { n.Emit(); }
                em("LBL LBL_{0}", lblFinishBlock);
            }
        }
    }
        
    #endregion
    // ******************************************************
    // ******************************************************
    // ******************************************************

    static class Emitter
    {
        public static string Process(Node program, int memSize) {
            Stopwatch sw = new Stopwatch(); sw.Restart();
            Node.MEMORY_SIZE = memSize;
            program.Emit();
            sw.Stop();
            Console.WriteLine("EMITTER : {0} ms", sw.ElapsedMilliseconds);
            return Node.GetProgramText();
        }
    }
}
