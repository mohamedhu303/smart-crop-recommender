# train_model.py
# Trains a crop recommendation model and exports it to ONNX format
# Dataset: synthetic agricultural data with Temperature, Humidity, Soil_pH, Rainfall

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from sklearn.metrics import accuracy_score
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
import json
import os

# ─── Reproducibility ────────────────────────────────────────────────────────
np.random.seed(42)
NUM_SAMPLES = 3000

# ─── Crop definitions with realistic environmental ranges ───────────────────
# Each crop has: [temp_mean, temp_std, humidity_mean, humidity_std,
#                 ph_mean, ph_std, rainfall_mean, rainfall_std]
CROP_PROFILES = {
    "Wheat":      [22, 4,  45, 10, 6.5, 0.5, 300,  60],
    "Rice":       [28, 3,  82, 8,  6.0, 0.4, 950, 100],
    "Maize":      [26, 4,  65, 10, 6.2, 0.5, 550,  80],
    "Soybean":    [24, 3,  68, 9,  6.8, 0.4, 500,  70],
    "Cotton":     [30, 4,  55, 10, 7.0, 0.5, 400,  80],
    "Sugarcane":  [32, 3,  75, 8,  6.5, 0.4, 850,  90],
    "Coffee":     [25, 2,  78, 7,  6.3, 0.3, 700,  80],
    "Potato":     [18, 3,  70, 8,  5.8, 0.4, 450,  70],
    "Tomato":     [27, 3,  72, 8,  6.4, 0.4, 400,  60],
    "Barley":     [20, 4,  50, 10, 7.2, 0.5, 280,  55],
}

# ─── Generate synthetic dataset ─────────────────────────────────────────────
records = []
samples_per_crop = NUM_SAMPLES // len(CROP_PROFILES)

for crop, (t_m, t_s, h_m, h_s, p_m, p_s, r_m, r_s) in CROP_PROFILES.items():
    for _ in range(samples_per_crop):
        temp      = np.clip(np.random.normal(t_m, t_s), 5,  45)
        humidity  = np.clip(np.random.normal(h_m, h_s), 20, 100)
        soil_ph   = np.clip(np.random.normal(p_m, p_s), 4,  9)
        rainfall  = np.clip(np.random.normal(r_m, r_s), 50, 1200)
        records.append([temp, humidity, soil_ph, rainfall, crop])

df = pd.DataFrame(records, columns=["Temperature", "Humidity", "Soil_pH", "Rainfall", "Crop"])
df = df.sample(frac=1, random_state=42).reset_index(drop=True)   # shuffle

print(f"Dataset shape: {df.shape}")
print(f"\nCrop distribution:\n{df['Crop'].value_counts()}")
print(f"\nFeature statistics:\n{df.describe().round(2)}")

# ─── Encode target labels ────────────────────────────────────────────────────
label_encoder = LabelEncoder()
df["Crop_Encoded"] = label_encoder.fit_transform(df["Crop"])

# Save class names so the backend can decode predictions
class_names = label_encoder.classes_.tolist()
print(f"\nClass mapping: {dict(enumerate(class_names))}")

# ─── Train / test split ──────────────────────────────────────────────────────
X = df[["Temperature", "Humidity", "Soil_pH", "Rainfall"]].values.astype(np.float32)
y = df["Crop_Encoded"].values

X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)

# ─── Train Random Forest ─────────────────────────────────────────────────────
print("\nTraining Random Forest...")
clf = RandomForestClassifier(
    n_estimators=150,
    max_depth=12,
    min_samples_split=4,
    min_samples_leaf=2,
    random_state=42,
    n_jobs=-1,
)
clf.fit(X_train, y_train)

# ─── Evaluate ────────────────────────────────────────────────────────────────
y_pred = clf.predict(X_test)
accuracy = accuracy_score(y_test, y_pred)
print(f"Test accuracy: {accuracy:.4f} ({accuracy * 100:.2f}%)")

# Feature importance
feature_names = ["Temperature", "Humidity", "Soil_pH", "Rainfall"]
importances = dict(zip(feature_names, clf.feature_importances_.round(4)))
print(f"Feature importances: {importances}")

# ─── Export to ONNX ──────────────────────────────────────────────────────────
print("\nExporting model to ONNX...")

initial_type = [("float_input", FloatTensorType([None, 4]))]

onnx_model = convert_sklearn(
    clf,
    initial_types=initial_type,
    options={id(clf): {"zipmap": False}},   # return raw class indices + probabilities
    target_opset=12,
)

output_path = "crop_model.onnx"
with open(output_path, "wb") as f:
    f.write(onnx_model.SerializeToString())

print(f"Model saved to: {os.path.abspath(output_path)}")
print(f"ONNX file size: {os.path.getsize(output_path) / 1024:.1f} KB")

# ─── Save class names for the backend ────────────────────────────────────────
metadata_path = "crop_classes.json"
with open(metadata_path, "w") as f:
    json.dump(class_names, f, indent=2)

print(f"Class names saved to: {os.path.abspath(metadata_path)}")
print(f"\nClass names: {class_names}")
print("\nDone! Copy crop_model.onnx to AgriAnalytics.API/ folder.")