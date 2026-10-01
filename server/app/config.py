import os
from dataclasses import dataclass
from pathlib import Path


def positive_int(name: str, default: int) -> int:
    value = int(os.environ.get(name, default))
    if value < 1:
        raise ValueError(f"{name} must be positive")
    return value


@dataclass(frozen=True)
class Settings:
    data_dir: Path
    default_epochs: int = 5
    max_epochs: int = 100
    batch_size: int = 64
    train_samples: int = 60000
    max_sessions: int = 4
    worker_threads: int = 1
    max_message_bytes: int = 1048576

    @classmethod
    def from_env(cls) -> "Settings":
        settings = cls(
            data_dir=Path(os.environ.get("DATA_DIR", ".data")).resolve(),
            default_epochs=positive_int("DEFAULT_EPOCHS", 5),
            max_epochs=positive_int("MAX_EPOCHS", 100),
            batch_size=positive_int("BATCH_SIZE", 64),
            train_samples=positive_int("TRAIN_SAMPLES", 60000),
            max_sessions=positive_int("MAX_SESSIONS", 4),
            worker_threads=positive_int("WORKER_THREADS", 1),
        )
        if settings.default_epochs > settings.max_epochs:
            raise ValueError("DEFAULT_EPOCHS must not exceed MAX_EPOCHS")
        if settings.train_samples > 60000:
            raise ValueError("TRAIN_SAMPLES must not exceed the MNIST training split (60000)")
        return settings
