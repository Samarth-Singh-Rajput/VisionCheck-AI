import sys
import json
import argparse
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


# ============================================================
# Load configuration & Model lazily / globally
# ============================================================

with open(CONFIG_PATH, "r") as f:
    config = json.load(f)

CLASS_NAMES = config["class_names"]
NUM_CLASSES = config["num_classes"]
MEAN = config["mean"]
STD = config["std"]
INPUT_SIZE = config["input_size"]


def get_model():
    model = models.efficientnet_b0(weights=None)
    num_features = model.classifier[1].in_features
    model.classifier[1] = torch.nn.Linear(num_features, NUM_CLASSES)

    state_dict = torch.load(MODEL_PATH, map_location=DEVICE)
    model.load_state_dict(state_dict)
    model = model.to(DEVICE)
    model.eval()
    return model


# Preprocessing pipeline
transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(INPUT_SIZE),
    transforms.ToTensor(),
    transforms.Normalize(mean=MEAN, std=STD)
])


# ============================================================
# Prediction function
# ============================================================

def predict_image(image_path, json_output=False):
    image_path = Path(image_path)

    if not image_path.exists():
        raise FileNotFoundError(f"Image not found: {image_path}")

    model = get_model()

    image = Image.open(image_path).convert("RGB")
    input_tensor = transform(image).unsqueeze(0).to(DEVICE)

    with torch.no_grad():
        outputs = model(input_tensor)
        probabilities = torch.softmax(outputs, dim=1)[0]

    confidence, predicted_index = torch.max(probabilities, dim=0)
    predicted_class = CLASS_NAMES[predicted_index.item()]
    confidence_val = confidence.item()

    # Probability dictionary mapping
    probs_dict = {
        class_name: prob
        for class_name, prob in zip(CLASS_NAMES, probabilities.tolist())
    }

    if json_output:
        result_json = {
            "prediction": predicted_class,
            "confidence": confidence_val,
            "probabilities": probs_dict
        }
        print(json.dumps(result_json))
        return result_json

    # Formatted terminal display
    results = sorted(
        probs_dict.items(),
        key=lambda x: x[1],
        reverse=True
    )

    print("\n========================================")
    print("       NUT SURFACE CLASSIFICATION")
    print("========================================")
    print(f"\nImage: {image_path}")
    print(f"Prediction: {predicted_class}")
    print(f"Confidence: {confidence_val * 100:.2f}%")
    print("\nClass probabilities:")
    print("----------------------------------------")
    for class_name, probability in results:
        print(f"{class_name:15s} {probability * 100:6.2f}%")
    print("----------------------------------------")

    return {
        "prediction": predicted_class,
        "confidence": confidence_val,
        "probabilities": probs_dict
    }


# ============================================================
# Command-line usage
# ============================================================

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Nut Surface Defect Classification CLI")
    parser.add_argument("image_path", type=str, help="Path to input image file")
    parser.add_argument("--json", action="store_true", help="Output result as JSON for API integration")

    args = parser.parse_args()
    predict_image(args.image_path, json_output=args.json)