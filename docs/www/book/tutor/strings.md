---
title: Work with strings
---

# Work with strings

In this guide, we will explore the functional capabilities for working with strings in vein.            
We'll cover string concatenation, various methods provided by the `std` library, and template strings.      

## Methods provided by the std library

### string.format

The `string.format` method is used to create formatted strings. Here is an example:

```vein
auto name = "Alice";
auto age = 30;
auto formattedString = string.format("Name: {0}, Age: {1}", name, age);
Out.println(formattedString);  // Output: Name: Alice, Age: 30
```

### string.startsWith

The `string.startsWith` method checks if a string starts with a specified prefix:

```vein
auto str = "Hello, World!";
result = string.startsWith(str, "Hello");
Out.println(result);  // Output: true
```

### string.endsWith

The `string.endsWith` method checks if a string ends with a specified suffix:

```vein
auto str = "Hello, World!";
result = string.endsWith(str, "World!");
Out.println(result);  // Output: true
```

### string.contains

The `string.contains` method checks if a string contains a specified substring:

```vein
auto str = "Hello, World!";
result = string.contains(str, "World");
Out.println(result);  // Output: true
```

### string.equal

The `string.equal` method checks if two strings are equal:

```vein
auto str1 = "Hello";
auto str2 = "Hello";
result = string.equal(str1, str2);
Out.println(result);  // Output: true
```

## Template Strings <Badge type="warning" text="experimental" />  <Badge type="danger" text="supported only in 0.45 version" /> 

Template strings allow you to embed expressions within string literals using `{}` brackets and are prefixed with `!`. Here is an example:

```vein
auto name = "Alice";
auto age = 30;
auto templateString = !"{name} is {age} years old.";
Out.println(templateString);  // Output: Alice is 30 years old.
```

Template strings are particularly useful for creating complex strings that include multiple variables and expressions.

## Example: Combining Various String Operations

Let's combine all these concepts into a single example:

```vein
#use "std"

class Prog {
   master(): void {
      auto name = "Alice";
      auto age = 30;
      auto greeting = "Hello, " + name + "!";

      auto formatted = string.format("Name: {0}, Age: {1}", name, age);
      auto startsWithHello = string.startsWith(greeting, "Hello");
      auto endsWithExclamation = string.endsWith(greeting, "!");
      auto containsName = string.contains(greeting, name);
      auto equalCheck = string.equal(name, "Alice");

      auto templateString = !"{name} is {age} years old.";

      Out.println("Concatenated: " + greeting);
      Out.println("Formatted: " + formatted);
      Out.println("Starts with 'Hello': " + startsWithHello);
      Out.println("Ends with '!': " + endsWithExclamation);
      Out.println("Contains name: " + containsName);
      Out.println("Name equals 'Alice': " + equalCheck);
      Out.println("Template String: " + templateString);
   }
}
```

In this example:
- We use concatenation to create a greeting message.
- We format a string with `string.format`.
- We check if the greeting starts with "Hello" using `string.startsWith`.
- We check if the greeting ends with "!" using `string.endsWith`.
- We check if the greeting contains the name using `string.contains`.
- We check if the name is equal to "Alice" using `string.equal`.
- We create a template string that embeds variables within the string.

Each method and concept provides powerful ways to work with strings in vein.

## Conclusion

Now that you know how to work with strings, including concatenation, using various `std` library methods, and template strings, you can efficiently handle text in your programs. Experiment with these features to become more familiar with their capabilities.