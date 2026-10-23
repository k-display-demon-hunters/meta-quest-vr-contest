# 모델 JSON 문법

클라이언트가 편집 모델을 서버에 전달하는 형식이다. [모델 편집 API](MODEL_DESIGN_API.md)의 Workspace를 직렬화하고 [WebSocket 통신](WEBSOCKET_PROTOCOL.md)의 `train.model`에 JSON 객체로 담는다.

## 구현 파일

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [ModelJsonSerializer.cs](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelJsonSerializer.cs) | Workspace를 전송 검증한 뒤 Input과 연결된 레이어만 실행 순서로 읽는다. Input은 `input.shape`, 나머지는 레이어별 필드로 기록하고 공간 레이어 유무로 `modelType`을 결정한다. 성공한 JSON 객체가 통신 요청의 `model`에 들어간다. |
| [ModelValidator.cs](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelValidator.cs) | 편집 레이어를 따라가며 공간 크기와 Flatten·Dense 전환을 검사한다. 전송 전에는 고정 Output 도달 여부와 최종 출력 규칙도 확인하며, 문제가 있으면 모델·블록 위치·설명 목록을 반환해 직렬화를 중단시킨다. |
| [models.py](../server/app/models.py) | 수신 JSON을 레이어별 Pydantic 객체로 바꾸면서 필수 필드·타입·허용값·추가 필드를 검사한다. 이어 입력부터 출력까지 형태와 누적 파라미터를 계산해 구조 오류나 서버 한도 초과를 거절한다. 통과한 정의만 worker에 전달된다. |
| [session.py](../server/app/session.py) | WebSocket 텍스트를 JSON 객체로 파싱하면서 중복 키·비유한 숫자를 거절한다. `type`에 맞는 요청 스키마를 선택하고 epoch·현재 작업 상태 등을 검사한 뒤, 유효한 학습 정의를 worker 생성에 사용한다. 오류는 프로토콜의 오류 응답으로 돌려준다. |
| [worker.py](../server/app/worker.py) | `build_model()`이 검증된 정의를 읽어 Keras Input과 Conv2D·Pooling·Flatten·Dense를 차례로 추가한다. 첫 Dense 입력은 필요하면 자동으로 펼치고 각 레이어의 설정을 Keras 생성자에 적용한다. 만든 Sequential 모델은 같은 파일의 실행 루프에서 학습·추론에 사용한다. |

## 기본 구조

```json
{
  "modelType": "mlp",
  "input": {"shape": [28, 28, 1]},
  "layers": []
}
```

- `layers`: 실행 순서. 각 층은 바로 이전 층의 출력을 입력으로 받는다.
- `modelType`: 모델 종류. `"mlp"`는 다층 퍼셉트론, `"cnn"`은 합성곱 신경망을 의미한다.
- 변환기가 전송 덩어리의 레이어를 보고 자동 생성한다. Conv2D 또는 Pooling이 있으면 `"cnn"`, 없으면 `"mlp"`. Flatten만으로는 CNN으로 분류하지 않는다.
- 입력: `[높이, 너비, 채널] = [28, 28, 1]`. 배치 차원 제외.
- 아래 필드는 모두 필수. `null`, 정의되지 않은 필드, 중복 키는 허용하지 않는다.

## 레이어

### Conv2D

```json
{
  "type": "conv2d",
  "filters": 8,
  "kernelSize": [3, 3],
  "activation": "relu"
}
```

- `filters`: 양의 정수.
- `kernelSize`: `[높이, 너비]`, 양의 정수 2개.

### MaxPooling2D

```json
{
  "type": "max_pooling2d",
  "poolSize": [2, 2]
}
```

- `poolSize`: `[높이, 너비]`, 양의 정수 2개.

### Flatten

```json
{"type": "flatten"}
```

### Dense

```json
{
  "type": "dense",
  "units": 3,
  "activation": "relu"
}
```

- `units`: 뉴런 개수, 양의 정수.
- 완전연결만 지원하며 연결 필드는 없다.

## 활성화 함수

Conv2D·Dense의 `activation` 허용값:

| 적용 위치 | 허용값 |
| --- | --- |
| Conv2D, 은닉 Dense | `"linear"`, `"relu"`, `"sigmoid"`, `"tanh"`, `"softmax"` |
| 최종 Dense | `"softmax"` |

Pooling·Flatten에는 `activation` 필드가 없다.

## 구조 제약

- 분기·합류·건너뛰기·순환 연결 금지.
- 첫 레이어가 Dense이면 서버가 입력을 784개 값으로 자동으로 펼친다. JSON에 Flatten을 넣지 않아도 된다.
- Conv2D·Pooling에서 Dense로 전환할 때는 Flatten을 명시한다.
- Dense·Flatten 이후에는 Dense만 허용한다.
- 마지막 층: Dense, `units = 10`, `activation = "softmax"` 고정.
- 출력 인덱스 0~9는 숫자 클래스 0~9에 대응한다.

## 서버의 모델 구성과 수신 한도

`build_model()`은 Conv2D에 `padding="valid"`, `strides=1`을 적용한다. MaxPooling2D는 `padding="valid"`와 풀 크기에 따른 기본 stride를 사용한다. Conv2D·Pooling 결과의 높이·너비가 1보다 작으면 수신 검증에서 거절한다.

`models.py`는 모델 생성 전에 다음 한도를 검사하며, 초과하면 `invalid_request`로 응답한다.

| 항목 | 한도 |
| --- | --- |
| `layers` 개수 | 1~32 |
| Conv2D `filters` | 1~256 |
| Dense `units` | 1~4096 |
| 공간 레이어의 높이 × 너비 × 채널 | 200,000 이하 |
| 누적 학습 파라미터 | 2,000,000 이하 |

공간 레이어의 출력 형태와 누적 파라미터는 각 레이어를 순서대로 계산한다. 클라이언트 편집 검증을 통과해도 서버의 수신 한도는 별도로 적용된다.

## 전체 예제

### MLP

```json
{
  "modelType": "mlp",
  "input": {"shape": [28, 28, 1]},
  "layers": [
    {"type": "dense", "units": 32, "activation": "relu"},
    {"type": "dense", "units": 10, "activation": "softmax"}
  ]
}
```

### CNN

```json
{
  "modelType": "cnn",
  "input": {"shape": [28, 28, 1]},
  "layers": [
    {
      "type": "conv2d",
      "filters": 8, "kernelSize": [3, 3],
      "activation": "relu"
    },
    {
      "type": "max_pooling2d",
      "poolSize": [2, 2]
    },
    {"type": "flatten"},
    {
      "type": "dense",
      "units": 10,
      "activation": "softmax"
    }
  ]
}
```
