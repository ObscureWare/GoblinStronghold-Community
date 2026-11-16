using Transkit;
using System.IO.Compression;
using System.Xml.Linq;

var root = Path.Combine(Path.GetTempPath(), "transkit-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Reject(Action action, string message)
{
    try { action(); } catch (InvalidDataException) { checks++; return; } catch (IOException) { checks++; return; }
    throw new Exception("Accepted invalid input: " + message);
}
var family = Guid.Parse("7ab1caa9-90f8-44da-a799-3dd3ad368f90");
var en = new Package { FamilyId = family, PackageId = Guid.NewGuid(), Author = "Core authors", Title = "Game UI",
    Identity = new("game-ui", "core"), SourceVersion = "1.0.0",
    Entries = new(StringComparer.Ordinal) { ["a"] = new("Hello {0}", true, "Greeting"), ["gone"] = new("Gone", true), ["same"] = new("Unchanged", true) } };
var initial = Workflow.Init(en, "pl", "Team One", "Polski");
var rival = Workflow.Init(en, "pl", "Team Two", "Alternative");
Check(initial.Package.TranslationId != rival.Package.TranslationId, "Rival translation lineage collides.");
Check(initial.Package.SourcePackageId == en.PackageId && rival.Package.SourcePackageId == en.PackageId, "Wrong English GUID.");
Check(initial.Package.PackageId != rival.Package.PackageId, "Package GUID collides.");
Reject(() => Workflow.Apply(rival.Package, initial.Kit, true), "rival base");
var translated = initial.Kit with { Changes = initial.Kit.Changes.Select(c => c with { Translation = c.Key == "a" ? "Witaj {0}" : "Gotowe", Reviewed = true }).ToList() };
var pl = Workflow.Apply(initial.Package, translated, false);
Check(pl.Version == "1.0.0.1" && pl.PackageId != initial.Package.PackageId && pl.TranslationId == initial.Package.TranslationId, "Revision identity.");
Workflow.Validate(pl, en, false); checks++;
Reject(() => Workflow.Apply(initial.Package, initial.Kit, false), "incomplete");
Check(!Workflow.Apply(initial.Package, initial.Kit, true).Entries["a"].Reviewed, "Draft incorrectly approved.");
var en2 = en with { PackageId = Guid.NewGuid(), SourceVersion = "1.1.0",
    Entries = new(StringComparer.Ordinal) { ["renamed"] = en.Entries["a"], ["same"] = en.Entries["same"], ["new"] = new("New {action:build}", true) },
    Renames = [new("1.1.0", "a", "renamed")] };
var delta = Workflow.Diff(en, en2, pl);
Check(delta.Changes.Single(c => c.Key == "renamed").Translation == "Witaj {0}", "Rename lost translation.");
Check(delta.Changes.Single(c => c.Key == "renamed").Reviewed, "Unchanged renamed text unnecessarily loses review.");
Check(delta.Changes.Any(c => c.Kind == "delete" && c.Key == "gone"), "Missing deletion.");
var filled = delta with { Changes = delta.Changes.Select(c => c.Kind == "add" ? c with { Translation = "Nowe {action:build}", Reviewed = true } : c).ToList() };
var updated = Workflow.Apply(pl, filled, false);
Check(updated.SourcePackageId == en2.PackageId && updated.Version == "1.1.0.1", "Wrong target version.");
Check(!updated.Entries.ContainsKey("gone") && !updated.Entries.ContainsKey("a") && updated.Entries.ContainsKey("renamed"), "Wrong migration keys.");
var en3 = en2 with { PackageId = Guid.NewGuid(), SourceVersion = "1.2.0", Entries = new(en2.Entries, StringComparer.Ordinal) };
en3.Entries["renamed"] = new("Welcome {0}", true, "Changed context");
var changed = Workflow.Diff(en2, en3, updated);
Check(!changed.Changes.Single(c => c.Key == "renamed").Reviewed, "Changed source not marked for review.");
Reject(() => Workflow.Apply(updated, changed, false), "unreviewed changed source");
Reject(() => Workflow.CheckKit(changed with { Changes = [] }), "removed operations");
Reject(() => Workflow.Diff(en, en2 with { FamilyId = Guid.NewGuid() }, pl), "different family");
Reject(() => Workflow.Diff(en, en2 with { Identity = new("editor-ui", "core") }, pl), "different domain");
Reject(() => Workflow.Apply(pl with { PackageId = Guid.NewGuid() }, delta, true), "different edition GUID");
Reject(() => Workflow.Contract("Hello {0}", "Witaj {1}", "a"), "placeholder changed");
Reject(() => Workflow.Contract("{0} {0}", "{0}", "a"), "placeholder occurrence lost");
Reject(() => Workflow.Contract("[b]Text[/b]", "Tekst", "a"), "markup lost");
Reject(() => Data.Parse<Package>("{\"locale\":\"en\",\"locale\":\"pl\"}"), "duplicate JSON key");
var archive = Path.Combine(root, "en.langpack"); Data.Write(archive, en);
Check(Data.Hash(Data.Read(archive)) == Data.Hash(en), "Package roundtrip.");
Reject(() => Data.Write(archive, en), "overwrite");
var workbook = Path.Combine(root, "translated.xlsx"); Workbook.Write(workbook, translated);
Check(Data.Serialize(Workbook.Read(workbook)) == Data.Serialize(translated), "XLSX roundtrip.");
var xlsxResult = Workflow.Apply(initial.Package, Workbook.Read(workbook), false);
Check(xlsxResult.Entries["a"].Text == "Witaj {0}", "XLSX apply.");
// Emulate an actual spreadsheet edit of the unlocked translation cells.
var editable = Path.Combine(root, "editable.xlsx"); Workbook.Write(editable, initial.Kit);
void EditCell(string path, string reference, string value, bool formula = false)
{
    using var zip = ZipFile.Open(path, ZipArchiveMode.Update);
    var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
    XDocument xml; using (var input = entry.Open()) xml = XDocument.Load(input);
    XNamespace n = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    var cell = xml.Descendants(n + "c").Single(c => (string?)c.Attribute("r") == reference);
    if (formula) cell.Add(new XElement(n + "f", value)); else cell.Descendants(n + "t").Single().Value = value;
    entry.Delete(); using var output = zip.CreateEntry("xl/worksheets/sheet1.xml").Open(); xml.Save(output);
}
EditCell(editable, "F2", "Cześć {0}"); EditCell(editable, "G2", "true");
Check(Workbook.Read(editable).Changes[0].Translation == "Cześć {0}", "Editable translation not imported.");
EditCell(editable, "D2", "Tampered English");
Reject(() => Workbook.Read(editable), "protected source edited");
var formulas = Path.Combine(root, "formula.xlsx"); Workbook.Write(formulas, translated);
EditCell(formulas, "F2", "1+1", true); Reject(() => Workbook.Read(formulas), "formula");
var ctx = System.Globalization.CultureInfo.CurrentCulture;
System.Globalization.CultureInfo.CurrentCulture = new("pl-PL");
Check(Data.Hash(Data.Parse<Package>(Data.Serialize(en))) == Data.Hash(en), "Culture-sensitive hash.");
System.Globalization.CultureInfo.CurrentCulture = ctx;
var invalidRename = en2 with { Renames = [new("1.1.0", "a", "same")] };
Reject(() => Workflow.Diff(en, invalidRename, pl), "rename collision");
Reject(() => Workflow.Contract("[b][i]Text[/i][/b]", "[b][i]Tekst[/b][/i]", "format"), "misnested formatting");
var chain = en2 with { PackageId = Guid.NewGuid(), SourceVersion = "1.2.0",
    Renames = [new("1.1.0", "a", "middle"), new("1.2.0", "middle", "renamed")] };
Check(Workflow.Diff(en, chain, pl).Changes.Any(c => c.PreviousKey == "a" && c.Key == "renamed"), "Version-skipping rename chain.");
var kitJson = Path.Combine(root, "filled.json");
File.WriteAllText(kitJson, Data.Serialize(translated));
var cliOut = Path.Combine(root, "cli.langpack"); var basePath = Path.Combine(root, "base.langpack"); Data.Write(basePath, initial.Package);
Check(Transkit.Program.Main(["apply", "--base", basePath, "--kit", kitJson, "--out", cliOut]) == 0, "CLI apply failed.");
Check(Transkit.Program.Main(["validate", "--package", cliOut, "--source", archive]) == 0, "CLI validate failed.");
Check(Transkit.Program.Main(["apply", "--base", basePath, "--kit", kitJson, "--out", cliOut]) == 2, "CLI overwrite should fail.");
var diffPath = Path.Combine(root, "delta.json"); var newPath = Path.Combine(root, "new.langpack"); Data.Write(newPath, en2);
Check(Transkit.Program.Main(["diff", "--from", archive, "--to", newPath, "--base", cliOut, "--kit", diffPath]) == 0, "CLI diff failed.");
Check(File.Exists(Path.ChangeExtension(diffPath, ".xlsx")), "CLI missing XLSX.");
var booleanBook = Path.Combine(root, "boolean.xlsx"); Workbook.Write(booleanBook, translated);
using (var zip = ZipFile.Open(booleanBook, ZipArchiveMode.Update))
{
    var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
    XDocument xml; using (var input = entry.Open()) xml = XDocument.Load(input);
    XNamespace n = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    var cell = xml.Descendants(n + "c").Single(c => (string?)c.Attribute("r") == "G2");
    cell.SetAttributeValue("t", "b"); cell.RemoveNodes(); cell.Add(new XElement(n + "v", "1"));
    entry.Delete(); using var output = zip.CreateEntry("xl/worksheets/sheet1.xml").Open(); xml.Save(output);
}
Check(Workbook.Read(booleanBook).Changes[0].Reviewed, "Excel Boolean cell not imported.");
var exportJson = Path.Combine(root, "exported.json");
Check(Transkit.Program.Main(["export", "--kit", booleanBook, "--out", exportJson]) == 0, "CLI XLSX to JSON.");
Check(Transkit.Program.Main(["validate", "--kit", exportJson]) == 0, "CLI transkit validation.");
Reject(() => Data.Shape(pl with { PackageId = en.PackageId }), "English GUID reused for translation edition");
Reject(() => Data.Shape(en2 with { Renames = [new("1.1", "a", "renamed")] }), "incomplete rename version");
Console.WriteLine($"PASS: {checks} checks. Artifacts: {root}");
