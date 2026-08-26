import json
from pathlib import Path

import torch
import streamlit as st
import pandas as pd
import matplotlib.pyplot as plt

from torchvision import models, transforms
from PIL import Image


# ============================================================
# Page configuration
# ============================================================

st.set_page_config(
    page_title="Nut Surface Classifier",
    page_icon="🔍",
    layout="wide"
)


# ============================================================
# Paths
# ============================================================

BASE_DIR = Path(__file__).resolve().parent

MODEL_PATH = BASE_DIR / "best_efficientnet_b0.pth"
if not MODEL_PATH.exists():
    ALT_MODEL_PATH = BASE_DIR / "best_efficientnet_b0 (1).pth"
    if ALT_MODEL_PATH.exists():
        MODEL_PATH = ALT_MODEL_PATH
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
# Load configuration
# ============================================================

with open(CONFIG_PATH, "r") as f:
    config = json.load(f)

CLASS_NAMES = config["class_names"]
NUM_CLASSES = config["num_classes"]
MEAN = config["mean"]
STD = config["std"]
INPUT_SIZE = config["input_size"]


# ============================================================
# Load model
# ============================================================

@st.cache_resource
def load_model():

    model = models.efficientnet_b0(weights=None)

    num_features = model.classifier[1].in_features

    model.classifier[1] = torch.nn.Linear(
        num_features,
        NUM_CLASSES
    )

    state_dict = torch.load(
        MODEL_PATH,
        map_location=DEVICE
    )

    model.load_state_dict(state_dict)

    model = model.to(DEVICE)
    model.eval()

    return model


model = load_model()


# ============================================================
# Transform
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
# Prediction
# ============================================================

def predict(image):

    image_rgb = image.convert("RGB")

    input_tensor = transform(image_rgb)

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

    probabilities = probabilities.cpu().numpy()

    return (
        predicted_class,
        confidence.item(),
        probabilities
    )


# ============================================================
# Header
# ============================================================

st.title("🔍 Nut Surface Classifier")

st.markdown(
    """
    Upload an image of a nut surface and the trained
    **EfficientNet-B0** model will classify it into one of
    five surface-condition categories.
    """
)


# ============================================================
# Model information
# ============================================================

with st.expander("Model Information"):

    col1, col2, col3 = st.columns(3)

    with col1:
        st.metric("Model", "EfficientNet-B0")

    with col2:
        st.metric("Classes", NUM_CLASSES)

    with col3:
        st.metric("Device", str(DEVICE))


# ============================================================
# Upload
# ============================================================

uploaded_file = st.file_uploader(
    "Upload a nut surface image",
    type=["jpg", "jpeg", "png"]
)


if uploaded_file is not None:

    image = Image.open(uploaded_file).convert("RGB")

    st.divider()

    # --------------------------------------------------------
    # Image + prediction
    # --------------------------------------------------------

    col1, col2 = st.columns(2)

    with col1:

        st.subheader("Input Image")

        st.image(
            image,
            use_container_width=True
        )

    with col2:

        st.subheader("Prediction")

        predicted_class, confidence, probabilities = predict(
            image
        )

        st.success(
            f"Prediction: {predicted_class}"
        )

        st.metric(
            "Confidence",
            f"{confidence * 100:.2f}%"
        )


    # ========================================================
    # Probability chart
    # ========================================================

    st.divider()

    st.subheader("Class Probabilities")

    probability_data = pd.DataFrame({
        "Class": CLASS_NAMES,
        "Probability": probabilities * 100
    })

    probability_data = probability_data.sort_values(
        "Probability",
        ascending=False
    )

    st.bar_chart(
        probability_data.set_index("Class")
    )


    # ========================================================
    # Probability table
    # ========================================================

    st.subheader("Detailed Probabilities")

    display_data = probability_data.copy()

    display_data["Probability"] = (
        display_data["Probability"]
        .map(lambda x: f"{x:.2f}%")
    )

    st.dataframe(
        display_data,
        use_container_width=True,
        hide_index=True
    )