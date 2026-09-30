# 모델 JSON 문법 v1

## 기본 구조

```json
{
  "schemaVersion": 1,
  "input": {"shape": [28, 28, 1]},
  "layers": []
}
```

- `layers`: 실행 순서. 각 층은 바로 이전 층의 출력을 입력으로 받는다.
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
| Conv2D, 은닉 Dense | `"linear"`, `"relu"`, `"sigmoid"`, `"tanh"` |
| 최종 Dense | `"softmax"` |

Pooling·Flatten에는 `activation` 필드가 없다.

## 구조 제약

- 분기·합류·건너뛰기·순환 연결 금지.
- Flatten은 정확히 한 번 필요하다. Flatten 이후에는 Dense만 허용한다.
- 마지막 층: Dense, `units = 10`, `activation = "softmax"` 고정.
- 출력 인덱스 0~9는 숫자 클래스 0~9에 대응한다.

## 전체 예제

```json
{
  "schemaVersion": 1,
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
