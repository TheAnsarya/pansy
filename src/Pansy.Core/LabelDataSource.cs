// ============================================================================
// LabelDataSource.cs - External Label Data Source Infrastructure
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pansy.Core;

/// <summary>
/// A cached label database entry for a specific game ROM.
/// Stored as JSON files indexed by CRC32.
/// </summary>
public class GameLabelDatabase {
	/// <summary>CRC32 of the ROM this database applies to.</summary>
	[JsonPropertyName("crc32")]
	public uint Crc32 { get; set; }

	/// <summary>Human-readable game title.</summary>
	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	/// <summary>Platform ID (matches Pansy platform constants).</summary>
	[JsonPropertyName("platform")]
	public byte Platform { get; set; }

	/// <summary>Source this data was fetched from.</summary>
	[JsonPropertyName("source")]
	public string Source { get; set; } = "";

	/// <summary>URL of the source page.</summary>
	[JsonPropertyName("sourceUrl")]
	public string? SourceUrl { get; set; }

	/// <summary>When this data was last fetched.</summary>
	[JsonPropertyName("fetchedAt")]
	public DateTimeOffset FetchedAt { get; set; }

	/// <summary>RAM map labels (address → name + description).</summary>
	[JsonPropertyName("ramLabels")]
	public Dictionary<string, LabelDatabaseEntry> RamLabels { get; set; } = [];

	/// <summary>ROM map labels (address → name + description).</summary>
	[JsonPropertyName("romLabels")]
	public Dictionary<string, LabelDatabaseEntry> RomLabels { get; set; } = [];
}

/// <summary>
/// A single label entry in a game label database.
/// </summary>
public class LabelDatabaseEntry {
	/// <summary>The label name.</summary>
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	/// <summary>Human-readable description.</summary>
	[JsonPropertyName("description")]
	public string? Description { get; set; }

	/// <summary>Symbol type.</summary>
	[JsonPropertyName("type")]
	public SymbolType Type { get; set; } = SymbolType.Label;
}

/// <summary>
/// Loads and caches game-specific label databases from JSON files.
/// The cache directory structure is: {cacheDir}/{crc32:x8}.json
/// </summary>
public class LabelDataCache {
	private readonly string _cacheDirectory;
	private static readonly JsonSerializerOptions _jsonOptions = new() {
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() },
	};

	/// <summary>
	/// Creates a new label data cache using the specified directory.
	/// </summary>
	/// <param name="cacheDirectory">Path to the cache directory for label databases.</param>
	public LabelDataCache(string cacheDirectory) {
		_cacheDirectory = cacheDirectory;
	}

	/// <summary>
	/// Try to load a cached label database for the given ROM CRC32.
	/// </summary>
	public GameLabelDatabase? Load(uint crc32) {
		var path = GetCachePath(crc32);
		if (!File.Exists(path)) return null;

		var json = File.ReadAllText(path, Encoding.UTF8);
		return JsonSerializer.Deserialize<GameLabelDatabase>(json, _jsonOptions);
	}

	/// <summary>
	/// Save a label database to the cache.
	/// </summary>
	public void Save(GameLabelDatabase database) {
		Directory.CreateDirectory(_cacheDirectory);
		var path = GetCachePath(database.Crc32);
		var json = JsonSerializer.Serialize(database, _jsonOptions);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	/// <summary>
	/// Check if a cached database exists for the given CRC32.
	/// </summary>
	public bool Exists(uint crc32) => File.Exists(GetCachePath(crc32));

	/// <summary>
	/// Check if a cached database is stale (older than maxAge).
	/// </summary>
	public bool IsStale(uint crc32, TimeSpan maxAge) {
		var db = Load(crc32);
		if (db == null) return true;
		return DateTimeOffset.UtcNow - db.FetchedAt > maxAge;
	}

	/// <summary>
	/// List all CRC32s that have cached data.
	/// </summary>
	public IEnumerable<uint> ListCached() {
		if (!Directory.Exists(_cacheDirectory)) yield break;

		foreach (var file in Directory.EnumerateFiles(_cacheDirectory, "*.json")) {
			var name = Path.GetFileNameWithoutExtension(file);
			if (uint.TryParse(name, System.Globalization.NumberStyles.HexNumber, null, out var crc32)) {
				yield return crc32;
			}
		}
	}

	/// <summary>
	/// Convert a GameLabelDatabase into MergedLabel entries for use with LabelMergeEngine.
	/// </summary>
	public static List<(uint Address, string Name, SymbolType Type, string? Description)> ToMergeLabels(
		GameLabelDatabase database) {
		var result = new List<(uint, string, SymbolType, string?)>();

		foreach (var (addrStr, entry) in database.RamLabels) {
			if (TryParseAddress(addrStr, out var addr)) {
				result.Add((addr, entry.Name, entry.Type, entry.Description));
			}
		}

		foreach (var (addrStr, entry) in database.RomLabels) {
			if (TryParseAddress(addrStr, out var addr)) {
				result.Add((addr, entry.Name, entry.Type, entry.Description));
			}
		}

		return result;
	}

	private string GetCachePath(uint crc32) =>
		Path.Combine(_cacheDirectory, $"{crc32:x8}.json");

	private static bool TryParseAddress(string addrStr, out uint address) {
		// Support "$xxxx", "0xxxxx", or plain hex
		var s = addrStr.TrimStart('$');
		if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
			s = s[2..];
		}
		return uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out address);
	}
}
