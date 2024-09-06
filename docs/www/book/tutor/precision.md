---
title: Numbers Precision
---

# Numbers Precision

In this guide, we will discuss the limits and precision of numbers in Vein. We will also cover some common pitfalls when working with floating-point numbers, such as precision errors.

## Limits of Number Types

Different number types in Vein have different limits and precision characteristics. Here is a summary:

### Integer Types

#### Signed Integers

- **i8 (SByte)**: Range from `-128` to `127`.
- **i16**: Range from `-32768 to 32767`.
- **i32**: Range from `-2_147_483_648 to 2_147_483_647`.
- **i64**: Range from `-9_223_372_036_854_775_808 to 9_223_372_036_854_775_807`.
- **i128**: Range from `-170_141_183_460_469_231_731_687_303_715_884_105_728 to 170_141_183_460_469_231_731_687_303_715_884_105_727`.

#### Unsigned Integers

- **u8 (Byte)**: Range from `0 to 255`.
- **u16**: Range from `0 to 65_535`.
- **u32**: Range from `0 to 4_294_967_295`.
- **u64**: Range from `0 to 18_446_744_073_709_551_615`.
- **u128**: Range from `0 to 340_282_366_920_938_463_463_374_607_431_768_211_455`.

### Floating-Point Types

- **f16 (Half)**: Approximate range from `6.10e-5 to 6.55e4`, with 3-4 decimal digits of precision.
- **f32 (Float)**: Approximate range from `1.18e-38 to 3.40e38`, with 6-7 decimal digits of precision.
- **f64 (Double)**: Approximate range from `2.23e-308 to 1.79e308`, with 15-16 decimal digits of precision.
- **f128 (Decimal)**: Approximate range from `1.0e-6145 to 7.9e6145`, with 33-34 decimal digits of precision.

::: warning Attention! 
Currently i8 (signed byte), u128, i128, u256, i256, u512, i512, f128, f256 number is not implemented
:::


## Common Pitfalls with Floating-Point Arithmetic

Floating-point numbers are an approximation and thus can lead to precision errors. A well-known example of this is:

```vein
auto a: f64 = 0.1;
auto b: f64 = 0.2;
auto sum: f64 = a + b;

Out.println(sum == 0.3);  // Output: false
Out.println(sum);         // Output: 0.30000000000000004
```

### Why Does This Happen?

The precision error occurs because floating-point numbers are represented in binary, and many decimal fractions (like 0.1 and 0.2) cannot be represented exactly in binary.         
As a result, small errors accumulate, leading to unexpected behavior.       


### How to Mitigate Precision Errors 

1. **Use Decimal for Financial Calculations**: When dealing with financial calculations or other applications that require high precision, consider using `f128 (Decimal)`.
   
2. **Rounding**: Explicitly round numbers to a fixed number of decimal places if exact precision is required for comparisons.

```vein
auto a: f64 = 0.1;
auto b: f64 = 0.2;
auto sum: f64 = a + b;

auto isEqual: bool = (Math.round(sum * 1e10) / 1e10) == 0.3;
Out.println(isEqual);  // Output: true
```

::: warning Attention! 
Currently decimal floating point number is not implemented fully
:::

3. **Tolerance for Comparisons**: Use a small tolerance value for equality comparisons.

```vein
auto tolerance: f64 = 1e-10;
auto isEqual: bool = Math.abs(sum - 0.3) < tolerance;
Out.println(isEqual);  // Output: true
```

## Conclusion

Understanding the limits of different numeric types and the inherent precision issues with floating-point arithmetic is crucial for writing robust programs. Always consider these factors when designing algorithms that involve numeric computations.