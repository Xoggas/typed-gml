# TypedGML CLI

## Commands

### Initialize a GameMaker project

```powershell
dotnet run --project .\TypedGML.CLI\TypedGML.CLI.csproj -- init --path "C:\Path\To\GameMakerProject"
```

What it does:
- creates `tgml_source`
- creates `tgml_source\Assets`
- generates `Sprites.tgml`, `Sounds.tgml`, `Rooms.tgml`, and `Fonts.tgml`
- ensures GameMaker folder metadata for generated TypedGML resources exists

### Build TypedGML into GameMaker resources

```powershell
dotnet run --project .\TypedGML.CLI\TypedGML.CLI.csproj -- build --path "C:\Path\To\GameMakerProject"
```

What it does:
- regenerates asset registry files from the GameMaker project
- transpiles the bundled `BCL` plus all `tgml_source\**\*.tgml`
- writes generated scripts into `scripts\...`
- updates existing objects by replacing only managed event/code entries
- creates default objects when a referenced `@Object("...")` does not exist yet
- updates `.yyp` and `.resource_order` for created folders/resources

