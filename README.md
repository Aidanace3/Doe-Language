# Dough

Dough is the Doe Language runtime, tooling, and release repo.

Current runtime version: `1.0.0`

This repo contains:

- the CLI/runtime
- the Windows MSI installer build
- the VS Code language tooling submodule in `Dough/`
- the built-in `Dough-2d` plugin and Doe wrappers

## Install

### Runtime

- Latest releases: `https://github.com/Aidanace3/Doe-Language/releases/latest`
- Standalone build: run `.\publish-runtime.ps1`
- Windows MSI build: run `.\build-msi.ps1`
- Full release bundle: run `.\build-release.ps1`

The MSI installs to `C:\Program Files\Dough`, adds that folder to `PATH`, and sets `DOE_PLUGIN_PATH` to `C:\Program Files\Dough\plugins`.

### VS Code extension

- Marketplace install: `code --install-extension aidanace3.dough-language`
- Extension repo: `https://github.com/Aidanace3/Dough`

## Run programs

Examples:

```powershell
.\dough.cmd examples\test.doe
.\dough.cmd --check examples\test.doe
.\dough.cmd --debug examples\test.doe
.\dough.cmd --version
.\dough.cmd --runtime-info
```

The wrappers prefer:

1. `published\win-x64\Dough.exe`
2. Release builds
3. Debug builds

## Language quick look

```dough
conf app:
{
    str title = "Hello"
    int width = 720
    int height = 420
}

def Main()
{
    Print(app.title)
}
```

Core features:

- `.doe` and `.dough` source files
- `conf`, `dict`, `map`, arrays, functions, points
- plugin loading with `with plugin:Name`
- local module imports from `lib/`, `libs/`, `library/`, and `libraries/`

## Plugins

### C# plugins

Plugins can be loaded with:

```dough
with plugin:Your.Plugin.Name
```

Search roots include:

- the current working directory
- the importing file directory
- `plugins/` and `plugin/` folders
- `DOE_PLUGIN_PATH`
- `PATH`

Two plugin styles are supported:

- convention plugin: public static `PluginFunctions` methods
- SDK plugin: implement `IDoePlugin` from `PluginSdk/DoePluginContracts.cs`

### Dough-2d

The repo ships a built-in Windows-focused 2D/UI plugin:

- primary Doe wrapper: `lib/Dough-2d.doe`
- compatibility wrapper: `lib/lib2d.doe`
- plugin project: `plugins/Dough-2d/Dough-2d.csproj`

Example imports:

```dough
with "../lib/Dough-2d.doe"
```

Legacy `lib2d` wrappers still work for compatibility.

## Development

### Runtime

```powershell
dotnet build .\Other_Bullshit\Doe-Language.csproj -c Release
```

### Dough-2d plugin

```powershell
dotnet build .\plugins\Dough-2d\Dough-2d.csproj -c Release
```

### VS Code extension

```powershell
cd .\Dough
npm ci
npx @vscode/vsce package --no-dependencies
```

## Release outputs

`.\build-release.ps1` produces:

- published runtime files under `artifacts\<version>\runtime-win-x64`
- `dough-runtime-x64.msi`
- `dough-language-<version>.vsix`

## Repository layout

- `src/`: runtime source
- `PluginSdk/`: plugin contracts
- `plugins/`: sample and built-in plugins
- `lib/`: Doe wrapper modules
- `examples/`: sample programs
- `installer/`: WiX MSI project
- `Dough/`: VS Code extension submodule
