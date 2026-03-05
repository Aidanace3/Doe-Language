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

    public sealed class PointRefExpr : Expr
    {
        public string PointName { get; }
        public Token PointToken { get; }

        public PointRefExpr(string pointName, Token pointToken)
        {
            PointName = pointName;
            PointToken = pointToken;
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

    public sealed class ArrayLiteralExpr : Expr
    {
        public List<Expr> Elements { get; }

        public ArrayLiteralExpr(List<Expr> elements)
        {
            Elements = elements;
        }
    }

    public sealed class ArrayCtorExpr : Expr
    {
        public string? ElementType { get; }

        public ArrayCtorExpr(string? elementType)
        {
            ElementType = elementType;
        }
    }

    public sealed class IndexExpr : Expr
    {
        public Expr Target { get; }
        public Expr Index { get; }
        public Token BracketToken { get; }

        public IndexExpr(Expr target, Expr index, Token bracketToken)
        {
            Target = target;
            Index = index;
            BracketToken = bracketToken;
        }
    }

    public sealed class IndexAssignExpr : Expr
    {
        public Expr Target { get; }
        public Expr Index { get; }
        public Expr Value { get; }
        public Token AtToken { get; }

        public IndexAssignExpr(Expr target, Expr index, Expr value, Token atToken)
        {
            Target = target;
            Index = index;
            Value = value;
            AtToken = atToken;
        }
    }

    public abstract class Stmt
    {
        public int Line { get; }

        protected Stmt(int line)
        {
            Line = line;
        }
    }

    public sealed class ImportStmt : Stmt
    {
        public string Module { get; }

        public ImportStmt(string module, int line = 0) : base(line)
        {
            Module = module;
        }
    }

    public sealed class FunctionStmt : Stmt
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public List<Stmt> Body { get; }

        public FunctionStmt(string name, List<string> parameters, List<Stmt> body, int line = 0) : base(line)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    public sealed class VarDeclStmt : Stmt
    {
        public string Name { get; }
        public bool IsConst { get; }
        public string? TypeHint { get; }
        public Expr? Initializer { get; }

        public VarDeclStmt(string name, bool isConst, string? typeHint, Expr? initializer, int line = 0) : base(line)
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
        public Stmt? ConditionAction { get; }
        public Stmt ThenBranch { get; }
        public Stmt? ElseBranch { get; }

        public IfStmt(Expr condition, Stmt? conditionAction, Stmt thenBranch, Stmt? elseBranch, int line = 0) : base(line)
        {
            Condition = condition;
            ConditionAction = conditionAction;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public sealed class IfCaseStmt : Stmt
    {
        public Expr Subject { get; }
        public List<CaseClause> Cases { get; }
        public Stmt? DefaultBranch { get; }

        public IfCaseStmt(Expr subject, List<CaseClause> cases, Stmt? defaultBranch, int line = 0) : base(line)
        {
            Subject = subject;
            Cases = cases;
            DefaultBranch = defaultBranch;
        }
    }

    public sealed class AwaitPointStmt : Stmt
    {
        public string PointName { get; }
        public string ParameterName { get; }
        public Stmt Body { get; }

        public AwaitPointStmt(string pointName, string parameterName, Stmt body, int line = 0) : base(line)
        {
            PointName = pointName;
            ParameterName = parameterName;
            Body = body;
        }
    }

    public sealed class DictDeclStmt : Stmt
    {
        public string Name { get; }
        public bool IsLocked { get; }
        public string? LockedType { get; }
        public List<Stmt> Body { get; }

        public DictDeclStmt(string name, bool isLocked, string? lockedType, List<Stmt> body, int line = 0) : base(line)
        {
            Name = name;
            IsLocked = isLocked;
            LockedType = lockedType;
            Body = body;
        }
    }

    public sealed class CaseClause
    {
        public Expr Match { get; }
        public Stmt Body { get; }

        public CaseClause(Expr match, Stmt body)
        {
            Match = match;
            Body = body;
        }
    }

    public sealed class BlockStmt : Stmt
    {
        public List<Stmt> Statements { get; }

        public BlockStmt(List<Stmt> statements, int line = 0) : base(line)
        {
            Statements = statements;
        }
    }

    public sealed class ExprStmt : Stmt
    {
        public Expr Expression { get; }

        public ExprStmt(Expr expression, int line = 0) : base(line)
        {
            Expression = expression;
        }
    }

    public sealed class ReturnStmt : Stmt
    {
        public Expr? Value { get; }
        public Token ReturnToken { get; }

        public ReturnStmt(Expr? value, Token returnToken, int line = 0) : base(line)
        {
            Value = value;
            ReturnToken = returnToken;
        }
    }

    public sealed class YieldStmt : Stmt
    {
        public Expr Value { get; }
        public string PointName { get; }
        public string? AliasName { get; }
        public Token YieldToken { get; }

        public YieldStmt(Expr value, string pointName, string? aliasName, Token yieldToken, int line = 0) : base(line)
        {
            Value = value;
            PointName = pointName;
            AliasName = aliasName;
            YieldToken = yieldToken;
        }
    }

    public sealed class WhileStmt : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public WhileStmt(Expr condition, Stmt body, int line = 0) : base(line)
        {
            Condition = condition;
            Body = body;
        }
    }

    public sealed class EachStmt : Stmt
    {
        public string IteratorName { get; }
        public Expr Iterable { get; }
        public Stmt Body { get; }

        public EachStmt(string iteratorName, Expr iterable, Stmt body, int line = 0) : base(line)
        {
            IteratorName = iteratorName;
            Iterable = iterable;
            Body = body;
        }
    }

    public sealed class ConfStmt : Stmt
    {
        public string TargetName { get; }
        public string PropertyName { get; }
        public Expr Value { get; }
        public Token TargetToken { get; }

        public ConfStmt(string targetName, string propertyName, Expr value, Token targetToken, int line = 0) : base(line)
        {
            TargetName = targetName;
            PropertyName = propertyName;
            Value = value;
            TargetToken = targetToken;
        }
    }

    public sealed class BreakStmt : Stmt
    {
        public BreakStmt(int line = 0) : base(line)
        {
        }
    }
}
