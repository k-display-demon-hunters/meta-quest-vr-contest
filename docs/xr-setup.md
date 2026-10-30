# 공용 XR 리그와 태블릿 구현

공용 리그는 [XRPlayerRig.prefab](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)에, 런타임 코드는 [Core/XR](../client/Assets/Scripts/Core/XR)와 [Core/Tablet](../client/Assets/Scripts/Core/Tablet)에 있다. 버튼과 키 사용법은 [통합 조작법](xr-simulator.md)을 참고한다.

## XR 파일과 역할

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [PersistentXRPlayerRig](../client/Assets/Scripts/Core/XR/PersistentXRPlayerRig.cs) | 가장 먼저 깨어난 리그를 `DontDestroyOnLoad`로 유지하고, 새 씬에서 생성된 중복 리그는 카메라·입력이 동작하기 전에 비활성화·제거한다. 유지할 리그에 공통 입력 컴포넌트가 없으면 추가한다. 카메라·양손·태블릿을 포함한 리그 전체가 씬 이동 뒤에도 같은 인스턴스로 남는다. |
| [XRPlayerInputController](../client/Assets/Scripts/Core/XR/XRPlayerInputController.cs) | 오른손 입력을 시점 회전·정면 복원·레이 전환으로 바꾸고 Trigger를 uGUI 클릭에 연결한다. 씬 전환이나 `EnsureRayReady()` 호출 시 UI 모듈 등록이 끊겼으면 복원한다. Grip을 누른 동안 오른손 레이가 가리킨 대상을 잡도록 설정하며 물체의 이동은 XRI의 오른손 부착점을 따른다. 이야기 중 입력 제한과 입구 안내의 ray 유지도 반영하며 비활성화 시 변경한 레이 설정을 반환한다. |
| [XREditorSimulator](../client/Assets/Scripts/Core/XR/XREditorSimulator.cs) | 에디터에서 가상 HMD·컨트롤러를 생성하고 키보드·마우스 액션을 XRI 시뮬레이터에 연결한다. 위치·회전과 버튼·스틱 입력을 가상 XR 기기 상태로 바꾸므로 공용 리그가 실제 컨트롤러와 같은 입력 경로로 읽는다. 컴포넌트의 활성·비활성·파괴에 맞춰 기기와 액션의 수명을 관리한다. |

### 리그 초기화와 수명

1. `PersistentXRPlayerRig.Awake()`가 먼저 실행되어 기존 리그를 확인한다. 중복 인스턴스는 즉시 비활성화한 뒤 파괴한다.
2. 유지할 리그는 부모에서 분리하고 `DontDestroyOnLoad`로 등록한다. `XRPlayerInputController`가 없으면 추가한다.
3. `XRPlayerInputController.Awake()`는 `XROrigin`, Camera Offset, 시작 정면과 입력 액션을 준비한다. `EnsureRayReady()`는 오른손 `XRRayInteractor`와 카메라를 찾아 연결한다.
4. 새 씬의 리그가 제거되어도 기존 카메라·추적·태블릿 상태는 유지된다. 리그 위치 변경은 각 씬의 진행 코드에서 처리한다.
5. `sceneLoaded`와 `EnsureRayReady()`는 오른손 ray가 현재 UI 모듈에 등록되어 있는지 확인한다. 이전 씬의 EventSystem이 파괴되어 등록을 잃었으면 다시 연결한다. 공장 안내는 공용 EventSystem을 사용하며 `FactoryTutorial` 언로드 시 이 EventSystem은 유지한다.

`SubsystemRegistration`에서 정적 리그 참조를 초기화하므로 도메인 재로드를 끈 에디터 실행에서도 이전 참조를 재사용하지 않는다.

### 추적과 이동

`XRPlayerRig`는 `XROrigin`, Camera Offset, Main Camera, 양손 추적 오브젝트와 `XRInteractionManager`로 구성된다. `TrackedPoseDriver`가 HMD와 컨트롤러의 위치·회전을 갱신한다. 컨트롤러 레이는 OpenXR의 aim pose를 우선 읽고, 해당 입력이 없으면 device pose로 대체한다. 손에 표시되는 오브젝트는 grip pose를 사용한다.

수평 이동은 `ContinuousMoveProvider → LocomotionMediator → XRBodyTransformer`가 처리한다. 이동 기준은 카메라의 전방이며 공용 스틱 이동 속도는 5m/s다. 리그 프리팹과 `XRProjectSetup`의 생성 기본값에 같은 속도를 적용한다. 비행·중력은 사용하지 않는다. 리그의 CharacterController와 XRBodyTransformer의 충돌 이동을 사용해 스틱 이동 중 벽을 통과하지 않게 한다. 캡슐 반경은 0.2m이며 높이와 중심은 XRI가 HMD 위치에 맞춰 갱신한다. 실제 사용자가 몸을 움직이는 물리적 이동은 제한하지 않는다. 왼손 이동 액션은 리그 프리팹에 직렬화되어 있다. 에디터 FPS 모드의 WASD 몸 이동 속도도 5m/s다. 시뮬레이터의 축 이동 속도 0.2에 몸 이동 배율 25을 적용하며, 손 조작 모드의 축 이동 속도는 0.2로 유지한다.

`XRPlayerInputController`는 수평 회전을 리그에, 수직 회전을 Camera Offset에 적용한다. `_turnSpeed`는 회전 속도, `_maximumPitch`는 스틱으로 추가하는 수직 회전의 한계다. `RestoreFrontView()`는 카메라 위치를 유지하며 시작 정면과 초기 Camera Offset 회전을 복원한다.

### 레이와 잡기

- `BindRay()`는 오른손 레이에 Trigger 기반 UI 입력을 연결하고, 가까운 대상 우선 처리와 `State` 선택 방식을 설정한다. Grip을 먼저 누른 뒤 레이가 잡을 수 있는 대상에 닿아도 선택이 시작되며, Grip을 놓으면 해제한다.
- `XRGrabInteractable`은 XRI의 오른손 레이와 부착점으로 잡는다. 잡은 물체는 잡기 시작한 거리에서 오른손의 위치·방향 변화를 따라 이동한다.
- 레이 계산과 선 표시 모두 오른손을 기준으로 유지한다. 잡는 동안에도 오른손 스틱은 시점을 회전시키며, XRI의 부착점 거리 조절은 비활성화한다.
- 레이 비활성화는 선 표시와 인터랙터를 함께 끄므로 현재 선택도 해제된다.
- `TutorialInputSuppressed`는 공통 시점·레이 전환 입력 처리를 막고, `TutorialRayForcedOn`은 튜토리얼 동안 레이를 켠 상태로 유지한다. 왼손 Grip은 앱을 종료하지 않으며, 태블릿 설정의 앱 종료 확인창에서 종료한다. 사용 흐름은 [오프닝·엔딩 구현](factory-intro-ending.md)에 있다.

`OnDisable()`은 씬 로드 이벤트 구독과 입력 액션을 해제하고 변경했던 레이 설정을 복원한다. `OnDestroy()`는 InputAction을 해제한다.

안내 버튼 입력 누락은 정적 검토에서 persistent ray가 파괴된 이전 씬의 UI 모듈을 참조하는 경로로 진단했다. 등록 복원 코드는 반영했으나 사용자 지시에 따라 Unity 실행·테스트·빌드·컴파일 없이 소스만 검토했으므로 실제 컨트롤러에서의 복구 여부는 실행 확인 전이다.

## 태블릿 파일과 역할

태블릿 코드는 `Tablet` 네임스페이스를 사용한다. `TabletManager`가 일반 C# Controller 객체를 생성하고, `TabletMinimapController`는 uGUI 그래픽 컴포넌트로 동작한다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [TabletManager](../client/Assets/Scripts/Core/Tablet/TabletManager.cs) | 기능별 Controller와 UIBuilder를 생성하고 저장된 설정·선택 인덱스를 복원한 뒤 화면을 구성한다. 입력의 화면 전환 요청을 받아 상세·설정·단계 목록의 우선순위와 뒤로 가기를 조율한다. 공장의 수집·지도 요청은 해당 Controller로 넘기고 단계 이동·저장·종료는 Inspector의 UnityEvent로 외부에 알린다. |
| [TabletInputController](../client/Assets/Scripts/Core/Tablet/TabletInputController.cs) | 왼손 버튼·스틱을 읽고 현재 화면의 활성 항목 중 입력 방향에 있는 다음 항목을 찾는다. 선택은 버튼 콜백 실행이나 슬라이더 조절로 바꾸며, 화면 전환·뒤로 가기는 Manager에 요청한다. Trigger 탐색 중에는 이동 입력을 0으로 교체하고 해제 후 스틱 중앙 복귀 또는 비활성화 시 원래 입력을 복원한다. |
| [TabletParchmentController](../client/Assets/Scripts/Core/Tablet/TabletParchmentController.cs) | 문서 인덱스별 수집 여부, 선택 문서, 현재 페이지와 예시 표시 상태를 보관한다. 수집 요청은 중복·범위를 검사한 뒤 인벤토리에 반영하고, 항목 선택은 `ParchmentLesson`의 본문과 페이지 버튼을 갱신한다. 문서 닫기 시 피드백 화면과 포커스를 복원하며 수집 배열은 리그 메모리에 유지한다. |
| [TabletSettingsController](../client/Assets/Scripts/Core/Tablet/TabletSettingsController.cs) | 시작 시 PlayerPrefs에서 밝기·음량·진동과 선택 인덱스를 읽고 Manager에 인덱스를 돌려준다. 값 변경은 밝기·음량 API와 설정 UI에 반영하며, 저장 요청 시 선택 단계·문서 인덱스를 받아 설정값과 함께 기록한다. 설정 시트의 등장 애니메이션과 저장 완료 피드백도 담당한다. |
| [TabletLearningController](../client/Assets/Scripts/Core/Tablet/TabletLearningController.cs) | 선택 단계와 할 일 목록을 보관하고 단계 강조·완료 개수·진행률 표시를 갱신한다. 단계 선택 시 Manager에 이동 요청을 알리고 연결된 시작 Transform이 있으면 리그를 옮긴다. 진행률은 자체 예시 값이고 할 일은 확인 버튼으로 갱신되므로 공장 체험의 완료 상태와 별도로 동작한다. |
| [TabletUIBuilder](../client/Assets/Scripts/Core/Tablet/TabletUIBuilder.cs) | World Space Canvas와 인벤토리·문서·설정·지도 화면을 런타임에 구성한다. 생성한 버튼을 기능별 Controller의 메서드에 연결하고 InputController의 포커스 그룹에도 등록한다. 텍스트·버튼·이미지 참조는 `ShellView`, `SettingsView`, `LearningView`, `ParchmentView`로 묶어 각 Controller가 이후 갱신에 사용하게 한다. |
| [TabletVisualResources](../client/Assets/Scripts/Core/Tablet/TabletVisualResources.cs) | UIBuilder가 사용할 배경·심볼·픽셀·문서 예시 Sprite와 텍스처를 생성해 재사용한다. 태블릿 본체 메시를 런타임 메시로 교체할 때 원래 메시·스케일을 보관한다. Manager 종료 시 생성 자원을 파괴하고 본체를 원래 참조로 복원한다. |
| [TabletMinimapController](../client/Assets/Scripts/Core/Tablet/TabletMinimapController.cs) | `TabletManager.MapData`에서 바닥·벽·양피지 목록을 읽어 `MaskableGraphic`의 UI 도형과 라벨을 만든다. 카메라 위치·방향과 수집 상태를 주기적으로 갱신하고 변경이 있을 때 다시 그린다. 주변 지도는 플레이어를 따라가고 전체 지도는 공장 경계 전체를 맞춰 표시한다. |
| [TabletMapData](../client/Assets/Scripts/Core/Tablet/TabletMapData.cs) | 공장 판독 결과인 바닥·벽·지도 범위를 읽기 전용 목록과 경계로 묶는다. 양피지는 원본 Behaviour와 문서 인덱스를 보관해 현재 위치·활성 상태를 조회하게 한다. 공장 코드가 이 데이터를 게시하면 태블릿은 씬의 이름 규칙을 알지 않아도 지도를 그릴 수 있다. |
| [ParchmentLesson](../client/Assets/Scripts/Core/Tablet/ParchmentLesson.cs) | 양피지의 제목·부서·배치 방·페이지 본문을 `All` 목록으로 제공한다. 수집 오브젝트와 태블릿이 같은 문서 인덱스를 사용해 인벤토리 설명과 열람 본문을 연결한다. 문서 내용의 원본이며 수집 여부·현재 페이지는 ParchmentController가 보관한다. |

### 초기화와 UI 연결

`TabletManager.Awake()`는 폰트를 결정하고 리소스·입력·UIBuilder·기능별 Controller를 생성한다. 설정을 읽어 선택 단계와 문서 인덱스를 복원한 뒤 런타임 이미지와 UI를 만들고 각 화면을 갱신한다.

`TabletUIBuilder`는 기존 `Tablet Canvas`를 찾거나 생성하고 자식 UI를 재구성한다. 태블릿의 GraphicRaycaster는 비활성화하고 `TabletInputController`의 포커스로 버튼을 실행한다. UI를 추가할 때는 생성한 `Selectable`을 해당 `FocusGroup`에 등록해야 한다. 인벤토리 항목이 메인 포커스 목록의 앞부분에 등록되며, 문서 선택 복원에도 이 순서를 사용한다.

Manager의 주요 Inspector 참조는 `_font`, `_parchmentFont`, `_parchmentSprite`, `_stageStartPoints`다. `_stageMoveRequested`, `_saveRequested`, `_exitRequested`는 외부 동작을 연결하는 UnityEvent다.

### 포커스와 이동 입력

활성 포커스 그룹의 우선순위는 상세 화면 → 단계 목록 → 설정 → 메인이다. `TabletInputController.Navigate()`는 화면 좌표에서 입력 방향에 있는 활성 항목을 찾으며, 비활성·선택 불가능 항목을 건너뛴다. 포커스 그룹이나 슬라이더 모드가 바뀌면 스틱이 중앙으로 돌아온 뒤 탐색을 재개한다.

태블릿 탐색 중에는 `ContinuousMoveProvider.leftHandMoveInput`을 0을 반환하는 입력으로 교체한다. Trigger 해제와 스틱 중앙 복귀가 모두 충족되면 이전 입력을 복원한다. `Disable()`과 `Dispose()`에서도 이동 입력을 반환한다.

`TabletManager.GoBack()`은 상세 화면 → 슬라이더 조절 → 단계 목록 → 설정 → 양피지 열람 → 태블릿 조작 종료 순서로 현재 상태를 닫는다. 상세 화면을 닫을 때는 진입 전 포커스를 복원한다.

### 상태와 저장

| 상태 | 소유·저장 방식 |
| --- | --- |
| 밝기·음량·진동 선택 | `TabletSettingsController`. `Tablet.Brightness`, `Tablet.Volume`, `Tablet.Vibration`으로 저장한다. 밝기·음량은 각각 `Screen.brightness`, `AudioListener.volume`에 적용한다. 진동은 설정값과 표시 상태를 관리한다. |
| 선택 단계·문서 인덱스 | Learning/Parchment Controller가 소유하고 저장 시 `Tablet.Stage`, `Tablet.Inventory`에 기록한다. |
| 양피지 수집 여부 | `TabletParchmentController`의 메모리 배열. 리그가 살아 있는 동안 유지되며 PlayerPrefs에는 저장하지 않는다. |
| 문서 페이지·예시 표시·포커스 | 각 Controller의 런타임 상태다. |
| 진행률·할 일 | `TabletLearningController`의 예시 데이터. 진행률은 `DefaultProgress`를 사용하고 할 일은 `ConfirmCurrentTask()`로 갱신한다. 공장 체험 완료 판정과는 별개다. |

`MoveToStage()`는 선택 상태를 갱신하고 `_stageMoveRequested`를 호출한 뒤, 연결된 `_stageStartPoints`가 있으면 리그를 해당 Transform으로 이동시킨다. 단계 인덱스는 튜토리얼, 기본 오프닝, 필터 조작, MLP, CNN, AI 업스케일링 순서다. 실제 공장 콘텐츠의 Additive 로드는 [FactoryLearningFlowManager](factory-environment.md)가 담당한다.

### 수집과 지도 데이터

공장 수집 오브젝트는 `TabletManager.CollectParchment(index)`를 호출한다. Manager는 이를 `TabletParchmentController`에 위임하고, 인벤토리는 `ParchmentLesson.All`의 문서 데이터를 표시한다.

`FactoryMapController`가 `TabletMapData`를 만들어 `SetMapData()`로 전달하면 모든 미니맵이 다시 구성된다. `ClearMapData(data)`는 현재 지도와 전달된 데이터가 같을 때만 지워서 이전 씬의 해제가 새 지도를 지우지 않게 한다. 공장 배치 판독 방식은 [공장 환경 구현](factory-environment.md)에 있다.

미니맵은 XZ 평면을 +Z 위쪽으로 투영한다. 주변 지도는 카메라를 따라가며, 전체 지도는 전체 범위를 표시한다. 위치·방향·수집 여부는 일정 간격으로 갱신하고 변경 시 그래픽을 다시 그린다.

## 에디터 시뮬레이터

[XREditorSimulator.prefab](../client/Assets/Prefabs/XR/XREditorSimulator.prefab)은 [XRDeviceSimulatorSettings.asset](../client/Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset)에 연결되어 에디터에서 자동 생성된다.

`XREditorSimulator.Awake()`는 비활성 자식 오브젝트에 `SimulatedDeviceLifecycleManager`와 `XRInteractionSimulator`를 추가한다. 이동·회전·버튼 액션을 코드에서 구성하며, `OnEnable()`에서 액션과 가상 기기를 활성화한다. `OnDisable()`에서 비활성화하고 `OnDestroy()`에서 액션을 해제한다. 가상 컨트롤러 입력은 실제 기기와 같은 리그 입력 경로로 전달된다.

태블릿은 에디터 시뮬레이터와 실기기 모두 같은 왼손 grip 기준 위치·크기를 유지한다. 태블릿 조작 시작·종료에 따라 태블릿만 따로 이동하거나 축소하지 않는다.

## 프로젝트 설정 코드

Unity 버전은 [ProjectVersion.txt](../client/ProjectSettings/ProjectVersion.txt), 패키지 버전은 [manifest.json](../client/Packages/manifest.json)과 [packages-lock.json](../client/Packages/packages-lock.json)에서 관리한다. XR 구성은 Unity 6000.3.24f1, OpenXR 1.16.1, XR Plug-in Management 4.5.3, XRI 3.3.2를 사용한다.

| 파일·메서드 | 역할 |
| --- | --- |
| [XRProjectSetup.ConfigureProject](../client/Assets/Editor/XRProjectSetup.cs) | Android·Standalone OpenXR 로더, 컨트롤러 프로필, Android 실행·렌더링 설정을 구성한다. 메뉴는 `Tools > XR > Configure Project`다. |
| `XRProjectSetup.ScheduleStartupScene` / `OpenStartupScene` | 에디터 세션에서 한 번, 저장 경로·변경사항·루트 오브젝트가 없는 빈 씬이면 `FactoryIntro`를 연다. |
| `XRProjectSetup.AddLeftStickMovement` | 기존 리그 프리팹에 이동 컴포넌트를 구성한다. `ContinuousMoveProvider`가 있으면 기존 구성을 사용한다. |
| `XRProjectSetup.ValidateProject` / `ValidateRig` | OpenXR 설정, 시작 씬 `FactoryIntro`의 존재 여부, 카메라·리그·태블릿·레이 참조를 검사한다. |
| [QuestSetup.Configure](../client/Assets/Editor/QuestSetup.cs) | `Tools > CNN > Configure Quest VR`를 `XRProjectSetup.ConfigureProject()`에 연결한다. |

`ConfigureProject()`는 Android 자동 XR 초기화, IL2CPP·ARM64, 최소 API 32, Target API 자동 선택, Vulkan·Linear·Single Pass Instanced를 적용한다. Android에는 Meta Quest와 Touch Plus 프로필을 활성화하고 Mobile URP의 HDR을 끄며 MSAA를 4로 설정한다. Standalone 자동 XR 초기화는 시뮬레이터 사용을 위해 꺼 둔다.

## Touch Plus 컨트롤러 에셋과 SDK 제거 (2026-10-27)

`com.meta.xr.sdk.core` 207.0.1, 그 의존성으로 추가된 XR Hands 1.7.3, Meta scoped registry를 패키지 설정에서 제거했다. 기존 OpenXR 1.16.1, XR Plug-in Management 4.5.3, XRI 3.3.2와 입력·리그 구성은 유지한다. 컨트롤러 표시는 SDK 실행 코드가 아니라 기존 `TouchPlusHandController`의 Unity XR 입력과 Unity Animator로 구동한다.

공식 207.0.1에서 Touch Plus 표시용 런타임 에셋 12개를 분리했다. 위치는 다음과 같다(`client/Assets` 기준).

| 위치 | 보존 파일 |
| --- | --- |
| `Meshes/Common/MetaQuestTouchPlus` | `MetaQuestTouchPlus_Left.fbx`, `MetaQuestTouchPlus_Right.fbx`. 버튼·Grip·Trigger·스틱 애니메이션은 FBX 안에 포함된다. |
| `Meshes/Common/MetaQuestTouchPlus/Animation` | `MetaQuestTouchPlusLeftAnimation.controller`, `MetaQuestTouchPlusRightAnimation.controller` |
| `Textures/Common/MetaQuestTouchPlus` | `MetaQuestTouchPlus_Left_BaseColor_AO_AlphaRoughness.png`, `MetaQuestTouchPlus_Right_BaseColor_AO_AlphaRoughness.png`, `MetaQuestTouchPlus_Metallic.png`, `MetaQuestTouchPlus_Normals.png`, `batteryIndicatorTexture32.png` |
| `Materials/Common/MetaQuestTouchPlus` | `MetaQuestTouchPlusMATphong_left.mat`, `MetaQuestTouchPlusMATphong_right.mat`, `batteryIndicator_mat.mat` |

FBX·PNG·Animator Controller와 기존 `.meta` 내용은 원본 그대로 유지했다. GUID와 importer 설정을 보존해 리그의 모델·Animator 참조 및 모델 내부 애니메이션 연결을 유지한다. 배터리 표시 재질·텍스처는 SDK의 Touch Pro 폴더에 있지만 Touch Plus 모델이 참조하므로 함께 보존했다.

재질 3개는 SDK 셰이더 의존성을 제거하기 위해 본체의 `MetaLit`을 기존 URP/Lit으로, 배터리의 `TransparentTexture`를 URP/Unlit으로 연결하고 텍스처·색상·투명도 속성을 추가했다. 기존 `TouchPlusHandController`의 런타임 재질 생성과 버튼 표식·입력 연결은 유지한다. SDK 스크립트·에디터 도구·네이티브 플러그인·셰이더 파일은 포함하지 않는다.

원래 `LICENSE.md`와 `NOTICE.txt`는 모델 폴더에 원문으로 보존한다. 출처·수정 여부는 [ASSET_CREDITS.md](ASSET_CREDITS.md)에 기록했다.

이번 SDK 제거 작업에서는 파일·참조의 정적 검토만 수행했다. Unity 실행·에셋 임포트·컴파일·Play·테스트·APK 빌드는 실행하지 않았다. 아래 2026-10-26 검증 기록은 SDK가 설치되어 있던 당시 결과이며, SDK 제거 후 동작을 확인한 결과가 아니다.

원본 왼손 Animator의 클립 fileID 18개는 FBX `.meta`의 `internalIDToNameTable`과 일치하지 않는다. 공식 207.0.1 원본에도 있는 상태이므로 모델·Controller·importer를 임의 수정하지 않았다. Unity 재임포트 시 실제 클립 참조가 해결되는지와 왼손 버튼 애니메이션은 실행 확인이 필요하다.

## 과거 기록: Touch Plus 모델 패키지 설치 (2026-10-26)

당시 Meta 공식 registry `https://npm.developer.oculus.com`에서 `com.meta.xr.sdk.core` 207.0.1을 설치했다. registry 범위는 해당 패키지로 제한했고, 의존 패키지 XR Hands 1.7.3은 Unity가 해결했다. 기존 OpenXR/XRI 버전과 리그 입력 연결은 유지했다.

당시 모델은 `Packages/com.meta.xr.sdk.core/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Left.fbx`, `MetaQuestTouchPlus_Right.fbx`에 있었고, Animation 하위 폴더의 좌·우 Animator Controller를 사용했다. 설치 시점에는 패키지 원본을 Assets에 복제하거나 수정하지 않았다.

Unity 6000.3.24f1에서 설치·가져오기·컴파일 완료를 확인했다 (`isCompiling=false`, `isUpdating=false`, `scriptCompilationFailed=false`). 좌·우 모델 로드 및 왼손 버튼·Grip·Trigger·8방향 스틱 애니메이션 목록을 확인했다. Unity는 공식 패키지의 서명 파일 부재 경고를 출력했다. 다운로드 원본의 SHA 검증값은 Meta registry 값과 일치했다. Play·APK 빌드·기기 검증은 수행하지 않았으며 실제 리그 모델 교체와 튜토리얼 연결은 후속 작업이다.

## 과거 기록: 컨트롤러 표시와 실제 미션 연결 (2026-10-26)

`PersistentXRPlayerRig`의 직렬화된 좌·우 Touch Plus 모델 및 Animator 참조로 손의 grip pose 아래 실제 컨트롤러 모델을 생성한다. `TouchPlusHandController`가 Unity XR의 버튼·Grip·Trigger·스틱 값을 공식 Animator에 전달한다. 오른손 Meta 시스템 버튼은 앱 입력으로 가로채지 않는다. 패키지 모델·애니메이션 원본은 수정하지 않고 표시용 런타임 재질과 튜토리얼 표식만 별도로 생성·정리한다.

`TabletLearningController`의 고정 75% 예시 진행률과 확인 버튼으로 임의 완료하던 동작을 제거했다. `FactoryLearningFlowManager`가 양피지 보유 수와 방 체험 완료 수를 전달해 8개 목표 기준 비율을 표시한다. 단계 선택은 이동 안내이며 실제 걸어서 방에 들어간다. 기존 앱 실행 간 설정 저장과 미션 진행 저장은 별개이고 미션 진행은 세션 안에서 유지한다.

SDK가 설치된 당시의 후속 컴파일·Play에서 손 모델 2개와 안내용 확대 모델 2개의 생성, 카메라 렌더 표시 및 실제 수집 상태에 따른 태블릿 갱신을 확인했다. 당시에는 공장 씬에서 별도 마우스 미리보기를 실행하지 않아 공용 XR 리그를 유지했다. 현재는 픽셀 씬 전용 마우스 미리보기와 설치 메뉴를 제거했으며 공용 XR 시뮬레이터를 사용한다. Quest 버튼 입력과 애니메이션의 실기기 일치는 아직 확인하지 않았다.
