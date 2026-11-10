# CNN실 #8 피처맵 복도

[CNNFeatureMapLab.unity](../client/Assets/Scenes/CNNFeatureMapLab.unity)의 학습 진입점은 `CorridorRoot`의 `CNNFeatureMapLabManager`다. 기존 XR 리그와 공장 학습 세션을 사용한다. 공장에서는 Room 02의 기존 앵커와 출입구 사이에 들어오도록 복도를 0.53배로 배치한다. 단독 씬의 복도 좌표는 1:1이다.

## 편집 모드의 배치

씬의 `CorridorRoot`는 [FeatureMapCorridor.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/FeatureMapCorridor.prefab) 인스턴스다. 입력판, 피처맵, 바닥, 출력 장치, 문, 안내 UI와 연출용 오브젝트가 미리 저장되어 Play 없이 Scene 뷰에서 보이고 편집된다.

전체 프리팹은 실행 데이터 진입점, 모듈 설정, 아래 중첩 프리팹 인스턴스를 연결한다. 각 원본은 `Prefabs/CNNFeatureMapLab/`에서 따로 열 수 있다.

| 중첩 프리팹 | 편집 대상 |
| --- | --- |
| `CorridorInput.prefab` | 입구 왼쪽의 입력 이미지와 이미지에 붙인 숫자 선택 버튼, RGB 채널 3장과 펼침 앵커 |
| `CorridorFirstConv.prefab` | 첫 Conv 피처맵 2장 |
| `CorridorPooling.prefab` | Pooling 결과판 2장과 계산용 판 |
| `CorridorSecondConv.prefab` | 두 번째 Conv 피처맵 4장과 비활성 여분 판 |
| `CorridorRoundPresentation.prefab` | Conv와 출력 비교에 함께 쓰는 필터 카드, 훑기 표시, 중간 결과판, 연출용 연결선 |
| `CorridorOutput.prefab` | 출력 통, 점수·액체·빛 표시, 비교 판과 답 이동 앵커 |
| `CorridorLessonUI.prefab` | 자막, 이전·다음 버튼, 힌트, 계산 요약, 완료 팝업 |
| `CorridorEnvironment.prefab` | 바닥·계단·보행 Collider, 구역 강조, 문과 문틀 |

- 기능별 배치는 해당 중첩 프리팹에서 편집한다. 전체 배치를 확인하려면 `FeatureMapCorridor.prefab`을 연다. 실행 시 복도 오브젝트를 다시 생성하지 않는다.
- 중첩 프리팹 루트는 위치 0, 회전 0, 스케일 1을 유지한다. RGB 펼침, 필터 위치, 답 이동 등은 복도 기준 좌표를 사용하므로 개별 오브젝트의 Transform에서 배치를 조정한다.
- 런타임 표시와 입력 참조는 전체 프리팹의 기존 모듈 설정에 연결되어 있다. `CorridorInput.prefab`에는 RGB 표시 오브젝트와 앵커가 함께 들어 있고, 다른 씬에서 펼침 동작을 사용하려면 해당 씬의 제어 연결이 필요하다.
- 입력 그림은 `Input image`, RGB 펼침 위치는 `RGB expanded 1~3`, 출력 통은 `Output 1~3`, 정답 도착 위치는 각 통의 `Answer destination`, 비교 연출 기준점은 `Evidence staging`, 문은 `Corridor door`에서 편집한다. 맵·입력판 크기는 패널 루트의 Scale로 조절한다.
- 출력 통의 액체·점수 기둥은 저장된 크기를 최대 표시 기준으로 사용한다. 문은 저장된 닫힘 위치에서 움직인다. 자막·팝업은 저장된 배율을 사용하되 위치는 실행 중 카메라를 따라간다. 필터·강조선·계산 결과판은 학습 단계에 따라 이동·표시되므로 편집 시의 위치가 고정 표시 위치를 뜻하지는 않는다.
- RGB 채널, 필터 카드, 계산 연출, 완료 팝업 등 단계별 오브젝트는 프리팹에 존재하지만 초기에는 비활성 상태일 수 있다. Hierarchy에서 선택해 편집하고 초기 표시 상태를 유지한다. 두 번째 Conv의 여분 판 한 장은 비활성 상태로 배치한다.
- `Editor/CorridorPrefabBuilder.cs`는 저장된 배치를 프리팹으로 저장하는 도구다. 기존 프리팹이 있으면 메뉴에서 재생성하지 않는다. 저장할 때 필요한 임시 표시 에셋의 영속화는 `CorridorLayoutBuilder`가 담당한다. 런타임의 최초 배치 생성 코드와 `BuildLayoutForEditor`는 제거했다.

## 진행

에디터에서는 Game 창에 포커스를 두고 공용 XR 시뮬레이터의 가상 오른손 레이로 대상을 가리킨 뒤 Shift 없이 T를 누른다. Quest에서는 오른손 레이와 Trigger를 사용한다. 숫자 버튼으로 입력을 바꾸고, 피처맵 판을 선택해 계산을 연다.

1. 첫 Conv에서 피처맵 1-1과 1-2를 차례로 열어 두 필터의 반응을 비교한다. 필터를 펼친 뒤 RGB별 훑기, 대표 결과 칸의 기여도와 편향, ReLU, 합치기를 본다. 저장된 대표 칸을 사용한다.
2. MaxPooling에서는 기존 가운데 구역의 두 판에 채널별 결과를 표시하고, 나머지 판을 계산 보기에 사용한다. 다음 버튼으로 대표 2×2 영역과 네 값, 최댓값이 결과 한 칸에 들어가는 과정을 본다. 양쪽 결과판으로 채널을 전환하고 전체 적용을 누르면 8×8 결과 두 장을 본다.
3. 두 번째 Conv는 Pooling 결과 두 장을 사용해 피처맵 네 장을 만든다. 피처맵 3-1과 3-2를 필수로 비교하며, 3-3과 3-4는 선택이다. 필터별 반응은 결과의 밝기와 위치로 관찰한다.
4. 출력에서 현재 입력 숫자의 통을 눌러 마지막 피처맵 네 장과 숫자별 기준 틀의 비교, 빛 분배, 답 비행을 본다.
5. 출력 정리에서 **체험 완료**를 누르면 진행률이 100%가 되고 문이 열린다. 공장 학습 세션에 출구 사용 가능 상태를 보낸다.

체험은 첫 Conv 판 선택부터 시작한다. 입구에서 입력판을 누르는 RGB 펼침과 수용 영역 투어, 픽셀 직접 선택을 제거했다. 첫 Conv의 필터 연출에서 RGB 채널을 표시한다. 이전 버튼은 해당 설명과 연출을 다시 보여준다. 공용 공간 버튼과 팝업은 [공용 공간 UI](world-ui.md)를 사용한다.

## 모듈과 상태 소유권

런타임 루트는 `client/Assets/Scripts/CNNFeatureMapLab/`이며 네임스페이스는 기존 `FeatureMapCorridor`를 유지한다. 씬 Manager는 아래 네 진입점과 데이터 계약만 사용한다. 모듈 간 요청은 씬 Manager가 연결하고, 내부 입력·연출은 해당 모듈 안에서 연결한다.

| 파일·모듈 | 현재 책임 |
| --- | --- |
| `CNNFeatureMapLabManager.cs` | 전체 학습 단계, 필수·선택 라운드 순서와 복귀, 진행률·출구 조건, 모듈 이벤트 연결 |
| `ExecutionData/FeatureMapExecutionDataManager.cs` | 선택 숫자 관리, 저장된 입력·레이어·연출 결과 조회 |
| `FeatureMaps/FeatureMapsManager.cs` | 탐색·Conv 및 Pooling 표시, 필터 입력 대기, 내부 표시·입력 기능 연결 |
| `LessonUI/LessonUIManager.cs` | 전달받은 상황에 따른 학습 문구 선택, 자막·힌트·팝업·디버그 표시, 이전·다음 요청 |
| `Environment/CorridorEnvironmentManager.cs` | 복도 가시 상태, 바닥·구역·문·카메라 효과 연결 |

실행 데이터 진입점은 복도 루트의 기존 컴포넌트 GUID를 유지한다. 나머지 세 모듈 Manager와 표시 Controller는 기존과 같이 씬 진입점 내부에 직렬화된 일반 C# 객체다. 별도 Update를 갖는 컴포넌트로 일괄 변환하지 않았다.

씬 Manager의 Awake가 실행 데이터 → 피처맵 → UI → 환경 순서로 초기화한다. 활성 기간 이벤트는 OnEnable/OnDisable에서 대칭으로 연결·해제한다. LateUpdate는 환경의 복도 가시 상태를 확인한 다음 활성 모듈의 Tick을 호출한다. OnDisable은 상호작용 대상의 이벤트 구독을 해제하고 입력을 비활성화하며 후처리를 복원하고, OnDestroy는 생성한 텍스처·디버그 표시를 정리한다.

### 외부 연결과 체험 내부 공간

- 외부 학습 세션과의 계약은 `CNNFeatureMapLabManager`의 기존 `ILearningExperience`다. CNNFeatureMapLab 런타임 코드는 FactoryEnvironment의 Manager, 복도 위치, 방·바닥 구역 데이터를 직접 참조하거나 조회하지 않는다. 카메라와 입력은 기존 공용 XR 리그와 XRI를 사용한다.
- `Environment/`는 이 체험이 소유한 복도·바닥·문·카메라 효과다. FactoryEnvironment 모듈이 아니다. `CorridorCameraController`는 카메라 위치를 체험 루트의 로컬 좌표로 변환해 내부 범위를 판정한다. 이 판정에 따른 입력·UI·연출 갱신의 활성 조건을 유지한다.
- `FeatureMaps/Interaction/ZoneSelectionController`는 체험 프리팹의 바닥 5개 영역 선택을 구역 번호로 전달한다. `InteractionManager → FeatureMapsManager → CNNFeatureMapLabManager`의 이벤트 경로를 거쳐, 씬 Manager가 Environment의 바닥 강조와 FeatureMaps의 표시 강조를 연결한다. 강조 상태의 소유자는 `CorridorEnvironmentManager`이며, 구역 선택 자체가 학습 단계를 변경하지 않는다.
- 공장 측의 체험 로딩·배치와 체험 내부의 로컬 공간 판정은 별도 책임이다. 이 입력 정리에서는 Core와 FactoryEnvironment 코드·에셋 및 기존 공용 인터페이스를 변경하지 않는다.
### 실행 데이터와 전달 계약

- `ExecutionData/Digit1ExecutionData.cs`부터 `Digit3ExecutionData.cs`까지 입력 RGB, 레이어 출력, 대표 칸과 채널별 기여도, Pooling 창, 출력 비교 틀·점수·확률을 C# 배열에 저장한다. 기준은 커밋 `b14a2db`의 기존 복도 모델이다.
- `DigitExecutionData.cs`는 숫자마다 입력과 각 레이어의 결과 한 세트를 제공한다. 출력에는 후보 숫자 1·2·3의 점수와 확률, 비교 연출에 쓰는 채널별 기여도를 함께 저장한다.
- `FeatureMapSnapshot`은 입력·맵·밝기 기준·점수와 확률을, `RoundCalculationResult`는 연출에 필요한 대표 칸·기여도·출력 틀을 읽기 전용으로 전달한다. 표시 모듈은 픽셀 값을 텍스처로 표시하고 연출을 재생한다.
- Conv 카드와 훑기 판에는 커널 숫자 표기를 표시하지 않는다. 필터별 반응, 대표 칸의 채널별 기여도·편향·합과 ReLU는 저장 데이터로 설명한다.

### 피처맵 표시와 연출

- 입력·은닉층·출력 사이의 기본 연결선 38개는 제거했다. 필터를 펼칠 때의 입력·결과 연결선과 대표 영역 테두리는 각 연출에서 표시한다.

- `FeatureMapDisplayController`는 기본 입력판·맵·출력의 표시 변경과 복원을 소유한다. 라운드가 기본 패널을 바꿀 때 이 경로를 사용한다.
- `InputPanelController`, `FeatureMapPanelController`, `OutputStageController`는 각 표시·입력을 담당한다. 입력판과 중간 결과판·템플릿은 같은 패널 타입의 서로 다른 인스턴스다.
- `Round/PoolingRoundController`는 저장된 대표 영역과 최댓값을 기존 판에 표시한다. 선택 영역의 숫자는 기존 자막에 표시한다.
- `Round/FeatureMapRoundController`는 라운드 내부 단계·재생 시간을 소유하고, `FilterStackController`와 `RoundResultsController`가 카드·훑기·합치기 표시를 처리한다. 라운드 코드가 안내 UI를 직접 조작하지 않는다.
- 다음 단계는 사용자 입력으로만 진행한다. 현재 단계의 연출을 먼저 완료하고 다음으로 이동하며, 이전 버튼은 해당 단계를 다시 재생한다.

### 입력 연결

- 상호작용 대상은 기존 Core XR 리그의 오른손 레이와 XRI의 활성화·호버 이벤트를 사용한다. 레이 거리·색상·햅틱은 공용 설정을 따른다. UI는 기존 XRUIInputModule과 TrackedDeviceGraphicRaycaster를 사용한다.
- InteractionManager는 복도 안에서만 선택용 XRSimpleInteractable을 활성화한다. 피처맵 판 8개, 필터 5개, 출력 3개, 바닥 구역 5개가 기존 Collider를 사용한다. Trigger로 판을 선택한다.
- 필터를 빠르게 두 번 누르면 접힌다. 바닥 선택은 구역을 강조하고 진행 단계를 유지한다.
- 숫자 변경은 실행 데이터에서 해당 입력과 결과를 선택한다. Conv와 Pooling 설명은 선택한 데이터의 대표 영역을 사용한다. 완료 후에도 숫자를 바꿀 수 있다. 선택 라운드를 마치면 다음 필수 라운드의 안내로 돌아간다.
- 버튼 구독은 해당 Controller가 소유하는 `CorridorButtonBindings`로 연결·해제한다. 완료 팝업의 확인·닫기는 기존 WorldUIPopupController가 담당한다.

### 안내 UI·환경·표시 보조

- `LessonContent`는 학습 설명 데이터, `LessonUIManager`는 상황에 맞는 설명 선택, SubtitlePanel/HintBubbles/CompletionPopup Controller는 실제 표시를 담당한다.
- `LessonUI/CorridorDebugController`는 에디터·Development Build의 왼손 위 HUD와 화면 진단을 담당한다. Inspector의 `Lesson UI > Debug > Show Debug`에서 켠다. 씬·라운드 상태 및 포인터·셀·클릭·UI 정보를 값으로 전달받으며 릴리스에서는 진단 표시를 제외한다.
- `CorridorCameraController`는 단독 씬 설정, 복도 안팎 판정, 후처리 적용·복원을 담당한다. `CorridorDoorController`는 완료 요청을 받아 문을 연다.
- `Presentation/CorridorRenderUtility`는 색·텍스처·빌보드·인스턴싱 표시 보조만 담당한다. 입력이나 런타임 자원 수명을 소유하지 않는다. 각 모듈은 자기 표시 객체의 빌보드 목록을 갖는다.
- `CorridorTheme`는 기존 Settings/CorridorTheme.asset을 사용한다. 동적 표시 이름은 `FeatureMapLabels`, 정수 퍼센트 배분은 `PercentageFormatter`가 담당한다. 고정 문구·기본 배치는 기존 프리팹에 남아 있다.

## 실행 데이터와 에셋

현재 배치는 입력 16×16×3, 첫 Conv 16×16×2, MaxPooling 8×8×2, 두 번째 Conv 8×8×4, 출력 숫자 1·2·3을 기준으로 한다. 데이터를 갱신할 때는 입력, 레이어 결과, 표시 스케일과 설명에 표시할 대표 칸·중간 결과를 함께 맞춘다. 씬 진입점이 선택한 숫자의 저장 결과를 가져오므로 씬에 모델 프리팹을 연결할 필요가 없다.

씬은 `Scenes/CNNFeatureMapLab.unity`, 프리팹은 `Prefabs/CNNFeatureMapLab/`에 유지한다. 재질·텍스처·메시는 각 종류의 CNNFeatureMapLab 폴더, 폰트는 Fonts/CNNFeatureMapLab, 후처리는 Settings/CorridorPostProcessing.asset을 그대로 사용한다. 원본을 재생성하거나 복제 보관하지 않았다.

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
