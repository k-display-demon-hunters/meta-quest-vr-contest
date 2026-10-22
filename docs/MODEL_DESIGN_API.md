# 모델 제작 씬과 편집 API

대상 씬은 [CNNModelDesignLab.unity](../client/Assets/Scenes/CNNModelDesignLab.unity)다. 슬롯에 배치된 블록 순서로 모델을 구성하고 서버에 학습·추론을 요청한다. 조작은 [공통 조작법](xr-simulator.md), 전송 형식은 [모델 JSON 명세](MODEL_JSON_SPEC.md)와 [WebSocket 통신](WEBSOCKET_PROTOCOL.md)을 참고한다.

## 파일과 클래스

씬 스크립트는 `client/Assets/Scripts/CNNModelDesignLab/`, 네임스페이스는 `ModelDesignScene`이다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [CNNModelDesignLabManager](../client/Assets/Scripts/CNNModelDesignLab/CNNModelDesignLabManager.cs) | Workspace와 고정 Input·Output을 준비하고 공급·배치·설정·실행 Controller의 이벤트를 연결한다. 블록을 꺼내거나 슬롯에 놓을 때 Workspace의 레이어와 연결 순서를 갱신하며, 학습 요청 시 검증·직렬화 후 ExecutionController에 JSON을 전달한다. 응답을 결과 화면에 넘기고 현재 편집 모델이 학습 당시 모델과 달라졌는지도 비교한다. |
| [ModelDesignEntrance](../client/Assets/Scripts/CNNModelDesignLab/ModelDesignEntrance.cs) | 진입 표지의 XRI Activate를 받아 제작 씬을 Additive로 연다. 진입 뒤 표지를 비활성화해 같은 안내판을 다시 선택하지 않게 한다. 실제 모델 생성과 학습 준비는 로드된 씬의 Manager가 수행한다. |
| [ModelSupplyController](../client/Assets/Scripts/CNNModelDesignLab/ModelSupplyController.cs) | 공급소에 놓일 레이어 종류별 기본값으로 블록을 준비한다. 블록이 꺼내졌는지 판별하고 원래 자리에 새 블록을 보충한다. 꺼낸 블록의 Workspace 등록과 슬롯 배치는 Manager가 처리하므로 공급소는 계속 사용할 부품을 제공하는 역할이다. |
| [ModelBlockController](../client/Assets/Scripts/CNNModelDesignLab/ModelBlockController.cs) | 하나의 `LayerDefinition`을 3D 블록에 연결하고 레이어 변경을 라벨에 반영한다. XRI 잡기·놓기를 이벤트로 알려 Manager가 공급소 출고나 슬롯 이동을 처리하게 한다. 고정 Input·Output은 선택을 차단해 공급 블록과 구분한다. |
| [ModelConveyorController](../client/Assets/Scripts/CNNModelDesignLab/ModelConveyorController.cs) | 슬롯별 점유 블록을 보관하고 잡은 블록의 중심을 벨트 로컬 좌표로 바꿔 배치 가능한 슬롯을 찾는다. 이동 중 후보를 표시하고 유효한 빈 슬롯에 놓으면 위치·방향을 맞춘다. 슬롯 순서의 블록 목록과 설정 조작기기 상태를 제공해 Manager가 모델 연결을 다시 만들게 한다. |
| [SlotSettingsController](../client/Assets/Scripts/CNNModelDesignLab/SlotSettingsController.cs) | 한 슬롯의 설정 조작기기에 붙어 현재 배치된 블록과 팝업 기준점을 보관한다. 슬롯의 사용 가능 상태에 맞춰 조작기기를 갱신하고 XRI Activate를 설정 요청으로 전달한다. Manager가 입력과 블록 상태를 확인한 뒤 실제 팝업을 열게 한다. |
| [ModelXRInputController](../client/Assets/Scripts/CNNModelDesignLab/ModelXRInputController.cs) | 공용 오른손 레이와 XR UI 입력 환경을 연결한다. 슬롯 설정 요청이 이 레이에서 왔는지, 물체를 잡고 있지 않은지, 가까운 UI에 가려지지 않고 해당 슬롯 Collider에 맞았는지 검사한다. 입력 종료 시 자신이 추가한 EventSystem 또는 UI 모듈을 정리한다. |
| [ModelSettingsController](../client/Assets/Scripts/CNNModelDesignLab/ModelSettingsController.cs) | 팝업 인스턴스 하나를 만들고 현재 설정 대상 블록을 보관한다. 슬롯 요청을 받으면 팝업을 해당 `Popup Anchor`의 자식으로 옮겨 대상 레이어를 표시하고, 삭제 버튼은 블록을 지정한 이벤트로 Manager에 전달한다. 닫기·비활성화·파괴 시 대상 참조와 팝업을 정리한다. |
| [BlockSettingsPopup](../client/Assets/Scripts/CNNModelDesignLab/BlockSettingsPopup.cs) | 선택한 레이어 종류에 필요한 숫자·활성화 항목을 구성한다. 설정 버튼은 `LayerDefinition.Set*()`을 호출해 값을 바꾸고 변경 이벤트가 Workspace 검증과 블록 라벨 갱신으로 이어지게 한다. Done은 팝업을 닫고 Delete는 상위 Controller에 삭제를 요청한다. |
| [ModelTrainingController](../client/Assets/Scripts/CNNModelDesignLab/ModelTrainingController.cs) | 컨베이어의 학습 시작·중지 버튼을 사용자 요청 이벤트로 바꾼다. Manager가 알려준 실행 상태와 취소 가능 여부로 문구와 버튼 활성 상태를 갱신한다. 모델 JSON 준비나 서버 통신은 Manager와 ExecutionController에서 수행한다. |
| [ModelExecutionController](../client/Assets/Scripts/CNNModelDesignLab/ModelExecutionController.cs) | 직렬화된 모델 JSON을 받아 통신 세션을 만들고 연결 → 시작 확인 대기 → 학습 → 완료 상태를 관리한다. 중지 시점에 따라 연결을 닫거나 서버 취소 응답을 기다리며, 학습된 모델이 있으면 이미지 ID 추론 요청을 보낸다. 진행·결과·오류를 이벤트로 알리고 세션·작업 번호로 종료된 요청의 늦은 응답을 걸러낸다. |
| [ModelResultsController](../client/Assets/Scripts/CNNModelDesignLab/ModelResultsController.cs) | 결과 화면을 구성하고 Metrics·Inference Controller를 생성한다. Manager가 넘긴 실행 상태·진행·완료·오류를 각 표시 담당 객체에 전달하며, 편집 모델 변경 안내를 별도로 표시한다. 샘플 선택에서 생긴 추론 요청은 이벤트로 Manager에 돌려보낸다. |
| [ModelTrainingMetricsController](../client/Assets/Scripts/CNNModelDesignLab/ModelTrainingMetricsController.cs) | 학습 진행 응답에서 epoch와 학습·검증 loss·accuracy를 읽어 숫자와 그래프에 누적한다. 완료 응답의 마지막 epoch도 반영하고, 별도 테스트 평가 지표는 최종 결과로 표시한다. 새 학습에서는 이전 지표를 초기화한다. |
| [ModelInferenceResultsController](../client/Assets/Scripts/CNNModelDesignLab/ModelInferenceResultsController.cs) | 이미지·정답 목록으로 샘플 페이지를 구성하고 선택한 이미지의 에셋 이름을 추론 ID로 요청한다. 응답 ID가 대기 중인 샘플과 일치할 때 예측 숫자·클래스별 출력을 표시한다. 학습 모델 유무와 작업 상태에 따라 샘플 선택 가능 여부도 갱신한다. |
| [ModelResultsUIBuilder](../client/Assets/Scripts/CNNModelDesignLab/ModelResultsUIBuilder.cs) | 결과 표시 객체들이 사용할 루트·텍스트·배경·버튼 등 uGUI 요소를 생성한다. 공통 글꼴과 색상·배치를 적용한 참조를 돌려주며, 지표 데이터나 추론 대기 상태는 생성한 UI를 사용하는 Controller가 관리한다. |
| [ModelMetricGraph](../client/Assets/Scripts/CNNModelDesignLab/ModelMetricGraph.cs) | epoch별 학습·검증 수치를 저장하고 `Graphic`의 메시로 두 데이터의 선 그래프를 그린다. MetricsController가 진행 응답을 추가하거나 초기화하면 그래프가 갱신된다. 서버 통신과 지표 계산은 수행하지 않고 전달된 수치를 시각화한다. |

Manager는 Controller의 인스턴스 이벤트를 구독하고 기능 사이의 순서를 조율한다. Controller는 Manager를 직접 참조하지 않는다. `ModelTrainingMetricsController`, `ModelInferenceResultsController`, `ModelResultsUIBuilder`는 `ModelResultsController`가 생성하는 일반 C# 객체다.

## 블록 배치에서 모델 갱신까지

1. Manager가 `Awake()`에서 Workspace를 생성한다. `Start()`에서 고정 Input·Output 블록, 공급소, 슬롯을 초기화하고 이벤트를 연결한다.
2. 공급소의 블록을 잡으면 `ModelSupplyController.TryTakeBlock()`이 기본 블록을 보충한다. Manager는 꺼낸 블록의 레이어를 Workspace에 추가한다.
3. 슬롯에 있던 블록을 잡으면 Manager가 팝업을 닫고 해당 슬롯을 비운 뒤 모델 순서를 갱신한다.
4. 잡고 있는 동안 `LateUpdate()`가 `PreviewPlacement()`를 호출한다. `ModelConveyorController`는 블록 중심을 벨트 로컬 좌표로 변환하고 슬롯 중심과 `Placement Half Extents`를 비교한다.
5. 정상적으로 놓으면 `TryPlaceBlock()`이 빈 슬롯에 블록을 등록하고 위치·방향을 정렬한다. 슬롯 밖·점유된 슬롯·취소된 잡기는 배치하지 않는다.
6. `RefreshModelOrder()`가 이전 연결을 해제하고 `Input → 슬롯 순서의 블록 → Output`으로 연결한다. 빈 슬롯은 건너뛰며 다른 블록의 실제 위치는 옮기지 않는다.

슬롯 밖 블록도 Workspace에는 남지만 Input에 연결되지 않아 전송 대상에서 제외된다. 배치는 레이어 순서 오류를 허용하며, 학습 시작 시 전송 검증으로 걸러낸다.

## 설정과 삭제

`SlotSettingsController`가 설정 요청을 보내면 Manager가 현재 블록, 잡기 상태, `ModelXRInputController.CanActivateSlot()` 결과를 확인한다. `ModelSettingsController`는 해당 슬롯의 `Popup Anchor`에 팝업을 연다. 팝업은 한 번에 하나이며 사용자 시점을 따라가지 않는다.

`BlockSettingsPopup`은 레이어 종류에 맞는 숫자·활성화 항목을 만들고 `LayerDefinition.Set*()`을 호출한다. 값 변경은 Workspace의 검증·변경 이벤트로 이어진다. 삭제 요청은 Manager가 받아 슬롯 제거, Workspace 레이어 제거, 이벤트 해제, 블록 비활성화·파괴 순서로 처리한다. Input·Output은 삭제할 수 없다.

## 학습·추론 흐름과 수명

`ModelTrainingController`의 시작 요청을 받은 Manager는 작업 중이거나 블록을 잡고 있으면 요청을 처리하지 않는다. `ModelJsonSerializer.TrySerialize()`가 성공하면 결과 화면을 초기화하고 `ModelExecutionController.StartTrainingAsync()`에 JSON을 넘긴다.

`ModelExecutionController`는 연결을 만들고 `TrainAsync(modelJson, epoch: null)`을 호출한다. 서버 기본 epoch를 사용하며 `Connecting → StartingTraining → Training` 상태로 진행한다. 시작 확인 전의 중지는 연결 종료로 처리하고, 학습 중의 중지는 취소 요청 후 서버 응답을 기다린다. 학습·추론·취소 대기 중에는 추가 학습·추론을 받지 않는다.

진행·완료 응답은 Manager를 거쳐 결과 화면으로 전달된다. `ModelTrainingMetricsController`는 학습·검증 지표를 epoch별로 누적하고 마지막 epoch는 완료 응답에서 추가한다. 최종 테스트 지표는 별도로 표시한다. 총 epoch 표시도 실제 진행·완료 응답을 사용한다.

학습 완료 후 `ModelInferenceResultsController`는 선택한 샘플의 `Texture2D.name`을 `image_id`로 보낸다. 응답 ID가 선택한 샘플과 일치하는지 확인한 뒤 예측 숫자와 클래스별 출력을 표시한다. 씬의 고정 Output은 Softmax이며 수신값을 재정규화하지 않고 백분율로 표시한다.

모델은 학습 중·후에도 편집할 수 있다. Manager는 현재 JSON과 `SubmittedModelJson`을 비교해 결과가 이전 구성에 해당하는지 표시한다. 추론은 서버에 남아 있는 마지막 학습 모델을 사용한다.

Manager의 `OnDisable()`은 세션 종료, 결과 초기화, 팝업 닫기, 잡기 취소와 입력 해제를 수행한다. `ModelExecutionController`는 세션·작업 세대 번호를 비교해 종료된 요청의 늦은 응답을 무시한다. `OnDestroy()`에서는 이벤트 구독과 Workspace를 해제한다. 서버 모델은 연결과 함께 사라지므로 다시 진입하면 학습이 필요하다.

## 씬·프리팹 연결

| 에셋·필드 | 구현 연결 |
| --- | --- |
| [XRPlayerRig.prefab](../client/Assets/Prefabs/XR/XRPlayerRig.prefab) | 공용 추적·입력 리그. `PersistentXRPlayerRig`가 씬 추가 로드 시 중복 리그를 제거 |
| [ModelBlock.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelBlock.prefab) | `ModelBlockController`, Collider, XRI 잡기 구성 |
| [PartsSupplyRack.prefab](../client/Assets/Prefabs/CNNModelDesignLab/PartsSupplyRack.prefab) | 공급소와 기본 블록 참조 |
| [ModelConveyor.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelConveyor.prefab) | 슬롯·설정 조작기기·고정 Input/Output 배치 |
| [SlotSettingsControl.prefab](../client/Assets/Prefabs/CNNModelDesignLab/SlotSettingsControl.prefab) | `SlotSettingsController`와 `Popup Anchor` |
| [TrainingControls.prefab](../client/Assets/Prefabs/CNNModelDesignLab/TrainingControls.prefab) | `ModelTrainingController`의 버튼과 상태 참조. 벨트의 `Training Anchor`에 연결 |
| [ModelResults.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelResults.prefab) | Output 자식의 결과 화면. `Font`와 `Samples`를 Inspector에서 지정 |

벨트의 `Slot Count`, `Slot Spacing`, `Block Center Height`, `Placement Half Extents`는 씬 초기화 시 슬롯 수·간격·기준 높이·배치 허용 범위에 적용된다. 실행 중 슬롯 수 변경은 지원하지 않는다. 슬롯 판정은 Renderer의 메시·Collider가 아니라 벨트 로컬 좌표를 사용한다. `Slot Visuals`는 직접 자식 Renderer로 구성하고 `Slot Template`에 복제할 Renderer를 연결한다.

공용 오른손 입력은 [XRPlayerInputController](../client/Assets/Scripts/Core/XR/XRPlayerInputController.cs)가 담당한다. 모델 씬은 레이 적중 여부만 검사한다. 서버 주소는 [WebSocketConnection](../client/Assets/Scripts/Core/Networking/WebSocketConnection.cs)의 `_serverUrl`에서 설정한다.

## 순수 모델 코드

소스는 [ModelDesign 폴더](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/)에 있으며 네임스페이스와 어셈블리 이름은 `ModelDesign`이다. [ModelDesign.asmdef](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelDesign.asmdef)는 `noEngineReferences: true`로 Unity API 의존성을 차단한다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [LayerDefinition](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/LayerDefinition.cs) | 레이어 한 개의 종류와 filters·kernelSize·units·activation 같은 설정값을 보관한다. 설정 API로 실제 값이 바뀌면 `OnChanged`를 발생시켜 Workspace의 재검증과 블록 표시 갱신을 유도한다. 3D 위치나 슬롯 번호는 포함하지 않는다. |
| [ModelDefinition](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelDefinition.cs) | 서로 연결된 레이어 덩어리 하나를 읽기 전용 `Layers` 목록으로 나타낸다. Workspace가 연결·분리 작업의 결과로 이 객체를 구성하고 Validator와 Serializer가 목록을 읽는다. 블록이 이동한 뒤의 소속은 Workspace에서 다시 조회한다. |
| [ModelWorkspace](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelWorkspace.cs) | 고정 Input·Output과 모든 레이어 덩어리를 소유한다. 연결·분리·삭제·교체 요청이 오면 덩어리 목록과 레이어 이벤트 구독을 갱신하고 편집 검증을 다시 수행한다. 변경된 검증 결과와 모델 변경을 이벤트로 알려 씬·UI가 대응하게 한다. |
| [ModelValidator](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelValidator.cs) | 레이어 순서대로 입력 형태를 추적해 공간 크기·Flatten 전환·Dense 연결 등 구조를 검사한다. 편집 검증은 아직 입력이 없는 조각의 판정을 보류할 수 있고, 전송 검증은 Input에서 고정 Output까지 유효하게 연결되었는지도 요구한다. 오류는 해당 모델과 블록 위치를 가진 `ValidationIssue` 목록으로 반환한다. |
| [ValidationIssue](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ValidationIssue.cs) | 검증에서 발견한 문제 하나를 `Model`, `Path`, `Message`로 묶는다. 호출자는 모델 참조로 문제가 속한 덩어리를 찾고 Path로 블록 위치를 구분해 오류를 표시할 수 있다. 검증을 실행하거나 모델을 수정하지 않는 결과 데이터다. |
| [ModelJsonSerializer](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelJsonSerializer.cs) | 전송 검증을 먼저 실행하고 성공하면 Input에 연결된 덩어리를 실행 순서로 읽는다. Input은 입력 형태로, Output은 고정 Dense로 변환하고 레이어 종류에 필요한 필드와 `modelType`을 JSON에 기록한다. 실패 시 `TrySerialize()`는 오류 목록을, `Serialize()`는 예외를 제공한다. |

### 편집 API

| 호출·속성 | 동작 |
| --- | --- |
| `workspace.AddLayer(layer)` | 단독 블록 덩어리 추가 |
| `workspace.AddAndConnect(existingLayer, newLayer)` | 기존 덩어리의 비어 있는 끝점에 새 블록 추가·연결 |
| `workspace.Connect(first, second)` | 서로 다른 두 덩어리의 비어 있는 끝점 연결. 인자 순서 무관 |
| `workspace.Disconnect(first, second)` | 인접한 두 블록의 연결 해제 |
| `workspace.RemoveLayer(layer)` | 블록 삭제. 중간 블록의 양쪽은 별도 덩어리가 됨 |
| `workspace.ReplaceLayer(oldLayer, newLayer)` | 연결 위치를 유지하며 블록 교체 |
| `workspace.GetModel(layer)` | 블록이 현재 속한 덩어리 |
| `workspace.InputModel` / `workspace.Models` | Input 덩어리 / 전체 덩어리의 읽기 전용 목록 |
| `workspace.ValidationIssues` | 최신 편집 검증 결과 |
| `layer.SetFilters(filters)` / `SetKernelSize(height, width)` | Conv2D 설정 변경 |
| `layer.SetPoolSize(height, width)` | Pooling 크기 변경 |
| `layer.SetUnits(units)` | Dense 뉴런 수 변경 |
| `layer.SetActivation(activation)` | Conv2D·Dense 활성화 함수 변경 |
| `ModelValidator.ValidateEditing(model)` | 미완성 덩어리의 편집 검증. 입력 형태를 모르면 해당 판정 보류 |
| `ModelValidator.ValidateForTransmission(workspace)` | Input 덩어리의 구조와 고정 Output 도달 여부 검사 |
| `ModelJsonSerializer.TrySerialize(workspace, out json, out issues)` | 검증 실패 시 `false`와 오류 목록, 성공 시 JSON 반환 |
| `ModelJsonSerializer.Serialize(workspace)` | JSON 반환. 검증 실패 시 `InvalidOperationException` 발생 |

Input·Output은 Workspace 생성 시 하나씩 생기며 연결·분리만 가능하다. 분기·순환은 허용하지 않고 같은 블록은 한 Workspace의 한 덩어리에만 속한다. 덩어리 목록은 양방향으로 저장할 수 있으며 검증·직렬화는 Input에서 Output 방향으로 읽는다. 끝점 없는 조각은 양방향을 검사해 가능한 방향을 사용한다.

레이어 순서 오류와 미완성 모델은 편집 상태로 보관할 수 있다. 목록·연결·설정 변경에 성공하면 편집 검증 후 `OnValidationChanged`, `OnChanged` 순서로 이벤트가 발생한다. 동일한 값 설정과 거절된 작업은 이벤트를 발생시키지 않는다.

연결·분리·삭제·교체는 덩어리 객체를 새 목록으로 교체하므로 UI는 블록 참조를 유지하고 `GetModel()`로 현재 덩어리를 조회한다. 삭제·교체 시 레이어 이벤트 구독을 해제하고, Workspace 사용 종료 시 `Dispose()`를 호출한다. 위치·회전·크기·표시는 씬 계층에서 관리한다.

직렬화는 Input을 `input`, Output을 마지막 Dense 10개·Softmax로 변환한다. Input에 연결되지 않은 덩어리는 보내지 않는다. `modelType`은 전송 덩어리에 Conv2D 또는 Pooling이 있으면 `cnn`, 없으면 `mlp`다.

### 사용 예제

```csharp
using ModelDesign;

ModelWorkspace workspace = new ModelWorkspace();
LayerDefinition dense = LayerDefinition.Dense(32, ActivationType.ReLU);
workspace.AddLayer(dense);
workspace.Connect(workspace.Input, dense);
workspace.Connect(dense, workspace.Output);

workspace.OnValidationChanged += issues =>
{
    // issue.Model, issue.Path, issue.Message로 블록별 오류를 표시한다.
};

dense.SetUnits(64);
if (ModelJsonSerializer.TrySerialize(workspace, out var json, out var issues))
{
    // json을 ModelProtocolClient.TrainAsync()에 전달한다.
}

workspace.Dispose();
```

## 추론 샘플 연결

기본 샘플은 [Textures/CNNModelDesignLab](../client/Assets/Textures/CNNModelDesignLab/)의 MNIST 이미지다. `ModelResultsController`의 Inspector `Samples`에 이미지와 정답을 함께 연결한다. ID는 확장자를 제외한 이미지 에셋 이름이며 서버도 같은 ID의 MNIST 테스트 이미지를 사용한다. 이미지 픽셀은 전송하지 않는다.

| 정답 | 에셋 이름·전송 ID |
| --- | --- |
| 0 | `mnist-test-3` |
| 1 | `mnist-test-2` |
| 2 | `mnist-test-1` |
| 3 | `mnist-test-18` |
| 4 | `mnist-test-4` |
| 5 | `mnist-test-8` |
| 6 | `mnist-test-11` |
| 7 | `mnist-test-0` |
| 8 | `mnist-test-61` |
| 9 | `mnist-test-7` |

이미지는 공식 MNIST NPZ 테스트 데이터의 원래 순서를 기준으로 숫자별 첫 항목을 선택했다. 서버의 ID 해석은 [dataset.py](../server/app/dataset.py)의 `Dataset.index()`가 담당한다.
