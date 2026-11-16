namespace Transkit;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "--help" or "help")
            {
                Console.WriteLine("""
transkit — independent community localization toolkit (format v1)
  pack     --input source.json --out source.langpack
  init     --source en.langpack --locale pl --author NAME --title TITLE --out pl.langpack --kit work.json
  diff     --from en-old.langpack --to en-new.langpack --base pl.langpack --kit update.json
  apply    --base pl.langpack --kit translated.xlsx|json --out pl-new.langpack [--allow-incomplete]
  validate --package file.langpack [--source en.langpack] [--allow-incomplete]
  validate --kit translated.xlsx|json
  export   --kit translated.xlsx|json --out work.json|xlsx
  status   --package file.langpack
Outputs are never overwritten. init/diff write JSON and XLSX. Edit one, then use export to synchronize.
""");
                return 0;
            }
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 1; i < args.Length; i++)
            {
                var key = args[i];
                if (!key.StartsWith("--") || !options.TryAdd(key, key == "--allow-incomplete" ? "true" :
                    ++i < args.Length ? args[i] : throw new ArgumentException("Missing value for " + key))) throw new ArgumentException("Invalid or duplicate option: " + key);
            }
            string Get(string name) => options.Remove("--" + name, out var value) ? value : throw new ArgumentException("Missing --" + name);
            bool allow = options.Remove("--allow-incomplete");
            void Done() { if (options.Count > 0) throw new ArgumentException("Unknown options: " + string.Join(", ", options.Keys)); }
            Kit ReadKit(string path) => Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ? Workbook.Read(path) : Data.Parse<Kit>(File.ReadAllText(path));
            void Pair(string path, Kit kit)
            {
                var json = Path.ChangeExtension(path, ".json"); var xlsx = Path.ChangeExtension(path, ".xlsx");
                if (File.Exists(json) || File.Exists(xlsx)) throw new IOException("Transkit output exists.");
                Workflow.CheckKit(kit);
                Workbook.Write(xlsx, kit);
                try { Data.Atomic(json, p => File.WriteAllText(p, Data.Serialize(kit))); }
                catch { File.Delete(xlsx); throw; }
            }
            switch (args[0])
            {
                case "pack":
                {
                    var input = Get("input"); var output = Get("out"); Done();
                    var p = Data.Read(input); Workflow.English(p); Data.Write(output, p); break;
                }
                case "init":
                {
                    var source = Get("source"); var locale = Get("locale"); var author = Get("author"); var title = Get("title");
                    var output = Get("out"); var kitPath = Get("kit"); Done();
                    if (new[] { Path.GetFullPath(output), Path.GetFullPath(Path.ChangeExtension(kitPath, ".json")), Path.GetFullPath(Path.ChangeExtension(kitPath, ".xlsx")) }.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
                        throw new ArgumentException("Package and transkit outputs must have distinct paths.");
                    if (File.Exists(output)) throw new IOException("Output exists.");
                    var result = Workflow.Init(Data.Read(source), locale, author, title);
                    Pair(kitPath, result.Kit); Data.Write(output, result.Package); break;
                }
                case "diff":
                {
                    var from = Get("from"); var to = Get("to"); var basis = Get("base"); var kitPath = Get("kit"); Done();
                    Pair(kitPath, Workflow.Diff(Data.Read(from), Data.Read(to), Data.Read(basis))); break;
                }
                case "apply":
                {
                    var basis = Get("base"); var kitPath = Get("kit"); var output = Get("out"); Done();
                    Data.Write(output, Workflow.Apply(Data.Read(basis), ReadKit(kitPath), allow)); break;
                }
                case "validate":
                {
                    if (options.ContainsKey("--kit"))
                    {
                        var path = Get("kit"); Done(); Workflow.CheckKit(ReadKit(path));
                    }
                    else
                    {
                        var path = Get("package"); var source = options.ContainsKey("--source") ? Get("source") : null; Done();
                        Workflow.Validate(Data.Read(path), source is null ? null : Data.Read(source), allow);
                    }
                    break;
                }
                case "export":
                {
                    var input = Get("kit"); var output = Get("out"); Done(); var kit = ReadKit(input); Workflow.CheckKit(kit);
                    if (Path.GetExtension(output).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) Workbook.Write(output, kit);
                    else Data.Atomic(output, p => File.WriteAllText(p, Data.Serialize(kit)));
                    break;
                }
                case "status":
                {
                    var path = Get("package"); Done(); var p = Data.Read(path); Data.Shape(p);
                    Console.WriteLine(Data.Serialize(new { p.PackageId, p.SourcePackageId, p.TranslationId, p.Author, p.Title, p.Locale, p.Version,
                        total = p.Entries.Count, translated = p.Entries.Values.Count(e => e.Text != ""), reviewed = p.Entries.Values.Count(e => e.Reviewed && e.Text != "") })); break;
                }
                default: throw new ArgumentException("Unknown command. Use --help.");
            }
            Console.WriteLine("OK"); return 0;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { Console.Error.WriteLine(error.Message); return 2; }
    }
}
