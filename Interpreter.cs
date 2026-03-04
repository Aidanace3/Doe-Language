using System;
using System.Collections.Generic;
using System.Globalization;

namespace Doe_Language
{
    public sealed class RuntimeValue
    {
        public object? Value { get; set; }
        public bool IsConst { get; }
        public string? TypeHint { get; }

        public RuntimeValue(object? value, bool isConst, string? typeHint)
        {
            Value = value;
            IsConst = isConst;
            TypeHint = typeHint;
        }
    }

    public sealed class RuntimeEnvironment
    {
        private readonly RuntimeEnvironment? _parent;
        private readonly Dictionary<string, RuntimeValue> _values = new Dictionary<string, RuntimeValue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FunctionStmt> _functions = new Dictionary<string, FunctionStmt>(StringComparer.OrdinalIgnoreCase);

        public RuntimeEnvironment(RuntimeEnvironment? parent)
        {
            _parent = parent;
        }

        public void Define(string name, object? value, bool isConst, string? typeHint)
        {
            if (_values.ContainsKey(name))
            {
                throw new InvalidOperationException("Variable '" + name + "' already exists in this scope.");
            }

            ValidateType(typeHint, value, name);
            _values[name] = new RuntimeValue(value, isConst, typeHint);
        }

        public object? Get(string name, Token at)
        {
            if (_values.TryGetValue(name, out RuntimeValue? local))
            {
                return local.Value;
            }

            if (_parent != null)
            {
                return _parent.Get(name, at);
            }

            throw new InvalidOperationException("Undefined variable '" + name + "' at " + at.Line + ":" + at.Column + ".");
        }

        public void Assign(string name, object? value, Token at)
        {
            if (_values.TryGetValue(name, out RuntimeValue? local))
            {
                if (local.IsConst)
                {
                    throw new InvalidOperationException("Cannot assign to const variable '" + name + "' at " + at.Line + ":" + at.Column + ".");
                }

                ValidateType(local.TypeHint, value, name);
                local.Value = value;
                return;
            }

            if (_parent != null)
            {
                _parent.Assign(name, value, at);
                return;
            }

            throw new InvalidOperationException("Undefined variable '" + name + "' at " + at.Line + ":" + at.Column + ".");
        }

        public void DefineFunction(string name, FunctionStmt function)
        {
            _functions[name] = function;
        }

        public bool TryGetFunction(string name, out FunctionStmt? function)
        {
            if (_functions.TryGetValue(name, out FunctionStmt? local))
            {
                function = local;
                return true;
            }

            if (_parent != null)
            {
                return _parent.TryGetFunction(name, out function);
            }

            function = null;
            return false;
        }

        private static void ValidateType(string? typeHint, object? value, string name)
        {
            if (typeHint == null || value == null)
            {
                return;
            }

            string normalized = typeHint.ToLowerInvariant();
            if (normalized.Contains("int") && !(value is int))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Int.");
            }

            if (normalized.Contains("flt") && !(value is int || value is double))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Flt.");
            }

            if ((normalized.Contains("str") || normalized.Contains("string")) && !(value is string))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects String.");
            }
        }
    }

    public sealed class Interpreter
    {
        private readonly RuntimeEnvironment _globals = new RuntimeEnvironment(null);

        public void ExecuteProgram(List<Stmt> statements)
        {
            RuntimeEnvironment env = _globals;
            for (int i = 0; i < statements.Count; i++)
            {
                if (statements[i] is FunctionStmt function)
                {
                    _globals.DefineFunction(function.Name, function);
                    continue;
                }

                Execute(statements[i], env);
            }

            if (_globals.TryGetFunction("Main", out FunctionStmt? main) && main != null)
            {
                InvokeUserFunction(main, new List<object?>(), env);
            }
        }

        private object? Execute(Stmt stmt, RuntimeEnvironment env)
        {
            if (stmt is ImportStmt)
            {
                return null;
            }

            if (stmt is FunctionStmt fn)
            {
                env.DefineFunction(fn.Name, fn);
                return null;
            }

            if (stmt is VarDeclStmt varStmt)
            {
                object? value = varStmt.Initializer == null ? null : Evaluate(varStmt.Initializer, env);
                env.Define(varStmt.Name, value, varStmt.IsConst, varStmt.TypeHint);
                return null;
            }

            if (stmt is BlockStmt blockStmt)
            {
                ExecuteBlock(blockStmt.Statements, new RuntimeEnvironment(env));
                return null;
            }

            if (stmt is IfStmt ifStmt)
            {
                if (IsTruthy(Evaluate(ifStmt.Condition, env)))
                {
                    Execute(ifStmt.ThenBranch, env);
                }
                else if (ifStmt.ElseBranch != null)
                {
                    Execute(ifStmt.ElseBranch, env);
                }

                return null;
            }

            if (stmt is ExprStmt exprStmt)
            {
                return Evaluate(exprStmt.Expression, env);
            }

            if (stmt is BreakStmt)
            {
                return null;
            }

            throw new InvalidOperationException("Unknown statement type at runtime.");
        }

        private void ExecuteBlock(List<Stmt> statements, RuntimeEnvironment env)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                if (statements[i] is FunctionStmt fn)
                {
                    env.DefineFunction(fn.Name, fn);
                    continue;
                }

                Execute(statements[i], env);
            }
        }

        private object? Evaluate(Expr expr, RuntimeEnvironment env)
        {
            if (expr is LiteralExpr literal)
            {
                return literal.Value;
            }

            if (expr is GroupExpr group)
            {
                return Evaluate(group.Expression, env);
            }

            if (expr is VariableExpr variable)
            {
                return env.Get(variable.Name, variable.NameToken);
            }

            if (expr is AssignExpr assign)
            {
                object? value = Evaluate(assign.Value, env);
                env.Assign(assign.Name, value, assign.NameToken);
                return value;
            }

            if (expr is UnaryExpr unary)
            {
                object? right = Evaluate(unary.Right, env);
                if (unary.Operator.Type == TokenType.Minus)
                {
                    return -ToNumber(right, unary.Operator);
                }

                if (unary.Operator.Type == TokenType.Bang)
                {
                    return !IsTruthy(right);
                }

                throw new InvalidOperationException("Unsupported unary operator: " + unary.Operator.Lexeme);
            }

            if (expr is BinaryExpr binary)
            {
                object? left = Evaluate(binary.Left, env);
                object? right = Evaluate(binary.Right, env);

                switch (binary.Operator.Type)
                {
                    case TokenType.Plus:
                        if (left is string || right is string)
                        {
                            return ToDoeString(left) + ToDoeString(right);
                        }

                        return ToNumber(left, binary.Operator) + ToNumber(right, binary.Operator);
                    case TokenType.Minus:
                        return ToNumber(left, binary.Operator) - ToNumber(right, binary.Operator);
                    case TokenType.Star:
                        return ToNumber(left, binary.Operator) * ToNumber(right, binary.Operator);
                    case TokenType.Slash:
                        return ToNumber(left, binary.Operator) / ToNumber(right, binary.Operator);
                    case TokenType.Percent:
                        return ToNumber(left, binary.Operator) % ToNumber(right, binary.Operator);
                    case TokenType.Greater:
                        return ToNumber(left, binary.Operator) > ToNumber(right, binary.Operator);
                    case TokenType.GreaterEqual:
                    case TokenType.EqualGreater:
                        return ToNumber(left, binary.Operator) >= ToNumber(right, binary.Operator);
                    case TokenType.Less:
                        return ToNumber(left, binary.Operator) < ToNumber(right, binary.Operator);
                    case TokenType.LessEqual:
                        return ToNumber(left, binary.Operator) <= ToNumber(right, binary.Operator);
                    case TokenType.DoubleAmpersand:
                        return IsTruthy(left) && IsTruthy(right);
                    case TokenType.Pipe:
                        return IsTruthy(left) || IsTruthy(right);
                    case TokenType.BangPipe:
                        return !(IsTruthy(left) || IsTruthy(right));
                    case TokenType.BangAmpersand:
                        return !(IsTruthy(left) && IsTruthy(right));
                    case TokenType.StarPipe:
                        return IsTruthy(left) ^ IsTruthy(right);
                    default:
                        throw new InvalidOperationException("Unsupported binary operator: " + binary.Operator.Lexeme);
                }
            }

            if (expr is CallExpr call)
            {
                List<object?> args = new List<object?>();
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    args.Add(Evaluate(call.Arguments[i], env));
                }

                return InvokeFunction(call.Callee.Lexeme, call.Callee, args, env);
            }

            throw new InvalidOperationException("Unknown expression type at runtime.");
        }

        private object? InvokeFunction(string name, Token at, List<object?> args, RuntimeEnvironment env)
        {
            if (string.Equals(name, "Print", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count == 0)
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(ToDoeString(args[0]));
                }

                return null;
            }

            if (string.Equals(name, "Input", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "readln", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count > 0)
                {
                    Console.Write(ToDoeString(args[0]));
                }

                return Console.ReadLine();
            }

            if (env.TryGetFunction(name, out FunctionStmt? function) && function != null)
            {
                return InvokeUserFunction(function, args, env);
            }

            throw new InvalidOperationException("Unknown function '" + name + "' at " + at.Line + ":" + at.Column + ".");
        }

        private object? InvokeUserFunction(FunctionStmt function, List<object?> args, RuntimeEnvironment caller)
        {
            RuntimeEnvironment local = new RuntimeEnvironment(caller);
            ExecuteBlock(function.Body, local);
            return null;
        }

        private static bool IsTruthy(object? value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool b)
            {
                return b;
            }

            if (value is int i)
            {
                return i != 0;
            }

            if (value is double d)
            {
                return Math.Abs(d) > double.Epsilon;
            }

            if (value is string s)
            {
                return !string.IsNullOrEmpty(s);
            }

            return true;
        }

        private static double ToNumber(object? value, Token at)
        {
            if (value is int i)
            {
                return i;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is bool b)
            {
                return b ? 1 : 0;
            }

            if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException("Expected number near '" + at.Lexeme + "' at " + at.Line + ":" + at.Column + ".");
        }

        private static string ToDoeString(object? value)
        {
            return value == null ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        }
    }
}
