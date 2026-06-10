# MAUI workload setup

The **TebbiDashboard.Maui** project is included in the solution but requires the .NET MAUI workload to build.

## Error during install

If `dotnet workload install maui` fails with **"There is not enough space on the disk"**, the Android SDK component needs several GB on `C:`.

### Fix

1. Free disk space on `C:` (at least **8 GB** recommended for Android SDK + emulator)
2. Run in an elevated terminal:

```bash
dotnet workload install maui
```

3. Verify:

```bash
dotnet workload list
```

4. Build MAUI (Windows first):

```bash
cd src/TebbiDashboard.Maui
dotnet build -f net10.0-windows10.0.19041.0
```

## Targets

| Platform | Framework | Use case |
|----------|-----------|----------|
| Windows | `net10.0-windows10.0.19041.0` | Office PCs |
| Android | `net10.0-android` | Terrein / barcode scan |

## Until MAUI builds

- Run **Web API**: `dotnet run --project src/TebbiDashboard.Api`
- Run **Blazor Web**: `dotnet run --project src/TebbiDashboard.Web`
- UI tests: `dotnet test tests/TebbiDashboard.UI.Tests`
