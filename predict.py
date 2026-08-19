import sys
import json
from pathlib import Path

import torch
from torchvision import models, transforms
from PIL import Image


# ============================================================
# Paths
# ============================================================

BASE_DIR = Path(__file__).resolve().parent

MODEL_PATH = BASE_DIR / "best_efficientnet_b0.pth"
CONFIG_PATH = BASE_DIR / "model_config.json"


# ============================================================
# Device
# ============================================================

if torch.backends.mps.is_available():
    DEVICE = torch.device("mps")
elif torch.cuda.is_available():
    DEVICE = torch.device("cuda")
else:
    DEVICE = torch.device("cpu")

print(f"Using device: {DEVICE}")


# ============================================================
# Load configuration
# ============================================================

with open(CONFIG_PATH, "r") as f:
    config = json.load(f)

CLASS_NAMES = config["class_names"]
NUM_CLASSES = config["num_classes"]
MEAN = config["mean"]
STD = config["std"]
INPUT_SIZE = config["input_size"]

print(f"Classes: {CLASS_NAMES}")


# ============================================================
# Build model
# ============================================================

model = models.efficientnet_b0(weights=None)

num_features = model.classifier[1].in_features

model.classifier[1] = torch.nn.Linear(
    num_features,
    NUM_CLASSES
)


# ============================================================
# Load trained weights
# ============================================================

state_dict = torch.load(
    MODEL_PATH,
    map_location=DEVICE
)

model.load_state_dict(state_dict)

model = model.to(DEVICE)
model.eval()

print("Model loaded successfully!")


# ============================================================
# Image preprocessing
# ============================================================

transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(INPUT_SIZE),
    transforms.ToTensor(),
    transforms.Normalize(
        mean=MEAN,
        std=STD
    )
])


# ============================================================
# Prediction function
# ============================================================

def predict_image(image_path):

    image_path = Path(image_path)

    if not image_path.exists():
        raise FileNotFoundError(
            f"Image not found: {image_path}"
        )

    image = Image.open(image_path).convert("RGB")

    input_tensor = transform(image)
    input_tensor = input_tensor.unsqueeze(0)
    input_tensor = input_tensor.to(DEVICE)

    with torch.no_grad():

        outputs = model(input_tensor)

        probabilities = torch.softmax(
            outputs,
            dim=1
        )[0]

    confidence, predicted_index = torch.max(
        probabilities,
        dim=0
    )

    predicted_class = CLASS_NAMES[
        predicted_index.item()
    ]

    confidence = confidence.item()

    # Sort probabilities from highest to lowest
    results = sorted(
        zip(CLASS_NAMES, probabilities.tolist()),
        key=lambda x: x[1],
        reverse=True
    )

    print("\n========================================")
    print("       NUT SURFACE CLASSIFICATION")
    print("========================================")

    print(f"\nImage: {image_path}")
    print(f"Prediction: {predicted_class}")
    print(f"Confidence: {confidence * 100:.2f}%")

    print("\nClass probabilities:")
    print("----------------------------------------")

    for class_name, probability in results:

        print(
            f"{class_name:15s} "
            f"{probability * 100:6.2f}%"
        )

    print("----------------------------------------")

    return {
        "prediction": predicted_class,
        "confidence": confidence,
        "probabilities": dict(results)
    }


# ============================================================
# Command-line usage
# ============================================================

if __name__ == "__main__":

    if len(sys.argv) != 2:

        print("\nUsage:")
        print("python predict.py <image_path>")
        print("\nExample:")
        print("python predict.py test_images/image.jpg")

        sys.exit(1)

    image_path = sys.argv[1]

    predict_image(image_path)