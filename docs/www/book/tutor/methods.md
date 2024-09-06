---
title: Methods and Operators
---

# Methods and Operators

In this guide, we will cover the basics of methods, constructors, operator overloading, and access modifiers in Vein. Understanding these concepts allows you to create more expressive and flexible code.

## Methods

Methods are functions defined within a class or structure. They encapsulate behavior that can be invoked on objects or the class itself.

### Instance Methods

Instance methods are associated with an instance of a class. They can access instance variables and other instance methods.

### Syntax

```vein
class MyClass {
    methodName(parameters): returnType {
        // Method body
    }
}
```

### Example

```vein
class Person {
    name: string;
    age: i32;

    new(name: string, age: i32) {
        this.name = name;
        this.age = age;
    }

    greet(): void {
        Out.println("Hello, my name is " + this.name);
    }
}

auto person = Person("Alice", 30);
person.greet();  // Output: Hello, my name is Alice.
```

In this example, `greet` is an instance method of the `Person` class.

### Static Methods

Static methods are associated with the class itself rather than an instance. They can access only static variables and other static methods.

### Syntax

```vein
class MathUtils {
    static add(a: i32, b: i32): i32 {
        return a + b;
    }
}
```

### Example

```vein
auto sum = MathUtils.add(3, 4);
Out.println(sum);  // Output: 7
```

In this example, `add` is a static method of the `MathUtils` class.

## Constructors

Constructors are special methods used to initialize objects. Constructors are declared using the `new` keyword.

### Syntax

```vein
class MyClass {
    new() {
        // Default constructor
    }

    new(parameters) {
        // Parameterized constructor
    }
}
```

### Example

```vein
class Product {
    name: string;
    price: f64;

    new() {
        this.name = "Unnamed";
        this.price = 0.0;
    }

    new(name: string, price: f64) {
        this.name = name;
        this.price = price;
    }
}

auto defaultProduct = Product();
auto namedProduct = Product("Laptop", 999.99);

Out.println("Default Product: " + defaultProduct.name + ", " + defaultProduct.price);  // Output: Default Product: Unnamed, 0.0
Out.println("Named Product: " + namedProduct.name + ", " + namedProduct.price);        // Output: Named Product: Laptop, 999.99
```

In this example, `Product` has both a default constructor and a parameterized constructor.

## Short Form Method Declaration

Vein also supports a short form for method declarations, similar to the arrow functions in C#.

### Syntax

```vein
methodName(parameters): returnType |> expression;
```

### Example

```vein
class Example {
    test(): i32 |> 1;

    add(a: i32, b: i32): i32 |> a + b;
}

auto example = Example();
Out.println(example.test());   // Output: 1
Out.println(example.add(4, 3)); // Output: 7
```

In this example:
- `test` is a method that returns `1` using the short form declaration.
- `add` is a method that returns the sum of `a` and `b` using the short form declaration.

## Operator Overloading

Operator overloading allows you to define custom behavior for operators when they are applied to instances of your classes. This can make your classes more intuitive to use.

### Syntax

```vein
public operator ==(lhs: FooBarClass, rhs: FooBarClass): bool {
    // Operator body
}
```

### Example

```vein
class Point {
    x: i32;
    y: i32;

    new(x: i32, y: i32) {
        this.x = x;
        this.y = y;
    }

    public operator ==(lhs: Point, rhs: Point): bool {
        return lhs.x == rhs.x && lhs.y == rhs.y;
    }

    public operator +(lhs: Point, rhs: Point): Point {
        return Point(lhs.x + rhs.x, lhs.y + rhs.y);
    }
}

auto p1 = Point(3, 4);
auto p2 = Point(3, 4);
auto p3 = Point(1, 2);

Out.println(p1 == p2);  // Output: true
Out.println(p1 == p3);  // Output: false

auto p4 = p1 + p3;
Out.println("p4: (" + p4.x + ", " + p4.y + ")");  // Output: p4: (4, 6)
```

In this example, the `==` operator is overloaded to compare two `Point` objects for equality based on their coordinates. The `+` operator is also overloaded to add the coordinates of two `Point` objects.

## Access Modifiers

Access modifiers control the visibility and accessibility of classes, methods, and fields. The following are the available access modifiers in Vein:

- `public`: The member is accessible from any code. This is the default modifier if none is specified.
- `protected`: The member is accessible within its own class and by derived class instances.
- `private`: The member is accessible only within its own class or structure.
- `static`: The member belongs to the class itself rather than to any specific object instance.
- `extern`: The member is provided by an external native library.
- `internal`: The member is accessible only within the same module.
- `override`: Indicates that a method overrides a base class method.
- `virtual`: The member can be overridden by derived classes.
- `readonly`: The member can be assigned a value only during its declaration or in a constructor in the same class.
- `abstract`: The member has no implementation in the base class and must be overridden in derived classes.
- `async`: The member supports asynchronous operations.
- `global`: Applicable only to type aliases, making them visible in all modules.
- `operation`: Applicable only to quantum operation functions.

### Example of Access Modifiers

```vein
#use "native("libname.dll", "method_name")"

class Example {
    public publicField: i32;
    protected protectedField: i32;
    private privateField: i32;

    static staticField: i32;

    public new() {
        this.publicField = 1;
        this.protectedField = 2;
        this.privateField = 3;
        self.staticField = 4;
    }

    public method(): void {
        Out.println("Public method");
    }

    protected method(): void {
        Out.println("Protected method");
    }

    private method(): void {
        Out.println("Private method");
    }

    static method(): void {
        Out.println("Static method");
    }

    [native("libname.dll", "method_name")]
    public static extern foobar(i: i32): bool;
}
```

In this example:
- `publicField` is accessible from any code.
- `protectedField` is accessible within `Example` and derived classes.
- `privateField` is accessible only within `Example`.
- `staticField` and `method` belong to the class itself.
- `foobar` is an external function provided by a native library.

## Conclusion

Understanding how to declare methods, constructors, and operator overloading in Vein is essential for writing expressive and maintainable code. Access modifiers provide control over the visibility and accessibility of your class members. By using these features appropriately, you can write more robust and flexible code.