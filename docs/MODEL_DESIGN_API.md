# 모델 편집 API

`client/Assets/Scripts/ModelDesign/`의 순수 C# API. 씬·게임 오브젝트 참조 없이 사용한다.

```csharp
using MetaQuest.ModelDesign;

ModelDefinition model = new ModelDefinition();
model.AddLayer(LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU));
model.AddLayer(LayerDefinition.MaxPooling2D(2, 2));
model.AddLayer(LayerDefinition.Flatten());
model.AddLayer(LayerDefinition.Dense(32, ActivationType.ReLU));
model.AddLayer(LayerDefinition.Output());

// UI에서 뉴런 개수나 활성화 함수를 변경했을 때
model.ReplaceLayer(3, LayerDefinition.Dense(64, ActivationType.Tanh));

if (ModelJsonSerializer.TrySerialize(model, out var json, out var issues))
{
    // json을 서버 전송 코드에 전달한다.
}
else
{
    // issues의 Path와 Message를 UI에 표시한다.
}
```

| 호출 | 동작 |
| --- | --- |
| `AddLayer(layer)` | 마지막에 추가 |
| `InsertLayer(index, layer)` | 지정 위치에 추가 |
| `ReplaceLayer(index, layer)` | 해당 층의 종류·설정 교체 |
| `MoveLayer(fromIndex, toIndex)` | 이동 후의 인덱스로 순서 변경 |
| `RemoveLayer(index)` | 해당 층 삭제 |
| `Layers` | 읽기 전용 목록 |
| `ModelValidator.Validate(model)` | 구조 오류 목록 반환 |
| `ModelJsonSerializer.TrySerialize(...)` | 유효하면 JSON, 아니면 오류 목록 반환 |

인덱스는 0부터 시작한다. 레이어 추가·삭제·이동 후에는 UI의 인덱스도 갱신한다.
레이어 설정은 불변이며 변경할 때 새 설정으로 교체한다.
잘못된 숫자·활성화 함수·인덱스·null은 예외를 발생시킨다.
편집 중 불완전한 구조는 허용하되 JSON 전송 전 검증한다. 실패 시 `json`은 null이다.
입력 형태와 버전은 직렬화 시 고정한다. 출력층은 `Output()`으로 추가한다.
MLP는 `Flatten()`부터 시작한다. 서버 통신·학습·추론은 이 API에 포함하지 않는다.
