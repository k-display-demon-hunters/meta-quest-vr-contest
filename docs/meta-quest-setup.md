# Meta Quest VR 기본 설정

Unity **6000.3.24f1**에서 사용하는 가상 공간 전용 구성이다.
OpenXR 1.16.1, XR Plug-in Management 4.5.3, XR Interaction Toolkit 3.3.2를 고정했다.
패키지의 전이 의존성은 `client/Packages/packages-lock.json`으로 관리한다.

## 시작하기

1. Unity Hub에서 Android Build Support, SDK/NDK Tools, OpenJDK를 설치한다.
2. `client` 프로젝트를 열고 패키지 설치와 컴파일이 끝날 때까지 기다린다.
3. `Assets/Scenes/QuestMain.unity`를 연다.
4. Build Profiles에서 Android 기반 Meta Quest 플랫폼으로 전환한다. `Tools > Quest > Validate Project`와 `Project Settings > XR Plug-in Management > Project Validation`을 확인한다.
5. 개발자 모드의 Quest를 USB로 연결하여 Build And Run한다. 빌드 씬은 `QuestMain`이다.

Windows Editor에서 Quest Link로 실행할 때에는 Meta Quest Link를 연결하고 Meta를 활성 OpenXR 런타임으로 지정한다. Standalone OpenXR도 설정되어 있다. 헤드셋 없이 일반 Play만 누르면 머리/컨트롤러 입력이 발생하지 않는다.

## 씬과 조작

- `Assets/Prefabs/XR/QuestRig.prefab`: XR Origin, Camera Offset, Main Camera, 양손 컨트롤러, XR Interaction Manager.
- 머리: Input System Tracked Pose Driver가 위치·회전·추적 상태를 읽는다. Update와 Before Render에서 갱신한다.
- 높이: Floor 모드로 실제 바닥을 기준으로 한다. Device 모드로 변경할 경우 Camera Y Offset은 1.6m이다.
- 컨트롤러: OpenXR aim pose(`pointerPosition`, `pointerRotation`)로 레이를 향하게 한다.
- Grip: 레이로 가리킨 큐브를 잡고 놓는다. Trigger: XRI Activate 입력이다. 현재 큐브에는 별도의 Activate 동작이 없다.
- 바닥은 8m × 8m, 큐브는 시작 위치의 정면 1.5m에 있다. Unity 1 unit을 1m로 사용한다.
- 기본 동작은 실제 몸의 이동과 회전이다. 스틱 이동·텔레포트·손 추적·UI·패스스루는 추가하지 않았다.

입력은 프리팹 컴포넌트에 직렬화한 Input Action으로 연결하며, Tracked Pose Driver와 XRI가 활성화/비활성화를 관리한다. 새로운 상호작용 대상에는 Collider, 필요한 Rigidbody, XRI Interactable을 붙인다.

## 프로젝트 설정

- Android/Standalone OpenXR 로더 자동 시작.
- Android Meta Quest Support와 Oculus Touch Controller Profile 활성화. Quest Touch 입력의 기본 프로필을 사용한다.
- Android IL2CPP, ARM64, 최소 API 32, 설치된 SDK 기준 Target API 자동 선택.
- Vulkan, Linear 색 공간, Single Pass Instanced.
- Android 기본 품질의 Mobile URP: HDR 끄기, MSAA 4배. XR 카메라는 후처리를 사용하지 않는다.
- 앱 ID: `com.kdisplaydemonhunters.metaquestvrcontest`.

`Target API: Automatic`은 로컬 개발 설정이며 배포 시점의 스토어 요구사항과 설치된 SDK를 다시 확인해야 한다.

## 설정 도구

`Assets/Editor/QuestProjectSetup.cs`의 명시적 메뉴/배치 명령으로 설정을 재현한다. 프로젝트를 열 때 자동으로 씬을 변경하지 않는다.

- `Tools > Quest > Configure Project`: 프로젝트 XR/Android 설정을 적용한다.
- `Tools > Quest > Create Initial VR Scene`: 초기 프리팹과 씬을 만든다. 기존 결과물이 있으면 덮어쓰지 않고 중단한다.
- `Tools > Quest > Validate Project`: OpenXR 공식 검증 규칙과 리그 참조를 검사한다.

## 실제 기기 확인

1. 가상 공간이 양쪽 눈에 표시되는지 확인한다.
2. 고개 회전, 숙이기, 실제 이동이 카메라에 반영되는지 확인한다.
3. 바닥 높이가 실제 바닥과 일치하는지 확인한다.
4. 양손 각각 레이로 큐브를 가리키고 Grip으로 잡기/놓기를 확인한다.
5. 헤드셋 절전·복귀와 컨트롤러 재연결 후 추적/입력이 복구되는지 확인한다.

에디터 컴파일과 APK 빌드는 실기기의 추적, 멀미, 프레임 성능 검증을 대신하지 않는다.

## 이번 변경의 검증 결과

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
