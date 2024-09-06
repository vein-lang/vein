---
title: Declare and use variables
---

# Declare and use variables

In this guide, we will learn how to declare and use variables in vein.

## Variable Declaration

Variables in vein can be declared using the `auto` keyword, which deduces the type of the variable from the value it is initialized with.      
Here are some examples:

### Example 1: Integer Variable

```vein
auto a = 1;
```

In this example, `a` is an integer variable initialized to `1`.

### Example 2: String Variable

```vein
auto greeting = "Hello, World!";
```

Here, `greeting` is a string variable initialized to `"Hello, World!"`.

### Example 3: Double Variable

```vein
auto pi = 3.14159d;
```

In this example, `pi` is a double variable initialized to `3.14159d`.

### Example 4: Boolean Variable

```vein
auto isActive = true;
```

Here, `isActive` is a boolean variable initialized to `true`.

## Usage of Variables

Once you declare a variable, you can use it in your code.       
Here's an example of using variables:

### Example: Using Variables in a Class

```vein
#use "std"

class Prog {
   master(): void {
      auto message = "Hello, World!";
      auto number = 42;
      auto isActive = true;

      Out.println(message);
      Out.println("Number: " + number);
      Out.println("Active: " + isActive);
   }
}
```

In this example:
- `message` is a string variable.
- `number` is an integer variable.
- `isActive` is a boolean variable.

The `Out.println` method is used to print the values of these variables.

## Best Practices

- **Use meaningful variable names**: Choose names that clearly indicate the purpose of the variable.
- **Keep scope in mind**: Declare variables in the smallest scope possible to improve readability and maintainability.
- **Consistency**: Stick to a consistent naming convention throughout your codebase.

## Conclusion

Now that you know how to declare and use variables, you can begin incorporating them into your programs.        
Experiment with different data types and operations to get a feel for how variables work in vein.