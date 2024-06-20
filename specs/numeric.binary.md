### ECMA Specification for Vein Language Binary Literal Expression

#### 1. Introduction

This section defines the behavior for parsing binary literal expressions in the Vein Language. Binary literals represent numeric values using the binary (base-2) numeral system, with optional type suffixes indicating integer types.

#### 2. Binary Literal Definition

A binary literal in the Vein Language begins with `0b` or `0B`, followed by a sequence of binary digits (`0` or `1`). Underscores (`_`) may be used as digit separators for readability. An optional type suffix may follow the binary digits to indicate the specific integer type, such as unsigned or long.

#### 3. Detailed Grammar

##### Binary Literal

The basic syntax for a binary literal is:
```
BinaryLiteral ::= '0' ('b' | 'B') BinaryDigits IntegerTypeSuffix?
BinaryDigits ::= (BinaryDigit | '_')* BinaryDigit
BinaryDigit ::= '0' | '1'
IntegerTypeSuffix ::= (UnsignedSuffix LongSuffix?) | (LongSuffix UnsignedSuffix?)
UnsignedSuffix ::= 'u' | 'U'
LongSuffix ::= 'l' | 'L'
```

Where:
- **BinaryLiteral**: Starts with `0b` or `0B`.
- **BinaryDigits**: A sequence of binary digits (`0` or `1`), potentially separated by underscores (`_`).
- **IntegerTypeSuffix**: An optional suffix that specifies the integer type (e.g., unsigned or long).

#### 4. Parsing Implementation

The parser rule for `BinaryLiteralExpression` is structured as follows:

```csharp
protected internal virtual Parser<NumericLiteralExpressionSyntax> BinaryLiteralExpression =>
    from zero in Parse.Char('0')
    from control in Parse.Chars("Bb")
    from chain in Parse.Char('_').Many().Then(_ => Parse.Chars("01")).AtLeastOnce().Text()
    from suffix in IntegerTypeSuffix.Optional()
    select FromBinary(chain.Replace("_", ""), suffix.GetOrDefault());
```

#### 5. Type Suffix Parsing Implementation

The parser rule for `IntegerTypeSuffix` handles optional type suffixes:

```csharp
private Parser<NumericSuffix> IntegerTypeSuffix =>
    (from l in Parse.Chars("lL").Optional()
     from u in Parse.Chars("uU")
     select l.IsDefined ? NumericSuffix.Long | NumericSuffix.Unsigned : NumericSuffix.Unsigned).Or(
        from u in Parse.Chars("uU").Optional()
        from l in Parse.Chars("lL")
        select u.IsDefined ? NumericSuffix.Unsigned | NumericSuffix.Long : NumericSuffix.Long);
```

### Implementation Details:

1. **Leading Zero**: 
   - `Parse.Char('0')` matches the leading zero required for binary literals.
   
2. **Binary Prefix**: 
   - `Parse.Chars("Bb")` matches the character `b` or `B`, indicating a binary literal.

3. **Binary Digits**:
   - `Parse.Char('_').Many().Then(_ => Parse.Chars("01")).AtLeastOnce().Text()` matches a sequence of binary digits (`0` or `1`), allowing underscores as separators. The `Text()` method converts the sequence to a string.

4. **Optional Integer Type Suffix**: 
   - `IntegerTypeSuffix.Optional()` matches an optional type suffix, which can either be:
     - Unsigned suffix (e.g., `u` or `U`)
     - Long suffix (e.g., `l` or `L`)
     - Combination of both (e.g., `ul`, `lu`, `Ul`, `LU`)

5. **Literal Conversion**:
   - `select FromBinary(chain.Replace("_", ""), suffix.GetOrDefault())` processes the parsed binary digits, removing underscores and converting the resulting number to a numeric literal expression with the specified suffix.

### `FromBinary` Function

The function `FromBinary` is responsible for converting the binary string to a numeric value based on the optional type suffix and encapsulating it into a `NumericLiteralExpressionSyntax` object.

```csharp
private NumericLiteralExpressionSyntax FromBinary(string binaryDigits, NumericSuffix suffix)
{
    // Conversion and encapsulation logic here, transforming binaryDigits to appropriate numeric type and handling suffix.
}
```

### NumericSuffix Enum

The `NumericSuffix` enum is used to represent possible combinations of integer type suffixes:

```csharp
[Flags]
public enum NumericSuffix
{
    None = 0,
    Unsigned = 1 << 0,
    Long = 1 << 1
}
```

#### 6. Examples

Valid binary literals:

```vein
0b1010               // Binary literal for decimal 10.
0B1101_0010          // Binary literal with an underscore separator for readability.
0b101010u            // Binary literal with an unsigned integer type suffix.
0B11110000L          // Binary literal with a long integer type suffix.
0b11111010uL         // Binary literal with both unsigned and long suffix.
```

#### 7. Exceptions

Errors will be raised for invalid binary literals, such as:
- Missing or incorrect binary prefix (`0b` or `0B`).
- No binary digits following the prefix.
- Invalid characters in the binary digit sequence (non-`0` or `1` characters).
- Incorrect placement of underscores (e.g., leading or trailing underscores, consecutive underscores).

#### 8. Conclusion

This specification details the rules and parsing behaviors for binary literal expressions in the Vein Language. By adhering to these guidelines, the Vein parser ensures accurate interpretation of binary values with support for optional integer type suffixes, enhancing the language's ability to handle various numeric expressions seamlessly.