# Stack Class Documentation

The `Stack<T>` class represents a simple last-in-first-out (LIFO) collection of items. It provides methods for adding, accessing, and removing elements, as well as clearing the entire stack.

## Type Parameters

- `T`: The type of elements in the stack.

## Public Methods

### Push
Adds an element to the top of the stack.

#### Syntax

```vein
stackInstance.Push(value);
```

#### Parameters

- `value` (T): The element to push onto the stack.

#### Example

```vein
auto stack = new Stack<i32>();
stack.Push(10);
stack.Push(20);
stack.Push(30);
```

### Peek
Returns the element at the top of the stack without removing it.

#### Syntax

```vein
auto topElement = stackInstance.Peek();
```

#### Returns

- `T`: The element at the top of the stack.

#### Example

```vein
auto topElement = stack.Peek(); // topElement is 30
```

### Pop
Removes and returns the element at the top of the stack.

#### Syntax

```vein
auto removedElement = stackInstance.Pop();
```

#### Returns

- `T`: The element removed from the top of the stack.

#### Example

```vein
auto removedElement = stack.Pop(); // removedElement is 30, stack contains 10, 20
```

### Clear
Removes all elements from the stack.

#### Syntax

```vein
stackInstance.Clear();
```

#### Example

```vein
stack.Clear(); // stack is now empty
```

## Examples

### Basic Usage

#### Creating a Stack and Performing Operations

```vein
// Creating a new stack of numbers
auto numberStack = new Stack<i32>();

// Pushing elements onto the stack
numberStack.Push(1);
numberStack.Push(2);
numberStack.Push(3);

// Peeking the top element
Out.println(numberStack.Peek()); // Output: 3

// Popping the top element
Out.println(numberStack.Pop()); // Output: 3

// Clearing the stack
numberStack.Clear();
Out.println(numberStack.Peek()); // Throws error because stack is empty
```

### Using a Stack with Custom Objects

```vein
class Person {
    new(name: string, age: i32) {}
}

// Creating a new stack of Person objects
auto personStack = new Stack<Person>();

// Pushing new Person objects onto the stack
personStack.Push(new Person("Alice", 30));
personStack.Push(new Person("Bob", 25));

// Peeking the top Person object
auto topPerson = personStack.Peek();
Out.println(topPerson.name); // Output: Bob

// Popping the top Person object
auto removedPerson = personStack.Pop();
Out.println(removedPerson.name); // Output: Bob

// Clearing the stack
personStack.Clear();
```

## Conclusion

The `Stack<T>` class is a versatile and straightforward collection for handling elements in a LIFO order. By making use of its methods, you can efficiently manage collections where the order of insertion and removal is significant.