---
title: Numbers Math
---

# Numbers Math <Badge type="danger" text="supported only in 0.45 version" /> 

In this guide, we will discuss how to perform mathematical operations using the static `Math` class. Additionally, we will cover some of the most popular constants available in the `Math` class.

## Math Class

The `Math` class provides various methods for performing basic numeric operations such as absolute value, rounding, power, and trigonometric functions. It also includes several important mathematical constants.

## Mathematical Methods

### Basic Methods

#### Absolute Value

```vein
auto value: i32 = -42;
auto absValue: i32 = Math.abs(value);
Out.println(absValue); // Output: 42
```

#### Rounding

```vein
auto value: f64 = 3.14159;
auto roundedValue: f64 = Math.round(value);
Out.println(roundedValue); // Output: 3
```

#### Ceiling and Floor

```vein
auto value: f64 = 3.14;
auto ceilValue: f64 = Math.ceil(value);
auto floorValue: f64 = Math.floor(value);

Out.println(ceilValue);  // Output: 4
Out.println(floorValue); // Output: 3
```

#### Power and Square Root

```vein
auto base: f64 = 2.0;
auto exponent: f64 = 3.0;
auto power: f64 = Math.pow(base, exponent);
Out.println(power);  // Output: 8

auto value: f64 = 16.0;
auto sqrtValue: f64 = Math.sqrt(value);
Out.println(sqrtValue);  // Output: 4
```

#### Maximum and Minimum

```vein
auto a: i32 = 10;
auto b: i32 = 20;

auto maxValue: i32 = Math.max(a, b);
auto minValue: i32 = Math.min(a, b);

Out.println(maxValue);  // Output: 20
Out.println(minValue);  // Output: 10
```

### Trigonometric Methods

#### Sine, Cosine, and Tangent

```vein
auto angle: f64 = Math.PI / 4;  // 45 degrees

auto sinValue: f64 = Math.sin(angle);
auto cosValue: f64 = Math.cos(angle);
auto tanValue: f64 = Math.tan(angle);

Out.println(sinValue);  // Output: 0.70710678118
Out.println(cosValue);  // Output: 0.70710678118
Out.println(tanValue);  // Output: 1.0
```

#### Arcsine, Arccosine, and Arctangent

```vein
auto value: f64 = 0.70710678118;

auto asinValue: f64 = Math.asin(value);
auto acosValue: f64 = Math.acos(value);
auto atanValue: f64 = Math.atan(value);

Out.println(asinValue);  // Output: 0.78539816339  (PI/4)
Out.println(acosValue);  // Output: 0.78539816339  (PI/4)
Out.println(atanValue);  // Output: 0.61547970867
```

## Mathematical Constants

The `Math` class also provides several important mathematical constants:

- **Math.PI**: The ratio of the circumference of a circle to its diameter, approximately 3.14159.
- **Math.E**: The base of the natural logarithm, approximately 2.71828.
- **Math.LN2**: The natural logarithm of 2, approximately 0.69314.
- **Math.LN10**: The natural logarithm of 10, approximately 2.30258.
- **Math.LOG2E**: The base-2 logarithm of E, approximately 1.44269.
- **Math.LOG10E**: The base-10 logarithm of E, approximately 0.43429.
- **Math.SQRT2**: The square root of 2, approximately 1.41421.
- **Math.SQRT1_2**: The square root of 1/2, approximately 0.70710.

### Example: Using Constants

```vein
auto circumference: f64 = 2 * Math.PI * 10;  // Circumference of a circle with radius 10
Out.println(circumference);  // Output: 62.83185307179586

auto exponential: f64 = Math.exp(1);  // e^1
Out.println(exponential);  // Output: 2.718281828459045
```

## Conclusion

The `Math` class in Vein provides a comprehensive set of methods and constants for performing a wide range of mathematical operations. Understanding how to use these methods and constants can help you perform complex calculations more efficiently in your programs.