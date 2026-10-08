import os
from dataclasses import dataclass, fields
from pathlib import Path

SERVER_DIR = Path(__file__).resolve().parents[1]


@dataclass(frozen=True)
class Settings:
    data_dir: Path
    default_epochs: int
    max_epochs: int
    batch_size: int
    train_samples: int
    max_sessions: int
    worker_threads: int
    max_message_bytes: int

    @classmethod
    def from_env(cls) -> "Settings":
        # Called at startup only; requests and child processes reuse this snapshot.
        values = dict(os.environ)
        settings = {}
        for setting in fields(cls):
            name = setting.name.upper()
            if name not in values:
                raise ValueError(f"Required environment variable is missing: {name}")
            raw = values[name]
            if raw is None or not raw.strip():
                raise ValueError(f"{name} must have a value")
            if setting.name == "data_dir":
                path = Path(raw)
                settings[setting.name] = (SERVER_DIR / path).resolve()
            else:
                try:
                    settings[setting.name] = int(raw)
                except ValueError:
                    raise ValueError(f"{name} must be an integer") from None
        return cls(**settings)

    def __post_init__(self) -> None:
        for name in (
            "default_epochs",
            "max_epochs",
            "batch_size",
            "train_samples",
            "max_sessions",
            "worker_threads",
            "max_message_bytes",
        ):
            value = getattr(self, name)
            if type(value) is not int or value < 1:
                raise ValueError(f"{name} must be a positive integer")
        if self.default_epochs > self.max_epochs:
            raise ValueError("default_epochs must not exceed max_epochs")
        if not 2 <= self.train_samples <= 60000:
            raise ValueError("train_samples must be between 2 and 60000 for training/validation")
