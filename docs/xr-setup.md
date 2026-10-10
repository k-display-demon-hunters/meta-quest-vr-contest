# XR 기본 설정

Unity **6000.3.24f1**에서 사용하는 가상 공간 전용 구성이다.
OpenXR 1.16.1, XR Plug-in Management 4.5.3, XR Interaction Toolkit 3.3.2를 고정했다.
패키지의 전이 의존성은 `client/Packages/packages-lock.json`으로 관리한다.

## 시작하기

1. Unity Hub에서 Android Build Support, SDK/NDK Tools, OpenJDK를 설치한다.
2. `client` 프로젝트를 열고 패키지 설치와 컴파일이 끝날 때까지 기다린다.
3. 시작 시 저장되지 않은 빈 `Untitled` 씬이면 `Assets/Scenes/QuestMain.unity`가 자동으로 열린다. 기존에 열어 둔 씬이나 저장되지 않은 작업이 있으면 유지한다. 필요할 때 Project 창에서 `QuestMain`을 직접 연다.
4. Build Profiles에서 Android 기반 Meta Quest 플랫폼으로 전환한다. `Tools > XR > Validate Project`와 `Project Settings > XR Plug-in Management > Project Validation`을 확인한다.
5. 개발자 모드의 Quest를 USB로 연결하여 Build And Run한다. 빌드 씬은 `QuestMain`이다.

기본 에디터 Play에는 아래 XR Interaction Simulator가 자동 생성되므로 헤드셋 없이 키보드·마우스로 조작할 수 있다. Standalone의 `Initialize XR on Startup`은 꺼 두어 PC에 OpenXR 런타임이 없어도 초기화를 시도하지 않는다. Android의 자동 초기화는 켜져 있다.

Windows Editor에서 Quest Link로 실행할 때에는 Meta Quest Link를 연결하고 Meta를 활성 OpenXR 런타임으로 지정한다. `Project Settings > XR Plug-in Management`의 PC/Standalone 탭에서 `Initialize XR on Startup`을 켜고, 시뮬레이터 자동 생성을 끈다. 키보드·마우스 개발로 돌아오면 두 설정을 원래대로 되돌린다.

## 에디터 XR Interaction Simulator

자세한 사용법과 전체 조작키는 [XR 에디터 시뮬레이터](xr-simulator.md)를 참고한다.

- `Assets/Scripts/XR/XREditorSimulator.cs`가 XRI 3.3.2 패키지의 `XRInteractionSimulator`와 기기 관리 컴포넌트를 직접 구성한다. 키보드·마우스 Input Action을 코드에서 생성하고 활성화·비활성화·해제한다. 손 표현 시뮬레이션은 사용하지 않는다.
- `Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`에서 자동 생성과 에디터 전용 생성을 활성화하고 프로젝트의 `Assets/Prefabs/XR/XREditorSimulator.prefab`을 연결했다. `QuestMain`에서 Play하면 가상 기기가 생성된다. 화면의 조작키 안내는 제거했으며 조작키는 아래 표를 참고한다. 씬에 프리팹을 별도로 배치할 필요가 없다.
- 가져왔던 `Assets/Samples`의 파일 132개(메타 포함)를 삭제했다. 샘플 프리팹, 입력 에셋, UI 이미지, UI 스크립트, 손 표현, 샘플 검증 도구에 대한 의존성이 없다. 기존 XRI 패키지의 시뮬레이션 API는 계속 사용한다.
- Game 뷰를 클릭하여 키보드 입력을 받도록 한다. 리그는 기존 Input System 추적·이동·태블릿 입력을 그대로 사용한다.

| 조작 | 기본 키 |
| --- | --- |
| 왼쪽 스틱 전후좌우 이동 | **Shift를 누른 채 I / K / J / L** |
| 태블릿 X / Y / Trigger | **Shift를 누른 채 1 / 2 / T** |
| 머리·몸 이동 | 기본 FPS 모드에서 WASD, Q/E로 하강/상승 |
| 시선 회전 | 마우스 오른쪽 버튼을 누른 채 마우스 이동 |
| 왼손 / 오른손 위치 조작 선택 | `[` / `]` |
| FPS 모드와 기기 조작 모드 전환 | Tab |

Shift는 왼손의 버튼·스틱 입력을 선택한다. 대괄호는 손의 위치·회전을 조작할 대상을 선택하며, 왼손 버튼 선택과 구분된다. 버튼을 놓을 때도 Shift를 유지하고, 버튼 키를 먼저 놓는다. Shift 없이 1/2/T 또는 IJKL을 누르면 오른손 입력으로 처리된다.

시뮬레이터의 초기 HMD 입력 위치는 0이다. 헤드셋이 연결되지 않아 실제 바닥 추적 높이가 제공되지 않는 경우 기본 FPS 모드에서 E로 시점을 올린다. WASD는 시뮬레이터의 몸 위치 이동이며 왼쪽 스틱 이동을 확인하려면 Shift+IJKL을 사용한다.

자동 생성을 끄려면 `Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit`에서 `Use XR Interaction Simulator in scenes`를 끈다. `Instantiate In Editor Only`는 켜 둔다.

샘플 제거 후 소스와 에셋 참조를 확인했다. Play 실행·테스트·컴파일 검증·빌드는 직접 실행하지 않았다.

## 씬과 조작

### 씬 전환과 리그 유지

- 시작 씬 `QuestMain`의 `XRPlayerRig`에 `PersistentXRPlayerRig`를 연결한다. 리그 전체를 `DontDestroyOnLoad`로 유지하므로 씬을 교체하거나 추가 로드해도 카메라, 양손 추적, 이동 입력, 태블릿과 오른손 컨트롤러가 유지된다.
- 태블릿의 World Space Canvas도 리그 아래에 있으므로 현재 단계, 인벤토리, 설정 상태가 리그와 함께 유지된다. 후속 UI 프리팹 역시 태블릿의 자식으로 연결한다.
- 콘텐츠 씬에 독자적인 XR 카메라·AudioListener·리그를 만들지 않는다. 단독 실행을 위해 공통 XRPlayerRig 프리팹을 배치할 수 있으며, 기존 리그가 있을 때는 새 복제본을 먼저 비활성화하고 제거하여 기존 리그를 유지한다.
- 씬 전환 때 리그 위치를 자동으로 초기화하지 않는다. 씬별 시작 위치로 이동하는 기능은 별도 요구사항으로 결정한다.
- 콘텐츠 씬을 에디터에서 단독 Play하면 시작 씬의 리그는 생성되지 않는다. 기본적으로 `QuestMain`에서 시작해 콘텐츠 씬으로 전환한다. 단독 작업에 리그가 필요하면 동일 XRPlayerRig 프리팹을 배치할 수 있으며, 앱에서 시작 씬을 거쳐 들어온 경우에는 중복 방지 처리가 적용된다.
- `CNNFeatureMapLab`, `CNNModelDesignLab`, `CNNPixelLab`에는 같은 `Assets/Prefabs/XR/XRPlayerRig.prefab`을 일반 씬 인스턴스로 배치한다. 단독 실행 시 해당 리그를 유지하고, 기존 리그가 있는 상태에서 씬을 로드하면 `PersistentXRPlayerRig`가 새 중복 인스턴스를 비활성화하고 제거한다. 기존 리그의 태블릿 상태와 위치는 유지한다. `CNNModelDesignLab`의 리그는 더 이상 `EditorOnly`가 아니다.
- 기존 프리팹과 초기 씬 생성 도구 모두 유지 컴포넌트를 포함한다. 이번 변경은 소스·프리팹 참조를 확인했으며 테스트·Play 실행·컴파일 검증·빌드는 실행하지 않았다.

- `Assets/Prefabs/XR/XRPlayerRig.prefab`: XR Origin, Camera Offset, Main Camera, 양손 컨트롤러, XR Interaction Manager.
- 머리: Input System Tracked Pose Driver가 위치·회전·추적 상태를 읽는다. Update와 Before Render에서 갱신한다.
- 높이: Floor 모드로 실제 바닥을 기준으로 한다. Device 모드로 변경할 경우 Camera Y Offset은 1.6m이다.
- 컨트롤러: XRI 기본 `Vector3Fallback`·`QuaternionFallback` 바인딩으로 OpenXR aim pose(`pointerPosition`, `pointerRotation`)를 우선 사용한다. aim 입력이 없는 XR Interaction Simulator에서는 `devicePosition`·`deviceRotation`으로 레이의 위치·방향을 갱신한다. 양손 레이의 프리팹 설정과 초기 씬 생성 도구에 동일하게 적용한다.
- Ray 표시는 `XRInteractorLineVisual`의 `Line Bend Ratio`를 1로 설정한다. 포트를 선택한 채 다른 포트를 가리킬 때에도 표시선이 출발 포트로 휘지 않고 컨트롤러의 직선 Ray를 따른다. 포트에서 끌어오는 연결선은 별도로 표시한다.
- 오른손에는 손잡이·스틱·버튼·트리거로 구성된 임시 컨트롤러 모델이 표시된다. 오른손 레이와 Grip/Trigger 입력은 다른 콘텐츠에 사용하며, 태블릿은 왼손 포커스 이동으로만 조작한다.
- 왼손에는 약 30cm × 20.5cm, 두께 1.4cm의 직사각형 태블릿이 표시된다. 화면은 손에 쥔 컨트롤러의 뒤쪽(-Z)을 향한다.
- 태블릿 본체는 어두운 흑연색 `TabletBody` URP Unlit 재질을 사용하고, 화면 UI는 `Tablet Canvas`를 런타임에 재구성한다. 오른손 컨트롤러는 기존 Lit 재질을 유지한다.
- 양손 표시용 오브젝트는 별도 Tracked Pose Driver로 grip pose(`devicePosition`, `deviceRotation`)를 추적한다. 기존 레이는 aim pose를 유지하고, 왼손 레이 및 인터랙터는 비활성화하여 태블릿 입력용으로 사용한다.
- 태블릿 입력은 왼손 스틱(`primary2DAxis`), 검지 Trigger(`triggerPressed`), X(`primaryButton`), Y(`secondaryButton`)다. X는 조작 시작/뒤로/기본 화면에서 조작 종료, Y는 설정 열기/닫기, Trigger는 조작 시작/포커스 항목 선택이다. 스틱은 항목을 이동하고 밝기·볼륨 항목에서는 좌우로 5%씩 조절한다. 시스템 Menu 버튼과 Grip은 태블릿에 사용하지 않는다.
- 태블릿을 조작하는 동안 현재 리그의 `ContinuousMoveProvider.leftHandMoveInput`을 0 입력으로 교체한다. 기본 화면에서 X 또는 나가기로 조작을 종료한 뒤 스틱이 중앙으로 돌아오면 원래 입력을 복원한다. 비활성화/파괴 시에도 원래 입력을 복원한다. 다른 리그와 오른손 입력은 변경하지 않는다.
- `Assets/Prefabs/XR/XRPlayerRig.prefab`의 `Tablet > Tablet Canvas`는 World Space Canvas 기준점으로 유지한다. Canvas 크기는 500 × 340, 배율은 0.0005이며, `LeftHandTablet`이 실행 시 기존 미리보기 자식을 지우고 스케치 기준 UI를 생성한다. 상단은 전체 진행률과 오른쪽 숫자, 왼쪽 열은 현재 단계·임무·미니맵, 중앙은 교육/팀 정보·개념 설명/피드백, 오른쪽 열은 5개 정사각형 인벤토리와 설정/나가기 버튼이다. 밝은 중립색 바탕과 작은 모서리를 사용하며, 선택 아이템은 노란색, 조작 포커스는 청록색 테두리다. `CanvasScaler.dynamicPixelsPerUnit`은 4로 설정한다.
- `Assets/Scripts/XR/Tablet/LeftHandTablet.cs`는 태블릿 UI 전체를 구성하고 왼손 방향 탐색을 처리하며, 설정 패널의 단계 이동·저장·밝기·볼륨·진동 상태를 관리한다. 태블릿 Canvas의 Raycaster는 비활성화한다. 설정창은 384 × 286 안에 컨트롤을 배치하고, 슬라이더 fill의 offset을 0으로 유지해 0~100%에서 트랙 밖으로 늘어나지 않게 한다. 단계 이동은 Inspector의 `Stage Start Points`가 연결되어 있으면 해당 Transform으로 XR rig를 이동하고, 연결 전에는 피드백 영역에 안내만 표시한다. `Save`, `Exit`, `Stage Move` UnityEvent를 제공하므로 후속 씬 전환/저장 로직과 연결할 수 있다.
- 바닥은 8m × 8m이며 Unity 1 unit을 1m로 사용한다.
- 왼쪽 스틱: 바라보는 방향을 기준으로 전후좌우 이동한다. 최대 속도는 1.5m/s이며 작은 스틱 입력은 데드존(0.15)으로 무시한다. 고개를 위아래로 움직여도 이동은 수평으로 유지된다.
- XR Body Transformer, Locomotion Mediator, Continuous Move Provider로 수평 이동만 처리한다. 이동 리그에는 충돌과 중력이 없으며, 스틱을 놓으면 현재 위치에 멈춘다. 바닥 밖에서도 떨어지지 않는다.
- 오른쪽 스틱 이동, 스틱 회전, 텔레포트, 손 추적, 패스스루는 추가하지 않았다. 회전은 실제 고개/몸 회전으로 한다.

추적과 이동 입력은 프리팹 컴포넌트에 직렬화한 Input Action으로 연결하며, Tracked Pose Driver와 XRI가 활성화/비활성화를 관리한다. 태블릿 버튼은 LeftHandTablet이 직접 관리한다. 새로운 상호작용 대상에는 Collider, 필요한 Rigidbody, XRI Interactable을 붙인다.

### 삭제 내역

- `XRPlayerRig`의 `Tablet Text Anchor` GameObject와 Transform, `Tablet Screen` GameObject와 Transform·MeshRenderer·MeshFilter: Canvas를 Tablet 바로 아래로 옮기고 UI Image로 화면 배경을 대체했다.
- `Assets/XR/Materials/TabletScreen.mat`와 `.meta`: 제거한 3D 화면 전용 재질.
- `Assets/Scenes/SampleScene.unity`와 `.meta`: 사용하지 않는 기본 씬 전체(기본 카메라·조명 포함).
- `QuestMain`의 `Grab Test Cube`: GameObject, Transform, MeshRenderer, MeshFilter, BoxCollider, Rigidbody, XRGrabInteractable 및 큐브 전용 씬 참조.
- `Assets/XR/Materials/GrabCube.mat`와 `.meta`: 삭제한 큐브 전용 재질.
- 초기 씬 생성 도구의 테스트 큐브·큐브 재질 생성 코드. 재생성 시에도 태블릿과 오른손 모델을 만들며 테스트 큐브를 복원하지 않는다.

바닥, 조명, QuestMain 씬, XR Origin과 카메라는 유지한다. 왼손 레이 관련 컴포넌트는 삭제하지 않고 비활성화했다.

### 교육 단계 순서

1. 튜토리얼
2. 기본 오프닝
3. 필터 조작 (이미지 확대 및 합성곱)
4. MLP (간략)
5. CNN
6. AI 업스케일링

태블릿 단계 목록과 `Stage Start Points`는 위 순서를 사용한다(배열 인덱스 0~5). 좁은 현재 단계 영역에서는 3단계를 `필터 조작`으로 줄여 표시하고, 단계 선택 목록에는 괄호 안 설명까지 표시한다. 저장된 단계가 없는 경우 튜토리얼부터 표시하며 기존 저장 인덱스는 유지한다. 튜토리얼 스킵 버튼은 후속 작업이다. 이번 변경은 단계 목록과 표시 배치 변경이며, 각 교육 콘텐츠와 실제 이동 지점의 연결을 추가하지 않는다.

## 프로젝트 설정

- Android OpenXR 로더 자동 시작. Standalone에는 OpenXR 로더를 등록하되 시뮬레이터 개발을 위해 자동 시작을 끈다.
- Android Meta Quest Support와 Oculus Touch Controller Profile 활성화. Quest Touch 입력의 기본 프로필을 사용한다.
- Android IL2CPP, ARM64, 최소 API 32, 설치된 SDK 기준 Target API 자동 선택.
- Vulkan, Linear 색 공간, Single Pass Instanced.
- Android 기본 품질의 Mobile URP: HDR 끄기, MSAA 4배. XR 카메라는 후처리를 사용하지 않는다.
- 앱 ID: `com.kdisplaydemonhunters.metaquestvrcontest`.

`Target API: Automatic`은 로컬 개발 설정이며 배포 시점의 스토어 요구사항과 설치된 SDK를 다시 확인해야 한다.

## 설정 도구

`Assets/Editor/XRProjectSetup.cs`의 명시적 메뉴/배치 명령으로 설정을 재현한다. 에디터 세션 시작 시 빈 `Untitled` 상태이면 기존 `QuestMain`을 연다. 씬·프리팹을 생성하거나 덮어쓰지 않으며, 스크립트 재컴파일 때마다 씬을 다시 열지 않는다. 배치 모드에서는 자동 열기를 실행하지 않는다.

- `Tools > XR > Configure Project`: 프로젝트 XR/Android 설정을 적용한다. 자동 초기화는 Android에서 켜고 Standalone에서 끄므로 Quest Link를 사용할 때는 Standalone 설정을 다시 켠다.
- `Tools > XR > Create Initial VR Scene`: 초기 프리팹과 씬을 만든다. 기존 결과물이 있으면 덮어쓰지 않고 중단한다.
- `Tools > XR > Add Left Stick Movement`: 기존 XRPlayerRig 프리팹에 이동 구성을 추가한다. 이미 이동 컴포넌트가 있으면 기존 조정값을 보존한다.
- `Tools > XR > Validate Project`: OpenXR 공식 검증 규칙과 리그 참조를 검사한다.

## 실제 기기 확인

1. 가상 공간이 양쪽 눈에 표시되는지 확인한다.
2. 고개 회전, 숙이기, 실제 이동이 카메라에 반영되는지 확인한다.
3. 바닥 높이가 실제 바닥과 일치하는지 확인한다.
4. 왼쪽 스틱으로 전후좌우 이동하고, 고개를 위아래로 기울여도 높이가 변하지 않는지 확인한다.
5. 왼손 태블릿과 오른손 모델이 각각 손의 위치·회전을 따라가며 태블릿 글자가 읽히는지 확인한다.
6. X로 태블릿 조작 시작, 스틱으로 항목 이동, Trigger로 선택, Y로 설정 열기/닫기를 확인한다. 설정의 단계 목록·저장·밝기·볼륨·진동·나가기에 왼손만으로 접근할 수 있어야 한다. 밝기/볼륨 0%, 50%, 100%에서 슬라이더와 숫자가 창 안에 있는지 확인한다.
7. UI 조작 중 플레이어 이동이 정지하는지, X로 조작을 종료한 후 스틱을 중앙으로 놓으면 이동이 복원되는지 확인한다.
8. 헤드셋 절전·복귀와 컨트롤러 재연결 후 추적/입력이 복구되는지 확인한다.

에디터 컴파일과 APK 빌드는 실기기의 추적, 멀미, 프레임 성능 검증을 대신하지 않는다.

## 초기 XR 설정의 과거 검증 결과

- Unity 6000.3.24f1 C# 컴파일 통과.
- Android OpenXR 필수 검증과 리그의 카메라·컨트롤러·입력 참조 검사 통과.
- 공식 검증에서 PoseControl 전환, StickControl 전환, SSAO 비용, 입력 폴링 우선순위에 대한 권고 4건은 남아 있다. Android 기본 Mobile URP와 별개로 현재 에디터 품질의 렌더러에서도 SSAO 권고가 발생할 수 있다.
- 사용자 요청에 따라 APK 빌드 실행 코드를 제거했다. APK 생성 및 실기기 검증은 하지 않았다.

## 공식 근거

- [Unity 6.3 Meta Quest 개발](https://docs.unity3d.com/6000.3/Documentation/Manual/xr-meta-quest-develop.html)
- [Unity 6.3 Meta Quest 빌드 플랫폼](https://docs.unity3d.com/6000.3/Documentation/Manual/xr-meta-quest-build-profile.html)
- [Meta Unity VR 설정](https://developers.meta.com/vr/documentation/unity/unity-project-setup/)
- [OpenXR 프로젝트 설정](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/project-configuration.html)
- [OpenXR Oculus Touch 프로필](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/oculustouchcontrollerprofile.html)
- [XR Interaction Toolkit 프로젝트 설정](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/general-setup.html)

API의 정확한 필드와 입력 바인딩은 위 버전으로 설치된 공식 패키지 소스에서도 확인했다.

- [XRI Continuous Move Provider 공식 문서](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/continuous-move-provider.html)

빌드 실행은 사용자가 명시적으로 허가한 경우에만 한다.

이동/드래그 후속 수정은 사용자 요청에 따라 테스트·컴파일 검증·빌드 없이 적용했다. 테스트 파일은 추가하지 않았다.

2026-10-08 태블릿·컨트롤러 표시 및 테스트 에셋 정리는 프로젝트 지침에 따라 테스트·Unity 컴파일·APK 빌드·실기기 검증 없이 적용했다. 위 과거 검증 결과는 이번 변경을 검증한 결과가 아니다.

2026-10-10 스케치 배치 복원, 사각형 스타일, 설정창 넘침 수정 및 왼손 포커스 조작 변경은 소스와 설치된 XRI/UGUI API를 대조했다. 이번 수정에 대한 테스트·Unity Play·컴파일·빌드는 실행하지 않았다. 미니맵과 93% 진행률은 기존 예시 데이터이며, 실제 위치/교육 진행 데이터 연동은 별도 작업이다.
