using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZCM
{
    public enum TOKENTYPE
    {
        NULL   = 0 ,
        UNKNOWN    ,
        VAR_DECL   ,
        FUNC_DECL  ,
        CONST_DECL ,
        STRING     ,
        IF         ,
        ELSE       ,
        WHILE      ,
        ASSIGN     ,
        LPAREN     ,
        RPAREN     ,
        LBRACE     ,
        RBRACE     ,
        LBRCK      ,
        RBRCK      ,
        COLON      ,
        COMA       ,
        VAR        ,
        OPERATOR   ,
        FUNC_CALL  ,
        FUNC_RET   ,
        INDEXER    ,
        GETADDR    ,
        VAR_ASSIGN ,
    }

    struct TOKEN
    {
        public TOKENTYPE Type;
        public string Value;
        public int line, col;
        public TOKEN(TOKENTYPE type, string value                   ) { Type = type; Value = value;      line =    0;      col =   0; }
        public TOKEN(TOKENTYPE type, string value, int line, int col) { Type = type; Value = value; this.line = line; this.col = col; }
    }

    static class Lexer
    {           
        enum STATE
        {
            UNKNOWN          = 0,
            SKIPPING         = 1,
            WORD             = 2,
            STRING           = 3,
            COMMENT          = 4,
            MULTILINECOMMENT = 5,
            NEWLINE          = 6,
        }   

        static STATE cstate = STATE.UNKNOWN;
        //static STATE pstate = STATE.UNKNOWN;
        
        static int  cidx;
        static char pc;
        static char cc;
        static char nc;

        static int cline = 0;
        static int ccol  = 0;

        static List<TOKEN> tokens = new List<TOKEN>();
        static TOKEN ptok;
        static TOKEN pptok;

        public static List<TOKEN> Process(string s) {

            Stopwatch sw = new Stopwatch(); sw.Restart();

            s = s.Replace("\r\n", "\n");

             ptok = new TOKEN(TOKENTYPE.NULL, "");
            pptok = new TOKEN(TOKENTYPE.NULL, "");

            while (true) {
                if (cidx >= s.Length) { break; }

                pc = (cidx >            0) ? s[cidx - 1] : (char)0;
                cc =                         s[cidx    ];
                nc = (cidx < s.Length - 1) ? s[cidx + 1] : (char)0;
                
                switch (cstate) {
                    case STATE.UNKNOWN         : StateUnknown  (); break;
                    case STATE.SKIPPING        : StateSkipping (); break;
                    case STATE.WORD            : StateWord     (); break;
                    case STATE.STRING          : StateString   (); break;
                    case STATE.COMMENT         : StateComment  (); break;
                    case STATE.MULTILINECOMMENT: StateMcomment (); break;
                    case STATE.NEWLINE         : StateNewline  (); break;
                }
            }

            tokens.Add(new TOKEN(TOKENTYPE.NULL, "", cline, ccol));

            sw.Stop(); Console.WriteLine("LEXER : {0} ms", sw.ElapsedMilliseconds);

            return tokens;
        }

        static string cword = "";

        static void StateWord() {
            //string cw = cword.ToLower();

            if (char.IsWhiteSpace(cc)) {
                string cw = cword.ToLower();

                int val;

                     if (cw == "var"              ) { tokens.Add(new TOKEN(TOKENTYPE.VAR_DECL  , cword, cline, ccol)); }
                else if (cw == "func"             ) { tokens.Add(new TOKEN(TOKENTYPE.FUNC_DECL , cword, cline, ccol)); }
                else if (int.TryParse(cw, out val)) { tokens.Add(new TOKEN(TOKENTYPE.CONST_DECL, cword, cline, ccol)); }
                else if (cw == "if"               ) { tokens.Add(new TOKEN(TOKENTYPE.IF        , cword, cline, ccol)); }
                else if (cw == "else"             ) { tokens.Add(new TOKEN(TOKENTYPE.ELSE      , cword, cline, ccol)); }
                else if (cw == "while"            ) { tokens.Add(new TOKEN(TOKENTYPE.WHILE     , cword, cline, ccol)); }
                else if (cw == "="                ) { tokens.Add(new TOKEN(TOKENTYPE.ASSIGN    , cword, cline, ccol)); }
                else if (cw == "("                ) { tokens.Add(new TOKEN(TOKENTYPE.LPAREN    , cword, cline, ccol)); }
                else if (cw == ")"                ) { tokens.Add(new TOKEN(TOKENTYPE.RPAREN    , cword, cline, ccol)); }
                else if (cw == "{"                ) { tokens.Add(new TOKEN(TOKENTYPE.LBRACE    , cword, cline, ccol)); }
                else if (cw == "}"                ) { tokens.Add(new TOKEN(TOKENTYPE.RBRACE    , cword, cline, ccol)); }
                else if (cw == "["                ) { tokens.Add(new TOKEN(TOKENTYPE.LBRCK     , cword, cline, ccol)); }
                else if (cw == "]"                ) { tokens.Add(new TOKEN(TOKENTYPE.RBRCK     , cword, cline, ccol)); }
                else if (cw == ";"                ) { tokens.Add(new TOKEN(TOKENTYPE.COLON     , cword, cline, ccol)); }
                else if (cw == ","                ) { tokens.Add(new TOKEN(TOKENTYPE.COMA      , cword, cline, ccol)); }

                else if (cw == "*"  ||
                         cw == "/"  ||
                         cw == "%"  ||
                         cw == "+"  ||
                         cw == "-"  ||
                         
                         cw == "<"  ||
                         cw == "<=" ||
                         cw == ">"  ||
                         cw == ">=" ||
                         
                         cw == "==" ||
                         cw == "!=" ||
                         
                         cw == "&&" ||
                         cw == "||" ||
                         cw == "!") { tokens.Add(new TOKEN(TOKENTYPE.OPERATOR, cword, cline, ccol)); }

                else if (cw[cw.Length - 1] == '('     ) { tokens.Add(new TOKEN(TOKENTYPE.FUNC_CALL, cword.Substring(0, cword.Length - 1), cline, ccol)); }
                else if (cw[0]             == '&'     ) { tokens.Add(new TOKEN(TOKENTYPE.GETADDR  , cword.Substring(1                  ), cline, ccol)); }
                else if (cw                == "return") { tokens.Add(new TOKEN(TOKENTYPE.FUNC_RET , cword                               , cline, ccol)); }
                else if (cw[cw.Length - 1] == '['     ) { tokens.Add(new TOKEN(TOKENTYPE.INDEXER  , cword.Substring(0, cword.Length - 1), cline, ccol)); }
                else                                    { tokens.Add(new TOKEN(TOKENTYPE.UNKNOWN  , cword                               , cline, ccol)); }
                     
                pptok = ptok;
                 ptok = tokens[tokens.Count - 1];

                cword = "";
                cstate = STATE.UNKNOWN;
                
                cidx++;
                ccol++;
                if (cc == '\r' || cc == '\n') { cline++; ccol = 0; }
            } else {
                cword += cc;
                cidx++;
                ccol++;
                if (cc == '\r' || cc == '\n') { cline++; ccol = 0; }
            }
        }

        static void StateUnknown() {
                 if (cc == '\r' || cc == '\n') { cidx++;  cline++; ccol = 0;                            }
            else if (cc == '/'  && nc == '*' ) { cstate = STATE.MULTILINECOMMENT; cidx += 2; ccol += 2; }
            else if (cc == '/'  && nc == '/' ) { cstate = STATE.COMMENT         ; cidx += 2; ccol += 2; }
            else if (cc == '\"'              ) { cstate = STATE.STRING          ; cidx += 1; ccol++;    }
            else if (char.IsWhiteSpace(cc)   ) { cstate = STATE.SKIPPING        ;            }
            else                               { cstate = STATE.WORD            ;            }
        }                                                                          

        static void StateSkipping() {
                 if (cc == '\r' || cc == '\n') { cidx++;  cline++; ccol = 0; }
            else if (!char.IsWhiteSpace(cc)  ) { cstate = STATE.UNKNOWN; }
            else                               { cidx++; ccol++;         }
        }

        static void StateString() {
                 if (cc == '\r' || cc == '\n') { cword += cc; cline++; ccol = 0; }
            else if (cc == '\"')               { tokens.Add(new TOKEN(TOKENTYPE.STRING, cword, cline, ccol)); cword = ""; cstate = STATE.UNKNOWN; cidx += 1; ccol++; }
            else                               { cword += cc;                                                                                     cidx += 1; ccol++; }
        }                                      

        static void StateComment() {
            if (cc == '\r' || cc == '\n') { cword = ""; cstate = STATE.UNKNOWN; cidx += 1; cline++; ccol = 0; }
            else                          { cword += cc;                        cidx += 1; ccol += 1; }
        }

        static void StateMcomment() {
                 if (cc == '\r' || cc == '\n') { cword += cc; cidx++; cline++; ccol = 0; }
            else if (cc == '*' && nc == '/'  ) { cword = ""; cstate = STATE.UNKNOWN; cidx += 2; ccol += 2; }
            else                               { cword += cc;                        cidx += 1; ccol += 1; }
        }

        static void StateNewline() { }
    }
}
