---
title: This and Self
description: This and Self
---

# This and Self

In this guide, we will cover the usage of `this` and `self` keywords in Vein.   
These keywords are used to refer to the current instance of a class and the static context of a class, respectively.    

## this Keyword

The `this` keyword is used to refer to the current instance of the class. It is commonly used within instance methods and constructors to access fields, properties, and other methods of the current object.

### Example

```vein
class Person {
    name: string;
    age: i32;

    new(name: string, age: i32) {
        this.name = name;
        this.age = age;
    }

    introduce(): void {
        Out.println("Hello, my name is " + this.name + " and I am " + this.age + " years old.");
    }
}

auto person = Person("Alice", 30);
person.introduce();  // Output: Hello, my name is Alice and I am 30 years old.
```

In this example, `this` is used in the constructor and the `introduce` method to refer to the current instance of the `Person` class.

## self Keyword

The `self` keyword is used to refer to the static context of the class. It allows you to access static fields, properties, and methods without using the class name.

### Example

```vein
class Calculator {
    static pi: f64 = 3.14159;

    static areaOfCircle(radius: f64): f64 {
        return self.pi * radius * radius;
    }

    static circumferenceOfCircle(radius: f64): f64 {
        return 2 * self.pi * radius;
    }
}

auto radius: f64 = 5.0;
Out.println("Area: " + Calculator.areaOfCircle(radius));          // Output: Area: 78.53975
Out.println("Circumference: " + Calculator.circumferenceOfCircle(radius));  // Output: Circumference: 31.4159
```

In this example, `self` is used to refer to the static variable `pi` within static methods of the `Calculator` class.

## Using this and self together

In some scenarios, you might need to use both `this` and `self` to differentiate between instance and static context within the same class.

### Example

```vein
class Counter {
    count: i32 = 0;
    static totalCount: i32 = 0;

    new() {
        self.totalCount = self.totalCount + 1;
    }

    increment(): void {
        this.count = this.count + 1;
        Out.println("Instance count: " + this.count);
        Out.println("Total count: " + self.totalCount);
    }
}

auto counter1 = Counter();
counter1.increment();  // Output: Instance count: 1, Total count: 1

auto counter2 = Counter();
counter2.increment();  // Output: Instance count: 1, Total count: 2
```

In this example, `this` is used to refer to the instance variable `count`, while `self` is used to refer to the static variable `totalCount`.

## Conclusion

Understanding the `this` and `self` keywords is essential for effective programming in Vein. The `this` keyword allows you to access the current instance of a class, while the `self` keyword provides a convenient way to access static members within the class. By using these keywords appropriately, you can write more readable and maintainable code.