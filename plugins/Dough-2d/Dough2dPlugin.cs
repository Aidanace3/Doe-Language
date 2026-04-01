using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Doe.PluginSdk;

namespace Dough2d;

public sealed class Dough2dPlugin : IDoePlugin
{
    private const string PluginDisplayName = "Dough-2d";

    public string Name => PluginDisplayName;

    public void Register(IDoePluginRegistry registry)
    {
        RegisterFunctionPair(registry, "version", _ => "1.0.0");
        RegisterFunctionPair(registry, "is_windows", _ => OperatingSystem.IsWindows());
        RegisterFunctionPair(registry, "window_create", CreateWindow);
        RegisterFunctionPair(registry, "window_show", ShowWindow);
        RegisterFunctionPair(registry, "window_close", CloseWindow);
        RegisterFunctionPair(registry, "window_set_background", SetWindowBackground);
        RegisterFunctionPair(registry, "window_get_info", GetWindowInfo);
        RegisterFunctionPair(registry, "gui_add_label", AddLabel);
        RegisterFunctionPair(registry, "gui_add_button", AddButton);
        RegisterFunctionPair(registry, "gui_set_text", SetControlText);
        RegisterFunctionPair(registry, "gui_get_last_event", GetLastEvent);
        RegisterFunctionPair(registry, "gui_clear_last_event", ClearLastEvent);
        RegisterFunctionPair(registry, "physics_create_world", CreateWorld);
        RegisterFunctionPair(registry, "physics_add_body", AddBody);
        RegisterFunctionPair(registry, "physics_step", StepWorld);
        RegisterFunctionPair(registry, "physics_get_body", GetBody);
        RegisterFunctionPair(registry, "physics_set_velocity", SetVelocity);
    }

    private static readonly ConcurrentDictionary<int, UiWindow> Windows = new();
    private static readonly ConcurrentDictionary<int, ControlHandle> Controls = new();
    private static readonly ConcurrentDictionary<int, PhysicsWorld> Worlds = new();
    private static readonly ConcurrentDictionary<int, PhysicsBody> Bodies = new();
    private static int _nextWindowId;
    private static int _nextControlId;
    private static int _nextWorldId;
    private static int _nextBodyId;

    private static void RegisterFunctionPair(IDoePluginRegistry registry, string suffix, DoePluginFunction handler)
    {
        registry.RegisterFunction("__dough2d_" + suffix, handler);
        registry.RegisterFunction("__lib2d_" + suffix, handler);
    }

    private static object? CreateWindow(IReadOnlyList<object?> args)
    {
        EnsureWindows();

        string title = GetString(args, 0, PluginDisplayName);
        int width = GetInt(args, 1, 960);
        int height = GetInt(args, 2, 540);

        int id = Interlocked.Increment(ref _nextWindowId);
        UiWindow window = UiWindow.Create(id, title, width, height);
        Windows[id] = window;
        return id;
    }

    private static object? ShowWindow(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        window.Invoke(form =>
        {
            form.Show();
            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
            }
        });

        return true;
    }

    private static object? CloseWindow(IReadOnlyList<object?> args)
    {
        int id = GetInt(args, 0, 0);
        UiWindow window = RequireWindow(id);
        window.Invoke(form => form.Close());
        Windows.TryRemove(id, out _);
        return true;
    }

    private static object? SetWindowBackground(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        int r = ClampColor(GetInt(args, 1, 30));
        int g = ClampColor(GetInt(args, 2, 30));
        int b = ClampColor(GetInt(args, 3, 30));

        window.Invoke(form => form.BackColor = Color.FromArgb(r, g, b));
        return true;
    }

    private static object? GetWindowInfo(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        return window.Invoke(form => new Dictionary<string, object?>
        {
            ["id"] = window.Id,
            ["title"] = form.Text,
            ["width"] = form.ClientSize.Width,
            ["height"] = form.ClientSize.Height,
            ["lastEvent"] = window.LastEvent
        });
    }

    private static object? AddLabel(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        string text = GetString(args, 1, string.Empty);
        int x = GetInt(args, 2, 12);
        int y = GetInt(args, 3, 12);
        int width = GetInt(args, 4, 160);
        int height = GetInt(args, 5, 24);

        int id = Interlocked.Increment(ref _nextControlId);
        Label label = window.Invoke(form =>
        {
            Label control = new()
            {
                Text = text,
                AutoSize = false,
                Left = x,
                Top = y,
                Width = width,
                Height = height,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            form.Controls.Add(control);
            return control;
        });

        Controls[id] = new ControlHandle(window, label);
        return id;
    }

    private static object? AddButton(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        string text = GetString(args, 1, "Button");
        int x = GetInt(args, 2, 12);
        int y = GetInt(args, 3, 48);
        int width = GetInt(args, 4, 120);
        int height = GetInt(args, 5, 32);

        int id = Interlocked.Increment(ref _nextControlId);
        Button button = window.Invoke(form =>
        {
            Button control = new()
            {
                Text = text,
                Left = x,
                Top = y,
                Width = width,
                Height = height
            };
            control.Click += (_, _) => window.LastEvent = "button:" + id;
            form.Controls.Add(control);
            return control;
        });

        Controls[id] = new ControlHandle(window, button);
        return id;
    }

    private static object? SetControlText(IReadOnlyList<object?> args)
    {
        int id = GetInt(args, 0, 0);
        string text = GetString(args, 1, string.Empty);
        ControlHandle handle = RequireControl(id);
        handle.Window.Invoke(_ => handle.Control.Text = text);
        return true;
    }

    private static object? GetLastEvent(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        return window.LastEvent ?? string.Empty;
    }

    private static object? ClearLastEvent(IReadOnlyList<object?> args)
    {
        UiWindow window = RequireWindow(GetInt(args, 0, 0));
        window.LastEvent = string.Empty;
        return true;
    }

    private static object? CreateWorld(IReadOnlyList<object?> args)
    {
        int id = Interlocked.Increment(ref _nextWorldId);
        PhysicsWorld world = new(id, GetDouble(args, 0, 0.0), GetDouble(args, 1, 9.8));
        Worlds[id] = world;
        return id;
    }

    private static object? AddBody(IReadOnlyList<object?> args)
    {
        PhysicsWorld world = RequireWorld(GetInt(args, 0, 0));
        int id = Interlocked.Increment(ref _nextBodyId);
        PhysicsBody body = new(
            id,
            world.Id,
            GetDouble(args, 1, 0.0),
            GetDouble(args, 2, 0.0),
            GetDouble(args, 3, 0.0),
            GetDouble(args, 4, 0.0),
            Math.Max(0.0001, GetDouble(args, 5, 1.0)),
            Math.Max(0.0, GetDouble(args, 6, 10.0)),
            GetBool(args, 7, false));

        world.Bodies.Add(body);
        Bodies[id] = body;
        return id;
    }

    private static object? StepWorld(IReadOnlyList<object?> args)
    {
        PhysicsWorld world = RequireWorld(GetInt(args, 0, 0));
        double dt = Math.Max(0.0001, GetDouble(args, 1, 0.016));
        double floorY = GetDouble(args, 2, 480.0);
        double bounce = Math.Clamp(GetDouble(args, 3, 0.75), 0.0, 1.0);

        for (int i = 0; i < world.Bodies.Count; i++)
        {
            PhysicsBody body = world.Bodies[i];
            if (body.IsStatic)
            {
                continue;
            }

            body.Vx += world.GravityX * dt;
            body.Vy += world.GravityY * dt;
            body.X += body.Vx * dt;
            body.Y += body.Vy * dt;

            if (body.Y + body.Radius > floorY)
            {
                body.Y = floorY - body.Radius;
                body.Vy = -body.Vy * bounce;
            }
        }

        return new Dictionary<string, object?>
        {
            ["worldId"] = world.Id,
            ["delta"] = dt,
            ["bodyCount"] = world.Bodies.Count,
            ["floorY"] = floorY
        };
    }

    private static object? GetBody(IReadOnlyList<object?> args)
    {
        PhysicsBody body = RequireBody(GetInt(args, 0, 0));
        return body.ToDictionary();
    }

    private static object? SetVelocity(IReadOnlyList<object?> args)
    {
        PhysicsBody body = RequireBody(GetInt(args, 0, 0));
        body.Vx = GetDouble(args, 1, body.Vx);
        body.Vy = GetDouble(args, 2, body.Vy);
        return body.ToDictionary();
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException(PluginDisplayName + " window and gui features require Windows.");
        }
    }

    private static UiWindow RequireWindow(int id)
    {
        if (Windows.TryGetValue(id, out UiWindow? window))
        {
            return window;
        }

        throw new InvalidOperationException("Unknown " + PluginDisplayName + " window id " + id.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static ControlHandle RequireControl(int id)
    {
        if (Controls.TryGetValue(id, out ControlHandle? control))
        {
            return control;
        }

        throw new InvalidOperationException("Unknown " + PluginDisplayName + " control id " + id.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static PhysicsWorld RequireWorld(int id)
    {
        if (Worlds.TryGetValue(id, out PhysicsWorld? world))
        {
            return world;
        }

        throw new InvalidOperationException("Unknown " + PluginDisplayName + " physics world id " + id.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static PhysicsBody RequireBody(int id)
    {
        if (Bodies.TryGetValue(id, out PhysicsBody? body))
        {
            return body;
        }

        throw new InvalidOperationException("Unknown " + PluginDisplayName + " physics body id " + id.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static int GetInt(IReadOnlyList<object?> args, int index, int fallback)
    {
        if (index >= args.Count || args[index] == null)
        {
            return fallback;
        }

        object value = args[index]!;
        if (value is int i)
        {
            return i;
        }

        if (value is double d)
        {
            return (int)Math.Round(d, MidpointRounding.AwayFromZero);
        }

        if (value is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(PluginDisplayName + " expected an integer argument at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static double GetDouble(IReadOnlyList<object?> args, int index, double fallback)
    {
        if (index >= args.Count || args[index] == null)
        {
            return fallback;
        }

        object value = args[index]!;
        if (value is int i)
        {
            return i;
        }

        if (value is double d)
        {
            return d;
        }

        if (value is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(PluginDisplayName + " expected a number argument at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static string GetString(IReadOnlyList<object?> args, int index, string fallback)
    {
        if (index >= args.Count || args[index] == null)
        {
            return fallback;
        }

        return Convert.ToString(args[index], CultureInfo.InvariantCulture) ?? fallback;
    }

    private static bool GetBool(IReadOnlyList<object?> args, int index, bool fallback)
    {
        if (index >= args.Count || args[index] == null)
        {
            return fallback;
        }

        object value = args[index]!;
        if (value is bool b)
        {
            return b;
        }

        if (value is int i)
        {
            return i != 0;
        }

        if (value is string text && bool.TryParse(text, out bool parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(PluginDisplayName + " expected a bool argument at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static int ClampColor(int value) => Math.Clamp(value, 0, 255);

    private sealed record ControlHandle(UiWindow Window, Control Control);

    private sealed class UiWindow
    {
        private readonly ManualResetEventSlim _ready = new(false);
        private readonly Thread _thread;
        private Form? _form;

        public int Id { get; }
        public string LastEvent { get; set; } = string.Empty;

        private UiWindow(int id, string title, int width, int height)
        {
            Id = id;
            _thread = new Thread(() =>
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Form form = new()
                {
                    Text = title,
                    ClientSize = new Size(Math.Max(200, width), Math.Max(150, height)),
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = Color.FromArgb(30, 30, 36)
                };
                form.FormClosed += (_, _) => LastEvent = "closed";
                _form = form;
                _ready.Set();
                Application.Run(form);
            })
            {
                IsBackground = true,
                Name = "dough-2d-window-" + id.ToString(CultureInfo.InvariantCulture)
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _ready.Wait();
        }

        public static UiWindow Create(int id, string title, int width, int height) => new(id, title, width, height);

        public void Invoke(Action<Form> action)
        {
            Form form = RequireForm();
            if (form.InvokeRequired)
            {
                form.Invoke(action, form);
                return;
            }

            action(form);
        }

        public T Invoke<T>(Func<Form, T> func)
        {
            Form form = RequireForm();
            if (form.InvokeRequired)
            {
                return (T)form.Invoke(func, form)!;
            }

            return func(form);
        }

        private Form RequireForm()
        {
            Form? form = _form;
            if (form == null || form.IsDisposed)
            {
                throw new InvalidOperationException(PluginDisplayName + " window is no longer available.");
            }

            return form;
        }
    }

    private sealed class PhysicsWorld
    {
        public int Id { get; }
        public double GravityX { get; }
        public double GravityY { get; }
        public List<PhysicsBody> Bodies { get; } = [];

        public PhysicsWorld(int id, double gravityX, double gravityY)
        {
            Id = id;
            GravityX = gravityX;
            GravityY = gravityY;
        }
    }

    private sealed class PhysicsBody
    {
        public int Id { get; }
        public int WorldId { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Mass { get; }
        public double Radius { get; }
        public bool IsStatic { get; }

        public PhysicsBody(int id, int worldId, double x, double y, double vx, double vy, double mass, double radius, bool isStatic)
        {
            Id = id;
            WorldId = worldId;
            X = x;
            Y = y;
            Vx = vx;
            Vy = vy;
            Mass = mass;
            Radius = radius;
            IsStatic = isStatic;
        }

        public Dictionary<string, object?> ToDictionary()
        {
            return new Dictionary<string, object?>
            {
                ["id"] = Id,
                ["worldId"] = WorldId,
                ["x"] = X,
                ["y"] = Y,
                ["vx"] = Vx,
                ["vy"] = Vy,
                ["mass"] = Mass,
                ["radius"] = Radius,
                ["static"] = IsStatic
            };
        }
    }
}
