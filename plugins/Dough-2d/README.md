# Dough-2d

Windows-focused Dough plugin for:

- native windows
- basic GUI controls
- simple 2D physics simulation

Build:

```powershell
dotnet build
```

Use from Doe:

```dough
with Dough-2d

int window = dough2d_window("demo", 720, 420)
int label = dough2d_label(window, "hello", 24, 24, 180, 24)
int button = dough2d_button(window, "launch", 24, 64, 120, 32)

int world = dough2d_world(0, 98)
int body = dough2d_body(world, 40, 20, 80, 0, 1, 10, @false)
dict state = dough2d_body_info(body)
Print(state.y)
```

Plugin functions:

- `__dough2d_window_create(title, width, height)`
- `__dough2d_window_show(windowId)`
- `__dough2d_window_close(windowId)`
- `__dough2d_window_set_background(windowId, r, g, b)`
- `__dough2d_window_get_info(windowId)`
- `__dough2d_gui_add_label(windowId, text, x, y, width, height)`
- `__dough2d_gui_add_button(windowId, text, x, y, width, height)`
- `__dough2d_gui_set_text(controlId, text)`
- `__dough2d_gui_get_last_event(windowId)`
- `__dough2d_gui_clear_last_event(windowId)`
- `__dough2d_physics_create_world(gravityX, gravityY)`
- `__dough2d_physics_add_body(worldId, x, y, vx, vy, mass, radius, isStatic)`
- `__dough2d_physics_step(worldId, dt, floorY, bounce)`
- `__dough2d_physics_get_body(bodyId)`
- `__dough2d_physics_set_velocity(bodyId, vx, vy)`

Legacy `lib2d` wrapper names remain available through `lib/lib2d.doe`.
