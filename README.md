Dough_V0.7.2-alpha-1



# Doe Language Documentation

## NOTE: install [here](https://github.com/Aidanace3/Dough)

## Running Dough Programs

- Direct command (no `dotnet run`): `.\dough.cmd examples\test.doe`
- PowerShell wrapper: `.\dough.ps1 examples\test.doe`
- Silent mode: `.\dough.cmd --silent examples\test.doe`
- Verbose mode (default): `.\dough.cmd examples\test.doe`

## Install In VS Code (Language Support)

- Install from Marketplace:
  - `code --install-extension aidanace3.dough-language`
- Or in VS Code Extensions search:
  - `aidanace3.dough-language`

## Install Runtime (CLI)

- Open: `https://github.com/Aidanace3/Doe-Language/releases/latest`
- Download `dough-runtime-win-x64.zip`
- Extract and run `Dough.exe yourfile.doe`
- Optional: add the extracted folder to your PATH so `Dough.exe` works from any terminal.

## Add `Launch.Json`
- Add a folder at top called `.vscode`
- Add a file called `Launch.json`
  paste this;
  
  ```json
  {
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Dough: Run Current File (CLI)",
      "type": "node-terminal",
      "request": "launch",
      "command": "powershell -NoProfile -ExecutionPolicy Bypass -File \"${workspaceFolder}/../Doe-Language/dough.ps1\" \"${file}\""
    },
    {
      "name": "Dough: Debug Current File (CLI)",
      "type": "node-terminal",
      "request": "launch",
      "command": "powershell -NoProfile -ExecutionPolicy Bypass -File \"${workspaceFolder}/../Doe-Language/dough.ps1\" --debug \"${file}\""
    }
  ]
}```


## Syntax

### Section 1: Basic Syntax

#### Other rules

- An independent bool is prefixed by an `@`
- - eg. `as (@true);`
- Any casing works, even if specifically noted here, (eg, ’Print` = `print`)


#### Operators

ARITH Operators: `+`,`**`,`-`,`/`,`%`,`^`,`%%`.

- `+`: Add
- `**`: Multiply ( \* is used for points )
- `-`: Subtract
- `/`: Divide
- `%`: Percentage of
- `^`: Exponentiate
- `%%`: Remdiv (Modulo)
  
CON Operators: `>`, `<`, `=>`, `<=`, `!`, `|`, `*|`, `!&`, `!|`, `&&`, `!&`.

- `>`: Greater
- `<`: Less
- `=>`: Equal or Greater
- `<=`: Equal or Less
- `!`: Not
- `|`: Or
- `*|`: Xor
- `&&`: Xand (Common And)
- `!&`: Nand
- `!|`: Nor
  
#### I/O

#### Input

- `readln(n)` - reads in line #n
- `Input("Prompt")` - accepts user input
  - `-H` - hide input with asterisks
  - `-W n` - adds a time limit to input

#### Output

- `Print("x")` - simple output
- Use `+` to concatenate variables with text

#### Types

- `NoPoly` - keep type; no polymorphism
- `Const` - keep value constant
- `Str`, `String` - text value
- `Int` - no-decimal numeral
- `Flt` - decimal numeral
- `Arr[Type]` - array with specified types
- `Max(Arr)` - append upper limit to array
- `Min(Arr)` - opposite (lower limit)

#### Conditions

**If Statement:**

```dough
if(condition)::then
{
    // code
}
```

/(
something cool you can do is change the `Then` after the `::` to a `Break` or `Func()`
to directly do a Break or Run a Function after check.
)\

eg.

```Dough
NoPoly Const Int X = 5

def FunctionA {
  Print("hi")
}

if ( X == 5 )::FunctionA()
{
  Return X >> this
}
```

**Else Statement:**

```dough
else::
{
    // otherwise code
}
```

**Switch Statement:**

```dough
IfCase(x)
{
    Case: X is N:
    {
        // code
    }
    Default: X is Outlier
    {
        // code for outlier cases
    }
}
```

#### Dictionaries & Functions

- `Dict` - create a dictionary (see Section 2.3)
- `Return` - written as `Return n >> (point)`
- `Funcs` - Depracated [\(?\)]

#### Points

- Written as `(*POINTNAME)`
- Used for `YEILD` and `RETURN`
- `awaitval` executes a function as soon as a value is taken from yeild
- `yeild(var >> *Point)` sends a value to a point.
- `exit(*Point)` removes point from list. use after cases and functions
- `Store(Val Asa Valname >> *Point)` saves a value to a point
- - accessible with `request(x << *Point.Valname)`
- - Another way to store it is defining it in the Point's function it's connected to
- Examples in Section 2.4

---

## Section 2: Examples & Syntax

### 2.1 Loops

Point loop

```dough
{
(*); 
{
  //code
  loop ( l >> this x10 )
  return l >> this
}
exit(*this)
}
```

Built in loops:

as (while)

```dough
as(@true):
{
  //code
  if(@stopcondition)::break
}
```

each in (foreach)

```dough
each(x in [arr]) do:
{
  //code
}
```

### 2.2 Arrays

To define a typed array:

```dough
/( set up )\ name = Arr[type]
/( length )\ conf name.Length = x
```

**Note:** `conf` changes the properties of an object instead of `obj.setting = x`

Array properties include:

- `type` (only if not NoPoly)
- `name` (constant)
- `length` (integer)
- `lower` (lowest index, useful for constants like `LettersFromO = [p,q,r,s...]`)

### 2.3 Dictionaries

Define a dict:

```dough
dict ExampleDict:
{
    // variables go here
};
```

Define a locked (single type) dict:

```dough
locked dict(type):
{
    // variables of specified type
}
```

to use a dictionary variable; `Dict.Varname’

### 2.4 Points

```dough
(*Taking:) awaitval(x;)
{
    print(x)
}

// other stuff
x = 5
x >> *Taking
```

**Output:** `5`

```dough
// Example of a continuous listener
(*LogStream:) awaitval(msg;)
{
    Print("LOG: " + msg)
}

// Later in the code
if ( Logval == 1 )::Then
  {"message 1" >> *LogStream}
elif ( Logvak == 2 )::Then
  {"message 2" >> *LogStream}
else::Break
```

### 2.5 Conditionals

#### 2.5.1 Cases

```dough
*Case ifCase(n;)
  *Case << 5 :: Then
  {/(code goes here)\}
  *Case << outlier? // equivelant to `Default` in clang
  {/(code goes here)\};
Exit(*case)
```

#### 2.5.2 If/Else

```dough
NoPoly Int x = 7
if ( X > 6 )::then
  x = 7
else::break

```


### Footnotes

