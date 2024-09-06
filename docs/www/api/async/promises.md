# Promises <Badge type="warning" text="experimental" /> 

The `Promise` struct is used to represent the eventual completion (or failure) of an asynchronous operation and its resulting value. 
<br/>
This struct provides methods to handle asynchronous operations in a standardized way, allowing chaining and composition of promises.

## Public Methods

### Wait
Synchronously waits for the promise to complete.

```vein
promiseInstance.Wait();
```

### TryWait
Attempts to wait for the promise to complete within the specified `TimeSpan`. Returns `true` if the promise is resolved within the time, otherwise returns `false`.

```vein
auto result = promiseInstance.TryWait(someTimeSpan);
```

### Finally
Adds a handler to be called when the promise is settled, regardless of its outcome (either resolved or rejected).

```vein
promiseInstance.Finally(() |> {
    // Cleanup or finalization code here
});
```

### Then
Adds a handler to be called when the promise is resolved.

```vein
promiseInstance.Then(() |> {
    // Code to handle resolved promise
});
```

### Catch
Adds a handler to be called when the promise is rejected.

```vein
promiseInstance.Catch((error) |> {
    // Code to handle rejected promise
});
```

### WaitAll
Synchronously waits for all the promises in the provided list to complete.

```vein
Promise.WaitAll([promise1, promise2, promise3]);
```

### WaitAny
Synchronously waits for any one of the promises in the provided list to complete.

```vein
Promise.WaitAny([promise1, promise2, promise3]);
```

### WhenAll
Returns a list of promises that complete when all the promises in the provided list have completed.

```vein
auto completedPromises = Promise.WhenAll([promise1, promise2, promise3]);
```

### WhenAny
Returns a promise that completes when any one of the promises in the provided list completes.

```vein
auto firstCompletedPromise = Promise.WhenAny([promise1, promise2, promise3]);
```

### When
Returns a promise that completes based on a custom predicate applied on the provided list of promises.

```vein
auto customCompletedPromise = Promise.When([promise1, promise2, promise3], customPredicate);
```

## Special Methods

The `Promise` struct also supports usage with the `await` syntax, allowing for asynchronous promise resolution using the `await` keyword. This integration allows for more readable and maintainable asynchronous code.

## Examples

### Basic Usage

#### Creating and Handling a Promise

```vein
auto myPromise = new Promise();

// Handle resolved state
myPromise.Then(() |> {
    Out.print("Promise resolved successfully.");
});

// Handle rejected state
myPromise.Catch((error) |> {
    console.error("Promise rejected with error:", error);
});

// Add a finalization handler
myPromise.Finally(() |> {
    Out.print("Promise settled (either resolved or rejected).");
});
```

### Wait for All Promises to Complete

```vein
Promise.WaitAll([promise1, promise2, promise3]);
Out.print("All promises have been resolved.");
```

### Wait for Any Promise to Complete

```vein
Promise.WaitAny([promise1, promise2, promise3]);
Out.print("At least one of the promises has resolved.");
```

### Using `await` with Promises

```vein
async asyncFunction(): IshtarTask {
    await promiseInstance;
    Out.print("Promise resolved using await.");
}
```

## Conclusion

The `Promise` struct provides a powerful and flexible way to handle asynchronous operations in a structured manner. By leveraging its methods, you can manage complex asynchronous workflows with ease, ensuring code readability and maintainability.