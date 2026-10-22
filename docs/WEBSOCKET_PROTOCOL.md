# WebSocket 학습·추론 구현과 통신 규칙

## 파일과 클래스

클라이언트 통신 코드는 `client/Assets/Scripts/Core/Networking/`의 `Networking` 네임스페이스에 있다. 모델 제작 씬은 [ModelExecutionController](../client/Assets/Scripts/CNNModelDesignLab/ModelExecutionController.cs)를 통해 이 API를 사용한다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [ModelProtocolClient.cs](../client/Assets/Scripts/Core/Networking/ModelProtocolClient.cs) | 학습 JSON·이미지 ID·취소 요청을 프로토콜 메시지로 만들고 현재 작업과 학습 모델 유무에 따라 송신을 허용한다. 수신 JSON은 메시지 종류·필드·수치·현재 상태를 검사한 뒤 타입이 있는 결과 객체와 이벤트로 바꾼다. `training_started`를 기다리는 Task와 연결 종료 시 상태 초기화도 관리한다. |
| [ModelProtocolMessages.cs](../client/Assets/Scripts/Core/Networking/ModelProtocolMessages.cs) | `ModelOperation`은 대기·학습 시작 대기·학습·취소·추론 상태를 표현한다. `TrainingProgress`는 epoch와 지표, `TrainingResult`는 최종 지표와 평가, `InferenceResult`는 이미지 ID와 출력 배열, `ModelServerError`는 요청 종류·오류 코드·설명을 담는다. UI는 원시 JSON 대신 이 객체들을 읽는다. |
| [WebSocketClient.cs](../client/Assets/Scripts/Core/Networking/WebSocketClient.cs) | Unity 컴포넌트가 켜질 때 Connection을 만들고 꺼질 때 폐기한다. `Update()`에서 Connection의 대기 이벤트를 꺼내 메인 스레드의 수신·오류·종료 이벤트로 전달하므로 상위 코드가 UI를 갱신할 수 있다. 송신 실패 시에는 서버 접수 여부가 불명확하므로 연결도 닫는다. |
| [WebSocketConnection.cs](../client/Assets/Scripts/Core/Networking/WebSocketConnection.cs) | 서버 주소로 `ClientWebSocket`을 연결하고 송신을 순서대로 처리한다. 수신 루프는 분할된 텍스트 프레임을 하나의 메시지로 조립하고 크기 한도를 검사한 뒤 콜백을 큐에 넣는다. 연결 세션을 구분해 종료된 소켓의 늦은 결과를 버리고, 종료 시 소켓·취소 토큰·송신 자원을 정리한다. |
| [main.py](../server/app/main.py) | 앱 시작 시 설정과 MNIST 데이터를 준비하고 `/ws` 연결마다 `Session`을 생성한다. 활성 세션 목록으로 동시 연결 수를 제한하고, 연결이 끝나면 해당 세션을 닫아 목록에서 제거한다. 앱 종료 시에는 남은 세션도 모두 정리한다. |
| [session.py](../server/app/session.py)의 `Session` | 한 WebSocket 연결의 현재 작업과 worker를 소유한다. 텍스트 JSON을 파싱·검증한 뒤 학습·추론·취소로 분기하고 작업 중 중복 요청을 거절한다. worker가 보낸 진행·결과를 클라이언트로 전달하며 새 학습·취소·연결 종료 때 모델 프로세스를 정리한다. |
| [models.py](../server/app/models.py) | `TrainRequest`, `InferRequest`, `CancelRequest`와 레이어별 Pydantic 형식으로 요청 필드·타입·추가 필드를 검사한다. `ModelDefinition`은 레이어를 순서대로 따라가며 형태·출력 규칙과 파라미터·활성값 크기 한도를 확인한다. Session이 worker를 만들기 전에 잘못된 요청을 거절하는 기준이다. |
| [worker.py](../server/app/worker.py)의 `Worker` | 모델 정의와 실행 설정을 받아 별도 프로세스와 양방향 Pipe를 만든다. Session에 비동기 메시지 수신과 추론 명령 송신·취소 신호 API를 제공한다. 취소는 공유 Event로 알리고, 닫을 때는 프로세스 종료와 회수를 기다린 뒤 Pipe를 해제한다. 종료되지 않은 프로세스는 강제 종료해 남은 계산 자원을 정리한다. |
| [worker.py](../server/app/worker.py)의 `build_model`·`run_worker` | `build_model()`은 JSON 레이어를 Keras Sequential 모델로 바꾼다. `run_worker()`는 MNIST 정규화·분할, 학습·검증·최종 평가를 수행하고 시작·epoch 진행·완료 메시지를 Pipe로 보낸다. 완료 후에도 모델을 보관해 같은 프로세스에서 이미지 ID에 해당하는 샘플의 추론을 처리한다. |
| [dataset.py](../server/app/dataset.py)의 `ensure_dataset`·`Dataset` | `ensure_dataset()`은 데이터 디렉터리에 MNIST 파일을 준비한다. `Dataset`은 테스트 데이터의 형태를 확인하고 `mnist-test-숫자` ID를 유효한 테스트 인덱스로 해석한다. Session은 이 인덱스를 worker에 전달하므로 클라이언트가 이미지 픽셀을 보낼 필요가 없다. |
| [config.py](../server/app/config.py)의 `Settings` | 앱 시작 시 환경 변수를 한 번 읽어 데이터 경로·기본/최대 epoch·배치 크기·샘플 수·세션 수·worker 스레드·메시지 크기를 고정된 설정 객체로 만든다. 필수값·양의 정수·범위와 기본 epoch가 최대값을 넘지 않는지 검사한다. 요청과 자식 프로세스는 이 설정 스냅샷을 재사용한다. |

### 처리 흐름

1. 클라이언트의 `ModelProtocolClient`가 요청을 만들고 `WebSocketClient → WebSocketConnection`으로 전송한다.
2. 서버의 `Session.serve()`가 텍스트 JSON을 파싱하고 중복 키·비유한 숫자를 거절한다. `Session.handle()`이 요청 스키마와 현재 작업 상태를 확인한다.
3. 학습 요청은 이전 worker를 정리하고 새 worker 프로세스를 만든다. `run_worker()`가 Keras 모델과 데이터를 준비하고 학습 시작·진행·완료를 Pipe로 보낸다.
4. `Session`은 worker 응답을 WebSocket으로 전달한다. 클라이언트는 메인 스레드에서 응답 상태·필드를 검증한 뒤 이벤트를 발생시킨다.
5. 학습 완료 후 같은 worker가 모델을 보관하고 추론 요청을 처리한다. 취소·새 학습·연결 종료 시 해당 모델과 worker를 정리한다.

서버는 `TRAIN_SAMPLES`만큼 읽은 MNIST 학습 데이터의 앞 90%를 학습, 뒤 10%를 검증에 사용한다. 나눈 뒤 학습 부분만 섞으며, MNIST 테스트 데이터는 최종 평가와 추론에 사용한다. 픽셀은 `float32`의 0~1 값으로 정규화하고 채널 축을 추가한다. 모델은 Adam, `sparse_categorical_crossentropy`, `accuracy`로 구성한다.

서버의 요청 크기는 `MAX_MESSAGE_BYTES`, 총 epoch는 `MAX_EPOCHS`, 동시 연결 수는 `MAX_SESSIONS`로 제한한다. 메시지 크기 초과는 WebSocket 코드 1009, 세션 한도 초과는 1013으로 연결을 닫는다. 클라이언트의 수신 메시지 한도는 1 MiB다.

## 범위와 연결 수명

클라이언트는 모델 JSON과 명령을 전송한다. 서버가 Keras 모델 생성, 학습, 추론을 수행한다.

- UTF-8 WebSocket 텍스트 메시지 하나에 JSON 객체 하나를 전송한다. `type`으로 메시지를 구분한다.
- 현재 연결에 모델을 하나만 둔다. 요청 ID, 모델 ID, 자동 재연결, 일시정지, 재개는 없다.
- 새로운 학습 요청을 접수할 때마다 기존 모델을 삭제하고 전달받은 모델 JSON으로 모델과 가중치를 새로 초기화한다.
- 한 번에 학습 또는 추론 작업 하나만 실행한다. 실행 중 추가 학습·추론 요청은 `busy` 오류로 거절하며 대기열에 넣지 않는다.
- 학습 중에는 취소 요청을 허용한다. 취소 확인 전까지 추가 학습·추론을 허용하지 않는다.
- 연결 종료 시 서버는 학습·추론을 종료하고 학습 중이거나 학습된 모델을 모두 삭제한다. 늦게 끝난 작업의 결과도 폐기한다.
- 새 연결은 모델이 없는 상태에서 시작한다. 클라이언트도 이전 연결의 상태를 초기화하며 처음부터 학습해야 한다.

## 메시지 목록

| type | 방향 | 의미 |
|---|---|---|
| `train` | 클라이언트 → 서버 | 모델을 새로 만들고 학습 |
| `training_started` | 서버 → 클라이언트 | 학습 시작 확인과 확정된 총 epoch 수 |
| `training_progress` | 서버 → 클라이언트 | 마지막을 제외한 epoch 완료 |
| `training_completed` | 서버 → 클라이언트 | 최종 epoch 지표와 테스트 평가 완료 |
| `cancel_training` | 클라이언트 → 서버 | 현재 학습 취소 |
| `training_cancelled` | 서버 → 클라이언트 | 학습 종료와 모델 삭제 확인 |
| `infer` | 클라이언트 → 서버 | 저장된 MNIST 이미지로 추론 |
| `inference_result` | 서버 → 클라이언트 | 해당 이미지의 모델 출력 |
| `error` | 서버 → 클라이언트 | 요청 거절 또는 작업 실패 |

학습 요청은 `training_started`로 시작을 확인한다. 시작 확인 대기 중에도 다른 학습·추론 요청을 허용하지 않는다.
추론과 학습 취소의 송신 Task는 송신 완료를 뜻하며, 처리 결과는 각각의 응답 이벤트로 받는다.

## 학습 요청

```json
{"type":"train","model":{},"epoch":5}
```

- `model`: 필수 JSON 객체. 위의 `{}`는 구조를 생략한 예시이며 실제로는 유효한 모델 JSON을 넣는다. JSON 문자열로 이중 인코딩하지 않는다.
- 모델의 내부 필드와 유효성은 [모델 JSON 명세](MODEL_JSON_SPEC.md)와 [모델 편집 API](MODEL_DESIGN_API.md)를 참고한다.
- `epoch`: 선택 필드. 지정하면 양의 정수이며 Keras `fit(epochs=...)`에 해당한다.
- `epoch`를 생략하거나 `null`로 보내면 서버 기본값을 사용한다. 클라이언트 API의 `null`은 필드 생략으로 직렬화한다.
- 옵티마이저, 배치 크기, 손실 함수, 지표, 학습 데이터 등은 서버에서 기본 설정한다. 요청에 다른 학습 설정을 넣지 않는다.
- 기본 epoch 수는 서버 설정이므로 클라이언트에서 추측하거나 고정하지 않는다.

아래 두 요청의 epoch 설정은 같은 의미다.

```json
{"type":"train","model":{}}
```

```json
{"type":"train","model":{},"epoch":null}
```

## 학습 진행과 완료

서버는 모델 생성·초기화와 학습 준비를 마치고 실제 학습을 시작할 때 아래 응답을 한 번 보낸다.
단순히 작업을 대기열에 넣은 시점에는 보내지 않는다. 모든 진행·완료 응답보다 먼저 보내야 한다.

```json
{"type":"training_started","epochs":5}
```

`epochs`는 서버가 확정한 양의 정수다. 요청의 `epoch`가 생략되거나 `null`이면 실제 적용한 서버 기본값이다.
클라이언트는 요청 송신 후 `StartingTraining` 상태에서 기다린다. 이 응답을 검증하면 `Training`으로 바뀌고
`await TrainAsync(...)`가 정상 완료된다. 시작 전 요청 거절은 `error`로 보내며 기다리던 Task도 예외로 끝난다.
시작 전에는 `CancelTrainingAsync()`를 허용하지 않는다. 시작 확인 대기를 취소하려면 `TrainAsync`의 토큰을 취소하거나 연결을 종료한다.

```json
{"type":"training_progress","epoch":1,"epochs":5,"logs":{"loss":0.35,"accuracy":0.89,"val_loss":0.38,"val_accuracy":0.87}}
```

- `epoch`: 완료한 epoch의 1부터 시작하는 번호. Keras 콜백의 0부터 시작하는 인덱스에 1을 더한다.
- `epochs`: 서버가 확정한 총 epoch 수. 생략 또는 `null` 요청에서도 실제 서버 기본값을 알려준다.
- `logs`: 해당 epoch의 학습·검증 지표. 아래 네 필드를 항상 포함한다.
- 마지막 epoch에서는 `training_progress`를 보내지 않고 아래 완료 메시지만 보낸다.

```json
{
  "type": "training_completed",
  "epoch": 5,
  "logs": {
    "loss": 0.08,
    "accuracy": 0.97,
    "val_loss": 0.10,
    "val_accuracy": 0.96
  },
  "evaluation": {
    "loss": 0.11,
    "accuracy": 0.95
  }
}
```

완료 메시지의 `epoch`는 최종 epoch 번호이며 `logs`는 마지막 epoch의 Keras 지표다.
`evaluation`은 학습 종료 후 서버가 별도의 테스트 데이터로 수행한 Keras 평가 결과다. `loss`, `accuracy` 두 필드를 항상 포함한다.
모델 파일과 가중치는 보내지 않는다. 학습된 모델은 서버의 현재 연결에 남아 추론에 사용된다.
전체 학습 이력은 완료 메시지에 포함하지 않는다.

진행·완료 메시지의 `logs`는 아래 **네 개의 필수 필드로 고정**한다. 다른 지표를 추가하거나 필드를 생략하지 않는다.

| 필드 | 의미 |
|---|---|
| `loss` | 해당 epoch에서 Keras가 계산한 학습 데이터의 손실 |
| `accuracy` | 해당 epoch에서 Keras가 계산한 학습 데이터의 분류 정확도 |
| `val_loss` | 해당 epoch 종료 시 검증 데이터의 손실 |
| `val_accuracy` | 해당 epoch 종료 시 검증 데이터의 분류 정확도 |

완료 메시지의 `evaluation`은 아래 **두 개의 필수 필드로 고정**한다.

| 필드 | 의미 |
|---|---|
| `loss` | 학습된 최종 모델의 테스트 데이터 손실 |
| `accuracy` | 학습된 최종 모델의 테스트 데이터 분류 정확도 |

서버는 정확도 지표의 이름을 `accuracy`로 설정하고 매 epoch 검증을 수행해야 한다.
학습·검증 데이터 분할은 매 학습 요청에서 동일하게 유지한다.
`logs`는 Keras의 epoch 종료 콜백에서 네 지표를 가져오고,
`evaluation`은 `model.evaluate(x_test, y_test, return_dict=True)`의 `loss`, `accuracy`를 가져온다.
검증 지표를 테스트 평가로 대신하거나 서버가 정확도를 별도로 추정하지 않는다.

모든 지표 값은 유한한 JSON 숫자다. 정확도는 백분율이 아닌 **0~1 사이 비율**이며, 예를 들어 `0.95`는 95%다.
필수 지표가 계산되지 않거나 NaN/Infinity가 발생하면 0 또는 `null`로 채우지 않고 `training_failed` 오류로 처리한다.
클라이언트도 필드 누락·추가, 잘못된 타입과 범위를 `ProtocolError`로 거절한다.

서버는 마지막 epoch와 최종 테스트 평가가 모두 끝난 뒤에만 `training_completed`를 보낸다.
평가 중에도 클라이언트는 학습 중 상태를 유지하며 추가 학습·추론을 거절한다. 취소와 연결 종료도 학습 취소 규칙을 적용한다.
평가 실패 시 `training_failed`로 종료하고 해당 모델을 삭제한다.

예를 들어 3 epoch 학습은 아래 순서다.

```text
train(epoch=3)
→ training_started(epochs=3)
→ training_progress(epoch=1, epochs=3)
→ training_progress(epoch=2, epochs=3)
→ training_completed(epoch=3)
```

1 epoch 학습도 `training_started(epochs=1)`을 먼저 받고, 진행 응답 없이 `training_completed(epoch=1)`을 받는다.

## 학습 취소

```json
{"type":"cancel_training"}
```

```json
{"type":"training_cancelled"}
```

- 서버는 취소를 접수하면 가능한 다음 안전한 시점에서 학습을 종료한다. 즉시 중단됐다고 가정하지 않는다.
- 작업 종료와 모델 삭제를 모두 마친 뒤에만 `training_cancelled`를 보낸다.
- 그전에는 클라이언트가 취소 대기 상태를 유지한다. 중간 진행 응답이 추가로 도착할 수 있다.
- 취소가 확정되면 `training_completed`는 보내지 않는다. 취소 후 추론할 모델은 없다.
- 학습 완료가 먼저 확정됐다면 `training_completed`를 먼저 보내고 뒤늦은 취소 요청에 `no_training` 오류를 보낸다. 이 경우 완료된 모델은 유지한다.
- 학습이 없으면 취소 요청은 `no_training`으로 거절한다. 취소는 완료된 모델을 삭제하는 명령이 아니다.

## 추론

```json
{"type":"infer","image_id":"mnist-test-42"}
```

`image_id`는 필수 문자열이며 양쪽이 가진 **동일한 이미지 목록의 ID**다.
`mnist-test-42`는 예시 ID이며 실제 목록의 ID를 그대로 사용한다. 클라이언트에서 ID를 인덱스로 변환하지 않는다.
서버도 동일한 목록을 조회한다. 이미지 픽셀이나 모델 JSON을 다시 전송하지 않는다.

```json
{"type":"inference_result","image_id":"mnist-test-42","output":[0.01,0.01,0.02,0.01,0.90,0.01,0.01,0.01,0.01,0.01]}
```

- `image_id`: 요청에서 받은 ID 그대로.
- `output`: Keras 모델이 그 이미지에 대해 출력한 숫자 10개. 순서는 MNIST 숫자 클래스 0부터 9까지.
- `predict()` 결과의 단일 이미지 배치 축만 제거한다. 출력값을 추가로 정규화하거나 확률로 바꾸지 않는다.
- 모델 출력층에 따라 확률 또는 점수일 수 있다. 클라이언트는 합이 1이라고 가정하지 않는다.
- 서버는 학습 완료 전에 추론을 허용하지 않는다. 모델이 없으면 `no_model`, 이미지가 없으면 `image_not_found` 오류다.
- 추론 후 모델은 유지된다. 다음 추론이나 새로 초기화하는 학습을 요청할 수 있다.

## 오류와 작업 상태

```json
{"type":"error","request_type":"train","code":"busy","message":"Another operation is running."}
```

`request_type`, `code`, `message`는 필수 문자열이다. 요청 ID 대신 명령 종류만 알린다.
오류는 통신 제어 정보이며 Keras 지표가 아니다.

| code | request_type | 처리 |
|---|---|---|
| `busy` | `train` / `infer` | 추가 요청만 거절. 진행 중 작업과 모델 상태 유지 |
| `invalid_request` | 해당 명령 | 잘못된 필드 거절. 다른 작업 실행 중이면 `busy`를 우선 |
| `training_failed` | `train` | 학습 또는 최종 평가 실패, 해당 모델 삭제 |
| `no_model` | `infer` | 모델 없음. 다시 학습 필요 |
| `image_not_found` | `infer` | 추론 종료, 모델 유지 |
| `inference_failed` | `infer` | 추론 종료, 모델 유지 |
| `no_training` | `cancel_training` | 취소할 학습 없음 |
| `cancellation_failed` | `cancel_training` | 취소 실패. 기존 학습은 계속 진행 |

클라이언트 API는 추가 학습·추론 요청을 로컬에서도 `InvalidOperationException`으로 거절한다.
서버 역시 이 규칙을 검증해야 한다. 서버의 `busy` 응답으로 기존 작업을 종료하면 안 된다.
단, 클라이언트가 `StartingTraining`에서 기다리는 최초 학습 요청이 `busy`로 거절되면
그 `TrainAsync`는 예외로 끝나고 클라이언트는 `Idle`로 돌아간다. 서버에서 실행 중인 작업을 취소하는 의미는 아니다.
학습 중 작업 실패는 취소 대기 중이라도 `training_failed`로 전달하며 모델을 삭제한다.

서버는 종료 결과를 한 번만 보낸다. 취소와 완료가 경합하면 먼저 확정한 종료 결과가 우선이다.
예상하지 못한 메시지 종류, 필수 필드 누락, 잘못된 지표, 현재 작업과 맞지 않는 응답은 클라이언트의 `ProtocolError`로 알린다.
오류 응답을 정상 완료로 간주하지 않는다.

worker 내부의 일반 추론 실패는 모델을 유지한다. worker 프로세스 자체가 종료되면 `Session.infer()`가 모델을 정리하고 `inference_failed`를 보낸다. 클라이언트는 이 오류만으로 모델 유무를 초기화하지 않으므로 다음 요청에서 `no_model`을 받을 수 있다.

## 클라이언트 사용

Unity 메인 스레드에서 `new ModelProtocolClient()`로 생성한다. 외부에서 송신 함수나 `WebSocketClient`를 넘기지 않는다.
생성자 내부에서 GameObject와 `WebSocketClient` 컴포넌트를 만들고, 수신·연결 종료·연결 오류 이벤트를 구독한다.
연결은 `ConnectAsync()`로 명시적으로 시작한다. 요청마다 같은 객체를 사용한다.
서버 주소는 `WebSocketConnection`의 `_serverUrl`에서만 설정한다. `WebSocketClient`, `ModelProtocolClient`와 그 호출자는 주소를 전달하지 않는다.
개발 기본 주소는 `ws://localhost:8000/ws`다. 주소를 바꿀 때는 `WebSocketConnection.cs` 맨 위의 `_serverUrl` 한 곳을 수정한다. 환경 변수나 `.env` 파일을 읽지 않는다. Quest 등 다른 기기에서는 `localhost` 대신 서버 PC의 LAN 주소를 사용한다.
내부 `WebSocketClient.Update()`에서 수신 이벤트를 메인 스레드로 전달한다.
사용하는 쪽은 내부 컴포넌트를 직접 다루지 않고 `ModelProtocolClient`의 메서드와 이벤트를 사용한다.
사용이 끝나면 소유한 코드의 `OnDestroy()` 등에서 `Dispose()`를 호출해 내부 GameObject와 소켓을 정리한다.

```csharp
ModelProtocolClient modelProtocol = new ModelProtocolClient();
modelProtocol.TrainingProgressReceived += HandleProgress;
modelProtocol.TrainingCompleted += HandleCompleted;
modelProtocol.TrainingCancelled += HandleCancelled;
modelProtocol.InferenceCompleted += HandleInference;
modelProtocol.ServerErrorReceived += HandleServerError;
modelProtocol.ProtocolError += HandleProtocolError;
modelProtocol.ConnectionError += HandleConnectionError;
modelProtocol.Disconnected += HandleDisconnected;
await modelProtocol.ConnectAsync();

await modelProtocol.TrainAsync(modelJson, epoch: 5);
// 위 Task는 서버의 학습 시작 확인까지 기다린다. 학습 완료는 TrainingCompleted 이벤트다.
// 학습 완료 이벤트 이후:
await modelProtocol.InferAsync("mnist-test-42");
```

학습 취소는 `CancelTrainingAsync()`로 요청한다. 메서드의 Task는 취소 요청 송신 완료를 뜻한다.
`TrainAsync`의 `CancellationToken`은 송신과 시작 확인 대기를 취소한다. 시작 확인 대기가 취소되면 연결을 닫아 서버 작업도 폐기한다.
연결 종료·Dispose·잘못된 시작 응답도 대기 Task를 예외로 종료한다. 시작 확인 후에는 이 토큰으로 학습을 취소하지 않으며 `CancelTrainingAsync()`를 사용한다.
시작 응답에 고정 시간제한은 없다. 필요한 제한 시간은 호출자가 `CancellationTokenSource.CancelAfter()`로 설정한다.
추론·학습 취소 메서드의 토큰은 네트워크 송신 취소에만 적용된다.
송신 실패는 서버가 요청을 접수했는지 알 수 없으므로 연결을 닫고 세션을 폐기한다.

`modelProtocol.Operation`은 현재 작업, `HasTrainedModel`은 추론 가능한 모델 유무다.
수동 연결 종료, 컴포넌트 비활성화, 서버 연결 종료 모두 상태를 초기화한다.
`SendJsonAsync` / `JsonReceived`는 저수준 통신 API다. 모델 작업에 직접 JSON 송신을 섞으면 요청 ID가 없는 프로토콜의 작업 상태 추적을 보장할 수 없다.
