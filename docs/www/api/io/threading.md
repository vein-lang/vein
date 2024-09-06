# Threading <Badge type="warning" text="experimental" /> 

The `Thread` class provides several methods for managing thread execution in a multithreaded environment. 

## Public Methods

### BeginAffinity
Starts a thread affinity, binding the current thread to a set of processors. This is useful for optimizing performance by reducing thread migration.

```vein
Thread.BeginAffinity();
```

### EndAffinity
Ends the thread affinity that was started with `BeginAffinity`, allowing the thread to run on any available processor.

```vein
Thread.EndAffinity();
```

### BeginCriticalRegion
Marks the beginning of a critical region of code that should not be interrupted by thread aborts.

```vein
Thread.BeginCriticalRegion();
```

### EndCriticalRegion
Marks the end of a critical region of code.

```vein
Thread.EndCriticalRegion();
```

### MemoryBarrier
Ensures that memory accesses before the barrier are completed before those after it. This is crucial in multi-threaded programming to ensure memory consistency.

```vein
Thread.MemoryBarrier();
```

### Yield
Causes the calling thread to yield execution to another thread that is ready to run on the current processor.

```vein
Thread.Yield();
```

### Create
Creates a new thread with the specified function.

```vein
static myThreadFunction(): void {
    // Thread work here
};
auto myThread = Thread.Create(&myThreadFunction);
```

### Sleep
Suspends the current thread for the specified number of milliseconds.

```vein
Thread.Sleep(1000); // Sleeps for 1 second
```

### Join
Blocks the calling thread until the thread represented by this instance terminates.

```vein
myThread.Join();
```

### Start
Starts the execution of the thread represented by this instance.

```vein
myThread.Start();
```

## Private Methods

### _join
A private static method used internally to join a thread. Should not be called directly.

### _start
A private static method used internally to start a thread. Should not be called directly.

## Examples

### Creating and Starting a Thread

```vein
static myThreadFunction(): void {
    Out.print("Thread is running");
};

auto myThread = Thread.Create(&myThreadFunction);
myThread.Start();
```

### Using Thread Sleep

```vein
static myThreadFunction(): void {
    Out.print("Thread started");
    Thread.Sleep(2000); // Sleep for 2 seconds
    Out.print("Thread woke up");
};

auto myThread = Thread.Create(&myThreadFunction);
myThread.Start();
```

### Joining a Thread

```vein
static myThreadFunction(): void {
    Out.print("Thread started");
};

auto myThread = Thread.Create(&myThreadFunction);
myThread.Start();
myThread.Join(); // Main thread waits for myThread to finish
Out.print("myThread has finished");
```

## Conclusion

The `Thread` class provides a robust interface for managing threads in a concurrent environment. 
By understanding and utilizing its methods, you can effectively control thread execution, synchronization, and resource management.