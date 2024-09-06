---
title: Exceptions and Errors
---

# Exceptions and Errors

In this guide, we will cover how to handle exceptions and errors in Vein. Vein provides a robust exception handling mechanism through the use of `try`, `catch`, and `finally` blocks, as well as filters for catching specific exceptions based on conditions.

## Throwing Exceptions

Exceptions in Vein are thrown using the `fail` keyword followed by the `new` keyword to instantiate an exception type.

### Syntax

```vein
fail new ExceptionType();
```

### Example

```vein
class Example {
    demoMethod(): void {
        if (true) {
            fail new Exception("An error occurred.");
        }
    }
}
```

## Try, Catch, and Finally

Vein uses `try`, `catch`, and `finally` blocks to handle exceptions. The `try` block contains the code that may throw an exception, the `catch` block handles the exception, and the `finally` block contains code that is always executed, regardless of whether an exception was thrown or not.

### Syntax

```vein
try {
    // Code that may throw an exception
}
catch (e: ExceptionName) {
    // Handle specific exception
}
catch (e: Exception) {
    // Handle all exceptions
}
finally {
    // Code that will always execute
}
```

### Example

```vein
try {
    auto result = 10 / 0;
}
catch (e: DivideByZeroException) {
    Out.println("Caught a divide by zero exception.");
}
catch (e: Exception) {
    Out.println("Caught a general exception.");
}
finally {
    Out.println("This will always execute.");
}
```

## Exception Filters <Badge type="danger" text="supported only in 0.78 version" /> 

Vein supports exception filters, allowing you to catch specific exceptions based on additional conditions.

### Syntax

```vein
try {
    // Code that may throw an exception
}
catch (e: ExceptionName) when (condition) {
    // Handle specific exception based on condition
}
catch (e: Exception) {
    // Handle all exceptions
}
```

### Example

```vein
class CustomException : Exception {
    FooBar: i32;

    new(FooBar: i32) {
        this.FooBar = FooBar;
    }
}

try {
    fail new CustomException(1);
}
catch (e: CustomException) when (e.FooBar == 1) {
    Out.println("Caught CustomException with FooBar == 1");
}
catch (e: CustomException) when (e.FooBar == 2) {
    Out.println("Caught CustomException with FooBar == 2");
}
catch (e: Exception) {
    Out.println("Caught a general exception.");
}
finally {
    Out.println("This will always execute.");
}
```

In this example, the exception filter `when (e.FooBar == 1)` ensures that the `catch` block is executed only when the `FooBar` property of the `CustomException` is equal to 1.

## Best Practices

- **Catch specific exceptions first**: Always catch more specific exceptions before general ones to ensure more granular error handling.
- **Use `finally` for cleanup**: Utilize the `finally` block to release resources or perform necessary cleanup operations.
- **Log exceptions**: Log exceptions for debugging and monitoring purposes.

## Conclusion

Understanding how to handle exceptions and errors in Vein is crucial for writing robust and maintainable code. The combination of `try`, `catch`, and `finally` blocks, along with exception filters, provides a flexible and powerful error-handling mechanism.