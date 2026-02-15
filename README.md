# AppointMed — Medical Appointment Booking API

**Muhamed Hamed | Backend Developer**

A RESTful API for booking and managing medical appointments. Patients search doctors and clinics, book appointments, and leave reviews; doctors manage profiles and availability; admins oversee the system.

---

## Features

| Area | Capabilities |
|------|----------------|
| **Auth** | Signup, login (email + password), Google OAuth, email verification, password reset, change password, logout |
| **Users** | Get/update own profile, list users (Admin), CRUD users (Admin/Worker) |
| **Doctors** | Search (filters, pagination), get by ID, onboard as doctor, update profile, manage availability, approve doctors (Admin), list pending (Admin) |
| **Clinics** | List, get by ID, create/update/delete (Admin) |
| **Appointments** | Book, get by ID, get my appointments, update status, cancel, reschedule, list all (Admin) |
| **Patients** | Create/get/update own profile, get patient by ID (Admin/Doctor) |
| **Reviews** | Submit review (patient, completed appointments), get by doctor, get by appointment |
| **Admin** | System statistics |

**Roles:** `Admin`, `Doctor`, `User` (patient). JWT-based authentication; Swagger UI available in all environments.

---

## Tech Stack

- **.NET 9** — ASP.NET Core Web API  
- **Entity Framework Core 9** — SQL Server  
- **ASP.NET Core Identity** — Users, roles  
- **JWT Bearer** — Authentication  
- **MediatR** — CQRS-style handlers  
- **FluentValidation** — Request validation  
- **AutoMapper** — Object mapping  
- **Serilog** — Logging  
- **Swashbuckle (Swagger)** — API docs with XML comments  
- **MailKit** — Email (verification, password reset)

---

## Architecture

Clean Architecture with four layers:

```
BookingSystem.sln
├── BookingSystem.API          → Controllers, middleware, Swagger
├── BookingSystem.Application → DTOs, interfaces, services, MediatR, validation
├── BookingSystem.Domain       → Entities, enums, domain logic
└── BookingSystem.Infrastructure → EF Core, Identity, repositories, email, JWT
```

- **API** depends on Application and Infrastructure (DI).  
- **Application** depends only on Domain.  
- **Infrastructure** implements Application interfaces and uses Domain entities.

---

## Project Structure

```
BookingSystem/
├── BookingSystem.API/
│   ├── Controllers/     # Auth, Users, Admin, Appointments, Clinics, Doctors, Patients, Reviews
│   ├── Program.cs
│   └── appsettings.json
├── BookingSystem.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Mapping/
│   └── (MediatR, validation)
├── BookingSystem.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Base/
└── BookingSystem.Infrastructure/
    ├── Data/            # DbContext, configurations
    ├── Identity/
    ├── Repositories/
    └── Services/
```

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
- SQL Server (local or remote)  
- SMTP (e.g. Gmail) for verification and password reset

---

## Getting Started

### 1. Clone and restore

```bash
git clone https://github.com/your-username/BookingSystem.git
cd BookingSystem
dotnet restore
```

### 2. Configure secrets (local development)

Secrets are not in the repo. Use **User Secrets** for local development:

```bash
cd BookingSystem.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AppointMed;TrustServerCertificate=True;..."
dotnet user-secrets set "JwtSettings:Secret" "your-long-random-jwt-secret"
dotnet user-secrets set "EmailSettings:SenderEmail" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

Use your own connection string, JWT secret, and SMTP credentials.

### 3. Apply migrations

From the solution directory:

```bash
dotnet ef database update --project BookingSystem.Infrastructure --startup-project BookingSystem.API
```

### 4. Run the API

```bash
dotnet run --project BookingSystem.API
```

- API: **https://localhost:7xxx** or **http://localhost:5xxx** (see console).  
- Swagger UI: **https://localhost:7xxx/swagger** (or same port as API + `/swagger`).

---

## Configuration

| Source | When | Purpose |
|--------|------|---------|
| `appsettings.json` | Always | Non-sensitive defaults (logging, JWT issuer/audience, rate limits). Secrets left empty. |
| User Secrets | Development | Connection string, JWT secret, email credentials (overrides appsettings). |
| Environment variables | Production | Same keys (e.g. `ConnectionStrings__DefaultConnection`, `JwtSettings__Secret`, `EmailSettings__*`). |

Production placeholders in `appsettings.Production.json` use `${VAR}` style; set the corresponding environment variables on the host (e.g. `DB_SERVER`, `DB_NAME`, `JWT_SECRET`, `SMTP_*`).

---

## API Documentation

- **Base URL:** `https://your-host/api/v1`  
- **Swagger UI:** `https://your-host/swagger`  

All endpoints are documented with summaries and descriptions. Use **Authorize** in Swagger with a Bearer token (from `/api/v1/auth/login` or signup) to call protected endpoints.

---

## Production Deployment

1. Set environment variables (or host configuration) for:
   - `ConnectionStrings:DefaultConnection` (or `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` if using placeholders)
   - `JwtSettings:Secret`
   - `EmailSettings:SenderEmail`, `Username`, `Password` (and optionally `SmtpServer`)
2. Run EF Core migrations against the production database.
3. Publish: `dotnet publish BookingSystem.API -c Release -o ./publish`
4. Deploy the contents of `./publish` to your host (e.g. MonsterASP, Azure, IIS).

---

## License

This project is for educational/portfolio use. Adjust license as needed for your case.
