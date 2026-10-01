# MNIST 학습·추론 서버

FastAPI + Keras/TensorFlow CPU 서버다. Unity가 보낸 순차 MLP/CNN 모델을 생성하고
MNIST로 학습한 뒤, 같은 WebSocket 연결에서 테스트 이미지 ID로 추론한다.
Python 3.12와 Docker Compose를 지원한다. Unity 설정은 필요하지 않다.

구현 기준은 다음 브랜치의 문서다.

- [MODEL_JSON_SPEC.md](https://github.com/k-display-demon-hunters/meta-quest-vr-contest/blob/0670e427fe2a82d44ad1cf9e9f7eba0084da63af/docs/MODEL_JSON_SPEC.md)
- [MODEL_DESIGN_API.md](https://github.com/k-display-demon-hunters/meta-quest-vr-contest/blob/0670e427fe2a82d44ad1cf9e9f7eba0084da63af/docs/MODEL_DESIGN_API.md)
- [WEBSOCKET_PROTOCOL.md](https://github.com/k-display-demon-hunters/meta-quest-vr-contest/blob/7fc1114bf1db8be4d3ab80ae464de2d6eb396bea/docs/WEBSOCKET_PROTOCOL.md)

공통 기획서의 MLP/CNN 숫자 분류 체험을 구현한다. 픽셀·필터 조작, 퀴즈·미션·목표
정답률 판정은 클라이언트 기능이다. 기획서의 초해상도 체험은 현재 모델 JSON과 통신
명세에 정의되어 있지 않아 이번 분류 서버의 API에 임의로 추가하지 않았다.

## 로컬 실행

저장소 루트에서:

```bash
cd server
uv sync --frozen
uv run --frozen uvicorn app.main:app --host 127.0.0.1 --port 8000 --ws-max-size 1048576
```

첫 시작 시 공식 MNIST 파일(약 11 MB)을 `server/.data/mnist.npz`에 다운로드한다.
Keras가 공개한 SHA-256으로 다운로드와 캐시를 모두 검증하며, 준비 실패 시 시작하지 않는다.
추가 API 키·DB는 필요 없다. 다운로드에는
`storage.googleapis.com/tensorflow/tf-keras-datasets/mnist.npz` HTTPS 접근이 필요하다.
의존성은 `pyproject.toml`과 `uv.lock`으로 고정한다.

클라우드에서 uv 기본 캐시 경로가 쓰기 금지라면 실행 전 설정한다.

```bash
export UV_CACHE_DIR=/workspace/server-cloud-setup/uv-cache
```

## Docker 실행

`server/`에서:

```bash
docker compose up --build -d
docker compose logs -f server
curl --fail http://127.0.0.1:8000/health
docker compose down
```

MNIST 파일은 `mnist-data` named volume에 남는다. 이미지/컨테이너에 모델이나 가중치를
저장하지 않는다. 컨테이너는 비-root 사용자로 실행되고 `/health` healthcheck를 제공한다.
Python/uv 베이스 이미지도 digest로 고정한다.

TLS 검사 프록시가 있는 클라우드에서는 제공된 CA 번들을 `SSL_CERT_FILE`로 지정하고:

```bash
docker compose -f compose.yaml -f compose.proxy.yaml up --build -d
docker compose -f compose.yaml -f compose.proxy.yaml down
```

CA는 BuildKit/Compose secret으로 전달하며 이미지에 저장하지 않는다. TLS 검증을 끄지 않는다.
일반 로컬 개발에서는 proxy override를 사용하지 않는다.

Compose는 기본적으로 localhost에 바인딩한다. 같은 LAN의 Quest에서 연결할 때
`server/.env.example`을 `.env`로 복사하고 `SERVER_BIND_ADDRESS=0.0.0.0`으로 설정한다.
클라이언트 URL은 `ws://<서버 LAN IP>:8000/ws`다. 이 데모에는 인증이 없으므로 신뢰할 수
있는 개발 네트워크에서 실행한다. Uvicorn worker는 1개로 유지한다. 학습 작업은 별도
프로세스에서 실행되어 WebSocket 메시지와 HTTP 요청을 막지 않는다.

## HTTP API와 이미지 목록

| 경로 | 동작 |
| --- | --- |
| `GET /health` | 데이터 준비가 끝난 서버의 상태 |
| `GET /images?offset=0&limit=100` | MNIST 테스트 이미지 ID·정답·PNG 경로 목록 |
| `GET /images/{image_id}` | 해당 이미지의 28×28 grayscale PNG |
| `GET /docs`, `/openapi.json` | HTTP API 문서 |
| `WS /ws` | 모델 학습·취소·추론 |

테스트 분할은 공식 파일의 `x_test` 원래 순서이며 ID는 `mnist-test-0`부터
`mnist-test-9999`까지다. 선행 0이 붙은 ID는 허용하지 않는다. `/images`에는 분할 이름,
파일 SHA-256, 전체 개수가 포함된다. Unity에서도 이 목록/PNG를 사용하면 표시한
이미지와 서버 추론 이미지가 일치한다. 클라이언트가 숫자 인덱스를 추측해 생성하지 않는다.
페이지 크기는 1~1000이고 offset은 0~10000이다.

## WebSocket 사용

UTF-8 텍스트 프레임 하나에 JSON 객체 하나를 전송한다.

```json
{"type":"train","model":{"modelType":"mlp","input":{"shape":[28,28,1]},"layers":[{"type":"dense","units":32,"activation":"relu"},{"type":"dense","units":10,"activation":"softmax"}]},"epoch":2}
```

응답:

```json
{"type":"training_progress","epoch":1,"epochs":2,"logs":{"loss":0.5,"accuracy":0.8}}
{"type":"training_completed","epoch":2,"logs":{"loss":0.3,"accuracy":0.9}}
```

지표 예시일 뿐이며 실제 Keras 지표만 전송한다. 마지막 epoch는 완료 메시지만 보낸다.
`epoch` 생략과 `null`은 서버 기본값을 사용한다. 완료 이후:

```json
{"type":"infer","image_id":"mnist-test-42"}
```

`inference_result`는 같은 `image_id`와 클래스 0~9의 출력 10개를 반환한다.
`predict()`의 배치 축만 제거하며 서버가 출력을 다시 정규화하지 않는다.

```json
{"type":"cancel_training"}
```

학습 중 취소는 다음 안전한 batch/epoch 시점에 반영된다. 프로세스 종료와 모델 폐기가
끝난 뒤 `training_cancelled`를 보낸다. 완료가 먼저 확정되면 완료된 모델을 유지하고
늦은 취소에 `no_training` 오류를 보낸다. 취소 대기 중 실패는 `training_failed`다.

- 연결마다 모델 하나. 새 유효한 학습 요청은 이전 모델/가중치를 폐기하고 새로 학습한다.
- 학습·추론은 한 번에 하나. 실행 중 `train`/`infer`는 필드 검증보다 `busy`를 우선한다.
- 잘못된 학습 요청은 `invalid_request`로 거절하고 이전 완료 모델을 유지한다.
- 연결 종료 시 native TensorFlow 작업 프로세스까지 종료하여 모델과 늦은 응답을 폐기한다.
- 오류는 명세의 `type`, `request_type`, `code`, `message`만 포함한다.
- 중복 키·알 수 없는 필드·NaN/Infinity·binary 프레임을 거절한다. 메시지 한도는 1 MiB다.

모델 구성은 명세의 Conv2D/MaxPooling2D/Flatten/Dense만 지원한다. 입력은 `[28,28,1]`,
출력은 Dense 10개/softmax다. 첫 Dense 앞에는 Flatten을 자동으로 넣는다. 공간 층 뒤
Dense 앞에는 JSON의 Flatten이 필요하다. Conv stride는 1, padding은 valid이고 Pooling
stride는 poolSize, padding은 valid다. 훈련/추론 입력은 float32 0~1로 변환한다.
서버는 Adam, sparse categorical crossentropy, accuracy를 기본 사용한다.

동작 예제(`server/`에서, 다른 터미널에 서버를 실행한 상태):

```bash
uv run --frozen python examples/demo.py --model examples/mlp.json --epochs 2
uv run --frozen python examples/demo.py --model examples/cnn.json --epochs 2
```

## 서버 설정

| 환경 변수 | 기본값 | 의미 |
| --- | --- | --- |
| `DATA_DIR` | `.data` (Docker: `/data`) | 검증된 MNIST 캐시 |
| `DEFAULT_EPOCHS` | 5 | epoch 생략/null 기본값 |
| `MAX_EPOCHS` | 100 | 요청 epoch 상한 |
| `BATCH_SIZE` | 64 | 서버 batch size |
| `TRAIN_SAMPLES` | 60000 | 공식 training split 앞부분 사용 개수. 데모/검증 시 축소 가능 |
| `MAX_SESSIONS` | 4 | 동시 연결 상한, 초과 연결은 1013으로 거절 |
| `WORKER_THREADS` | 1 | 학습 프로세스별 TensorFlow intra-op 스레드 |

연결별 메모리/연산 폭증을 막기 위해 모델 최대 32층·2백만 파라미터,
층당 20만 activation·Conv 필터 256개·Dense 뉴런 4096개로 제한한다.
상한을 초과한 모델은 `invalid_request`다. CPU 데모 기준이며 GPU·모델 저장·재접속 복구·
pause/resume·추가 optimizer 옵션은 현재 통신 명세에 포함되지 않는다.

## 검증

```bash
uv sync --frozen
uv run --frozen ruff check app tests examples
uv run --frozen ruff format --check app tests examples
uv run --frozen pytest -q -m 'not integration'
uv run --frozen pytest -q -m integration
```

일반 테스트는 모델 검증, 통신 상태, 오류·취소 경합, 이미지 API를 다룬다.
integration 테스트는 공식 MNIST에서 256개 training sample로 실제 Keras MLP/CNN 학습,
추론, 진행 이벤트, 학습 도중 취소, 연결 종료 시 프로세스 회수를 확인한다.
최초 실행 시 MNIST 다운로드가 필요하다. 전체 60000개 학습과 같은 정확도를 의미하지 않는다.
