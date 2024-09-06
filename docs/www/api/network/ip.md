# Ipv4 and IpEndpoint

The `Ipv4Addr` class represents an IPv4 address consisting of four octets.

## Public Constructors

### new

Initializes a new instance of the `Ipv4Addr` class with the specified octets.

#### Syntax

```vein
auto ipAddr = new Ipv4Addr(a1, a2, a3, a4);
```

#### Parameters

- `a1` (u8): The first octet of the IPv4 address.
- `a2` (u8): The second octet of the IPv4 address.
- `a3` (u8): The third octet of the IPv4 address.
- `a4` (u8): The fourth octet of the IPv4 address.

#### Example

```vein
auto ipAddr = new Ipv4Addr(192, 168, 1, 1);
```

## IpEndpoint Class Documentation

The `IpEndpoint` class represents an endpoint in a network which consists of an IP address and a port number.

### Public Constructors

### new

Initializes a new instance of the `IpEndpoint` class with the specified address and port.

#### Syntax

```vein
auto endpoint = new IpEndpoint(addr, port);
```

#### Parameters

- `addr` (Ipv4Addr): The IP address of the endpoint.
- `port` (u16): The port number of the endpoint.

#### Example

```vein
auto ipAddr = new Ipv4Addr(192, 168, 1, 1);
auto endpoint = new IpEndpoint(ipAddr, 8080);
```
