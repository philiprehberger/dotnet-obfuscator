namespace Philiprehberger.Obfuscator;

/// <summary>
/// Configuration options for <see cref="IdObfuscator"/>.
/// </summary>
/// <param name="Salt">The salt used to shuffle the alphabet and produce unique encodings.</param>
/// <param name="Alphabet">
/// The characters used for encoding. Defaults to alphanumeric characters (a-z, A-Z, 0-9).
/// Must contain at least 4 unique characters.
/// </param>
/// <param name="MinLength">The minimum length of encoded strings. Defaults to 0 (no padding).</param>
public record ObfuscatorOptions(
    string Salt,
    string? Alphabet = null,
    int MinLength = 0);
