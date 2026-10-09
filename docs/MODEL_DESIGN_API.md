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

## VR 모델 제작 씬

`Assets/Scenes/CNNModelDesign.unity`와 `Assets/Scripts/ModelDesignScene/`에 3D 블록 조작을 구성했다. 별도의 작업판·배경판·경계를 만들지 않으며 빈 3D 공간 자체를 사용한다. 꾸러미 주변의 제목·사용법·Palette Hint 안내 요소는 없다.

- `QuestMain`에서 오른손 레이로 `CNN MODEL DESIGN / Grip to enter` 표지를 가리키고 Grip을 누르면 제작 씬을 추가 로드한다. 이 경로에서는 기존 바닥·XR 리그를 사용한다.
- 중간 스테이지 단독 개발을 위해 `CNNModelDesign` 씬에 기존 `XRPlayerRig.prefab` 인스턴스를 `TEMP XRPlayerRig - Remove before final` 이름으로 임시 배치했다. 에디터에서 이 씬을 바로 Play하면 기존 자동 생성 XR Interaction Simulator의 입력을 사용한다. 별도 데스크톱 입력 코드나 시뮬레이터 인스턴스는 추가하지 않는다. 임시 리그는 `EditorOnly` 태그로 빌드에서 제외되며, `QuestMain`을 거쳐 에디터에서 진입하면 기존 중복 방지 코드가 임시 리그를 제거한다. **최종 씬 진입 흐름이 연결되면 이 씬의 임시 리그 인스턴스를 제거한다. 원본 프리팹은 유지한다.**
- 시뮬레이터에서 `G`는 오른손 Grip(잡기·놓기), `T`는 Trigger(설정·UI 클릭)다. `]`로 오른손 위치 조작을 선택하고 WASD/QE와 마우스 우클릭으로 움직인다. 시점이 낮으면 FPS 모드에서 `E`로 높이를 올린다. 전체 조작법은 [XR 시뮬레이터 조작법](xr-simulator.md)을 참고한다.
- 오른쪽 꾸러미에는 Conv2D, MaxPooling2D, Flatten, Dense 입체 블록이 직접 보인다. Input·Output과 꾸러미 블록은 씬의 프리팹 인스턴스로 미리 배치되어 Play 전에도 보인다. 블록 본체를 Grip으로 잡아 끌어오면 꾸러미에는 같은 기본 설정의 새 블록이 남는다. 꾸러미에 남아 있는 블록의 포트는 연결 조작을 받지 않는다.
- 꾸러미 블록 4개는 `Palette` 오브젝트의 자식이다. 상단 `Handle`을 오른손 레이로 가리키고 Grip을 누르면 꾸러미 전체를 옮길 수 있다. 방향은 유지하고 위치만 이동한다. 새 블록은 이동한 꾸러미의 해당 자리에 보충되며, 이미 꺼낸 블록은 꾸러미와 함께 움직이지 않는다.
- 연결된 블록을 잡아도 선택한 블록만 3D 공간에서 이동한다. 연결된 다른 블록의 위치는 유지하며 연결선의 끝점이 이동한 포트를 따라간다. 특정 평면이나 작업판 영역에 위치를 제한하지 않는다. 블록의 방향은 유지되며, 잡은 상태에서 기존 오른쪽 스틱으로 거리를 조절할 수 있다.
- 작업공간의 빈 포트를 오른손 Ray로 가리키고 Grip으로 잡으면 포트에서 연결선이 나온다. Grip을 유지한 채 다른 덩어리의 반대쪽 빈 포트를 가리키면 미리보기 선이 노란색이 된다. 그 포트에서 Grip을 놓으면 두 포트를 잇는 초록색 선이 남으며, 블록 위치는 바뀌지 않는다. 빈 공간·블록 본체·꾸러미·이미 사용 중인 포트·같은 쪽 포트에서 놓으면 취소한다. 같은 덩어리 안의 연결도 허용하지 않아 분기·순환을 만들 수 없다. 시뮬레이터에서는 `G`로 잡기·놓기를 조작한다.
- 블록 본체를 Grip으로 잡으면 해당 블록을 이동하고, 포트를 Grip으로 잡으면 선을 연결한다. 본체에는 `XRGrabInteractable`, 포트에는 `XRSimpleInteractable`을 사용하며 입력·Ray 판정은 기존 XRI가 담당한다. 블록을 가까이 옮겨 놓는 동작만으로는 연결하지 않는다. 연결선의 양 끝은 이동한 포트를 따라간다.
- 완료된 연결선에 오른손 Ray가 닿으면 노란색으로 표시하고 벗어나면 초록색으로 돌아간다. 선을 가리키고 Trigger(시뮬레이터 `T`)를 누르면 해당 연결 하나만 해제하고 선을 제거한다. 양쪽 블록의 위치는 유지한다. 선에도 `XRSimpleInteractable`과 이동하는 캡슐 콜라이더를 사용하며, 포트 바로 앞에는 클릭 판정 여백을 둔다. 블록 이동·포트 연결 중에는 선 클릭으로 해제하지 않는다. 연결 미리보기 선에는 클릭 판정을 붙이지 않는다.
- 연결 포트는 지름 5cm이며, 오른손 Ray가 연결된 포트에 닿으면 노란색, 비어 있는 포트에 닿으면 빨간색으로 표시한다. 연결된 포트를 가리키고 Trigger(시뮬레이터 `T`)를 누르면 그 포트에 붙은 연결 하나만 해제한다. 같은 블록의 다른 포트 연결과 블록 위치는 유지한다. 레이가 벗어나면 현재 연결 상태에 따라 기본색·노란색·초록색으로 돌아간다. 앞에 팝업이 있는 경우 뒤쪽 포트를 강조하거나 연결·해제하지 않는다.
- 작업공간의 블록 본체 또는 비어 있는 포트를 오른손 레이로 가리키고 Trigger를 누르면 설정 팝업이 열린다. 연결된 포트는 Trigger로 연결을 해제한다. 블록을 잡고 이동하거나 포트를 연결하는 중에는 팝업을 열거나 포트 클릭으로 연결을 해제하지 않는다.
- 숫자는 `-`/`+`, 활성화 함수는 `<`/`>`로 변경한다. Input·Output은 고정 설정을 표시하고 Flatten에는 숫자 설정이 없다.
- 팝업의 `Disconnect`는 선택 블록의 양쪽 연결과 연결선을 제거한다. 블록 위치는 유지하며 양옆 덩어리를 자동으로 연결하지 않는다. `Delete`는 선택 블록과 그 블록에 붙은 연결선을 삭제한다. Input·Output의 삭제 버튼은 비활성화되며 삭제 처리에서도 보호한다. `Done`은 팝업을 닫는다.

씬 컨트롤러가 `ModelWorkspace`의 연결 데이터와 블록의 수명을 관리하며 모델 편집은 기존 API를 사용한다. 블록·팝업은 `Assets/Prefabs/ModelDesign/`의 프리팹으로 편집할 수 있다. 블록 초기 배치는 씬 프리팹 인스턴스의 Transform으로 조절한다. 연결선은 컨트롤러가 `LineRenderer`로 생성하며 재질과 굵기를 Inspector에서 조절한다. 블록 본체는 깊이가 있는 메시와 URP Lit 재질을 사용하며 앞면에 종류·설정 라벨을 표시한다.

오류 표시 위치·연동 방식은 미정이다. 기존 검증 동작을 유지하며 오류 UI, 별도 오류 이벤트, 태블릿 연동, 학습 요청·저장 기능은 추가하지 않았다. 오른손의 UI/hovered activation 설정은 제작 씬 활성화 동안 적용하고 비활성화 때 복구한다.

이 변경에서는 소스와 에셋 참조만 정적으로 검토했다. 테스트 실행·테스트 파일 생성·Unity 실행·컴파일·빌드는 수행하지 않았다.
