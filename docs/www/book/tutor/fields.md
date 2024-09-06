---
title: Fields and Properties
---

# Fields and Properties

In this guide, we will cover the basics of fields and properties in Vein. Fields and properties are fundamental concepts used to encapsulate data within classes and structures.

## Fields

Fields are variables that are declared directly within a class or structure. They are used to store data that belongs to the object.

### Syntax

```vein
class Person {
    name: string;
    age: i32;

    new(name: string, age: i32) {
        this.name = name;
        this.age = age;
    }
}
```

### Example

```vein
class Car {
    make: string;
    model: string;
    year: i32;

    new(make: string, model: string, year: i32) {
        this.make = make;
        this.model = model;
        this.year = year;
    }
}

auto car = Car("Toyota", "Camry", 2020);
Out.println("Make: " + car.make);   // Output: Make: Toyota
Out.println("Model: " + car.model); // Output: Model: Camry
Out.println("Year: " + car.year);   // Output: Year: 2020
```

In this example, `make`, `model`, and `year` are fields of the `Car` class.

## Properties <Badge type="warning" text="experimental" />

Properties provide a way to encapsulate fields and add logic for getting and setting their values. They can be used to enforce access control and validate data before it is assigned to a field.

### Syntax

```vein
class Person {
    public name: string { get; set; }
    public age: i32 |> this._age;

    private _age: i32;

    new(name: string, age: i32) {
        this.name = name;
        this._age = age;
    }
}
```

### Example

```vein
class Person {
    public name: string { 
        get { get; set; }
        set {
            if (value.length == 0) {
                throw Exception("Name cannot be empty");
            }
            this.name = value;
        }
    }
    public age: i32 { 
        get; 
        set 
        {
            if (value < 0) {
                throw Exception("Age cannot be negative");
            }
            this.age = value;
        } 
    }

    new(name: string, age: i32) {
        this.name = name;
        this.age = age;
    }
}

auto person = Person("Alice", 30);
Out.println("Name: " + person.name);  // Output: Name: Alice
Out.println("Age: " + person.age);    // Output: Age: 30

person.name = "Bob";

Out.println("Updated Name: " + person.name);  // Output: Updated Name: Bob
// Age property has only a getter, so it cannot be updated directly.
```

In this example, `name` is a property with both a getter and a setter, and `age` is a read-only property with only a getter. The `set` method provides logic for validating the name and age before they are assigned.

## Best Practices

- **Use fields for internal data storage**: Fields should generally be private and only accessed via properties.
- **Use properties to encapsulate data**: Properties provide a flexible way to enforce validation and access control.
- **Consistent naming conventions**: Use consistent naming conventions for fields and properties to make your code more readable.

## Conclusion

Understanding fields and properties is essential for effective programming in Vein. Fields provide a way to store data, while properties offer a flexible mechanism for data encapsulation and validation. By using these features appropriately, you can write more maintainable and robust code.