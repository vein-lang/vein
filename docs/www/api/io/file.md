# File Class Documentation

The `File` class provides simple and convenient methods for file operations such as reading all text from a file, writing all text to a file, and creating a new file. These methods are wrapped around internal native implementations for efficient file handling.

## Methods

### `readAllText`

```vein
public static readAllText(path: string): string
```

**Description:**

Reads all text from the file at the specified path.

**Parameters:**

- `path` (_string_): The path to the file to read.

**Returns:**

- (_string_): The contents of the file.

**Example:**

```vein
auto content = File.readAllText("example.txt");
Out.println(content);
```

### `writeAllText`

```vein
public static writeAllText(path: string, content: string): void
```

**Description:**

Writes the specified text to a file at the given path. If the file already exists, its content is overwritten.

**Parameters:**

- `path` (_string_): The path to the file to write.
- `content` (_string_): The content to write to the file.

**Example:**

```vein
File.writeAllText("example.txt", "Hello, World!");
```

### `create`

```vein
public static create(path: string): StreamWriter
```

**Description:**

Creates a new file at the specified path and returns a `StreamWriter` for writing to the file.

**Parameters:**

- `path` (_string_): The path to the file to create.

**Returns:**

- (_StreamWriter_): A `StreamWriter` for writing to the newly created file.

**Example:**

```vein
auto writer = File.create("newfile.txt");
writer.write("New file content");
writer.close();
```

## Example Usage

### Reading a File

```vein
auto content = File.readAllText("readme.txt");
Out.println(content);
```

### Writing to a File

```vein
File.writeAllText("output.txt", "This is some text content.");
```

### Creating a New File

```vein
auto writer = File.create("log.txt");
writer.write("Log entry 1");
writer.close();
```
