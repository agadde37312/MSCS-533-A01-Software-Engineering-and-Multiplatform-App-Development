# 📍 Location Heat Map — .NET MAUI

A cross-platform mobile application built with **.NET MAUI** (C#) that:
- Records the user's GPS position at regular intervals
- Persists every reading to a local **SQLite** database
- Visualises all stored positions as an interactive **heat map** overlaid on a native tile map

---

## Features

| Feature | Detail |
|---------|--------|
| **Continuous GPS tracking** | Polls location every 10 s using MAUI Geolocation API |
| **SQLite persistence** | All readings survive app restarts via `sqlite-net-pcl` |
| **Heat map overlay** | Custom `GraphicsView`-based overlay; colour gradient blue → green → yellow → red |
| **Grid clustering** | Nearby samples merged into weighted cells (≈ 55 m grid) |
| **MVVM architecture** | `MainViewModel` exposes `ICommand` and `INotifyPropertyChanged` |
| **Cross-platform** | Android, iOS, macOS (Catalyst), Windows |

---

## Architecture

```
LocationHeatMap/
├── Models/
│   ├── LocationEntry.cs       # SQLite entity (one GPS sample)
│   └── HeatMapPoint.cs        # Aggregated cluster for rendering
├── Services/
│   ├── DatabaseService.cs     # SQLite CRUD operations
│   ├── LocationService.cs     # MAUI Geolocation wrapper
│   └── HeatMapService.cs      # Clustering + colour mapping
├── ViewModels/
│   └── MainViewModel.cs       # MVVM binding target for MainPage
├── Views/
│   ├── MainPage.xaml          # Declarative UI (Map + Overlay + Controls)
│   └── MainPage.xaml.cs       # Viewport sync (map → overlay)
├── Controls/
│   └── HeatMapOverlay.cs      # Custom IDrawable GraphicsView overlay
└── Platforms/
    ├── Android/AndroidManifest.xml
    ├── iOS/Info.plist
    └── Windows/App.xaml.cs
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| Visual Studio 2022 (Windows) or Visual Studio for Mac 2022 | 17.8+ |
| .NET SDK | 8.0+ |
| .NET MAUI workload | `dotnet workload install maui` |
| Android SDK | API 21+ |
| Xcode (iOS / macOS builds) | 15+ |

---

## Setup

### 1. Clone the repository
```bash
git clone https://github.com/<your-username>/LocationHeatMap.git
cd LocationHeatMap
```

### 2. Obtain API keys

#### Android
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Enable **Maps SDK for Android**
3. Create an API key and paste it into `Platforms/Android/AndroidManifest.xml`:
```xml
<meta-data android:name="com.google.android.geo.API_KEY"
           android:value="YOUR_KEY_HERE" />
```

#### iOS / macOS
1. Enable **Maps SDK for iOS**
2. Paste the key into `Platforms/iOS/Info.plist`:
```xml
<key>GMSApiKey</key>
<string>YOUR_KEY_HERE</string>
```

### 3. Restore NuGet packages
```bash
dotnet restore
```

### 4. Run the app
```bash
# Android emulator
dotnet build -t:Run -f net8.0-android

# iOS simulator (macOS only)
dotnet build -t:Run -f net8.0-ios

# Windows
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

---

## How It Works

1. **Tap ▶ Start** — the app requests location permission and begins polling the platform GPS every 10 seconds.
2. Each reading is saved to `LocationHeatMap.db3` in the app's sandboxed data directory.
3. The `HeatMapService` groups nearby readings into 55 m grid cells and calculates a normalised intensity for each cell.
4. The `HeatMapOverlay` (a transparent `GraphicsView`) converts GPS coordinates to screen pixels using the map's `VisibleRegion` bounding box, then paints radial colour circles.
5. **Tap ↻ Map** to manually refresh. **Tap 🗑 Clear** to wipe all stored readings.

---

## Coding Standards

This project follows the [Common C# Code Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):
- PascalCase for types, methods, and public members
- camelCase with `_` prefix for private fields
- XML doc comments on all public APIs
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- `async`/`await` throughout; no `.Result` or `.Wait()`

---

## License

MIT © 2024 Your Name
