import asyncio
from contextlib import asynccontextmanager
from io import BytesIO

from fastapi import FastAPI, HTTPException, Query, WebSocket
from fastapi.responses import Response
from PIL import Image

from app.config import Settings
from app.dataset import Dataset, MNIST_SHA256, ensure_dataset
from app.session import Session
from app.worker import Worker


def create_app(
    settings: Settings | None = None, dataset: Dataset | None = None, worker_factory=Worker
) -> FastAPI:
    settings = settings or Settings.from_env()
    sessions = set()

    @asynccontextmanager
    async def lifespan(app: FastAPI):
        if dataset is None:
            path = await asyncio.to_thread(ensure_dataset, settings.data_dir)
            app.state.dataset = await asyncio.to_thread(Dataset, path)
        else:
            app.state.dataset = dataset
        try:
            yield
        finally:
            await asyncio.gather(*(session.close() for session in list(sessions)))

    app = FastAPI(title="Meta Quest MNIST server", version="0.1.0", lifespan=lifespan)

    @app.get("/health")
    async def health():
        return {"status": "ok"}

    @app.get("/images")
    async def images(
        offset: int = Query(0, ge=0, le=10000), limit: int = Query(100, ge=1, le=1000)
    ):
        data = app.state.dataset
        return {
            "dataset": "mnist",
            "split": "test",
            "sha256": MNIST_SHA256,
            "total": len(data.images),
            "offset": offset,
            "items": [
                {
                    "image_id": f"mnist-test-{i}",
                    "label": int(data.labels[i]),
                    "url": f"/images/mnist-test-{i}",
                }
                for i in range(offset, min(offset + limit, len(data.images)))
            ],
        }

    @app.get("/images/{image_id}")
    async def image(image_id: str):
        data = app.state.dataset
        try:
            index = data.index(image_id)
        except KeyError:
            raise HTTPException(404, "Unknown MNIST test image ID") from None
        buffer = BytesIO()
        Image.fromarray(data.images[index]).save(buffer, format="PNG")
        return Response(buffer.getvalue(), media_type="image/png")

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
