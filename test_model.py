import json
import torch
from torchvision import models
from PIL import Image
from torchvision import transforms


# ==========================================
# Configuration
# ==========================================

MODEL_PATH = "best_efficientnet_b0.pth"
CONFIG_PATH = "model_config.json"


# ==========================================
# Device
# ==========================================

if torch.backends.mps.is_available():
    device = torch.device("mps")
elif torch.cuda.is_available():
    device = torch.device("cuda")
else:
    device = torch.device("cpu")

print("Using device:", device)


# ==========================================
# Load configuration
# ==========================================

with open(CONFIG_PATH, "r") as f:
    config = json.load(f)

class_names = config["class_names"]
num_classes = config["num_classes"]
mean = config["mean"]
std = config["std"]
input_size = config["input_size"]

print("Classes:", class_names)
print("Number of classes:", num_classes)


# ==========================================
# Create EfficientNet-B0
# ==========================================

model = models.efficientnet_b0(weights=None)

num_features = model.classifier[1].in_features

model.classifier[1] = torch.nn.Linear(
    num_features,
    num_classes
)


# ==========================================
# Load trained weights
# ==========================================

state_dict = torch.load(
    MODEL_PATH,
    map_location=device
)

model.load_state_dict(state_dict)

model = model.to(device)
model.eval()

print("Model loaded successfully!")


# ==========================================
# Image preprocessing
# ==========================================

transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(input_size),
    transforms.ToTensor(),
    transforms.Normalize(
        mean=mean,
        std=std
    )
])


# ==========================================
# Prediction
# ==========================================

def predict_image(image_path):

    image = Image.open(image_path).convert("RGB")

    image_tensor = transform(image)
    image_tensor = image_tensor.unsqueeze(0)
    image_tensor = image_tensor.to(device)

    with torch.no_grad():

        outputs = model(image_tensor)

        probabilities = torch.softmax(
            outputs,
            dim=1
        )

    confidence, predicted_index = torch.max(
        probabilities,
        dim=1
    )

    predicted_class = class_names[
        predicted_index.item()
    ]

    confidence = confidence.item()

    print("\n================================")
    print("NUT SURFACE CLASSIFICATION")
    print("================================")

    print(f"\nPrediction: {predicted_class}")
    print(f"Confidence: {confidence * 100:.2f}%")

    print("\nClass probabilities:")

    sorted_probs = sorted(
        zip(class_names, probabilities[0].tolist()),
        key=lambda x: x[1],
        reverse=True
    )

    for class_name, probability in sorted_probs:

        print(
            f"{class_name:15s} "
            f"{probability * 100:6.2f}%"
        )

    return predicted_class, confidence


# ==========================================
# Test image
# ==========================================

if __name__ == "__main__":

    image_path = "test_images/image.jpg"

    predict_image(image_path)