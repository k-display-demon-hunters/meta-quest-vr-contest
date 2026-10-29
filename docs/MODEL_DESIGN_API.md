# 모델 제작 씬과 편집 API

[CNNModelDesignLab.unity](../client/Assets/Scenes/CNNModelDesignLab.unity)는 8개 슬롯에 블록을 배치해 모델을 구성하고, 내장 MNIST로 학습·평가·추론한다. 조작은 [공통 조작법](xr-simulator.md), 수학·데이터 형식·학습 설정은 [기기 내 CNN 학습](CNN_TRAINING.md)을 참고한다. 현재 씬은 Python 서버나 JSON·WebSocket 프로토콜을 사용하지 않는다.

## 모듈과 책임

런타임 루트는 `client/Assets/Scripts/CNNModelDesignLab/`다. `CNNModelDesignLabManager`는 편집 모델과 체험 완료 상태를 소유하고, 아래 다섯 모듈의 진입점과 공용 입력 어댑터 `ModelXRInputController`를 연결한다.

| 폴더·진입점 | 내부 담당과 책임 |
| --- | --- |
| `ModelBuilder/ModelBuilderManager` | `ModelBuilderController`가 공급소·고정 Input/Output·컨베이어·블록을 관리한다. `ConveyorSlots`는 실제 블록의 슬롯 점유를, `ModelDefinition`은 같은 슬롯의 레이어 데이터를 보관한다. 설정 요청은 레이어와 팝업 위치를 이벤트로 전달한다. |
| `BlockSettings/ModelBlockSettingsManager` | `ModelSettingsController`가 팝업을 열고 닫으며 설정값을 갱신한다. `BlockSettingsCatalog`·`BlockSettingsDefinition`은 블록 종류별 항목을 제공하고, `BlockSettingsPopup`·`LayerNumberSetting`이 입력을 처리한다. 삭제는 `OnDeleteRequested`로 요청한다. |
| `Execution/ModelExecutionManager` | Dataset에서 실행 데이터를 받고 `ModelExecutionDefinitionBuilder`·`ModelExecutionValidator`로 실행 요청을 준비한다. `ModelExecutionController`는 실행 상태와 메인 스레드 결과 전달을, 하위 `Engine/CnnEngineManager`는 학습 세션을 관리한다. |
| `ControlPanel/ModelControlPanelManager` | `ModelControlPanelController`가 구성 검사·학습·예측 페이지와 버튼을 연결한다. `ModelValidationController`, `ModelTrainingMetricsController`, `ModelInferenceResultsController`가 각 표시를 담당하고 `ModelMetricGraph`가 지표 그래프를 그린다. |
| `Dataset/ModelDatasetManager` | `ModelDatasetController`가 MNIST 바이너리와 샘플 텍스처를 준비한다. `MnistDataset`은 파일 해석·정규화·split 분리를, `DatasetSample`은 화면에 필요한 ID·텍스처·정답을 담당한다. |
| `Model/ModelDefinition` | 고정 Input·Output과 편집 슬롯 8개의 레이어를 소유한다. `LayerDefinition`은 레이어 설정과 변경 이벤트, `LayerLimits`는 입력 크기와 개별 설정 범위를 정의한다. |

의존 방향은 `씬 Manager → 모듈 Manager → 내부 Controller/데이터`다. Execution 아래의 엔진은 Unity API·씬·블록·MNIST 이미지 이름을 알지 않는다. 현재 네임스페이스는 `ModelDesignScene`과 각 모듈 하위 이름이다. `Model/ModelDesign.asmdef`와 `Execution/Engine/CNNTraining.asmdef`는 각각 Unity 참조가 없는 별도 어셈블리이며, 편집 Model 어셈블리는 엔진 어셈블리도 참조하지 않는다. 두 표현의 변환은 Execution 모듈이 맡는다.

## 초기화와 이벤트 연결

1. 씬 Manager의 `Awake()`가 `ModelDefinition`을 만든다. `OnEnable()`은 XR 입력을 켜고 재활성화 시 블록 상호작용을 재개한다.
2. `Start()`가 Model·Builder·Settings·Execution·ControlPanel의 이벤트를 연결한다. 각 컴포넌트의 `Awake()`가 끝난 뒤 Dataset → Execution → ControlPanel → Builder 순서로 초기화한다.
3. Builder 초기화에는 편집 모델과 XR 적중 조회 함수를 전달한다. Execution과 ControlPanel에는 Dataset 진입점을 전달하여 실행 데이터와 화면 샘플을 각각 준비한다.
4. 변경이 발생하면 `_modelUIIsDirty`를 설정한다. `LateUpdate()`는 필요한 때 블록 라벨·팝업·구성 검사 표시와 학습 당시 구성 여부를 갱신한다. 매 프레임 모델 구조를 재검증하지 않는다.

| 상향 이벤트 | 씬 Manager의 처리 |
| --- | --- |
| Model `OnChanged`, Builder `OnChanged` | 기존 검증 결과를 비우고 UI 갱신을 예약한다. Builder 변경은 열린 설정 팝업도 닫는다. |
| Builder `OnSettingsRequested(layer, pose)` | Settings 진입점에 해당 레이어의 팝업 열기를 요청한다. |
| Settings `OnDeleteRequested(layer)` | 팝업을 닫고 Builder에 삭제를 요청한다. |
| ControlPanel `OnStartRequested` / `OnStopRequested` / `OnInferenceRequested` | 활성 상태·실행 상태·잡기 상태를 확인한 뒤 Execution 진입점에 요청한다. |
| Execution `OnValidationFailed` | 문제 목록과 슬롯 대응을 표시하고 구성 검사 페이지에 초점을 맞춘다. |
| Execution 상태·진행·완료·취소·오류 이벤트 | ControlPanel 진입점으로 화면을 갱신하고 학습 완료 상태를 관리한다. |

현재 씬 Manager의 이벤트 연결 수명은 `Start()`부터 `OnDestroy()`까지다. `OnDisable()`에서는 학습 세션·상호작용·팝업·입력을 종료하고 UI와 완료 상태를 초기화한다. 활성 기간에만 구독하는 형태로 구현되어 있다고 해석하지 않는다.

## 슬롯 기반 편집

`ModelDefinition.Layers`는 빈 슬롯을 null로 보존하는 읽기 전용 8칸 목록이다. `PlaceLayer(slot, layer)`와 `RemoveLayer(slot)`가 슬롯을 변경하며, 배치된 레이어의 설정 변경도 모델의 `OnChanged`로 전달된다. Input·Output은 편집 슬롯에 넣지 않는다. 모델 수명이 끝나면 `Dispose()`로 레이어 구독을 해제한다.

1. 공급소에서 블록을 잡으면 같은 종류의 프리팹을 보충한다. 잡은 블록은 공급 블록 상태에서 벗어난다.
2. 기존 슬롯의 블록을 잡으면 슬롯 점유와 모델의 해당 레이어를 제거한다. 나머지 슬롯은 이동하지 않는다.
3. 놓을 때 컨베이어 로컬 좌표와 슬롯의 `Placement Half Extents`로 위치를 판단한다. 유효한 빈 슬롯이면 점유·모델 데이터를 등록하고 블록을 앵커에 정렬한다.
4. 슬롯 밖·점유 슬롯에 놓거나 잡기가 취소되면 블록을 등록 해제하고 파괴한다. 상호작용 종료 시 아직 손에 들린 임시 블록도 정리한다.
5. 실행 정의를 만들 때 빈 슬롯을 건너뛰고 슬롯 순서대로 레이어를 복사한 뒤 고정 Output(Dense 10·Softmax)을 붙인다.

설정 입력은 Builder 내부에서 잡기 여부와 오른손 레이 적중을 확인한다. 선택한 슬롯의 `Popup Anchor` 위치를 Settings 모듈에 전달하며, 팝업은 하나만 열린다. 값 변경은 `LayerDefinition.SetFilters/SetKernelSize/SetPoolSize/SetUnits/SetActivation`을 사용한다. 삭제 요청은 씬 Manager를 거쳐 Builder에 돌아오므로 Settings가 실제 블록이나 컨베이어를 직접 조작하지 않는다.

## 실행 검증과 학습

시작 버튼은 씬이 활성 상태이고 실행 중이 아니며 블록을 잡고 있지 않을 때 Execution에 요청한다. Execution은 Dataset 준비 오류를 먼저 처리하고, 현재 모델의 값 스냅샷과 원래 슬롯 대응표를 만든 뒤 구조를 검사한다. 첫 오류가 있으면 엔진을 실행하지 않고 `ValidationIssue.SlotIndex`와 메시지를 전달한다. 모델 전체 오류에는 임의의 슬롯을 지정하지 않는다.

현재 검사는 Flatten 이전의 공간 레이어, 명시적 Flatten 한 번, Flatten 이후 Dense, 현재 입력 크기에 맞는 커널·풀 크기, 최종 Dense 10·Softmax를 확인한다. 첫 레이어를 Conv2D로 강제하지 않으므로 `Flatten → Output`도 구조상 허용한다. Dense 앞에 Flatten을 자동으로 추가하지 않는다. 빈 모델은 Flatten 누락으로 거절한다. 개별 값 범위는 `LayerLimits`가 관리하고, 모델 전체 연산량·파라미터 수의 별도 상한은 없다.

`ModelExecutionManager`는 같은 GameObject의 Controller를 `Awake()`에서 연결하고, 최초 유효한 학습 요청 때 `CnnEngineManager`를 만든다. 엔진 Manager가 재사용하는 `CnnTrainingSession`을 소유한다. 학습 설정은 `Execution/CnnTrainingSettings.cs`의 코드 상수이며 현재 10 epoch, batch 16, learning rate 0.001, seed 42다.

작업은 스레드풀에서 실행한다. Controller의 큐가 진행·결과를 메인 스레드 `Update()`로 전달하며 세션/작업 번호로 늦은 응답을 차단한다. 학습·검증·최종 평가가 끝나야 완료이고, 취소·실패는 완료로 처리하지 않는다. 세션은 한 번에 하나의 학습 또는 추론만 허용한다. 새 학습은 새 가중치와 optimizer 상태로 시작한다.

`SubmittedModel`과 현재 모델은 실행 정의의 값으로 비교한다. 학습 중·후 편집은 가능하지만 제출 당시 스냅샷은 바뀌지 않으며, 현재 구성이 다르면 콘솔에 알린다. 계산 구현이나 가중치는 화면 코드에 노출하지 않는다.

## 추론과 공장 완료 조건

Dataset 모듈이 텍스처 이름 `mnist-test-N`을 내장 test 데이터의 원본 인덱스와 연결한다. ControlPanel은 `DatasetSample` 목록으로 샘플을 표시하고 선택한 ID만 추론 요청으로 전달한다. Execution이 해당 픽셀을 조회해 엔진에 전달하며 엔진은 ID를 결과 식별값으로만 사용한다. 화면은 요청한 ID와 결과 ID를 비교하여 예측 숫자와 10개 출력을 표시한다.

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

공장 흐름은 `ReadyForFactoryExit`를 읽는다. 이 값은 `_trainingCompleted && !_execution.IsBusy`이며, 학습 완료 결과를 화면에 전달한 뒤 완료 상태를 설정한다. 새 학습의 `Training` 상태와 씬 비활성화 시 초기화한다. 현재 씬에는 단계 건너뛰기로 출구를 허용하는 경로가 없다. 추론 완료나 특정 정확도 달성은 출구 조건이 아니다.

## 씬·프리팹 연결

| 연결 대상 | 현재 구성 |
| --- | --- |
| 씬 Manager | `_builder`, `_settings`, `_execution`, `_controlPanel`, `_dataset`, `_xrInput` |
| `ModelBuilderController` | 고정 Input·Output 블록, 공급소, 컨베이어 |
| `ModelConveyor.prefab` | 8개 블록 앵커, 슬롯 표시와 설정 기기. 자식 순서는 슬롯 순서와 대응한다. |
| `PartsSupplyRack.prefab` 및 블록 프리팹 | 공급용 Conv·Pooling·Flatten·Dense와 고정 Input·Output 외형·Collider·잡기 구성 |
| `SlotSettingsControl.prefab` / `BlockSettingsPopup.prefab` | 슬롯 입력·팝업 기준점과 레이어 설정 UI |
| `ModelControlPanel.prefab` | 구성 검사·학습·예측 화면, 시작·중지 버튼과 하위 표시 Controller. 배치는 프리팹에 저장한다. |
| `ModelDatasetController` | `_trainingDataset`, `_testDataset`, `_sampleImages`로 바이너리 에셋과 샘플 텍스처 연결 |
| `Prefabs/XR/XRPlayerRig.prefab` | 공용 입력 리그. 모델 씬은 `ModelXRInputController`를 통해 레이 적중을 조회한다. |

## 확인 범위

이 문서는 현재 소스·씬·프리팹의 참조와 동작 경로를 읽어 갱신했다. 이번 문서 갱신에서는 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다. 이전 실행 측정은 [CNN 학습 문서의 과거 검증 기록](CNN_TRAINING.md#과거-검증-기록)에 별도로 남기며 현재 코드의 검증 결과로 간주하지 않는다.
