# 모델 제작 씬과 편집 API

[CNNModelDesignLab.unity](../client/Assets/Scenes/CNNModelDesignLab.unity)는 8개 슬롯에 블록을 배치해 모델을 구성하고, 내장 MNIST로 학습·평가·추론한다. 조작은 [공통 조작법](xr-simulator.md), 수학·데이터 형식·학습 설정은 [기기 내 CNN 학습](CNN_TRAINING.md)을 참고한다. 현재 씬은 Python 서버나 JSON·WebSocket 프로토콜을 사용하지 않는다.

## 모듈과 책임

런타임 루트는 `client/Assets/Scripts/CNNModelDesignLab/`다. `CNNModelDesignLabManager`는 편집 모델과 체험 완료 상태를 소유하고, 아래 다섯 모듈의 진입점을 연결한다.

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

1. 씬 Manager의 `Awake()`가 `ModelDefinition`을 만든다. `OnEnable()`은 UI 갱신을 예약하고 초기화 이후 재활성화 시 블록 상호작용을 재개한다.
2. `Start()`가 Model·Builder·Settings·Execution·ControlPanel의 이벤트를 연결한다. 각 컴포넌트의 `Awake()`가 끝난 뒤 Dataset → Execution → ControlPanel → Builder 순서로 초기화한다.
3. Builder 초기화에는 편집 모델을 전달한다. Execution과 ControlPanel에는 Dataset 진입점을 전달하여 실행 데이터와 화면 샘플을 각각 준비한다.
4. 변경이 발생하면 `_modelUIIsDirty`를 설정한다. `LateUpdate()`는 필요한 때 블록 라벨·팝업·구성 검사 표시와 학습 당시 구성 여부를 갱신한다. 매 프레임 모델 구조를 재검증하지 않는다.

EventSystem과 XRUIInputModule은 공용 `XRPlayerRig.prefab`의 자식으로 유지된다. 모델 씬은 UI 입력 환경을 생성·삭제하지 않는다. 제어 패널·팝업의 버튼은 기존 Button.onClick 연결로 요청을 받는다.

3D 슬롯 설정 기기는 `SlotSettingsController`가 `XRSimpleInteractable.activated`를 직접 받는다. 활성 상태의 점유된 슬롯에서 오른손 `XRRayInteractor`가 보낸 입력만 허용하고, ray가 물체를 잡고 있거나 UI가 가장 가까운 적중 대상이면 차단한다. 입력 조건을 통과하면 슬롯의 로컬 이벤트를 Builder 내부로 전달한다. Builder는 상호작용 활성 상태와 블록 이동 여부를 확인한 뒤 레이어와 팝업 위치를 씬 Manager에 알린다. 씬 Manager는 Settings 모듈에 팝업 열기를 요청하며, 별도 입력 Controller나 적중 조회 콜백을 거치지 않는다.

| 상향 이벤트 | 씬 Manager의 처리 |
| --- | --- |
| Model `OnChanged`, Builder `OnChanged` | 기존 검증 결과를 비우고 UI 갱신을 예약한다. Builder 변경은 열린 설정 팝업도 닫는다. |
| Builder `OnSettingsRequested(layer, pose)` | Settings 진입점에 해당 레이어의 팝업 열기를 요청한다. |
| Settings `OnDeleteRequested(layer)` | 팝업을 닫고 Builder에 삭제를 요청한다. |
| ControlPanel `OnStartRequested` / `OnStopRequested` / `OnInferenceRequested` | 활성 상태·실행 상태·잡기 상태를 확인한 뒤 Execution 진입점에 요청한다. |
| Execution `OnValidationFailed` | 문제 목록과 슬롯 대응을 표시하고 구성 검사 페이지에 초점을 맞춘다. |
| Execution 상태·진행·완료·취소·오류 이벤트 | ControlPanel 진입점으로 화면을 갱신하고 학습 완료 상태를 관리한다. |

현재 씬 Manager의 이벤트 연결 수명은 `Start()`부터 `OnDestroy()`까지다. `OnDisable()`에서는 학습 세션·상호작용·팝업을 종료하고 UI와 완료 상태를 초기화한다. 활성 기간에만 구독하는 형태로 구현되어 있다고 해석하지 않는다.

## 슬롯 기반 편집

`ModelDefinition.Layers`는 빈 슬롯을 null로 보존하는 읽기 전용 8칸 목록이다. `PlaceLayer(slot, layer)`와 `RemoveLayer(slot)`가 슬롯을 변경하며, 배치된 레이어의 설정 변경도 모델의 `OnChanged`로 전달된다. Input·Output은 편집 슬롯에 넣지 않는다. 모델 수명이 끝나면 `Dispose()`로 레이어 구독을 해제한다.

1. 공급소에서 블록을 잡으면 같은 종류의 프리팹을 보충한다. 잡은 블록은 공급 블록 상태에서 벗어난다.
2. 기존 슬롯의 블록을 잡으면 슬롯 점유와 모델의 해당 레이어를 제거한다. 나머지 슬롯은 이동하지 않는다.
3. 놓을 때 슬롯 앵커의 로컬 좌표와 `Placement Half Extents`로 위치를 판단한다. 유효한 빈 슬롯이면 점유·모델 데이터를 등록하고 블록을 앵커에 정렬한다.
4. 슬롯 밖·점유 슬롯에 놓거나 잡기가 취소되면 블록을 등록 해제하고 파괴한다. 상호작용 종료 시 아직 손에 들린 임시 블록도 정리한다.
5. 실행 정의를 만들 때 빈 슬롯을 건너뛰고 슬롯 순서대로 레이어를 복사한 뒤 고정 Output(Dense 4·Softmax)을 붙인다.

설정 입력은 Builder 내부에서 잡기 여부와 오른손 레이 적중을 확인한다. 선택한 슬롯의 `Popup Anchor` 위치를 Settings 모듈에 전달하며, 팝업은 하나만 열린다. 값 변경은 `LayerDefinition.SetFilters/SetKernelSize/SetPoolSize/SetUnits/SetActivation`을 사용한다. 삭제 요청은 씬 Manager를 거쳐 Builder에 돌아오므로 Settings가 실제 블록이나 컨베이어를 직접 조작하지 않는다.

### 단계별 출력과 연결

Input → Conv → Pooling → Flatten → Dense → Output을 하나의 흐름으로 표시한다. 각 블록은 입력을 중복해서 그리지 않고 해당 단계의 결과 묶음 하나만 갖는다. Conv의 구체는 커널 가중치가 아니라 출력 특징 맵 원소다. Input은 28×28×3, Conv는 valid·stride 1 출력, Pooling은 valid·stride=풀 크기의 출력을 표시한다. Input은 R·G·B 세 채널의 28×28 격자를 모두 표시한다. 뒤쪽 두 평면은 각각 X=0.04/0.08, Z=0.12/0.24로 엇갈려 놓고 하나의 메시를 공유한다. Conv·Pooling 공간 출력은 대표 채널 하나의 모든 H×W 구체를 그리며 전체 채널 수와 `1 shown`을 명시한다. 구체 간격은 0.025로 동일하므로 Pooling 후 격자의 크기가 줄어든다. 색상은 구조 구분용이며 실제 추론값·가중치·최댓값 강조는 없다.

Flatten은 H×W×C 길이의 벡터 하나, Dense는 설정 유닛 수의 벡터 하나, 고정 Output은 Softmax 4개 클래스를 표시한다. 벡터는 8개 이하면 전부, 초과하면 앞뒤 4개를 상징하는 구체와 가운데 사각 점 세 개를 표시한다. 전체 원소 수와 표시·생략 수를 함께 적으며 개별 조회·페이지 이동은 없다. 유효한 공간 입력이나 명시적인 Flatten 이후의 벡터 입력이 없는 편집 블록은 빈 결과 프레임과 안내를 표시한다. 고정 Output은 설정된 4개 클래스를 계속 표시한다.

`ModelBuilderController.RefreshLabels()`가 빈 슬롯을 건너뛰며 H/W/C와 벡터 길이를 전달한다. 모델 변경 시 기존 dirty 갱신 경로로 뒤쪽 표시까지 갱신한다. Conv(4, 3×3) → Pool(2×2) → Flatten → Dense(32)의 출력은 26×26×4 → 13×13×4 → 676 → 32다. 엔진 결과나 Execution 내부 객체에 대한 의존성은 없다.

`ModelBlockController`가 라벨·출력 캡션·메시·Collider를 갱신하고 `VectorBlockMesh`가 공간 격자와 벡터의 기하를 구성한다. 크기가 바뀔 때만 메시를 다시 만들고 이전 메시를 해제한다. 구체별 GameObject는 만들지 않는다. 모든 블록의 기본 메시·라벨·필수 참조를 기존 에셋과 프리팹에 저장하며 GUID와 기존 재질을 유지한다. 공급 예시는 Conv 26×26×4, Pool 4×4×3, Flatten 48, Dense 32이며 `Example`을 표시한다. 배치 시 현재 모델 크기로 전환하고, 잡은 블록은 마지막 크기를 유지한다.

Builder가 인접한 배치 블록의 연결 지점 쌍을 모아 Conveyor에 전달한다. Conveyor는 저장된 Flow MeshFilter에 Input부터 Output까지 화살표 메시를 갱신하며 빈 슬롯을 건너뛴다. 화살표는 단계 사이의 데이터 흐름이고 개별 가중치 연결을 뜻하지 않는다.

### 바닥 조립 슬롯과 공급 책상

조립 행은 씬 루트 기준 Y=0, Z=0.62에 두고 0.85 간격으로 8개 슬롯(X=-2.975…2.975)을 배치한다. 고정 Input과 Output은 X=-3.85와 3.85, Y=0.138, Z=0.62에 둔다. 슬롯은 폭 0.84, 깊이 0.82, 높이 0.03인 낮은 판으로 표시한다. 판 앞쪽 윗면에는 슬롯 번호와 빈 상태 또는 설정 가능 상태를 표시하고 블록은 뒤쪽에 안착한다. 판 앞의 설정 기기는 점유된 슬롯에서 오른손 Trigger로 팝업을 연다.

공급 책상은 조립 행과 동쪽 벽 사이에 두고 방 중앙을 향한다. 씬 루트 기준 위치는 (-0.333333, 0.9696, 2.05)이고 공급대 그룹의 균일 배율은 0.8이다. 흰색 둥근 상판 아래에 얇은 테두리와 바깥으로 벌어지는 다리 네 개를 둔다. 책상은 그룹 안에서 긴 축 배율 1.7을 사용한다. Conv·Pooling·Flatten·Dense 공급 블록과 보충 블록은 이 부모 아래 X=-1.6575, -0.5525, 0.5525, 1.6575, Y=Z=0에서 네 패드에 정렬된다. 블록을 슬롯에 놓으면 고정 Input·Output과 같은 루트 기준 배율 1로 정렬된다.

제어 패널은 조립 행 중앙과 나란한 씬 루트 기준 (0, 2.4, 3.7)에 두고 ROOM #3 동쪽 벽을 등진다. 패널 하단은 공급 블록의 제목 위에 위치한다. 모델 검사 페이지의 첫 안내는 공급 책상에서 Grip으로 블록을 잡고 빈 슬롯에 놓는 두 단계다. 구성 오류가 있으면 위치·원인·수정 안내를 표시하며 여러 오류는 이전·다음 버튼으로 확인한다. 콘솔은 ROOM #1-2의 짙은 패널과 호박색 강조선, 청회색 버튼을 사용한다.

## 실행 검증과 학습

시작 버튼은 씬이 활성 상태이고 실행 중이 아니며 블록을 잡고 있지 않을 때 Execution에 요청한다. Execution은 Dataset 준비 오류를 먼저 처리하고, 현재 모델의 값 스냅샷과 원래 슬롯 대응표를 만든 뒤 구조를 검사한다. 첫 오류가 있으면 엔진을 실행하지 않고 `ValidationIssue.SlotIndex`와 메시지를 전달한다. 모델 전체 오류에는 임의의 슬롯을 지정하지 않는다.

현재 검사는 Flatten 이전의 공간 레이어, 명시적 Flatten 한 번, Flatten 이후 Dense, 현재 입력 크기에 맞는 커널·풀 크기, 최종 Dense 4·Softmax를 확인한다. 첫 레이어를 Conv2D로 강제하지 않으므로 `Flatten → Output`도 구조상 허용한다. Dense 앞에 Flatten을 자동으로 추가하지 않는다. 빈 모델은 Flatten 누락으로 거절한다. 개별 값 범위는 `LayerLimits`가 관리하고, 모델 전체 연산량·파라미터 수의 별도 상한은 없다.

`ModelExecutionManager`는 같은 GameObject의 Controller를 `Awake()`에서 연결하고, 최초 유효한 학습 요청 때 `CnnEngineManager`를 만든다. 엔진 Manager가 재사용하는 `CnnTrainingSession`을 소유한다. 학습 설정은 `Execution/CnnTrainingSettings.cs`의 코드 상수이며 현재 10 epoch, batch 16, learning rate 0.001, seed 42다.

작업은 스레드풀에서 실행한다. Controller의 큐가 진행·결과를 메인 스레드 `Update()`로 전달하며 세션/작업 번호로 늦은 응답을 차단한다. 학습·검증·최종 평가가 끝나야 완료이고, 취소·실패는 완료로 처리하지 않는다. 세션은 한 번에 하나의 학습 또는 추론만 허용한다. 새 학습은 새 가중치와 optimizer 상태로 시작한다.

`SubmittedModel`과 현재 모델은 실행 정의의 값으로 비교한다. 학습 중·후 편집은 가능하지만 제출 당시 스냅샷은 바뀌지 않으며, 현재 구성이 다르면 콘솔에 알린다. 계산 구현이나 가중치는 화면 코드에 노출하지 않는다.

## 추론과 공장 완료 조건

Dataset 모듈은 RGB CMNT v2의 평가 데이터 120장으로 미리보기 텍스처와 `DatasetSample` 목록을 만든다. `mnist-color-test-N`의 N은 원본 MNIST training 인덱스이며 PNG 이름에 의존하지 않는다. ControlPanel은 기존 10개 샘플 버튼과 페이지 이동으로 120장을 표시하고 선택한 ID를 추론 요청으로 전달한다. Execution이 같은 레코드의 정규화된 RGB 픽셀을 엔진에 전달한다. 화면은 요청·결과 ID를 비교하고 숫자 0~3의 확률 네 개를 표시한다. 씬 인스턴스에서 나머지 확률 행은 비활성화한다.

입력은 28×28×3, 출력은 Dense 4·Softmax다. Conv 시각화는 출력 특징 맵의 대표 채널 하나를 표시하고 필터 수를 출력 채널 수로 표기한다.

공장 흐름은 `ReadyForFactoryExit`를 읽는다. 이 값은 `_trainingCompleted && !_execution.IsBusy`이며, 학습 완료 결과를 화면에 전달한 뒤 완료 상태를 설정한다. 새 학습의 `Training` 상태와 씬 비활성화 시 초기화한다. 현재 씬에는 단계 건너뛰기로 출구를 허용하는 경로가 없다. 추론 완료나 특정 정확도 달성은 출구 조건이 아니다.

## 씬·프리팹 연결

| 연결 대상 | 현재 구성 |
| --- | --- |
| 씬 Manager | `_builder`, `_settings`, `_execution`, `_controlPanel`, `_dataset` |
| `ModelBuilderController` | 고정 Input·Output 블록, 공급소, 컨베이어 |
| `ModelConveyor.prefab` | 8개 블록 앵커, 슬롯 표시와 설정 기기. 자식 순서는 슬롯 순서와 대응한다. |
| `PartsSupplyRack.prefab` 및 블록 프리팹 | 공급용 Conv·Pooling·Flatten·Dense와 고정 Input·Output 외형·Collider·잡기 구성 |
| `SlotSettingsControl.prefab` / `BlockSettingsPopup.prefab` | 슬롯 입력·팝업 기준점과 레이어 설정 UI |
| `ModelControlPanel.prefab` | 구성 검사·학습·예측 화면, 시작·중지 버튼과 하위 표시 Controller. 화면 요소는 프리팹에, 패널 기준점은 씬에 저장한다. |
| `ModelDatasetController` | `_trainingDataset`, `_testDataset`으로 RGB 바이너리 연결. 평가 픽셀에서 샘플 텍스처 생성 |
| `Prefabs/XR/XRPlayerRig.prefab` | 공용 입력 리그. 오른손 ray가 슬롯의 XRI 활성화 이벤트와 제어 패널·팝업의 UI 입력을 제공한다. |

## 확인 범위

이 문서는 현재 소스·씬·프리팹의 참조와 동작 경로를 읽어 갱신했다. 이번 문서 갱신에서는 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다. 이전 실행 측정은 [CNN 학습 문서의 과거 검증 기록](CNN_TRAINING.md#과거-검증-기록)에 별도로 남기며 현재 코드의 검증 결과로 간주하지 않는다.

## Reality 외부 이미지 연결

ModelExecutionManager.InferExternalAsync는 정규화된 RGB float 배열과 reality: 요청 ID를 기존 학습 엔진에 전달한다. CNNModelDesignLabManager는 EnterRealityAsync/ExitRealityAsync 및 InferRealityAsync를 제공한다. ModelRealitySessionController가 CNNRealityLab을 Additive로 로드하고 기존 카메라를 연결한다. 모델은 이동하거나 복제하지 않는다. 기존 내부 샘플 UI는 reality: 결과를 표시하지 않는다. 공장 경유 실행은 지원하지 않으며 UI 버튼 연결도 추가하지 않았다. 상세 계약과 제한은 [CNNRealityLab](cnn-reality-lab.md)을 따른다. 컴파일·실행 검증은 수행하지 않았다.
