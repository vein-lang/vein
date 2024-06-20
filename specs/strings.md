### ECMA Specification for Vein Language String Literals

#### 1. Introduction

This section defines the behavior for parsing string literals in the Vein Language. String literals are sequences of characters enclosed in double quotes, which may contain escaped sequences.

#### 2. String Literal Definition

A string literal in the Vein Language is a contiguous sequence of characters enclosed within double quotes (`"`). It may include whitespace and escaped characters.

#### 3. Detailed Grammar

##### String Literal

The basic syntax for a string literal is:
```
StringLiteral ::= '"' (Character | EscapedCharacter)* '"'
```

Where:
- **Character**: Any character except backslash (`\`) and double-quote (`"`).
- **EscapedCharacter**: A backslash followed by any character, which is treated as a single escaped sequence.

### Parsing Rules:
1. **Leading Whitespace**: 
   - Zero or more whitespace characters can precede the string literal.

2. **Opening Quote**: 
   - The string literal starts with a double-quote character (`"`).

3. **Content**: 
   - The content within the string can include:
     - Regular characters, excluding backslash (`\`) and double-quote (`"`).
     - Escaped sequences, where a backslash (`\`) is followed by any character.

4. **Closing Quote**: 
   - The string literal ends with a double-quote character (`"`).

5. **Trailing Whitespace**: 
   - Zero or more whitespace characters can follow the string literal.

#### 4. Parsing Implementation

The given parsing rule for `StringLiteral` is structured as follows:

```csharp
protected internal virtual Parser<string> StringLiteral =>
    from leading in Parse.WhiteSpace.Many()
    from openQuote in Parse.Char('\"')
    from fragments in Parse.Char('\\').Then(_ => Parse.AnyChar.Select(c => $"\\{c}"))
        .Or(Parse.CharExcept("\\\"").Many().Text()).Many()
    from closeQuote in Parse.Char('\"')
    from trailing in Parse.WhiteSpace.Many()
    select $"\"{string.Join(string.Empty, fragments)}\"";
```

### Implementation Details:

1. **Leading Whitespace**: 
   - `Parse.WhiteSpace.Many()` matches zero or more whitespace characters.
   
2. **Opening Quote**: 
   - `Parse.Char('\"')` matches the opening double-quote character.

3. **Content Fragments**:
   - The content of the string literal is parsed as a collection of fragments:
     - **Escaped Sequences**: `Parse.Char('\\').Then(_ => Parse.AnyChar.Select(c => $"\\{c}"))` matches a backslash followed by any character, treating it as an escaped sequence.
     - **Regular Characters**: `Parse.CharExcept("\\\"").Many().Text()` matches any character except backslash (`\`) and double-quote (`"`), avoiding special characters.
   - `Many()` is used to match multiple instances of either escaped sequences or regular characters.

4. **Closing Quote**: 
   - `Parse.Char('\"')` matches the closing double-quote character.

5. **Trailing Whitespace**: 
   - `Parse.WhiteSpace.Many()` matches zero or more whitespace characters following the string literal.

6. **Resulting String**:
   - `select $"\"{string.Join(string.Empty, fragments)}\""` combines the parsed fragments into a single string, enclosed in double quotes.

#### 5. Examples

Valid string literals:

```vein
"Hello, World!"            // Regular string with no escaped sequences.
"Line1\nLine2"             // String with an escaped newline character.
"Tab\tSeparated"           // String with an escaped tab character.
"Quote: \"Embedded\""      // String with an escaped double-quote character.
"Backslash: \\"            // String with an escaped backslash character.
```

#### 6. Exceptions

Errors will be raised for invalid string literals, including:
- Unclosed strings (missing closing quote).
- Invalid escape sequences (backslash not followed by a valid character).

#### 7. Conclusion

This specification provides comprehensive guidelines for parsing string literals in the Vein Language. By adhering to these rules, the Vein parser ensures correct and efficient interpretation of string literals, supporting a wide range of valid strings, including those with escaped sequences.