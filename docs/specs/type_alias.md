### ECMA Specification for Vein Language Type Alias Declarations

#### 1. Introduction

This section defines the behavior for parsing type alias declarations in the Vein Language. Type aliases allow developers to create shorthand notations for types, including primitive types, generic types, and method delegates.

#### 2. Alias Declaration

An alias declaration allows defining a new name for an existing type. Aliases can be declared globally or locally within a specific scope.

#### 3. Global vs Local Aliases

- **Global Alias**: A global alias is accessible throughout the entire program.
- **Local Alias**: A local alias is restricted to the scope in which it is defined.

#### 4. Detailed Grammar

##### Alias Declaration

The basic syntax for an alias declaration is as follows:
```
AliasDeclaration ::= ('global' 'alias' IdentifierExpression '=' TypeExpression ';')
                   | ('alias' IdentifierExpression '=' TypeExpression ';')
                   | ('alias' IdentifierExpression '=' MethodParametersAndBody ';')
```

Where:
- **Global Alias Declaration**: Defines a globally accessible alias.
- **Local Alias Declaration**: Defines an alias within the local scope.
- **IdentifierExpression**: The new alias name.
- **TypeExpression**: The type being aliased.
- **MethodParametersAndBody**: The method signature for delegate aliases.

##### Parsing Rules

###### Global Alias Declaration
Defines a globally accessible alias.
```
global alias AliasName = TypeReference;
```

###### Local Alias Declaration
Defines a locally scoped alias.
```
alias AliasName = TypeReference;
```

###### Local Alias Declaration for Generic Type
Defines an alias for a generic type.
```
alias AliasName = GenericType<Parameters>;
```

###### Local Alias Declaration for Delegate
Defines an alias for a method delegate.
```
alias AliasName = (ParameterList): ReturnType;
```

Example language constructs:
```vein
global alias SameInt32 = i32;          // Global alias for a primitive type.
alias AnotherString = string;          // Local alias for a primitive type.
alias genericFooBar = FooBar<i32>;     // Local alias for a generic type.
alias methodDelegate = (arg1: i32, arg2: i32, arg3: f32): void; // Local alias for a delegate.
```

#### 5. Parsing Procedures

1. **Starting Keyword**: 
   - `global` keyword for global aliases (optional).
   - `alias` keyword to introduce the alias.

2. **Alias Name**: 
   - An `IdentifierExpression` representing the name of the alias.

3. **Type or Method Definition**:
   - The `TypeExpression` for type aliases.
   - The `MethodParametersAndBody` for delegate aliases.

4. **Terminal Character**: 
   - `;` symbol to terminate the alias declaration.

#### 6. Example Parsing Implementation

The parser rule for alias declarations is structured as follows:

```csharp
internal virtual Parser<AliasSyntax> AliasDeclaration =>
    from global in KeywordExpression("global").Token().Optional()
    from keyword in KeywordExpression("alias").Token()
    from aliasName in IdentifierExpression.Token()
    from equals in Parse.String("=").Token()
    from body in MethodParametersAndBody.Token().Select(x => new TypeOrMethod(null, x))
        .Or(TypeExpression.Token().Then(_ => Parse.Char(';').Token().Return(_)).Select(x => new TypeOrMethod(x, null)))
    select new AliasSyntax(global.IsDefined, aliasName, body);
```

#### 7. Example Alias Declarations

```vein
global alias SameInt32 = i32;        // Defines a global alias for the primitive type 'i32'.
alias AnotherString = string;        // Defines a local alias for the primitive type 'string'.
alias genericFooBar = FooBar<i32>;   // Defines a local alias for a generic type 'FooBar<i32>'.
alias methodDelegate = (arg1: i32, arg2: i32, arg3: f32): void; // Defines a local alias for a delegate type.
```

#### 8. Conclusion

This specification outlines the rules and behaviors for parsing type alias declarations in the Vein Language, providing a structured approach to define aliases that enhance code readability and maintainability. By following these guidelines, the Vein parser can correctly interpret and process alias declarations, ensuring proper functionality within the language.