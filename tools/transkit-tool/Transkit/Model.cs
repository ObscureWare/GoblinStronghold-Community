using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace Transkit;

public sealed record Identity(string Domain, string Id);
public sealed record TextEntry(string Text, bool Reviewed = false, string Context = "");
public sealed record Rename(string Since, string From, string To);
public sealed record Package
{
    public int FormatVersion { get; init; } = 1;
    public Guid FamilyId { get; init; }
    public Guid PackageId { get; init; }
    public Guid SourcePackageId { get; init; }
    public Guid TranslationId { get; init; }
    public string Author { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public Identity Identity { get; init; } = new("core", "core");
    public string Locale { get; init; } = "en";
    public string SourceVersion { get; init; } = "1.0.0";
    public int Revision { get; init; }
    public string SourceHash { get; init; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Version => Locale == "en" ? SourceVersion : SourceVersion + "." + Revision;
    public SortedDictionary<string, TextEntry> Entries { get; init; } = new(StringComparer.Ordinal);
    public List<Rename> Renames { get; init; } = [];
}
public sealed record Change(string Key, string? PreviousKey, string Kind, string OldSource,
    string Source, string PreviousTranslation, string Translation, bool Reviewed, string Context);
public sealed record Kit
{
    public int FormatVersion { get; init; } = 1;
    public Package Source { get; init; } = new();
    public Package? PreviousSource { get; init; }
    public Package Base { get; init; } = new();
    public string BaseHash { get; init; } = "";
    public string SourceHash { get; init; } = "";
    public List<Change> Changes { get; init; } = [];
}

public static class Data
{
    public static readonly JsonSerializerOptions Json = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false, WriteIndented = true,
      UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
    public static T Parse<T>(string text)
    {
        using var document = JsonDocument.Parse(text);
        void Unique(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in node.EnumerateObject()) { if (!names.Add(p.Name)) throw new InvalidDataException("Duplicate JSON key: " + p.Name); Unique(p.Value); }
            }
            else if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) Unique(item);
        }
        Unique(document.RootElement);
        var result = JsonSerializer.Deserialize<T>(text, Json) ?? throw new InvalidDataException("Empty JSON.");
        Package Sorted(Package p) => p with { Entries = new(p.Entries, StringComparer.Ordinal) };
        if (result is Package package) return (T)(object)Sorted(package);
        if (result is Kit kit) return (T)(object)(kit with { Source = Sorted(kit.Source), Base = Sorted(kit.Base), PreviousSource = kit.PreviousSource is null ? null : Sorted(kit.PreviousSource) });
        return result;
    }
    public static string Hash(Package value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(value with
    {
        Entries = new SortedDictionary<string, TextEntry>(value.Entries, StringComparer.Ordinal),
        Renames = value.Renames.OrderBy(r => r.Since, StringComparer.Ordinal).ThenBy(r => r.From, StringComparer.Ordinal).ToList()
    }))));
    public static Package Read(string path)
    {
        if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("Package too large.");
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            return Parse<Package>(File.ReadAllText(path));
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count != 1 || zip.Entries[0].FullName != "langpack.json") throw new InvalidDataException("Expected exactly langpack.json.");
        if (zip.Entries[0].Length > 32 * 1024 * 1024) throw new InvalidDataException("Expanded package too large.");
        using var reader = new StreamReader(zip.Entries[0].Open());
        return Parse<Package>(reader.ReadToEnd());
    }
    public static void Write(string path, Package package)
    {
        Atomic(path, temp =>
        {
            if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)) File.WriteAllText(temp, Serialize(package), new UTF8Encoding(false));
            else
            {
                using var zip = ZipFile.Open(temp, ZipArchiveMode.Create);
                var entry = zip.CreateEntry("langpack.json"); entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(Serialize(package));
            }
        });
    }
    public static void Atomic(string path, Action<string> write)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path)) throw new IOException("Output already exists: " + path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { write(temporary); File.Move(temporary, path, false); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static void Shape(Package p)
    {
        if (p.FamilyId == Guid.Empty || p.PackageId == Guid.Empty || p.Author is null || p.Title is null || p.Description is null)
            throw new InvalidDataException("Package/family GUID and author metadata required.");
        if (p.FamilyId == p.PackageId || p.Locale != "en" && new[] { p.FamilyId, p.PackageId, p.SourcePackageId, p.TranslationId }.Distinct().Count() != 4)
            throw new InvalidDataException("GUID roles must not reuse the same identifier.");
        if (p.Locale == "en" ? p.SourcePackageId != Guid.Empty || p.TranslationId != Guid.Empty :
            p.SourcePackageId == Guid.Empty || p.TranslationId == Guid.Empty || string.IsNullOrWhiteSpace(p.Author) || string.IsNullOrWhiteSpace(p.Title))
            throw new InvalidDataException("Invalid translation identity.");
        if (p.FormatVersion != 1 || p.Identity is null || p.Entries is null || p.Renames is null)
            throw new InvalidDataException("Unsupported or incomplete package.");
        if (!new[] { "game-ui", "editor-ui", "core", "mod", "map", "tutorial" }.Contains(p.Identity.Domain) ||
            !Regex.IsMatch(p.Identity.Id, "^[a-z0-9][a-z0-9:._-]*$")) throw new InvalidDataException("Invalid package identity.");
        if (!Regex.IsMatch(p.Locale, "^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$") ||
            !Regex.IsMatch(p.SourceVersion, "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$") ||
            !System.Version.TryParse(p.SourceVersion, out _) || p.Revision < 0) throw new InvalidDataException("Invalid locale or version.");
        if (p.Locale == "en" ? p.Revision != 0 || p.SourceHash != "" : !Regex.IsMatch(p.SourceHash, "^[a-f0-9]{64}$"))
            throw new InvalidDataException("Invalid source reference.");
        if (p.Entries.Any(e => string.IsNullOrWhiteSpace(e.Key) || e.Value is null || e.Value.Text is null || e.Value.Context is null))
            throw new InvalidDataException("Invalid entry.");
        if (p.Entries.Count > 100000) throw new InvalidDataException("Too many entries.");
        foreach (var r in p.Renames)
            if (r is null || !Regex.IsMatch(r.Since, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$") || !System.Version.TryParse(r.Since, out var since) || since > System.Version.Parse(p.SourceVersion) ||
                string.IsNullOrWhiteSpace(r.From) || string.IsNullOrWhiteSpace(r.To) || r.From == r.To)
                throw new InvalidDataException("Invalid rename metadata.");
        if (p.Renames.GroupBy(r => (r.Since, r.From)).Any(g => g.Count() > 1) ||
            p.Renames.GroupBy(r => (r.Since, r.To)).Any(g => g.Count() > 1)) throw new InvalidDataException("Ambiguous rename metadata.");
    }
}
