---
title: Quantum Operations
---

# Quantum Operations <Badge type="danger" text="supported only in 0.97 version" /> 

In this guide, we will cover the basics of quantum operations in Vein. Vein supports the declaration and execution of quantum operations, which are essential for quantum computing tasks.

::: danger Attention! 
At the moment quantum feature of the language is under development, the syntax, runtime part can be changed
:::

## Declaring Quantum Operations

Quantum operations in Vein are declared using the `operation` keyword, and they return a quantum result (`QResult`).

### Syntax

```vein
public operation OperationName(parameters): QResult
```

### Example

```vein
public operation TestOperation(i: i32): QResult {
    // Quantum operation body
}
```

In this example, `TestOperation` is a quantum operation that takes an integer parameter `i` and returns a `QResult`.

## Quantum Commands

Quantum commands are used in special expressions denoted by `Q!{}`. These commands allow you to manipulate qubits and perform quantum operations.

### Syntax

```vein
Q!{
    .qd qubitName[index];
    .cnot controlQubit targetQubit;
    .if (qubit is One) {
        // Conditional quantum operation
    }
    .qcall otherOperation(parameters) |> targetQubit;
    .x(qubit);
}
```

### Example

```vein
public operation QuantumExample(qubits: array<Qubit>): QResult {
    return Q!{
        .qd q[0];
        .qd q[1];
        
        .cnot q[0] q[1];
        .if (q[0] is One) {
            .h(q[0]);
        }
        .qcall AnotherOperation(q[0]) |> q[1];
        .x(q[1]);
    };
}

public operation AnotherOperation(qubit: Qubit): QResult {
    return Q!{
        .h(qubit);
    };
}
```

In this example:
- Qubits are declared and indexed with `.qd`.
- The `CNOT` gate is applied using `.cnot`.
- A conditional quantum operation is performed with `.if`.
- Another operation is called using `.qcall`.
- The `X` gate is applied using `.x`.

## Running Quantum Operations

Quantum operations in Vein are executed using the Microsoft Quantum Simulator. For seamless integration, you need to install the quantum SDK workload.

### Installation

To enable support for quantum operations in Vein, install the quantum SDK workload:

```sh
rune workload install vein.quantum
```

### Running Quantum Operations

Once the workload is installed, you can run quantum operations within your program. The simulator will be invoked transparently to execute the quantum commands.

### Example

```vein
async master(): Job<void> {
    // Example of running a quantum operation
    auto result = qcall TestOperation(5);
    if (result == QResult.Success) {
        Out.println("Quantum operation succeeded.");
    } else {
        Out.println("Quantum operation failed.");
    }
}
```

In this example, `TestOperation` is executed asynchronously, and the result is checked to determine if the quantum operation was successful.

## Conclusion

Understanding quantum operations in Vein allows you to leverage quantum computing capabilities within your applications. By using the `operation` keyword, quantum command expressions (`Q!{}`), and integrating with the Microsoft Quantum Simulator, you can perform complex quantum tasks efficiently.