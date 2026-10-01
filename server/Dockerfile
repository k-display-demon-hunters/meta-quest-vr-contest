# syntax=docker/dockerfile:1
FROM ghcr.io/astral-sh/uv:0.12.19@sha256:04d046b13e60d6bcec73cbc5e1cad25d680dea90c8573340950a0ac2d1aef424 AS uv
FROM python:3.12-slim@sha256:f77ac9e44ae96ef2c90b8053ea08c31f8be030f824196b0ae4db6d462c84e51f

COPY --from=uv /uv /usr/local/bin/uv
WORKDIR /app
ENV UV_PROJECT_ENVIRONMENT=/opt/venv \
    UV_LINK_MODE=copy \
    PATH="/opt/venv/bin:$PATH" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    DATA_DIR=/data \
    KERAS_BACKEND=tensorflow \
    TF_CPP_MIN_LOG_LEVEL=2
COPY --chown=10001:10001 pyproject.toml uv.lock ./
# Optional corporate/cloud proxy CA; TLS verification stays enabled.
RUN --mount=type=secret,id=ca_cert \
    if [ -f /run/secrets/ca_cert ]; then export SSL_CERT_FILE=/run/secrets/ca_cert; fi; \
    uv sync --frozen --no-dev --no-cache
COPY --chown=10001:10001 app ./app
RUN useradd --uid 10001 --create-home appuser && mkdir -p /data && chown appuser:appuser /data
USER appuser
EXPOSE 8000
HEALTHCHECK --interval=10s --timeout=3s --start-period=90s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=2)"
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1", "--ws-max-size", "1048576"]
