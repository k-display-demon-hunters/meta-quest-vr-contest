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

Windows Editor에서 Quest Link로 실행할 때에는 Meta Quest Link를 연결하고 Meta를 활성 OpenXR 런타임으로 지정한다. Standalone OpenXR도 설정되어 있다. 기본 에디터 Play에는 아래 XR Interaction Simulator가 자동 생성되므로 헤드셋 없이 키보드·마우스로 조작할 수 있다. Quest Link로 실제 기기를 사용할 때는 시뮬레이터 자동 생성을 먼저 끈다.

## 에디터 XR Interaction Simulator

- `Assets/Scripts/XR/XREditorSimulator.cs`가 XRI 3.3.2 패키지의 `XRInteractionSimulator`와 기기 관리 컴포넌트를 직접 구성한다. 키보드·마우스 Input Action을 코드에서 생성하고 활성화·비활성화·해제한다. 손 표현 시뮬레이션은 사용하지 않는다.
- `Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`에서 자동 생성과 에디터 전용 생성을 활성화하고 프로젝트의 `Assets/Prefabs/XR/XREditorSimulator.prefab`을 연결했다. `QuestMain`에서 Play하면 가상 기기와 프로젝트 코드의 간단한 조작키 안내가 생성된다. 씬에 프리팹을 별도로 배치할 필요가 없다.
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
- 태블릿의 World Space Canvas도 리그 아래에 있으므로 현재 문자와 누적 입력 횟수가 유지된다. 후속 UI 프리팹 역시 태블릿의 자식으로 연결한다.
- 다른 씬은 배경과 기능 콘텐츠만 포함한다. XR 카메라, AudioListener, XRPlayerRig를 별도로 추가하지 않는다. 실수로 같은 XRPlayerRig 프리팹이 다시 로드되면 새 복제본을 먼저 비활성화하고 제거하여 기존 리그를 유지한다.
- 씬 전환 때 리그 위치를 자동으로 초기화하지 않는다. 씬별 시작 위치로 이동하는 기능은 별도 요구사항으로 결정한다.
- 콘텐츠 씬을 에디터에서 단독 Play하면 시작 씬의 리그는 생성되지 않는다. 기본적으로 `QuestMain`에서 시작해 콘텐츠 씬으로 전환한다. 단독 작업에 리그가 필요하면 동일 XRPlayerRig 프리팹을 배치할 수 있으며, 앱에서 시작 씬을 거쳐 들어온 경우에는 중복 방지 처리가 적용된다.
- 기존 프리팹과 초기 씬 생성 도구 모두 유지 컴포넌트를 포함한다. 이번 변경은 소스·프리팹 참조를 확인했으며 테스트·Play 실행·컴파일 검증·빌드는 실행하지 않았다.

- `Assets/Prefabs/XR/XRPlayerRig.prefab`: XR Origin, Camera Offset, Main Camera, 양손 컨트롤러, XR Interaction Manager.
- 머리: Input System Tracked Pose Driver가 위치·회전·추적 상태를 읽는다. Update와 Before Render에서 갱신한다.
- 높이: Floor 모드로 실제 바닥을 기준으로 한다. Device 모드로 변경할 경우 Camera Y Offset은 1.6m이다.
- 컨트롤러: OpenXR aim pose(`pointerPosition`, `pointerRotation`)로 레이를 향하게 한다.
- 오른손에는 손잡이·스틱·버튼·트리거로 구성된 임시 컨트롤러 모델이 표시된다. 오른손 레이와 Grip/Trigger 입력은 유지한다.
- 왼손에는 28cm × 20cm, 두께 1.2cm의 직사각형 태블릿이 표시된다. 화면은 손에 쥔 컨트롤러의 뒤쪽(-Z)을 향한다. 최종 디자인은 추후 교체한다.
- 태블릿 본체와 화면은 전용 `TabletBody`·`TabletScreen` URP Unlit 재질을 사용하여 조명 반사 없이 일정한 색으로 표시한다. 오른손 컨트롤러는 기존 Lit 재질을 유지한다.
- 양손 표시용 오브젝트는 별도 Tracked Pose Driver로 grip pose(`devicePosition`, `deviceRotation`)를 추적한다. 기존 레이는 aim pose를 유지하고, 왼손 레이 및 인터랙터는 비활성화하여 태블릿 입력용으로 사용한다.
- 태블릿 입력은 왼손 X(`primaryButton`), Y(`secondaryButton`), Trigger(`triggerPressed`) 3개다. 누른 버튼의 이름과 누적 입력 횟수를 화면에 표시한다. 스틱은 이동 전용으로 유지한다. 시스템 Menu 버튼과 Grip은 태블릿 입력으로 사용하지 않는다.
- `Assets/Scripts/XR/LeftHandTablet.cs`는 기존 uGUI의 World Space Canvas에 문자를 출력하며 입력 활성화·비활성화·해제를 관리한다. 서버 통신이나 실제 메뉴 기능은 추가하지 않는다.
- 바닥은 8m × 8m이며 Unity 1 unit을 1m로 사용한다.
- 왼쪽 스틱: 바라보는 방향을 기준으로 전후좌우 이동한다. 최대 속도는 1.5m/s이며 작은 스틱 입력은 데드존(0.15)으로 무시한다. 고개를 위아래로 움직여도 이동은 수평으로 유지된다.
- XR Body Transformer, Locomotion Mediator, Continuous Move Provider로 수평 이동만 처리한다. 이동 리그에는 충돌과 중력이 없으며, 스틱을 놓으면 현재 위치에 멈춘다. 바닥 밖에서도 떨어지지 않는다.
- 오른쪽 스틱 이동, 스틱 회전, 텔레포트, 손 추적, 패스스루는 추가하지 않았다. 회전은 실제 고개/몸 회전으로 한다.

추적과 이동 입력은 프리팹 컴포넌트에 직렬화한 Input Action으로 연결하며, Tracked Pose Driver와 XRI가 활성화/비활성화를 관리한다. 태블릿 버튼은 LeftHandTablet이 직접 관리한다. 새로운 상호작용 대상에는 Collider, 필요한 Rigidbody, XRI Interactable을 붙인다.

### 삭제 내역

- `Assets/Scenes/SampleScene.unity`와 `.meta`: 사용하지 않는 기본 씬 전체(기본 카메라·조명 포함).
- `QuestMain`의 `Grab Test Cube`: GameObject, Transform, MeshRenderer, MeshFilter, BoxCollider, Rigidbody, XRGrabInteractable 및 큐브 전용 씬 참조.
- `Assets/XR/Materials/GrabCube.mat`와 `.meta`: 삭제한 큐브 전용 재질.
- 초기 씬 생성 도구의 테스트 큐브·큐브 재질 생성 코드. 재생성 시에도 태블릿과 오른손 모델을 만들며 테스트 큐브를 복원하지 않는다.

바닥, 조명, QuestMain 씬, XR Origin과 카메라는 유지한다. 왼손 레이 관련 컴포넌트는 삭제하지 않고 비활성화했다.

## 프로젝트 설정

- Android/Standalone OpenXR 로더 자동 시작.
- Android Meta Quest Support와 Oculus Touch Controller Profile 활성화. Quest Touch 입력의 기본 프로필을 사용한다.
- Android IL2CPP, ARM64, 최소 API 32, 설치된 SDK 기준 Target API 자동 선택.
- Vulkan, Linear 색 공간, Single Pass Instanced.
- Android 기본 품질의 Mobile URP: HDR 끄기, MSAA 4배. XR 카메라는 후처리를 사용하지 않는다.
- 앱 ID: `com.kdisplaydemonhunters.metaquestvrcontest`.

`Target API: Automatic`은 로컬 개발 설정이며 배포 시점의 스토어 요구사항과 설치된 SDK를 다시 확인해야 한다.

## 설정 도구

`Assets/Editor/XRProjectSetup.cs`의 명시적 메뉴/배치 명령으로 설정을 재현한다. 에디터 세션 시작 시 빈 `Untitled` 상태이면 기존 `QuestMain`을 연다. 씬·프리팹을 생성하거나 덮어쓰지 않으며, 스크립트 재컴파일 때마다 씬을 다시 열지 않는다. 배치 모드에서는 자동 열기를 실행하지 않는다.

- `Tools > XR > Configure Project`: 프로젝트 XR/Android 설정을 적용한다.
- `Tools > XR > Create Initial VR Scene`: 초기 프리팹과 씬을 만든다. 기존 결과물이 있으면 덮어쓰지 않고 중단한다.
- `Tools > XR > Add Left Stick Movement`: 기존 XRPlayerRig 프리팹에 이동 구성을 추가한다. 이미 이동 컴포넌트가 있으면 기존 조정값을 보존한다.
- `Tools > XR > Validate Project`: OpenXR 공식 검증 규칙과 리그 참조를 검사한다.

## 실제 기기 확인

1. 가상 공간이 양쪽 눈에 표시되는지 확인한다.
2. 고개 회전, 숙이기, 실제 이동이 카메라에 반영되는지 확인한다.
3. 바닥 높이가 실제 바닥과 일치하는지 확인한다.
4. 왼쪽 스틱으로 전후좌우 이동하고, 고개를 위아래로 기울여도 높이가 변하지 않는지 확인한다.
5. 왼손 태블릿과 오른손 모델이 각각 손의 위치·회전을 따라가며 태블릿 글자가 읽히는지 확인한다.
6. X, Y, Trigger를 각각 눌러 버튼 이름과 횟수가 바뀌는지 확인한다. 누른 채 유지할 때 한 번만 출력되고, 놓았다 다시 누르면 횟수가 증가해야 한다.
7. 헤드셋 절전·복귀와 컨트롤러 재연결 후 추적/입력이 복구되는지 확인한다.

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
