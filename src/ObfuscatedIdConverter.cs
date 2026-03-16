using System.Text.Json;
using System.Text.Json.Serialization;

namespace Philiprehberger.Obfuscator;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that serializes <see cref="long"/> values as
/// obfuscated strings using an <see cref="IdObfuscator"/> instance.
/// </summary>
/// <remarks>
/// When reading JSON, decodes the obfuscated string back to a <see cref="long"/>.
/// When writing JSON, encodes the <see cref="long"/> value as an obfuscated string.
/// </remarks>
public sealed class ObfuscatedIdConverter : JsonConverter<long>
{
    private readonly IdObfuscator _obfuscator;

    /// <summary>
    /// Creates a new <see cref="ObfuscatedIdConverter"/> with the specified obfuscator.
    /// </summary>
    /// <param name="obfuscator">The <see cref="IdObfuscator"/> used to encode and decode values.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obfuscator"/> is null.</exception>
    public ObfuscatedIdConverter(IdObfuscator obfuscator)
    {
        ArgumentNullException.ThrowIfNull(obfuscator);
        _obfuscator = obfuscator;
    }

    /// <summary>
    /// Reads an obfuscated string from JSON and decodes it to a <see cref="long"/> value.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The decoded long value.</returns>
    /// <exception cref="JsonException">Thrown when the JSON token is not a string or the value cannot be decoded.</exception>
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a string token for obfuscated ID.");

        var encoded = reader.GetString()
            ?? throw new JsonException("Obfuscated ID string was null.");

        try
        {
            return _obfuscator.DecodeSingle(encoded);
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw new JsonException($"Failed to decode obfuscated ID '{encoded}'.", ex);
        }
    }

    /// <summary>
    /// Writes a <see cref="long"/> value as an obfuscated string to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The long value to encode.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        var encoded = _obfuscator.Encode(value);
        writer.WriteStringValue(encoded);
    }
}
