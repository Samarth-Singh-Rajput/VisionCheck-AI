# VisionCheck-AI — Backend API Server (`VisionCheckAI.Server`)

The backend for **VisionCheck-AI** is built as a RESTful Web API using **ASP.NET Core 8 (.NET 9 compatible)** and **Entity Framework Core with SQLite**. 

It acts as the central orchestrator connecting the **Blazor WebAssembly frontend** with the **PyTorch EfficientNet-B0 AI model**, managing user authentication, product catalogs, image uploads, AI inference execution, inspection audit logs, and dashboard analytical aggregations.

---

## Technical Stack & Architecture

- **Framework**: ASP.NET Core Web API (.NET 8 / .NET 9)
- **Database**: SQLite (`visioncheck.db`) managed via Entity Framework Core 9.0.2
- **Authentication**: JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **API Documentation**: OpenAPI / Swagger UI (`Swashbuckle.AspNetCore`)
- **AI Model Bridge**: C# `System.Diagnostics.Process` execution bridge calling Python PyTorch predictor (`predict.py --json`)

---

## Code Base & File Walkthrough

Below is a detailed breakdown of every file in the backend codebase and what each piece of code does:

```text
backend/VisionCheckAI.Server/
├── Program.cs                         Application entry point, middleware pipeline & service configuration
├── VisionCheckAI.Server.csproj        Project configuration & NuGet dependency declarations
├── Properties/
│   └── launchSettings.json            Dev server configuration (Ports: 7080 HTTP / 7081 HTTPS)
├── Data/
│   ├── VisionCheckDbContext.cs        EF Core DbContext, DB set declarations & initial seed data
│   └── Entities/
│       └── Entities.cs                Database ORM model classes (User, Product, Inspection, Defect)
├── Models/
│   └── DtoModels.cs                   Data Transfer Objects matching Blazor frontend JSON contracts
├── Services/
│   ├── InferenceService.cs            Bridge service executing PyTorch ML inference script
│   └── AuthService.cs                JWT token generator service
└── Controllers/
    ├── AuthController.cs              Handles POST /api/auth/login endpoint
    ├── ProductsController.cs          Handles GET /api/products endpoint
    ├── InspectionsController.cs       Handles upload, review/override, and inspection search queries
    └── DashboardController.cs         Handles GET /api/dashboard/summary live analytics
```

---

### Detailed File Explanations

#### 1. `Program.cs`
- **Purpose**: The startup entry point for the backend application.
- **What the code does**:
  - Configures Dependency Injection (DI) for services (`IAuthService`, `IInferenceService`, `VisionCheckDbContext`).
  - Initializes SQLite Database connection string (`visioncheck.db`).
  - Sets up CORS policy (`AllowBlazorClient`) allowing cross-origin requests from the Blazor WebAssembly client (`http://localhost:5285` / `https://localhost:7285`).
  - Configures JWT Bearer Authentication parameters (Secret Key, Issuer, Audience, Token Validation).
  - Configures Swagger/OpenAPI interactive API documentation.
  - Automatically executes `db.Database.EnsureCreated()` at startup to initialize SQLite tables and seed data.
  - Configures static file middleware (`UseStaticFiles()`) to serve uploaded inspection images over `/uploads/`.

#### 2. `Data/Entities/Entities.cs`
- **Purpose**: Defines the Entity Framework Core database schema objects.
- **What the code does**:
  - `UserEntity`: Represents user accounts (`Id`, `Username`, `PasswordHash`, `DisplayName`, `Role`).
  - `ProductEntity`: Represents inspectable product SKUs (`Id`, `Code`, `Name`, `Category`, `IsActive`).
  - `InspectionEntity`: Stores inspection audit records (`Id`, `ProductId`, `ImageUrl`, `Result`, `Confidence`, `Severity`, `InspectedAtUtc`, `ReviewStatus`, `ReviewedBy`, `ReviewedAtUtc`, `ReviewNotes`).
  - `DefectEntity`: Stores individual defect details detected by AI (`Id`, `InspectionId`, `Category`, `Severity`, `Confidence`, `BboxX`, `BboxY`, `BboxWidth`, `BboxHeight`).

#### 3. `Data/VisionCheckDbContext.cs`
- **Purpose**: EF Core Database Context class.
- **What the code does**:
  - Inherits from `DbContext` and exposes `DbSet<T>` properties for Users, Products, Inspections, and Defects.
  - Overrides `OnModelCreating` to pre-seed default database records:
    - Users: `admin` (Administrator), `supervisor` (Supervisor), `operator` (Inspector).
    - Products: `NUT-M8` (M8 Hex Steel Nut), `NUT-M10` (M10 Flange Nut), `NUT-M12` (M12 Nylon Lock Nut).

#### 4. `Services/InferenceService.cs`
- **Purpose**: Bridges the C# ASP.NET Core application with the PyTorch Python model.
- **What the code does**:
  - Defines `IInferenceService` and `PyTorchInferenceService`.
  - Spawns a background OS process (`python predict.py <image_path> --json`).
  - Redirects `stdout` and deserializes the JSON response into an `InferenceResult` object containing predicted defect class (`Deformation`, `Fracture`, `Rusting`, `Scratches`, `Excellent`), confidence score, and class probabilities.
  - Includes robust exception handling and fallback logic if Python or model weights are unavailable.

#### 5. `Services/AuthService.cs`
- **Purpose**: Handles security and JWT token generation.
- **What the code does**:
  - Implements `IAuthService.GenerateJwtToken(UserEntity user)`.
  - Encodes claims (`NameIdentifier`, `UniqueName`, `Role`, `Name`) into a HMAC-SHA256 signed JWT token valid for 24 hours.

#### 6. `Controllers/AuthController.cs`
- **Purpose**: REST Controller for user authentication.
- **What the code does**:
  - Endpoints: `POST /api/auth/login`.
  - Validates login credentials against the database (or auto-provisions demo users matching role keywords).
  - Returns `LoginResponse` payload containing JWT `Token`, expiry time, and user identity.

#### 7. `Controllers/ProductsController.cs`
- **Purpose**: REST Controller for product catalog management.
- **What the code does**:
  - Endpoints: `GET /api/products`.
  - Queries active products from the SQLite database and returns a list of `ProductDto` objects used by the frontend product selector dropdown.

#### 8. `Controllers/InspectionsController.cs`
- **Purpose**: Core REST Controller handling inspection operations.
- **What the code does**:
  - Endpoints:
    - `POST /api/inspections/upload`: Receives `multipart/form-data` with image file and product ID. Saves image to `wwwroot/uploads/`, triggers `IInferenceService`, creates database entity with bounding box details, and returns `InspectionDto`.
    - `POST /api/inspections/{id}/review`: Accepts human-in-the-loop review requests (`isConfirmed`, `correctedResult`, `notes`), updating inspection status to `Confirmed` or `Overridden`.
    - `GET /api/inspections`: Accepts search query filters (`productId`, `fromUtc`, `toUtc`, `defectCategory`, `severity`, `result`, `page`, `pageSize`) and returns paged results (`PagedResult<InspectionDto>`).

#### 9. `Controllers/DashboardController.cs`
- **Purpose**: REST Controller for live analytics and dashboard metrics.
- **What the code does**:
  - Endpoints: `GET /api/dashboard/summary`.
  - Aggregates inspection stats: Total Inspections, Pass Count, Defective Count, Defect Rate %, Category distribution breakdown, Severity breakdown, and 7-day daily trend timeline.

#### 10. `Models/DtoModels.cs`
- **Purpose**: Strongly typed Data Transfer Objects (DTOs) for JSON serialization/deserialization.
- **What the code does**:
  - Defines records matching the exact JSON structure expected by Blazor WebAssembly frontend (`LoginRequest`, `LoginResponse`, `ProductDto`, `InspectionDto`, `DefectDto`, `BoundingBoxDto`, `ReviewDto`, `DashboardSummaryDto`, etc.).

---

## REST Endpoint Reference Summary

| Method | Path | Description | Access |
| ------ | ---- | ----------- | ------ |
| `POST` | `/api/auth/login` | Authenticate user & issue JWT token | Public |
| `GET`  | `/api/products` | Retrieve active inspectable part catalog | Public / Auth |
| `POST` | `/api/inspections/upload` | Upload image for AI inference & save inspection | Inspector / All |
| `POST` | `/api/inspections/{id}/review` | Submit human confirmation or verdict override | Supervisor / Admin |
| `GET`  | `/api/inspections` | Query paged inspection history with filters | Auth |
| `GET`  | `/api/dashboard/summary` | Fetch live KPIs, defect rates, category & daily trends | Auth |

---

## Running the Server Locally

1. Navigate to the server folder:
   ```bash
   cd backend/VisionCheckAI.Server
   ```
2. Build and run:
   ```bash
   dotnet run
   ```
3. Access API & Swagger UI:
   - Base URL: `http://localhost:7080`
   - Swagger UI: `http://localhost:7080/swagger`
