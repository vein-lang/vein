---
title: Namespaces
---

# Namespaces

In this guide, we will cover the use of namespaces in Vein. Namespaces help organize and manage code by grouping related functionalities together. This is particularly useful in large projects to avoid naming conflicts.

## Declaring a Namespace

Namespaces in Vein are declared using the `#space` directive.

### Syntax

```vein
#space "my::namespace"

class MyClass {
    // Class definition
}
```

### Example

```vein
#space "example"

class Person {
    name: string;
    age: i32;

    new(name: string, age: i32) {
        this.name = name;
        this.age = age;
    }

    greet(): void {
        Out.println("Hello, my name is " + name + ".");
    }
}
```

In this example, the `Person` class is defined within the `example` namespace.

## Using a Namespace

To use types and functionalities from a specific namespace, you need to include it in your file using the `#use` directive.

### Syntax

```vein
#use "my::namespace"
```

### Example

```vein
#use "example"

auto person = Person("Alice", 25);
person.greet();  // Output: Hello, my name is Alice.
```

In this example, the `example` namespace is included, making the `Person` class available in this file.

## Namespace Hierarchies

Namespaces can be nested to create a hierarchical structure. This is useful for further organizing your code into subcategories.

### Example

```vein
#space "example::utils"

class MathUtils {
    static add(a: i32, b: i32): i32 {
        return a + b;
    }
}
```

```vein
#use "example::utils"

auto sum = MathUtils.add(3, 5);
Out.println(sum);  // Output: 8
```

In this example, the `MathUtils` class is defined within the `example::utils` namespace.

## Best Practices

- **Consistent Naming**: Use a consistent naming convention for namespaces to make your code easier to understand and maintain.
- **Logical Grouping**: Group related classes, interfaces, and functions within the same namespace.
- **Avoid Overlapping Names**: Use namespaces to avoid name conflicts, especially in larger projects or when using third-party libraries.

## Conclusion

Namespaces in Vein help you organize and manage your code, making it more readable and maintainable. By understanding how to declare and use namespaces, you can better structure your projects and avoid naming conflicts.