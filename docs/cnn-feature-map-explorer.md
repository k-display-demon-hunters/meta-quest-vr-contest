# CNN실 #8 피처맵 복도

[CNNFeatureMapLab.unity](../client/Assets/Scenes/CNNFeatureMapLab.unity)의 학습 진입점은 `CorridorRoot`의 `CNNFeatureMapLabManager`다. 기존 XR 리그와 공장 학습 세션을 사용한다. 공장에서는 기준점 (-3.65, 0, 12)에서 동쪽으로 향하도록 복도를 0.53배로 배치한다. 단독 씬의 복도 좌표는 1:1이다.

## 편집 모드의 배치

씬의 `CorridorRoot`는 [FeatureMapCorridor.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/FeatureMapCorridor.prefab) 인스턴스다. 입력판, 피처맵, 바닥, 출력 장치, 안내 UI와 연출용 오브젝트가 미리 저장되어 Play 없이 Scene 뷰에서 보이고 편집된다.

전체 프리팹은 실행 데이터 진입점, 모듈 설정, 아래 중첩 프리팹 인스턴스를 연결한다. 각 원본은 `Prefabs/CNNFeatureMapLab/`에서 따로 열 수 있다.

| 중첩 프리팹 | 편집 대상 |
| --- | --- |
| `CorridorInput.prefab` | 입구 왼쪽의 입력 이미지와 이미지에 붙인 숫자 선택 버튼, RGB 채널 3장과 펼침 앵커 |
| `CorridorFirstConv.prefab` | 첫 Conv 필터 장치 2개, 계산 앵커, 결과판 2장 |
| `CorridorPooling.prefab` | 채널별 Pooling 조작부 2개, 계산 앵커, 결과판 2장과 계산용 판 |
| `CorridorSecondConv.prefab` | 두 번째 Conv 필터 장치 4개, 계산 앵커, 결과판 4장과 비활성 여분 판 |
| `CorridorRoundPresentation.prefab` | Conv와 출력 비교에 함께 쓰는 필터 카드, 훑기 표시, 중간 결과판, 연출용 연결선 |
| `CorridorOutput.prefab` | 출력 통, 점수·액체·빛 표시, 비교 판과 답 이동 앵커 |
| `CorridorLessonUI.prefab` | 장치 위 설명 카드와 이전·다음 버튼, 힌트, 계산 요약 |
| `CorridorEnvironment.prefab` | 바닥·계단·보행 Collider, 구역 강조 |

- 기능별 배치는 해당 중첩 프리팹에서 편집한다. 전체 배치를 확인하려면 `FeatureMapCorridor.prefab`을 연다. 실행 시 복도 오브젝트를 다시 생성하지 않는다.
- 중첩 프리팹 루트는 위치 0, 회전 0, 스케일 1을 유지한다. RGB 펼침, 필터 위치, 답 이동 등은 복도 기준 좌표를 사용하므로 개별 오브젝트의 Transform에서 배치를 조정한다.
- 런타임 표시와 입력 참조는 전체 프리팹의 기존 모듈 설정에 연결되어 있다. `CorridorInput.prefab`에는 RGB 표시 오브젝트와 앵커가 함께 들어 있고, 다른 씬에서 펼침 동작을 사용하려면 해당 씬의 제어 연결이 필요하다.
- 입력 그림은 `Input image`, RGB 펼침 위치는 `RGB expanded 1~3`, 출력 통은 `Output 1~3`, 정답 도착 위치는 각 통의 `Answer destination`, 비교 연출 기준점은 `Evidence staging`에서 편집한다. 맵·입력판 크기는 패널 루트의 Scale로 조절한다.
- 연산 장치와 결과판은 프리팹에 저장되어 있다. 필터 장치는 3×3 돌출 격자, Pooling 장치는 2×2 격자다. 장치의 Transform에서 조작 위치를, 같은 번호의 `Calculation anchor`에서 Conv 카드나 Pooling 계산판이 나타나는 위치를 편집한다. 출력 통의 액체·점수 기둥은 저장된 크기를 최대 표시 기준으로 사용한다. 설명 카드의 크기는 `CorridorLessonUI.prefab`의 `Corridor subtitles` 배율에서 조절한다.
- RGB 채널, 필터 카드, 계산 연출, 완료 팝업 등 단계별 오브젝트는 프리팹에 존재하지만 초기에는 비활성 상태일 수 있다. Hierarchy에서 선택해 편집하고 초기 표시 상태를 유지한다. 두 번째 Conv의 여분 판 한 장은 비활성 상태로 배치한다.
- `Editor/CorridorPrefabBuilder.cs`는 저장된 배치를 프리팹으로 저장하는 도구다. 기존 프리팹이 있으면 메뉴에서 재생성하지 않는다. 저장할 때 필요한 임시 표시 에셋의 영속화는 `CorridorLayoutBuilder`가 담당한다. 런타임의 최초 배치 생성 코드와 `BuildLayoutForEditor`는 제거했다.

공장 Room 02의 체험 발판은 폭 5.104m이며 구역 사이에 계단이 있다. 입력판은 첫 발판의 입구 쪽 왼편에 있고, RGB 채널은 첫 발판 끝의 중앙에서 R·G·B 순으로 펼쳐진다. 입력 발판과 첫 Conv 사이의 계단을 입구 쪽으로 1.59m 옮겨 첫 Conv 발판을 그만큼 넓혔다. 계단의 각 단 깊이와 높이는 유지한다. 첫 Conv의 1-1·1-2와 Pooling의 채널 1·2 조작 장치와 결과판은 각 발판의 진행 방향 끝 쪽에서 좌우로 마주 본다. 첫 Conv에서는 RGB 채널이 다음 발판 중앙으로 이동하고, 필터 세 장은 같은 색 채널보다 출구 쪽에 배치된다. 두 번째 Conv의 필수 필터 3-1·3-2와 결과판은 발판의 출구 쪽에서 좌우로 배치한다. 추가 체험용 3-3·3-4는 양옆에 배치한다. 판정 장치는 중심 기준 동쪽 셔터에서 약 1.3m 앞에 있고, 세 통 사이로 출구 방향 안내선이 이어진다.

현재 학습 단계의 발판만 무광 파스텔 하늘색으로 표시하고 다른 발판은 무광 검은색으로 표시한다. 현재 구역의 바닥 글자는 짙은 남색으로 표시하며, 계단 앞 모서리는 낮은 채도의 호박색으로 구분한다. 지금 누를 필터·Pooling 장치의 얇은 외곽선만 호박색으로 밝아졌다 어두워진다. 결과판 외곽은 무광 회색으로 유지한다. 자막과 완료 창은 흑연색 배경을 사용하며 RGB 채널과 판정 결과는 각 데이터의 색을 유지한다.

## 진행

에디터에서는 Game 창에 포커스를 두고 공용 XR 시뮬레이터의 가상 오른손 레이로 대상을 가리킨 뒤 Shift 없이 T를 누른다. Quest에서는 오른손 레이와 Trigger를 사용한다. 숫자 버튼으로 입력을 바꾸고, 필터 또는 Pooling 장치를 눌러 계산을 시작한다. 실행 초기에는 결과판에 빈 격자가 보인다.

1. Room 2 입구를 지나면 입력 이미지 위에 안내가 표시된다. 옆의 숫자 버튼은 둥근 호박색 테두리와 짙은 청록 배경을 사용하며, 가리키거나 누르면 배경이 밝아진다. 이미지를 누르면 RGB 입력이 펼쳐지고, 첫 Conv 쪽으로 걸으면 고정 명찰이 붙은 RGB 입력판이 계산 위치까지 전진한다. 필터 1-1과 1-2는 선택 대기 중에만 테두리가 반짝인다. 장치의 R 미리보기는 첫 채널의 실제 가중치를 색으로 표시하고, 펼친 3×3 카드 세 장은 격자와 R·G·B 채널 이름을 표시한다. 적용과 훑기 화면에서는 입력 채널을 소개한 문장을 반복하지 않는다. **필터 적용**으로 계산을 시작하고 훑는 도중 **다음**을 누르면 결과를 바로 볼 수 있다. 첫 Conv의 필터 적용과 훑기 안내에서는 카드 안의 버튼을 더 크게 표시한다. **확인**으로 1-1을 마치면 피처맵 1-2와 필터 1-2가 나타난다.
2. Pooling 1을 마치면 Pooling 2의 장치와 결과판이 나타난다. 장치 선택 안내는 결과판 위에 표시하고, 장치를 누르면 앞쪽 계산판이 체험 구역 중앙에서 정면을 향하고, 계산판을 약 20cm 뒤에 두고, 안내는 계산판 위에서 약 15cm 사용자 쪽으로 앞당겨 평행하게 고정한다. 장치·계산판·결과판은 옆으로 띄워 배치하고 각 판 아래 불투명한 명찰에 흰 글씨로 이름을 표시한다. 각 장치는 2×2 영역의 강한 반응을 결과판에 옮긴다. **채널 전체 적용**으로 현재 채널의 결과를 채운다.
3. 왼쪽의 필터 3-1을 마치면 오른쪽에 필터 3-2와 결과판이 나타난다. 두 필터는 3×3 카드 형태를 사용한다. 펼친 카드 두 장은 발판의 출구 쪽에 배치해 조작자 앞에 설 공간을 남긴다. 3단계에서는 입력으로 쓰이는 Pooling 결과판 두 장도 기존 자리에서 그 앞 중앙으로 약 1.35초 동안 이동해 필터의 이동과 훑기를 같은 방향에서 볼 수 있다. 다른 단계로 이동하면 결과판은 Pooling 구역의 원래 자리로 돌아간다. 필수 피처맵의 결과판과 조작 필터는 중앙 입력판 양옆에 배치한다. 결과판은 정면에서 안쪽으로 10° 돌린다. 필터 3-1과 3-2는 결과판을 덜 가리도록 각각 왼쪽과 오른쪽 바깥에 두고 안쪽으로 30° 돌린다. 설명 카드는 해당 결과판 바로 위에 고정하고 **필터 적용**, **다음**, **확인**을 순서대로 표시한다. 두 필터를 마치면 가운데의 확대된 설명 카드에서 **추가 체험** 또는 **판정으로**를 고른다. 추가 체험을 고르면 필터 3-3과 3-4 및 결과판이 나타난다. 두 필터를 마치거나 **판정으로**를 누르면 출력으로 넘어간다. 체험하지 않은 필터의 결과는 출력 직전에 채운다.
4. 출력에 들어가면 피처맵 네 장이 준비되는 대로 판정이 시작된다. 마지막 피처맵 네 장을 숫자 1·2·3의 기준 틀과 비교한 뒤 세 통에 확률이 표시된다. 입력 그림은 가장 확률이 높은 통으로 이동한다.
5. 판정 첫 설명에서 **이전**을 누르면 추가 체험 선택 화면으로 돌아간다. 판정 정리에서 **점수 계산 보기**로 현재 입력 숫자의 점수 계산을 살펴보거나 **체험 완료**로 마친다. 완료 시 진행률이 100%가 되고 공장 학습 세션에 출구 사용 가능 상태를 보낸다. 출구 개방을 요청하는 시점에 Room 2의 모든 체험 장치, 결과판, 안내를 숨긴다. 공장에서는 구역 이동에 따라 FactorySceneLoadController가 이전·현재·다음 체험 씬을 유지하고, 그 범위를 벗어난 CNNFeatureMapLab 씬을 공통 언로드 절차로 정리한다. 단독 씬에서는 씬이 종료될 때까지 완료 상태를 유지한다.

다음 단계의 콘텐츠는 해당 순서에 나타난다. MaxPooling으로 넘어가면 입력 이미지, RGB 채널, Conv2D 1의 장치와 결과판을 숨긴다. MaxPooling 계산은 Conv2D 1 결과를 계속 사용한다. 복도 상단의 별도 `입력 · 16×16` 문구는 표시하지 않는다. Pooling 계산판의 입력 크기 명찰은 유지한다. 이전 버튼은 해당 단계의 설명과 표시를 복원한다. 공용 공간 버튼은 [공용 공간 UI](world-ui.md)를 사용한다.

판정 단계에서는 앞 단계 장치와 결과판이 숨겨진다. 점수 계산 보기에서는 입력 피처맵 네 장을 표시하고, 판정 정리 화면으로 돌아오면 다시 숨긴다. 판정 구역은 자동으로 회전하지 않는 출력·숫자 틀 명찰과 통 받침의 3D 숫자를 사용한다. 상단 공용 원통과 확률 통의 높이를 줄이고, 설명 카드를 기존 배율의 1.4배로 표시한다. 비교 틀에는 각 숫자의 실제 가중치 배열 네 장을 표시하고, 테두리는 점수와 관계없이 회색을 유지한다. 비교 점수는 기존 점수 기둥에서, 확률 값은 각 통 앞쪽의 고정 명찰에서 확인한다. 최종 판정 카드는 무광 테두리와 고정 명찰을 사용한다. 문은 기존 완료 조건을 충족할 때 열린다.

## 모듈과 상태 소유권

런타임 루트는 `client/Assets/Scripts/CNNFeatureMapLab/`이며 네임스페이스는 기존 `FeatureMapCorridor`를 유지한다. 씬 Manager는 아래 네 진입점과 데이터 계약만 사용한다. 모듈 간 요청은 씬 Manager가 연결하고, 내부 입력·연출은 해당 모듈 안에서 연결한다.

| 파일·모듈 | 현재 책임 |
| --- | --- |
| `CNNFeatureMapLabManager.cs` | LessonState의 소유·변경, 학습 전환·출구 조건, 모듈 이벤트 연결 |
| `LessonState.cs` | 현재 Phase·선택 숫자·활성 라운드와 공통 Step·필수 라운드 인덱스·필수 여부·진행률 |
| `ExecutionData/FeatureMapExecutionDataManager.cs` | 전달받은 숫자의 저장된 입력·레이어·연출 결과 조회 |
| `FeatureMaps/FeatureMapsManager.cs` | 체험별 진행 규칙 조회, 필터 입력과 연출 준비 상태 전달, 내부 표시·입력 조율 |
| `LessonUI/LessonUIManager.cs` | 전달받은 상황에 따른 학습 문구 선택, 자막·힌트·팝업·디버그 표시, 이전·다음 요청 |
| `Environment/CorridorEnvironmentManager.cs` | 복도 가시 상태, 바닥·구역 표시 연결 |

실행 데이터 진입점은 복도 루트의 기존 컴포넌트 GUID를 유지한다. 나머지 세 모듈 Manager와 표시 Controller는 기존과 같이 씬 진입점 내부에 직렬화된 일반 C# 객체다. 별도 Update를 갖는 컴포넌트로 일괄 변환하지 않았다.

씬 Manager는 private LessonState 인스턴스를 소유하고 상태 변경을 담당한다. 다른 모듈에는 필요한 값과 표시 데이터만 전달한다. 초기 선택 숫자는 2이며, 상태는 실행 중 메모리에 보관한다. Conv·Pooling·출력은 공통 Round Phase와 LessonStep을 사용한다. 단계 전환은 씬 Manager가 규칙의 결과를 받아 적용하며, Controller는 전달받은 단계의 표시와 재생 시간을 관리한다. 씬 Manager의 Awake는 상태의 선택 숫자로 스냅샷을 조회한 뒤 피처맵 → UI → 환경 순서로 초기화한다. 활성 기간 이벤트는 OnEnable/OnDisable에서 대칭으로 연결·해제한다. LateUpdate는 환경의 복도 가시 상태를 확인한 다음 활성 모듈의 Tick을 호출한다. OnDisable은 상호작용 대상의 이벤트 구독을 해제하고 입력을 비활성화하고, OnDestroy는 생성한 텍스처·디버그 표시를 정리한다.

### 외부 연결과 체험 내부 공간

- 외부 학습 세션과의 계약은 `CNNFeatureMapLabManager`의 기존 `ILearningExperience`다. CNNFeatureMapLab 런타임 코드는 FactoryEnvironment의 Manager, 복도 위치, 방·바닥 구역 데이터를 직접 참조하거나 조회하지 않는다. 카메라와 입력은 기존 공용 XR 리그와 XRI를 사용한다.
- `Environment/`는 체험 복도의 바닥 표시와 가시 상태를 담당한다. `CorridorCameraController`는 카메라 위치를 체험 루트의 로컬 좌표로 변환해 Room 2 입구부터 내부 범위를 판정한다. 이 판정으로 입력·UI·연출 갱신을 활성화한다.
- 발판 `Walk surface`의 BoxCollider와 표시용 `Slab`은 `CorridorEnvironment.prefab`에 저장되어 있다. 받침 윗면은 발판 밑면에 맞춘다. `CorridorFloorController`는 현재 학습 단계의 `Walk surface` 재질을 하늘색으로 바꾸고 다른 발판은 검은색으로 표시한다. 충돌체 17개는 발판 상단에서 받침 바닥까지 이어진다.
- 씬 Manager가 학습 단계에 맞춰 CorridorEnvironmentManager의 바닥 강조와 FeatureMapsManager의 피처맵 표시 강조를 함께 갱신한다.
- 공장 측의 체험 로딩·배치와 체험 내부의 로컬 공간 판정은 별도 책임이다. 이 입력 정리에서는 Core와 FactoryEnvironment 코드·에셋 및 기존 공용 인터페이스를 변경하지 않는다.
### 실행 데이터와 전달 계약

- `ExecutionData/Digit1ExecutionData.cs`부터 `Digit3ExecutionData.cs`까지 입력 RGB, 레이어 출력, 대표 칸과 채널별 기여도, Pooling 창, 출력 비교 틀·점수·확률을 C# 배열에 저장한다. 입력은 기존 복도 픽셀을 보존하고, 결과는 [데이터로 적합한 교육용 필터](../mnist-color-data/learned-filters/README.md)로 재계산했다. 숫자·배경 분리와 방향 반응을 목표로 하는 회귀 학습이며, 분류 정확도로 선택한 CNN은 아니다.
- `DigitExecutionData.cs`는 숫자마다 입력과 각 레이어의 결과 한 세트를 제공한다. 출력에는 후보 숫자 1·2·3의 점수와 확률, 비교 연출에 쓰는 채널별 기여도를 함께 저장한다.
- `FeatureMapSnapshot`은 입력·맵·밝기 기준·점수와 확률을, `RoundCalculationResult`는 연출에 필요한 대표 칸·기여도·출력 틀을 읽기 전용으로 전달한다. 표시 모듈은 픽셀 값을 텍스처로 표시하고 연출을 재생한다.
- 저장된 결과 데이터로 시각 연출을 재생한다. 입력 영역과 결과의 밝기·위치를 비교하며 특징 추출과 보존을 설명한다. `FirstKernels`와 `SecondKernels`는 같은 세 입력에 공통 적용하는 적합 가중치다. 가중치 변경에 맞춰 채널별 부분합·대표 칸·Pooling·공통 표시 스케일·출력 틀·점수도 함께 갱신했다. 출력은 기존 템플릿 비교와 softmax 계수 8을 유지하며, 시연 입력 자체가 템플릿에 포함되므로 출력 비율을 미지 데이터 성능으로 해석하지 않는다.

### 피처맵 표시와 연출

- 첫 Conv의 연결선은 펼친 필터 카드가 보일 때만 표시된다. 3-1·3-2의 필터 두 장은 연결선 없이 체험 중앙에 나란히 배치된다. 누를 차례인 3-1·3-2 장치는 호박색 테두리로 강조한다.

- 씬 Manager의 `LessonState`는 공개한 결과를 기억한다. `FeatureMapDisplayController`는 그 결과에 맞춰 입력판·피처맵·출력의 표시를 복원하고, 라운드 연출은 이 경로로 대표 칸과 순차 공개 상태를 그린다.
- `InputPanelController`, `FeatureMapPanelController`, `OutputStageController`는 각 표시·입력을 담당한다. 입력판과 중간 결과판·템플릿은 같은 패널 타입의 서로 다른 인스턴스다.
- `Round/PoolingRoundController`는 저장된 대표 영역과 최댓값을 기존 판에 표시한다. 자막은 강한 반응의 보존을 설명한다.
- `Round/ConvRoundRules.cs`, `PoolingRoundRules.cs`, `OutputRoundRules.cs`는 각 체험의 순서와 종료 동작을 정의한다. `RoundStepSequence`는 같은 순서에서 다음과 이전을 찾고, 필터 입력 대기와 연출 준비 여부를 반영해 `RoundTransition`을 반환한다.
- 씬 Manager가 이전·다음·필터 펼침 요청을 받아 `FeatureMapsManager`에 전환을 조회한다. 전환을 적용할 때 `LessonState.Step`을 갱신한 뒤, 표시와 UI에 같은 단계 값을 전달한다. Pooling과 첫 Conv의 필터 1-1은 마지막 단계 후 다음 필수 체험으로 이동한다.
- `Round/FeatureMapRoundController`는 받은 단계의 연출을 실행한다. 첫 Conv에서 `InputPanelController`는 RGB 입력판을 계산 앵커의 아래쪽 입구 방향으로 옮기고 `FilterStackController`는 선택한 장치에서 같은 간격으로 카드 세 장을 펼친다. `RoundResultsController`는 입력에 커널을 겹쳐 훑고 결과판을 채운다. 첫 Conv의 분리 연출과 출력의 필터 펼침 대기가 끝나면 `FeatureMapsManager`가 `PresentationReady`를 씬 Manager에 전달한다.
- 라운드를 마칠 때 씬 Manager는 `FeatureMapsManager.FinishRoundForRecap()`으로 연출을 마무리한다. 두 번째 Conv의 정리 화면에만 `RecapInfo`를 전달해 결과판 위에 요약을 표시한다.

### 입력 연결

- 상호작용 대상은 기존 Core XR 리그의 오른손 레이와 XRI의 활성화·호버 이벤트를 사용한다. 레이 거리·색상·햅틱은 공용 설정을 따른다. UI는 기존 XRUIInputModule과 TrackedDeviceGraphicRaycaster를 사용한다.
- 필터·Pooling 장치 8개는 `OperationDeviceController`가 XRI 입력을 받는다. `FeatureMapsManager`가 선택 가능한 장치를 활성화하고 선택 요청을 씬 Manager에 전달한다. 출력 통과 출력 비교 카드는 각각 `OutputStageController`와 `FilterStackController`가 입력을 받는다. 피처맵 결과판은 표시용이다.
- 각 대상은 오른손 레이의 활성화 이벤트와 기존 히트 결과를 사용하며, UI가 앞에 있거나 레이가 물체를 잡고 있으면 클릭을 처리하지 않는다. Grip으로 학습 조작 대상을 잡는 것은 각 Controller의 선택 필터가 차단한다. 호버 효과는 진입·이탈 이벤트로 갱신하고 디버그용 대상 이름과 클릭 기록은 모듈 내부에서 전달한다.
- 출력 비교 카드를 빠르게 두 번 누르면 접힌다. 0.4초 이내 같은 카드의 두 번째 클릭 판정은 FilterStackController가 소유한다.
- 숫자를 선택하면 씬 Manager가 LessonState.Digit을 변경하고 ExecutionData에서 해당 숫자의 스냅샷을 조회해 표시한다. ExecutionData는 선택 상태를 보관하지 않는다. Conv와 Pooling 설명은 선택한 데이터의 대표 영역을 사용한다. 선택 라운드를 마치면 다음 필수 라운드의 안내로 돌아간다.
- 버튼 구독은 해당 Controller가 소유하는 `CorridorButtonBindings`로 연결·해제한다.

### 안내 UI·환경·표시 보조

- `LessonContent`는 학습 설명 데이터, `LessonUIManager`는 상황에 맞는 설명 선택, SubtitlePanel/HintBubbles/CompletionPopup Controller는 실제 표시를 담당한다.
- `FeatureMapsManager`는 현재 단계에 해당하는 장치의 월드 좌표를 제공한다. 설명 카드는 입력판, 조작 장치, 계산 연출 또는 판정 장치 위로 이동한 뒤 그 위치에서 플레이어를 향한다. 이전·다음 버튼은 같은 카드에 표시한다.
- `LessonUI/CorridorDebugController`는 에디터·Development Build의 왼손 위 HUD와 화면 진단을 담당한다. Inspector의 `Lesson UI > Debug > Show Debug`에서 켠다. 씬·라운드 상태 및 포인터·셀·클릭·UI 정보를 값으로 전달받으며 릴리스에서는 진단 표시를 제외한다.
- `CorridorCameraController`는 공용 XR 카메라 위치를 읽어 복도 안팎을 판정한다. 체험은 카메라 후처리·배경색, XR 눈높이, 전역 안개를 변경하지 않는다. 카메라·환경 설정은 FactoryEnvironment에서 관리하며, 피처맵 전용 Global Volume과 후처리 프로필은 제거했다. 단독 실행에서도 환경 설정을 자동으로 덮어쓰지 않는다.
- `Presentation/CorridorRenderUtility`는 색·텍스처·빌보드·인스턴싱 표시 보조만 담당한다. 입력이나 런타임 자원 수명을 소유하지 않는다. 각 모듈은 자기 표시 객체의 빌보드 목록을 갖는다.
- `CorridorTheme`는 기존 Settings/CorridorTheme.asset을 사용한다. 동적 표시 이름은 `FeatureMapLabels`, 정수 퍼센트 배분은 `PercentageFormatter`가 담당한다. 고정 문구·기본 배치는 기존 프리팹에 남아 있다.

## 실행 데이터와 에셋

현재 배치는 입력 16×16×3, 첫 Conv 16×16×2, MaxPooling 8×8×2, 두 번째 Conv 8×8×4, 출력 숫자 1·2·3을 기준으로 한다. 데이터를 갱신할 때는 입력, 레이어 결과, 표시 스케일과 설명에 표시할 대표 칸·중간 결과를 함께 맞춘다. 씬 진입점이 선택한 숫자의 저장 결과를 가져오므로 씬에 모델 프리팹을 연결할 필요가 없다.

씬은 `Scenes/CNNFeatureMapLab.unity`, 프리팹은 `Prefabs/CNNFeatureMapLab/`에 유지한다. 재질·텍스처·메시는 각 종류의 CNNFeatureMapLab 폴더, 폰트는 Fonts/CNNFeatureMapLab에 유지한다. 원본을 재생성하거나 복제 보관하지 않았다.

## 이번 리팩토링의 확인 범위

2026-11-09: 소스와 Unity YAML의 정적 확인만 수행했다. 기존 스크립트 GUID 22개를 보존했고, FeatureMapCorridor의 오브젝트 3,748개와 Transform/RectTransform 배치를 유지했다. 변경된 직렬화 내용은 씬 진입점 컴포넌트의 모듈별 참조 배치다. 씬 인스턴스에는 이전 필드 경로 override가 없어 별도 변경하지 않았다. 사용자 지시에 따라 테스트·빌드·컴파일·Unity 실행을 수행하지 않았다. 아래 과거 결과는 리팩토링 이후의 실행 보증이 아니다.

### 공용 XRI 입력 전환

전용 FeatureMapPointerController와 CorridorMouseRaycaster를 제거하고, 기존 공용 오른손 레이의 XRI 호버·활성화 이벤트를 연결했다. 프리팹의 입력 대상 37개는 XRSimpleInteractable을 참조한다. 기존 34개 컴포넌트를 유지하고 RGB 대상 3개에 컴포넌트를 추가했으며, 전용 마우스 컴포넌트 8개를 제거했다. 기존 Transform·RectTransform·Collider 배치와 유지되는 에셋 GUID는 보존한다. 복도 안팎 판정과 바닥 구역 선택·강조는 유지한다. 소스·직렬화 참조만 정적으로 확인했으며 테스트·빌드·컴파일·Unity 실행은 수행하지 않았다.
## 공용 XRI 전환 이후 실행 검증 (2026-11-09)

사용자의 컴파일·Play·임시 검증기 허가로 최신 main `d9cdfe6` 기준에서 확인했다. 로컬 구버전에서 재현한 `PercentageFormatter`의 `| =` 컴파일 오류와 프리팹 루트의 YAML 별칭 참조 누락은 해당 main에 포함된 수정으로 해소했다. 시작 안내에는 오른손 레이와 Trigger, 헤드셋 없는 에디터에서는 Game 창 포커스와 Shift 없는 T키를 표시한다.

Unity 6000.3.24f1 / D3D11 Play에서 가상 오른손 XR 트리거와 기존 XREditorSimulator의 키보드 T 경로로 각각 입력 선택 → RGB 투어 → 필수 피처맵 1-1·2-1·3-1 → 출력 → 스위치 끄기·복원 → 완료까지 통과했다. 진행 메서드나 Button.onClick을 직접 호출하지 않고 Input System 기기 이벤트 → 공용 XRI/uGUI → 실제 콜백을 사용했다. 두 경로 모두 체험 중 런타임 오류 0건이며 진행률 1, 출구 사용 가능, 문 애니메이션 완료, 완료 팝업 닫힘을 확인했다.

T키 검증에는 W 이동 1회, 투어 다음·이전, 선택 맵 2-2 후 필수 라운드 복귀, 각 라운드 이전·다음, 완료 후 숫자 3 변경 및 완료 상태 유지도 포함했다. 배치 모드의 키보드 포커스 조건은 임시 메모리 설정으로 맞췄으며 프로젝트 입력 설정은 저장하지 않았다. 최종 녹화는 1280×720 무음 MP4, 약 206.5초이며 Unity VideoPlayer로 준비·길이·마지막 구간 디코딩까지 확인했다.

대상 사이의 위치와 레이 방향은 검증기가 배치했다. 최종 녹화는 1.6m 눈높이를 유지했으며 사람이 직접 걸으며 플레이한 실기기 영상은 아니다. 연결된 Quest가 없어 물리 컨트롤러·햅틱·기기 성능은 검증하지 않았다. 오프닝·엔딩 씬은 이번 작업에서 수정하지 않았다. 임시 Editor 검증 스크립트와 .meta를 삭제한 뒤 최종 배치 컴파일도 종료 코드 0으로 통과했다.

## 리팩토링 이전 입력 수정의 진단·검증 기록

### 진단과 확인 범위

| 항목 | 파일에서 확인한 내용 |
| --- | --- |
| SDK | manifest: XRI 3.3.2 / OpenXR 1.16.1. Meta/Oculus 전용 UI로 교체할 근거 없음. |
| 이동/트리거 | XRPlayerInputController와 XRPlayerRig는 스틱 이동, XREditorSimulator는 WASD 이동과 T 트리거를 분리한다. 트리거 이동 차단 플래그는 불필요하여 공용 이동을 수정하지 않음. |
| EventSystem | 공용 XRPlayerRig에 EventSystem 1개와 XRUIInputModule 1개. 단독 씬은 해당 리그 인스턴스를 참조하며 추가 EventSystem은 없음. |
| UI | 기존 TrackedDeviceGraphicRaycaster는 마우스용이 아니고, 숫자/중첩 버튼에는 3D occlusion이 켜져 있었음. Camera 필드는 비어 있었으나 XRI는 Camera.main fallback을 지원하므로 그 자체를 불통 원인으로 단정하지 않음. CanvasGroup은 저장된 복도에 없음. |
| 3D | 기존 XRSimpleInteractable.activated는 XRI hover/등록/거리 판정에 의존했고 기본 레이는 5m. RGB 판 3개는 Collider와 클릭 구독이 모두 없었음. OnMouseDown·마우스 전용 물리 클릭 코드는 사용하지 않았음. |
| 좌표 | 기존 BoxCollider + InverseTransformPoint 방식으로 textureCoord 오류에는 해당하지 않음. |
| 초기화/Console | 저장된 모델 참조와 Play 초기화를 확인함. 전체 입력 불통의 단일 원인을 단정하지 않았으며 실행에서 발견한 투어 메시 오류는 수정 후 재검증함. |
| 진행 | 시작은 입력 선택 대기. 다음/이전/숫자는 코드 구독 존재. RGB 미연결, 필터 접기 시 0단계로 복귀, 숫자 변경 시 라운드 이탈, 의무 맵 중 다른 층 선택 무시를 수정함. |

프리팹 파일 ID/컴포넌트 참조 점검에서 누락·중복이 없고 기존 Transform/RectTransform 블록 변경은 0개다. git diff --check를 확인했다. 이 결과는 소스·직렬화 정적 확인이며 컴파일/Play 통과를 뜻하지 않는다. 수동으로 레이어를 만들거나 버튼을 다시 연결할 필요는 없다.

2026-11-09 사용자 허가 후 Unity 6000.3.24f1의 D3D11 배치 Play 모드에서 가상 XR 트리거와 가상 마우스 경로를 각각 처음부터 끝까지 실행했다. 진행 메서드나 Button.onClick을 검증기에서 직접 호출하지 않았다. Input System의 기기 상태 이벤트 → 기존 XRI/uGUI → 실제 복도 콜백 경로를 사용했다. OS 마우스 커서·창 포커스는 조작하지 않았다.

| 실행 항목 | 가상 XR 트리거 | 가상 마우스 |
| --- | --- | --- |
| 시작 위치 입력판 선택 → RGB 펼침·투어 시작 | 통과 | 통과 |
| 입력판 두 번 누르기, RGB 채널 칸 선택 | 통과 | 통과 |
| 좌상단 (0,0), 우하단 (15,15) | 통과 | 통과 |
| 투어 다음/이전 및 의무 라운드 진입 | 통과 | 통과 |
| 필터 펼침·두 번 눌러 접기·다시 펼침, 단계 유지 | 통과 | 통과 |
| 훑기 중 숫자 1→2 변경, 재계산 및 단계 유지 | 통과 | 통과 |
| 필수 1-1 / 2-1 / 3-1 및 출력 2 라운드 | 통과 | 통과 |
| 선택 2-2 라운드 후 원래 의무 안내 복귀 | 통과 | 통과 |
| 스위치 끄기→다음→켜기→완료 | 통과 | 통과 |
| 완료 확인으로 팝업 닫힘·진행률 1·출구 사용 가능·문 애니메이션 완료 | 통과 | 통과 |
| 완료 후 숫자 3 변경 및 빈 공간 입력 시 리그 위치 유지 | 통과 | 통과 |

첫 XR 실행에서는 전체 진행이 완료됐으나 투어 재시작에서 Mesh.vertices 오류가 기록됐다. ReceptiveFieldTour.Clear에서 이전 삼각형 인덱스를 제거한 뒤 새 정점 배열을 설정하도록 수정했다. 수정 후 두 입력 경로 모두 전체 진행에서 런타임 오류 없이 PASS ALL을 확인했다.

검증기는 이동 구간의 리그 위치와 시점을 설정해 대상에 접근했다. 실제 WASD/스틱을 이용한 복도 보행, Quest 물리 컨트롤러·햅틱, 기기 렌더링/성능, APK 빌드는 이 검증에 포함하지 않았다. 가상 마우스 경로에서는 화면 밖 대상을 바라보도록 가상 HMD를 회전했다. 임시 Editor 검증 스크립트와 .meta는 검증 후 삭제했다. 삭제 후 최종 컴파일도 종료 코드 0으로 완료했으며 실제 기능 코드에 검증기는 포함되지 않는다. 아래는 이전 작업의 별도 기록이다.


## 이전 프리팹 저장 전환의 실행 기록

2026-11-09 편집용 저장 전환: 사용자 허가 범위에서 Unity 스크립트를 컴파일하고 프리팹·씬·표시 에셋을 저장했다. Play하지 않은 Scene 뷰에서 복도 표시를 확인했다. 이번 전환의 Play 테스트·앱 빌드는 수행하지 않았다.

아래는 전환 전 런타임 생성 방식의 실행 확인 기록이며, 이번 변경의 플레이 검증 결과가 아니다.

Unity 6000.3.24f1 에디터에서 더미 숫자 1·2·3의 예측, 맵 스위치의 전파, 필수·선택 라운드와 완료 상태를 확인했다. 숫자 2의 예측 확률은 97.56%였다.

Direct3D 12에서 투어 자막·출력·완료 팝업을 렌더링했다. XRI의 추적 레이 이벤트로 이전·다음·확인 버튼의 타격을 검증하고, 확인 버튼이 팝업을 닫는지 검사했다. 복도가 활성화된 완료 상태에서 `CorridorDirector.LateUpdate`를 48회 샘플링한 관리 힙 할당은 0바이트였다. 이 수치는 에디터의 해당 경로에 한정된다. ADB에 연결된 Quest가 없어 실기기 72fps와 전체 프레임의 GC는 측정하지 못했다.

단독 씬의 기본 시작 위치에서 가상 오른손 컨트롤러로 48회 레이 입력을 수행해 숫자 변경, 입력 칸 선택, 이전·다음, 필수 라운드 4개, 스위치 복원과 완료 팝업 닫기를 검증했다. 진행 함수는 직접 호출하지 않았으며, 구역 사이의 위치 이동은 검증 코드가 설정했다.
