# Smart Mobility API

Realtids informationssystem til offentlig transport. Backend API til visning af buspositioner, ankomsttider og taxa-bestilling.

## Tech Stack

| Komponent | Teknologi |
|-----------|-----------|
| Framework | .NET 9 Web API |
| Database | SQLite (via EF Core) |
| ORM | Entity Framework Core 9 |
| API Docs | Swagger / OpenAPI |
| Pattern | Repository Pattern |

---

## Projektstruktur

```
SmartMobility/
├── SmartMobility.sln
├── README.md
├── Reference/
│   └── CONTEXT.md              # Kravspecifikation
└── SmartMobility/              # Web API projekt
    ├── Controllers/            # API endpoints
    │   ├── BusesController.cs
    │   ├── BusPositionsController.cs
    │   ├── RoutesController.cs
    │   └── StopsController.cs
    ├── Data/
    │   └── SmartMobilityDbContext.cs
    ├── DTOs/                   # Data Transfer Objects
    │   ├── BusDto.cs
    │   ├── BusPositionDto.cs
    │   ├── RouteDto.cs
    │   └── StopDto.cs
    ├── Migrations/             # EF Core migrations
    ├── Models/Entities/        # Database entiteter
    │   ├── Bus.cs
    │   ├── BusPosition.cs
    │   ├── Notification.cs
    │   ├── Route.cs
    │   ├── RouteStop.cs
    │   ├── Schedule.cs
    │   ├── Stop.cs
    │   ├── Taxi.cs
    │   ├── TaxiBooking.cs
    │   └── User.cs
    ├── Repositories/
    │   ├── Interfaces/
    │   │   └── IRepository.cs  # Generisk repository interface
    │   └── Repository.cs       # Generisk repository implementation
    ├── Program.cs
    ├── appsettings.json
    └── SmartMobility.db        # SQLite database fil
```

---

## Database Model

### ER Diagram (tekstuel)

```
User (1) ──────< (N) Notification
User (1) ──────< (N) TaxiBooking

Bus (1) ──────< (N) BusPosition
Bus (N) >────── (1) Route (CurrentRoute)
Bus (1) ──────< (N) Schedule

Route (1) ──────< (N) RouteStop >────── (1) Stop
Route (1) ──────< (N) Schedule
Route (1) ──────< (N) Notification

Taxi (1) ──────< (N) TaxiBooking
```

### Entiteter

#### User
Brugere med login-funktionalitet.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| Email | string | Unik email (index) |
| PasswordHash | string | Krypteret password |
| Name | string? | Visningsnavn |
| CreatedAt | DateTime | Oprettelsestidspunkt |

#### Bus
Busser/køretøjer i systemet.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| BusNumber | string | Busnummer (f.eks. "BUS-001") |
| LicensePlate | string? | Nummerplade |
| IsActive | bool | Om bussen er aktiv |
| CurrentRouteId | int? | FK til nuværende rute |

#### Stop
Stoppesteder med GPS-koordinater.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| Name | string | Navn på stoppested |
| Latitude | double | GPS breddegrad |
| Longitude | double | GPS længdegrad |
| Address | string? | Adresse |

#### Route
Busruter.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| RouteNumber | string | Rutenummer (f.eks. "1A") |
| Name | string? | Rutenavn |
| Description | string? | Beskrivelse |
| IsActive | bool | Om ruten er aktiv |

#### RouteStop
Kobling mellem ruter og stoppesteder (many-to-many med metadata).

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| RouteId | int | FK til Route |
| StopId | int | FK til Stop |
| StopOrder | int | Rækkefølge på ruten (unik per rute) |
| EstimatedMinutesFromStart | int? | Estimeret tid fra start |

#### BusPosition
GPS-positioner (telemetri data).

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| BusId | int | FK til Bus |
| Latitude | double | GPS breddegrad |
| Longitude | double | GPS længdegrad |
| Speed | double? | Hastighed (km/t) |
| Heading | double? | Retning (0-360 grader) |
| Timestamp | DateTime | Tidsstempel (index) |

#### Schedule
Køreplaner.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| RouteId | int | FK til Route |
| BusId | int? | FK til Bus |
| DayOfWeek | DayOfWeek | Ugedag |
| DepartureTime | TimeSpan | Afgangstid |
| ArrivalTime | TimeSpan? | Ankomsttid |
| IsActive | bool | Om køreplanen er aktiv |

#### Notification
Notifikationer til brugere.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| Title | string | Titel |
| Message | string | Besked |
| Type | NotificationType | General, Delay, Cancellation, NextStop |
| UserId | int? | FK til User (null = alle) |
| RouteId | int? | FK til Route |
| IsRead | bool | Læst-status |
| CreatedAt | DateTime | Oprettelsestidspunkt |

#### Taxi
Taxa køretøjer.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| LicensePlate | string | Nummerplade |
| DriverName | string? | Chaufførnavn |
| Status | TaxiStatus | Available, Busy, Offline |
| CurrentLatitude | double? | GPS position |
| CurrentLongitude | double? | GPS position |

#### TaxiBooking
Taxa-bestillinger.

| Felt | Type | Beskrivelse |
|------|------|-------------|
| Id | int | Primary key |
| UserId | int | FK til User |
| TaxiId | int? | FK til Taxi |
| PickupLatitude/Longitude | double | Afhentningssted |
| DestinationLatitude/Longitude | double | Destination |
| RequestedPickupTime | DateTime | Ønsket afhentningstid |
| Status | BookingStatus | Pending, Confirmed, InProgress, Completed, Cancelled |

---

## API Endpoints

### Stops Controller
`/api/Stops`

| Method | Endpoint | Beskrivelse |
|--------|----------|-------------|
| GET | `/api/Stops` | Hent alle stoppesteder |
| GET | `/api/Stops/{id}` | Hent specifikt stoppested |
| GET | `/api/Stops/nearby?latitude=X&longitude=Y&radiusKm=1` | Find nærliggende stoppesteder |
| GET | `/api/Stops/search?name=X` | Søg stoppesteder på navn |
| POST | `/api/Stops` | Opret nyt stoppested |
| PUT | `/api/Stops/{id}` | Opdater stoppested |
| DELETE | `/api/Stops/{id}` | Slet stoppested |

**Eksempel - Opret stoppested:**
```json
POST /api/Stops
{
  "name": "Rådhuspladsen",
  "latitude": 55.6761,
  "longitude": 12.5683,
  "address": "Rådhuspladsen, København"
}
```

### Routes Controller
`/api/Routes`

| Method | Endpoint | Beskrivelse |
|--------|----------|-------------|
| GET | `/api/Routes` | Hent alle ruter |
| GET | `/api/Routes/{id}` | Hent rute med stoppesteder |
| POST | `/api/Routes` | Opret ny rute |
| PUT | `/api/Routes/{id}` | Opdater rute |
| DELETE | `/api/Routes/{id}` | Slet rute |
| POST | `/api/Routes/{id}/stops` | Tilføj stoppested til rute |
| DELETE | `/api/Routes/{id}/stops/{stopId}` | Fjern stoppested fra rute |

**Eksempel - Opret rute:**
```json
POST /api/Routes
{
  "routeNumber": "1A",
  "name": "Centrum Linje",
  "description": "Fra Hovedbanegården til Østerport"
}
```

**Eksempel - Tilføj stoppested til rute:**
```json
POST /api/Routes/1/stops
{
  "stopId": 1,
  "stopOrder": 1,
  "estimatedMinutesFromStart": 0
}
```

### Buses Controller
`/api/Buses`

| Method | Endpoint | Beskrivelse |
|--------|----------|-------------|
| GET | `/api/Buses` | Hent alle busser |
| GET | `/api/Buses/{id}` | Hent bus med seneste position |
| GET | `/api/Buses/active` | Hent aktive busser med positioner |
| POST | `/api/Buses` | Opret ny bus |
| PUT | `/api/Buses/{id}` | Opdater bus |
| DELETE | `/api/Buses/{id}` | Slet bus |

**Eksempel - Opret bus:**
```json
POST /api/Buses
{
  "busNumber": "BUS-001",
  "licensePlate": "AB 12 345"
}
```

### BusPositions Controller
`/api/BusPositions`

| Method | Endpoint | Beskrivelse |
|--------|----------|-------------|
| GET | `/api/BusPositions/bus/{busId}` | Hent positionshistorik for bus |
| GET | `/api/BusPositions/bus/{busId}/latest` | Hent seneste position |
| GET | `/api/BusPositions/all/latest` | Hent alle aktive bussers positioner |
| POST | `/api/BusPositions` | Rapporter ny position (GPS) |

**Query parametre for historik:**
- `from` - Fra tidspunkt (DateTime)
- `to` - Til tidspunkt (DateTime)
- `limit` - Max antal resultater (default: 100)

**Eksempel - Rapporter GPS position:**
```json
POST /api/BusPositions
{
  "busId": 1,
  "latitude": 55.6761,
  "longitude": 12.5683,
  "speed": 25.5,
  "heading": 180.0
}
```

---

## Repository Pattern

Projektet bruger et generisk repository pattern til dataadgang.

### IRepository<T> Interface

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<bool> ExistsAsync(int id);
    Task<int> SaveChangesAsync();
}
```

### Brug i Controller

```csharp
public class ExampleController : ControllerBase
{
    private readonly IRepository<Bus> _busRepository;

    public ExampleController(IRepository<Bus> busRepository)
    {
        _busRepository = busRepository;
    }

    public async Task<IActionResult> GetBus(int id)
    {
        var bus = await _busRepository.GetByIdAsync(id);
        return bus == null ? NotFound() : Ok(bus);
    }
}
```

---

## Konfiguration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SmartMobility.db"
  }
}
```

### Program.cs Services

```csharp
// Database
builder.Services.AddDbContext<SmartMobilityDbContext>(options =>
    options.UseSqlite(connectionString));

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// JSON (case-insensitive)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

---

## Kør Projektet

### Forudsætninger
- .NET 9 SDK

### Start API

```bash
cd SmartMobility/SmartMobility
dotnet run
```

API kører på: `http://localhost:5174`
Swagger UI: `http://localhost:5174/swagger`

### Database Migrations

```bash
# Opret ny migration
dotnet ef migrations add MigrationName

# Anvend migrations
dotnet ef database update

# Fjern seneste migration
dotnet ef migrations remove
```

---

## Ikke Implementeret Endnu

Følgende features fra kravspecifikationen mangler:

| Feature | Status |
|---------|--------|
| User authentication | Entitet klar, controller mangler |
| Schedules controller | Entitet klar, controller mangler |
| Notifications controller | Entitet klar, controller mangler |
| Taxi/TaxiBooking controller | Entiteter klar, controllers mangler |
| ETA-beregning | Ikke implementeret |
| Realtidskommunikation | Ikke implementeret (SignalR/WebSockets) |

---

## Konventioner

- **Kodestandard:** Microsoft C# Coding Conventions
- **API:** RESTful design
- **Arkitektur:** Repository pattern (backend), MVVM (frontend - ikke implementeret)
- **Database:** Code-first med EF Core migrations
