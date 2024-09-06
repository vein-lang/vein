# Regex Class Documentation

The `Regex` class provides a set of static methods for working with regular expressions, 
including escaping/unescaping strings, counting matches, checking for matches, and replacing text based on patterns. 
These methods facilitate working with regular expressions in a convenient and efficient manner.

## Methods

### `escape`

```vein
public static escape(str: string): string
```

**Description:**

Escapes a string to safely use it within a regular expression.

**Parameters:**

- `str` (_string_): The string to escape.

**Returns:**

- (_string_): The escaped string.

**Example:**

```vein
auto escapedStr = Regex.escape("special*characters?");
Out.println(escapedStr);
```

### `unescape`

```vein
public static unescape(str: string): string
```

**Description:**

Unescapes a string from a regular expression.

**Parameters:**

- `str` (_string_): The string to unescape.

**Returns:**

- (_string_): The unescaped string.

**Example:**

```vein
auto unescapedStr = Regex.unescape("special\\*characters\\?");
Out.println(unescapedStr);
```

### `count`

```vein
public static count(pattern: string, value: string): i32
```

**Description:**

Counts the number of matches of the specified pattern in the given value.

**Parameters:**

- `pattern` (_string_): The regular expression pattern to match.
- `value` (_string_): The string to search for matches.

**Returns:**

- (_i32_): The number of matches found.

**Example:**

```vein
auto matchCount = Regex.count("\\d+", "There are 24 apples and 42 oranges.");
Out.println(matchCount);
```

### `isMatch`

```vein
public static isMatch(pattern: string, value: string): bool
```

**Description:**

Checks if the specified pattern matches any part of the given value.

**Parameters:**

- `pattern` (_string_): The regular expression pattern to match.
- `value` (_string_): The string to search for matches.

**Returns:**

- (_bool_): `true` if a match is found, `false` otherwise.

**Example:**

```vein
auto isMatching = Regex.isMatch("\\d+", "There are 24 apples and 42 oranges.");
Out.println(isMatching); // true
```

### `replace`

```vein
public static replace(pattern: string, value: string, replacement: string): bool
```

**Description:**

Replaces all occurrences of the specified pattern in the given value with the replacement string.

**Parameters:**

- `pattern` (_string_): The regular expression pattern to match.
- `value` (_string_): The string in which to replace matches.
- `replacement` (_string_): The replacement string.

**Returns:**

- (_bool_): `true` if replacements were made, `false` otherwise.

**Example:**

```vein
auto result = Regex.replace("\\d+", "There are 24 apples and 42 oranges.", "many");
Out.println(result); // "There are many apples and many oranges."
```

## Example Usage

### Escape and Unescape Strings

```vein
auto specialPattern = ".*+?^${}()|[]\\";
auto escapedPattern = Regex.escape(specialPattern);
Out.println(escapedPattern); 

auto unescapedPattern = Regex.unescape(escapedPattern);
Out.println(unescapedPattern);
```

### Counting Matches

```vein
auto text = "The rain in Spain falls mainly in the plain.";
auto wordCount = Regex.count("\\b\\w+\\b", text);
Out.println(wordCount); // Number of words
```

### Checking for Matches

```vein
auto text = "Sample text with numbers 1234 and 5678.";
auto hasNumbers = Regex.isMatch("\\d+", text);
Out.println(hasNumbers); // true
```

### Replacing Matches

```vein
auto text = "Apples are $1, oranges are $2.";
auto newText = Regex.replace("\\$\\d+", text, "$3");
Out.println(newText); // "Apples are $3, oranges are $3."
```