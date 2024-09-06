# String Class

The `String` class in Vein provides a comprehensive set of methods and operators for efficient string manipulation.     
It extends the `Object` class and provides various methods and operators to work with strings effectively.  
It includes functionalities for equality checks, concatenation, substring checks, and encoding support. The use of the garbage collector for buffer management ensures efficient memory handling during string manipulations.   

With this documentation, you should have a clear understanding of how to use the `String` class and manage string operations in Vein.       


## Properties

### `Length: i32`

The `Length` property returns the length of the string in terms of the number of characters.

## Methods

### `toString(): String`

Overrides the `toString` method from the `Object` class. Returns the string representation of the object.

### `equal(v1: string, v2: string): bool`

Checks if two strings are equal.

- `v1`: First string.
- `v2`: Second string.
  
Returns `true` if `v1` is equal to `v2`, otherwise `false`.

### `contains(v1: string, v2: string): bool`

Checks if the first string contains the second string as a substring.

- `v1`: The string to be searched.
- `v2`: The substring to search for.

Returns `true` if `v1` contains `v2`, otherwise `false`.

### `endsWith(v1: string, v2: string): bool`

Checks if the first string ends with the second string.

- `v1`: The string to be checked.
- `v2`: The suffix to check for.

Returns `true` if `v1` ends with `v2`, otherwise `false`.

### `startsWith(v1: string, v2: string): bool`

Checks if the first string starts with the second string.

- `v1`: The string to be checked.
- `v2`: The prefix to check for.

Returns `true` if `v1` starts with `v2`, otherwise `false`.

### `getSize(v1: string, encoding: i32): i32`

Gets the size of the string when encoded using the specified encoding.

- `v1`: The string whose size is to be calculated.
- `encoding`: The encoding format to use.

Returns the size of the string in bytes.

### `copyTo(v1: string, buffer: Span<u8>, encoding: i32): void`

Copies the string into the provided buffer using the specified encoding.

- `v1`: The string to be copied.
- `buffer`: The buffer where the string will be copied.
- `encoding`: The encoding format to use.

### `createFrom(buffer: Span<u8>, encoding: i32): string`

Creates a string from the given buffer using the specified encoding.

- `buffer`: The buffer containing the string data.
- `encoding`: The encoding format to use.

### `createFrom(buffer: Span<u8>, size: i32, encoding: i32): string`

Creates a string from the given buffer using the specified encoding and specified size.

- `buffer`: The buffer containing the string data.
- `size`: The size containing the string data in buffer.
- `encoding`: The encoding format to use.

## Operators

### `op_Add(v1: string, v2: string): string`

Concatenates two strings.

- `v1`: The first string.
- `v2`: The second string.

Returns the concatenated result of `v1` and `v2`.

### `op_Add(v1: string, v2: i32): string`

Concatenates a string with an integer.

- `v1`: The string.
- `v2`: The integer.

Returns the concatenated result of `v1` and `v2`.

### `op_NotEqual(v1: string, v2: string): bool`

Checks if two strings are not equal.

- `v1`: The first string.
- `v2`: The second string.

Returns `true` if `v1` is not equal to `v2`, otherwise `false`.

### `op_Equal(v1: string, v2: string): bool`

Checks if two strings are equal.

- `v1`: The first string.
- `v2`: The second string.

Returns `true` if `v1` is equal to `v2`, otherwise `false`.


## Supported Encodings

The `encoding` parameter in the `getSize`, `copyTo`, and `createFrom` methods denotes the encoding scheme to be used.   
The following encoding schemes are supported:   

- `0`: UTF-32
- `1`: Unicode (UTF-16)
- `2`: Big Endian Unicode (UTF-16)
- `3`: UTF-8
- `4`: ASCII
- `5`: ISO-8859-1

## Usage Example

Here is an example of how you can use the `String` class:

```vein
#space "std"

public class Example {
    public static master(): void {
        auto str1: string = "Hello";
        auto str2: string = "World";
        
        // Concatenate strings
        auto concatenated: string = String.op_Add(str1, str2);
        
        // Check if strings are equal
        auto isEqual: bool = String.op_Equal(str1, "Hello");
        
        // Use the contains method
        auto containsSubstr: bool = String.contains(concatenated, "loWo");
        
        // Allocate buffer and copy string into it
        auto buffer: Span<u8> = GC.allocate_u8(String.getSize(concatenated, 1));
        String.copyTo(concatenated, buffer, 1);
        
        // Create string from buffer
        auto newString: string;
        String.createFrom(buffer, 1);
        
        // Free the allocated buffer
        GC.free_span(buffer);
        
        println(newString);
    }
}
```

In this example:

1. Two strings are created and concatenated.
2. The equality of two strings is checked.
3. The `contains` method is used to check for a substring.
4. A buffer is allocated using `GC.allocate_u8(size)`, and the concatenated string is copied into it.
5. A new string is created from the buffer using `String.createFrom(buffer, encoding)`.
6. The buffer is freed using `GC.free_span(buffer)` after its use.

