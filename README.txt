

# Syntax

## Section 1; Basic syntax

I/O:
|-  In:
|   readln(n): reads in line #n
|	Input("Prompt") [-H (hide input with *s, -W n (adds a time limit to input)]
|-  Out:
|   Print("x"); simple out. use + __ to add var to text-+++
‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾
Types:
|   NoPoly: Keep type; No polymorphism
|   Const: Keep value
|   \[Str, String\]: Text value
|   Int: No-Decimal numeral
|   Flt: Decimal numeral
~~
|   Arr\[Type\]: array with types
|   Max(Arr): append upper limit to Arr
|   Min(Arr): oppisite.
Conditions:
|   If: if( cond )::\[break (stop), then\]  
*   {
*
*   }
|   Else: 
* {
*  Otherwise:
* {
*
* }
|   Switch: IfCase(x)
|   Case: X is N:
|   Default: X is Outlier //SPECIFFICALLY THIS!!! can use \x to check X for "outlier"
‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾

Dicts, funcs
|   Dict; Make a dictionary (see s2.3 for syntax)
|   return is written as Return n >> (point)
|   Funcs: See 2.1 for syntax
- by default, funcs take point variable input.

Points:
|   written as (*POINTNAME)
|   used for GOTO and RETURN.
|   awaitval does a function as soon as a value is taken from point.
Examples in s2.4

## 2. Examples & Syntax:
1. FUNCS:
```Dough
def test(x) 
{
    Print("Functions test: check")
    Print("Functions Variable test:" + x)
    return x >> test // returns to origin of value
}
``
2. ARRAYS:
to define a typed array:
```Dough
/( set up )\ name = Arr[type]
/( length )\ conf name.Length = x
// notes; conf changes the properties of an object, instead of obj.setting = x, we'd write conf obj.setting
```
- array properties include: type (ONLY IF NOT NOPOLY), name (constant), length (int), and lower (lowest index, useful for consts, like LettersFromO = [p,q,r,s...]

3. DICTS:

define a dict with: 
```Dough 
dict ExampleDict:
{
//variables would go HERE.
};
```

to define a locked (one type) dict:
```Dough
locked dict(type):
```

4. POINTS:
```Dough
(*Taking:) awaitval(x;){	print(x)}
//other stuff
x = 5
x >> *Taking
```
output: 5

## 3. Recommended Colors

blue:
	> variable types (Poly, Str, Int, Flt, Asa, etc)
	> conditions (if ( A > B )::break)
	  		^^^^^^^
	> end, break
	> operators (>,<,=>,<=,!,|,*|,!&,!|,&&,!&)

red:
	> functions
	> null (ex. A = Arr[nopoly int]; \n Arr[1..10] = null)  //for those wondering, this makes an array labeled A then sets index 1 to ten to Nil (an unset variable.) 

pink:
	> if, else, ifcase
	> imports
	> def

orange:
	> numbers
	> variables
	> arrays

	
