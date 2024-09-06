# DNS 

The `Dns` static class provides methods to perform DNS queries and retrieve domain details asynchronously.

### Methods

#### `resolveAsync`

```vein
static resolveAsync(domain: string): Job<DomainDetails>;
```

**Description:**

Resolves the domain details for the given domain name asynchronously.

**Parameters:**

- `domain` (_string_): The domain name to query.

**Returns:**

- `Job<DomainDetails>`: A job that resolves to a `DomainDetails` object containing the details of the domain.

### Example Usage

```vein
public class Example {
    public async GetDomainDetailsAsync(domain: string): Job<DomainDetails> {
        return await Dns.resolveAsync(domain);
    }
}
```

## Class: `DomainDetails`

The `DomainDetails` class encapsulates the details of a domain returned from a DNS query.

### Properties

#### `ipv4`

```vein
public ipv4: Ipv4Addr;
```

**Description:**

The IPv4 address of the domain.

#### `hostname`

```vein
public hostname: string;
```

**Description:**

The hostname associated with the domain.

### Example Usage

```vein
public class Example {
    public PrintDomainDetails(details: DomainDetails): void {
        Out.println("Hostname: " + details.hostname);
        Out.println("IPv4 Address: " + details.ipv4);
    }
}
```

## Summary

The `Dns` class provides a method `resolveAsync` to perform DNS queries asynchronously, returning domain details encapsulated in a `DomainDetails` object. 
The `DomainDetails` class includes properties for the IPv4 address and hostname of the domain.