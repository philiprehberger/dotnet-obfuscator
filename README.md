# Philiprehberger.Obfuscator

[![CI](https://github.com/philiprehberger/dotnet-obfuscator/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-obfuscator/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.Obfuscator.svg)](https://www.nuget.org/packages/Philiprehberger.Obfuscator)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-obfuscator)](LICENSE)

Reversible integer/long ID obfuscation for URL-safe, non-sequential public IDs.

## Installation

```bash
dotnet add package Philiprehberger.Obfuscator
```

## Usage

### Basic Encode/Decode

```csharp
using Philiprehberger.Obfuscator;

var obfuscator = new IdObfuscator(salt: "my-secret-salt");

var encoded = obfuscator.Encode(42);
// e.g. "xR3j"

long decoded = obfuscator.DecodeSingle(encoded);
// 42
```

### Multi-Value Encoding

```csharp
var encoded = obfuscator.Encode(1, 2, 3);
// e.g. "aBcXyZ"

long[] values = obfuscator.Decode(encoded);
// [1, 2, 3]
```

### Custom Alphabet and Minimum Length

```csharp
var obfuscator = new IdObfuscator(
    salt: "my-salt",
    alphabet: "abcdefghijklmnopqrstuvwxyz",
    minLength: 8);

var encoded = obfuscator.Encode(1);
// e.g. "bejklqrs" (at least 8 characters, lowercase only)
```

### Safe Decoding

```csharp
if (obfuscator.TryDecode("xR3j", out var values))
{
    // values contains the decoded longs
}
```

### Using Options Record

```csharp
var options = new ObfuscatorOptions(Salt: "my-salt", MinLength: 6);
var obfuscator = new IdObfuscator(options);
```

### JSON Serialization

```csharp
using System.Text.Json;

var obfuscator = new IdObfuscator(salt: "my-salt");
var converter = new ObfuscatedIdConverter(obfuscator);

var jsonOptions = new JsonSerializerOptions();
jsonOptions.Converters.Add(converter);

// Serializes long fields as obfuscated strings
var json = JsonSerializer.Serialize(new { Id = 42L }, jsonOptions);
// {"Id":"xR3j"}

// Deserializes obfuscated strings back to longs
var obj = JsonSerializer.Deserialize<MyDto>(json, jsonOptions);
```

## API

### `IdObfuscator`

| Method | Description |
|--------|-------------|
| `IdObfuscator(string salt, string? alphabet, int minLength)` | Create an obfuscator with the given salt, optional alphabet, and minimum encoded length |
| `IdObfuscator(ObfuscatorOptions options)` | Create an obfuscator from an options record |
| `Encode(long value)` | Encode a single value to an obfuscated string |
| `Encode(params long[] values)` | Encode multiple values to a single obfuscated string |
| `Decode(string encoded)` | Decode an obfuscated string to an array of longs |
| `DecodeSingle(string encoded)` | Decode an obfuscated string containing exactly one value |
| `TryDecode(string encoded, out long[]? values)` | Attempt to decode without throwing on failure |

### `ObfuscatedIdConverter`

| Method | Description |
|--------|-------------|
| `ObfuscatedIdConverter(IdObfuscator obfuscator)` | Create a JSON converter using the given obfuscator |

### `ObfuscatorOptions`

| Property | Description |
|----------|-------------|
| `Salt` | The salt for shuffling the alphabet |
| `Alphabet` | Custom alphabet characters (default: a-z, A-Z, 0-9) |
| `MinLength` | Minimum encoded string length (default: 0) |

## Development

```bash
dotnet build src/Philiprehberger.Obfuscator.csproj --configuration Release
```

## License

MIT
