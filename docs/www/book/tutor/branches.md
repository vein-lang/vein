---
title: Branches
---

# Branches

In this guide, we will cover the basics of branching (conditional statements) in Vein. Branching is a fundamental concept in programming that allows you to make decisions in your code based on certain conditions.

## if Statement

The `if` statement is used to execute a block of code if a specified condition evaluates to `true`.

### Syntax

```vein
if (condition) {
    // Code to execute if condition is true
}
```

### Example

```vein
auto number: i32 = 10;

if (number > 5) {
    Out.println("Number is greater than 5");
}
```

## else if Statement

The `else if` statement is used to specify a new condition if the previous `if` condition was `false`.

### Syntax

```vein
if (condition1) {
    // Code to execute if condition1 is true
}
else if (condition2) {
    // Code to execute if condition2 is true
}
```

### Example

```vein
auto number: i32 = 10;

if (number > 15) {
    Out.println("Number is greater than 15");
}
else if (number > 5) {
    Out.println("Number is greater than 5 but less than or equal to 15");
}
```

## else Statement

The `else` statement is used to execute a block of code if none of the previous conditions were `true`.

### Syntax

```vein
if (condition1) {
    // Code to execute if condition1 is true
}
else if (condition2) {
    // Code to execute if condition2 is true
}
else {
    // Code to execute if none of the above conditions are true
}
```

### Example

```vein
auto number: i32 = 3;

if (number > 15) {
    Out.println("Number is greater than 15");
}
else if (number > 5) {
    Out.println("Number is greater than 5 but less than or equal to 15");
}
else {
    Out.println("Number is 5 or less");
}
```

## Nested if Statements

You can also nest `if` statements within other `if`, `else if`, or `else` blocks to create more complex conditions.

### Example

```vein
auto number: i32 = 10;
auto isPositive: bool = true;

if (number > 0) {
    if (isPositive) {
        Out.println("Number is positive and greater than 0");
    }
    else {
        Out.println("Logical error: A positive number cannot be non-positive.");
    }
}
else {
    Out.println("Number is less than or equal to 0");
}
```

## Conclusion

Using `if`, `else if`, and `else` statements allows you to control the flow of your program based on different conditions. Understanding how to use these branching statements will enable you to write more dynamic and complex code in Vein.