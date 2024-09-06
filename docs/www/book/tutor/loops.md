---
title: Loops
---

# Loops

In this guide, we will cover the basics of loops in Vein. Loops are fundamental structures that allow you to repeat a block of code multiple times.

## Foreach Loop

The `foreach` loop is used to iterate over the elements of a collection or array.

### Syntax

```vein
foreach (auto element in collection) {
    // Code to execute for each element
}
```

### Example

```vein
auto numbers = [1, 2, 3, 4, 5];

foreach (auto number in numbers) {
    Out.println(number);
}
```

In this example, each element in the `numbers` array is printed on a new line.

## For Loop

The `for` loop is used to execute a block of code a specific number of times. It consists of three parts: initialization, condition, and iteration expression.

### Syntax

```vein
for (initialization; condition; iteration) {
    // Code to execute for each iteration
}
```

### Example

```vein
for (auto i: i32 = 0; i < 5; i = i + 1) {
    Out.println(i);
}
```

In this example, the loop starts with `i` equal to 0 and runs until `i` is less than 5. After each iteration, `i` is incremented by 1.

## While Loop

The `while` loop is used to repeat a block of code as long as a specified condition is true.

### Syntax

```vein
while (condition) {
    // Code to execute while the condition is true
}
```

### Example

```vein
auto count: i32 = 0;

while (count < 5) {
    Out.println(count);
    count = count + 1;
}
```

In this example, the loop continues to execute as long as `count` is less than 5. After each iteration, `count` is incremented by 1.

## Nested Loops

You can also nest loops within other loops to create more complex looping structures.

### Example

```vein
for (auto i: i32 = 0; i < 3; i = i + 1) {
    for (auto j: i32 = 0; j < 3; j = j + 1) {
        Out.println("i: " + i + ", j: " + j);
    }
}
```

In this example, for each iteration of the outer `for` loop, the inner `for` loop runs from 0 to 2, printing the values of `i` and `j`.

## Conclusion

Using loops such as `foreach`, `for`, and `while` allows you to perform repetitive tasks efficiently in Vein. Understanding how to use these loops will enable you to handle iterating over collections and executing code multiple times more effectively.