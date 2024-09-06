---
title: Run your first program
---

# Run your first program


In this guide, we will run a simple "Hello World" program using your language.

## Prerequisites

Ensure you have your environment set up to run the code. This usually means:
- 1️⃣ [Install SDK](/install.md)
- 2️⃣ [Create New Project](/newproject.md)


## Hello World Program

Let's create a new file called `helloworld.vein` and add the following code:

```vein
#use "std"

class Prog {
   master(): void {
      Out.println("Hello World!");
   }
}
```

## Explanation

- `#use "std"`: This line includes the standard library, providing essential functionalities like input and output.
- `class Prog`: Defines a class named `Prog`.
- `master(): void`: Defines a [entry point](https://en.wikipedia.org/wiki/Entry_point) method named `master` that returns nothing (void).
- `Out.println("Hello World!");`: Prints the string "Hello World!" to the output.

## Running the Program

To run the program, execute the following command in your terminal or command prompt:

```sh
rune run
```

If everything is set up correctly, you should see the following output:

```
Hello World!
```

Congratulations! You've successfully run your first program.