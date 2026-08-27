# VisionCheck-AI

VisionCheck-AI is a full-stack quality-control application for classifying visible nut-surface conditions. It combines a Blazor WebAssembly dashboard, an ASP.NET Core REST API, SQLite persistence, and a PyTorch EfficientNet-B0 classifier.

## What It Does

An operator uploads an image of a nut through the web application. The backend stores the image, invokes the Python inference script, saves the inspection result, and returns the prediction and confidence to the dashboard. Supervisors can review or override results, while the dashboard exposes inspection history and quality metrics.

The model predicts five classes:

- `Deformation`
- `Excellent`
- `Fracture`
- `Rusting`
- `Scratches`

## Architecture

```text
Blazor WebAssembly frontend (.NET 8)
        |
        | REST / JSON / JWT
        v
ASP.NET Core Web API backend (.NET 9)
        |
        +--> SQLite database and uploaded images
        |
        +--> ai_engine/predict.py --json
                    |
                    +--> EfficientNet-B0 + model weights
```

## Repository Layout

```text
VisionCheck-AI/
├── README.md
├── ai_engine/
│   ├── predict.py                    CLI and JSON model inference
│   ├── app.py                        Local AI application entry point
│   ├── test_model.py                 Model smoke test
│   ├── model_config.json             Labels and preprocessing values
│   └── best_efficientnet_b0.pth      Trained model weights
├── requirements.txt                  Python runtime dependencies
├── test_images/                      Sample inference images
├── nutsurface-classifier-training-history.ipynb
│                                     Training and evaluation notebook
├── backend/VisionCheckAI.Server/
│   ├── Controllers/                  REST API endpoints
│   ├── Data/                         EF Core entities and database context
│   ├── Models/                       API DTOs
│   ├── Services/                     Authentication and AI bridge services
│   └── README.md                     Backend walkthrough
├── frontend/VisionCheckAI.Client/
│   ├── Pages/                        Login, dashboard, upload, and history
│   ├── Services/                     Typed API clients and application state
│   ├── Shared/                       Layouts, charts, and reusable components
│   └── wwwroot/                      Static assets and API configuration
├── docs/                             Technical and research documentation
│   ├── documentation-VisionCheckAI.pdf
│   ├── high_level_documentation.pdf
│   ├── low_level_documentation.pdf
│   └── paper-VisionCheckAI.pdf
└── NUT_SURFACE_CLASSIFIER_README.md  AI training and inference notes
```

## Requirements

- .NET 9 SDK for the backend
- .NET 8 SDK for the Blazor frontend
- Python 3.9 or newer
- PyTorch, Torchvision, and Pillow

The Python dependencies are listed in [requirements.txt](requirements.txt). The `.pth` model file is committed so a fresh clone has the weights required for inference. Future checkpoints remain ignored by `.gitignore` unless deliberately selected for release.

## Run Locally

### 1. Install Python dependencies

From the repository root:

```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

### 2. Test model inference directly

```bash
python ai_engine/predict.py test_images/image.jpg
python ai_engine/predict.py test_images/image.jpg --json
```

The JSON form is the interface used by the ASP.NET Core inference bridge.

### 3. Start the backend

In a terminal where the Python environment is available:

```bash
cd backend/VisionCheckAI.Server
dotnet run --launch-profile http
```

The API runs at `http://localhost:7080`. Swagger is available at `http://localhost:7080/swagger`.

The backend creates `visioncheck.db` and the upload directory automatically. These generated files are ignored by Git.

### 4. Start the frontend

In a second terminal:

```bash
cd frontend/VisionCheckAI.Client
dotnet run
```

Open `http://localhost:5285`. The development frontend uses HTTP to match the backend at `http://localhost:7080/`, so uploaded images served from the backend are not blocked by browser mixed-content rules.

For local demonstration accounts, use one of these usernames:

- `admin`
- `supervisor`
- `operator`

## API Endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Authenticate and issue a JWT |
| `GET` | `/api/products` | List inspectable products |
| `POST` | `/api/inspections/upload` | Upload an image and run inference |
| `POST` | `/api/inspections/{id}/review` | Confirm or override a result |
| `GET` | `/api/inspections` | Search inspection history |
| `GET` | `/api/dashboard/summary` | Return dashboard metrics |

The upload request uses `multipart/form-data` with these fields:

```text
file       image file (.jpg, .jpeg, or .png)
productId  selected product identifier
```

## AI Engine

The AI engine is based on the training workflow in `nutsurface-classifier-training-history.ipynb`. It uses a pretrained EfficientNet-B0 model and a two-stage transfer-learning strategy:

1. Stage 1 freezes the feature extractor and trains only the classifier head.
2. Stage 2 unfreezes the full network and fine-tunes it with a smaller learning rate.

Training uses five ImageFolder classes: `Deformation`, `Excellent`, `Fracture`, `Rusting`, and `Scratches`. The training pipeline applies random resized crops, horizontal and vertical flips, rotation, and color jitter. Validation and production inference convert images to RGB, resize them to 256 pixels, center-crop to `224 x 224`, convert them to tensors, and normalize them with ImageNet mean and standard deviation.

The inference implementation is in `ai_engine/predict.py`. Its `--json` output is the contract used by the ASP.NET Core backend. `ai_engine/model_config.json` stores the class order and preprocessing values, while `ai_engine/best_efficientnet_b0.pth` stores the trained model weights.

The training notebook reports these validation results:

| Metric | Score |
| --- | ---: |
| Accuracy | 98.41% |
| Weighted F1 | 98.43% |
| Macro F1 | 97.98% |

These are validation results, not a guarantee of performance on new production images. Confidence should be reviewed alongside image quality and human inspection.

## Documentation

- [Backend README](backend/VisionCheckAI.Server/README.md)
- [AI and training README](NUT_SURFACE_CLASSIFIER_README.md)
- `docs/documentation-VisionCheckAI.pdf`
- `docs/high_level_documentation.pdf`
- `docs/low_level_documentation.pdf`
- `docs/paper-VisionCheckAI.pdf`

## Development Notes

- Do not commit `.venv`, build output, SQLite databases, uploaded images, datasets, or secrets.
- The backend prefers the repository virtual environment at `.venv/bin/python` (or `.venv/Scripts/python.exe` on Windows), then checks `python3` and `python` on PATH.
- The backend must be able to find `ai_engine/predict.py` and `ai_engine/best_efficientnet_b0.pth` when launched.
- If model inference is unavailable, the upload endpoint returns an error instead of generating a fallback prediction.
- Configure the JWT secret through application configuration or environment variables before deployment.
- Configure CORS with the deployed frontend origin instead of allowing every origin.
