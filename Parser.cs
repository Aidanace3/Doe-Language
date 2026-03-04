using System;
using System.Collections.Generic;

namespace Doe_Language
{
    public sealed class Parser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private int _current;

        public Parser(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<Stmt> ParseProgram()
        {
            List<Stmt> statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            return statements;
        }

        private Stmt ParseDeclaration()
        {
            if (Match(TokenType.Import))
            {
                return ParseImport();
            }

            if (Match(TokenType.Def))
            {
                return ParseFunction();
            }

            if (IsVariableDeclarationStart())
            {
                return ParseVariableDeclaration();
            }

            return ParseStatement();
        }

        private bool IsVariableDeclarationStart()
        {
            TokenType t = Peek().Type;
            return t == TokenType.Const ||
                   t == TokenType.NoPoly ||
                   t == TokenType.Str ||
                   t == TokenType.StringType ||
                   t == TokenType.Int ||
                   t == TokenType.Flt ||
                   t == TokenType.Arr;
        }

        private Stmt ParseImport()
        {
            Token module = Consume(TokenType.Identifier, "Expected module name after import.");
            Match(TokenType.Semicolon);
            return new ImportStmt(module.Lexeme);
        }

        private Stmt ParseFunction()
        {
            Token name = Consume(TokenType.Identifier, "Expected function name after def.");
            Consume(TokenType.LeftParen, "Expected '(' after function name.");
            Consume(TokenType.RightParen, "Expected ')' after function name.");
            BlockStmt body = ParseBlockStatement("Expected '{' to start function body.");
            return new FunctionStmt(name.Lexeme, body.Statements);
        }

        private Stmt ParseVariableDeclaration()
        {
            bool isConst = Match(TokenType.Const);
            string? typeHint = null;

            if (Match(TokenType.NoPoly))
            {
                typeHint = "NoPoly";
            }

            if (IsTypeKeyword(Peek().Type))
            {
                Token typeToken = Advance();
                typeHint = typeHint == null ? typeToken.Lexeme : typeHint + " " + typeToken.Lexeme;
            }

            Token name = Consume(TokenType.Identifier, "Expected variable name in declaration.");
            Expr? initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = ParseExpression();
            }

            Match(TokenType.Semicolon);
            return new VarDeclStmt(name.Lexeme, isConst, typeHint, initializer);
        }

        private Stmt ParseStatement()
        {
            if (Match(TokenType.If))
            {
                return ParseIfStatement();
            }

            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement("Expected '{' to start block.");
            }

            if (Match(TokenType.Break))
            {
                Match(TokenType.Semicolon);
                return new BreakStmt();
            }

            return ParseExpressionStatement();
        }

        private Stmt ParseIfStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after if.");
            Expr condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after if condition.");

            if (Match(TokenType.DoubleColon))
            {
                Match(TokenType.Then);
                Match(TokenType.Break);
            }

            Stmt thenBranch = ParseStatementBody();

            Stmt? elseBranch = null;
            if (Match(TokenType.Else) || Match(TokenType.Otherwise))
            {
                elseBranch = ParseStatementBody();
            }

            return new IfStmt(condition, thenBranch, elseBranch);
        }

        private Stmt ParseStatementBody()
        {
            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement("Expected '{' to start block.");
            }

            return ParseStatement();
        }

        private BlockStmt ParseBlockStatement(string message)
        {
            Consume(TokenType.LeftBrace, message);
            List<Stmt> statements = new List<Stmt>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RightBrace, "Expected '}' after block.");
            return new BlockStmt(statements);
        }

        private Stmt ParseExpressionStatement()
        {
            Expr expression = ParseExpression();
            Match(TokenType.Semicolon);
            return new ExprStmt(expression);
        }

        private Expr ParseExpression()
        {
            return ParseAssignment();
        }

        private Expr ParseAssignment()
        {
            Expr expr = ParseLogicalOr();

            if (Match(TokenType.Equal))
            {
                Token equals = Previous();
                Expr value = ParseAssignment();
                VariableExpr? variable = expr as VariableExpr;

                if (variable == null)
                {
                    throw ParseError(equals, "Invalid assignment target.");
                }

                return new AssignExpr(variable.Name, value, variable.NameToken);
            }

            return expr;
        }

        private Expr ParseLogicalOr()
        {
            Expr expr = ParseLogicalAnd();

            while (Match(TokenType.Pipe) || Match(TokenType.BangPipe))
            {
                Token op = Previous();
                Expr right = ParseLogicalAnd();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseLogicalAnd()
        {
            Expr expr = ParseComparison();

            while (Match(TokenType.DoubleAmpersand) || Match(TokenType.BangAmpersand) || Match(TokenType.StarPipe))
            {
                Token op = Previous();
                Expr right = ParseComparison();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseComparison()
        {
            Expr expr = ParseTerm();

            while (Match(TokenType.Greater) || Match(TokenType.GreaterEqual) || Match(TokenType.EqualGreater) || Match(TokenType.Less) || Match(TokenType.LessEqual))
            {
                Token op = Previous();
                Expr right = ParseTerm();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseTerm()
        {
            Expr expr = ParseFactor();

            while (Match(TokenType.Plus) || Match(TokenType.Minus))
            {
                Token op = Previous();
                Expr right = ParseFactor();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseFactor()
        {
            Expr expr = ParseUnary();

            while (Match(TokenType.Star) || Match(TokenType.Slash) || Match(TokenType.Percent))
            {
                Token op = Previous();
                Expr right = ParseUnary();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseUnary()
        {
            if (Match(TokenType.Bang) || Match(TokenType.Minus))
            {
                Token op = Previous();
                Expr right = ParseUnary();
                return new UnaryExpr(op, right);
            }

            return ParseCall();
        }

        private Expr ParseCall()
        {
            Expr expr = ParsePrimary();

            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    List<Expr> args = new List<Expr>();
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        }
                        while (Match(TokenType.Comma));
                    }

                    Token close = Consume(TokenType.RightParen, "Expected ')' after function arguments.");
                    VariableExpr? variable = expr as VariableExpr;
                    if (variable == null)
                    {
                        throw ParseError(close, "Only named functions can be called.");
                    }

                    expr = new CallExpr(variable.NameToken, args);
                    continue;
                }

                break;
            }

            return expr;
        }

        private Expr ParsePrimary()
        {
            if (Match(TokenType.Number))
            {
                return new LiteralExpr(Previous().Literal);
            }

            if (Match(TokenType.String))
            {
                return new LiteralExpr(Previous().Literal);
            }

            if (Match(TokenType.Null))
            {
                return new LiteralExpr(null);
            }

            if (Match(TokenType.Identifier) || Match(TokenType.Print) || Match(TokenType.Input) || Match(TokenType.ReadLn))
            {
                Token name = Previous();
                return new VariableExpr(name.Lexeme, name);
            }

            if (Match(TokenType.LeftParen))
            {
                Expr expression = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return new GroupExpr(expression);
            }

            throw ParseError(Peek(), "Expected expression.");
        }

        private static bool IsTypeKeyword(TokenType type)
        {
            return type == TokenType.Str ||
                   type == TokenType.StringType ||
                   type == TokenType.Int ||
                   type == TokenType.Flt ||
                   type == TokenType.Arr;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type))
            {
                return Advance();
            }

            throw ParseError(Peek(), message);
        }

        private bool Match(TokenType type)
        {
            if (!Check(type))
            {
                return false;
            }

            Advance();
            return true;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd())
            {
                return false;
            }

            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd())
            {
                _current++;
            }

            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.Eof;
        }

        private Token Peek()
        {
            return _tokens[_current];
        }

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private Exception ParseError(Token token, string message)
        {
            return new InvalidOperationException("Parse error at " + token.Line + ":" + token.Column + " near '" + token.Lexeme + "': " + message);
        }
    }
}
