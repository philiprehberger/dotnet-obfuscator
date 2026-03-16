namespace Philiprehberger.Obfuscator;

/// <summary>
/// Provides reversible obfuscation of integer and long values into URL-safe,
/// non-sequential string identifiers. Thread-safe and deterministic.
/// </summary>
public sealed class IdObfuscator
{
    private const string DefaultAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string SeparatorChars = "cfhistuCFHISTU";
    private const string GuardChars = "0123456789";

    private readonly string _salt;
    private readonly char[] _alphabet;
    private readonly char[] _separators;
    private readonly char[] _guards;
    private readonly int _minLength;

    /// <summary>
    /// Creates a new <see cref="IdObfuscator"/> instance.
    /// </summary>
    /// <param name="salt">The salt used to shuffle the alphabet and produce unique encodings.</param>
    /// <param name="alphabet">
    /// The characters used for encoding. Defaults to alphanumeric characters (a-z, A-Z, 0-9).
    /// Must contain at least 4 unique characters.
    /// </param>
    /// <param name="minLength">The minimum length of encoded strings. Defaults to 0 (no padding).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="salt"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the alphabet has fewer than 4 unique characters.</exception>
    public IdObfuscator(string salt, string? alphabet = null, int minLength = 0)
    {
        ArgumentNullException.ThrowIfNull(salt);

        _salt = salt;
        _minLength = Math.Max(0, minLength);

        var alphabetStr = alphabet ?? DefaultAlphabet;

        // Remove duplicate characters from alphabet
        var seen = new HashSet<char>();
        var uniqueAlphabet = new List<char>();
        foreach (var c in alphabetStr)
        {
            if (seen.Add(c))
                uniqueAlphabet.Add(c);
        }

        if (uniqueAlphabet.Count < 4)
            throw new ArgumentException("Alphabet must contain at least 4 unique characters.", nameof(alphabet));

        // Separate out separator characters from the alphabet
        var separators = new List<char>();
        var remaining = new List<char>();

        foreach (var c in uniqueAlphabet)
        {
            if (SeparatorChars.Contains(c))
                separators.Add(c);
            else
                remaining.Add(c);
        }

        // Ensure we have at least some separators
        if (separators.Count == 0)
        {
            // Take a few characters from the alphabet as separators
            var count = Math.Max(1, remaining.Count / 8);
            for (var i = 0; i < count && remaining.Count > 2; i++)
            {
                separators.Add(remaining[0]);
                remaining.RemoveAt(0);
            }
        }

        _separators = ConsistentShuffle(separators.ToArray(), _salt);

        // Extract guards from the alphabet
        var guardCount = Math.Max(1, remaining.Count / 8);
        var guards = new List<char>();
        for (var i = 0; i < guardCount && remaining.Count > 2; i++)
        {
            guards.Add(remaining[0]);
            remaining.RemoveAt(0);
        }

        _guards = guards.ToArray();
        _alphabet = ConsistentShuffle(remaining.ToArray(), _salt);
    }

    /// <summary>
    /// Creates a new <see cref="IdObfuscator"/> from an <see cref="ObfuscatorOptions"/> instance.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public IdObfuscator(ObfuscatorOptions options)
        : this(options.Salt, options.Alphabet, options.MinLength)
    {
    }

    /// <summary>
    /// Encodes a single non-negative long value into an obfuscated string.
    /// </summary>
    /// <param name="value">The value to encode. Must be non-negative.</param>
    /// <returns>An obfuscated string representation of the value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public string Encode(long value)
    {
        return Encode(new[] { value });
    }

    /// <summary>
    /// Encodes one or more non-negative long values into a single obfuscated string.
    /// </summary>
    /// <param name="values">The values to encode. All values must be non-negative.</param>
    /// <returns>An obfuscated string representation of the values.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any value is negative.</exception>
    public string Encode(params long[] values)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value must be provided.", nameof(values));

        foreach (var v in values)
        {
            if (v < 0)
                throw new ArgumentOutOfRangeException(nameof(values), v, "Values must be non-negative.");
        }

        return InternalEncode(values);
    }

    /// <summary>
    /// Decodes an obfuscated string back into an array of long values.
    /// </summary>
    /// <param name="encoded">The obfuscated string to decode.</param>
    /// <returns>An array of decoded long values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoded"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="encoded"/> is empty or invalid.</exception>
    public long[] Decode(string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);

        if (string.IsNullOrWhiteSpace(encoded))
            throw new ArgumentException("Encoded string cannot be empty.", nameof(encoded));

        return InternalDecode(encoded);
    }

    /// <summary>
    /// Decodes an obfuscated string that contains a single encoded value.
    /// </summary>
    /// <param name="encoded">The obfuscated string to decode.</param>
    /// <returns>The decoded long value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoded"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="encoded"/> is empty or invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the encoded string does not contain exactly one value.</exception>
    public long DecodeSingle(string encoded)
    {
        var values = Decode(encoded);
        if (values.Length != 1)
            throw new InvalidOperationException(
                $"Expected a single value but decoded {values.Length} values.");
        return values[0];
    }

    /// <summary>
    /// Attempts to decode an obfuscated string back into an array of long values.
    /// </summary>
    /// <param name="encoded">The obfuscated string to decode.</param>
    /// <param name="values">When this method returns true, contains the decoded values; otherwise, null.</param>
    /// <returns><c>true</c> if the string was successfully decoded; otherwise, <c>false</c>.</returns>
    public bool TryDecode(string encoded, out long[]? values)
    {
        values = null;

        if (string.IsNullOrWhiteSpace(encoded))
            return false;

        try
        {
            var decoded = InternalDecode(encoded);

            // Verify by re-encoding
            var reEncoded = InternalEncode(decoded);
            if (reEncoded != encoded)
                return false;

            values = decoded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string InternalEncode(long[] values)
    {
        // Calculate a hash of the input values to determine the lottery character
        long hashValue = 0;
        for (var i = 0; i < values.Length; i++)
        {
            hashValue += values[i] % (i + 100);
        }

        var alphabet = (char[])_alphabet.Clone();
        var lottery = alphabet[hashValue % alphabet.Length];

        var result = new List<char> { lottery };

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];

            // Build the salt for this iteration
            var iterSalt = BuildIterationSalt(lottery, alphabet);
            alphabet = ConsistentShuffle(alphabet, iterSalt);

            // Encode the number using the shuffled alphabet
            var encoded = HashNumber(value, alphabet);
            result.AddRange(encoded);

            // Add a separator between values
            if (i + 1 < values.Length)
            {
                var sepIndex = (int)(value % (encoded[0] + i));
                result.Add(_separators[Math.Abs(sepIndex) % _separators.Length]);
            }
        }

        // Pad to minimum length
        if (result.Count < _minLength)
        {
            var guardIndex = (int)((hashValue + result[0]) % _guards.Length);
            result.Insert(0, _guards[guardIndex]);

            if (result.Count < _minLength)
            {
                guardIndex = (int)((hashValue + result[2]) % _guards.Length);
                result.Add(_guards[guardIndex]);
            }
        }

        // Further padding with alphabet characters if still too short
        var halfLen = alphabet.Length / 2;
        while (result.Count < _minLength)
        {
            alphabet = ConsistentShuffle(alphabet, alphabet);
            var padding = new List<char>(alphabet);
            result.InsertRange(0, padding.GetRange(halfLen, alphabet.Length - halfLen));
            result.AddRange(padding.GetRange(0, halfLen));

            var excess = result.Count - _minLength;
            if (excess > 0)
            {
                var start = excess / 2;
                result = result.GetRange(start, _minLength);
            }
        }

        return new string(result.ToArray());
    }

    private long[] InternalDecode(string encoded)
    {
        var input = encoded.ToCharArray();

        // Strip guard characters from the edges
        var guardSet = new HashSet<char>(_guards);
        var parts = SplitOnChars(input, guardSet);
        var working = parts.Length >= 2 ? parts[1] : parts[0];

        if (working.Length == 0)
            throw new ArgumentException("Invalid encoded string.", nameof(encoded));

        // First character is the lottery
        var lottery = working[0];
        var rest = working[1..];

        var alphabet = (char[])_alphabet.Clone();
        var separatorSet = new HashSet<char>(_separators);
        var segments = SplitOnChars(rest, separatorSet);

        var results = new List<long>();

        foreach (var segment in segments)
        {
            var iterSalt = BuildIterationSalt(lottery, alphabet);
            alphabet = ConsistentShuffle(alphabet, iterSalt);
            results.Add(UnhashNumber(segment, alphabet));
        }

        return results.ToArray();
    }

    private static char[] HashNumber(long value, char[] alphabet)
    {
        var result = new List<char>();
        var len = alphabet.Length;

        do
        {
            result.Insert(0, alphabet[value % len]);
            value /= len;
        } while (value > 0);

        return result.ToArray();
    }

    private static long UnhashNumber(char[] input, char[] alphabet)
    {
        long number = 0;
        var len = alphabet.Length;

        for (var i = 0; i < input.Length; i++)
        {
            var pos = Array.IndexOf(alphabet, input[i]);
            if (pos < 0)
                throw new ArgumentException($"Invalid character '{input[i]}' in encoded string.");

            number = number * len + pos;
        }

        return number;
    }

    private static string BuildIterationSalt(char lottery, char[] alphabet)
    {
        var salt = new char[alphabet.Length + 1];
        salt[0] = lottery;
        Array.Copy(alphabet, 0, salt, 1, Math.Min(alphabet.Length, salt.Length - 1));
        return new string(salt);
    }

    private static char[][] SplitOnChars(char[] input, HashSet<char> splitChars)
    {
        var parts = new List<char[]>();
        var current = new List<char>();

        foreach (var c in input)
        {
            if (splitChars.Contains(c))
            {
                parts.Add(current.ToArray());
                current.Clear();
            }
            else
            {
                current.Add(c);
            }
        }

        parts.Add(current.ToArray());
        return parts.ToArray();
    }

    private static char[] ConsistentShuffle(char[] items, string salt)
    {
        return ConsistentShuffle(items, salt.ToCharArray());
    }

    private static char[] ConsistentShuffle(char[] items, char[] salt)
    {
        if (salt.Length == 0)
            return (char[])items.Clone();

        var result = (char[])items.Clone();
        int v = 0, p = 0;

        for (var i = result.Length - 1; i > 0; i--)
        {
            v %= salt.Length;
            int a = salt[v];
            p += a;
            var j = (a + v + p) % i;

            (result[i], result[j]) = (result[j], result[i]);
            v++;
        }

        return result;
    }
}
