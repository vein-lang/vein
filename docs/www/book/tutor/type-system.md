---
title: Type System.
description: Vein Type System
---
# Type System

In this guide, we will cover the standard type system in Vein.    
Understanding the type system is crucial for writing robust and efficient code.  
We will discuss classes, abstract classes, interfaces, structures, and related functionalities. 


## Constructors

Constructors are declared using the `new` keyword.

### Syntax

```vein
class Example {
    value: i32;

    // Default constructor
    new() {
        value = 0;
    }

    // Parameterized constructor
    new(i: i32) {
        value = i;
    }
}
```

In this example:
- `new()` is the default constructor with no parameters.
- `new(i: i32)` is a parameterized constructor that initializes the `value` field with the provided argument.

## Classes

Classes are the fundamental building blocks in Vein. They can encapsulate data and behavior.

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
      Out.println("Hello, my name is " + name + ".");
   }
}

auto person = Person("Alice", 30);
person.greet();  // Output: Hello, my name is Alice.
```

### Object

`Object` is the base type of all classes. All classes implicitly inherit from `Object`.

## Abstract Classes

Abstract classes cannot be instantiated directly. They are designed to be subclassed, and they can contain abstract methods that must be implemented by subclasses.

### Example

```vein
abstract class Animal {
   abstract makeSound(): void;

   move(): void {
      Out.println("The animal moves.");
   }
}

class Dog : Animal {
   makeSound(): void {
      Out.println("Woof!");
   }
}

auto dog = Dog();
dog.makeSound();  // Output: Woof!
dog.move();       // Output: The animal moves.
```

## Interfaces <Badge type="warning" text="experimental" /> <Badge type="danger" text="Currently interface feature has been disabled" />

Interfaces define a contract that implementing classes must fulfill. They can be inherited multiple times.

### Example

```vein
interface Drivable {
   drive(): void;
}

class Car : Drivable {
   drive(): void {
      Out.println("The car is driving.");
   }
}

auto car = Car();
car.drive();  // Output: The car is driving.
```

## Structures

Structures are value types and can be used to encapsulate small data objects. Unlike classes, structures cannot be inherited.

### Example

```vein
struct Point : ValueType {
   x: i32;
   y: i32;
   
   new(x: i32, y: i32) {
      this.x = x;
      this.y = y;
   }

   display(): void {
      Out.println("Point(" + x + ", " + y + ")");
   }
}

auto point = Point(3, 4);
point.display();  // Output: Point(3, 4)
```

### ValueType

`ValueType` is the base type of all structures. Structures are sealed and cannot be inherited.

## Type Operators

### typeof\<T\>()

The `typeof<T>()` operator returns information about the type `T`.

### Example

```vein
auto typeInfo = typeof<Person>();
Out.println(typeInfo);  // Output: Person
```

### is\<T\>(value)

The `is<T>(value)` operator checks whether `value` is of type `T` or inherits from `T`.

### Example

```vein
auto person = Person("Alice", 30);
auto isPerson = is<Person>(person);

Out.println(isPerson);  // Output: true
```

## Conclusion

Understanding the type system in Vein, including classes, abstract classes, interfaces, and structures, is key to writing effective programs. The `Object` and `ValueType` base types provide a foundation for all types in Vein, while operators like `typeof<T>()` and `is<T>(value)` offer powerful ways to work with types dynamically.