using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LLVMSharp;

namespace Doe_Language
{
    enum tokens
    {
        tok_eof = -1,
        tok_def = -2,
        tok_extern = -3,
        tok_identifier = -4,
        tok_num = -5
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            string? path = ResolveInputPath(args);
            if (path == null)
            {
                Console.Error.WriteLine("No input file found. Pass a .doe file path, or create test.doe.");
                return;
            }

            string source = File.ReadAllText(path);

            try
            {
                Lexer lexer = new Lexer(source);
                IReadOnlyList<Token> tokensList = lexer.Tokenize();

                if (HasFlag(args, "--tokens"))
                {
                    foreach (Token token in tokensList)
                    {
                        Console.WriteLine(token);
                    }

                    return;
                }

                Parser parser = new Parser(tokensList);
                List<Stmt> program = parser.ParseProgram();

                Interpreter interpreter = new Interpreter();
                interpreter.ExecuteProgram(program);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Doe runtime error: " + ex.Message);
            }
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? ResolveInputPath(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal) && File.Exists(args[i]))
                {
                    return args[i];
                }
            }

            if (File.Exists("test.doe"))
            {
                return "test.doe";
            }

            string lexingPath = Path.Combine("Lexing", "test.doe");
            return File.Exists(lexingPath) ? lexingPath : null;
        }
    }

    public enum TokenType
    {
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Dot,
        DotDot,
        Colon,
        DoubleColon,
        Semicolon,
        Backslash,

        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Pipe,
        StarPipe,
        Bang,
        BangPipe,
        Ampersand,
        DoubleAmpersand,
        BangAmpersand,

        Equal,
        EqualGreater,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,

        Identifier,
        String,
        Number,

        If,
        Else,
        Otherwise,
        IfCase,
        Case,
        Default,
        Def,
        Import,
        Break,
        Then,
        End,

        ReadLn,
        Input,
        Print,

        NoPoly,
        Const,
        Str,
        StringType,
        Int,
        Flt,
        Arr,
        Null,

        Eof
    }

    public sealed class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public object? Literal { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string lexeme, object? literal, int line, int column)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            string literal = Literal == null ? string.Empty : " -> " + Literal;
            return "[" + Line + ":" + Column + "] " + Type + " '" + Lexeme + "'" + literal;
        }
    }

    public sealed class Lexer
    {
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "otherwise", TokenType.Otherwise },
            { "ifcase", TokenType.IfCase },
            { "case", TokenType.Case },
            { "default", TokenType.Default },
            { "def", TokenType.Def },
            { "import", TokenType.Import },
            { "break", TokenType.Break },
            { "then", TokenType.Then },
            { "end", TokenType.End },
            { "readln", TokenType.ReadLn },
            { "input", TokenType.Input },
            { "print", TokenType.Print },
            { "nopoly", TokenType.NoPoly },
            { "const", TokenType.Const },
            { "str", TokenType.Str },
            { "string", TokenType.StringType },
            { "int", TokenType.Int },
            { "flt", TokenType.Flt },
            { "arr", TokenType.Arr },
            { "null", TokenType.Null }
        };

        private readonly string _source;
        private readonly List<Token> _tokens = new List<Token>();
        private int _start;
        private int _current;
        private int _line = 1;
        private int _column = 1;
        private int _tokenLine = 1;
        private int _tokenColumn = 1;

        public Lexer(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IReadOnlyList<Token> Tokenize()
        {
            while (!IsAtEnd)
            {
                _start = _current;
                _tokenLine = _line;
                _tokenColumn = _column;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.Eof, string.Empty, null, _line, _column));
            return _tokens;
        }

        private bool IsAtEnd => _current >= _source.Length;

        private void ScanToken()
        {
            char c = Advance();

            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    return;
                case '(':
                    AddToken(TokenType.LeftParen);
                    return;
                case ')':
                    AddToken(TokenType.RightParen);
                    return;
                case '{':
                    AddToken(TokenType.LeftBrace);
                    return;
                case '}':
                    AddToken(TokenType.RightBrace);
                    return;
                case '[':
                    AddToken(TokenType.LeftBracket);
                    return;
                case ']':
                    AddToken(TokenType.RightBracket);
                    return;
                case ',':
                    AddToken(TokenType.Comma);
                    return;
                case ';':
                    AddToken(TokenType.Semicolon);
                    return;
                case '\\':
                    AddToken(TokenType.Backslash);
                    return;
                case '.':
                    AddToken(Match('.') ? TokenType.DotDot : TokenType.Dot);
                    return;
                case ':':
                    AddToken(Match(':') ? TokenType.DoubleColon : TokenType.Colon);
                    return;
                case '+':
                    AddToken(TokenType.Plus);
                    return;
                case '-':
                    AddToken(TokenType.Minus);
                    return;
                case '*':
                    AddToken(Match('|') ? TokenType.StarPipe : TokenType.Star);
                    return;
                case '%':
                    AddToken(TokenType.Percent);
                    return;
                case '|':
                    AddToken(TokenType.Pipe);
                    return;
                case '&':
                    AddToken(Match('&') ? TokenType.DoubleAmpersand : TokenType.Ampersand);
                    return;
                case '!':
                    if (Match('|'))
                    {
                        AddToken(TokenType.BangPipe);
                        return;
                    }

                    if (Match('&'))
                    {
                        AddToken(TokenType.BangAmpersand);
                        return;
                    }

                    AddToken(TokenType.Bang);
                    return;
                case '=':
                    AddToken(Match('>') ? TokenType.EqualGreater : TokenType.Equal);
                    return;
                case '<':
                    AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                    return;
                case '>':
                    AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                    return;
                case '"':
                    ReadString();
                    return;
                case '/':
                    if (Match('/'))
                    {
                        SkipLineComment();
                        return;
                    }

                    if (Match('('))
                    {
                        SkipBlockComment();
                        return;
                    }

                    AddToken(TokenType.Slash);
                    return;
                default:
                    if (char.IsDigit(c))
                    {
                        ReadNumber();
                        return;
                    }

                    if (IsIdentifierStart(c))
                    {
                        ReadIdentifier();
                        return;
                    }

                    throw new InvalidOperationException("Unexpected character '" + c + "' at " + _tokenLine + ":" + _tokenColumn + ".");
            }
        }

        private void SkipLineComment()
        {
            while (!IsAtEnd && Peek() != '\n')
            {
                Advance();
            }
        }

        private void SkipBlockComment()
        {
            while (!IsAtEnd)
            {
                if (Peek() == ')' && PeekNext() == '\\')
                {
                    Advance();
                    Advance();
                    return;
                }

                Advance();
            }

            throw new InvalidOperationException("Unterminated block comment starting at " + _tokenLine + ":" + _tokenColumn + ".");
        }

        private void ReadString()
        {
            while (!IsAtEnd && Peek() != '"')
            {
                if (Peek() == '\\' && PeekNext() == '"')
                {
                    Advance();
                    Advance();
                    continue;
                }

                Advance();
            }

            if (IsAtEnd)
            {
                throw new InvalidOperationException("Unterminated string starting at " + _tokenLine + ":" + _tokenColumn + ".");
            }

            Advance();
            string lexeme = _source.Substring(_start, _current - _start);
            string value = lexeme.Substring(1, lexeme.Length - 2).Replace("\\\"", "\"");
            AddToken(TokenType.String, value);
        }

        private void ReadNumber()
        {
            while (char.IsDigit(Peek()))
            {
                Advance();
            }

            bool isFloat = false;
            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                isFloat = true;
                Advance();

                while (char.IsDigit(Peek()))
                {
                    Advance();
                }
            }

            string text = _source.Substring(_start, _current - _start);
            object literal = isFloat
                ? (object)double.Parse(text, CultureInfo.InvariantCulture)
                : (object)int.Parse(text, CultureInfo.InvariantCulture);

            AddToken(TokenType.Number, literal);
        }

        private void ReadIdentifier()
        {
            while (IsIdentifierPart(Peek()))
            {
                Advance();
            }

            string text = _source.Substring(_start, _current - _start);
            if (Keywords.TryGetValue(text, out TokenType keywordType))
            {
                AddToken(keywordType);
                return;
            }

            AddToken(TokenType.Identifier);
        }

        private static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private char Advance()
        {
            char c = _source[_current++];
            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            return c;
        }

        private bool Match(char expected)
        {
            if (IsAtEnd || _source[_current] != expected)
            {
                return false;
            }

            Advance();
            return true;
        }

        private char Peek()
        {
            return IsAtEnd ? '\0' : _source[_current];
        }

        private char PeekNext()
        {
            return _current + 1 >= _source.Length ? '\0' : _source[_current + 1];
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object? literal)
        {
            string lexeme = _source.Substring(_start, _current - _start);
            _tokens.Add(new Token(type, lexeme, literal, _tokenLine, _tokenColumn));
        }
    }
}
