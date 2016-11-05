// CptS 321
// Emily Clemens

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CptS322
{
    class Node { }

    class OpNode : Node
    {
        public OpNode(char o) { op = o; }
        public char op;
        public Node left, right;
    }

    class VarNode : Node
    {
        public VarNode(string n) { name = n; }
        public string name;
    }

    class ConstNode : Node
    {
        public ConstNode(double v) { value = v; }
        public double value;
    }

    public class ExpTree
    {
        public ExpTree(string expression, Dictionary<string, double> vars)
        {
            this.vars = vars;
            try
            {
                // compile the expression with all whitespace stripped
                // from the string via a regex
                Compile(Regex.Replace(expression, @"\s+", ""));
            }
            catch
            {
                // malformed expressions are just zero
                root = new ConstNode(0);
            }
        }

        public void SetVar(string varName, double varValue)
        {
            vars[varName] = varValue;
        }

        // return the variables in the expression
        public HashSet<string> GetExpLocals()
        {
            return locals;
        }

        // eval a tree by calling its recursive private implementation
        public double Eval() { return Eval(root); }
        private double Eval(Node n)
        {
            if (n is ConstNode)
                return (n as ConstNode).value;
            if (n is VarNode)
            {
                var name = (n as VarNode).name;
                if (vars.ContainsKey(name))
                    return vars[name];

                return 0;
            }

            var on = n as OpNode;
            switch (on.op)
            {
                case '+': return Eval(on.left) + Eval(on.right);
                case '-': return Eval(on.left) - Eval(on.right);
                case '*': return Eval(on.left) * Eval(on.right);
                case '/': return Eval(on.left) / Eval(on.right);
            }

            // should never get here unless malformed OpNode
            return 0;
        }

        private void Compile(string exp)
        {
            // tokenize the expression using 3 regex capture groups:
            //  ([A-Za-z]+\d*)  :  at least one alpha character followed by 0 or more digits
            //  ([-/\+\*\(\)])  :  any of the +, -, *, / operators or parens
            //  (\d+\.?\d+)     :  a constant number that may contain a decimal number
            //
            // after tokenizing make sure that the resulting token list doesnt
            // contain any empty strings and then listify the enumerable
            var tokens = Regex.Split(exp, @"([A-Za-z]+\d*)|([-/\+\*\(\)])|(\d+\.?\d+)")
                              .Where(s => s != String.Empty)
                              .ToList<string>();

            // convert token list to prefix expression which is then trivial to iterate through
            // and linearly build the expression tree
            var nodeStack = new Stack<Node>();
            foreach (var tok in InfixToPrefix(tokens))
            {
                if (Char.IsLetter(tok[0]))
                {
                    nodeStack.Push(new VarNode(tok));

                    /*
                    if(!vars.ContainsKey(tok))
                        vars[tok] = 0;  // default value of 0
                    */
                    locals.Add(tok);
                }
                else if (Char.IsDigit(tok[0]))
                {
                    nodeStack.Push(new ConstNode(Double.Parse(tok)));
                }
                else
                {
                    var on = new OpNode(tok[0]);
                    on.right = nodeStack.Pop();
                    on.left = nodeStack.Pop();
                    nodeStack.Push(on);
                }
            }

            root = nodeStack.Pop();
        }

        // shunting-yard algorithm
        // https://en.wikipedia.org/wiki/Shunting-yard_algorithm
        private static List<string> InfixToPrefix(List<string> tokens)
        {
            var opStack = new Stack<string>();
            var prefix = new List<string>();

            foreach (var tok in tokens)
            {
                switch (tok)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                        while (opStack.Count > 0)
                        {
                            // break if cur operator precedence is greater than prev
                            if (Precedence(opStack.Peek()) < Precedence(tok))
                                break;

                            prefix.Add(opStack.Pop());
                        }

                        opStack.Push(tok);
                        break;
                    case "(":
                        opStack.Push(tok);
                        break;
                    case ")":
                        // pop until left paren and try to handle mismatched paren
                        while (opStack.Count > 0 && opStack.Peek() != "(")
                            prefix.Add(opStack.Pop());

                        opStack.Pop();  // remove ) and dont put it into prefix list
                        break;
                    default:
                        // not an operator so just push var/const vals 
                        // directly
                        prefix.Add(tok);
                        break;
                }
            }

            while (opStack.Count > 0)
                prefix.Add(opStack.Pop());

            return prefix;
        }

        // return numbers that are greater the higher the operator precedence
        private static int Precedence(String op)
        {
            switch (op)
            {
                case "*":
                case "/":
                    return 2;
                case "+":
                case "-":
                    return 1;
                case "(":
                case ")":
                    return 0;
                default:
                    return -1;  // error
            }
        }

        Node root;
        Dictionary<string, double> vars = new Dictionary<string, double>();
        HashSet<string> locals = new HashSet<string>();
    }
}
