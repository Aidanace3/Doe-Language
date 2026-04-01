using System;
using System.Collections.Generic;
using System.Text;

namespace Doe_Language
{
    public sealed class Parser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private int _current;
        private readonly List<ParseError> _errors = new List<ParseError>();

        public Parser(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
        }

        public IReadOnlyList<ParseError> Errors => _errors;

        public List<Stmt> ParseProgram()
        {
            List<Stmt> statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                SkipNewLines();
                if (IsAtEnd())
                {
                    break;
                }

                statements.Add(ParseDeclaration());
            }

            return statements;
        }

        private Stmt ParseDeclaration()
        {
            SkipNewLines();

            if (Match(TokenType.Import))
            {
                return ParseImport(Previous());
            }

            if (Match(TokenType.With))
            {
                return ParseImport(Previous());
            }

            if (Match(TokenType.Locked))
            {
                Token lockedToken = Previous();
                Consume(TokenType.Dict, "Expected 'dict' after 'locked'.");
                return ParseDictDeclaration(lockedToken, true);
            }

            if (Match(TokenType.Dict))
            {
                return ParseDictDeclaration(Previous(), false);
            }

            if (Match(TokenType.Def))
            {
                return ParseFunction(Previous());
            }

            if (Match(TokenType.New))
            {
                return ParseNewDeclaration(Previous());
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

        private Stmt ParseImport(Token importToken)
        {
            StringBuilder moduleBuilder = new StringBuilder();
            TokenType? previousType = null;
            while (!IsStatementTerminator(Peek().Type))
            {
                Token token = Advance();
                if (ShouldInsertImportSpace(previousType, token.Type))
                {
                    moduleBuilder.Append(' ');
                }

                moduleBuilder.Append(token.Lexeme);
                previousType = token.Type;
            }

            if (moduleBuilder.Length == 0)
            {
                throw ParseError(Peek(), "Expected module name after import/with.");
            }

            ConsumeOptionalStatementTerminator();
            return new ImportStmt(moduleBuilder.ToString(), importToken.Line);
        }

        private Stmt ParseNewDeclaration(Token newToken)
        {
            List<Token> parts = new List<Token>();
            while (IsNameLikeToken(Peek().Type))
            {
                parts.Add(Advance());
            }

            if (parts.Count == 0)
            {
                throw ParseError(Peek(), "Expected declaration name after 'new'.");
            }

            string variableName = parts[parts.Count - 1].Lexeme;
            Match(TokenType.Colon);
            Expr initializer = ParseInlineArrayLiteralBlock();
            ConsumeOptionalStatementTerminator();
            return new VarDeclStmt(variableName, false, "Arr", initializer, newToken.Line);
        }

        private Stmt ParseDictDeclaration(Token startToken, bool isLocked)
        {
            string? lockedType = null;
            if (isLocked && Match(TokenType.LeftParen))
            {
                Token typeToken;
                if (IsTypeKeyword(Peek().Type) || Check(TokenType.Dict) || Check(TokenType.Identifier))
                {
                    typeToken = Advance();
                }
                else
                {
                    throw ParseError(Peek(), "Expected locked dict type.");
                }

                lockedType = typeToken.Lexeme;
                Consume(TokenType.RightParen, "Expected ')' after locked dict type.");
            }

            string dictName = "__dict_" + startToken.Line;
            if (Check(TokenType.Identifier) &&
                (Check(TokenType.Colon, 1) || Check(TokenType.LeftBrace, 1)))
            {
                dictName = Advance().Lexeme;
            }

            Match(TokenType.Colon);
            SkipNewLines();
            BlockStmt body = ParseBlockStatement("Expected '{' to start dictionary body.");
            ConsumeOptionalStatementTerminator();
            return new DictDeclStmt(dictName, isLocked, lockedType, body.Statements, startToken.Line);
        }

        private Stmt ParseFunction(Token defToken)
        {
            Token name = Consume(TokenType.Identifier, "Expected function name after def.");
            List<string> parameters = new List<string>();

            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        Token param = Consume(TokenType.Identifier, "Expected parameter name.");
                        parameters.Add(param.Lexeme);
                    }
                    while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' after function parameters.");
            }

            SkipNewLines();
            BlockStmt body = ParseBlockStatement("Expected '{' to start function body.");
            return new FunctionStmt(name.Lexeme, parameters, body.Statements, defToken.Line);
        }

        private Stmt ParseVariableDeclaration()
        {
            int line = Peek().Line;
            bool isConst = false;
            string? typeHint = null;

            bool consumedModifier = true;
            while (consumedModifier)
            {
                consumedModifier = false;
                if (Match(TokenType.Const))
                {
                    isConst = true;
                    consumedModifier = true;
                }

                if (Match(TokenType.NoPoly))
                {
                    typeHint = typeHint == null ? "NoPoly" : typeHint;
                    consumedModifier = true;
                }
            }

            if (IsTypeKeyword(Peek().Type))
            {
                Token typeToken = Advance();
                typeHint = typeHint == null ? typeToken.Lexeme : typeHint + " " + typeToken.Lexeme;

                if (typeToken.Type == TokenType.Arr && Match(TokenType.LeftBracket))
                {
                    List<string> parts = new List<string>();
                    while (!Check(TokenType.RightBracket) && !IsAtEnd())
                    {
                        parts.Add(Advance().Lexeme);
                    }

                    Consume(TokenType.RightBracket, "Expected ']' after Arr[type] declaration.");
                    typeHint = typeHint + "[" + string.Join(" ", parts) + "]";
                }
            }

            Token name = Consume(TokenType.Identifier, "Expected variable name in declaration.");
            Expr? initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = ParseExpression();
            }

            ConsumeOptionalStatementTerminator();
            return new VarDeclStmt(name.Lexeme, isConst, typeHint, initializer, line);
        }

        private Stmt ParseStatement()
        {
            SkipNewLines();

            // Check for standalone else/elif/otherwise without matching if
            if (Match(TokenType.Elif))
            {
                throw ParseError(Previous(), "elif without matching if statement.");
            }

            if (Match(TokenType.Else) || Match(TokenType.Otherwise))
            {
                throw ParseError(Previous(), "else/otherwise without matching if statement.");
            }

            if (CheckLegacyPointCaseStart())
            {
                return ParseLegacyPointCaseStatement();
            }

            if (CheckPointAwaitStart())
            {
                return ParsePointAwaitStatement();
            }

            if (Match(TokenType.Conf))
            {
                return ParseConfStatement(Previous());
            }

            if (Match(TokenType.If))
            {
                return ParseIfStatement(Previous());
            }

            if (Match(TokenType.Unless))
            {
                return ParseIfStatement(Previous(), true);
            }

            if (Match(TokenType.IfCase))
            {
                return ParseIfCaseStatement(Previous());
            }

            if (CheckIdentifierLexeme("as"))
            {
                return ParseAsLoopStatement();
            }

            if (CheckIdentifierLexeme("each"))
            {
                return ParseEachLoopStatement();
            }

            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement("Expected '{' to start block.");
            }

            if (Match(TokenType.Break))
            {
                int line = Previous().Line;
                ConsumeOptionalStatementTerminator();
                return new BreakStmt(line);
            }

            if (Match(TokenType.Return))
            {
                return ParseReturnStatement(Previous());
            }

            if (Match(TokenType.Yield))
            {
                return ParseYieldStatement(Previous());
            }

            return ParseExpressionStatement();
        }

        private Stmt ParseConfStatement(Token confToken)
        {
            Token name = Consume(TokenType.Identifier, "Expected config name after conf.");
            Match(TokenType.Colon);
            SkipNewLines();
            BlockStmt body = ParseBlockStatement("Expected '{' to start config body.");
            ConsumeOptionalStatementTerminator();
            return new ConfigDeclStmt(name.Lexeme, body.Statements, confToken.Line);
        }

        private Stmt ParseReturnStatement(Token returnToken)
        {
            Expr? value = null;
            if (!Check(TokenType.Semicolon) && !Check(TokenType.NewLine) && !Check(TokenType.RightBrace) && !Check(TokenType.Eof))
            {
                value = ParseExpression();
            }

            ConsumeOptionalStatementTerminator();
            return new ReturnStmt(value, returnToken, returnToken.Line);
        }

        private Stmt ParseYieldStatement(Token yieldToken)
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

                Consume(TokenType.RightParen, "Expected ')' after yield/yeild arguments.");
                ConsumeOptionalStatementTerminator();

                // README form: yeild(value >> *Point) should dispatch directly.
                if (args.Count == 1 &&
                    TryUnpackYieldDispatch(args[0], out Expr dispatchValue, out string dispatchPoint))
                {
                    return new YieldStmt(dispatchValue, dispatchPoint, null, yieldToken, yieldToken.Line);
                }

                return new ExprStmt(new CallExpr(yieldToken, args), yieldToken.Line);
            }

            Expr dispatchExpr = ParseShift();
            if (!TryUnpackYieldDispatch(dispatchExpr, out Expr value, out string pointName))
            {
                throw ParseError(Peek(), "Expected point dispatch in yield/yeild statement.");
            }

            string? aliasName = null;
            if (MatchIdentifierLexeme("as"))
            {
                aliasName = Consume(TokenType.Identifier, "Expected alias variable after 'as'.").Lexeme;
            }

            ConsumeOptionalStatementTerminator();
            return new YieldStmt(value, pointName, aliasName, yieldToken, yieldToken.Line);
        }

        private Stmt ParseAsLoopStatement()
        {
            Token asToken = Advance();
            Consume(TokenType.LeftParen, "Expected '(' after as.");
            Expr condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after as condition.");
            Consume(TokenType.Colon, "Expected ':' after as(condition).");

            Stmt body = ParseStatementBody();
            return new WhileStmt(condition, body, asToken.Line);
        }

        private Stmt ParseEachLoopStatement()
        {
            Token eachToken = Advance();
            Consume(TokenType.LeftParen, "Expected '(' after each.");
            Token iterator = Consume(TokenType.Identifier, "Expected iterator variable in each loop.");

            if (!MatchIdentifierLexeme("in"))
            {
                throw ParseError(Peek(), "Expected 'in' in each loop.");
            }

            Expr iterable = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after each iterable.");
            if (!MatchIdentifierLexeme("do"))
            {
                throw ParseError(Peek(), "Expected 'do' after each(...).");
            }

            Consume(TokenType.Colon, "Expected ':' after each(... ) do.");

            Stmt body = ParseStatementBody();
            return new EachStmt(iterator.Lexeme, iterable, body, eachToken.Line);
        }

        private Stmt ParseIfStatement(Token ifToken, bool negateCondition = false)
        {
            if (!Check(TokenType.LeftParen))
            {
                throw ParseError(Peek(), "Expected '(' after conditional keyword.");
            }
            Consume(TokenType.LeftParen, "Expected '(' after conditional keyword.");
            
            Expr condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after if condition.");
            if (negateCondition)
            {
                condition = new UnaryExpr(new Token(TokenType.Bang, "!", null, ifToken.Line, ifToken.Column), condition);
            }

            Stmt? conditionAction = ParseOptionalConditionAction();

            Stmt thenBranch = ParseStatementBody();

            Stmt? elseBranch = null;
            SkipNewLines();
            
            if (Match(TokenType.Elif))
            {
                elseBranch = ParseElifBranch();
            }
            else if (Match(TokenType.Else) || Match(TokenType.Otherwise))
            {
                elseBranch = ParseElseBranch();
            }

            return new IfStmt(condition, conditionAction, thenBranch, elseBranch, ifToken.Line);
        }

        private Stmt ParseElifBranch()
        {
            int line = Previous().Line;
            Consume(TokenType.LeftParen, "Expected '(' after elif.");
            Expr condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after elif condition.");
            Stmt? conditionAction = ParseOptionalConditionAction();

            Stmt thenBranch = ParseStatementBody();

            Stmt? elseBranch = null;
            SkipNewLines();
            
            if (Match(TokenType.Elif))
            {
                elseBranch = ParseElifBranch();
            }
            else if (Match(TokenType.Else) || Match(TokenType.Otherwise))
            {
                elseBranch = ParseElseBranch();
            }

            return new IfStmt(condition, conditionAction, thenBranch, elseBranch, line);
        }

        private Stmt? ParseOptionalConditionAction()
        {
            if (!Match(TokenType.DoubleColon))
            {
                return null;
            }

            if (Match(TokenType.Then))
            {
                return null;
            }

            if (Match(TokenType.Break))
            {
                return new BreakStmt(Previous().Line);
            }

            if (Match(TokenType.Yield))
            {
                return ParseYieldStatement(Previous());
            }

            if (IsCallableToken(Peek().Type))
            {
                int line = Peek().Line;
                Expr expr = ParseExpression();
                return new ExprStmt(expr, line);
            }

            return null;
        }

        private Stmt ParseElseBranch()
        {
            if (Match(TokenType.DoubleColon))
            {
                if (Match(TokenType.Break))
                {
                    return new BreakStmt(Previous().Line);
                }

                Match(TokenType.Then);

                if (Match(TokenType.Yield))
                {
                    return ParseYieldStatement(Previous());
                }

                if (IsCallableToken(Peek().Type))
                {
                    int line = Peek().Line;
                    Expr expr = ParseExpression();
                    return new ExprStmt(expr, line);
                }
            }

            return ParseStatementBody();
        }

        private static bool IsCallableToken(TokenType type)
        {
            return type == TokenType.Identifier ||
                   type == TokenType.Print ||
                   type == TokenType.Input ||
                   type == TokenType.ReadLn ||
                   type == TokenType.Yield;
        }

        private Stmt ParseIfCaseStatement(Token ifCaseToken)
        {
            Consume(TokenType.LeftParen, "Expected '(' after IfCase.");
            Expr subject = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after IfCase subject.");
            SkipNewLines();
            Consume(TokenType.LeftBrace, "Expected '{' to start IfCase block.");

            List<CaseClause> cases = new List<CaseClause>();
            Stmt? defaultBranch = null;

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(TokenType.RightBrace) || IsAtEnd())
                {
                    break;
                }

                if (Match(TokenType.Case))
                {
                    Match(TokenType.Colon);
                    Expr matchExpr = ParseCaseMatchExpression();
                    if (MatchIdentifierLexeme("is"))
                    {
                        matchExpr = ParseCaseMatchExpression();
                    }

                    Consume(TokenType.Colon, "Expected ':' after Case expression.");
                    Stmt body = ParseCaseBody();
                    cases.Add(new CaseClause(matchExpr, body));
                    continue;
                }

                if (Match(TokenType.Default) || Match(TokenType.Otherwise))
                {
                    Match(TokenType.Colon);
                    if (MatchIdentifierLexeme("x"))
                    {
                        MatchIdentifierLexeme("is");
                        if (!Check(TokenType.LeftBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RightBrace))
                        {
                            ParseExpression();
                        }

                        Match(TokenType.Colon);
                    }

                    defaultBranch = ParseCaseBody();
                    continue;
                }

                throw ParseError(Peek(), "Expected Case or Default in IfCase block.");
            }

            Consume(TokenType.RightBrace, "Expected '}' after IfCase block.");
            return new IfCaseStmt(subject, cases, defaultBranch, ifCaseToken.Line);
        }

        private Expr ParseCaseMatchExpression()
        {
            if (Match(TokenType.LeftParen))
            {
                List<Expr> elements = new List<Expr>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        elements.Add(ParseCaseGroupItem());
                    }
                    while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' after Case group.");
                return new ArrayLiteralExpr(elements);
            }

            return ParseExpression();
        }

        private Expr ParseCaseGroupItem()
        {
            if (Match(TokenType.Identifier))
            {
                return new LiteralExpr(Previous().Lexeme);
            }

            return ParseExpression();
        }

        private Stmt ParseCaseBody()
        {
            SkipNewLines();

            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement("Expected '{' to start Case block.");
            }

            return ParseStatement();
        }

        private Stmt ParseStatementBody()
        {
            SkipNewLines();

            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement("Expected '{' to start block.");
            }

            return ParseStatement();
        }

        private BlockStmt ParseBlockStatement(string message)
        {
            Token leftBrace = Consume(TokenType.LeftBrace, message);
            List<Stmt> statements = new List<Stmt>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(TokenType.RightBrace) || IsAtEnd())
                {
                    break;
                }

                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RightBrace, "Expected '}' after block.");
            return new BlockStmt(statements, leftBrace.Line);
        }

        private Stmt ParseExpressionStatement()
        {
            int line = Peek().Line;
            Expr expression = ParseExpression();
            ConsumeOptionalStatementTerminator();
            return new ExprStmt(expression, line);
        }

        private Expr ParseInlineArrayLiteralBlock()
        {
            Consume(TokenType.LeftBrace, "Expected '{' to start new declaration body.");
            List<Expr> elements = new List<Expr>();
            SkipNewLines();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                elements.Add(ParseExpression());

                if (Match(TokenType.Comma))
                {
                    SkipNewLines();
                    continue;
                }

                SkipNewLines();
            }

            Consume(TokenType.RightBrace, "Expected '}' after new declaration body.");
            return new ArrayLiteralExpr(elements);
        }

        private Expr ParseExpression()
        {
            return ParseAssignment();
        }

        private Expr ParseAssignment()
        {
            Expr expr = ParseShift();

            if (Match(TokenType.Equal))
            {
                Token equals = Previous();
                Expr value = ParseAssignment();
                VariableExpr? variable = expr as VariableExpr;

                if (variable == null)
                {
                    IndexExpr? indexExpr = expr as IndexExpr;
                    if (indexExpr == null)
                    {
                        throw ParseError(equals, "Invalid assignment target.");
                    }

                    return new IndexAssignExpr(indexExpr.Target, indexExpr.Index, value, equals);
                }

                return new AssignExpr(variable.Name, value, variable.NameToken);
            }

            return expr;
        }

        private Expr ParseShift()
        {
            Expr expr = ParseLogicalOr();

            while (Match(TokenType.ShiftRight) || Match(TokenType.ShiftLeft))
            {
                Token op = Previous();
                Expr right = ParseLogicalOr();
                expr = new BinaryExpr(expr, op, right);
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
            Expr expr = ParseEquality();

            while (Match(TokenType.DoubleAmpersand) || Match(TokenType.BangAmpersand) || Match(TokenType.StarPipe))
            {
                Token op = Previous();
                Expr right = ParseEquality();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseEquality()
        {
            Expr expr = ParseComparison();

            while (Match(TokenType.EqualEqual) || Match(TokenType.TripleEqual))
            {
                Token op = Previous();
                Expr right = ParseComparison();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseComparison()
        {
            Expr expr = ParseRange();

            while (Match(TokenType.Greater) || Match(TokenType.GreaterEqual) || Match(TokenType.EqualGreater) || Match(TokenType.Less) || Match(TokenType.LessEqual))
            {
                Token op = Previous();
                Expr right = ParseRange();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParseRange()
        {
            Expr expr = ParseTerm();

            while (Match(TokenType.DotDot))
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
            Expr expr = ParsePower();

            while (Match(TokenType.Star) || Match(TokenType.Slash) || Match(TokenType.Percent) || Match(TokenType.DoublePercent))
            {
                Token op = Previous();
                Expr right = ParsePower();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private Expr ParsePower()
        {
            Expr expr = ParseUnary();

            while (Match(TokenType.DoubleStar) || Match(TokenType.Caret))
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

            if (Match(TokenType.Star))
            {
                Token pointName = ConsumeNameLikeToken("Expected point name after '*'.");
                return new PointRefExpr(pointName.Lexeme, pointName);
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

                if (Match(TokenType.LeftBracket))
                {
                    Token openBracket = Previous();
                    Expr index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after index expression.");
                    expr = new IndexExpr(expr, index, openBracket);
                    continue;
                }

                if (Match(TokenType.Dot))
                {
                    Token dot = Previous();
                    Token member = ConsumeNameLikeToken("Expected property name after '.'.");
                    expr = new IndexExpr(expr, new LiteralExpr(member.Lexeme), dot);
                    continue;
                }

                break;
            }

            return expr;
        }

        private Expr ParsePrimary()
        {
            if (Match(TokenType.Arr))
            {
                return ParseArrayCtorExpression();
            }

            if (Match(TokenType.LeftBracket))
            {
                return ParseArrayLiteral();
            }

            if (Match(TokenType.Number))
            {
                return new LiteralExpr(Previous().Literal);
            }

            if (Match(TokenType.Boolean))
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

            if (Match(TokenType.Identifier) || Match(TokenType.Print) || Match(TokenType.Input) || Match(TokenType.ReadLn) || Match(TokenType.Yield))
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

        private Expr ParseArrayCtorExpression()
        {
            string? elementType = null;
            if (Match(TokenType.LeftBracket))
            {
                List<string> parts = new List<string>();
                while (!Check(TokenType.RightBracket) && !IsAtEnd())
                {
                    parts.Add(Advance().Lexeme);
                }

                Consume(TokenType.RightBracket, "Expected ']' after Arr[type] constructor.");
                elementType = parts.Count == 0 ? null : string.Join(" ", parts);
            }

            return new ArrayCtorExpr(elementType);
        }

        private Expr ParseArrayLiteral()
        {
            List<Expr> elements = new List<Expr>();
            if (!Check(TokenType.RightBracket))
            {
                do
                {
                    elements.Add(ParseExpression());
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightBracket, "Expected ']' after array literal.");
            return new ArrayLiteralExpr(elements);
        }

        private static bool IsTypeKeyword(TokenType type)
        {
            return type == TokenType.Str ||
                   type == TokenType.StringType ||
                   type == TokenType.Int ||
                   type == TokenType.Flt ||
                   type == TokenType.Arr;
        }

        private bool CheckPointAwaitStart()
        {
            return Check(TokenType.LeftParen) &&
                   Check(TokenType.Star, 1) &&
                   IsNameLikeToken(PeekType(2)) &&
                   Check(TokenType.Colon, 3) &&
                   Check(TokenType.RightParen, 4) &&
                   Check(TokenType.AwaitVal, 5);
        }

        private bool CheckLegacyPointCaseStart()
        {
            return Check(TokenType.Star) &&
                   IsNameLikeToken(PeekType(1)) &&
                   Check(TokenType.IfCase, 2);
        }

        private Stmt ParsePointAwaitStatement()
        {
            int line = Peek().Line;
            Consume(TokenType.LeftParen, "Expected '(' to start point declaration.");
            Consume(TokenType.Star, "Expected '*' in point declaration.");
            Token pointName = ConsumeNameLikeToken("Expected point name.");
            Consume(TokenType.Colon, "Expected ':' after point name.");
            Consume(TokenType.RightParen, "Expected ')' after point declaration.");
            Consume(TokenType.AwaitVal, "Expected awaitval keyword after point declaration.");
            Consume(TokenType.LeftParen, "Expected '(' after awaitval.");
            Token parameter = Consume(TokenType.Identifier, "Expected awaitval parameter name.");
            Match(TokenType.Semicolon);
            Consume(TokenType.RightParen, "Expected ')' after awaitval parameter.");

            Stmt body = ParseStatementBody();
            return new AwaitPointStmt(pointName.Lexeme, parameter.Lexeme, body, line);
        }

        private Stmt ParseLegacyPointCaseStatement()
        {
            int line = Peek().Line;
            Consume(TokenType.Star, "Expected '*' before point case name.");
            Token pointName = ConsumeNameLikeToken("Expected point case name.");
            Consume(TokenType.IfCase, "Expected ifCase after point name.");
            Consume(TokenType.LeftParen, "Expected '(' after ifCase.");
            Token parameter = Consume(TokenType.Identifier, "Expected ifCase parameter.");
            Match(TokenType.Semicolon);
            Consume(TokenType.RightParen, "Expected ')' after ifCase parameter.");

            List<CaseClause> cases = new List<CaseClause>();
            Stmt? defaultBranch = null;

            SkipNewLines();
            while (Check(TokenType.Star) && IsNameLikeToken(PeekType(1)))
            {
                Consume(TokenType.Star, "Expected '*' in point case branch.");
                Token branchPoint = ConsumeNameLikeToken("Expected point case branch name.");
                if (!string.Equals(pointName.Lexeme, branchPoint.Lexeme, StringComparison.OrdinalIgnoreCase))
                {
                    throw ParseError(branchPoint, "Point case branch must use '*" + pointName.Lexeme + "'.");
                }

                Consume(TokenType.ShiftLeft, "Expected '<<' in point case branch.");

                bool isDefault = false;
                Expr? matchExpr = null;
                if (MatchIdentifierLexeme("outlier") || Match(TokenType.Default))
                {
                    isDefault = true;
                    Match(TokenType.Question);
                }
                else
                {
                    matchExpr = ParseExpression();
                }

                if (Match(TokenType.DoubleColon))
                {
                    Match(TokenType.Then);
                }

                Stmt body = ParseStatementBody();
                if (isDefault)
                {
                    defaultBranch = body;
                }
                else if (matchExpr != null)
                {
                    cases.Add(new CaseClause(matchExpr, body));
                }

                Match(TokenType.Semicolon);
                SkipNewLines();
            }

            Match(TokenType.Semicolon);
            Expr subject = new VariableExpr(parameter.Lexeme, parameter);
            Stmt dispatchBody = new IfCaseStmt(subject, cases, defaultBranch, line);
            return new AwaitPointStmt(pointName.Lexeme, parameter.Lexeme, dispatchBody, line);
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

        private bool MatchIdentifierLexeme(string text)
        {
            if (IsAtEnd())
            {
                return false;
            }

            if (Peek().Type != TokenType.Identifier)
            {
                return false;
            }

            if (!string.Equals(Peek().Lexeme, text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Advance();
            return true;
        }

        private bool CheckIdentifierLexeme(string text)
        {
            if (IsAtEnd())
            {
                return false;
            }

            if (Peek().Type != TokenType.Identifier)
            {
                return false;
            }

            return string.Equals(Peek().Lexeme, text, StringComparison.OrdinalIgnoreCase);
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd())
            {
                return false;
            }

            return Peek().Type == type;
        }

        private bool Check(TokenType type, int offset)
        {
            if (_current + offset >= _tokens.Count)
            {
                return false;
            }

            return _tokens[_current + offset].Type == type;
        }

        private static bool IsStatementTerminator(TokenType type)
        {
            return type == TokenType.Semicolon ||
                   type == TokenType.NewLine ||
                   type == TokenType.Eof;
        }

        private static bool ShouldInsertImportSpace(TokenType? previousType, TokenType currentType)
        {
            if (previousType == null)
            {
                return false;
            }

            return IsNameLikeToken(previousType.Value) && IsNameLikeToken(currentType);
        }

        private void SkipNewLines()
        {
            while (Match(TokenType.NewLine))
            {
            }
        }

        private void ConsumeOptionalStatementTerminator()
        {
            Match(TokenType.Semicolon);
            SkipNewLines();
        }

        private TokenType PeekType(int offset)
        {
            if (_current + offset >= _tokens.Count)
            {
                return TokenType.Eof;
            }

            return _tokens[_current + offset].Type;
        }

        private Token ConsumeNameLikeToken(string message)
        {
            if (IsNameLikeToken(Peek().Type))
            {
                return Advance();
            }

            throw ParseError(Peek(), message);
        }

        private static bool IsNameLikeToken(TokenType type)
        {
            if (type == TokenType.Identifier)
            {
                return true;
            }

            return type >= TokenType.If && type <= TokenType.Null;
        }

        private static bool TryUnpackYieldDispatch(Expr expression, out Expr value, out string pointName)
        {
            if (expression is GroupExpr grouped)
            {
                return TryUnpackYieldDispatch(grouped.Expression, out value, out pointName);
            }

            if (expression is BinaryExpr binary &&
                binary.Operator.Type == TokenType.ShiftRight &&
                TryExtractPointName(binary.Right, out string rightPointName))
            {
                value = binary.Left;
                pointName = rightPointName;
                return true;
            }

            if (expression is BinaryExpr binaryRightPoint &&
                binaryRightPoint.Operator.Type == TokenType.ShiftLeft &&
                TryExtractPointName(binaryRightPoint.Right, out string rightPointNameShiftLeft))
            {
                value = binaryRightPoint.Left;
                pointName = rightPointNameShiftLeft;
                return true;
            }

            if (expression is BinaryExpr binaryLeft &&
                binaryLeft.Operator.Type == TokenType.ShiftLeft &&
                TryExtractPointName(binaryLeft.Left, out string leftPointName))
            {
                value = binaryLeft.Right;
                pointName = leftPointName;
                return true;
            }

            if (expression is BinaryExpr binaryLeftPoint &&
                binaryLeftPoint.Operator.Type == TokenType.ShiftRight &&
                TryExtractPointName(binaryLeftPoint.Left, out string leftPointNameShiftRight))
            {
                value = binaryLeftPoint.Right;
                pointName = leftPointNameShiftRight;
                return true;
            }

            value = expression;
            pointName = string.Empty;
            return false;
        }

        private static bool TryExtractPointName(Expr pointExpr, out string pointName)
        {
            if (pointExpr is GroupExpr grouped)
            {
                return TryExtractPointName(grouped.Expression, out pointName);
            }

            if (pointExpr is PointRefExpr pointRef)
            {
                pointName = pointRef.PointName;
                return true;
            }

            if (pointExpr is VariableExpr variable)
            {
                pointName = variable.Name;
                return true;
            }

            pointName = string.Empty;
            return false;
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
            _errors.Add(new ParseError(token.Line, token.Column, token.Lexeme, message));
            return new InvalidOperationException("Parse error at " + token.Line + ":" + token.Column + " near '" + token.Lexeme + "': " + message);
        }
    }

    public sealed class ParseError
    {
        public int Line { get; }
        public int Column { get; }
        public string Lexeme { get; }
        public string Message { get; }

        public ParseError(int line, int column, string lexeme, string message)
        {
            Line = line;
            Column = column;
            Lexeme = lexeme;
            Message = message;
        }
    }
}
