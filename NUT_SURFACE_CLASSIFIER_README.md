# Nut Surface Classifier

An image classifier for identifying visible nut-surface conditions. The project uses a fine-tuned **EfficientNet-B0** model implemented with PyTorch and supports both a Streamlit web interface and command-line inference.

## Classes

The model predicts one of five classes:

- Deformation
- Excellent
- Fracture
- Rusting
- Scratches

## Requirements

- Python 3.9 or newer
- PyTorch
- Torchvision
- Pillow
- Streamlit
- Pandas
- Matplotlib

The model automatically uses Apple Metal (`mps`) when available, then CUDA, and otherwise falls back to the CPU.

## Setup

Create and activate a virtual environment from the project directory:

```bash
python3 -m venv .venv
source .venv/bin/activate
```

Install the dependencies:

```bash
python -m pip install --upgrade pip
python -m pip install torch torchvision pillow streamlit pandas matplotlib
```

The trained checkpoint and configuration are stored in `ai_engine/`:

- `ai_engine/best_efficientnet_b0.pth`
- `ai_engine/model_config.json`

## Web App

Start the Streamlit interface:

```bash
streamlit run app.py
```

Open the local URL printed by Streamlit, upload a `.jpg`, `.jpeg`, or `.png` image, and review the predicted class, confidence, and class probabilities.

## Command-Line Prediction

Run inference for one image:

```bash
python ai_engine/predict.py test_images/image.jpg
```

The command prints the selected class, confidence, and probabilities for all classes. Any image path can be supplied in place of the example path.

## Performance

The training notebook reports the following validation results on **690 images**:

| Metric | Score |
| --- | ---: |
| Accuracy | 98.41% |
| Precision (weighted) | 98.53% |
| Recall (weighted) | 98.41% |
| F1 score (weighted) | 98.43% |
| F1 score (macro) | 97.98% |

Per-class validation F1 scores:

| Class | Precision | Recall | F1 score | Samples |
| --- | ---: | ---: | ---: | ---: |
| Deformation | 99.45% | 98.10% | 98.77% | 368 |
| Excellent | 88.73% | 100.00% | 94.03% | 63 |
| Fracture | 100.00% | 98.28% | 99.13% | 58 |
| Rusting | 99.06% | 99.06% | 99.06% | 106 |
| Scratches | 100.00% | 97.89% | 98.94% | 95 |

These are validation results from the training notebook, not an independently held-out test benchmark. The repository's `test_model.py` performs a single-image smoke test and does not calculate test-set metrics.

## Testing the Model

The basic model smoke test uses `test_images/image.jpg`:

```bash
python test_model.py
```

Additional sample images are available in `test_images/`, including examples for deformation, excellent condition, fracture, rusting, and scratches.

## Image Preprocessing

Input images are converted to RGB, resized to 256 pixels, center-cropped to `224 x 224`, converted to tensors, and normalized using the ImageNet mean and standard deviation. These values are stored in `ai_engine/model_config.json` and are shared by the web app and CLI predictor.

## Training Notebook

`nutsurface-classifier-training-history.ipynb` documents the EfficientNet-B0 training and evaluation workflow, including dataset preparation, training history, and evaluation artifacts. It references the optional `nutSurfaceDataset.zip` dataset archive.

## Project Structure

```text
ai_engine/app.py                    Optional Streamlit web application
ai_engine/predict.py                Command-line inference script
ai_engine/test_model.py             Model loading and smoke test
ai_engine/model_config.json         Class labels and preprocessing settings
ai_engine/best_efficientnet_b0.pth  Trained model weights
test_images/                        Sample input images
nutsurface-classifier-training-history.ipynb
                                    Training and evaluation notebook
```

Large model and dataset artifacts are excluded from version control by `.gitignore`; make sure they are present locally before running inference.

## Limitations

This model is intended for the nut-surface image categories represented in its training data. Prediction confidence should not be treated as a guarantee of correctness, especially for images with different lighting, backgrounds, viewpoints, or surface conditions.