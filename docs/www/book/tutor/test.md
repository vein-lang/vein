# Working with Tests

Testing is an essential aspect of any development process. In the Vein framework, we ensure that your tests are well-integrated and easy to manage.     
This section provides an overview of how to work with tests in Vein.    

Defining and running tests in Vein is straightforward:

1. Define a `fixture()` method in your test classes.
2. Write your test methods.
3. Use the `Assert` class for assertions.
4. Run your tests using `rune test`.

With these steps, you can ensure that your code is robust and reliable. Keep an eye out for future updates that will make testing even easier.

::: warning Attention! 
Currently entry point for test declaring by define `fixture` static method, in future we planning using aspects (attributes) for declaring fixtures of test
:::

## Defining Tests

In Vein, tests are defined using static public methods. Specifically, you need to declare a `fixture()` method in your test classes.    
This `fixture()` method allows you to set up the necessary environment and state before running your actual tests.  

### Example

Here is a basic example of how to define a test in Vein:

```vein
class MyTest {
    public static fixture() {
        self.testSomething();
        self.testAnotherThing();
    }
    
    public static testSomething() {
        // Your test logic
    }

    public static testAnotherThing() {
        // Another test logic
    }
}
```

In this example, the `fixture()` method is defined as a static public method. You can then define your individual test methods within the same class.

## Running Tests

To execute your tests, you can use the following command in your terminal:

```shell
rune test
```

This command will run all the tests defined in your project, ensuring that everything is functioning as expected.

## Using the `Assert` Class

Vein provides a utility class named `Assert` to facilitate assertions in your tests. The `Assert` class offers a variety of static methods to help you verify that your code behaves as expected. Here are the available methods:

### Method Descriptions

- `equal(s1: string, s2: string): void`  
  Checks if two strings are equal.

- `equal(s1: i32, s2: i32): void`  
  Checks if two 32-bit integers are equal.

- `equal(s1: i64, s2: i64): void`  
  Checks if two 64-bit integers are equal.

- `isTrue(s1: bool): void`  
  Asserts that the given boolean is true.

- `isFalse(s1: bool): void`  
  Asserts that the given boolean is false.

- `isNull(s1: Object): void`  
  Asserts that the given object is null.

- `isNotNull(s1: Object): void`  
  Asserts that the given object is not null.

### Example Usage

Here is an example of how you can use the `Assert` class in your test methods:

```vein
public static testEquality() {
    Assert.equal("hello", "hello");
    Assert.equal(123, 123);
    Assert.equal(1234567890123, 1234567890123);
}

public static testBoolean() {
    Assert.isTrue(true);
    Assert.isFalse(false);
}

public static testNullChecks() {
    auto obj: Object = null;
    Assert.isNull(obj);
    
    auto notNullObj: Object = new Object();
    Assert.isNotNull(notNullObj);
}
```
