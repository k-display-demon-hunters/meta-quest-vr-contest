# MNIST 학습·추론 서버

Unity가 보낸 MLP/CNN 모델을 MNIST로 학습하고 추론하는 FastAPI 서버다.

- [모델 JSON 명세](../docs/MODEL_JSON_SPEC.md)
- [WebSocket 통신 규칙](../docs/WEBSOCKET_PROTOCOL.md)
- [클라이언트 모델 편집 API](../docs/MODEL_DESIGN_API.md)

## Windows 실행 (Docker 없이)

Windows에서 간단히 실행해 볼 때는 설치된 Python을 사용하면 된다.
가상환경(`.venv`)은 필수가 아니며, 아래 안내는 가상환경 없이 실행하는 방법이다.

### 1. PowerShell 열기

파일 탐색기에서 이 저장소의 `server` 폴더를 연다. 위쪽 주소 표시줄을 클릭하고
`powershell`을 입력한 뒤 Enter를 누르면, 해당 폴더에서 PowerShell 창이 열린다.

아래 명령은 이 창에 복사하고 Enter를 눌러 실행한다. 코드 블록 위의 `powershell`은
명령에 포함하지 않는다.

### 2. Python 확인하기

```powershell
python --version
```

`Python 3.x.x`처럼 버전이 표시되면 설치된 Python을 사용할 수 있다.
Python 3.12(64비트)를 권장한다.

### 3. 필요한 패키지 설치하기

다음 명령을 실행하고 설치가 끝날 때까지 기다린다. 이 작업은 처음 한 번만 하면 된다.

```powershell
python -m pip install fastapi "uvicorn[standard]" tensorflow-cpu
```

패키지는 FastAPI 0.142.2, Uvicorn 0.54.0, TensorFlow CPU 2.20.0을 권장한다.
위 명령은 버전을 고정하지 않으며, 권장 버전을 설치하려면 패키지 이름 뒤에 `==버전`을 붙인다.

### 4. 서버 실행하기

테스트 과정에서만 적용: 만약 .env 파일이 없다면 .env.example 파일을 복사하고 복사본의 이름을 .env로 바꾸면 된다.

```powershell
python -m uvicorn app.main:app --env-file .env --host 127.0.0.1 --port 8000 --workers 1 --ws-max-size 1048576
```

첫 실행 시 MNIST를 `.data/mnist.npz`에 다운로드하므로 잠시 기다린다.
`Application startup complete`가 표시되면 준비가 끝난 것이다.

서버를 사용하는 동안 이 PowerShell 창은 열어 둔다. 종료하려면 창을 클릭하고 `Ctrl+C`를 누른다.
다음 실행부터는 `server` 폴더에서 PowerShell을 열고 서버 실행 명령만 입력하면 된다.

## Docker 실행

Docker Compose가 필요하다. Windows Docker Desktop은 Linux 컨테이너 모드를 사용한다.
저장소 루트에서 실행한다.

처음에는 `server/.env.example`을 `server/.env`로 복사하고 필요한 설정을 수정한다.

```sh
cd server
docker compose up --build -d
```

로그는 `docker compose logs -f server`, 종료는 `docker compose down`으로 확인·수행한다.
MNIST 캐시는 `mnist-data` 볼륨에 유지된다.

프록시 CA가 필요한 환경에서는 `SSL_CERT_FILE`에 CA 파일 경로를 설정하고 다음 명령을 사용한다.

```sh
docker compose -f compose.yaml -f compose.proxy.yaml up --build -d
```

이 구성의 종료에도 같은 `-f` 옵션을 사용한다.

## 연결

같은 PC의 WebSocket 주소는 `ws://127.0.0.1:8000/ws`다.
다른 기기에서 연결하려면 두 기기를 같은 네트워크에 연결하고 다음을 설정한다.

- Windows 직접 실행: `--host 0.0.0.0`으로 실행하고 방화벽에서 TCP 8000 접근을 허용한다.
- Docker: `.env.example`을 `.env`로 복사하고 `SERVER_BIND_ADDRESS=0.0.0.0`으로 설정한다.
- 클라이언트 주소: `ws://<서버 PC의 LAN IP>:8000/ws`.

## 설정

학습·서버 동작 설정은 `server/.env`에서 변경한다. 항목은 [.env.example](.env.example)을 참고한다.
서버 코드는 시작 시 환경 변수로 설정 객체를 한 번 만들고 검증한다.
요청과 학습 프로세스는 이 객체를 재사용하며 `.env` 파일을 직접 읽지 않는다.
Windows는 서버를 재시작하고, Docker는 `docker compose up -d --force-recreate`로 변경을 반영한다.

Windows는 `server/`에서 실행 명령의 `--env-file .env`로 Uvicorn이 파일을 읽도록 한다.
다른 위치에서 실행하면 `--env-file`에 해당 파일의 경로를 지정해야 한다.
Docker Compose는 `env_file`로 같은 파일을 컨테이너 환경 변수에 전달한다.
Windows에서 이미 설정된 환경 변수는 `.env`보다 우선한다.
`Settings`의 모든 항목은 필수이며, 누락되거나 비어 있으면 해당 변수 이름과 함께 시작 오류를 낸다.
`config.py`에는 기본값이 없다. 설정 예시는 [.env.example](.env.example)에 둔다.
Uvicorn의 `.env` 로딩에 쓰이는 `python-dotenv`는 `uvicorn[standard]`에 포함된 의존성이다.
캐시 경로 `DATA_DIR`의 상대 경로는 `server/` 기준이며 Docker에서는 볼륨 경로 `/data`로 고정한다.
`SERVER_BIND_ADDRESS`와 `SERVER_PORT`는 Docker 포트 공개용이다.
Uvicorn worker는 1개로 유지한다. `max_message_bytes`를 변경하면 실행 명령과 Dockerfile의
`--ws-max-size`도 맞춘다.

서버의 모델 제한은 최대 32층·200만 파라미터, 층당 20만 activation,
Conv 필터 256개·Dense 뉴런 4096개다. 초과 요청은 `invalid_request`로 거절한다.

## 사용 예제

Windows에서 서버를 실행한 채로, 파일 탐색기의 `server` 폴더에서 PowerShell 창을 하나 더 연다.
다음 중 하나를 실행하면 모델 학습과 이미지 추론을 요청하고 응답을 표시한다.

MLP 예제:

```powershell
python examples/demo.py --model examples/mlp.json --epochs 2
```

CNN 예제:

```powershell
python examples/demo.py --model examples/cnn.json --epochs 2
```

`--epochs 2`는 두 번의 epoch 동안 학습한다는 뜻이다. 학습·추론 응답이 표시되고
명령 입력줄이 다시 나타나면 예제가 끝난 것이다. 서버 창은 계속 실행 중이다.

## 개발 검증

`server/`에서 실행한다. 통합 테스트에는 MNIST 다운로드와 실제 Keras 학습이 포함된다.

```sh
uv sync --frozen
uv run --frozen ruff check app tests examples
uv run --frozen ruff format --check app tests examples
uv run --frozen pytest -q -m "not integration"
uv run --frozen pytest -q -m integration
```
