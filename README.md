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

---

## Getting Started

### 1. Clone and restore

```bash
git clone https://github.com/muhamedhamedvl/BookingSystem.git
cd BookingSystem
dotnet restore
```

### 2. Apply migrations

From the solution directory:

```bash
dotnet ef database update --project BookingSystem.Infrastructure --startup-project BookingSystem.API
```

### 3. Run the API

```bash
dotnet run --project BookingSystem.API
```

---



## 👨‍💻 Author
Muhamed Hamed | Backend Developer

## License
MIT Licensee

