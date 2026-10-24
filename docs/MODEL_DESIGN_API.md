# 모델 제작 씬과 편집 API

대상은 `client/Assets/Scenes/CNNModelDesignLab.unity`다. 슬롯의 블록 순서로 CNN을 구성하고 기기 내 학습 세션에 학습·추론을 요청한다. 조작은 [공통 조작법](xr-simulator.md), 연산·데이터·실행 제한은 [CNN 학습](CNN_TRAINING.md)을 참고한다.

## 파일과 책임

씬 스크립트는 `client/Assets/Scripts/CNNModelDesignLab/`, 네임스페이스는 `ModelDesignScene`이다. 학습 엔진은 `client/Assets/Scripts/Core/CNNTraining/`에 분리한다.

| 파일·클래스 | 책임 |
| --- | --- |
| `CNNModelDesignLabManager` | Workspace·Input·Output 준비, Controller 이벤트 연결, 배치 순서 갱신, 검증 후 학습 요청, 결과와 편집 모델 비교, 엔딩 진입 조율 |
| `ModelSupplyController` | 공급 블록 보충 및 꺼내기 |
| `ModelConveyorController` | 저장된 슬롯 앵커의 배치 판정·정렬과 점유 상태 표시 |
| `ModelBlockController` | 개별 블록의 잡기 상태·표시 및 레이어 연결 |
| `ModelXRInputController` | 씬의 XR 입력·슬롯 활성화 조건 |
| `ModelSettingsController` / `BlockSettingsPopup` | 슬롯 팝업 수명, 레이어 설정 입력과 변경 |
| `ModelExecutionController` | 설정·데이터 준비, 재사용하는 로컬 세션 소유, 실행·취소 요청, 메인 스레드 결과 전달, 종료된 실행 결과 차단 |
| `CnnTrainingSettings` | 데이터 TextAsset 참조와 학습·자원 한도. `CreateOptions()`와 `LoadDataset()` 제공 |
| `MnistDataset` | 바이너리 데이터 해석·정규화·학습/검증 분리 및 UI 샘플 ID 조회. `Data`는 엔진용 `TrainingData` |
| `ModelResultsController` | 저장된 구성 검사·학습·예측 페이지, 시작·중지 버튼, 실행 상태와 학습 당시 구성 여부 표시 |
| `ModelValidationController` | 구성 문제의 원인·수정 방법·실제 슬롯 위치와 이전/다음 문제 표시 |
| `ModelTrainingMetricsController` | 실제 학습·검증 지표의 epoch별 그래프와 최종 평가 표시 |
| `ModelInferenceResultsController` | 샘플 선택·예측 결과 표시. 요청 ID와 결과 ID 비교 |
| `ModelMetricGraph` | 전달받은 지표를 학습·검증 선 그래프로 시각화 |

Manager는 기능 사이 순서를 조율하고 Controller는 개별 기능을 담당한다. 계산 구현과 가중치는 씬에 노출하지 않는다. Validation·Metrics·InferenceResults는 프리팹에 저장된 MonoBehaviour이며 ResultsController가 연결한다. UI 계층과 배치는 런타임에 생성하지 않는다.

## 블록 배치와 설정

1. Manager가 Workspace와 고정 Input·Output을 준비하고 공급소·슬롯·이벤트를 연결한다.
2. 공급소에서 꺼낸 블록의 레이어를 Workspace에 추가한다. 공급소는 같은 종류의 전용 프리팹으로 기본 블록을 보충한다.
3. 슬롯의 블록을 잡으면 팝업을 닫고 슬롯을 비운 뒤 순서를 갱신한다.
4. 배치 미리보기는 벨트 로컬 좌표의 블록 중심과 슬롯별 `Placement Half Extents`를 비교한다.
5. 정상 배치 시 빈 슬롯에 등록하고 위치·방향을 정렬한다. 점유 슬롯·슬롯 밖·취소된 잡기는 배치하지 않는다.
6. 연결은 `Input → 슬롯 순서의 블록 → Output`으로 갱신한다. 빈 슬롯은 건너뛰며 다른 블록 위치를 옮기지 않는다.

점유 슬롯·슬롯 밖에 놓거나 잡기를 취소한 블록은 Workspace에서 제거하고 파괴한다. 미완성·잘못된 순서는 편집 상태로 보관할 수 있지만 학습 시작 전에 거절한다.

설정 요청은 Manager가 잡기 상태와 `CanActivateSlot()`로 확인한다. 팝업은 해당 슬롯의 `Popup Anchor`에 하나만 열며 시점을 따라가지 않는다. 값 변경은 `LayerDefinition.Set*()`을 통해 재검증·표시 갱신으로 이어진다. 삭제는 슬롯 제거, 레이어 제거, 이벤트 해제, 블록 파괴 순이다. Input·Output은 삭제할 수 없다.

## 구성 검사와 결과 콘솔

구성 검사·학습·숫자 예측 페이지는 `ModelResults.prefab`에 저장한다. Workspace 변경은 Manager가 모아 `LateUpdate()`에서 실행 정의와 자원 한도를 검사하고, 문제 안내와 학습 당시 모델의 변경 여부를 함께 갱신한다. 연결 해제·재연결 중간 상태는 표시하지 않는다.

학습 시작 시 즉시 다시 검사한다. 문제가 있으면 학습을 요청하지 않고 구성 검사 페이지에 초점을 맞춘다. 시작 버튼은 `구성 문제 확인하기`로 표시되며, 작업 중이나 블록을 잡는 동안에는 시작할 수 없다. 문제 위치는 모델 내부 레이어 참조를 실제 슬롯과 비교해 표시한다. 엔진의 모델 전체 자원 오류는 임의의 슬롯을 지목하지 않고 원문과 일반 수정 안내를 표시한다.

빈 모델도 로컬 CNN 실행 규칙에 따라 Conv2D가 필요하다는 문제로 표시한다. 순서·자원 검사 통과는 학습 성공이나 정확도를 보장하지 않는다. 실행 오류는 별도 스크롤 패널에 표시한다.

## 로컬 학습·추론

`ModelExecutionDefinitionBuilder.TryBuild()`가 Input부터 Output까지 연결된 블록을 값으로 복사하고 자원 한도를 검사한다. Conv2D로 시작하고 Dense 앞에는 명시적 Flatten이 필요하다. 고정 Output은 Dense 10·Softmax다. JSON 직렬화나 통신 요청은 없다.

`ModelExecutionController`는 설정 에셋으로 데이터를 준비하고 `CnnTrainingSession`을 재사용한다. 엔진은 하나의 스레드풀 작업에서 학습·검증·평가를 수행한다. 동시에 두 작업을 실행하지 않으며 씬은 진행·완료·취소·오류만 받아 UI를 갱신한다. 최종 평가 동안에도 `Training` 상태를 유지하고 실제 평가가 끝난 뒤 완료된다.

진행 지표는 epoch별 실제 학습·검증 loss/accuracy이며 완료 결과에는 별도 test 부분집합 평가가 있다. 취소·오류·평가 미완료를 성공이나 공장 엔딩 조건 충족으로 표시하지 않는다.

추론 UI는 텍스처 이름 `mnist-test-N`을 Controller에 전달한다. `MnistDataset.TryGetTestImage()`가 같은 원본 인덱스의 픽셀을 찾아 세션에 넘긴다. 엔진은 이미지 이름 규칙을 해석하지 않는다. 결과 ID가 현재 요청과 같은지 확인한 뒤 예측 숫자와 10개 Softmax 출력을 표시한다.

학습 중·후 편집은 가능하다. `SubmittedModel`과 현재 실행 정의의 값을 비교해 이전 구성으로 학습한 결과임을 표시한다. 새 학습과 씬 종료 시 모델 수명은 세션이 관리한다.

씬 종료 시 작업 취소, 결과 초기화, 팝업 닫기, 잡기 취소와 입력 해제를 수행한다. Controller는 종료된 세션·작업의 늦은 결과를 무시한다. 계산 종료 전 가중치·버퍼를 다른 작업에 재사용하지 않는다. 재진입 시 다시 학습한다.

## 씬·프리팹 연결

| 에셋·필드 | 연결 |
| --- | --- |
| `Prefabs/XR/XRPlayerRig.prefab` | 공용 입력 리그. `PersistentXRPlayerRig`가 추가 로드 시 중복 제거 |
| `Prefabs/CNNModelDesignLab/ModelBlock.prefab` 및 종류별 블록 프리팹 | Conv·Pooling·Flatten·Dense와 고정 Input·Output의 외형, Controller, Collider, 잡기 설정 |
| `PartsSupplyRack.prefab` | 공급소·기본 블록 |
| `ModelConveyor.prefab` | 슬롯·설정 조작기기·Input/Output |
| `SlotSettingsControl.prefab` | 설정 Controller와 `Popup Anchor` |
| `ModelResults.prefab` | 구성 검사·학습·예측 UI와 Controller, 시작·중지 버튼, `Samples` 참조 |
| 씬 `ModelExecutionController._trainingSettings` | `Settings/CNNModelDesignLab/LocalTrainingSettings.asset` |

벨트의 `Block Anchors`에 저장된 슬롯 위치·방향을 사용한다. `Slot Visuals Root`와 `Settings Controls Root`의 자식 순서는 앵커 순서와 일치해야 한다. 각 앵커의 로컬 좌표와 `Placement Half Extents`로 배치를 판정하며 실행 중 슬롯 수 변경은 지원하지 않는다.

공용 오른손 입력은 `Scripts/Core/XR/XRPlayerInputController.cs`가 담당하고 모델 씬은 레이 적중 여부를 검사한다. 서버 주소 설정은 필요하지 않다.

## 순수 편집 모델 API

`ModelDesign` 어셈블리는 Unity API를 참조하지 않으며 `CNNTraining`의 공개 정의만 참조한다. 엔진에서 편집 모델로의 역참조는 없다.

| 타입 | 역할 |
| --- | --- |
| `LayerDefinition` | 레이어 종류·설정값. 실제 변경 시 `OnChanged` 발생 |
| `ModelDefinition` | 연결된 레이어 덩어리의 읽기 전용 목록 |
| `ModelWorkspace` | Input·Output·덩어리 소유, 연결·삭제·교체, 재검증·변경 이벤트 |
| `ModelValidator` | 순서·출력 형태·Flatten 전환 및 Input–Output 연결 검사 |
| `ValidationIssue` | 문제 모델·블록 경로·메시지 |
| `ModelExecutionDefinitionBuilder` | 불변 실행 정의로 변환, 자원 한도 검증, 정의 값 비교 |

| 호출·속성 | 동작 |
| --- | --- |
| `workspace.AddLayer(layer)` | 단독 덩어리 추가 |
| `workspace.AddAndConnect(existingLayer, newLayer)` | 빈 끝점에 새 블록 추가·연결 |
| `workspace.Connect(first, second)` | 다른 덩어리의 빈 끝점 연결. 인자 순서 무관 |
| `workspace.Disconnect(first, second)` | 인접 연결 해제 |
| `workspace.RemoveLayer(layer)` | 삭제. 중간 블록 양쪽은 별도 덩어리가 됨 |
| `workspace.ReplaceLayer(oldLayer, newLayer)` | 연결 위치 유지하며 교체 |
| `workspace.GetModel(layer)` | 블록이 현재 속한 덩어리 |
| `workspace.InputModel` / `workspace.Models` | Input 덩어리 / 전체 덩어리 |
| `workspace.ValidationIssues` | 최신 편집 검증 결과 |
| `layer.SetFilters()` / `SetKernelSize()` / `SetPoolSize()` / `SetUnits()` / `SetActivation()` | 레이어별 설정 변경 |
| `ModelValidator.ValidateEditing(model)` | 미완성 덩어리 검사. 모르는 입력 형태는 판정 보류 |
| `ModelValidator.ValidateForExecution(workspace, options)` | 연결된 실행 모델과 자원 한도 검사 |
| `ModelExecutionDefinitionBuilder.TryBuild(workspace, options, out definition, out issues)` | 실행 정의 생성. 실패 시 오류 목록 |
| `ModelExecutionDefinitionBuilder.Build(workspace, options)` | 정의 반환. 실패 시 예외 |
| `ModelExecutionDefinitionBuilder.Matches(first, second)` | 입력·레이어 순서·설정값 비교 |

Input·Output은 Workspace당 하나다. 분기·순환은 허용하지 않는다. 연결·분리·삭제·교체는 덩어리 목록을 갱신하므로 UI는 블록 참조를 유지하고 `GetModel()`로 현재 덩어리를 조회한다. 실제 변경 시 재검증 후 `OnValidationChanged`, `OnChanged` 순서로 이벤트가 발생한다. 동일 값 설정과 거절된 작업은 이벤트를 발생시키지 않는다. Workspace 사용 종료 시 `Dispose()`한다.

```csharp
using CNNTraining;
using ModelDesign;

using (ModelWorkspace workspace = new ModelWorkspace())
{
    LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
    LayerDefinition pool = LayerDefinition.MaxPooling2D(2, 2);
    LayerDefinition flatten = LayerDefinition.Flatten();
    workspace.AddAndConnect(workspace.Input, conv);
    workspace.AddAndConnect(conv, pool);
    workspace.AddAndConnect(pool, flatten);
    workspace.Connect(flatten, workspace.Output);
    ModelExecutionDefinition definition = ModelExecutionDefinitionBuilder.Build(
        workspace, new CnnTrainingOptions());
    // 실행 Controller에 definition을 전달한다. 편집 객체와 가중치는 공유하지 않는다.
}
```

## 추론 샘플 ID

`Textures/CNNModelDesignLab/`의 텍스처·정답을 `ModelResultsController.Samples`에 연결한다. ID는 확장자를 제외한 이름이다.

| 정답 | 이미지 이름 |
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

원본 MNIST test split에서 숫자별 첫 이미지다. 새 샘플의 원본 인덱스는 내장 test 데이터에 있어야 한다. 없는 ID는 오류로 처리하며 다른 이미지로 대체하지 않는다.
