---
title: Primitives
---

# Primitives

In this guide, we will cover the basic primitive types available in your language, Vein. Primitives are the fundamental data types that are used to represent simple values.

## Integer Types

### Signed Integers

- **i8 (alias SByte)**: 8-bit signed integer.
- **i16**: 16-bit signed integer.
- **i32**: 32-bit signed integer.
- **i64**: 64-bit signed integer.
- **i128**: 128-bit signed integer.

### Unsigned Integers

- **u8 (alias Byte)**: 8-bit unsigned integer.
- **u16**: 16-bit unsigned integer.
- **u32**: 32-bit unsigned integer.
- **u64**: 64-bit unsigned integer.
- **u128**: 128-bit unsigned integer.

## Floating-Point Types

- **f16 (alias Half)**: 16-bit floating-point number.
- **f32 (Float)**: 32-bit floating-point number.
- **f64 (Double)**: 64-bit floating-point number.
- **f128 (Decimal)**: 128-bit floating-point number.

## Boolean Type

- **bool**: Represents a boolean value, which can be either `true` or `false`.

## String Type

- **string**: Represents a sequence of characters.

## Void Type

- **void**: Represents the absence of a value. Typically used in methods that do not return a value.

## Raw Pointer Type

- **raw** `(alias intptr)`: Represents a raw pointer to a memory address.

## Range Structure

- **range** `alias Range`: A structure containing two numbers, representing a range from `start` to `end`.

## Span\<T\> Structure

- **span\<T\>** `alias Span<T>`: A structure referencing a raw slice of memory. Often used for working with arrays or buffers.

## TimeSpan Structure

- **TimeSpan**: A structure representing a unit of time, e.g., 1 hour, 32 seconds, 672 milliseconds.

## System Types

- **ValueType**: The system type that all structures inherit from.
- **Object**: The system type that all classes inherit from.
- **Exception**: The base type for all exceptions and errors.

## Array and Collections

- **Array\<Any\>**: A generic array that can hold elements of any type.

## Guid

- **Guid**: A globally unique identifier (UUID) version 4.

# Examples

Here are some examples of how to declare and use these primitive types:

### Integer Example

```vein
auto age = 30;  // i32 by default

auto smallNumber: i8 = -128;
auto byteValue: u8 = 255;

auto shortNumber: i16 = -32768;
auto ushortNumber: u16 = 65535;

auto integerNumber: i32 = 2147483647;
auto uintNumber: u32 = 4294967295;

auto longNumber: i64 = 9223372036854775807;
auto ulongNumber: u64 = 18446744073709551615;

auto bigNumber: i128 = 170141183460469231731687303715884105727;
auto ubigNumber: u128 = 340282366920938463463374607431768211455;
```

::: warning Attention! 
Currently i8 (signed byte), u128, i128, u256, i256, u512, i512, f128, f256 number is not implemented
:::


### Floating-Point Example

```vein
auto halfPrecision: f16 = 1.5h;  // Half-precision float
auto floatNumber: f32 = 3.14f;   // Single-precision float
auto doubleNumber: f64 = 3.1415d;  // Double-precision float
auto decimalNumber: f128 = 3.1415926535m;  // Decimal precision float
```


::: warning Attention! 
Currently decimal floating point number is not implemented fully
:::


### Boolean, String, and Void Example

```vein
auto isActive: bool = true;
auto greeting: string = "Hello, World!";
```

### Range Example <Badge type="warning" text="experimental" /> <Badge type="danger" text="not fully implemented at the moment" /> 

```vein
auto myRange: range = 1..10;
```

### Span Example  <Badge type="warning" text="experimental" /> <Badge type="danger" text="not fully implemented at the moment" /> 

```vein
auto intSpan = span.allocate{0,1,2,3,4};
```

### TimeSpan Example

```vein
auto duration: TimeSpan = 
    new TimeSpan(hours: 1, minutes: 32, seconds: 45, milliseconds: 672);
```

### Array Example <Badge type="warning" text="experimental" />

```vein
auto messages = new Array<string>() ["Hello", "World"];
```

### Guid Example

```vein
auto myGuid = new Guid();
```

## Conclusion

Understanding these primitive types is crucial for effective programming in Vein.   
They form the building blocks for more complex data structures and operations in your programs. 