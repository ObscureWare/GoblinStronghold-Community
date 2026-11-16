using System.Text.RegularExpressions;

namespace Transkit;

public static class Workflow
{
    public static void English(Package source) { Data.Shape(source); if (source.Locale != "en") throw new InvalidDataException("English source required."); }
    public static (Package Package, Kit Kit) Init(Package source, string locale, string author = "Translator", string title = "Community translation")
    {
        English(source);
        if (locale == "en") throw new InvalidDataException("Target locale must differ from en.");
        var target = new Package { FamilyId = source.FamilyId, PackageId = Guid.NewGuid(), SourcePackageId = source.PackageId,
            TranslationId = Guid.NewGuid(), Author = author, Title = title,
            Identity = source.Identity, Locale = locale, SourceVersion = source.SourceVersion,
            SourceHash = Data.Hash(source), Entries = new(source.Entries.ToDictionary(e => e.Key, e => new TextEntry("", false, e.Value.Context)), StringComparer.Ordinal) };
        Data.Shape(target);
        var changes = source.Entries.Select(e => new Change(e.Key, null, "add", "", e.Value.Text, "", "", false, e.Value.Context)).ToList();
        return (target, MakeKit(source, target, changes));
    }
    private static Kit MakeKit(Package source, Package basis, List<Change> changes) =>
        new() { Source = source, Base = basis, SourceHash = Data.Hash(source), BaseHash = Data.Hash(basis), Changes = changes };

    public static Kit Diff(Package oldSource, Package newSource, Package basis)
    {
        English(oldSource); English(newSource); Data.Shape(basis);
        if (oldSource.FamilyId != newSource.FamilyId || oldSource.PackageId == newSource.PackageId || oldSource.Identity != newSource.Identity || basis.Identity != oldSource.Identity || basis.Locale == "en" || basis.SourcePackageId != oldSource.PackageId || basis.FamilyId != newSource.FamilyId ||
            basis.SourceHash != Data.Hash(oldSource) || basis.SourceVersion != oldSource.SourceVersion)
            throw new InvalidDataException("Base translation does not match old English package.");
        if (System.Version.Parse(newSource.SourceVersion) <= System.Version.Parse(oldSource.SourceVersion))
            throw new InvalidDataException("New English version must be newer.");
        if (!basis.Entries.Keys.SequenceEqual(oldSource.Entries.Keys)) throw new InvalidDataException("Base keys differ from source.");
        // Re-key a tracked copy in declared chronological order; never infer renames from text similarity.
        var tracked = oldSource.Entries.ToDictionary(e => e.Key, e => (Original: e.Key, Entry: e.Value), StringComparer.Ordinal);
        foreach (var r in newSource.Renames.Where(r => System.Version.Parse(r.Since) > System.Version.Parse(oldSource.SourceVersion))
                     .OrderBy(r => System.Version.Parse(r.Since)).ThenBy(r => r.From, StringComparer.Ordinal))
        {
            if (!tracked.Remove(r.From, out var value)) throw new InvalidDataException("Rename source missing: " + r.From);
            if (!tracked.TryAdd(r.To, value)) throw new InvalidDataException("Rename destination already exists: " + r.To);
        }
        var changes = new List<Change>();
        foreach (var (key, entry) in newSource.Entries)
        {
            if (!tracked.Remove(key, out var old))
                changes.Add(new(key, null, "add", "", entry.Text, "", "", false, entry.Context));
            else
            {
                var translation = basis.Entries[old.Original];
                if (key != old.Original || entry.Text != old.Entry.Text || entry.Context != old.Entry.Context || !translation.Reviewed || translation.Text == "")
                    changes.Add(new(key, old.Original, key != old.Original ? "rename" : "change", old.Entry.Text, entry.Text,
                        translation.Text, translation.Text, translation.Reviewed && old.Entry.Text == entry.Text && old.Entry.Context == entry.Context, entry.Context));
            }
        }
        foreach (var (key, old) in tracked)
            changes.Add(new(key, old.Original, "delete", old.Entry.Text, "", basis.Entries[old.Original].Text, "", false, old.Entry.Context));
        return MakeKit(newSource, basis, changes.OrderBy(c => c.Key, StringComparer.Ordinal).ToList()) with { PreviousSource = oldSource };
    }
    public static Package Apply(Package basis, Kit kit, bool allowIncomplete)
    {
        CheckKit(kit);
        if (Data.Hash(basis) != kit.BaseHash) throw new InvalidDataException("Conflict: base package changed. Rebuild the transkit against this base.");
        var entries = new SortedDictionary<string, TextEntry>(basis.Entries, StringComparer.Ordinal);
        foreach (var change in kit.Changes)
            if (change.PreviousKey is not null) entries.Remove(change.PreviousKey);
        foreach (var c in kit.Changes)
            if (c.Kind != "delete") entries[c.Key] = new(c.Translation, c.Reviewed, c.Context);
        var result = basis with { PackageId = Guid.NewGuid(), SourcePackageId = kit.Source.PackageId, SourceVersion = kit.Source.SourceVersion, SourceHash = kit.SourceHash,
            Revision = basis.SourceVersion == kit.Source.SourceVersion ? checked(basis.Revision + 1) : 1, Entries = entries, Renames = [] };
        Validate(result, kit.Source, allowIncomplete);
        return result;
    }
    public static void CheckKit(Kit kit)
    {
        if (kit.FormatVersion != 1) throw new InvalidDataException("Unsupported transkit.");
        English(kit.Source); Data.Shape(kit.Base);
        if (kit.Base.FamilyId != kit.Source.FamilyId || kit.Base.Locale == "en" || kit.Base.Identity != kit.Source.Identity ||
            Data.Hash(kit.Source) != kit.SourceHash || Data.Hash(kit.Base) != kit.BaseHash) throw new InvalidDataException("Invalid transkit provenance.");
        var expected = kit.PreviousSource is not null ? Diff(kit.PreviousSource, kit.Source, kit.Base).Changes :
            kit.Source.Entries.Select(e => new Change(e.Key, null, "add", "", e.Value.Text, "", "", false, e.Value.Context)).ToList();
        if (kit.PreviousSource is null && (kit.Base.SourceHash != kit.SourceHash || kit.Base.SourcePackageId != kit.Source.PackageId ||
            kit.Base.Revision != 0 || kit.Base.Entries.Any(e => e.Value.Text != "" || e.Value.Reviewed) ||
            !kit.Base.Entries.Keys.SequenceEqual(kit.Source.Entries.Keys)))
            throw new InvalidDataException("Invalid initial translation base.");
        if (expected.Count != kit.Changes.Count || expected.Zip(kit.Changes).Any(pair =>
            pair.First with { Translation = pair.Second.Translation, Reviewed = pair.Second.Reviewed } != pair.Second))
            throw new InvalidDataException("Transkit operation plan modified. Regenerate it with init/diff.");
        if (kit.Changes.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count() != kit.Changes.Count) throw new InvalidDataException("Duplicate transkit key.");
        foreach (var c in kit.Changes)
        {
            if (!new[] { "add", "change", "rename", "delete" }.Contains(c.Kind) || c.Translation is null)
                throw new InvalidDataException("Invalid operation.");
            if (c.Kind != "delete" && (!kit.Source.Entries.TryGetValue(c.Key, out var s) || c.Source != s.Text || c.Context != s.Context))
                throw new InvalidDataException("Transkit source text modified.");
            if (c.PreviousKey is not null && (!kit.Base.Entries.TryGetValue(c.PreviousKey, out var b) || b.Text != c.PreviousTranslation))
                throw new InvalidDataException("Transkit previous translation modified.");
            if (c.Kind == "delete" && kit.Source.Entries.ContainsKey(c.Key)) throw new InvalidDataException("Cannot delete a current source key.");
            if (c.Reviewed && c.Kind != "delete") Contract(c.Source, c.Translation, c.Key);
        }
    }
    public static void Validate(Package p, Package? source, bool allowIncomplete)
    {
        Data.Shape(p);
        if (p.Locale == "en") { English(p); return; }
        if (source is null) throw new InvalidDataException("Target validation requires --source.");
        English(source);
        if (p.SourcePackageId != source.PackageId || p.FamilyId != source.FamilyId || p.Identity != source.Identity || p.SourceVersion != source.SourceVersion || p.SourceHash != Data.Hash(source))
            throw new InvalidDataException("Source identity/version/hash mismatch.");
        if (!p.Entries.Keys.SequenceEqual(source.Entries.Keys)) throw new InvalidDataException("Translation keys differ from English.");
        foreach (var (key, value) in p.Entries)
        {
            if (value.Text.Length != 0) Contract(source.Entries[key].Text, value.Text, key);
            if (!allowIncomplete && (!value.Reviewed || value.Text.Length == 0 && source.Entries[key].Text.Length != 0)) throw new InvalidDataException("Translation incomplete or unreviewed: " + key);
        }
    }
    public static void Contract(string source, string target, string key)
    {
        if (source == "" && target == "") return;
        if (string.IsNullOrWhiteSpace(target)) throw new InvalidDataException("Empty reviewed translation: " + key);
        // Preserve argument/action tokens and the existing lightweight bracket/HTML markup vocabulary.
        string Tokens(string value) => string.Join("\n", Regex.Matches(value, @"(?<!\{)\{[^{}]+\}(?!\})|</?[A-Za-z][^>]*>|\[/?[A-Za-z][^\]\r\n]*\]")
            .Select(m => m.Value).Order(StringComparer.Ordinal));
        if (Tokens(source) != Tokens(target)) throw new InvalidDataException("Placeholder or markup mismatch: " + key);
        var stack = new Stack<string>();
        foreach (Match match in Regex.Matches(target, @"</?(b|i|u|s|em|strong|code)>|\[/?(b|i|u|s|color|font|url|center|left|right|code|table|cell|p)(?:=[^\]]+)?\]"))
        {
            var token = match.Value; var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (token[1] == '/') { if (stack.Count == 0 || stack.Pop() != name) throw new InvalidDataException("Unbalanced formatting: " + key); }
            else stack.Push(name);
        }
        if (stack.Count != 0) throw new InvalidDataException("Unbalanced formatting: " + key);
    }
}
