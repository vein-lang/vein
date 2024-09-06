# SocketServer and SocketClient <Badge type="warning" text="experimental" /> 

# Socket Class

The `Socket` class provides a robust interface for network communication, facilitating key operations such as binding, listening, accepting connections, sending, and receiving data.       
The use of the garbage collector for buffer allocation and deallocation ensures efficient memory management during network operations.      

With this documentation, you should have a clear understanding of how to use the `Socket` class and manage buffers in Vein.     

## Overview

The `Socket` class provides the following primary functionalities:

- Creating a new socket with specific characteristics (family, stream kind, and protocol).
- Binding the socket to an address and port.
- Listening for incoming connections.
- Accepting incoming client connections.
- Sending data through the socket.
- Receiving data from the socket.
- Shutting down the socket.

## Methods

### `listen(addr: IpEndpoint, mttu: i32): void`

Prepares the socket to listen for incoming connections.

- `addr`: The IP address and port to bind to.
- `mttu`: The maximum transmission unit.

### `accept(): Socket`

Accepts an incoming connection and returns a new `Socket` instance representing the client.

### `send(data: Span<u8>, flags: i32): i32`

Sends data through the socket.

- `data`: The byte span containing the data to be sent.
- `flags`: Flags influencing the send behavior.

Returns the number of bytes sent.

### `receive(data: Span<u8>, flags: i32): i32`

Receives data from the socket.

- `data`: The byte span where the received data will be stored.
- `flags`: Flags influencing the receive behavior.

Returns the number of bytes received.

### `shutdown(flags: i32): void`

Shuts down the socket, closing any active connections and releasing resources.

## Usage Example

Here is a basic example of how to use the `Socket` class including buffer management:

```vein
#space "std"

public class Example {
    public static master(): void {
        // Create a server socket
        auto server = new Socket(2, 1, 6); // AF_INET, SOCK_STREAM, IPPROTO_TCP
        auto addr = new IpEndpoint("127.0.0.1", 8080);

        // Listen for incoming connections
        server.listen(addr, 128);

        while (true) {
            // Accept a client connection
            auto client = server.accept();

            // Allocate buffer for receiving data
            auto buffer = GC.allocate_u8(1024);

            // Receive data from the client
            auto bytesReceived = client.receive(buffer, 0);
            if (bytesReceived > 0) {
                // Echo the received data back to the client
                client.send(buffer.slice(0, bytesReceived), 0);
            }

            // Free the allocated buffer
            GC.destroy_u8(buffer);

            // Shut down the client socket
            client.shutdown(2);
        }

        // Shut down the server socket when done
        server.shutdown(2);
    }
}
```

In this example:

1. A server socket is created and bound to the address `127.0.0.1` on port `8080`.
2. The server listens for incoming connections and then enters a loop to accept and handle client connections.
3. For each client connection:
    - A buffer of `1024` bytes is allocated using `GC.allocate_u8()`.
    - Data is received into the buffer and echoed back to the client.
    - The buffer is freed using `GC.destroy_u8()`.
    - The client socket is shut down after the data is handled.
4. The server socket is finally shut down after exiting the loop.
