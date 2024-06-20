### ECMA Specification for Vein Language Identifier Expression

#### 1. Introduction

This section defines the behavior of the `IdentifierExpression` in the Vein Language. The `IdentifierExpression` is a fundamental lexical element used to declare and reference identifiers within the language syntax.

#### 2. Identifier Expression Definition

An `IdentifierExpression` in the Vein Language represents a valid identifier that allows for the naming of variables, functions, types, and other language constructs. The rules for forming an `IdentifierExpression` are as follows:

#### 3. Raw Identifier

The basic building block of an `IdentifierExpression` is the `RawIdentifier`. The `RawIdentifier` must adhere to the following criteria:

- **Starting characters**: The identifier must begin with a letter (`A-Z`, `a-z`), an underscore (`_`), or an at symbol (`@`). This allows for the inclusion of conventional identifiers, special identifiers, and identifiers within specific namespaces or contexts.
- **Subsequent characters**: After the initial character, the identifier may consist of any combination of letters, digits (`0-9`), or underscores (`_`).

#### 4. Tokenization and Whitespace Handling

An `IdentifierExpression` is derived from the `RawIdentifier` and includes tokenization to handle leading and trailing whitespace. This ensures that the identifier is correctly parsed in the context of surrounding whitespace.

- **Token**: The `RawIdentifier` is tokenized to ignore any whitespace characters that may surround it.
- **Naming**: The resulting tokenized identifier is named as "Identifier" for referencing within syntactical structures.

#### 5. Validation and Error Marking

An additional validation step is implemented to ensure that certain reserved system types cannot be used as identifiers. This enhances the robustness and predictability of the language by preventing naming conflicts with key system types.

- **Error Marking**: If an identifier matches any system type listed in the `VeinKeywords`, it will be marked as an error with the message "cannot use system type as identifier."

#### 6. Positional Tracking

The `IdentifierExpression` tracks its position within the source code to provide detailed diagnostic information during parsing and compilation.

- **Positioned**: The identifier token retains positional information to assist in error reporting and debugging processes.

#### 7. Sample Grammar

The following grammar describes the structure of an `IdentifierExpression` using the rules defined above:

```
IdentifierExpression ::= Token(RawIdentifier) : {
    if VeinKeywords.contains(identifier) {
        raise "cannot use system type as identifier"
    }
}
```

#### 8. Exception Handling

If an invalid identifier is detected (i.e., using a reserved system type), a parsing exception will be thrown with an appropriate error message, indicating the nature of the violation and its position in the source code.

#### 9. Example

```vein
// Valid identifier
let _myVariable123 = 10;

// Invalid identifier (system type)
let i32 = 15; // Error: cannot use system type as identifier
```

#### 10. Conclusion

This specification defines the rules and behavior for parsing and validating `IdentifierExpression` in the Vein Language. By adhering to these guidelines, Vein ensures consistent and predictable identifier handling, thereby aiding developers in writing clear and error-free code.