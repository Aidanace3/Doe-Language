# Doe Language Documentation

## Syntax

### Section 1: Basic Syntax

#### I/O

**Input:**
- `readln(n)` - reads in line #n
- `Input("Prompt")` - accepts user input
  - `-H` - hide input with asterisks
  - `-W n` - adds a time limit to input

**Output:**
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
if(condition)::
{
    // code
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
- `Funcs` - see Section 2.1 for syntax
- By default, functions take point variable input

#### Points

- Written as `(*POINTNAME)`
- Used for `YEILD` and `RETURN`
- `awaitval` executes a function as soon as a value is taken from yeild
- `yeild(*Point)` sends a value to a point.
- `exit(*Point)` removes point from list. use after cases and functions
- useful for:
- - changing a block variable later on
- Examples in Section 2.4

---

## Section 2: Examples & Syntax

### 2.1 Functions

```dough
def test(x) 
{
    Print("Functions test: check")
    Print("Functions Variable test:" + x)
    return x >> test // returns to origin of valu
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
```dough
*Case ifCase(n;)
  *Case is 5 :: Then
  {/(code goes here)\}
  *Case is outlier?
  {/(code goes here)\};
Exit(*case)
```
---

## Section 3: Recommended Syntax Highlighting Colors

### Blue
- Variable types (`Poly`, `Str`, `Int`, `Flt`, `Asa`, etc.)
- Conditions (`if ( A > B )::break`)
- Keywords: `end`, `break`
- Operators: `>`, `<`, `=>`, `<=`, `!`, `|`, `*|`, `!&`, `!|`, `&&`, `!&`

### Red
- Functions
- `null` (e.g., `A = Arr[nopoly int]; Arr[1..10] = null`)

### Pink
- Keywords: `if`, `else`, `ifcase`, `imports`, `def`

### Orange
- Numbers
- Variables
- Arrays
