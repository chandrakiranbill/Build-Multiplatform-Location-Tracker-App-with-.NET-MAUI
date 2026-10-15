# Location Tracker (.NET MAUI)

A cross-platform app that tracks the user's position, saves every reading to a local
**SQLite** database, and visualises the history as a **heat map** on top of the
.NET MAUI **Maps** control (blue = rarely visited, red = frequently visited).

| Platform     | Map provider | Status |
|--------------|--------------|--------|
| Android      | Google Maps  | ✅ (needs API key) |
| iOS          | Apple Maps   | ✅ |
| Mac Catalyst | Apple Maps   | ✅ |
| Windows      | –            | ❌ The MAUI Map control is not supported on Windows |

## Requirements

* Visual Studio 2022 (17.12+) or Visual Studio 2026 on Windows, or VS Code + .NET MAUI extension on Mac
* **.NET MAUI** workload (`dotnet workload install maui`)
* .NET 9 SDK (to use .NET 10, change `net9.0` to `net10.0` in `LocationTracker.csproj`)
* Android emulator (API 21+) or an iOS simulator / device

## Getting started

1. Open **`LocationTracker.sln`** in Visual Studio.
2. **Android only:** get a Google Maps API key (Google Cloud Console → enable
   *Maps SDK for Android* → Credentials → Create API key) and paste it into
   `LocationTracker/Platforms/Android/AndroidManifest.xml`, replacing `YOUR_GOOGLE_MAPS_API_KEY`.
3. Select an Android emulator (or iOS simulator) as the run target and press **F5**.
4. Allow location access when prompted.
5. Move around — or in the emulator use **Extended controls (⋯) → Location → Routes** to
   play back a route — and watch the heat map build up.
   Tap **Demo route** to insert a sample track instantly.

## Project structure

```
LocationTracker/
├── Controls/
│   ├── HeatMap.cs              # Extends the MAUI Map: bindable Points → coloured circles
│   └── HeatMapCalculator.cs    # Grid clustering + blue→red colour gradient (pure logic)
├── Models/
│   └── LocationPoint.cs        # SQLite table row
├── Services/
│   ├── ILocationRepository.cs / LocationRepository.cs           # SQLite storage (sqlite-net)
│   ├── ILocationTrackingService.cs / LocationTrackingService.cs # GPS listening + saving
│   └── SampleRouteGenerator.cs # Demo data for emulators
├── ViewModels/
│   └── MainViewModel.cs        # MVVM (CommunityToolkit.Mvvm)
├── Views/
│   └── MainView.xaml(.cs)      # Page titled "MainView"
├── Platforms/
│   ├── Android/AndroidManifest.xml   # Permissions + Google Maps API key
│   ├── iOS/Info.plist                # Location usage descriptions
│   └── MacCatalyst/...
└── MauiProgram.cs              # UseMauiMaps() + dependency injection
```

## How it works

1. **Tracking** – `LocationTrackingService` calls
   `Geolocation.StartListeningForegroundAsync` (best accuracy, every 5 s).
   Readings within 10 m of the previous saved point are skipped to avoid GPS jitter.
2. **Storage** – each accepted reading is inserted into the `LocationPoints` table
   (`locations.db3` in `FileSystem.AppDataDirectory`).
3. **Heat map** – `HeatMap` (a subclass of `Microsoft.Maui.Controls.Maps.Map`) groups the
   points into ~150 m grid cells, counts the points per cell, and adds a `Circle` map
   element per cell whose colour runs blue → cyan → green → yellow → red with density.
   It listens to the bound `ObservableCollection`, so new points appear live.

## Publishing to GitHub

```bash
cd LocationTracker
git init
git add .
git commit -m "Location Tracker .NET MAUI app"
git branch -M main
git remote add origin https://github.com/<your-username>/LocationTracker.git
git push -u origin main
```

> Don't commit a real Google Maps API key to a public repository — restrict the key to your
> app's package name / SHA-1 in Google Cloud Console, or replace it with the placeholder before pushing.
