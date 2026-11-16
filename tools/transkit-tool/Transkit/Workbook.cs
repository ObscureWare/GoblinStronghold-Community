using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Transkit;

/// <summary>Dependency-free OpenXML exchange. Only Translation and Reviewed are editable.</summary>
public static class Workbook
{
    private static readonly XNamespace N = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly string[] Headers = ["Key", "Operation", "Previous EN", "Current EN", "Previous translation", "Translation", "Reviewed", "Context"];
    public static void Write(string path, Kit kit) => Data.Atomic(path, temp =>
    {
        using var zip = ZipFile.Open(temp, ZipArchiveMode.Create);
        void Part(string name, string text) { using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); w.Write(text); }
        Part("[Content_Types].xml", """<?xml version="1.0" encoding="utf-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
        Part("_rels/.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
        Part("xl/workbook.xml", """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Translations" sheetId="1" r:id="rId1"/><sheet name="_metadata" sheetId="2" state="veryHidden" r:id="rId2"/></sheets></workbook>""");
        Part("xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");
        Part("xl/styles.xml", """<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="3"><xf fontId="0" fillId="0" borderId="0" xfId="0"/><xf fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment wrapText="1" vertical="top"/></xf><xf fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1" applyProtection="1"><alignment wrapText="1" vertical="top"/><protection locked="0"/></xf></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>""");
        XElement Row(int row, IEnumerable<string> values, bool editable)
        {
            return new XElement(N + "row", new XAttribute("r", row), values.Select((text, col) =>
            {
                if (text.Length > 32767) throw new InvalidDataException("Text exceeds XLSX cell limit. Use JSON for this transkit.");
                return new XElement(N + "c", new XAttribute("r", ((char)('A' + col)).ToString() + row),
                    new XAttribute("t", "inlineStr"), new XAttribute("s", editable && col is 5 or 6 ? 2 : 1),
                    new XElement(N + "is", new XElement(N + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text)));
            }));
        }
        var sheet = new XElement(N + "worksheet",
            new XElement(N + "sheetViews", new XElement(N + "sheetView", new XAttribute("workbookViewId", 0),
                new XElement(N + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"), new XAttribute("state", "frozen")))),
            new XElement(N + "cols", new XElement(N + "col", new XAttribute("min", 1), new XAttribute("max", 8), new XAttribute("width", 34), new XAttribute("customWidth", 1))),
            new XElement(N + "sheetData", new[] { Row(1, Headers, false) }.Concat(kit.Changes.Select((c, i) => Row(i + 2, Values(c), true)))),
            new XElement(N + "sheetProtection", new XAttribute("sheet", 1), new XAttribute("autoFilter", 0)),
            new XElement(N + "autoFilter", new XAttribute("ref", "A1:H" + (kit.Changes.Count + 1))));
        Part("xl/worksheets/sheet1.xml", sheet.ToString());
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Data.Serialize(kit)));
        var chunks = Enumerable.Range(0, (payload.Length + 29999) / 30000).Select(i => payload.Substring(i * 30000, Math.Min(30000, payload.Length - i * 30000)));
        Part("xl/worksheets/sheet2.xml", new XElement(N + "worksheet", new XElement(N + "sheetData", chunks.Select((c, i) => Row(i + 1, [c], false)))).ToString());
    });
    private static string[] Values(Change c) => [c.Key, c.Kind, c.OldSource, c.Source, c.PreviousTranslation, c.Translation, c.Reviewed ? "true" : "false", c.Context];
    public static Kit Read(string path)
    {
        if (new FileInfo(path).Length > 64 * 1024 * 1024) throw new InvalidDataException("Workbook too large.");
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count > 128 || zip.Entries.Sum(e => e.Length) > 128L * 1024 * 1024 ||
            zip.Entries.Select(e => e.FullName).Distinct().Count() != zip.Entries.Count) throw new InvalidDataException("Invalid workbook archive.");
        XDocument Xml(string part)
        {
            using var input = (zip.GetEntry(part) ?? throw new InvalidDataException("Missing workbook part: " + part)).Open();
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 128 * 1024 * 1024 });
            return XDocument.Load(reader);
        }
        var shared = zip.GetEntry("xl/sharedStrings.xml") is null ? [] :
            Xml("xl/sharedStrings.xml").Descendants(N + "si").Select(s => string.Concat(s.Descendants(N + "t").Select(t => t.Value))).ToArray();
        string Value(XElement cell)
        {
            if (cell.Element(N + "f") is not null) throw new InvalidDataException("Formulas are not accepted in transkits.");
            var type = (string?)cell.Attribute("t");
            if (type == "b") return cell.Element(N + "v")?.Value switch { "1" => "true", "0" => "false", _ => throw new InvalidDataException("Invalid Boolean cell.") };
            return type == "inlineStr" ? string.Concat(cell.Descendants(N + "t").Select(t => t.Value)) :
                type == "s" ? shared[int.Parse(cell.Element(N + "v")!.Value)] : cell.Element(N + "v")?.Value ?? "";
        }
        // Resolve names through relationships so Excel/LibreOffice may renumber worksheet files.
        string Sheet(string name)
        {
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var id = (string?)Xml("xl/workbook.xml").Descendants(N + "sheet").Single(s => (string?)s.Attribute("name") == name).Attribute(rel + "id");
            var target = (string?)Xml("xl/_rels/workbook.xml.rels").Root!.Elements().Single(r => (string?)r.Attribute("Id") == id).Attribute("Target")
                ?? throw new InvalidDataException("Invalid worksheet relationship.");
            var normalized = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
            if (normalized.Contains("..") || normalized.Contains(':') || normalized.Contains('\\')) throw new InvalidDataException("Unsafe worksheet target.");
            return normalized;
        }
        var metadata = string.Concat(Xml(Sheet("_metadata")).Descendants(N + "row").OrderBy(r => (int)r.Attribute("r")!).SelectMany(r => r.Elements(N + "c")).Select(Value));
        var kit = Data.Parse<Kit>(Encoding.UTF8.GetString(Convert.FromBase64String(metadata)));
        var rows = Xml(Sheet("Translations")).Descendants(N + "row").ToArray();
        string[] Cells(XElement row)
        {
            var result = Enumerable.Repeat("", 8).ToArray(); var seen = new HashSet<int>();
            foreach (var cell in row.Elements(N + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? throw new InvalidDataException("Missing cell reference.");
                var column = reference.TakeWhile(char.IsLetter).ToArray();
                if (column.Length != 1 || column[0] < 'A' || column[0] > 'H') { if (Value(cell) != "") throw new InvalidDataException("Unexpected column."); continue; }
                var index = column[0] - 'A'; if (!seen.Add(index)) throw new InvalidDataException("Duplicate cell.");
                result[index] = Value(cell);
            }
            return result;
        }
        if (rows.Length == 0 || !Cells(rows[0]).SequenceEqual(Headers)) throw new InvalidDataException("Transkit headers changed.");
        var byKey = kit.Changes.ToDictionary(c => c.Key, StringComparer.Ordinal);
        var updated = new Dictionary<string, Change>(StringComparer.Ordinal);
        foreach (var row in rows.Skip(1))
        {
            var values = Cells(row);
            if (values.All(string.IsNullOrEmpty)) continue;
            if (!byKey.TryGetValue(values[0], out var original)) throw new InvalidDataException("Unknown key: " + values[0]);
            var expected = Values(original);
            foreach (var column in new[] { 0, 1, 2, 3, 4, 7 })
                if (values[column] != expected[column]) throw new InvalidDataException("Protected source column changed: " + original.Key);
            if (!bool.TryParse(values[6], out var reviewed)) throw new InvalidDataException("Reviewed must be true or false: " + original.Key);
            if (!updated.TryAdd(original.Key, original with { Translation = values[5], Reviewed = reviewed })) throw new InvalidDataException("Duplicate key.");
        }
        if (updated.Count != byKey.Count) throw new InvalidDataException("Transkit rows removed.");
        return kit with { Changes = kit.Changes.Select(c => updated[c.Key]).ToList() };
    }
}
