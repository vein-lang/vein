---
title: Type Aliases
description: Type aliases allow you to create new names for existing types, which can make your code more readable and easier to maintain. 
---

# Type Aliases <Badge type="warning" text="experimental" />

In this guide, we will cover type aliases in Vein.    
Type aliases allow you to create new names for existing types, which can make your code more readable and easier to maintain. 

## Local Type Aliases

Local type aliases are visible only within the module where they are declared.

### Syntax

```vein
alias typeAlias <| Int32;
```

### Example

```vein
alias Age <| Int32;

class Person {
   age: Age;

   new(age: Age) {
      this.age = age;
   }
}

auto person = Person(25);
Out.println("Age: " + person.age);  // Output: Age: 25
```

## Global Type Aliases

Global type aliases are visible across different modules.

### Syntax

```vein
global alias typeAlias <| Int32;
```

### Example

```vein
global alias Age <| Int32;

class Person {
   age: Age;

   new(age: Age) {
      this.age = age;
   }
}

auto person = Person(30);
Out.println("Age: " + person.age);  // Output: Age: 30
```

::: warning Attention! 
Currently global type alias is disabled for using in user modules (only in system modules)
:::


## Function Aliases

Function aliases allow you to create new names for function signatures.

### Local Function Aliases

Local function aliases are visible only within the module where they are declared.

### Syntax

```vein
alias fnAlias <| (i: i32): void;
```

### Example

```vein
alias PrintFunction <| (i: i32): void;

fnAlias printNumber;

printNumber = (i: i32): void -> {
    Out.println("Number: " + i);
};

printNumber(42);  // Output: Number: 42
```

## Global Function Aliases

Global function aliases are visible across different modules.

### Syntax

```vein
global alias fnAlias <| (i: i32): void;
```

### Example

```vein
global alias PrintFunction <| (i: i32): void;

fnAlias printNumber;

printNumber = (i: i32): void -> {
    Out.println("Number: " + i);
};

printNumber(42);  // Output: Number: 42
```

## Conclusion

Type aliases in Vein provide a powerful way to create more readable and maintainable code. Whether you are creating local or global aliases for types or functions, understanding how to use these aliases can help you better organize your codebase.