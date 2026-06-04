from app.pipelines.train_recommendation_model import train_model


if __name__ == "__main__":
    path = train_model()
    print(f"Saved model to {path}")
