using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCD_Chapter1
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            string program = @"(((9 + (5 * 5)) * 5) + 5)
                               (5 * (9 + 2))
                               (5 * (5 + 5))";
            
            Lexer lexer = new Lexer(program);
            Parser parser = new Parser();

            CodeGenerator generator = new CodeGenerator();
            Interpreter interpreter = new Interpreter();

            Expression expression = Expression.Create();

            Console.WriteLine("\tProgram: " + program + "\n");

            while (true)
            {
                parser.ParseExpression(lexer, expression);

                if (lexer.CurrentToken.Class == TokenClass.EOF)
                {
                    break;
                }

                generator.Process(expression);
                interpreter.Process(expression);

            }

            Console.WriteLine("\n\tEnd");
        }
    }

    #region Enums
    internal enum TokenClass
    {
        Invalid,
        EOF,
        Digit,
        Char
    }

    internal enum Operator
    {
        Nop,
        Add,
        Mul
    }
    #endregion

    #region Structs
    internal struct TokenType
    {
        #region Fields
        public TokenClass Class;
        public char Repr;
        public int Line;
        #endregion

        public override string ToString()
        {
            return string.Format("{0} - {1}", Class.ToString().ToUpper(), Repr);
        }
    }
    #endregion

    internal sealed class Expression
    {
        #region Fields
        public int Line;
        public char Type;
        public int Value;

        public Expression Left;
        public Expression Right;

        public Operator Operator;
        #endregion

        public Expression()
        {
        }

        public static Expression Create()
        {
            Expression e = new Expression();

            e.Left = new Expression();
            e.Right = new Expression();

            return e;
        }

        public string Collapse()
        {
            string left = Left == null ? "" : Left.Collapse();
            string right = Right == null ? "" : Right.Collapse();

            return string.Format("{0} - {1} - {2}", left, ToString(), right);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Type, Value, Operator.ToString().ToUpper());
        }
    }

    internal sealed class Lexer
    {
        #region Fields
        private readonly string[] lines;
        private string line;

        private TokenType currentToken;
        
        private int charIndex;
        private int lineIndex;
        #endregion

        #region Properties
        public TokenType CurrentToken
        {
            get
            {
                return currentToken;
            }
        }
        #endregion

        public Lexer(string source)
        {
            lines = source.Replace(" ", "").Split('\n');

            line = lines.Length > 0 ? lines[0] : string.Empty;
        }

        private char NextChar()
        {
            return line[charIndex++];
        }

        private bool IsLayoutChar(char ch)
        {
            return ch == '\t' || ch == '\n'; 
        }

        public TokenType GetNextToken()
        {
            currentToken = new TokenType();

            char ch;

            do
            {
                if (charIndex >= line.Length)
                {
                    lineIndex++;

                    if (lineIndex >= lines.Length)
                    {
                        // EOF token.
                        currentToken.Class = TokenClass.EOF;
                        currentToken.Repr = '#';

                        return currentToken;
                    }

                    line = lines[lineIndex];
                    charIndex = 0;
                }

                ch = NextChar();

            } while (IsLayoutChar(ch));

            currentToken.Class = char.IsDigit(ch) ? TokenClass.Digit : TokenClass.Char;
            currentToken.Repr = ch;
            currentToken.Line = lineIndex;

            return currentToken;
        }
    }

    internal sealed class Parser
    {
        public Parser()
        {
        }

        public bool ParseOperator(Lexer lexer, ref Operator oper) 
        {
            TokenType token = lexer.GetNextToken();

            if (token.Repr == '+')
            {
                oper = Operator.Add;

                return true;
            }
            if (token.Repr == '*')
            {
                oper = Operator.Mul;

                return true;
            }

            return false;
        }

        public bool ParseExpression(Lexer lexer, Expression expression)
        {
            TokenType token = lexer.GetNextToken();
            expression.Line = token.Line;

            // Check if digit.
            if (token.Class == TokenClass.Digit)
            {
                expression.Type = 'D';
                expression.Value = token.Repr - '0';
                
                return true;
            }

            // Try to parse parenthesized expression.
            if (token.Repr == '(')
            {
                expression.Type = 'P';

                if (expression.Left == null) expression.Left = Expression.Create();
                if (expression.Right == null) expression.Right = Expression.Create();

                Expression left = expression.Left;
                Expression right = expression.Right;

                if (!ParseExpression(lexer, left))
                {
                    throw new InvalidOperationException("Missing expression.");
                }
                if (!ParseOperator(lexer, ref expression.Operator))
                {
                    throw new InvalidOperationException("Missing operator.");
                }
                if (!ParseExpression(lexer, right))
                {
                    throw new InvalidOperationException("Missing expression.");
                }

                token = lexer.GetNextToken();

                if (token.Repr != ')')
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\t\n ---Error---\n");
                    Console.ResetColor();

                    Console.WriteLine("Expression: " + expression.Collapse());

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\t\n ---Error---");
                    Console.ResetColor();

                    throw new InvalidOperationException("Missing ')'.");
                }

                return true;
            }

            return false;
        }
    }

    internal sealed class CodeGenerator
    {
        public CodeGenerator()
        {
        }

        private void ProcessExpression(Expression expression)
        {
            switch (expression.Type)
            {
                case 'D':
                    Console.WriteLine("LINE {0}: PUSH {1}", expression.Line, expression.Value);
                   break;
                case 'P':
                    ProcessExpression(expression.Left);
                    ProcessExpression(expression.Right);

                    string oper = expression.Operator.ToString().ToUpper();

                    Console.WriteLine("LINE {0}: {1}", expression.Line, oper);
                   break;
                default:
                    break;
            }
        }

        public void Process(Expression expression)
        {
            ProcessExpression(expression);

            Console.WriteLine("LINE {0}: PRINT", expression.Line);
        }
    }

    internal sealed class Interpreter 
    {
        public Interpreter()
        {
        }

        private int Interpret(Expression expression)
        {
            switch (expression.Type)
            {
                case 'D':
                    return expression.Value;
                case 'P':
                    int a = Interpret(expression.Left);
                    int b = Interpret(expression.Right);

                    int result = expression.Operator == Operator.Add ? a + b : a * b;

                    return result;
                default:
                    return 0;
            }
        }

        public void Process(Expression expression)
        {
            Console.WriteLine("LINE {0}: {1}", expression.Line, Interpret(expression));
        }
    }
}
