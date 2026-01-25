# Smart Mobility

Et realtids informationssystem til offentlig transport. Passagerer kan se buspositioner på kort, få estimerede ankomsttider og modtage notifikationer. Chauffører kan aktivere GPS-tracking via mobilappen.

## Features

- **Realtids GPS-tracking** - Se buspositioner live på kort via SignalR
- **ETA-beregning** - Estimerede ankomsttider til stoppesteder
- **Notifikationer** - Forsinkelser, aflysninger og næste stoppested
- **Rolle-baseret adgang** - Passager, Chauffør og Admin
- **Rute-søgning** - Find og følg specifikke busruter

## Tech Stack

| Komponent | Teknologi |
|-----------|-----------|
| Backend | .NET 9 Web API |
| Database | SQLite / Entity Framework Core |
| Realtid | SignalR (WebSocket) |
| Auth | JWT + BCrypt |
| Mobile | .NET MAUI (Android) |
| Pattern | Repository (Backend), MVVM (Mobile) |

## Kom i gang

### Forudsætninger

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Android SDK](https://developer.android.com/studio) (til MAUI app)

### Installation

```bash
git clone https://github.com/dit-brugernavn/smartmobility.git
cd smartmobility
```

### Kør Backend

```bash
cd SmartMobility
dotnet run
```

API: `http://localhost:5174`
Swagger: `http://localhost:5174/swagger`

### Kør Mobile App

```bash
cd SmartMobilityApp
dotnet build -t:Run -f net9.0-android
```

## Projektstruktur

```
smartmobility/
├── SmartMobility/              # Backend Web API
│   ├── Controllers/            # API endpoints
│   ├── Services/               # Forretningslogik
│   ├── Repositories/           # Dataadgang
│   ├── Models/                 # Entiteter og Enums
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Hubs/                   # SignalR hub
│   └── Data/                   # DbContext
│
├── SmartMobilityApp/           # MAUI Mobile App
│   ├── Views/                  # XAML sider
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Services/               # API og SignalR klienter
│   └── Models/                 # DTOs og Enums
```

## API Oversigt

| Endpoint | Beskrivelse |
|----------|-------------|
| `POST /api/auth/register` | Opret bruger |
| `POST /api/auth/login` | Log ind |
| `GET /api/routes` | Hent alle ruter |
| `GET /api/routes/{id}` | Hent rute med stoppesteder |
| `GET /api/buses/active` | Hent aktive busser |
| `SignalR /gps-hub` | Realtids GPS-opdateringer |

Se fuld API dokumentation i Swagger UI.

## Roller

| Rolle | Rettigheder |
|-------|-------------|
| User | Se busser, ruter, modtage notifikationer |
| Driver | GPS-tracking, vælg bus, gå online/offline |
| Admin | Administrer ruter, send notifikationer, tildel roller |

## Udviklet til

Dette projekt er udviklet som svendeprøve-opgave med fokus på:
- SignalR realtidskommunikation
- JWT authentication og sikkerhed
