# Buffers and Spans <Badge type="warning" text="experimental" /> 

## Buffer Management

Vein relies on its garbage collector (GC) for memory management, especially for tasks such as allocating and freeing buffers. Buffers are used to store data temporarily during send and receive operations.

### Allocating Buffers

Buffers are allocated using the `GC.allocate_u8(size)` method, which returns a `Span<u8>`. The buffer size is specified as an argument to the method.

```vein
auto buffer: Span<u8> = GC.allocate_u8(1024);
```

### Deallocating Buffers

Buffers are deallocated using the `GC.destroy_u8(span)` method. This method takes the `Span<u8>` to be freed as an argument.

```vein
GC.destroy_u8(buffer);
```
