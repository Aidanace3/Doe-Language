using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LLVMSharp;

namespace Doe_Language
{
    enum Tokens
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
                Console.Error.WriteLine("No input file found. Pass a .doe file path, or create examples/test.doe.");
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

                DebuggerSession? debugger = null;
                if (HasFlag(args, "--debug"))
                {
                    List<int> breakpoints = ParseBreakpoints(args);
                    bool startInStepMode = breakpoints.Count == 0 || HasFlag(args, "--step");
                    debugger = new DebuggerSession(breakpoints, startInStepMode);
                }

                bool silentOutput = HasFlag(args, "--silent");
                if (HasFlag(args, "--verbose"))
                {
                    silentOutput = false;
                }

                Interpreter interpreter = new Interpreter(debugger, silentOutput);
                interpreter.ExecuteProgram(program);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
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

        private static List<int> ParseBreakpoints(string[] args)
        {
            List<int> lines = new List<int>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string? payload = null;

                if (arg.StartsWith("--break=", StringComparison.OrdinalIgnoreCase))
                {
                    payload = arg.Substring("--break=".Length);
                }
                else if (string.Equals(arg, "--break", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    payload = args[i + 1];
                }

                if (string.IsNullOrWhiteSpace(payload))
                {
                    continue;
                }

                string[] parts = payload.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int p = 0; p < parts.Length; p++)
                {
                    if (int.TryParse(parts[p].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int line) && line > 0)
                    {
                        lines.Add(line);
                    }
                }
            }

            return lines;
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

            string examplesPath = Path.Combine("examples", "test.doe");
            if (File.Exists(examplesPath))
            {
                return examplesPath;
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
        Question,
        Colon,
        DoubleColon,
        Semicolon,
        Backslash,

        Plus,
        Minus,
        Star,
        DoubleStar,
        Slash,
        Percent,
        DoublePercent,
        Caret,
        Pipe,
        StarPipe,
        Bang,
        BangPipe,
        Ampersand,
        DoubleAmpersand,
        BangAmpersand,

        Equal,
        EqualEqual,
        EqualGreater,
        ShiftRight,
        ShiftLeft,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,

        Identifier,
        String,
        Number,
        Boolean,

        If,
        Elif,
        Else,
        Otherwise,
        IfCase,
        Case,
        Default,
        Def,
        Import,
        Break,
        Return,
        Then,
        End,
        Dict,
        Locked,
        Conf,
        Yield,

        ReadLn,
        Input,
        Print,
        AwaitVal,

        NoPoly,
        Const,
        Str,
        StringType,
        Int,
        Flt,
        Arr,
        Null,
        NewLine,

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
            { "elif", TokenType.Elif },
            { "else", TokenType.Else },
            { "otherwise", TokenType.Otherwise },
            { "ifcase", TokenType.IfCase },
            { "case", TokenType.Case },
            { "default", TokenType.Default },
            { "def", TokenType.Def },
            { "import", TokenType.Import },
            { "break", TokenType.Break },
            { "return", TokenType.Return },
            { "then", TokenType.Then },
            { "end", TokenType.End },
            { "dict", TokenType.Dict },
            { "locked", TokenType.Locked },
            { "conf", TokenType.Conf },
            { "yield", TokenType.Yield },
            { "yeild", TokenType.Yield },
            { "readln", TokenType.ReadLn },
            { "input", TokenType.Input },
            { "print", TokenType.Print },
            { "awaitval", TokenType.AwaitVal },
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
        private int _expressionNesting;

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
                    return;
                case '\n':
                    if (_expressionNesting == 0)
                    {
                        AddToken(TokenType.NewLine);
                    }

                    return;
                case '(':
                    AddToken(TokenType.LeftParen);
                    _expressionNesting++;
                    return;
                case ')':
                    AddToken(TokenType.RightParen);
                    if (_expressionNesting > 0)
                    {
                        _expressionNesting--;
                    }

                    return;
                case '{':
                    AddToken(TokenType.LeftBrace);
                    return;
                case '}':
                    AddToken(TokenType.RightBrace);
                    return;
                case '[':
                    AddToken(TokenType.LeftBracket);
                    _expressionNesting++;
                    return;
                case ']':
                    AddToken(TokenType.RightBracket);
                    if (_expressionNesting > 0)
                    {
                        _expressionNesting--;
                    }

                    return;
                case ',':
                    AddToken(TokenType.Comma);
                    return;
                case ';':
                    AddToken(TokenType.Semicolon);
                    return;
                case '?':
                    AddToken(TokenType.Question);
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
                    if (Match('|'))
                    {
                        AddToken(TokenType.StarPipe);
                        return;
                    }

                    AddToken(Match('*') ? TokenType.DoubleStar : TokenType.Star);
                    return;
                case '%':
                    AddToken(Match('%') ? TokenType.DoublePercent : TokenType.Percent);
                    return;
                case '^':
                    AddToken(TokenType.Caret);
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
                    if (Match('>'))
                    {
                        AddToken(TokenType.EqualGreater);
                        return;
                    }

                    AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                    return;
                case '@':
                    ReadBooleanLiteral();
                    return;
                case '<':
                    if (Match('<'))
                    {
                        AddToken(TokenType.ShiftLeft);
                        return;
                    }

                    AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                    return;
                case '>':
                    if (Match('>'))
                    {
                        AddToken(TokenType.ShiftRight);
                        return;
                    }

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

        private void ReadBooleanLiteral()
        {
            while (IsIdentifierPart(Peek()))
            {
                Advance();
            }

            string lexeme = _source.Substring(_start, _current - _start);
            string text = lexeme.Length > 1 ? lexeme.Substring(1) : string.Empty;
            if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
            {
                AddToken(TokenType.Boolean, true);
                return;
            }

            if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
            {
                AddToken(TokenType.Boolean, false);
                return;
            }

            throw new InvalidOperationException("Invalid bool literal '" + lexeme + "' at " + _tokenLine + ":" + _tokenColumn + ".");
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
