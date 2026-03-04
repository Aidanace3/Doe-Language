using System;
using System.Collections.Generic;

namespace Doe_Language
{
    public abstract class Expr
    {
    }

    public sealed class LiteralExpr : Expr
    {
        public object? Value { get; }

        public LiteralExpr(object? value)
        {
            Value = value;
        }
    }

    public sealed class GroupExpr : Expr
    {
        public Expr Expression { get; }

        public GroupExpr(Expr expression)
        {
            Expression = expression;
        }
    }

    public sealed class VariableExpr : Expr
    {
        public string Name { get; }
        public Token NameToken { get; }

        public VariableExpr(string name, Token nameToken)
        {
            Name = name;
            NameToken = nameToken;
        }
    }

    public sealed class AssignExpr : Expr
    {
        public string Name { get; }
        public Expr Value { get; }
        public Token NameToken { get; }

        public AssignExpr(string name, Expr value, Token nameToken)
        {
            Name = name;
            Value = value;
            NameToken = nameToken;
        }
    }

    public sealed class UnaryExpr : Expr
    {
        public Token Operator { get; }
        public Expr Right { get; }

        public UnaryExpr(Token op, Expr right)
        {
            Operator = op;
            Right = right;
        }
    }

    public sealed class BinaryExpr : Expr
    {
        public Expr Left { get; }
        public Token Operator { get; }
        public Expr Right { get; }

        public BinaryExpr(Expr left, Token op, Expr right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public sealed class CallExpr : Expr
    {
        public Token Callee { get; }
        public List<Expr> Arguments { get; }

        public CallExpr(Token callee, List<Expr> arguments)
        {
            Callee = callee;
            Arguments = arguments;
        }
    }

    public abstract class Stmt
    {
    }

    public sealed class ImportStmt : Stmt
    {
        public string Module { get; }

        public ImportStmt(string module)
        {
            Module = module;
        }
    }

    public sealed class FunctionStmt : Stmt
    {
        public string Name { get; }
        public List<Stmt> Body { get; }

        public FunctionStmt(string name, List<Stmt> body)
        {
            Name = name;
            Body = body;
        }
    }

    public sealed class VarDeclStmt : Stmt
    {
        public string Name { get; }
        public bool IsConst { get; }
        public string? TypeHint { get; }
        public Expr? Initializer { get; }

        public VarDeclStmt(string name, bool isConst, string? typeHint, Expr? initializer)
        {
            Name = name;
            IsConst = isConst;
            TypeHint = typeHint;
            Initializer = initializer;
        }
    }

    public sealed class IfStmt : Stmt
    {
        public Expr Condition { get; }
        public Stmt ThenBranch { get; }
        public Stmt? ElseBranch { get; }

        public IfStmt(Expr condition, Stmt thenBranch, Stmt? elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public sealed class BlockStmt : Stmt
    {
        public List<Stmt> Statements { get; }

        public BlockStmt(List<Stmt> statements)
        {
            Statements = statements;
        }
    }

    public sealed class ExprStmt : Stmt
    {
        public Expr Expression { get; }

        public ExprStmt(Expr expression)
        {
            Expression = expression;
        }
    }

    public sealed class BreakStmt : Stmt
    {
    }
}
