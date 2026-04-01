using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Doe.PluginSdk;

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

    public sealed class RangeValue
    {
        public int Start { get; }
        public int End { get; }

        public RangeValue(int start, int end)
        {
            Start = start;
            End = end;
        }
    }

    public sealed class PointReferenceValue
    {
        public string? Name { get; }

        public PointReferenceValue(string? name)
        {
            Name = name;
        }
    }

    public sealed class PointAwaitHandler
    {
        public string ParameterName { get; }
        public Stmt Body { get; }
        public RuntimeEnvironment CapturedEnvironment { get; }

        public PointAwaitHandler(string parameterName, Stmt body, RuntimeEnvironment capturedEnvironment)
        {
            ParameterName = parameterName;
            Body = body;
            CapturedEnvironment = capturedEnvironment;
        }
    }

    internal sealed class ReturnSignal : Exception
    {
        public object? Value { get; }

        public ReturnSignal(object? value)
        {
            Value = value;
        }
    }

    internal sealed class BreakSignal : Exception
    {
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
                if (_parent.TryResolve(name, out _))
                {
                    _parent.Assign(name, value, at);
                    return;
                }

                _values[name] = new RuntimeValue(value, false, null);
                return;
            }

            _values[name] = new RuntimeValue(value, false, null);
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

        public bool TryResolve(string name, out object? value)
        {
            if (_values.TryGetValue(name, out RuntimeValue? local))
            {
                value = local.Value;
                return true;
            }

            if (_parent != null)
            {
                return _parent.TryResolve(name, out value);
            }

            value = null;
            return false;
        }

        public Dictionary<string, object?> SnapshotVisibleValues()
        {
            Dictionary<string, object?> values = _parent == null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : _parent.SnapshotVisibleValues();

            foreach (KeyValuePair<string, RuntimeValue> pair in _values)
            {
                values[pair.Key] = pair.Value.Value;
            }

            return values;
        }

        public Dictionary<string, object?> SnapshotLocalValues()
        {
            Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, RuntimeValue> pair in _values)
            {
                values[pair.Key] = pair.Value.Value;
            }

            return values;
        }

        private static void ValidateType(string? typeHint, object? value, string name)
        {
            if (typeHint == null || value == null)
            {
                return;
            }

            string normalized = typeHint.ToLowerInvariant();
            bool isArrayType = normalized.Contains("arr");
            if (isArrayType && !(value is List<object?>))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Arr.");
            }

            if (normalized.Contains("dict") && !(value is Dictionary<string, object?>))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Dict.");
            }

            if (!isArrayType && normalized.Contains("int") && !(value is int))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Int.");
            }

            if (!isArrayType && normalized.Contains("flt") && !(value is int || value is double))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects Flt.");
            }

            if (!isArrayType && (normalized.Contains("str") || normalized.Contains("string")) && !(value is string))
            {
                throw new InvalidOperationException("Variable '" + name + "' expects String.");
            }
        }
    }

    public sealed class Interpreter
    {
        private sealed class DoePluginRegistry : IDoePluginRegistry
        {
            private readonly Dictionary<string, DoePluginFunction> _functions;

            public DoePluginRegistry(Dictionary<string, DoePluginFunction> functions)
            {
                _functions = functions;
            }

            public void RegisterFunction(string name, DoePluginFunction handler)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException("Plugin function name cannot be empty.");
                }

                if (handler == null)
                {
                    throw new InvalidOperationException("Plugin function handler cannot be null.");
                }

                if (_functions.ContainsKey(name))
                {
                    throw new InvalidOperationException("Plugin function '" + name + "' is already registered.");
                }

                _functions[name] = handler;
            }
        }

        private readonly RuntimeEnvironment _globals = new RuntimeEnvironment(null);
        private readonly Dictionary<string, PointAwaitHandler> _points = new Dictionary<string, PointAwaitHandler>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _pointDispatchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DoePluginFunction> _pluginFunctions = new Dictionary<string, DoePluginFunction>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _pointContext = new Stack<string>();
        private readonly Stack<string> _modulePathStack = new Stack<string>();
        private readonly HashSet<string> _loadedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _loadedPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly DebuggerSession? _debugger;
        private readonly bool _silentOutput;
        private int _functionDepth;
        private int _loopDepth;

        public Interpreter(DebuggerSession? debugger = null, bool silentOutput = true)
        {
            _debugger = debugger;
            _silentOutput = silentOutput;
        }

        public void ExecuteProgram(List<Stmt> statements, string? sourcePath = null)
        {
            ExecuteModule(statements, _globals, sourcePath, true);
            WarnUncalledPoints();
        }

        private object? Execute(Stmt stmt, RuntimeEnvironment env)
        {
            _debugger?.BeforeExecute(stmt, env);

            if (stmt is ImportStmt)
            {
                ImportModules(((ImportStmt)stmt).Module, env, stmt.Line);
                return null;
            }

            if (stmt is FunctionStmt fn)
            {
                env.DefineFunction(fn.Name, fn);
                return null;
            }

            if (stmt is DictDeclStmt dictStmt)
            {
                RuntimeEnvironment dictScope = new RuntimeEnvironment(env);
                ExecuteBlock(dictStmt.Body, dictScope);
                Dictionary<string, object?> values = dictScope.SnapshotLocalValues();

                if (dictStmt.IsLocked && !string.IsNullOrWhiteSpace(dictStmt.LockedType))
                {
                    ValidateDictionaryValues(values, dictStmt.LockedType, dictStmt.Name);
                }

                env.Define(dictStmt.Name, values, false, "Dict");
                return null;
            }

            if (stmt is VarDeclStmt varStmt)
            {
                object? value = varStmt.Initializer == null ? null : Evaluate(varStmt.Initializer, env);
                if (value == null && varStmt.TypeHint != null && varStmt.TypeHint.IndexOf("arr", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    value = new List<object?>();
                }

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
                    if (ifStmt.ConditionAction != null)
                    {
                        Execute(ifStmt.ConditionAction, env);
                    }

                    Execute(ifStmt.ThenBranch, env);
                }
                else if (ifStmt.ElseBranch != null)
                {
                    Execute(ifStmt.ElseBranch, env);
                }

                return null;
            }

            if (stmt is IfCaseStmt ifCaseStmt)
            {
                object? subject = Evaluate(ifCaseStmt.Subject, env);
                for (int i = 0; i < ifCaseStmt.Cases.Count; i++)
                {
                    object? candidate = Evaluate(ifCaseStmt.Cases[i].Match, env);
                    if (MatchesCase(subject, candidate))
                    {
                        Execute(ifCaseStmt.Cases[i].Body, env);
                        return null;
                    }
                }

                if (ifCaseStmt.DefaultBranch != null)
                {
                    Execute(ifCaseStmt.DefaultBranch, env);
                }

                return null;
            }

            if (stmt is AwaitPointStmt awaitPointStmt)
            {
                _points[awaitPointStmt.PointName] = new PointAwaitHandler(awaitPointStmt.ParameterName, awaitPointStmt.Body, env);
                if (!_pointDispatchCounts.ContainsKey(awaitPointStmt.PointName))
                {
                    _pointDispatchCounts[awaitPointStmt.PointName] = 0;
                }

                return null;
            }

            if (stmt is WhileStmt whileStmt)
            {
                _loopDepth++;
                try
                {
                    while (IsTruthy(Evaluate(whileStmt.Condition, env)))
                    {
                        try
                        {
                            Execute(whileStmt.Body, env);
                        }
                        catch (BreakSignal)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _loopDepth--;
                }

                return null;
            }

            if (stmt is EachStmt eachStmt)
            {
                object? iterable = Evaluate(eachStmt.Iterable, env);
                IEnumerable<object?> values = NormalizeIterable(eachStmt, iterable);

                _loopDepth++;
                try
                {
                    foreach (object? item in values)
                    {
                        RuntimeEnvironment loopScope = new RuntimeEnvironment(env);
                        loopScope.Define(eachStmt.IteratorName, item, false, null);
                        try
                        {
                            Execute(eachStmt.Body, loopScope);
                        }
                        catch (BreakSignal)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _loopDepth--;
                }

                return null;
            }

            if (stmt is ExprStmt exprStmt)
            {
                return Evaluate(exprStmt.Expression, env);
            }

            if (stmt is ConfigDeclStmt configStmt)
            {
                RuntimeEnvironment configScope = new RuntimeEnvironment(env);
                ExecuteBlock(configStmt.Body, configScope);
                Dictionary<string, object?> values = configScope.SnapshotLocalValues();
                env.Define(configStmt.Name, values, false, "Dict");
                return null;
            }

            if (stmt is ReturnStmt returnStmt)
            {
                if (_functionDepth <= 0)
                {
                    throw new InvalidOperationException("return can only be used inside a function at " + returnStmt.ReturnToken.Line + ":" + returnStmt.ReturnToken.Column + ".");
                }

                object? returnValue = returnStmt.Value == null ? null : Evaluate(returnStmt.Value, env);
                throw new ReturnSignal(returnValue);
            }

            if (stmt is YieldStmt yieldStmt)
            {
                object? value = Evaluate(yieldStmt.Value, env);
                string pointName = yieldStmt.PointName;
                if (string.Equals(pointName, "this", StringComparison.OrdinalIgnoreCase))
                {
                    string? currentPoint = GetCurrentPointName();
                    if (string.IsNullOrWhiteSpace(currentPoint))
                    {
                        throw new InvalidOperationException("Point reference 'this' is only valid while running inside a point handler at " + yieldStmt.YieldToken.Line + ":" + yieldStmt.YieldToken.Column + ".");
                    }

                    pointName = currentPoint;
                }

                DispatchPoint(pointName, value, yieldStmt.YieldToken, yieldStmt.AliasName);
                return value;
            }

            if (stmt is BreakStmt)
            {
                if (_loopDepth <= 0)
                {
                    throw new InvalidOperationException("break can only be used inside a loop.");
                }

                throw new BreakSignal();
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

        private void ExecuteModule(List<Stmt> statements, RuntimeEnvironment env, string? sourcePath, bool invokeMain)
        {
            string? normalizedSourcePath = string.IsNullOrWhiteSpace(sourcePath)
                ? null
                : Path.GetFullPath(sourcePath);

            if (!string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                _modulePathStack.Push(normalizedSourcePath);
            }

            try
            {
                ExecuteBlock(statements, env);

                if (invokeMain && _globals.TryGetFunction("Main", out FunctionStmt? main) && main != null)
                {
                    InvokeUserFunction(main, new List<object?>(), env);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(normalizedSourcePath))
                {
                    _modulePathStack.Pop();
                }
            }
        }

        private void ImportModules(string rawModuleSpec, RuntimeEnvironment env, int line)
        {
            string[] moduleSpecs = rawModuleSpec.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < moduleSpecs.Length; i++)
            {
                string moduleSpec = moduleSpecs[i].Trim();
                if (moduleSpec.Length == 0)
                {
                    continue;
                }

                ImportSingleModule(moduleSpec, env, line);
            }
        }

        private void ImportSingleModule(string rawModuleSpec, RuntimeEnvironment env, int line)
        {
            string moduleSpec = NormalizeImportSpec(rawModuleSpec);
            if (IsPluginImportSpec(moduleSpec) || IsDllImportSpec(moduleSpec))
            {
                string pluginModuleSpec = IsPluginImportSpec(moduleSpec)
                    ? moduleSpec
                    : "plugin:" + moduleSpec;
                LoadPluginAssembly(pluginModuleSpec, line);
                return;
            }

            string fullPath = ResolveImportPath(moduleSpec, line);
            if (_loadedModules.Contains(fullPath))
            {
                return;
            }

            _loadedModules.Add(fullPath);
            try
            {
                string source = File.ReadAllText(fullPath);
                Lexer lexer = new Lexer(source);
                IReadOnlyList<Token> tokens = lexer.Tokenize();
                Parser parser = new Parser(tokens);
                List<Stmt> statements = parser.ParseProgram();

                if (parser.Errors.Count > 0)
                {
                    throw new InvalidOperationException("Imported module '" + fullPath + "' contains parse errors.");
                }

                ExecuteModule(statements, env, fullPath, false);
            }
            catch
            {
                _loadedModules.Remove(fullPath);
                throw;
            }
        }

        private void LoadPluginAssembly(string moduleSpec, int line)
        {
            string pluginSpec = NormalizePluginSpec(moduleSpec);
            string assemblyPath = ResolvePluginPath(pluginSpec, line);
            if (_loadedPlugins.Contains(assemblyPath))
            {
                return;
            }

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            if (TryRegisterConventionPlugin(assembly))
            {
                _loadedPlugins.Add(assemblyPath);
                return;
            }

            Type[] pluginTypes = GetSdkPluginTypes(assembly);

            if (pluginTypes.Length == 0)
            {
                throw new InvalidOperationException("Plugin assembly '" + assemblyPath + "' does not contain an IDoePlugin implementation.");
            }

            DoePluginRegistry registry = new DoePluginRegistry(_pluginFunctions);
            for (int i = 0; i < pluginTypes.Length; i++)
            {
                IDoePlugin? plugin = Activator.CreateInstance(pluginTypes[i]) as IDoePlugin;
                if (plugin == null)
                {
                    throw new InvalidOperationException("Unable to create plugin type '" + pluginTypes[i].FullName + "'.");
                }

                plugin.Register(registry);
            }

            _loadedPlugins.Add(assemblyPath);
        }

        private bool TryRegisterConventionPlugin(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            string[] candidateTypeNames =
            {
                assemblyName + ".PluginFunctions",
                assemblyName + ".PluginExports",
                "PluginFunctions",
                "PluginExports"
            };

            for (int i = 0; i < candidateTypeNames.Length; i++)
            {
                Type? type = assembly.GetType(candidateTypeNames[i], throwOnError: false, ignoreCase: false);
                if (type == null)
                {
                    continue;
                }

                RegisterConventionMethods(type);
                return true;
            }

            return false;
        }

        private void RegisterConventionMethods(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            int registeredCount = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.IsSpecialName)
                {
                    continue;
                }

                string name = method.Name;
                if (_pluginFunctions.ContainsKey(name))
                {
                    throw new InvalidOperationException("Plugin function '" + name + "' is already registered.");
                }

                _pluginFunctions[name] = args => InvokeConventionPluginMethod(method, args);
                registeredCount++;
            }

            if (registeredCount == 0)
            {
                throw new InvalidOperationException("Convention plugin type '" + type.FullName + "' does not define any public static methods.");
            }
        }

        private static Type[] GetSdkPluginTypes(Assembly assembly)
        {
            try
            {
                return assembly
                    .GetTypes()
                    .Where(static type => !type.IsAbstract && typeof(IDoePlugin).IsAssignableFrom(type))
                    .ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(static type => type != null && !type.IsAbstract && typeof(IDoePlugin).IsAssignableFrom(type))
                    .Cast<Type>()
                    .ToArray();
            }
        }

        private static object? InvokeConventionPluginMethod(MethodInfo method, IReadOnlyList<object?> args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object?[] callArgs = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Count)
                {
                    callArgs[i] = ConvertPluginArgument(args[i], parameters[i].ParameterType);
                    continue;
                }

                if (parameters[i].HasDefaultValue)
                {
                    callArgs[i] = parameters[i].DefaultValue;
                    continue;
                }

                throw new InvalidOperationException("Plugin method '" + method.Name + "' expects " + parameters.Length + " arguments.");
            }

            return method.Invoke(null, callArgs);
        }

        private static object? ConvertPluginArgument(object? value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            Type effectiveTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effectiveTarget.IsInstanceOfType(value))
            {
                return value;
            }

            if (effectiveTarget == typeof(string))
            {
                return ToDoeString(value);
            }

            if (effectiveTarget == typeof(int))
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }

            if (effectiveTarget == typeof(double))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            if (effectiveTarget == typeof(bool))
            {
                if (value is bool b)
                {
                    return b;
                }

                if (value is string s && bool.TryParse(s, out bool parsedBool))
                {
                    return parsedBool;
                }

                return Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0;
            }

            return Convert.ChangeType(value, effectiveTarget, CultureInfo.InvariantCulture);
        }

        private string ResolveImportPath(string moduleSpec, int line)
        {
            List<string> candidates = new List<string>();
            AddImportCandidates(candidates, moduleSpec);

            string dottedCandidate = moduleSpec.Replace('.', Path.DirectorySeparatorChar);
            if (!string.Equals(dottedCandidate, moduleSpec, StringComparison.Ordinal))
            {
                AddImportCandidates(candidates, dottedCandidate);
            }

            List<string> searchRoots = BuildImportRoots();
            for (int i = 0; i < searchRoots.Count; i++)
            {
                string root = searchRoots[i];
                for (int c = 0; c < candidates.Count; c++)
                {
                    string candidate = Path.IsPathRooted(candidates[c])
                        ? candidates[c]
                        : Path.Combine(root, candidates[c]);

                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            throw new InvalidOperationException("Unable to resolve import '" + moduleSpec + "' at line " + line + ".");
        }

        private string ResolvePluginPath(string pluginSpec, int line)
        {
            List<string> candidates = new List<string>();
            AddPluginCandidates(candidates, pluginSpec);

            List<string> roots = BuildPluginRoots();
            for (int i = 0; i < roots.Count; i++)
            {
                for (int c = 0; c < candidates.Count; c++)
                {
                    string candidate = Path.IsPathRooted(candidates[c])
                        ? candidates[c]
                        : Path.Combine(roots[i], candidates[c]);

                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            throw new InvalidOperationException("Unable to resolve plugin '" + pluginSpec + "' at line " + line + ".");
        }

        private List<string> BuildImportRoots()
        {
            List<string> roots = new List<string>();
            string currentDirectory = Directory.GetCurrentDirectory();
            AddImportRoot(roots, currentDirectory);
            AddImportRoot(roots, Path.Combine(currentDirectory, "lib"));
            AddImportRoot(roots, Path.Combine(currentDirectory, "libs"));
            AddImportRoot(roots, Path.Combine(currentDirectory, "library"));
            AddImportRoot(roots, Path.Combine(currentDirectory, "libraries"));

            if (_modulePathStack.Count > 0)
            {
                string? currentModuleDir = Path.GetDirectoryName(_modulePathStack.Peek());
                if (!string.IsNullOrWhiteSpace(currentModuleDir))
                {
                    AddImportRoot(roots, currentModuleDir);
                    AddImportRoot(roots, Path.Combine(currentModuleDir, "lib"));
                    AddImportRoot(roots, Path.Combine(currentModuleDir, "libs"));
                    AddImportRoot(roots, Path.Combine(currentModuleDir, "library"));
                    AddImportRoot(roots, Path.Combine(currentModuleDir, "libraries"));
                }
            }

            return roots;
        }

        private List<string> BuildPluginRoots()
        {
            List<string> roots = BuildImportRoots();
            AddImportRoot(roots, AppContext.BaseDirectory);
            AddImportRoot(roots, Path.Combine(AppContext.BaseDirectory, "plugins"));
            AddImportRoot(roots, Path.Combine(AppContext.BaseDirectory, "plugin"));

            string[] baseRoots = roots.ToArray();
            for (int i = 0; i < baseRoots.Length; i++)
            {
                AddImportRoot(roots, Path.Combine(baseRoots[i], "plugins"));
                AddImportRoot(roots, Path.Combine(baseRoots[i], "plugin"));
            }

            AddEnvironmentRoots(roots, "DOE_PLUGIN_PATH");
            AddEnvironmentRoots(roots, "PATH");

            return roots;
        }

        private static void AddImportRoot(List<string> roots, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            for (int i = 0; i < roots.Count; i++)
            {
                if (string.Equals(roots[i], fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            roots.Add(fullPath);
        }

        private static void AddEnvironmentRoots(List<string> roots, string variableName)
        {
            string? value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string[] parts = value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                AddImportRoot(roots, parts[i].Trim());
            }
        }

        private static void AddImportCandidates(List<string> candidates, string moduleSpec)
        {
            if (string.IsNullOrWhiteSpace(moduleSpec))
            {
                return;
            }

            AddImportCandidate(candidates, moduleSpec);

            if (!Path.HasExtension(moduleSpec))
            {
                AddImportCandidate(candidates, moduleSpec + ".doe");
                AddImportCandidate(candidates, moduleSpec + ".dough");
                AddImportCandidate(candidates, Path.Combine(moduleSpec, "index.doe"));
                AddImportCandidate(candidates, Path.Combine(moduleSpec, "index.dough"));
            }
        }

        private static void AddPluginCandidates(List<string> candidates, string pluginSpec)
        {
            if (string.IsNullOrWhiteSpace(pluginSpec))
            {
                return;
            }

            AddImportCandidate(candidates, pluginSpec);
            if (!pluginSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                AddImportCandidate(candidates, pluginSpec + ".dll");
                AddImportCandidate(candidates, Path.Combine(pluginSpec, pluginSpec + ".dll"));
            }

            string dottedPath = pluginSpec.Replace('.', Path.DirectorySeparatorChar);
            if (!string.Equals(dottedPath, pluginSpec, StringComparison.Ordinal))
            {
                AddImportCandidate(candidates, dottedPath + ".dll");
                AddImportCandidate(candidates, Path.Combine(dottedPath, pluginSpec + ".dll"));
            }
        }

        private static void AddImportCandidate(List<string> candidates, string candidate)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            candidates.Add(candidate);
        }

        private static string NormalizeImportSpec(string rawModuleSpec)
        {
            string moduleSpec = rawModuleSpec.Trim();
            moduleSpec = Regex.Replace(moduleSpec, @"\s*->\s*", "->");
            moduleSpec = Regex.Replace(moduleSpec, @"\s*([.:/\\])\s*", "$1");
            int aliasSeparator = moduleSpec.IndexOf("->", StringComparison.Ordinal);
            if (aliasSeparator >= 0)
            {
                moduleSpec = moduleSpec.Substring(0, aliasSeparator).Trim();
            }

            if (moduleSpec.Length >= 2 &&
                ((moduleSpec[0] == '"' && moduleSpec[moduleSpec.Length - 1] == '"') ||
                 (moduleSpec[0] == '\'' && moduleSpec[moduleSpec.Length - 1] == '\'')))
            {
                moduleSpec = moduleSpec.Substring(1, moduleSpec.Length - 2);
            }

            return moduleSpec;
        }

        private static bool IsPluginImportSpec(string moduleSpec)
        {
            return moduleSpec.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDllImportSpec(string moduleSpec)
        {
            return moduleSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePluginSpec(string moduleSpec)
        {
            return moduleSpec.Substring("plugin:".Length).Trim();
        }

        private object? Evaluate(Expr expr, RuntimeEnvironment env)
        {
            if (expr is ArrayCtorExpr)
            {
                return new List<object?>();
            }

            if (expr is ArrayLiteralExpr arrayLiteral)
            {
                List<object?> values = new List<object?>();
                for (int i = 0; i < arrayLiteral.Elements.Count; i++)
                {
                    values.Add(Evaluate(arrayLiteral.Elements[i], env));
                }

                return values;
            }

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
                if (env.TryResolve(variable.Name, out object? resolved))
                {
                    return resolved;
                }

                if (string.Equals(variable.Name, "this", StringComparison.OrdinalIgnoreCase))
                {
                    return new PointReferenceValue(GetCurrentPointName());
                }

                if (_points.ContainsKey(variable.Name))
                {
                    return new PointReferenceValue(variable.Name);
                }

                throw new InvalidOperationException("Undefined variable '" + variable.Name + "' at " + variable.NameToken.Line + ":" + variable.NameToken.Column + ".");
            }

            if (expr is PointRefExpr pointRefExpr)
            {
                if (string.Equals(pointRefExpr.PointName, "this", StringComparison.OrdinalIgnoreCase))
                {
                    return new PointReferenceValue(GetCurrentPointName());
                }

                return new PointReferenceValue(pointRefExpr.PointName);
            }

            if (expr is AssignExpr assign)
            {
                object? value = Evaluate(assign.Value, env);
                env.Assign(assign.Name, value, assign.NameToken);
                return value;
            }

            if (expr is IndexExpr indexExpr)
            {
                object? target = Evaluate(indexExpr.Target, env);
                object? indexValue = Evaluate(indexExpr.Index, env);
                return ReadIndexedValue(target, indexValue, indexExpr.BracketToken);
            }

            if (expr is IndexAssignExpr indexAssignExpr)
            {
                object? value = Evaluate(indexAssignExpr.Value, env);
                AssignIndexedValue(indexAssignExpr.Target, indexAssignExpr.Index, value, env, indexAssignExpr.AtToken);
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
                    case TokenType.DoubleStar:
                    case TokenType.Caret:
                        return Math.Pow(ToNumber(left, binary.Operator), ToNumber(right, binary.Operator));
                    case TokenType.Slash:
                        return ToNumber(left, binary.Operator) / ToNumber(right, binary.Operator);
                    case TokenType.Percent:
                        return (ToNumber(left, binary.Operator) / 100.0) * ToNumber(right, binary.Operator);
                    case TokenType.DoublePercent:
                        return ToNumber(left, binary.Operator) % ToNumber(right, binary.Operator);
                    case TokenType.DotDot:
                        return new RangeValue(ToDoeIndexNumber(left, binary.Operator), ToDoeIndexNumber(right, binary.Operator));
                    case TokenType.ShiftRight:
                        if (right is PointReferenceValue rightPoint)
                        {
                            DispatchPointIfPresent(rightPoint, left, binary.Operator);
                            return left;
                        }

                        if (left is PointReferenceValue leftPointAsShiftRight)
                        {
                            DispatchPointIfPresent(leftPointAsShiftRight, right, binary.Operator);
                            return right;
                        }

                        return (int)ToNumber(left, binary.Operator) >> (int)ToNumber(right, binary.Operator);
                    case TokenType.ShiftLeft:
                        if (left is PointReferenceValue leftPoint)
                        {
                            DispatchPointIfPresent(leftPoint, right, binary.Operator);
                            return right;
                        }

                        if (right is PointReferenceValue rightPointAsShiftLeft)
                        {
                            DispatchPointIfPresent(rightPointAsShiftLeft, left, binary.Operator);
                            return left;
                        }

                        return (int)ToNumber(left, binary.Operator) << (int)ToNumber(right, binary.Operator);
                    case TokenType.EqualEqual:
                        return AreEqual(left, right);
                    case TokenType.TripleEqual:
                        return AreStrictlyEqual(left, right);
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
                if (_silentOutput)
                {
                    return null;
                }

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
                if (_silentOutput)
                {
                    return string.Empty;
                }

                if (args.Count > 0)
                {
                    Console.Write(ToDoeString(args[0]));
                }

                return Console.ReadLine();
            }

            if (string.Equals(name, "Max", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeMax(args, at);
            }

            if (string.Equals(name, "Min", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeMin(args, at);
            }

            if (string.Equals(name, "map", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeMap(args, at);
            }

            if (string.Equals(name, "exit", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count == 0)
                {
                    throw new InvalidOperationException("exit requires a point argument.");
                }

                PointReferenceValue? point = args[0] as PointReferenceValue;
                if (point == null || string.IsNullOrWhiteSpace(point.Name))
                {
                    throw new InvalidOperationException("exit expects a point reference like exit(*PointName).");
                }

                _points.Remove(point.Name);
                return null;
            }

            if (string.Equals(name, "debug", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "breakpoint", StringComparison.OrdinalIgnoreCase))
            {
                if (_debugger == null)
                {
                    throw new InvalidOperationException("debug() requires runtime debug mode. Run with --debug.");
                }

                _debugger.BreakNow(env, at.Line);
                return null;
            }

            if (string.Equals(name, "yield", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "yeild", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeYield(args, at);
            }

            if (env.TryGetFunction(name, out FunctionStmt? function) && function != null)
            {
                return InvokeUserFunction(function, args, env);
            }

            if (_pluginFunctions.TryGetValue(name, out DoePluginFunction? pluginFunction))
            {
                return pluginFunction(args);
            }

            throw new InvalidOperationException("Unknown function '" + name + "' at " + at.Line + ":" + at.Column + ".");
        }

        private object? InvokeUserFunction(FunctionStmt function, List<object?> args, RuntimeEnvironment caller)
        {
            RuntimeEnvironment local = new RuntimeEnvironment(caller);
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                object? value = i < args.Count ? args[i] : null;
                local.Define(function.Parameters[i], value, false, null);
            }

            _functionDepth++;
            try
            {
                ExecuteBlock(function.Body, local);
                return null;
            }
            catch (ReturnSignal signal)
            {
                return signal.Value;
            }
            finally
            {
                _functionDepth--;
            }
        }

        private void DispatchPointIfPresent(PointReferenceValue pointRef, object? value, Token at)
        {
            if (string.IsNullOrWhiteSpace(pointRef.Name))
            {
                throw new InvalidOperationException("Point reference 'this' is only valid while running inside a point handler at " + at.Line + ":" + at.Column + ".");
            }

            DispatchPoint(pointRef.Name, value, at, null);
        }

        private void DispatchPoint(string pointName, object? value, Token at, string? aliasName = null)
        {
            if (!_points.TryGetValue(pointName, out PointAwaitHandler? handler))
            {
                throw new InvalidOperationException("Point '*" + pointName + "' is not registered at " + at.Line + ":" + at.Column + ".");
            }

            _pointDispatchCounts[pointName] = _pointDispatchCounts.TryGetValue(pointName, out int count) ? count + 1 : 1;

            RuntimeEnvironment pointScope = new RuntimeEnvironment(handler.CapturedEnvironment);
            pointScope.Define(handler.ParameterName, value, false, null);
            if (!string.IsNullOrWhiteSpace(aliasName) &&
                !string.Equals(aliasName, handler.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                pointScope.Define(aliasName, value, false, null);
            }

            _pointContext.Push(pointName);
            try
            {
                Execute(handler.Body, pointScope);
            }
            finally
            {
                _pointContext.Pop();
            }
        }

        private void WarnUncalledPoints()
        {
            foreach (KeyValuePair<string, PointAwaitHandler> entry in _points)
            {
                if (_pointDispatchCounts.TryGetValue(entry.Key, out int count) && count > 0)
                {
                    continue;
                }

                Console.Error.WriteLine("Doe warning: Point '*" + entry.Key + "' was declared but never called.");
            }
        }

        private object? InvokeYield(List<object?> args, Token at)
        {
            if (args.Count == 0)
            {
                throw new InvalidOperationException("yield/yeild requires a point argument.");
            }

            PointReferenceValue? pointRef = null;
            object? value = null;

            if (args.Count == 1)
            {
                pointRef = args[0] as PointReferenceValue;
                if (pointRef == null || string.IsNullOrWhiteSpace(pointRef.Name))
                {
                    // Parser may already have dispatched when expression used shift syntax.
                    return args[0];
                }
            }
            else
            {
                PointReferenceValue? first = args[0] as PointReferenceValue;
                PointReferenceValue? second = args[1] as PointReferenceValue;
                if (first != null && !string.IsNullOrWhiteSpace(first.Name))
                {
                    pointRef = first;
                    value = args[1];
                }
                else if (second != null && !string.IsNullOrWhiteSpace(second.Name))
                {
                    pointRef = second;
                    value = args[0];
                }
            }

            if (pointRef == null || string.IsNullOrWhiteSpace(pointRef.Name))
            {
                throw new InvalidOperationException("yield/yeild expects one point reference and optional value.");
            }

            DispatchPoint(pointRef.Name, value, at, null);
            return value;
        }

        private static IEnumerable<object?> NormalizeIterable(EachStmt eachStmt, object? iterable)
        {
            if (iterable is List<object?> list)
            {
                return list;
            }

            if (iterable is Dictionary<string, object?> dict)
            {
                return dict.Values;
            }

            throw new InvalidOperationException("each loop expects Arr or Dict iterable at line " + eachStmt.Line + ".");
        }

        private string? GetCurrentPointName()
        {
            return _pointContext.Count == 0 ? null : _pointContext.Peek();
        }

        private static void ValidateDictionaryValues(Dictionary<string, object?> dict, string typeName, string dictName)
        {
            string normalized = typeName.ToLowerInvariant();
            foreach (KeyValuePair<string, object?> entry in dict)
            {
                object? value = entry.Value;
                if (value == null)
                {
                    continue;
                }

                bool ok = normalized switch
                {
                    "int" => value is int,
                    "flt" => value is int || value is double,
                    "str" => value is string,
                    "string" => value is string,
                    "arr" => value is List<object?>,
                    "dict" => value is Dictionary<string, object?>,
                    _ => true
                };

                if (!ok)
                {
                    throw new InvalidOperationException("Locked dict '" + dictName + "' expected values of type '" + typeName + "' for key '" + entry.Key + "'.");
                }
            }
        }

        private static bool AreEqual(object? left, object? right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left is int || left is double || right is int || right is double)
            {
                return Math.Abs(ToRawNumber(left) - ToRawNumber(right)) < 0.0000001;
            }

            return Equals(left, right);
        }

        private static bool AreStrictlyEqual(object? left, object? right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            if (left.GetType() != right.GetType())
            {
                return false;
            }

            return Equals(left, right);
        }

        private static bool MatchesCase(object? subject, object? candidate)
        {
            if (candidate is List<object?> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (AreEqual(subject, list[i]))
                    {
                        return true;
                    }
                }

                return false;
            }

            return AreEqual(subject, candidate);
        }

        private object? ReadIndexedValue(object? target, object? indexValue, Token at)
        {
            if (target is Dictionary<string, object?> dict)
            {
                string key = ToDoeString(indexValue);
                return dict.TryGetValue(key, out object? value) ? value : null;
            }

            List<object?> list = target as List<object?> ??
                                 throw new InvalidOperationException("Indexing requires an Arr value at " + at.Line + ":" + at.Column + ".");

            if (indexValue is RangeValue range)
            {
                List<object?> slice = new List<object?>();
                int start = range.Start;
                int end = range.End;
                if (end < start)
                {
                    int temp = start;
                    start = end;
                    end = temp;
                }

                for (int i = start; i <= end; i++)
                {
                    slice.Add(GetArrayAtDoeIndex(list, i, at));
                }

                return slice;
            }

            int doeIndex = ToDoeIndexNumber(indexValue, at);
            return GetArrayAtDoeIndex(list, doeIndex, at);
        }

        private void AssignIndexedValue(Expr targetExpr, Expr indexExpr, object? value, RuntimeEnvironment env, Token at)
        {
            object? indexValue = Evaluate(indexExpr, env);
            List<object?>? list = null;
            Dictionary<string, object?>? dict = null;

            if (targetExpr is VariableExpr variableTarget)
            {
                object? current = env.Get(variableTarget.Name, variableTarget.NameToken);
                if (current == null)
                {
                    list = new List<object?>();
                    env.Assign(variableTarget.Name, list, variableTarget.NameToken);
                }
                else
                {
                    list = current as List<object?>;
                    dict = current as Dictionary<string, object?>;
                    if (list == null && dict == null)
                    {
                        throw new InvalidOperationException("Variable '" + variableTarget.Name + "' is not an Arr or Dict.");
                    }
                }
            }
            else
            {
                object? targetValue = Evaluate(targetExpr, env);
                list = targetValue as List<object?>;
                dict = targetValue as Dictionary<string, object?>;
                if (list == null && dict == null)
                {
                    throw new InvalidOperationException("Index assignment requires an Arr or Dict value at " + at.Line + ":" + at.Column + ".");
                }
            }

            if (dict != null)
            {
                if (indexValue is RangeValue)
                {
                    throw new InvalidOperationException("Range index assignment is not supported for Dict at " + at.Line + ":" + at.Column + ".");
                }

                dict[ToDoeString(indexValue)] = value;
                return;
            }

            if (list == null)
            {
                throw new InvalidOperationException("Index assignment requires an Arr value at " + at.Line + ":" + at.Column + ".");
            }

            if (indexValue is RangeValue range)
            {
                int start = range.Start;
                int end = range.End;
                if (end < start)
                {
                    int temp = start;
                    start = end;
                    end = temp;
                }

                for (int i = start; i <= end; i++)
                {
                    SetArrayAtDoeIndex(list, i, value, at);
                }

                return;
            }

            int doeIndex = ToDoeIndexNumber(indexValue, at);
            SetArrayAtDoeIndex(list, doeIndex, value, at);
        }

        private static object? GetArrayAtDoeIndex(List<object?> list, int doeIndex, Token at)
        {
            int internalIndex = ToInternalIndex(doeIndex, at);
            if (internalIndex >= list.Count)
            {
                return null;
            }

            return list[internalIndex];
        }

        private static void SetArrayAtDoeIndex(List<object?> list, int doeIndex, object? value, Token at)
        {
            int internalIndex = ToInternalIndex(doeIndex, at);
            EnsureArraySize(list, internalIndex + 1);
            list[internalIndex] = value;
        }

        private static int ToInternalIndex(int doeIndex, Token at)
        {
            if (doeIndex <= 0)
            {
                throw new InvalidOperationException("Array indices are 1-based and must be >= 1 at " + at.Line + ":" + at.Column + ".");
            }

            return doeIndex - 1;
        }

        private static void EnsureArraySize(List<object?> list, int size)
        {
            while (list.Count < size)
            {
                list.Add(null);
            }
        }

        private static int ToDoeIndexNumber(object? value, Token at)
        {
            if (value is int i)
            {
                return i;
            }

            if (value is double d)
            {
                return (int)Math.Round(d, MidpointRounding.AwayFromZero);
            }

            if (value is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException("Expected integer index near '" + at.Lexeme + "' at " + at.Line + ":" + at.Column + ".");
        }

        private static double ToRawNumber(object value)
        {
            if (value is int i)
            {
                return i;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            if (value is bool b)
            {
                return b ? 1 : 0;
            }

            throw new InvalidOperationException("Expected numeric value.");
        }

        private object? InvokeMax(List<object?> args, Token at)
        {
            if (args.Count == 0)
            {
                throw new InvalidOperationException("Max requires at least one argument.");
            }

            if (args.Count == 1 && args[0] is List<object?> list)
            {
                return list.Count;
            }

            double max = ToNumber(args[0], at);
            for (int i = 1; i < args.Count; i++)
            {
                double value = ToNumber(args[i], at);
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private object? InvokeMin(List<object?> args, Token at)
        {
            if (args.Count == 0)
            {
                throw new InvalidOperationException("Min requires at least one argument.");
            }

            if (args.Count == 1 && args[0] is List<object?> list)
            {
                return list.Count == 0 ? 0 : 1;
            }

            double min = ToNumber(args[0], at);
            for (int i = 1; i < args.Count; i++)
            {
                double value = ToNumber(args[i], at);
                if (value < min)
                {
                    min = value;
                }
            }

            return min;
        }

        private object? InvokeMap(List<object?> args, Token at)
        {
            if (args.Count == 0)
            {
                throw new InvalidOperationException("map requires at least one argument.");
            }

            Dictionary<string, object?> source = args[0] as Dictionary<string, object?> ??
                throw new InvalidOperationException("map expects the first argument to be a Dict/config at " + at.Line + ":" + at.Column + ".");

            Dictionary<string, object?> mapped = new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);

            if (args.Count == 1)
            {
                return mapped;
            }

            if (args.Count == 2 && args[1] is Dictionary<string, object?> overlay)
            {
                foreach (KeyValuePair<string, object?> pair in overlay)
                {
                    mapped[pair.Key] = pair.Value;
                }

                return mapped;
            }

            List<object?> projected = new List<object?>();
            for (int i = 1; i < args.Count; i++)
            {
                string key = ToDoeString(args[i]);
                projected.Add(mapped.TryGetValue(key, out object? value) ? value : null);
            }

            return projected;
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
