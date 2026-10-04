# 블록 모델 편집 API

`client/Assets/Scripts/ModelDesign/`, namespace `ModelDesign`.

```csharp
using ModelDesign;

ModelWorkspace workspace = new ModelWorkspace();
LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
LayerDefinition pool = LayerDefinition.MaxPooling2D(2, 2);
LayerDefinition flatten = LayerDefinition.Flatten();
LayerDefinition dense = LayerDefinition.Dense(32, ActivationType.ReLU);

workspace.AddLayer(conv);
workspace.AddLayer(pool);
workspace.AddLayer(flatten);
workspace.AddLayer(dense);
workspace.Connect(workspace.Input, conv);
workspace.Connect(conv, pool);
workspace.Connect(pool, flatten);
workspace.Connect(flatten, dense);
workspace.Connect(dense, workspace.Output);

workspace.OnValidationChanged += issues =>
{
    // issues가 비어 있으면 경고를 지운다.
    // issue.Model: 해당 덩어리, issue.Path: /blocks/인덱스, issue.Message: 설명.
};

conv.SetFilters(16); // 같은 객체에서 변경하며 자동 검증·이벤트 발생

if (ModelJsonSerializer.TrySerialize(workspace, out var json, out var issues))
{
    // json을 서버에 전달한다.
}

workspace.Disconnect(pool, flatten); // 덩어리 두 개로 분리
workspace.Connect(pool, flatten);    // 다시 연결
```

| 타입 | 역할 |
| --- | --- |
| `LayerDefinition` | 블록 설정과 `OnChanged` 이벤트 |
| `ModelDefinition` | 덩어리 하나의 읽기 전용 블록 목록 `Layers` |
| `ModelWorkspace` | 모든 덩어리와 고정 Input·Output 관리 |
| `ModelValidator` | 편집·전송 검증 |
| `ValidationIssue` | 오류가 속한 덩어리·블록 위치·설명 |
| `ModelJsonSerializer` | Input 덩어리만 JSON으로 변환 |

| 호출 | 동작 |
| --- | --- |
| `workspace.AddLayer(layer)` | 단독 블록 덩어리 추가 |
| `workspace.AddAndConnect(existingLayer, newLayer)` | 기존 덩어리의 비어 있는 끝점에 새 블록 추가·연결 |
| `workspace.Connect(first, second)` | 두 덩어리의 비어 있는 끝점 연결. 인자 순서 무관 |
| `workspace.Disconnect(first, second)` | 인접한 두 블록의 연결 해제 |
| `workspace.RemoveLayer(layer)` | 블록 삭제. 중간 블록의 양쪽은 별도 덩어리가 됨 |
| `workspace.ReplaceLayer(oldLayer, newLayer)` | 연결 위치를 유지하며 다른 블록으로 교체 |
| `workspace.GetModel(layer)` | 블록이 현재 속한 덩어리 |
| `workspace.InputModel` | Input이 현재 속한 덩어리 |
| `workspace.Models` | 읽기 전용 덩어리 목록 |
| `workspace.ValidationIssues` | 모든 덩어리의 최신 편집 검증 결과 |
| `layer.SetFilters(filters)` | Conv2D 필터 수 변경 |
| `layer.SetKernelSize(height, width)` | Conv2D 커널 크기 변경 |
| `layer.SetPoolSize(height, width)` | Pooling 크기 변경 |
| `layer.SetUnits(units)` | Dense 뉴런 수 변경 |
| `layer.SetActivation(activation)` | Conv2D·Dense 활성화 함수 변경 |
| `ModelValidator.ValidateEditing(model)` | 끝점 없는 조각도 검사. 앞부분이 없어 입력 형태를 모르면 판단 보류 |
| `ModelValidator.ValidateForTransmission(workspace)` | Input 덩어리의 구조와 고정 Output 도달 여부 검사 |

- Input·Output은 생성 시 하나씩 생긴다. 추가·삭제·교체·설정 변경 불가, 연결·분리는 가능.
- 연결은 분기·순환을 만들 수 없다. 같은 블록은 한 Workspace의 한 덩어리에만 속한다.
- 레이어 순서 오류는 편집 상태로 허용하며 검증 결과로 알린다.
- 성공한 목록·연결·설정 변경마다 편집 검증 후 `OnValidationChanged`, `OnChanged` 순서로 이벤트가 발생한다.
- 동일한 값 설정과 거절된 작업은 이벤트를 발생시키지 않는다. 최초 결과는 `ValidationIssues`로 읽는다.
- 미완성은 편집 오류가 아니다. 전송 가능 여부는 `ValidateForTransmission()`으로 확인한다.
- 연결·분리·삭제·교체 시 덩어리 객체는 새 목록으로 교체된다. UI는 블록 참조를 유지하고 `GetModel()`로 현재 덩어리를 조회한다.
- 삭제·교체된 블록은 Workspace의 이벤트 구독에서 해제한다. 화면 종료 시 `Dispose()`를 호출한다.
- 위치·회전·크기와 연결선 표현은 오브젝트/UI 계층에서 관리한다.
- Input은 JSON의 `input`으로, Output은 마지막 Dense 10개·Softmax로 변환한다. 화면의 블록 이름을 JSON에 추가하지 않는다.
- 분리된 다른 덩어리는 전송하지 않는다. `modelType`은 전송 덩어리만 보고 자동 판단한다.

덩어리 목록은 양방향으로 저장할 수 있다. 검증과 JSON 변환은 Input에서 Output 방향으로 읽는다. 끝점이 없는 조각은 양방향을 검사하여 가능한 방향을 사용한다. Input·Output은 연결을 하나씩만 가질 수 있다.
