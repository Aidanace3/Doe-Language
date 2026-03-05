using System;
using System.Collections.Generic;
using System.Globalization;

namespace Doe_Language
{
    public sealed class DebuggerSession
    {
        private readonly HashSet<int> _breakpoints;
        private bool _stepMode;

        public DebuggerSession(IEnumerable<int> breakpoints)
        {
            _breakpoints = new HashSet<int>(breakpoints);
        }

        public void BeforeExecute(Stmt stmt, RuntimeEnvironment env)
        {
            if (stmt.Line <= 0)
            {
                return;
            }

            if (!_stepMode && !_breakpoints.Contains(stmt.Line))
            {
                return;
            }

            while (true)
            {
                Console.Write("dbg(" + stmt.Line + ")> ");
                string input = (Console.ReadLine() ?? string.Empty).Trim();
                if (input.Length == 0)
                {
                    input = "s";
                }

                if (string.Equals(input, "s", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(input, "step", StringComparison.OrdinalIgnoreCase))
                {
                    _stepMode = true;
                    return;
                }

                if (string.Equals(input, "c", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(input, "continue", StringComparison.OrdinalIgnoreCase))
                {
                    _stepMode = false;
                    return;
                }

                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase))
                {
                    throw new OperationCanceledException("Debugger stopped execution.");
                }

                if (string.Equals(input, "locals", StringComparison.OrdinalIgnoreCase))
                {
                    PrintLocals(env);
                    continue;
                }

                if (input.StartsWith("p ", StringComparison.OrdinalIgnoreCase))
                {
                    string variable = input.Substring(2).Trim();
                    if (variable.Length == 0)
                    {
                        Console.WriteLine("Usage: p <variable>");
                        continue;
                    }

                    PrintVariable(env, variable);
                    continue;
                }

                if (input.StartsWith("b ", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = input.Substring(2).Trim();
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int line) || line <= 0)
                    {
                        Console.WriteLine("Usage: b <line>");
                        continue;
                    }

                    if (_breakpoints.Contains(line))
                    {
                        _breakpoints.Remove(line);
                        Console.WriteLine("Breakpoint removed at line " + line + ".");
                    }
                    else
                    {
                        _breakpoints.Add(line);
                        Console.WriteLine("Breakpoint added at line " + line + ".");
                    }

                    continue;
                }

                Console.WriteLine("Commands: s(step), c(continue), b <line>, p <var>, locals, q(quit)");
            }
        }

        private static void PrintVariable(RuntimeEnvironment env, string name)
        {
            if (!env.TryResolve(name, out object? value))
            {
                Console.WriteLine(name + " = <undefined>");
                return;
            }

            Console.WriteLine(name + " = " + FormatValue(value));
        }

        private static void PrintLocals(RuntimeEnvironment env)
        {
            Dictionary<string, object?> vars = env.SnapshotVisibleValues();
            if (vars.Count == 0)
            {
                Console.WriteLine("<no variables>");
                return;
            }

            foreach (KeyValuePair<string, object?> pair in vars)
            {
                Console.WriteLine(pair.Key + " = " + FormatValue(pair.Value));
            }
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is List<object?> list)
            {
                List<string> parts = new List<string>();
                for (int i = 0; i < list.Count; i++)
                {
                    parts.Add(FormatValue(list[i]));
                }

                return "[" + string.Join(", ", parts) + "]";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        }
    }
}
