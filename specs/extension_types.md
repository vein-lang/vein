# ECMA Specification for Vein Language Extension Types

## 1. Introduction

This section defines the behavior for parsing extension type declarations in the Vein Language. Extensions allow developers to augment existing types with new functionality, including methods, properties, and implementing interfaces, without modifying the original type or using inheritance.

## 2. Extension Declaration

An extension declaration enables the definition of new members for an existing type or the implementation of interfaces for that type. Extensions can be declared explicitly or implicitly.

## 3. Explicit vs Implicit Extensions

- **Explicit Extension:** An explicit extension requires the use of the `explicit` keyword and is defined for a specific type.
- **Implicit Extension:** An implicit extension does not require the `explicit` keyword and can be applied to a type more generically.

### 3.1 Explicit Extension Types

Explicit extension types allow adding new members to an existing type without modifying the original type. These members need to be explicitly qualified when accessed to avoid naming conflicts and ambiguity.

#### Characteristics
- Declared with the `explicit` keyword.
- Members must be accessed with explicit qualification.
- Provide clear separation between the base type and the extension.

### 3.2 Implicit Extension Types

Implicit extension types also allow adding new members to an existing type. Unlike explicit extensions, these members are directly accessible on the base type, providing seamless integration.

#### Characteristics
- Declared with the `implicit` keyword.
- Members are automatically accessible on the base type.
- Provide a seamless and intuitive extension experience.

## 4. Detailed Grammar

### Extension Declaration

The basic syntax for an extension declaration is as follows:

```
ExtensionDeclaration ::= (ExtensionModifier* ('implicit' | 'explicit') 'extension' Identifier TypeParameterList? ('for' Type)? TypeParameterConstraintsClause* ExtensionBody)
```

Where:
- `ExtensionModifier` specifies optional modifiers for the extension.
- `implicit` | `explicit` specifies whether the extension is implicit or explicit.
- `extension` is the keyword introducing the extension declaration.
- `Identifier` represents the name of the extension.
- `TypeParameterList` is an optional list of type parameters.
- `for Type` specifies the type the extension is for.
- `TypeParameterConstraintsClause` defines any constraints on the type parameters.
- `ExtensionBody` contains the members of the extension.

### Extension Body

The extension body defines the members of the extension:

```
ExtensionBody ::= '{' ExtensionMemberDeclaration* '}'
```

### Extension Member Declaration

The extension member declaration allows defining various members within an extension:

```
ExtensionMemberDeclaration ::= ConstantDeclaration
                             | FieldDeclaration
                             | MethodDeclaration
                             | PropertyDeclaration
                             | EventDeclaration
                             | IndexerDeclaration
                             | OperatorDeclaration
                             | TypeDeclaration
```

### Extension Modifier

The extension modifier defines the accessibility and other modifiers for the extension:

```
ExtensionModifier ::= 'unsafe'
                    | 'static'
                    | 'protected'
                    | 'internal'
                    | 'private'
```

## 5. Parsing Rules

### Extension Declaration

Defines an extension for a specific type or a generic type.

```
extension ExtensionName for Type {
    // Extension members
}
```

### Explicit Extension Declaration

Defines an explicit extension for a specific type.

```
explicit extension ExtensionName for Type {
    // Extension members
}
```

### Implicit Extension Declaration

Defines an implicit extension without specifying the `explicit` keyword.

```
implicit extension ExtensionName for Type {
    // Extension members
}
```

### Type Parameter List

Defines type parameters for generic extensions.

```
extension ExtensionName<T> for Type {
    // Extension members
}
```

### Type Parameter Constraints Clause

Defines constraints on type parameters.

```
extension ExtensionName<T> for Type when T : Constraint {
    // Extension members
}
```

## 6. Parsing Procedures

### Starting Keyword

- `explicit` keyword for explicit extensions (optional).
- `implicit` keyword for implicit extensions (optional).
- `extension` keyword to introduce the extension.

### Extension Name

- An `Identifier` representing the name of the extension.

### Type or Method Definition

- The `Type` for which the extension is being defined.

### Extension Body

- The members defined within the extension.

### Terminal Character

- `;` symbol to terminate the extension declaration.

## 7. Example Parsing Implementation

The parser rule for extension declarations is structured as follows:

```csharp
internal virtual Parser<ExtensionSyntax> ExtensionDeclaration =>
    from modifiers in ExtensionModifiers.Token().Many()
    from keyword in KeyworkExpression("extension").Token()
    from identifier in IdentifierExpression.Token()
    from typeParams in TypeParameterList.Token().Optional()
    from forKeyword in Parse.String("for").Token().Optional()
    from type in TypeExpression.Token()
    from constraints in GenericConstraintParser.Token().Many().Optional()
    from body in ClassMemberDeclaration.Token()
    select new ExtensionSyntax(modifiers, identifier, typeParams, type, constraints, body);
```

## 8. Example Extension Declarations

### Explicit Extension

```
explicit extension R for U {
    public void M2() { } // warning: needs `new`
    void M() {
        M2(); // find `R.M2()`, no ambiguity
    }
}
```

### Implicit Extension

```
implicit extension V for W {
    public void M3() { }
}
```

## 9. Conclusion

This specification outlines the rules and behaviors for parsing extension declarations in the Vein Language, providing a structured approach to defining extensions that enhance code functionality and maintainability. By following these guidelines, the Vein parser can correctly interpret and process extension declarations, ensuring proper functionality within the language.
