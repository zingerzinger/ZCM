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
        public List<Node> chld = new List<Node>();
        public bool Clear() { chld.Clear(); return true; }

        public virtual void DebugPrint() { Console.Write("PROGRAM"); }
    }

    partial class VarDecl : Node
    {
        public string Name;
        public string Type;
        public string Value;

        override public void DebugPrint() { Console.Write("VARDECL : {0}, {1}, {2}", Name, Type, Value); }
    }

    partial class FuncDecl : Node
    {
        public string Name;
        public bool   isVoid;
        public int    numArgs;
        public int    numLocalVars;

        public List<string>  argList = new List<string>();
        public List<VarDecl> varList = new List<VarDecl>();

        // all children are function body expressions

        override public void DebugPrint() {
            Console.Write("FUNCDECL : {0}, {1}, {2}; (", Name, isVoid, numArgs);
            foreach (string a in argList) { Console.Write("{0}, ", a); }
            Console.Write(") | {0} | ", numLocalVars);
            foreach (VarDecl vd in varList) { Console.Write("{0}, {1}, {2}; ", vd.Name, vd.Type, vd.Value); }
        }
    }

    partial class Expression : Node
    {
        override public void DebugPrint() { Console.Write("EXPRESSION : "); foreach (Node n in chld) { Console.Write(' '); n.DebugPrint(); } }
    }

    partial class Operator : Node
    {
        public string Value;
        override public void DebugPrint() { Console.Write("OP {0}", Value); }
    }

    partial class Var : Node
    {
        public string Name;
        override public void DebugPrint() { Console.Write("VAR {0}", Name); }
    }

    partial class FuncCall : Node
    {
        public string Name;
        // all children are expressions
        override public void DebugPrint() { Console.Write("FUNCCALL {0} | ", Name); foreach (Node n in chld) { n.DebugPrint(); } }
    }

    partial class Indexer : Node
    {
        public Var var = new Var();
        public Expression ex = new Expression();
        override public void DebugPrint() { Console.Write("INDEXER {0} : ", var.Name); ex.DebugPrint(); }
    }

    partial class Const : Node
    {
        public string Value;
        override public void DebugPrint() { Console.Write("CONST {0}", Value); }
    }

    partial class VarAssign : Node
    {
        public Expression lex = new Expression();
        public Expression rex = new Expression();
        override public void DebugPrint() { Console.Write("VARASSIGN | "); lex.DebugPrint(); Console.Write(" = "); rex.DebugPrint(); }
    }

    partial class FuncRet : Node
    {
        public Expression ex = new Expression();
        override public void DebugPrint() { Console.Write("FUNCRET | "); ex.DebugPrint(); }
    }

    partial class GetAddr : Node
    {
        public Var var = new Var();
        override public void DebugPrint() { Console.Write("GETADDR {0}", var.Name); }
    }

    partial class While : Node
    {
        // all children are body expressions
        public Expression condition = new Expression();
        override public void DebugPrint() { Console.Write("WHILE | "); condition.DebugPrint(); }
    }

    partial class IF : Node
    {
        public Expression condition   = new Expression();
        public List<Node> branchTrue  = new List<Node>();
        public List<Node> branchFalse = new List<Node>();

        override public void DebugPrint() {
            Console.Write("IF | "); condition.DebugPrint();
            Console.Write(" | TRUE : ");
            foreach (Node n in branchTrue) { n.DebugPrint(); }
            Console.Write(" | FALSE : ");
            foreach (Node n in branchFalse) { n.DebugPrint(); }
            Console.Write(" |");
        }
    }
        
    #endregion
    // ******************************************************
    // ******************************************************
    // ******************************************************

    static class Parser
    {
        static List<TOKEN> toks;

        public static Node Process(List<TOKEN> tokens) {
            Stopwatch sw = new Stopwatch(); sw.Restart();
            
            toks = tokens;
            int index = 0;
            Node root = new Node();
            bool success = Parse(ref index, root);
            sw.Stop();

            Console.WriteLine("===============");
            Console.WriteLine("PARSER : {0}", success ? "SUCCESS" : "FAIL");
            Console.WriteLine("===============");
            Console.Write(errors);
            Console.WriteLine("===============");

            DebugPrint(root, 0, -1); Console.WriteLine("===============");
            
            Console.WriteLine("PARSER : {0} ms", sw.ElapsedMilliseconds);

            return root;
        }

        static bool Expect(int index, TOKENTYPE expectedTokenType) { return (index >= toks.Count) ? (false) : (toks[index].Type == expectedTokenType); }
        static TOKENTYPE Peek(int index) { return (index >= toks.Count) ? (TOKENTYPE.NULL) : (toks[index].Type); }
        static TOKEN TokenAt(int index) { return (index >= toks.Count) ? (new TOKEN(TOKENTYPE.NULL, "NULL", -1, -1)) : (toks[index]); }
        static TOKEN Accept(ref int index) { TOKEN result = toks[index]; index++; return result; }

        static void DebugPrint(Node node, int level, int prevLevel) { // DFS
            for (int i = 0; i < level; i++) { Console.Write(' '); /*Console.Write("{0}", (i == prevLevel) ? '|' : ' ');*/ }
            Console.Write("|- ");
            node.DebugPrint();
            Console.WriteLine();
            foreach (Node n in node.chld) { DebugPrint(n, level + 3, level); }
        }

        static void rerr(int index, string message) {
            success = false;
            TOKEN t = TokenAt(index);

            errors += string.Format("{0} : {1} | {2}, {3} : {4}\n",
                                t.Type .ToString().PadRight(10, ' '),
                                t.Value.ToString().PadRight(10, ' '),

                                t.line .ToString() .PadLeft( 3, ' '),
                                t.col  .ToString().PadRight( 3, ' '),

                                message);
        }

        // ******************************************************
        // ******************************************************
        // ******************************************************
        #region PARSERS

        static string errors = "";
        static FuncDecl currentFunction = null;
        static bool success = true;
        
        // S -> [VAR_DECL], [FUNCTION_DECL]
        static bool Parse(ref int index, Node root) {
            int cidx = index;

            Node n = new Node();

            while (pVarDecl (ref index, ref n)) { root.chld.Add(n); }
            while (pFuncDecl(ref index, ref n)) { root.chld.Add(n); }
            
            if (!Expect(index, TOKENTYPE.NULL)) { rerr(index, "Expected EOF"); }

            bool entryFound = false;
            foreach (Node nn in root.chld) { if (nn is FuncDecl) { if (((FuncDecl)nn).Name == "main") { entryFound = true; break; } } }
            if (!entryFound) { rerr(index, "Program entry point not found"); }
            return true;
        }

        // VAR_DECL -> "var", UNKNOWN, (ASSIGN | CONST_DECL), CONST_DECL | STRING
        static bool pVarDecl(ref int index, ref Node node) {
            int cidx = index;

            if (!(Expect(index    , TOKENTYPE.VAR_DECL) &&
                  Expect(index + 1, TOKENTYPE.UNKNOWN))) { rerr(index, "var?"); return false; }

                VarDecl vd = new VarDecl();


                   if (Expect(index + 2, TOKENTYPE.ASSIGN  ) &&
                       Expect(index + 3, TOKENTYPE.CONST_DECL)) {
                       
                Accept(ref index);
                vd.Name  = Accept(ref index).Value;
                Accept(ref index);
                vd.Value = Accept(ref index).Value;
                vd.Type = "const";
                node = vd;
                return true;
            } else if (Expect(index + 2, TOKENTYPE.ASSIGN  ) &&
                       Expect(index + 3, TOKENTYPE.STRING  )) {

                Accept(ref index);
                vd.Name = Accept(ref index).Value;
                Accept(ref index);
                vd.Value = Accept(ref index).Value;
                vd.Type = "string";
                node = vd;
                return true;
            } else if (Expect(index + 2, TOKENTYPE.CONST_DECL)) {

                Accept(ref index);
                vd.Name = Accept(ref index).Value;
                vd.Value = Accept(ref index).Value;
                vd.Type = "array";
                node = vd;
                return true;
            } else { rerr(index, "var?"); return false; }
        }

        static bool pIf(ref int index, ref Node node) {
            int cidx = index;
            
            if (!(Expect(index    , TOKENTYPE.IF    ) &&
                  Expect(index + 1, TOKENTYPE.LPAREN))) { rerr(index, "if?"); return false; }

            Accept(ref index); // "if"
            Accept(ref index); // "("

            IF iff = new IF();
            Node condition = new Node();

            if (!pExpression(ref index, ref condition)) { index = cidx; rerr(index, "if?"); return false; }
            iff.condition = (Expression)condition;

            if (Expect(index, TOKENTYPE.RPAREN)) { Accept(ref index); } else { index = cidx; rerr(index, "if?"); return false; } // ')'

            if (Expect(index, TOKENTYPE.LBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "if?"); return false; } // parsing header done

            // *** *** ***

            bool error          = false;
            bool branchTrueDone = false;

            while (true) { // parse anything in the { } block (branchTrue) [IF | WHILE | VAR_ASSIGN | FUNC_CALL | FUNC_RET]
                TOKENTYPE tt = Peek(index);
                Node n = new Node();
                
                switch (tt) {
                    case TOKENTYPE.IF         : if (pIf       (ref index, ref n)) { iff.branchTrue.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.WHILE      : if (pWhile    (ref index, ref n)) { iff.branchTrue.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.ASSIGN     : if (pVarAssign(ref index, ref n)) { iff.branchTrue.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_CALL  : if (pFuncCall (ref index, ref n)) { iff.branchTrue.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_RET   : if (pFuncRet  (ref index, ref n)) { iff.branchTrue.Add(n); currentFunction.isVoid = false; } else { error = true; } break;
                    default                   : branchTrueDone = true; break;
                }

                if (error || branchTrueDone) { break; }
            }

            if (error) { index = cidx; rerr(index, "if?"); return false; }

            if (Expect(index, TOKENTYPE.RBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "if?"); return false; } // parse '}'
            
            // *** *** ***

            if (!Expect(index, TOKENTYPE.ELSE)) { node = iff; /*rerr(index, "if?");*/ return true; } // no else branch

            Accept(ref index); // eat else

            if (Expect(index, TOKENTYPE.LBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "if?"); return false; } // parse '{'

            // *** *** ***

            bool branchFalseDone = false;
            
            while (true) { // parse anything in the { } block (branchFalse) [IF | WHILE | VAR_ASSIGN | FUNC_CALL | FUNC_RET]
                TOKENTYPE tt = Peek(index);
                Node n = new Node();
                
                switch (tt) {                                                                      
                    case TOKENTYPE.IF         : if (pIf       (ref index, ref n)) { iff.branchFalse.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.WHILE      : if (pWhile    (ref index, ref n)) { iff.branchFalse.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.ASSIGN     : if (pVarAssign(ref index, ref n)) { iff.branchFalse.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_CALL  : if (pFuncCall (ref index, ref n)) { iff.branchFalse.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_RET   : if (pFuncRet  (ref index, ref n)) { iff.branchFalse.Add(n); currentFunction.isVoid = false; } else { error = true; } break;
                    default                   : branchFalseDone = true; break;
                }

                if (error || branchFalseDone) { break; }
            }

            if (error) { index = cidx; rerr(index, "if?"); return false; }

            if (Expect(index, TOKENTYPE.RBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "if?"); return false; } // parse '}'

            // *** *** ***

            node = iff;
            return true;
        }

        static bool pWhile(ref int index, ref Node node) {
            int cidx = index;

            if (!(Expect(index    , TOKENTYPE.WHILE ) &&
                  Expect(index + 1, TOKENTYPE.LPAREN))) { rerr(index, "while?"); return false; }

            Accept(ref index); // while
            Accept(ref index); // (

            While wh = new While();
            Node condition = new Node();

            if (!pExpression(ref index, ref condition)) { index = cidx; return false; }
            wh.condition = (Expression)condition;

            if (Expect(index, TOKENTYPE.RPAREN)) { Accept(ref index); } else { index = cidx; rerr(index, "while?"); return false; } // ')'
            if (Expect(index, TOKENTYPE.LBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "while?"); return false; } // '{'

            bool error     = false;
            bool blockDone = false;

            while (true) { // parse anything in the { } block [IF | WHILE | VAR_ASSIGN | FUNC_CALL | FUNC_RET]
                TOKENTYPE tt = Peek(index);
                Node n = new Node();
                
                switch (tt) {
                    case TOKENTYPE.IF         : if (pIf       (ref index, ref n)) { wh.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.WHILE      : if (pWhile    (ref index, ref n)) { wh.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.ASSIGN     : if (pVarAssign(ref index, ref n)) { wh.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_CALL  : if (pFuncCall (ref index, ref n)) { wh.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_RET   : if (pFuncRet  (ref index, ref n)) { wh.chld.Add(n); currentFunction.isVoid = false; } else { error = true; } break;
                    default                   : blockDone = true; break;
                }

                if (error || blockDone) { break; }
            }

            if (error) { index = cidx; rerr(index, "while?"); return false; }

            if (Expect(index, TOKENTYPE.RBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "while?"); return false; } // parse '}'

            node = wh;
            return true;
        }

        // FUNC_DECL -> "func", UNKNOWN, LPAREN, [UNKNOWN], RPAREN, LBRACE, [VAR_DECL], [IF | WHILE | VAR_ASSIGN | FUNC_CALL | FUNC_RET], RBRACE
        static bool pFuncDecl(ref int index, ref Node node) {
            int cidx = index;

            if (!(Expect(index    , TOKENTYPE.FUNC_DECL) &&
                  Expect(index + 1, TOKENTYPE.UNKNOWN  ) &&
                  Expect(index + 2, TOKENTYPE.LPAREN ))) { rerr(index, "funcdecl?"); return false; }

            FuncDecl fd = new FuncDecl();
            fd.isVoid = true;
            currentFunction = fd;

            // parse header
            Accept(ref index); // func
            fd.Name = Accept(ref index).Value;
            Accept(ref index); // '('

            while (true) { // parse arg list
                if      (Expect(index, TOKENTYPE.RPAREN))  { Accept(ref index); break; }
                else if (Expect(index, TOKENTYPE.UNKNOWN)) { TOKEN arg = Accept(ref index); fd.argList.Add(arg.Value); }
                else                                       { rerr(index, "funcdecl?"); return false; }
            }

            fd.numArgs = fd.argList.Count;
                
            if (Expect(index, TOKENTYPE.LBRACE)) { Accept(ref index); } else { rerr(index, "funcdecl?"); return false; } // parsing header done
                
            Node localVar = new Node();
            while (pVarDecl(ref index, ref localVar)) { fd.varList.Add((VarDecl)localVar); } // parsed local variables
            fd.numLocalVars = fd.varList.Count;

            // *** *** ***

            bool error     = false;
            bool blockDone = false;

            while (true) { // parse anything in the { } block [IF | WHILE | VAR_ASSIGN | FUNC_CALL | FUNC_RET]
                TOKENTYPE tt = Peek(index);
                Node n = new Node();
                    
                switch (tt) {
                    case TOKENTYPE.IF         : if (pIf       (ref index, ref n)) { fd.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.WHILE      : if (pWhile    (ref index, ref n)) { fd.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.ASSIGN     : if (pVarAssign(ref index, ref n)) { fd.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_CALL  : if (pFuncCall (ref index, ref n)) { fd.chld.Add(n);                                 } else { error = true; } break;
                    case TOKENTYPE.FUNC_RET   : if (pFuncRet  (ref index, ref n)) { fd.chld.Add(n); currentFunction.isVoid = false; } else { error = true; } break;
                    default                   : blockDone = true; break;              
                }

                if (error || blockDone) { break; }
            }

            if (error) { index = cidx; rerr(index, "funcdecl?"); return false; }

            if (Expect(index, TOKENTYPE.RBRACE)) { Accept(ref index); } else { index = cidx; rerr(index, "funcdecl?"); return false; } // parse '}'

            node = fd;
            return true;
        }

        static bool pVar(ref int index, ref Node node) {
            int cidx = index;
            if (!Expect(index, TOKENTYPE.UNKNOWN)) { rerr(index, "var?"); return false; }
            Var var = new Var();
            var.Name = Accept(ref index).Value;
            node = var;
            return true;
        }

        static bool pFuncCall(ref int index, ref Node node) {
            int cidx = index;

            if (!Expect(index, TOKENTYPE.FUNC_CALL)) { rerr(index, "funccall?"); return false; }

            FuncCall fc = new FuncCall();
            fc.Name = Accept(ref index).Value;

            Node n = new Node();
            while (pExpression(ref index, ref n)) {
                if (n.chld.Count > 0) { fc.chld.Add(n); }
                if (Expect(index, TOKENTYPE.COMA  )) { Accept(ref index); continue; } // ','
                if (Expect(index, TOKENTYPE.RPAREN)) { Accept(ref index); break;    } // ')'
            }
               
            node = fc;
            return true;
        }

        static bool pGetAddr(ref int index, ref Node node) {
            int cidx = index;
            if (!Expect(index, TOKENTYPE.GETADDR)) { rerr(index, "getaddr?"); return false; }
            GetAddr ga = new GetAddr();
            ga.var.Name = Accept(ref index).Value;
            node = ga;
            return true;
        }

        static bool pFuncRet(ref int index, ref Node node) {
            int cidx = index;
            if (!Expect(index, TOKENTYPE.FUNC_RET)) { rerr(index, "funcret?"); return false; }

            Accept(ref index); // 'return'
            FuncRet fr = new FuncRet();
            Node ex = new Node();
            //if (!pExpression(ref index, ref ex)) { index = cidx; return false; }

            pExpression(ref index, ref ex);
            if (ex.chld.Count > 0) { fr.ex = (Expression)ex; }

            if (Expect(index, TOKENTYPE.COLON)) { Accept(ref index); } else { index = cidx; rerr(index, "funcret?"); return false; } // ';'

            node = fr;
            return true;
        }

        static bool pIndexer(ref int index, ref Node node) {
            int cidx = index;
            if (!Expect(index, TOKENTYPE.INDEXER)) { rerr(index, "indexer?"); return false; }

            Indexer indx = new Indexer();
            indx.var = new Var();
            indx.var.Name = Accept(ref index).Value;

            Node ex = new Node();
            if (!pExpression(ref index, ref ex)) { index = cidx; rerr(index, "indexer?"); return false; }

            if (Expect(index, TOKENTYPE.RBRCK)) { Accept(ref index); } else { index = cidx; rerr(index, "indexer?"); return false; } // ']'

            indx.ex = (Expression)ex;
            
            node = indx;
            return true;
        }

        // = ( EXPRESSION , EXPRESSION )
        static bool pVarAssign(ref int index, ref Node node) {
            int cidx = index;
            if (Expect(index, TOKENTYPE.ASSIGN)) { Accept(ref index); } else { index = cidx; rerr(index, "assign?"); return false; } // '='
            if (Expect(index, TOKENTYPE.LPAREN)) { Accept(ref index); } else { index = cidx; rerr(index, "assign?"); return false; } // '('

            VarAssign va = new VarAssign();

            Node lex = new Node();
            if (!pExpression(ref index, ref lex)) { index = cidx; rerr(index, "assign?"); return false; }
            if (Expect(index, TOKENTYPE.COMA  )) { Accept(ref index); } else { index = cidx; rerr(index, "assign?"); return false; } // ','

            Node rex = new Node();
            if (!pExpression(ref index, ref rex)) { index = cidx; rerr(index, "assign?"); return false; }
            if (Expect(index, TOKENTYPE.RPAREN)) { Accept(ref index); } else { index = cidx; rerr(index, "assign?"); return false; } // ')'

            va.lex = (Expression)lex;
            va.rex = (Expression)rex;
                                 
            node = va;
            return true;
        }

        // EXPRESSION -> [UNKNOWN | FUNC_CALL | INDEXER, <OPERATOR>]
        static bool pExpression(ref int index, ref Node node) {
            int cidx = index;

            Expression ex = new Expression();

            bool error = false;
            bool done  = false;
            int numOperands = 0;
            int numLParens  = 0;
            int numRParens  = 0;

            while (true) {
                TOKENTYPE tt = Peek(index);
                Node operand = new Node();

                switch (tt) {
                    case TOKENTYPE.UNKNOWN    : if (pVar     (ref index, ref operand)) { ex.chld.Add(operand); } else { error = true; } break;
                    case TOKENTYPE.FUNC_CALL  : if (pFuncCall(ref index, ref operand)) { ex.chld.Add(operand); } else { error = true; } break;
                    case TOKENTYPE.INDEXER    : if (pIndexer (ref index, ref operand)) { ex.chld.Add(operand); } else { error = true; } break;
                    case TOKENTYPE.GETADDR    : if (pGetAddr (ref index, ref operand)) { ex.chld.Add(operand); } else { error = true; } break;
                    case TOKENTYPE.CONST_DECL : Const    co = new    Const(); co.Value = Accept(ref index).Value; ex.chld.Add(co);               break;
                    case TOKENTYPE.LPAREN     : Operator lp = new Operator(); lp.Value = Accept(ref index).Value; ex.chld.Add(lp); numLParens++; break;
                    case TOKENTYPE.RPAREN     : 
                        if (numRParens >= numLParens) { done = true; break; }
                                                Operator rp = new Operator(); rp.Value = Accept(ref index).Value; ex.chld.Add(rp); numRParens++; break;
                    case TOKENTYPE.OPERATOR   : Operator op = new Operator(); op.Value = Accept(ref index).Value; ex.chld.Add(op);               break;
                    default: done = true; break;
                }

                if (done || error) { break; }
                numOperands++;
            }

            if (error) { index = cidx; rerr(index, "expression?"); return false; }
            if (!SortExpression(ref ex.chld)) { index = cidx; rerr(index, "expression?"); return false; }
            node = ex;
            return true;
        }

        static int prec(string op) {
            switch (op) {
                case "*"  : return 4;
                case "/"  : return 4;
                case "%"  : return 4;

                case "+"  : return 3;
                case "-"  : return 3;
                          
                case "<"  : return 2;
                case "<=" : return 2;
                case ">"  : return 2;
                case ">=" : return 2;

                case "==" : return 1;
                case "!=" : return 1;

                case "&&" : return 0;
                case "||" : return 0;
                case "!"  : return 0;

                default: return -1;
            }
        }
        
        static bool SortExpression(ref List<Node> t) {
            if (t.Count < 2) { return true; }

            int i = 0;
            Stack<Node> stack = new Stack<Node>();
            List<Node> output = new List<Node>();
            Node n;

            while (i < t.Count) {
                n = t[i]; i++;

                if ((n.GetType() == typeof(Const   )) ||
                     n.GetType() == typeof(FuncCall)  ||
                     n.GetType() == typeof(Var     )  ||
                     n.GetType() == typeof(Indexer )  ||
                     n.GetType() == typeof(GetAddr )) { output.Add(n); }
                else if ((n.GetType() == typeof(Operator)) &&
                         (((Operator)n).Value != "(") &&
                         (((Operator)n).Value != ")")) {

                    while (stack.Count > 0 &&
                        ((stack.Peek().GetType() == typeof(Operator)) && (((Operator)stack.Peek()).Value != "(")) &&
                        ((stack.Peek().GetType() == typeof(Operator)) && (prec(((Operator)stack.Peek()).Value) > prec(((Operator)n).Value))
                        )) {
                        output.Add(stack.Pop());
                    }

                    stack.Push(n);
                } else if (n.GetType() == typeof(Operator) && (((Operator)n).Value == "(")) {
                    stack.Push(n);
                } else if (n.GetType() == typeof(Operator) && (((Operator)n).Value == ")") ) {
                    while (stack.Count > 0) {
                        if ((stack.Peek().GetType() == typeof(Operator)) && (((Operator)(stack.Peek())).Value != "(")) {
                            output.Add(stack.Pop());
                        } else {
                            stack.Pop();
                        }
                    }
                }
            }

            while (stack.Count > 0) {
                Node l = stack.Pop();

                if ((l.GetType() == typeof(Operator)) &&
                    ((((Operator)l).Value == "(") ||
                    ( ((Operator)l).Value == ")"))) {
                    
                } else { output.Add(l); }
            }

            t = output; return true;
        }

        #endregion
        // ******************************************************
        // ******************************************************
        // ******************************************************
    }
}
