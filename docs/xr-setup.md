# 공용 XR 리그와 태블릿 구현

공용 리그는 [XRPlayerRig.prefab](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)에, 런타임 코드는 [Core/XR](../client/Assets/Scripts/Core/XR)와 [Core/Tablet](../client/Assets/Scripts/Core/Tablet)에 있다. 버튼과 키 사용법은 [통합 조작법](xr-simulator.md)을 참고한다.

## XR 파일과 역할

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [PersistentXRPlayerRig](../client/Assets/Scripts/Core/XR/PersistentXRPlayerRig.cs) | 가장 먼저 깨어난 리그를 `DontDestroyOnLoad`로 유지하고, 새 씬에서 생성된 중복 리그는 카메라·입력이 동작하기 전에 비활성화·제거한다. 유지할 리그에 공통 입력 컴포넌트가 없으면 추가한다. 카메라·양손·태블릿·EventSystem을 포함한 리그 전체가 씬 이동 뒤에도 같은 인스턴스로 남는다. |
| [XRPlayerInputController](../client/Assets/Scripts/Core/XR/XRPlayerInputController.cs) | 오른손 입력을 시점 회전·정면 복원·레이 전환으로 바꾸고 Trigger를 uGUI 클릭에 연결한다. 씬 전환이나 `EnsureRayReady()` 호출 시 UI 모듈 등록이 끊겼으면 복원한다. Grip을 누른 동안 오른손 레이가 가리킨 대상을 잡도록 설정하며 물체의 이동은 XRI의 오른손 부착점을 따른다. 이야기 중 입력 제한과 입구 안내의 ray 유지도 반영하며 비활성화 시 변경한 레이 설정을 반환한다. |
| [XREditorSimulator](../client/Assets/Scripts/Core/XR/XREditorSimulator.cs) | 에디터에서 가상 HMD·컨트롤러를 생성하고 키보드·마우스 액션을 XRI 시뮬레이터에 연결한다. 위치·회전과 버튼·스틱 입력을 가상 XR 기기 상태로 바꾸므로 공용 리그가 실제 컨트롤러와 같은 입력 경로로 읽는다. 컴포넌트의 활성·비활성·파괴에 맞춰 기기와 액션의 수명을 관리한다. |

### 리그 초기화와 수명

1. `PersistentXRPlayerRig.Awake()`가 먼저 실행되어 기존 리그를 확인한다. 중복 인스턴스는 즉시 비활성화한 뒤 파괴한다.
2. 유지할 리그는 부모에서 분리하고 `DontDestroyOnLoad`로 등록한다. 프리팹의 활성 자식 `EventSystem`에 EventSystem과 XRUIInputModule이 미리 구성되어 있으며 리그와 함께 유지된다. `XRPlayerInputController`가 없으면 추가한다.
3. `XRPlayerInputController.Awake()`는 `XROrigin`, Camera Offset, 시작 정면과 입력 액션을 준비한다. `EnsureRayReady()`는 오른손 `XRRayInteractor`와 카메라를 찾아 연결한다.
4. 새 씬의 중복 리그는 `PersistentXRPlayerRig.Awake()`에서 먼저 비활성화하므로 자식 EventSystem도 활성 입력 환경으로 등록되지 않는다. 기존 카메라·추적·태블릿·UI 입력 환경은 유지되며, 리그 위치 변경은 각 씬의 진행 코드에서 처리한다.
5. `sceneLoaded`와 `EnsureRayReady()`는 오른손 ray의 UI 모듈 등록을 확인하고, 필요한 경우 인터랙터나 잡기 상태를 끊지 않고 다시 연결한다. 체험 씬을 언로드해도 공용 EventSystem과 XRUIInputModule은 제거되지 않는다.

`SubsystemRegistration`에서 정적 리그 참조를 초기화하므로 도메인 재로드를 끈 에디터 실행에서도 이전 참조를 재사용하지 않는다.

### 공용 UI 입력 환경

UI 입력 환경의 소유자는 공용 `XRPlayerRig`다. EventSystem과 XRUIInputModule은 프리팹에 미리 배치되어 있으며, 씬별 준비 함수나 패키지의 자동 생성에 의존해 구성하지 않는다. 리그의 초기 중복 제거는 실행 순서 -10000에서, EventSystem은 -1000에서 처리한다. `XRPlayerInputController.Start()`는 준비된 환경에 오른손 ray와 UI Trigger를 연결한다.

픽셀 관찰·합성곱에는 별도 UI 입력 환경의 생성·해제 호출이 없다. 모델 설계는 슬롯의 `SlotSettingsController`가 XRI 활성화 이벤트를 직접 받고 오른손·잡기 상태·UI 우선순위에 따른 입력 제한을 처리한다. 오프닝·엔딩의 `CinematicMovementController`는 연출 중 이동·잡기와 레이 표시를 제어한다. `CinematicInputController`는 트리거를 2초간 눌러 건너뛰는 입력을 처리한다. 이들 씬은 EventSystem·XRUIInputModule을 생성·삭제하거나 공용 UI Trigger 바인딩을 교체하지 않는다.

클릭 전달은 `XRRayInteractor → XRUIInputModule → Button.onClick → 해당 UI Controller` 순서다. 각 Canvas는 XR용 TrackedDeviceGraphicRaycaster로 입력을 판정하며 버튼 콜백 연결은 각 UI에 둔다. 픽셀 관찰·합성곱 씬과 합성곱 안내 프리팹, 출력 위치 퀴즈의 런타임 선택 영역도 같은 구성을 사용한다. ScreenLabBuilder와 ConvolutionLabBuilder는 이 구성을 생성한다. 태블릿의 왼손 스틱 탐색·X 선택은 기존 `TabletInputController`가 담당한다. 공용 리그를 배치한 씬의 단독 실행도 동일한 프리팹을 사용한다. 공용 리그가 없는 `CNNConvolutionLab`과 `FactoryTutorial`은 공장에 로드되어 유지 중인 리그를 사용한다.

### 추적과 이동

`XRPlayerRig`는 `XROrigin`, Camera Offset, Main Camera, 양손 추적 오브젝트와 `XRInteractionManager`로 구성된다. `TrackedPoseDriver`가 HMD와 컨트롤러의 위치·회전을 갱신한다. 컨트롤러 레이는 OpenXR의 aim pose를 우선 읽고, 해당 입력이 없으면 device pose로 대체한다. 손에 표시되는 오브젝트는 grip pose를 사용한다.

수평 이동은 `ContinuousMoveProvider → LocomotionMediator → XRBodyTransformer`가 처리한다. 이동 기준은 카메라의 전방이며 공용 스틱 이동 속도는 5m/s다. 리그 프리팹과 `XRProjectSetup`의 생성 기본값에 같은 속도를 적용한다. 비행·중력은 사용하지 않는다. 리그의 CharacterController와 XRBodyTransformer의 충돌 이동을 사용해 스틱 이동 중 벽을 통과하지 않게 한다. 캡슐 반경은 0.2m이며 높이와 중심은 XRI가 HMD 위치에 맞춰 갱신한다. 실제 사용자가 몸을 움직이는 물리적 이동은 제한하지 않는다. 왼손 이동 액션은 리그 프리팹에 직렬화되어 있다. 에디터 FPS 모드의 WASD 몸 이동 속도도 5m/s다. 시뮬레이터의 축 이동 속도 0.2에 몸 이동 배율 25을 적용하며, 손 조작 모드의 축 이동 속도는 0.2로 유지한다.

`XRPlayerInputController`는 수평 회전을 리그에, 수직 회전을 Camera Offset에 적용한다. `_turnSpeed`는 회전 속도, `_maximumPitch`는 스틱으로 추가하는 수직 회전의 한계다. `RestoreFrontView()`는 카메라 위치를 유지하며 시작 정면과 초기 Camera Offset 회전을 복원한다.

### 레이와 잡기

- `BindRay()`는 오른손 레이에 Trigger 기반 UI 입력을 연결하고, 가까운 대상 우선 처리와 `State` 선택 방식을 설정한다. Grip을 먼저 누른 뒤 레이가 잡을 수 있는 대상에 닿아도 선택이 시작되며, Grip을 놓으면 해제한다.
- `XRGrabInteractable`은 XRI의 오른손 레이와 부착점으로 잡는다. 잡은 물체는 잡기 시작한 거리에서 오른손의 위치·방향 변화를 따라 이동한다.
- 레이 계산과 선 표시 모두 오른손을 기준으로 유지한다. 잡는 동안에도 오른손 스틱은 시점을 회전시키며, XRI의 부착점 거리 조절은 비활성화한다.
- 레이 비활성화는 선 표시와 인터랙터를 함께 끄므로 현재 선택도 해제된다.
- `TutorialInputSuppressed`는 공통 시점·레이 전환 입력 처리를 막고, `TutorialRayForcedOn`은 튜토리얼 동안 레이를 켠 상태로 유지한다. 왼손 Grip은 앱을 종료하지 않으며, 태블릿 설정의 앱 종료 확인창에서 종료한다. 사용 흐름은 [오프닝·엔딩 구현](cnn-documentary-cinematics.md)에 있다.

`OnDisable()`은 씬 로드 이벤트 구독과 입력 액션을 해제하고 변경했던 레이 설정을 복원한다. `OnDestroy()`는 InputAction을 해제한다.

공용 UI 입력 환경의 프리팹 배치와 씬별 중복 준비 제거는 소스·직렬화 참조·수명 경로를 정적으로 검토했다. Unity 실행·임포트·컴파일·Play·테스트·빌드는 수행하지 않았으며, 실제 씬 전환과 컨트롤러 입력 동작은 실행 확인 전이다.

## 태블릿 파일과 역할

태블릿은 상단 학습 진행률과 왼쪽의 지도·학습 단계·양피지 앱으로 구성한다. 4:3 화면(1200×900, 월드 폭 0.3m)에 진주색 바탕, 금속 테두리와 짙은 베젤, 대비가 있는 글자·선택 표시를 사용한다. 기존 왼손 부착 위치와 조작 입력은 유지한다. 기본 설정은 설정 버튼 또는 Menu로 연다.

고정 버튼·캡션·카드·빈 목록 안내는 `Prefabs/Common/TabletUI.prefab`에 배치되어 있고 `XRPlayerRig.prefab`의 TabletManager가 해당 프리팹을 참조한다. `TabletUIBuilder`는 MonoBehaviour로 직렬화된 화면 참조를 연결하고 표면 스프라이트·앱 표시를 설정한다. 실행 중 화면 계층을 코드로 재작성하지 않는다. 기존 Runtime UI Preview와 Background는 리그에서 비활성화했다.

| 파일·클래스 | 역할 |
| --- | --- |
| TabletManager | 태블릿 모듈 진입점. 앱·설정·종료 확인창, 입력 중단·복원, 학습 스냅샷 표시와 이동 요청, 공용 양피지 열람 연결을 조율한다. |
| TabletUIBuilder | 저장된 프리팹의 UI 참조·버튼 콜백·표면 스타일·앱 선택 표시를 연결한다. |
| TabletInputController | 기존 Trigger+스틱 탐색, X 선택, Y 뒤로, Menu 설정. 비활성 항목을 건너뛰며 포커스 테두리를 표시한다. |
| TabletLearningController | Core 세션의 읽기 전용 LearningProgressSnapshot으로 현재 단계·완료 표시·진행률·이동 버튼을 갱신한다. 이동 시작·실패·완료 안내 문구와 표시용 스냅샷 캐시만 소유하며, 완료 기록을 변경하거나 씬을 로드하지 않는다. |
| TabletParchmentController | 공용 수집 상태를 조회해 보유 문서만 빈틈 없이 목록에 표시하고 선택 시 재열람을 요청한다. |
| TabletSettingsController | 밝기·음량·진동·선택 문서 인덱스를 기존 PlayerPrefs 키로 읽고 저장한다. |
| TabletDetailController | 앱 종료 확인창의 표시·닫기. |
| TabletMinimapController | 플레이어를 중심에 두고 주변 세로 22m의 바닥·벽·미수집 양피지·바라보는 방향을 0.1초 간격으로 갱신한다. 전체 지도 화면은 사용하지 않는다. |
| TabletVisualResources | 둥근 표면·금속 그라데이션과 4:3 본체 메시의 생성·정리. |
| [TabletMapData](../client/Assets/Scripts/Core/Tablet/TabletMapData.cs) | 공장 판독 결과인 바닥·벽·지도 범위를 읽기 전용 목록과 경계로 묶는다. 양피지는 원본 Behaviour와 문서 인덱스를 보관해 현재 위치·활성 상태를 조회하게 한다. 공장 코드가 이 데이터를 게시하면 태블릿은 씬의 이름 규칙을 알지 않아도 지도를 그릴 수 있다. |

### 학습과 이동 연결

학습 카드의 순서와 요청 ID는 Core LearningStageCatalog.ProgressStages에 연결한다. TabletUI 프리팹에는 픽셀 관찰·합성곱·특징 맵·모델 제작 네 카드의 제목 Text를 배치한다. 공통 LearningStageId를 쓰며 UI 배열 인덱스를 학습 단계로 전달하지 않는다. 전체 진행률은 Core가 Stages의 튜토리얼 포함 다섯 체험별 최대 도달 진행률을 평균 낸 값이다. ProgressStages는 기존 네 카드의 연결용 목록으로 유지하며, 태블릿 코드·프리팹은 수정하지 않는다. 양피지 수는 학습 진행률에 합산하지 않는다. 태블릿은 StageMoveRequested(LearningStageId) 로컬 C# 이벤트로 이동 의도를 알린다. FactoryEnvironmentManager가 요청을 받아 이동 가능 여부를 판단하고 플레이어를 이동시킨다. FactoryAreaController의 LateUpdate 구역 판정에서 발생한 AreaChanged만 로더가 받아 이전·현재·다음 씬을 준비한다. LearningSessionManager는 요청 기준으로 문서를 지급하고 로더 상태와 공장의 이동 결과를 받아 태블릿을 갱신한다. 결과 상태는 SetLearningState(LearningProgressSnapshot), 이동 안내는 ShowTravelStarted/ShowTravelFailed/ShowTravelCompleted로 표시한다. 세션 연결이 없으면 빈 스냅샷을 표시하고 이동 카드는 비활성화한다.

네 체험 모두 완료 여부와 무관하게 선택할 수 있다. 로딩 중에는 이동을 시작하지 않으며 로드된 체험으로 이동하면 상태를 유지하며 언로드된 체험은 새로 로드한다. 누적 완료는 공장 씬 수명의 LearningSessionManager가 소유한 LearningProgress에 보관한다. 같은 공장 세션 안의 재방문·초기화·태블릿 비활성화는 기록을 지우지 않으며, 공장 씬 종료 시 폐기되고 재진입 시 새로 시작한다. 태블릿에는 수정할 수 없는 스냅샷만 전달한다. 공용 양피지 기록의 기존 영속 수명은 별개로 유지한다. 이동만으로 완료 처리하지 않는다. ScreenLab과 ConvolutionLab은 각각 독립된 체험이며, 같은 구역을 다시 선택하면 도착점으로 이동하며 체험을 재시작하지 않는다.

### 포커스·양피지·설정

포커스 우선순위는 종료 확인 → 설정 → 앱 화면이다. 메인 그룹에 앱 버튼·단계 카드·수집 목록·설정 버튼을 등록한다. 앱 전환 후 해당 앱 버튼으로 포커스를 돌린다. Trigger 탐색이 끝나고 스틱이 중앙으로 돌아와야 원래 이동 입력을 복원한다. 양피지 열람 동안 태블릿 입력은 중단하며 닫은 다음 프레임부터 기존 조작 상태·포커스를 복원한다.

수집 전 문서는 목록에 표시하지 않는다. 문서가 없으면 빈 목록 안내를 표시하며, 수집된 문서는 기존 `ParchmentReader.prefab`으로 다시 읽는다. 문서 본문·수집 여부는 공용 ParchmentManager와 ParchmentCatalog가 소유한다. `Tablet.Brightness`, `Tablet.Volume`, `Tablet.Vibration`, `Tablet.Inventory` 저장 키를 유지한다. 이전 `Tablet.Stage` 값은 학습 완료 상태로 사용하지 않는다. 학습 진행률과 양피지 수집 기록은 디스크에 저장하지 않는다.

지도 형상은 공장 씬의 TabletMiniMapController가 생성하고 자체 생명주기에서 TabletManager의 SetMapData/ClearMapData를 직접 호출한다. 이전 씬의 해제가 새 지도를 지우지 않도록 ClearMapData는 동일한 데이터만 해제한다.

### 뒷면 로고와 확인 범위

제공된 원본은 `Textures/Common/KDHuntersLogo.png`에 내용 변경 없이 저장하고 리그의 Tablet Rear Logo에서 참조한다. `Shaders/Common/TabletLogo.shader`와 `Materials/Common/TabletLogo.mat`가 원본의 흰색을 곱셈 혼합의 중립색으로 처리해 청회색 본체 위에 잉크처럼 표시한다. 양쪽 눈의 인스턴싱 변환을 포함한다. 로고·폰트 및 이전 이미지 아이콘 기록은 ASSET_CREDITS.md에 있다. 현재 앱·양피지 목록 아이콘은 TabletAppIconGraphic이 그리는 평면 도형이며 이미지 텍스처·그라데이션·광택·그림자를 사용하지 않는다. 지도는 위치 핀, 학습 단계는 네 칸, 양피지는 선으로 된 문서 모양이다.

2026-10-31, Core 학습 세션 분리 이전 기록: 당시 소스·직렬화 참조·에셋 경로를 정적으로 점검했다. 사용자 허가로 Unity를 열고 코루틴 안의 ReadOnlySpan 반복문 컴파일 오류를 일반 메서드로 분리해 수정했다. 일반 에디터 모드와 FactoryEnvironment Play에서 진행률·앱 아이콘·주변 미니맵 표시를 확인했다. 당시 설정 버튼의 글자 잘림을 발견했으며 미수정 상태다. 이후 평면 아이콘으로 교체했고 이 교체 후 화면은 재확인하지 않았다. 앱 전환·텔레포트·재진입·뒷면 로고·Quest 실기기 및 빌드는 확인하지 않았다.

공장 씬의 TabletMiniMapController는 생성한 TabletMapData를 TabletManager에 직접 전달한다. ClearMapData(data)는 현재 지도와 전달된 데이터가 같을 때만 지운다. 주변 지도는 XZ 평면을 +Z 위쪽으로 투영하고 카메라 위치·방향과 수집 상태를 주기적으로 갱신한다.

## 에디터 시뮬레이터

[XREditorSimulator.prefab](../client/Assets/Prefabs/XR/XREditorSimulator.prefab)은 [XRDeviceSimulatorSettings.asset](../client/Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset)에 연결되어 에디터에서 자동 생성된다.

`XREditorSimulator.Awake()`는 비활성 자식 오브젝트에 `SimulatedDeviceLifecycleManager`와 `XRInteractionSimulator`를 추가한다. 이동·회전·버튼 액션을 코드에서 구성하며, `OnEnable()`에서 액션과 가상 기기를 활성화한다. `OnDisable()`에서 비활성화하고 `OnDestroy()`에서 액션을 해제한다. 가상 컨트롤러 입력은 실제 기기와 같은 리그 입력 경로로 전달된다.

태블릿은 에디터 시뮬레이터와 실기기 모두 같은 왼손 grip 기준 위치·크기를 유지한다. 태블릿 조작 시작·종료에 따라 태블릿만 따로 이동하거나 축소하지 않는다.

## 프로젝트 설정 코드

Unity 버전은 [ProjectVersion.txt](../client/ProjectSettings/ProjectVersion.txt), 패키지 버전은 [manifest.json](../client/Packages/manifest.json)과 [packages-lock.json](../client/Packages/packages-lock.json)에서 관리한다. XR 구성은 Unity 6000.3.24f1, OpenXR 1.16.1, XR Plug-in Management 4.5.3, XRI 3.3.2를 사용한다.

| 파일·메서드 | 역할 |
| --- | --- |
| [XRProjectSetup.ConfigureProject](../client/Assets/Editor/XRProjectSetup.cs) | Android·Standalone OpenXR 로더, 컨트롤러 프로필, Android 실행·렌더링 설정을 구성한다. 메뉴는 `Tools > XR > Configure Project`다. |
| `XRProjectSetup.ScheduleStartupScene` / `OpenStartupScene` | 에디터 세션에서 한 번, 저장 경로·변경사항·루트 오브젝트가 없는 빈 씬이면 `CNNArchiveOpening`를 연다. |
| `XRProjectSetup.AddLeftStickMovement` | 기존 리그 프리팹에 이동 컴포넌트를 구성한다. `ContinuousMoveProvider`가 있으면 기존 구성을 사용한다. |
| `XRProjectSetup.ValidateProject` / `ValidateRig` | OpenXR 설정, 시작 씬 `CNNArchiveOpening`의 존재 여부, 카메라·리그·태블릿·레이 참조를 검사한다. |
| [QuestSetup.Configure](../client/Assets/Editor/QuestSetup.cs) | `Tools > CNN > Configure Quest VR`를 `XRProjectSetup.ConfigureProject()`에 연결한다. |

`ConfigureProject()`는 Android 자동 XR 초기화, IL2CPP·ARM64, 최소 API 32, Target API 자동 선택, Vulkan·Linear·Single Pass Instanced를 적용한다. Android에는 Meta Quest와 Touch Plus 프로필을 활성화하고 Mobile URP의 HDR을 끄며 MSAA를 4로 설정한다. Standalone 자동 XR 초기화는 시뮬레이터 사용을 위해 꺼 둔다.

## Quest 3S Touch Plus 표시

공용 `XRPlayerRig`의 `PersistentXRPlayerRig.CreateControllerVisual()`은 Meta XR Core 207.0.1의 `OVRControllerPrefab`을 참조한다. 양손 grip pose 아래의 손 모델과 FactoryTutorial 안내 패널의 확대 모델에 같은 패키지 프리팹을 사용한다. `OVRControllerHelper`가 좌우 컨트롤러의 표시와 조이스틱·버튼·트리거·그립 애니메이션을 갱신한다.

생성한 Touch Plus 모델의 본체 재질은 SDK의 `MetaQuestTouchPlusMATphong_left.mat` / `MetaQuestTouchPlusMATphong_right.mat`을 공유 참조한다. 공식 프리팹의 `MetaQuestTouchPlusLeft_Material` / `MetaQuestTouchPlusRight_Material` 슬롯만 교체하며 배터리 재질은 유지한다. 이 `.mat`들은 `Meta/Lit` 셰이더와 SDK 텍스처 GUID를 사용한다. 프리팹에 포함된 재질 FBX의 이전 `Assets/Oculus/VR/Textures` 경로에 의존하지 않는다. 모델·Animator·애니메이션 클립과 SDK 패키지 원본은 수정하지 않는다.

중복 제거를 통과한 공용 리그가 `Meta XR Runtime` 자식을 활성화한다. 이 자식의 `OVRManager`가 Meta 입력의 수명을 관리한다. OpenXR/XRI는 손 추적·이동·잡기·UI 입력을 처리한다. 공용 생성 메서드는 공식 `OVRControllerHelper`를 반환하며, 예전 기본 도형 컨트롤러와 이를 숨기던 코드는 제거했다. `Scripts/FactoryTutorial/TouchPlusHandController`는 튜토리얼의 확대 모델에만 추가한다. 튜토리얼의 `SetCue()` 호출을 받아 Touch Plus 모델에 표시를 붙이고 기존 Unity XR 입력으로 수행 여부를 확인한다. 오프닝은 이 컴포넌트를 참조하지 않으며 기존 별도 손 안내 표식을 사용한다. 에디터에서 XR 장치가 없을 때는 정적인 Touch Plus 모델을 표시한다.

과거 SDK 제거 시 분리했던 표시용 에셋 12개는 패키지 원본으로 전환하고, 이전 추출본과 라이선스 사본 폴더는 삭제했다. 현재 사용하는 SDK 패키지에 포함된 원본 라이선스·고지는 유지한다. 이번 전환은 소스·직렬화 참조를 검토했으며, Quest 3S에서 재질·왼손 애니메이션·튜토리얼 강조·Reality 진입 및 복귀의 실행 확인이 남아 있다.

과거 정적 검토에서 공식 207.0.1 왼손 Animator의 클립 fileID 18개가 FBX의 `internalIDToNameTable`과 일치하지 않았다. 실제 임포트 시 참조가 해결되는지는 실행 확인 항목에 포함한다.

## 과거 기록: Touch Plus 모델 패키지 설치 (2026-10-26)

당시 Meta 공식 registry `https://npm.developer.oculus.com`에서 `com.meta.xr.sdk.core` 207.0.1을 설치했다. registry 범위는 해당 패키지로 제한했고, 의존 패키지 XR Hands 1.7.3은 Unity가 해결했다. 기존 OpenXR/XRI 버전과 리그 입력 연결은 유지했다.

당시 모델은 `Packages/com.meta.xr.sdk.core/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Left.fbx`, `MetaQuestTouchPlus_Right.fbx`에 있었고, Animation 하위 폴더의 좌·우 Animator Controller를 사용했다. 설치 시점에는 패키지 원본을 Assets에 복제하거나 수정하지 않았다.

Unity 6000.3.24f1에서 설치·가져오기·컴파일 완료를 확인했다 (`isCompiling=false`, `isUpdating=false`, `scriptCompilationFailed=false`). 좌·우 모델 로드 및 왼손 버튼·Grip·Trigger·8방향 스틱 애니메이션 목록을 확인했다. Unity는 공식 패키지의 서명 파일 부재 경고를 출력했다. 다운로드 원본의 SHA 검증값은 Meta registry 값과 일치했다. Play·APK 빌드·기기 검증은 수행하지 않았으며 실제 리그 모델 교체와 튜토리얼 연결은 후속 작업이다.

## 과거 기록: 컨트롤러 표시와 실제 미션 연결 (2026-10-26)

`PersistentXRPlayerRig`의 직렬화된 좌·우 Touch Plus 모델 및 Animator 참조로 손의 grip pose 아래 실제 컨트롤러 모델을 생성한다. `TouchPlusHandController`가 Unity XR의 버튼·Grip·Trigger·스틱 값을 공식 Animator에 전달한다. 오른손 Meta 시스템 버튼은 앱 입력으로 가로채지 않는다. 패키지 모델·애니메이션 원본은 수정하지 않고 표시용 런타임 재질과 튜토리얼 표식만 별도로 생성·정리한다.

당시 `TabletLearningController`의 고정 75% 예시 진행률과 확인 버튼으로 임의 완료하던 동작을 제거했다. 당시의 `FactoryLearningFlowManager`가 양피지 보유 수와 방 체험 완료 수를 전달해 8개 목표 기준 비율을 표시했다. 단계 선택은 이동 안내였으며 실제로 걸어서 방에 들어갔다. 이 동작은 이후 Core 학습 세션과 자유 이동으로 교체되었으며, 현재 이동 카드는 ScreenLab·ConvolutionLab·FeatureMap·ModelDesign 네 체험이며, 전체 진행률은 Tutorial까지 포함한 다섯 체험을 기준으로 한다.

SDK가 설치된 당시의 후속 컴파일·Play에서 손 모델 2개와 안내용 확대 모델 2개의 생성, 카메라 렌더 표시 및 실제 수집 상태에 따른 태블릿 갱신을 확인했다. 당시에는 공장 씬에서 별도 마우스 미리보기를 실행하지 않아 공용 XR 리그를 유지했다. 현재는 픽셀 씬 전용 마우스 미리보기와 설치 메뉴를 제거했으며 공용 XR 시뮬레이터를 사용한다. Quest 버튼 입력과 애니메이션의 실기기 일치는 아직 확인하지 않았다.

## 공용 양피지 연결

공장 Pickup과 태블릿은 공용 ParchmentManager를 사용한다. 양피지는 클릭 즉시 수집되며, 태블릿은 Collected 이벤트와 재활성화 시 보유 목록을 갱신한다. 문서 소속·지급은 Core LearningStageCatalog의 ParchmentIds, 바깥문 입장은 EntranceParchmentIds를 따르고 지도는 실제 Pickup 위치를 표시한다. 세션·입력·수집 조건은 [공용 양피지](parchment.md)에 정리되어 있다.

양피지 데이터는 ParchmentPage(본문·삽화), ParchmentLesson(한 문서·페이지 목록), ParchmentCatalog(전체 데이터·ID 조회)로 분리한다. 실제 데이터 편집 위치는 [ParchmentCatalog](../client/Assets/Scripts/Core/Parchment/ParchmentLesson.cs)다.

## Quest 빌드의 중복 기능 설정 정리 (2026-10-30)

프로젝트의 활성 OpenXR 설정은 OpenXRPackageSettings.asset이다. 기존 OpenXR Package Settings.asset의 중복 MetaQuestFeature 하위 에셋 때문에 OpenXR 1.16.1 빌드 훅의 단일 기능 검색이 실패했다. 비활성 설정 파일의 중복 하위 에셋만 제거하고 활성 설정의 MetaQuestFeature를 공유 참조하도록 연결했다. 폴더·파일 위치와 활성 프로젝트 설정은 유지한다.

XRPlayerRig에는 공용 AudioManager 컴포넌트와 Tablet Rear Logo를 함께 유지한다. 태블릿의 이동 안내는 TabletManager의 ShowTravelStarted/ShowTravelFailed/ShowTravelCompleted를 통해 TabletLearningController가 표시한다.

튜토리얼을 포함한 다섯 체험의 ILearningExperience 중 Progress·ProgressChanged와 EndSession은 Core 세션이 기록·종료에 사용하고, CanExit·ExitAvailabilityChanged는 세션이 구독하여 공장에 전달하고 문 개방 판단에 사용한다. LearningProgress는 체험별 최대 도달 진행률을 보관하며 1에 도달하면 완료를 기록한다. 전체 진행률은 LearningStageCatalog.Stages에 등록한 다섯 체험의 동일 비중 평균이다. 재방문·초기화로 누적 기록은 감소하지 않는다. 튜토리얼도 공통 ProgressChanged·ExitAvailabilityChanged 이벤트와 EndSession을 사용하며 최종 확인 후 진행률이 0에서 1로 바뀐다. 상태 스냅샷은 TabletManager로 전달하고 Factory 지도 이벤트 연결은 LearningSessionManager가 담당한다.

TabletManager와 TabletLearningController는 누적 완료 집합을 소유하지 않는다. Core 세션의 스냅샷을 표시하며 같은 단계의 중복 완료·일시적 진행 불가·이전 체험 복귀에 대한 기록 정책은 Core LearningProgress가 처리한다. 공장 씬 종료 시 세션은 폐기되고 태블릿에는 LearningProgressSnapshot.Empty를 전달한다. 이번 Core 분리 변경에 대한 Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.
