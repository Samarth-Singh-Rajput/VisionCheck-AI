# VisionCheck-AI — Automated Industrial Surface Defect Detection System

**VisionCheck-AI** is an end-to-end, machine vision-based quality control and automated surface defect detection platform designed for modern manufacturing pipelines. 

By combining a high-accuracy **PyTorch EfficientNet-B0** deep learning classification model with an **ASP.NET Core 8 Web API** and an interactive **Blazor WebAssembly** web application, VisionCheck-AI eliminates manual inspection bottleneck, reduces human error, and provides real-time quality analytics for factory conveyor systems.

---

## Key Features

- **Automated AI Surface Inspection**: Fine-grained classification of industrial parts into 5 surface condition categories:
  - `Deformation` (Defective)
  - `Excellent` (Non-Defective / Pass)
  - `Fracture` (Defective)
  - `Rusting` (Defective)
  - `Scratches` (Defective)
- **High Accuracy Model**: EfficientNet-B0 model trained with a 2-stage transfer learning pipeline, achieving **98.41% validation accuracy** and **0.9798 macro F1-score**.
- **Human-in-the-Loop Review System**: Supervisors and Administrators can inspect AI predictions, confirm results, or apply manual overrides when edge cases occur.
- **Real-Time Analytics Dashboard**: Live metrics monitoring total inspections, pass/defect rates, category breakdowns, severity distributions, and 7-day trend analysis.
- **Historical Audit Log**: Filterable inspection search engine with date range, category, severity, and product SKU filters.
- **Role-Based Access Control (RBAC)**: Role gating for Inspectors, Supervisors, and Administrators powered by JWT Bearer Authentication.

---

## System Architecture

```
                                 +-------------------------------------+
                                 |  Blazor WebAssembly Frontend (.NET) |
                                 |  (frontend/VisionCheckAI.Client)    |
                                 +------------------+------------------+
                                                    |
                                                    | REST APIs (JSON / JWT)
                                                    v
                                 +-------------------------------------+
                                 |  ASP.NET Core 8 Web API Backend     |
                                 |  (backend/VisionCheckAI.Server)     |
                                 +---------+-----------------+---------+
                                           |                 |
                         SQLite DB (EF Core)                 | Process Invocation
                                           v                 v
                                 +------------------+  +---------------+
                                 |  visioncheck.db  |  |  predict.py   |
                                 +------------------+  |  (PyTorch ML) |
                                                       +---------------+
```

---

## Repository Structure

```text
VisionCheck-AI/
├── README.md                            Main project documentation
├── paper(VisionCheckAI).pdf             Research paper detailing EfficientNet-B0 architecture & benchmarks
├── model_config.json                    Model configuration (mean, std, input size, class labels)
├── best_efficientnet_b0.pth             Trained EfficientNet-B0 PyTorch model weights (optional local artifact)
├── predict.py                           Python CLI & JSON inference script
├── test_model.py                        PyTorch model loading smoke test
├── test_images/                         Sample test images (deformation, fracture, rust, scratches, excellent)
│
├── backend/
│   └── VisionCheckAI.Server/            ASP.NET Core 8 Web API backend project
│       ├── Controllers/                 REST API endpoints (Auth, Products, Inspections, Dashboard)
│       ├── Data/                        EF Core DbContext and SQLite Database Entities
│       ├── Models/                      DTOs matching REST API contracts
│       ├── Services/                    PyTorch Inference Service & JWT Authentication Service
│       ├── Program.cs                   Application startup & middleware pipeline
│       └── README.md                    Detailed backend documentation & code walkthrough
│
└── frontend/
    └── VisionCheckAI.Client/            Blazor WebAssembly (.NET 8) frontend project
        ├── Pages/                       UI Views (Login, Dashboard, Inspection Upload, History)
        ├── Services/                    Typed HTTP Client services & state management
        ├── Shared/                      Reusable components & SVG chart engines
        └── wwwroot/                     CSS tokens, static assets, and appsettings configuration
```

---

## Quick Start Guide

### Prerequisites
- [.NET 8 SDK or .NET 9 SDK](https://dotnet.microsoft.com/download)
- [Python 3.9+](https://www.python.org/downloads/) with `torch`, `torchvision`, `pillow` installed.

### 1. Running the Backend API
Navigate to the backend directory and launch the ASP.NET Core server:
```bash
cd backend/VisionCheckAI.Server
dotnet run
```
The server will start on **`http://localhost:7080`**.
> Interactive Swagger API Documentation will be available at **`http://localhost:7080/swagger`**.

### 2. Running the Blazor Frontend Web App
In a separate terminal, launch the Blazor WebAssembly client:
```bash
cd frontend/VisionCheckAI.Client
dotnet run
```
Open your web browser and navigate to **`http://localhost:5285`** (or `https://localhost:7285`).

### 3. Signing In & Uploading Test Images
- **Login Credentials**: Enter any password. Use username `admin` (Administrator), `supervisor` (Supervisor), or `operator` (Inspector).
- **Upload Inspection**: Go to **New Inspection**, select a product SKU, and upload any image from `test_images/` to execute real-time PyTorch inference!

---

## Model Benchmark Summary

| Metric | Benchmark Score |
| --- | ---: |
| Overall Validation Accuracy | **98.41%** |
| Weighted Precision | **98.53%** |
| Weighted Recall | **98.41%** |
| Weighted F1-Score | **98.43%** |
| Macro F1-Score | **0.9798** |

---

## License

Developed as part of an advanced machine vision & industrial quality assurance initiative.
