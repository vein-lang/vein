---
title: Collections
---

# Collections <Badge type="danger" text="supported only in 0.45 version" /> 

In this guide, we will cover the basics of collections in Vein. Collections provide a way to store and manipulate groups of objects. Vein offers several types of collections, including `List<T>`, `Queue<T>`, `Stack<T>`, and `Map<TKey, TValue>`.

## List\<T\>

The `List<T>` collection is a dynamic array that can grow in size. It allows for indexed access, adding and removing elements.

### Example

```vein
auto numbers: List<i32> = List<i32>();

numbers.add(1);
numbers.add(2);
numbers.add(3);

Out.println(numbers[0]);  // Output: 1
Out.println(numbers[1]);  // Output: 2
Out.println(numbers[2]);  // Output: 3

numbers.removeAt(1);  // Removes the element at index 1

foreach (auto number in numbers) {
    Out.println(number);  // Output: 1, 3
}
```

## Queue\<T\>

The `Queue<T>` collection represents a first-in, first-out (FIFO) collection of objects. You can enqueue elements to add them to the queue and dequeue elements to remove them.

### Example

```vein
auto queue: Queue<string> = Queue<string>();

queue.enqueue("first");
queue.enqueue("second");
queue.enqueue("third");

Out.println(queue.dequeue());  // Output: first
Out.println(queue.dequeue());  // Output: second
Out.println(queue.peek());     // Output: third (without removing it)
```

## Stack\<T\>

The `Stack<T>` collection represents a last-in, first-out (LIFO) collection of objects. You can push elements onto the stack and pop elements off the stack.

### Example

```vein
auto stack: Stack<string> = Stack<string>();

stack.push("bottom");
stack.push("middle");
stack.push("top");

Out.println(stack.pop());  // Output: top
Out.println(stack.pop());  // Output: middle
Out.println(stack.peek()); // Output: bottom (without removing it)
```

## Map\<TKey, TValue\>

The `Map<TKey, TValue>` collection represents a collection of key-value pairs. It allows for fast retrieval of values based on their keys.

### Example

```vein
auto map: Map<string, i32> = Map<string, i32>();

map["one"] = 1;
map["two"] = 2;
map["three"] = 3;

Out.println(map["one"]);   // Output: 1
Out.println(map["two"]);   // Output: 2
Out.println(map["three"]); // Output: 3

foreach (auto key in map.keys()) {
    Out.println(key + ": " + map[key]);  // Output: one: 1, two: 2, three: 3
}
```

In this example, we create a `Map` collection with string keys and integer values. We then retrieve and print the values based on their keys.

## Conclusion

Understanding collections like `List<T>`, `Queue<T>`, `Stack<T>`, and `Map<TKey, TValue>` is crucial for effective programming in Vein. These collections provide a variety of ways to store, access, and manipulate groups of objects, making it easier to manage and process data in your applications.