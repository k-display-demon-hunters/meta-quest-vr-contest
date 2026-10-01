"""Run an actual WebSocket train/infer round trip against the server."""

import argparse
import asyncio
import json
import math
from pathlib import Path

from websockets.asyncio.client import connect


async def run(args):
    model = json.loads(Path(args.model).read_text())
    async with asyncio.timeout(args.timeout):
        async with connect(args.url, proxy=None) as socket:
            request = {"type": "train", "model": model}
            if args.epochs is not None:
                request["epoch"] = args.epochs
            await socket.send(json.dumps(request))
            while True:
                result = json.loads(await socket.recv())
                print(json.dumps(result, ensure_ascii=False))
                if result["type"] == "training_completed":
                    break
                if result["type"] != "training_progress":
                    raise RuntimeError(f"Training did not succeed: {result}")
            await socket.send(json.dumps({"type": "infer", "image_id": args.image_id}))
            result = json.loads(await socket.recv())
            if (
                result["type"] != "inference_result"
                or result["image_id"] != args.image_id
                or len(result["output"]) != 10
                or not all(math.isfinite(value) for value in result["output"])
            ):
                raise RuntimeError(f"Inference did not succeed: {result}")
            print(json.dumps(result))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", default="ws://127.0.0.1:8000/ws")
    parser.add_argument("--model", default="examples/mlp.json")
    parser.add_argument("--epochs", type=int)
    parser.add_argument("--image-id", default="mnist-test-42")
    parser.add_argument("--timeout", type=int, default=300)
    asyncio.run(run(parser.parse_args()))
