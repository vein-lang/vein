# Sync Class Documentation

The `Sync` class provides a unified interface for semaphore and mutex synchronization. Depending on the mode specified during instantiation, it can either lock/unlock a semaphore or a mutex.

## Constants

### `MODE_SEMAPHORE`

```vein
public const MODE_SEMAPHORE: i32 = 0;
```

**Description:**

Mode constant for semaphore synchronization.

### `MODE_MUTEX`

```vein
public const MODE_MUTEX: i32 = 1;
```

**Description:**

Mode constant for mutex synchronization.

## Constructor

### `new(mode: i32)`

```vein
new(mode: i32)
```

**Description:**

Constructs a new `Sync` object with the specified mode.

**Parameters:**

- `mode` (_i32_): The synchronization mode, either `Sync.MODE_SEMAPHORE` or `Sync.MODE_MUTEX`.

**Example:**

```vein
Sync sync = new Sync(Sync.MODE_MUTEX);
```

## Methods

### `unlock`

```vein
unlock(): void
```

**Description:**

Unlocks the semaphore or mutex based on the current mode.

**Example:**

```vein
sync.unlock();
```

### `lock`

```vein
lock(): void
```

**Description:**

Locks the semaphore or mutex based on the current mode.

**Example:**

```vein
sync.lock();
```

## Example Usage

```vein
Sync semaphoreSync = new Sync(Sync.MODE_SEMAPHORE);
semaphoreSync.lock();
// critical section
semaphoreSync.unlock();

Sync mutexSync = new Sync(Sync.MODE_MUTEX);
mutexSync.lock();
// critical section
mutexSync.unlock();
```
