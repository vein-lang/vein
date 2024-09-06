---
title: Async
---

# Async and Await <Badge type="warning" text="experimental" />  <Badge type="danger" text="supported only in 0.45 version" /> 

In this guide, we will cover the basics of asynchronous programming in Vein. Asynchronous programming allows you to perform tasks without blocking the main thread, enabling more responsive applications.

## Async and Await

The `async` and `await` keywords are used to define and work with asynchronous methods.

### Syntax

- **async**: Used to define an asynchronous method.
- **await**: Used to wait for an asynchronous operation to complete.

### Example

```vein
async fetchData(): Job<string> {
    // Simulate an asynchronous operation
    await Job.sleep(2000);
    return "Data fetched";
}

async main(): Job<void> {
    auto result = await fetchData();
    Out.println(result);  // Output: Data fetched
}
```

In this example, `fetchData` is an asynchronous method that waits for 2 seconds before returning a string. The `main` method awaits the completion of `fetchData` and prints the result.

## Job\<T\> and Job

`Job<T>` represents an asynchronous task that produces a result of type `T`. `Job` represents an asynchronous task that does not produce a result.

### Example with Job\<T\>

```vein
async fetchData(): Job<string> {
    await Job.sleep(2000);
    return "Data fetched";
}

async processData(): Job<void> {
    auto data = await fetchData();
    Out.println(data);  // Output: Data fetched
}

processData().wait();
```

### Example with Job

```vein
async delayAction(): Job {
    await Job.sleep(1000);
    Out.println("Action after delay");
}

delayAction().wait();  // Output: Action after delay
```

In these examples, `Job<T>` and `Job` are used to represent and manage asynchronous tasks.

## Promises

`Promise<T>` represents a value that may be available now, or in the future, or never. Promises are often used in conjunction with `async` and `await`.

### Example with Promise\<T\>

```vein
async fetchData(): Job<Promise<string>> {
    auto promise = Promise<string>();

    // Simulate an asynchronous operation
    await Job.runAsync(() -> {
        Job.sleep(2000).wait();
        promise.resolve("Data fetched");
    });

    return promise;
}

async main(): Job<void> {
    auto promise = await fetchData();
    auto data = await promise;
    Out.println(data);  // Output: Data fetched
}

main().wait();
```

### Example with Promise

```vein
async delayAction(): Job<Promise> {
    auto promise = Promise();

    await Job.runAsync(() -> {
        Job.sleep(1000).wait();
        promise.resolve();
    });

    return promise;
}

async main(): Job<void> {
    auto promise = await delayAction();
    await promise;
    Out.println("Action after delay");
}

main().wait();  // Output: Action after delay
```

In these examples, `Promise<T>` and `Promise` are used to represent values that will be available in the future. They are resolved within asynchronous tasks.

## Best Practices

- **Use `async` and `await` for asynchronous methods**: This makes your code easier to read and maintain.
- **Avoid blocking the main thread**: Utilize asynchronous constructs to keep your application responsive.
- **Handle exceptions in asynchronous code**: Use try-catch blocks to handle exceptions that may occur during asynchronous operations.

### Example: Handling Exceptions in Async Code

```vein
async fetchData(): Job<string> {
    try {
        // Simulate a potential failure
        await Job.sleep(1000);
        fail new Exception("Data fetch failed");
    }
    catch (e: Exception) {
        return "Error: " + e.message;
    }
}

async main(): Job<void> {
    auto result = await fetchData();
    Out.println(result);  // Output: Error: Data fetch failed
}

main().wait();
```

In this example, a try-catch block is used to handle exceptions in an asynchronous method.

## Conclusion

Asynchronous programming in Vein, facilitated by `async`, `await`, `Job<T>`, `Job`, `Promise<T>`, and `Promise`, enables you to write more responsive and efficient code. Understanding these concepts helps you manage asynchronous tasks effectively in your applications.