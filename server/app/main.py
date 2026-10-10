import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, WebSocket

from app.config import Settings
from app.dataset import Dataset, ensure_dataset
from app.session import Session
from app.worker import Worker


def create_app(
    settings: Settings | None = None, dataset: Dataset | None = None, worker_factory=Worker
) -> FastAPI:
    sessions = set()

    @asynccontextmanager
    async def lifespan(app: FastAPI):
        nonlocal settings
        if settings is None:
            settings = Settings.from_env()
        if dataset is None:
            path = await asyncio.to_thread(ensure_dataset, settings.data_dir)
            app.state.dataset = await asyncio.to_thread(Dataset, path)
        else:
            app.state.dataset = dataset
        try:
            yield
        finally:
            await asyncio.gather(*(session.close() for session in list(sessions)))

    app = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None, openapi_url=None)

    @app.websocket("/ws")
    async def websocket(socket: WebSocket):
        if len(sessions) >= settings.max_sessions:
            await socket.accept()
            await socket.close(code=1013)
            return
        session = Session(socket, settings, app.state.dataset, worker_factory)
        sessions.add(session)
        try:
            await socket.accept()
            await session.serve()
        finally:
            await session.close()
            sessions.discard(session)

    return app


app = create_app()
