# Map Class Documentation

The `Map<TKey, TValue>` class is a collection that maps keys to values. A map cannot contain duplicate keys; each key can map to at most one value. It provides methods for adding, removing, and accessing elements as well as checking for the presence of a key in the map.

## Type Parameters

- `TKey`: The type of keys maintained by this map.
- `TValue`: The type of mapped values.

## Public Methods

### Add
Adds a key-value pair to the map. If the key already exists, the value is updated.

#### Syntax

```vein
mapInstance.Add(key, value);
```

#### Parameters

- `key` (TKey): The key of the element to add.
- `value` (TValue): The value of the element to add.

#### Example

```vein
auto map = new Map<string, i32>();
map.Add("one", 1);
map.Add("two", 2);
map.Add("three", 3);
```

### Remove
Removes the element with the specified key from the map.

#### Syntax

```vein
mapInstance.Remove(key);
```

#### Parameters

- `key` (TKey): The key of the element to remove.

#### Example

```vein
map.Remove("two"); // Removes the element with key "two"
```

### ContainsKey
Checks if the map contains an element with the specified key.

#### Syntax

```vein
auto contains = mapInstance.ContainsKey(key);
```

#### Parameters

- `key` (TKey): The key to check for.

#### Returns

- `boolean`: `true` if the map contains an element with the specified key, otherwise `false`.

#### Example

```vein
auto hasKey = map.ContainsKey("one"); // true
auto hasKey = map.ContainsKey("two"); // false (if "two" was removed)
```

### Get
Returns the value to which the specified key is mapped.

#### Syntax

```vein
auto value = mapInstance.Get(key);
```

#### Parameters

- `key` (TKey): The key whose associated value is to be returned.

#### Returns

- `TValue`: The value to which the specified key is mapped.

#### Example

```vein
auto value = map.Get("one"); // 1
```

## Examples

### Basic Usage

#### Creating a Map and Performing Operations

```vein
auto map = new Map<string, number>();

// Adding elements to the map
map.Add("apple", 1);
map.Add("banana", 2);
map.Add("cherry", 3);

// Checking if the map contains a key
Out.print(map.ContainsKey("banana")); // Output: true
Out.print(map.ContainsKey("grape"));  // Output: false

// Getting the value associated with a key
Out.print(map.Get("cherry")); // Output: 3

// Removing an element
map.Remove("banana");
Out.print(map.ContainsKey("banana")); // Output: false
```

### Using a Map with Custom Objects

```vein
class Product {
    new(name: string, price: i32) {}
}

// Creating a new map of products
auto productMap = new Map<i32, Product>();

// Adding products to the map
productMap.Add(101, new Product("Laptop", 1200));
productMap.Add(102, new Product("Smartphone", 800));

// Checking if the map contains a product by ID
Out.print(productMap.ContainsKey(101)); // Output: true

// Getting a product by ID
auto laptop = productMap.Get(101);
Out.print(laptop.name); // Output: Laptop

// Removing a product by ID
productMap.Remove(102);
Out.print(productMap.ContainsKey(102)); // Output: false
```

## Conclusion

The `Map<TKey, TValue>` class provides a robust and efficient way to store and manage key-value pairs. By leveraging its methods, you can handle various scenarios where mapping relationships between keys and values is required. This can be particularly useful in many programming situations like caching, lookups, and data transformation.