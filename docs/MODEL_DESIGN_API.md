# 블록 모델 편집 API

`client/Assets/Scripts/CNNModelDesignLab/ModelDesign/`, namespace `ModelDesign`.

씬 전용 폴더 아래에 배치하지만 순수 모델 코드는 `ModelDesign.asmdef`로 Unity 컴포넌트와 분리한다. 어셈블리 이름 `ModelDesign`과 `noEngineReferences: true`를 유지하며 Unity API에 의존하지 않는다.

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

`Assets/Scenes/CNNModelDesignLab.unity`와 `Assets/Scripts/CNNModelDesignLab/`에 컨베이어 슬롯 방식의 3D 블록 조작을 구성했다. Input과 Output 사이의 칸에 블록을 놓으면 배치 순서가 모델 순서가 된다. 포트와 연결선 조작은 사용하지 않는다.

### 진입과 기본 조작

- `QuestMain`에서 오른손 레이로 `CNN MODEL DESIGN / Grip to enter` 표지를 가리키고 Grip을 누르면 제작 씬을 추가 로드한다. 기존 바닥·XR 리그를 사용한다.
- 단독 개발용으로 씬에 기존 `XRPlayerRig.prefab` 인스턴스가 배치되어 있다. 현재 리그는 `EditorOnly`가 아니며, 기존 리그가 있는 상태에서 진입하면 `PersistentXRPlayerRig`가 새 중복 리그를 제거한다. 리그와 시뮬레이터 설정은 이번 변경에서 수정하지 않았다.
- 시뮬레이터에서 `G`는 오른손 Grip(잡기·놓기), `T`는 Trigger(설정·UI 클릭)다. `]`로 오른손 위치 조작을 선택하고 WASD/QE와 마우스 우클릭으로 움직인다. 전체 조작법은 [XR 시뮬레이터 조작법](xr-simulator.md)을 참고한다.
- 오른쪽 `Parts Supply`는 직육면체 벽·선반으로 만든 고정 부품 공급소다. 각 선반에 Conv2D, MaxPooling2D, Flatten, Dense 블록이 있다. 본체를 Grip으로 잡아 꺼내면 같은 기본 설정의 새 블록이 해당 자리에 보충된다.
- 공급소에는 이동 핸들·XRGrabInteractable·Rigidbody가 없다. 공급소 자체는 이동할 수 없고, 꺼낸 블록만 자유롭게 움직인다. 외형은 `PartsSupplyRack.prefab`에서 교체한다.

### 슬롯 배치 규칙

- Input과 Output은 벨트 양 끝에 고정되어 있으며 이동·삭제할 수 없다. 고정 입력·출력 설정은 본체 라벨로 표시한다.
- 두 끝점 사이에는 기본 8칸이 있고, 칸 하나에는 블록 하나만 배치된다. 벨트와 슬롯은 정지해 있으며 자동 운반은 하지 않는다.
- 블록을 잡고 있는 동안에는 3D 공간에서 자유롭게 움직인다. 방향은 유지하며 기존 오른쪽 스틱으로 거리를 조절할 수 있다. 벨트 평면이나 영역 안으로 이동을 제한하지 않는다.
- 배치 판정은 레이가 가리키는 곳이 아니라 **블록 중심의 실제 위치**를 사용한다. 빈 칸의 배치 범위에 들어오면 노란색, 이미 찬 칸이면 빨간색으로 표시한다.
- 빈 칸 위에서 Grip을 놓으면 칸 중앙과 벨트 방향으로 정렬한다. 배치된 칸은 녹색으로 표시한다.
- 찬 칸이나 벨트 밖에서 놓으면 기존 블록은 그대로 두고, 들고 있던 블록은 놓은 위치에 자유 상태로 남는다. 자동 교환·밀어내기·삭제·원위치 복귀는 하지 않는다.
- 배치된 블록을 다시 잡으면 원래 칸이 즉시 비고 모델 순서에서도 빠진다. 다른 빈 칸에 놓으면 새 순서로 반영된다. 잡기가 취소되어도 자동 배치하지 않는다.
- 중간 빈 칸은 건너뛰고 Input → 배치된 블록들 → Output 순서로 모델을 구성한다. 다른 블록의 실제 위치를 자동으로 당기지 않는다. 벨트 밖 블록과 꾸러미 블록은 전송 모델에 포함되지 않는다.
- 레이어 순서가 잘못되어도 물리적인 배치는 허용한다. 기존 모델 검증이 학습 시작 시 전송 가능 여부를 판단한다.

### 설정과 학습 UI

- 각 슬롯 앞에 고정된 설정 조작기기가 하나씩 있다. 빈 칸에서는 `SLOT 번호 / EMPTY`와 어두운 색으로 비활성 상태를 표시한다. 블록을 놓으면 `SETTINGS` 표시와 함께 사용할 수 있게 된다.
- 오른손 레이로 해당 조작기기를 가리키고 Trigger를 누르면 **현재 그 슬롯에 배치된 블록**의 설정이 열린다. 블록 본체 클릭으로는 팝업을 열지 않는다. 벨트 밖 블록을 설정하거나 삭제하려면 먼저 빈 슬롯에 놓는다.
- 팝업은 조작기기의 `Popup Anchor`에 붙어 같은 위치·방향으로 열린다. 사용자 위치·머리 방향·카메라 방향을 읽거나 따라가지 않는다. 팝업은 한 번에 하나만 열며 다른 슬롯 조작기를 누르면 그쪽 기기로 옮겨 해당 블록을 표시한다.
- 블록을 잡으면 열려 있던 팝업을 닫고, 슬롯을 비우면 그 칸의 조작기도 비활성화한다. 삭제 후에도 같은 슬롯의 이전 블록을 편집할 수 없다. 블록 이동 중이나 앞에 다른 UI가 있는 경우 조작기기를 통과해서 설정을 열지 않는다.
- 숫자는 `-`/`+`, 활성화 함수는 `<`/`>`로 변경한다. Flatten에는 숫자 설정이 없다.
- 팝업에는 `Delete`와 `Done`이 있다. 기존 `Disconnect` 버튼은 제거했다. 모델에서 빼고 보관하려면 블록을 잡아 벨트 밖에 놓는다.
- `Delete`는 블록을 삭제하고 점유 슬롯을 비운 뒤 남은 슬롯 순서로 모델을 갱신한다. Input·Output의 삭제는 버튼과 코드 양쪽에서 막는다.
- 벨트 맨 왼쪽 Input 앞의 `TrainingControls.prefab`에 학습 시작·중지 버튼과 학습중 표시를 함께 배치했다. `학습 시작` 버튼은 `ModelJsonSerializer.TrySerialize`로 현재 슬롯 순서의 모델을 검증하고 JSON으로 변환한다. 블록을 잡고 이동하는 동안에는 시작하지 않는다. 성공하면 `학습중` 문구와 `중지` 버튼으로 바뀌며, 실패하면 시작 버튼을 유지한다.
- `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 JSON 또는 실패 경로·메시지를 콘솔에 출력한다. 오류 표시 UI와 서버 학습·중지 요청은 아직 연결하지 않았다. `중지` 또는 `CNNModelDesignLabManager.FinishTraining()`은 시작 상태로 복원한다.

### 구성과 외형 교체

- `CNNModelDesignLabManager`: Workspace 수명, 씬 초기화·종료 순서, 블록 이동·삭제에 따른 모델 갱신, 학습 시작 검증과 JSON 변환을 조율한다. Controller를 직접 참조하고 각 인스턴스 이벤트를 구독한다. 기존 `ModelWorkspace.Connect`/`Disconnect` API를 사용하며 모델 JSON 명세는 변경하지 않는다.
- `ModelSupplyController`: 고정 공급소의 기본 블록 생성과 잡아 꺼낸 자리의 보충을 담당한다.
- `ModelBlockController`: 개별 블록의 XRI 잡기·놓기 입력과 라벨·색상을 담당한다. XRI 선택 필터로 Input·Output의 잡기를 막으며, 본체의 Trigger 설정 이벤트는 사용하지 않는다. 기존 `ModelBlockView`의 역할을 유지하면서 이름을 바꿨다.
- `ModelConveyorController`: 슬롯 점유, 배치 후보 표시, 위치 판정과 놓을 때의 정렬, 슬롯별 조작기기와 Input 쪽 학습 조작부의 배치를 담당한다.
- `SlotSettingsController`: 해당 슬롯의 블록 참조, 조작기기의 비활성/활성 표시와 Trigger 입력, 팝업 기준점을 담당한다.
- `ModelSettingsController`: 설정 팝업의 대상 블록과 고정 기준점, 열기·닫기와 삭제 요청을 담당한다. 기존 `ModelDesignSceneController`에서 공급·학습·XR 입력을 분리하고 팝업 역할만 남겼다. `BlockSettingsPopup`은 레이어별 설정 항목 표시와 값 편집을 유지한다.
- `ModelTrainingController`: 학습 시작·중지 버튼 입력 이벤트와 학습 상태 표시를 담당한다.
- `ModelXRInputController`: 오른손 레이와 UI 입력을 설정하고 조작기기 입력의 유효한 레이 적중 여부를 확인한다. 비활성화할 때 기존 오른손 설정을 복구한다.

Controller는 Manager를 참조하지 않고 자신의 입력이나 요청을 인스턴스 이벤트로 알린다. Manager는 블록을 잡을 때 공급 보충·팝업 닫기·슬롯 비우기를 조율하고, 놓을 때 슬롯 배치 결과를 모델 순서에 반영한다. Singleton, 전역 이벤트 시스템, 새로운 공통 기반 클래스는 도입하지 않는다. `LayerDefinition`, `ModelWorkspace`, 검증·직렬화 클래스의 이름과 API는 그대로 유지한다.

`Assets/Prefabs/CNNModelDesignLab/ModelConveyor.prefab`에는 직사각형 벨트와 8개의 슬롯·설정 조작기기가 미리 배치되어 Play 전에도 보인다. `ModelBlock.prefab`은 포트 없는 직육면체이며, 이름·기본 색상으로 종류를 구분한다. 새 재질은 `Assets/Materials/CNNModelDesignLab/`에 있다.

벨트 Inspector의 `Slot Count`, `Slot Spacing`, `Block Center Height`, `Placement Half Extents`로 칸 수·간격·블록 높이·배치 허용 범위를 조절한다. **이 값은 씬 시작 시 적용**되며 실행 중 칸 수 변경은 지원하지 않는다. 기본 8칸과 다른 값을 저장하면 편집 화면의 기존 슬롯 미리보기는 그대로지만 실행 시 필요한 슬롯과 조작기기를 보충하거나 숨기고 양 끝점 위치·벨트 길이·Input 쪽 학습 조작부 위치를 맞춘다.

외형은 `Belt Visual`, `Slot Visuals`의 Renderer, 블록의 `Body`를 교체한다. 슬롯 판정은 렌더러의 메시·콜라이더가 아닌 벨트 기준 위치와 허용 범위를 사용한다. 슬롯 시각 요소는 `Slot Visuals`의 직접 자식 Renderer로 유지하고 `Slot Template`에는 그중 하나를 연결한다. 새 블록 외형의 크기가 달라지면 본체 Collider와 `Block Center Height` 등도 함께 조절한다.

`SlotSettingsControl.prefab`에서 조작기기 외형과 `Popup Anchor`의 로컬 위치·회전을 조절한다. 슬롯 조작기기는 `Slot Settings Controls` 아래에 두며, 빈 칸 표시와 입력 상태는 슬롯 점유에 따라 갱신된다. `Training Anchor`에는 기존 학습 UI 인스턴스가 연결되어 있다.

이번 변경은 소스와 에셋 참조만 정적으로 검토했다. 테스트 실행·테스트 파일 생성·Unity 실행·컴파일·빌드는 수행하지 않았다.
