using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ast_builder
{
    class Program
    {
        static string s = "roaiad"; //"read";

        /* rules:
         * S → rXd | rZd
         * X → oZa | eZa
         * Z → ai
         */

        class Node
        {
            public string val;
            public List<Node> chld;
            public Node() { val = "()"; chld = new List<Node>(); }
            public bool Clear() { chld.Clear(); return true; }
        }

        static void Main(string[] args) {
            int index = 0;
            Node root = new Node();
            root.val = "S";

            Console.WriteLine(Build_S(ref index, 0, root));
            Console.ReadKey();
        }

        static bool accept(char c, ref int index) {
            if (index >= s.Length) { return false; }
            if (c == s[index]) { index++; return true; }
            return false;
        }

        static bool Build_S(ref int index, int level, Node node) {
            int cidx = index;

            Node nX = new Node();
            Node nZ = new Node();

            if (accept('r', ref index) && Build_X(ref index, level + 1, nX) && accept('d', ref index)) {
                for (int i = 0; i < level; i++) { Console.Write(' '); } Console.WriteLine('S');
                node.val = "Xrd";
                node.chld.Add(nX);
                return true;    
            } else if (((index = cidx) > -1) && (node.Clear()) && accept('r', ref index) && Build_Z(ref index, level + 1, nZ) && accept('d', ref index)) {
                for (int i = 0; i < level; i++) { Console.Write(' '); } Console.WriteLine('S');
                node.val = "Zrd";
                node.chld.Add(nZ);
                return true;
            } else {
                return false;
            }
        }
        static bool Build_X(ref int index, int level, Node nx) {
            int cidx = index;

            Node nz = new Node();

            if (accept('o', ref index) && Build_Z(ref index, level + 1, nz) && accept('a', ref index)) {
                for (int i = 0; i < level; i++) { Console.Write(' '); } Console.WriteLine('X');
                nx.val = "Xoa";
                nx.chld.Add(nz);
                return true;
            } else if (((index = cidx) > -1) && accept('e', ref index) && accept('a', ref index)) { // great hack here!
                for (int i = 0; i < level; i++) { Console.Write(' '); } Console.WriteLine('X');
                nx.val = "Xea";
                nx.chld.Add(nz);
                return true;
            } else {
                return false;
            }
        }

        static bool Build_Z(ref int index, int level, Node nz) {
            int cidx = index;

            if (accept('a', ref index) && accept('i', ref index)) {
                for (int i = 0; i < level; i++) { Console.Write(' '); } Console.WriteLine('Z');
                nz.val = "Z";
                return true;
            } else {
                return false;
            }
        }
    }
}
