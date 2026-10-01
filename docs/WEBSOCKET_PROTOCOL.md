# WebSocket 모델 학습·추론 통신 규칙 (데모)

## 범위와 연결 수명

클라이언트는 모델 JSON과 명령을 전송한다. 서버가 Keras 모델 생성, 학습, 추론을 수행한다.
이번 브랜치는 클라이언트와 통신 명세만 구현하며 서버 구현은 포함하지 않는다.

- UTF-8 WebSocket 텍스트 메시지 하나에 JSON 객체 하나를 전송한다. `type`으로 메시지를 구분한다.
- 현재 연결에 모델을 하나만 둔다. 요청 ID, 모델 ID, 자동 재연결, 일시정지, 재개는 없다.
- 새로운 학습 요청을 접수할 때마다 기존 모델을 삭제하고 전달받은 모델 JSON으로 모델과 가중치를 새로 초기화한다.
- 한 번에 학습 또는 추론 작업 하나만 실행한다. 실행 중 추가 학습·추론 요청은 `busy` 오류로 거절하며 대기열에 넣지 않는다.
- 학습 중에는 취소 요청을 허용한다. 취소 확인 전까지 추가 학습·추론을 허용하지 않는다.
- 연결 종료 시 서버는 학습·추론을 종료하고 학습 중이거나 학습된 모델을 모두 삭제한다. 늦게 끝난 작업의 결과도 폐기한다.
- 새 연결은 모델이 없는 상태에서 시작한다. 클라이언트도 이전 연결의 상태를 초기화하며 처음부터 학습해야 한다.

나중에 모델 ID와 재연결 복구를 추가할 수 있지만 이번 메시지에는 그 필드나 자리표시자를 넣지 않는다.

## 메시지 목록

| type | 방향 | 의미 |
|---|---|---|
| `train` | 클라이언트 → 서버 | 모델을 새로 만들고 학습 |
| `training_progress` | 서버 → 클라이언트 | 마지막을 제외한 epoch 완료 |
| `training_completed` | 서버 → 클라이언트 | 최종 epoch 및 학습 완료 |
| `cancel_training` | 클라이언트 → 서버 | 현재 학습 취소 |
| `training_cancelled` | 서버 → 클라이언트 | 학습 종료와 모델 삭제 확인 |
| `infer` | 클라이언트 → 서버 | 저장된 MNIST 이미지로 추론 |
| `inference_result` | 서버 → 클라이언트 | 해당 이미지의 모델 출력 |
| `error` | 서버 → 클라이언트 | 요청 거절 또는 작업 실패 |

별도의 접수 응답은 없다. 학습·추론 요청을 보낸 클라이언트는 결과나 오류가 올 때까지 해당 작업을 실행 중으로 취급한다.

## 학습 요청

```json
{"type":"train","model":{},"epoch":5}
```

- `model`: 필수 JSON 객체. 위의 `{}`는 구조를 생략한 예시이며 실제로는 유효한 모델 JSON을 넣는다. JSON 문자열로 이중 인코딩하지 않는다.
- 모델의 내부 필드와 유효성은 `feature/graph-to-model-compiler` 브랜치의 `docs/MODEL_JSON_SPEC.md`와 `docs/MODEL_DESIGN_API.md`를 참고한다. 이 문서에서는 모델 구조를 재정의하지 않는다.
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

```json
{"type":"training_progress","epoch":1,"epochs":5,"logs":{"loss":0.35,"accuracy":0.89}}
```

- `epoch`: 완료한 epoch의 **1부터 시작하는 번호**. Keras 콜백의 0부터 시작하는 인덱스에 1을 더한다.
- `epochs`: 서버가 확정한 총 epoch 수. 생략 또는 `null` 요청에서도 실제 서버 기본값을 알려준다.
- `logs`: 해당 epoch의 Keras 콜백 지표 사전. 지표 이름을 그대로 유지한다.
- 마지막 epoch에서는 `training_progress`를 보내지 않고 아래 완료 메시지만 보낸다.

```json
{"type":"training_completed","epoch":5,"logs":{"loss":0.08,"accuracy":0.97}}
```

완료 메시지의 `epoch`는 최종 epoch 번호이며 `logs`는 마지막 epoch의 Keras 지표다.
모델 파일, 가중치, 서버가 별도로 만든 평가 결과는 보내지 않는다. 학습된 모델은 서버의 현재 연결에 남아 추론에 사용된다.
전체 학습 이력은 이번 완료 메시지에 포함하지 않는다.

`logs`의 지표는 고정하지 않는다. `loss`, `accuracy`, `val_loss`, `val_accuracy` 등 **실제 Keras에서 제공된 지표만** 담는다.
정확도나 검증 지표가 설정되지 않았다면 해당 필드는 없다. 없는 값에 0이나 `null`을 넣지 않는다.
지표 값은 유한한 JSON 숫자다. NaN/Infinity가 발생하면 정상 완료 대신 학습 실패로 처리한다.

예를 들어 3 epoch 학습은 아래 순서다.

```text
train(epoch=3)
→ training_progress(epoch=1, epochs=3)
→ training_progress(epoch=2, epochs=3)
→ training_completed(epoch=3)
```

1 epoch 학습은 `training_completed(epoch=1)`만 받는다.

Keras의 [콜백 logs](https://keras.io/api/callbacks/base_callback/)와
[fit / predict API](https://keras.io/api/models/model_training_apis/)를 기준으로 지표와 출력값을 전달한다.
콘솔의 진행 막대 문자열을 전송하지 않는다.

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
이미지 목록을 연결할 때 클라이언트와 서버가 같은 데이터 분할과 ID 매핑을 사용해야 한다.

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
| `training_failed` | `train` | 학습 종료, 해당 모델 삭제 |
| `no_model` | `infer` | 모델 없음. 다시 학습 필요 |
| `image_not_found` | `infer` | 추론 종료, 모델 유지 |
| `inference_failed` | `infer` | 추론 종료, 모델 유지 |
| `no_training` | `cancel_training` | 취소할 학습 없음 |
| `cancellation_failed` | `cancel_training` | 취소 실패. 기존 학습은 계속 진행 |

클라이언트 API는 추가 학습·추론 요청을 로컬에서도 `InvalidOperationException`으로 거절한다.
서버 역시 이 규칙을 검증해야 한다. 서버의 `busy` 응답으로 기존 작업을 종료하면 안 된다.
학습 중 작업 실패는 취소 대기 중이라도 `training_failed`로 전달하며 모델을 삭제한다.

서버는 종료 결과를 한 번만 보낸다. 취소와 완료가 경합하면 먼저 확정한 종료 결과가 우선이다.
예상하지 못한 메시지 종류, 필수 필드 누락, 잘못된 지표, 현재 작업과 맞지 않는 응답은 클라이언트의 `ProtocolError`로 알린다.
오류 응답을 정상 완료로 간주하지 않는다.

## 클라이언트 사용

`WebSocketClient` 컴포넌트의 Inspector에서 `_serverUrl`을 설정한다. 연결을 자동으로 시작하지 않는다.
Unity 메인 스레드에서 메서드를 호출하고 `ModelProtocol` 이벤트를 구독한다. `Update()`에서 수신 이벤트를 전달한다.

```csharp
await webSocketClient.ConnectAsync();
webSocketClient.ModelProtocol.TrainingProgressReceived += HandleProgress;
webSocketClient.ModelProtocol.TrainingCompleted += HandleCompleted;
webSocketClient.ModelProtocol.TrainingCancelled += HandleCancelled;
webSocketClient.ModelProtocol.InferenceCompleted += HandleInference;
webSocketClient.ModelProtocol.ServerErrorReceived += HandleServerError;
webSocketClient.ModelProtocol.ProtocolError += HandleProtocolError;

await webSocketClient.TrainAsync(modelJson, epoch: 5);
// 위 Task는 송신 완료를 뜻한다. 학습 완료는 TrainingCompleted 이벤트다.
// 학습 완료 이벤트 이후:
await webSocketClient.InferAsync("mnist-test-42");
```

학습 취소는 `CancelTrainingAsync()`로 요청한다. 메서드의 Task는 취소 요청 송신 완료를 뜻한다.
`CancellationToken`은 네트워크 송신 취소이며 서버 학습 취소 명령을 대신하지 않는다.
송신 실패는 서버가 요청을 접수했는지 알 수 없으므로 연결을 닫고 세션을 폐기한다.

`ModelProtocol.Operation`은 현재 작업, `HasTrainedModel`은 추론 가능한 모델 유무다.
수동 연결 종료, 컴포넌트 비활성화, 서버 연결 종료 모두 상태를 초기화한다.
기존 `SendJsonAsync` / `JsonReceived`는 저수준 통신용으로 유지하지만 모델 작업은 위 API를 사용한다.
직접 JSON 송신을 섞으면 요청 ID가 없는 데모의 작업 상태 추적을 보장할 수 없다.

## 검증

`client/Assets/Tests/EditMode/Networking`의 테스트는 요청 직렬화, 상태 전환, 취소와 완료의 경합,
오류 처리, 연결 종료 후 모델 폐기를 검증한다. 로컬 WebSocket으로 분할 메시지 수신,
JSON 송신, 연결 종료 후 대기 중이던 응답 폐기도 확인한다. 실제 Keras 서버와의 연동은 별도로 필요하다.

```text
unity test <repository>/client --mode EditMode --filter Networking.Tests
```
