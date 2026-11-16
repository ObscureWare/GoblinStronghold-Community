# Goblin Stronghold Editor — standalone Windows edition

## English

Extract the entire ZIP into a writable folder, then run `GoblinStronghold.Editor.exe`.
Windows x64 is required. The .NET runtime is included; no SDK, Git checkout, Godot installation or Steam login is required for authoring.

Keep `data` beside the executable. It contains a versioned `editor-workspace.json`, editable content catalogs, the source workbook and schema, English/Polish localization, artwork recipe definitions, maps and tutorials. The `src` names inside it are compatibility paths for authoring data; they contain no C# source or game project. Do not mix data from different editor releases.

Map editor → Open map starts in the bundled Maps directory, including Great mine. Use Save as to keep the original. Terrain brushes offer preview and undo/redo. The tutorial editor can import, edit and export `.tut` packages; content-pack tools load and edit supported pack archives. Core catalog edits change this extracted copy, not your installed game. Back up your work before replacing the distribution.

Game artwork rendering/atlas baking and launching a live tutorial session from the editor are developer-runtime features and are unavailable in this standalone edition. The game runtime and generated game atlases are not bundled; recipe source images are included for authoring validation. Map previews and tutorial text editing remain available; export packages and load them in a compatible game for playtesting.

For diagnostics, run from PowerShell (use a new output folder):

```powershell
& .\GoblinStronghold.Editor.exe --verify-distribution "$env:TEMP\GoblinEditorVerification"
```

The offline diagnostic opens catalogs, validates them, loads/edits/saves/reopens the bundled mine, checks undo/redo, generates a map, exports/imports a tutorial and captures EN/PL map windows. It exits after writing `result.json`, screenshots and test packages in the requested directory. It does not alter bundled maps or contact any service. A report with `success: true` indicates completion.

## Polski

Rozpakuj cały ZIP do katalogu z prawem zapisu i uruchom `GoblinStronghold.Editor.exe`.
Środowisko .NET jest dołączone. Do edycji nie potrzeba SDK, prywatnego repozytorium, Godota ani logowania Steam. Folder `data` musi pozostać obok programu; nie mieszaj go z danymi innego wydania.

W edytorze map przycisk **Otwórz mapę** wskazuje dołączone paczki, w tym Wielką kopalnię. **Zapisz jako** pozwala zachować oryginał. Dostępne są pędzle z podglądem i cofaniem, edytor tutoriali z eksportem `.tut` oraz edycja katalogów i paczek zawartości. Zmiany katalogów bazowych dotyczą rozpakowanej kopii danych, nie instalacji gry. Zrób kopię własnych zmian przed aktualizacją.

Renderowanie grafik gry, wypalanie atlasów i uruchamianie rozgrywki tutoriala z edytora wymagają środowiska deweloperskiego i nie są częścią tej paczki. Podgląd map oraz edycja treści tutoriala działają. Test rozgrywki wykonaj w zgodnej wersji gry po wyeksportowaniu paczki.

Powyższe polecenie diagnostyczne zapisuje wynik, zrzuty okien i próbne paczki w podanym katalogu. Nie zmienia dołączonych map i nie wysyła danych do sieci.
