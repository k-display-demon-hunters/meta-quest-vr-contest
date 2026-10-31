# 기기 내 CNN 학습

`CNNModelDesignLab`은 서버 없이 내장 MNIST 데이터로 CNN 전체를 학습한다. 합성곱 필터·Dense 가중치·편향을 실제 역전파와 Adam으로 갱신하며, 학습된 가중치로 평가와 숫자 예측을 수행한다. Python, 네트워크 연결, 외부 학습 프레임워크는 실행에 필요하지 않다.

기존 `server/` 구현과 서버용 JSON·WebSocket 명세는 보관한다. 현재 Unity 씬은 로컬 학습 엔진을 사용하며, 보관된 서버는 별도로 실행할 수 있다.

**현재 구현의 Quest 3S 학습 시간·정확도·VR 프레임은 이 문서 갱신에서 검증하지 않았다.** 완료 표시는 실제 학습·평가 결과를 사용하며, 제한 시간이 지났다는 이유로 성공이나 임의 지표를 표시하지 않는다.

## 모듈 경계

씬의 모듈 연결·편집 API는 [모델 제작 씬](MODEL_DESIGN_API.md)에 정리한다. 현재 학습 코드는 모두 `client/Assets/Scripts/CNNModelDesignLab/` 아래에 있다.

| 위치 | 책임 |
| --- | --- |
| `Model/` | 슬롯 8개의 편집 모델, 레이어 값과 개별 설정 범위. 엔진 어셈블리를 참조하지 않는 순수 C# 어셈블리 `ModelDesign` |
| `ModelBuilder/`, `BlockSettings/` | 블록 배치·잡기와 설정 입력. 실행 계산을 수행하지 않는다. |
| `Execution/ModelExecutionManager.cs` | Dataset과 엔진 사이의 진입점. 실행 스냅샷·슬롯 대응표·검증·엔진 수명 조율 |
| `Execution/ModelExecutionController.cs` | 실행 상태, 세션/작업 번호, 메인 스레드 결과 전달 |
| `Execution/Engine/` | 순수 C# 어셈블리 `CNNTraining`. `CnnEngineManager`를 진입점으로 모델 연산·역전파·최적화·학습 세션 제공 |
| `Dataset/` | MNIST 파일 해석·정규화·split 분리, UI 샘플과 추론 입력 제공 |
| `ControlPanel/` | 검증 문제·학습 지표·추론 결과 표시와 실행 요청 |
| `client/Assets/Data/CNNModelDesignLab/` | 학습·검증 후보 및 평가 이미지 바이너리 에셋 |

의존 방향은 `씬 Manager → Execution Manager → Engine Manager → 학습 세션`이다. Execution이 편집 Model과 엔진 표현을 변환한다. 엔진은 Unity API, 블록, 씬, MNIST 파일 형식과 이미지 ID 규칙을 모른다. `Execution/Engine/CNNTraining.asmdef`는 `noEngineReferences: true`이며 다른 어셈블리를 참조하지 않는다.

## 세션과 실행 수명

`ModelExecutionManager`가 최초 유효한 학습 요청 때 `CnnEngineManager`를 만들고 재사용한다. Engine Manager가 `CnnTrainingSession` 하나를 소유한다. `ModelExecutionController`는 세션을 직접 소유하지 않고 실행 함수를 전달받아 상태와 결과 큐를 관리한다. 싱글톤·전역 모델·전용 상주 스레드는 없다. 요청마다 스레드풀 작업 하나에서 계산하며 세션당 동시에 한 작업만 허용한다.

시작 시 모델 정의·설정·데이터는 실행용 값으로 고정한다. 학습 중 블록 설정을 바꿔도 진행 중인 모델이 바뀌지 않는다. 순전파·역전파·가중치 갱신 버퍼는 엔진 내부에서 재사용한다. 씬은 내부 레이어나 가중치에 접근하지 않고 요청·취소·진행·결과만 다룬다.

백그라운드 진행 알림은 Controller의 큐에 담고 메인 스레드 `Update()`에서 UI로 전달한다. 학습·평가·추론 중 추가 실행을 거절하고 취소는 협력적으로 처리한다. 종료 시 Execution Manager가 Controller의 세대 번호·큐를 초기화하고 엔진을 Dispose한다. 세션은 취소를 요청하며 계산이 끝나기 전 자원을 다른 작업에서 재사용하지 않는다. 이전 세션·작업의 늦은 응답은 현재 화면을 갱신하지 못한다.

새 학습은 새로운 가중치와 optimizer 상태로 시작한다. 모델은 파일로 저장하지 않으며 씬 종료 후에는 다시 학습해야 한다. `CNNModelDesignLabManager.CanProceed(LearningStageId.ModelDesign)`은 정상 학습 결과 반영과 작업 종료, 활성 상태를 요구한다. 처음 충족하면 StageCompleted를, 학습 결과 반영·실행 상태 변경·비활성화로 진행 가능 여부가 바뀌면 ProgressionAvailabilityChanged를 알린다. Core LearningSessionManager가 두 이벤트를 구독하고 최초 연결 때 HasCompleted와 CanProceed로 상태를 동기화한다. 누적 완료는 Core 세션의 LearningProgress에 남고 현재 진행 조건이 거짓이 되어도 지우지 않는다. 태블릿은 읽기 전용 스냅샷을 표시하며, 공장은 세션의 지시에 따라 후문과 안내선을 제어한다. 공장 씬 종료 시 학습 진행 기록은 폐기된다. 현재 구현에는 단계 건너뛰기로 완료를 대체하는 상태가 없다. 취소·실패·추론만으로 학습 완료를 설정하지 않는다.

## 지원 구조와 연산

```text
Input(28 × 28 × 1)
→ [Conv2D / MaxPooling2D ...]
→ Flatten → [Dense ...] → Output(Dense 10, Softmax)
```

슬롯 순서의 순차 연결만 허용하며 분기·합류·순환·잔차 연결은 지원하지 않는다. 첫 레이어를 Conv2D로 강제하지 않으며 `Flatten → Output` 같은 구성도 검사를 통과할 수 있다. Flatten은 명시적으로 한 번 들어가고 이후에는 Dense만 허용한다. 공간 입력을 Dense에 직접 연결하거나 자동으로 Flatten을 삽입하는 동작은 없다.

| 연산 | 순전파 및 역전파 |
| --- | --- |
| Conv2D | valid padding, stride 1. 출력 높이·너비는 입력에서 커널 크기−1을 뺀 값. 입력·필터·편향 기울기를 누적 |
| MaxPooling2D | valid padding, stride는 각 풀 크기. 최대값의 원래 위치를 기록하여 그 위치로 기울기 전달 |
| Flatten | HWC(높이·너비·채널) 순서의 공간 배열을 벡터로 해석. 원소 순서 보존 |
| Dense | 완전연결과 편향. 입력·가중치·편향 기울기 계산 |
| Linear / ReLU / Sigmoid / Tanh | Conv 및 은닉 Dense에서 선택 가능. 각 함수의 미분을 사용 |
| Softmax | Conv의 각 픽셀에서는 채널 축, Dense에서는 전체 출력 벡터에 적용. 은닉 Softmax는 벡터의 결합 미분을 계산 |
| 최종 분류 | Dense 10·Softmax 고정. 클래스 0~9. 최종 로짓에서 안정적인 교차엔트로피와 결합 기울기 계산 |
| Adam | 미니배치 평균 기울기로 전체 학습 파라미터를 갱신. 모멘트·시점은 해당 실행 내부에 보관 |

## 학습 설정과 실행 검증

`CnnTrainingSettings.CreateOptions()`가 코드의 `private const` 값으로 변경 불가능한 `CnnTrainingOptions`를 만든다. Inspector 설정 에셋은 사용하지 않는다. `ModelExecutionManager`는 같은 GameObject의 Controller를 `Awake()`에서 연결한다.

| 기본값 | 값 |
| --- | --- |
| epoch / batch size | 10 / 16 |
| learning rate / seed | 0.001 / 42 |
| Conv 필터 / Dense 뉴런 상한 | 16 / 128 |

개별 레이어의 설정 범위는 Model의 `LayerLimits`와 `LayerDefinition`에서 관리한다. Execution은 해당 범위 검사를 반복하지 않는다. 씬 편집 모델은 슬롯 8개로 제한한다. 이와 별도로 모델 전체 파라미터 수·중간 출력 크기·연산량·전체 학습량의 실행 자원 한도는 적용하지 않으며, 엔진 입력 정의와 데이터의 임의 크기 상한도 두지 않는다.

`Execution/ModelExecutionValidator.cs`는 학습 시작 요청 시 레이어 순서, Flatten 전환, 현재 공간 크기에 대한 커널·풀 크기, 최종 출력과 데이터 입력 형태를 검사한다. Manager는 검증 통과 후 엔진을 호출한다. 편집 시에는 기존 검사 결과만 초기화하며 재검증하지 않는다. 양수 차원·배열 길이 일치·정수 오버플로 같은 데이터 표현의 유효성 검사는 유지한다.

아래 과거 측정 기록의 설정 에셋과 자원 한도는 당시 구현 기준이며 현재 실행 정책에는 적용되지 않는다.

## 내장 MNIST 데이터

원본은 [Keras 공식 MNIST 배포](https://keras.io/api/datasets/mnist/)의 `mnist.npz`다. [공식 로더](https://github.com/keras-team/keras/blob/v3.15.1/keras/src/datasets/mnist.py)가 지정한 다운로드 경로와 SHA-256으로 원본을 확인하여 변환했다. 전체 원본 NPZ나 변환 스크립트 사본은 저장소에 추가하지 않는다. 데이터의 출처·라이선스는 [ASSET_CREDITS.md](ASSET_CREDITS.md)에 기록한다.

- `MnistTraining.bytes`: 원본 training split에서 숫자별로 가장 앞선 50개, 합계 500개. 원래 인덱스 순으로 저장한다. 로더가 숫자별 매 10번째를 validation에 배정하므로 실제 학습 450개(숫자별 45개), 검증 50개(숫자별 5개)다.
- `MnistTest.bytes`: 원본 test split에서 숫자별로 가장 앞선 20개, 합계 200개. 최종 평가 및 UI 예측에 사용한다. 학습·검증 split에 포함되지 않는다.
- 이미지는 원본 28×28 흑백 바이트와 정답을 보존한다. `MnistDataset.Load()`가 픽셀을 `value / 255f`로 정규화하며 회전·반전·재크기 조정은 없다.
- 학습·검증·평가 정확도는 각각의 작은 부분집합 기준이다. MNIST 전체 10,000장에 대한 벤치마크 정확도로 해석하지 않는다.
- 기존 표시 이미지 이름 `mnist-test-N`의 N은 원본 test split 인덱스다. `TryGetTestImage()`가 같은 원본 인덱스의 픽셀을 반환한다. 텍스처의 read/write 설정이나 GPU readback은 필요하지 않다.

엔진에는 `TrainingDataset(float[] images, int[] labels, int inputSize)` 세 개를 묶은 `TrainingData`를 전달한다. 원본 인덱스와 MNIST 이미지 이름 해석은 씬 어댑터에만 남는다.

### 바이너리 형식 (version 1)

모든 정수는 little-endian이다. 헤더 24바이트 뒤에 지정 개수의 레코드가 오며 뒤쪽 추가 바이트를 허용하지 않는다.

| 위치·크기 | 값 |
| --- | --- |
| 0, 4 bytes | ASCII `CMNT` |
| 4, int32 | version = 1 |
| 8, int32 | split = 0(training) 또는 1(test) |
| 12, int32 | height = 28 |
| 16, int32 | width = 28 |
| 20, int32 | record count |
| 각 레코드, int32 | 해당 원본 split의 index |
| 각 레코드, uint8 | label 0~9 |
| 각 레코드, 784 × uint8 | 행 우선 픽셀, 채널 1 |

로더는 헤더, split, 길이, 개수, 인덱스 범위·중복, 정답을 검사한다. training split과 test split에서 같은 정수 인덱스는 서로 다른 원본 샘플이다.

## 과거 검증 기록

다음은 2026-10-24 당시 구현의 실행 기록이다. 이후 모듈 경로·편집 API·설정·검증 정책이 바뀌었으므로 현재 코드에 대한 통과 결과나 성능 보장으로 사용하지 않는다. 이번 문서 갱신에서는 현재 소스와 에셋 참조만 읽었고 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다.

2026-10-24, Windows의 Unity 6000.3.24f1 EditMode에서 당시 구현의 핵심 테스트 9개를 실행하여 모두 통과했다. 테스트 실행에 필요한 스크립트 컴파일도 성공했다.

- 수학 4개: 다채널·직사각형 Conv와 Tanh, Dense와 Softmax의 입력·가중치·편향 기울기를 수치 미분과 비교했다. MaxPool의 최대 위치·채널·경계·버퍼 재사용, 큰 로짓에서의 안정적인 교차엔트로피와 결합 기울기를 확인했다.
- 세션 2개: 작은 합성 데이터에서 손실 감소, 반복 추론과 정답을 뒤집은 재학습, 동시 요청 거부, 취소 후 부분 모델 차단과 세션 재사용을 확인했다.
- 블록 구성 3개: 역방향 체인의 실행 순서, 분리된 블록 제외, 잘못된 구조·자원 한도 거부, 실행 스냅샷의 불변성과 비교를 확인했다.

실행 필터는 `CNNTraining.Tests;ModelDesign.Tests.ModelDesignTests.LocalCnn`이다. 당시 테스트 소스 위치는 `client/Assets/Tests/EditMode/Core/CNNTraining/`과 `client/Assets/Tests/EditMode/ModelDesign/ModelDesignTests.cs`였으며, 현재 편집 모델 API와 동일하다고 해석하지 않는다. 당시 결과 XML 기록 경로는 Git에서 제외되는 `client/Logs/local-cnn-tests.xml`이다. 전체 테스트 모음이나 앱 빌드는 실행하지 않았다.

위 테스트는 PC의 소규모 기능 검증이다. 실제 씬의 VR 상호작용·종료 처리와 Quest 3S의 시간·메모리·프레임 측정은 남아 있다.

### 내장 MNIST 에디터 단일 실행 측정

2026-10-24에 Windows 11, Intel Core i5-1235U, Unity 6000.3.24f1의 Mono 에디터 배치 모드에서 한 번만 학습했다. 그래픽·VR 씬 렌더링은 사용하지 않았다. 실제 `LocalTrainingSettings.asset`과 연결된 MNIST 에셋을 읽고, 씬과 같은 실행 정의 Builder 및 `CnnTrainingSession.TrainAsync()`를 사용했다. 워밍업·반복 실행·설정 조정은 하지 않았다.

모델은 `Conv(4, 3×3, ReLU) → Pool(2×2) → Flatten → Dense(10, Softmax)`이며 파라미터는 6,810개다. 실제 에셋 설정인 3 epoch, batch 16, learning rate 0.001, seed 42와 학습 450장·검증 50장·평가 200장을 사용했다.

| 시간 범위 | 측정값 |
| --- | --- |
| 설정·데이터 로드, 정규화, 실행 정의·세션 준비 | 0.04344초 |
| 모델 초기화·학습·매 epoch 검증·최종 평가·완료 | 1.14875초 |
| 위 두 단계 전체 | 1.19243초 |
| 최종 평가·완료 처리 (학습·평가 시간에 포함) | 0.08550초 |

Unity 실행·임포트·스크립트 컴파일과 결과 파일 기록은 측정 시간에서 제외했다. 첫 학습 호출의 모델 초기화와 JIT 비용은 포함된다. epoch 시간은 해당 epoch 검증까지 포함하며, 첫 epoch에는 모델 초기화도 포함된다.

| Epoch | 해당 epoch 시간 | 학습 정확도 | 검증 정확도 | 학습 손실 | 검증 손실 |
| --- | --- | --- | --- | --- | --- |
| 1 | 0.37239초 | 30.67% | 52% | 2.19036 | 2.05557 |
| 2 | 0.36664초 | 65.56% | 72% | 1.88593 | 1.75415 |
| 3 | 0.32422초 | 79.56% | 76% | 1.51013 | 1.40079 |

**별도 평가 200장의 최종 정확도는 74% (148/200), 손실은 1.44730이다.** 학습 정확도·손실은 epoch 중 갱신되는 가중치로 계산한 누적 평균이며, 마지막 가중치로 학습 데이터를 다시 평가한 수치는 아니다. 검증·최종 평가는 각 단계 종료 가중치로 계산했다.

원시 결과는 Git에서 제외되는 `client/Logs/mnist-editor-measurement-20261024.json`, 실행 로그는 같은 이름의 `.log`에 있다. 한 번 실행한 임시 에디터 코드는 측정 후 제거했다. 이 결과는 작은 MNIST 부분집합과 PC CPU에서의 단일 측정이며, MNIST 전체 10,000장 정확도나 Quest 3S의 10~15초 달성을 의미하지 않는다.

### 추가 레이어·10 epoch 측정

같은 날 같은 PC·에디터 환경과 MNIST 학습 450장·검증 50장·평가 200장을 사용하여 다음 모델을 별도로 한 번 학습했다.

```text
Input(28×28×1)
→ Conv(4, 3×3, ReLU) → Pool(2×2)
→ Conv(8, 3×3, ReLU) → Pool(2×2)
→ Flatten → Dense(32, ReLU) → Dense(10, Softmax)
```

측정 실행의 epoch만 10으로 지정하고, batch 16·learning rate 0.001·seed 42와 자원 한도는 설정 에셋에서 그대로 읽었다. 프로젝트 설정 에셋의 기본 epoch는 3으로 유지했다. 파라미터는 7,098개, 한 이미지 순전파 Conv·Dense 연산량은 65,904 MAC, 전체 학습·평가 추정치는 935,836,800 MAC으로 기존 20억 한도 안이다.

| 항목 | 기본 모델·3 epoch | 추가 레이어·10 epoch |
| --- | --- | --- |
| 준비 | 0.04344초 | 0.03710초 |
| 모델 초기화·학습·검증·최종 평가 | 1.14875초 | 6.21803초 |
| 준비 포함 전체 | 1.19243초 | 6.25535초 |
| 마지막 epoch 학습 정확도 (누적 평균) | 79.56% | 92.67% |
| 마지막 검증 정확도 (50장) | 76% | 86% |
| 최종 평가 정확도 (200장) | 74% (148/200) | 84% (168/200) |
| 최종 평가 손실 | 1.44730 | 0.46175 |

추가 모델의 학습 손실은 1 epoch의 2.23726에서 10 epoch의 0.29679로, 검증 손실은 2.15928에서 0.41178로 감소했다. 9 epoch 검증 정확도는 90%였고 10 epoch에는 86%였다. 가장 높은 검증 epoch를 골라 재실행하거나 별도 평가하지 않았으며, 최종 결과는 요청한 10 epoch 완료 가중치 기준이다.

모델 구조와 epoch를 함께 바꾼 단일 실행 비교이므로 정확도 향상을 레이어 추가만의 효과로 분리해 해석할 수 없다. PC에서의 준비 포함 시간은 6.26초였으며 Quest 3S 시간은 여전히 미측정이다. 그래픽·VR 렌더링과 에디터 시작·임포트·컴파일 시간의 제외 기준은 이전 측정과 같다.

원시 결과는 `client/Logs/mnist-editor-deeper-10epochs-20261024.json`, 로그는 같은 이름의 `.log`다. 임시 실행 코드는 측정 후 제거했다.

## 현재 구현 확인 범위

현재 경로, 10 epoch 설정, 슬롯 기반 모델·검증, Dataset/Execution/ControlPanel의 책임과 공장 완료 조건은 소스와 씬 참조를 기준으로 정리했다. 위 PC 측정과 현재 Quest 실행 성능은 별개다. 현재 씬 전체의 VR 입력·취소·재진입 및 Quest 3S 시간·메모리·프레임은 이 문서 갱신에서 실행 확인하지 않았다.

이번 공통 완료 이벤트와 Core 학습 세션 연결 변경은 소스로 검토했으며 Unity 실행·컴파일·테스트·빌드는 수행하지 않았다. 이전 실행 기록은 이번 변경의 검증 결과가 아니다.
