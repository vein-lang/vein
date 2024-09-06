# Queue Class Documentation

The `Queue<T>` class represents a first-in-first-out (FIFO) collection of items. It provides methods for adding, accessing, and removing elements, as well as clearing the entire queue.

## Type Parameters

- `T`: The type of elements in the queue.

## Public Methods

### Enqueue
Adds an element to the end of the queue.

#### Syntax

```vein
queueInstance.Enqueue(value);
```

#### Parameters

- `value` (T): The element to add to the queue.

#### Example

```vein
auto queue = new Queue<number>();
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);
```

### Dequeue
Removes and returns the element at the beginning of the queue.

#### Syntax

```vein
auto removedElement = queueInstance.Dequeue();
```

#### Returns

- `T`: The element removed from the beginning of the queue.

#### Example

```vein
auto removedElement = queue.Dequeue(); // removedElement is 1
```

### Clear
Removes all elements from the queue.

#### Syntax

```vein
queueInstance.Clear();
```

#### Example

```vein
queue.Clear(); // queue is now empty
```

## Examples

### Basic Usage

#### Creating a Queue and Performing Operations

```vein
// Creating a new queue of numbers
auto numberQueue = new Queue<number>();

// Enqueuing elements
numberQueue.Enqueue(1);
numberQueue.Enqueue(2);
numberQueue.Enqueue(3);

// Dequeuing an element
Out.println(numberQueue.Dequeue()); // Output: 1

// Clearing the queue
numberQueue.Clear();
```

### Using a Queue with Custom Objects

```vein
class Person {
    new(name: string, age: i32) {}
}

// Creating a new queue of Person objects
auto personQueue = new Queue<Person>();

// Enqueuing new Person objects
personQueue.Enqueue(new Person("Alice", 30));
personQueue.Enqueue(new Person("Bob", 25));

// Dequeuing a Person object
auto dequeuedPerson = personQueue.Dequeue();
Out.println(dequeuedPerson.name); // Output: Alice

// Clearing the queue
personQueue.Clear();
```

## Conclusion

The `Queue<T>` class is a versatile and straightforward collection for handling elements in a FIFO order. By making use of its methods, you can efficiently manage collections where the order of insertion and removal is significant.