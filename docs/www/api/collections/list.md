# List Class Documentation <Badge type="warning" text="beta" />

The `List<T>` class represents a collection of objects that can be individually accessed by index. It provides methods for adding, removing, and accessing elements, as well as manipulation of the list as a whole.

## Type Parameters

- `T`: The type of elements in the list.

## Public Methods

### Add
Adds an element to the end of the list.

#### Syntax

```vein
listInstance.Add(value);
```

#### Parameters

- `value` (T): The element to add to the list.

#### Example

```vein
auto list = new List<i32>();
list.Add(1);
list.Add(2);
list.Add(3);
```

### Remove
Removes the first occurrence of a specific element from the list.

#### Syntax

```vein
listInstance.Remove(value);
```

#### Parameters

- `value` (T): The value to remove from the list.

#### Example

```vein
list.Remove(2); // Removes the first occurrence of 2
```

### RemoveAt
Removes the element at the specified index of the list.

#### Syntax

```vein
listInstance.RemoveAt(index);
```

#### Parameters

- `index` (i32): The zero-based index of the element to remove.

#### Example

```vein
list.RemoveAt(1); // Removes the element at index 1
```

### AddRange
Adds the elements of the specified collection to the end of the list.

#### Syntax

```vein
listInstance.AddRange(values);
```

#### Parameters

- `values` (T[]): The collection whose elements should be added to the list.

#### Example

```vein
list.AddRange([4, 5, 6]); // Adds 4, 5, and 6 to the list
```

### Get
Returns the element at the specified index.

#### Syntax

```vein
auto value = listInstance.Get(index);
```

#### Parameters

- `index` (i32): The zero-based index of the element to get.

#### Returns

- `T`: The element at the specified index.

#### Example

```vein
auto value = list.Get(0); // Gets the element at index 0
```

## Examples

### Basic Usage

#### Creating a List and Performing Operations

```vein
auto list = new List<i32>();

// Adding elements to the list
list.Add(1);
list.Add(2);
list.Add(3);

// Accessing an element
Out.println(list.Get(0)); // Output: 1

// Removing an element
list.Remove(2);

// Adding a range of elements
list.AddRange([4, 5, 6]);

// Removing an element at a specific index
list.RemoveAt(1); // Removes the element at index 1 (formerly the value 3)
```

### Using a List with Custom Objects

```vein
class Person {
    new(name: string, age: i32) {}
}

// Creating a new list of Person objects
auto personList = new List<Person>();

// Adding new Person objects
personList.Add(new Person("Alice", 30));
personList.Add(new Person("Bob", 25));

// Accessing a Person object
auto person = personList.Get(0);
Out.println(person.name); // Output: Alice

// Removing a Person object
personList.Remove(person);

// Adding a range of Person objects
personList.AddRange([new Person("Charlie", 35), new Person("David", 40)]);

// Removing a Person object at a specific index
personList.RemoveAt(1); // Removes the element at index 1
```

## Conclusion

The `List<T>` class provides a flexible and efficient way to store and manipulate collections of objects. By leveraging its methods, you can handle various scenarios where dynamic arrays are required, ensuring ease of use and maintainability.