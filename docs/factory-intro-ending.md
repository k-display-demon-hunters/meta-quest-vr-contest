# 공장 오프닝·튜토리얼과 엔딩

[FactoryIntro](../client/Assets/Scenes/FactoryIntro.unity)는 이야기와 기본 조작 튜토리얼을 진행한 뒤 공장 씬을 연다. [FactoryEnding](../client/Assets/Scenes/FactoryEnding.unity)은 복구 연출과 개념 복습을 담당하는 독립 씬이다. 버튼과 연습 순서는 [조작법](xr-simulator.md)에 정리한다.

## 오프닝 파일과 클래스

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [FactoryIntroManager](../client/Assets/Scripts/FactoryIntro/FactoryIntroManager.cs) | 공용 리그를 찾아 시작 위치에 배치하고 이야기 단계와 튜토리얼을 순서대로 진행한다. 화면 버튼과 TutorialController의 단계 변경·완료 이벤트를 받아 PresentationController의 표시와 InputController의 입력 권한을 함께 바꾼다. 완료·건너뛰기 후에는 입력을 복원하고 공장 씬을 Single 모드로 연다. |
| [FactoryIntroPresentationController](../client/Assets/Scripts/FactoryIntro/FactoryIntroPresentationController.cs) | Manager가 지정한 제목·본문·버튼과 정상·손상 배경, 연습 대상, 진행 안내를 표시한다. 습격 연출과 태블릿에서 펼쳐지는 홀로그램을 실행하고 건너뛰기 확인창의 선택을 이벤트로 알린다. 연습 완료 판정은 TutorialController에 맡기고 화면과 연출을 담당한다. |
| [FactoryTutorialController](../client/Assets/Scripts/FactoryIntro/FactoryTutorialController.cs) | 현재 연습 단계와 단계 안의 입력 순서를 보관한다. 왼손 스틱 사용·목표 위치 도달, 선택 확인, 연습 물체 배치, 시작점 복귀, 태블릿 입력 순서를 검사해 완료 시 다음 단계와 안내 이벤트를 발생시킨다. 중립 입력 대기, 건너뛰기 확인 중 일시정지·재개도 처리한다. |
| [FactoryIntroInputController](../client/Assets/Scripts/FactoryIntro/FactoryIntroInputController.cs) | Bind 시 이동 Provider·태블릿·오른손 레이의 기존 입력과 활성 상태를 저장하고 XR UI 입력을 준비한다. Manager가 허용한 단계에 맞춰 이동·잡기·태블릿을 켜고 끄되 추적과 안내 버튼 선택은 유지한다. Release 시 저장한 설정을 되돌리고 자신이 추가한 InputAction·UI 입력 오브젝트를 정리한다. |

## 오프닝 실행 흐름

```text
Title → Introduction → Attack → Mission → Tutorial → Ready → Loading
```

1. `FactoryIntroManager.Start`가 공용 `XROrigin`을 찾고 `_exteriorStart`에 플레이어를 배치한다. `FactoryIntroInputController.Bind`와 `FactoryIntroPresentationController.BindPlayer`로 입력과 화면을 연결한다.
2. 화면의 계속 버튼은 `FactoryIntroManager.Continue`를 호출한다. Manager는 현재 단계에 따라 화면을 바꾸거나 `PlayAttack` 코루틴을 실행한다.
3. 튜토리얼 진입 시 `_interiorStart`로 이동하고 `BeginTutorial`을 호출한다. Presentation Controller는 태블릿 위치에서 안내창이 펼쳐지는 연출과 단계별 연습 대상을 표시한다.
4. Tutorial Controller의 `OnStepChanged`, `OnInstructionChanged`, `OnCompleted`를 Manager가 받아 입력 권한과 화면을 함께 갱신한다.
5. 완료하면 `Ready`로 전환한다. `EnterFactory`는 UI 콜백이 끝난 다음 프레임에 입력을 복원하고 `_nextSceneName`을 Single 모드로 연다. 기본 대상은 `FactoryEnvironment`다. 씬을 열 수 없으면 준비 화면에서 재시도할 수 있다.

플레이어 배치는 바닥 기준점과 시선 방향을 맞추되 카메라의 추적 높이를 보존한다. 공용 리그의 수명과 중복 제거는 [XR 구성](xr-setup.md)을 따른다.

## 튜토리얼 판정과 입력 권한

| 단계 | 완료 판정 | 허용하는 게임 입력 |
| --- | --- | --- |
| `Movement` | 왼손 스틱 입력을 한 번 이상 사용하고 카메라의 수평 위치가 `_movementTarget` 반경 안에 들어옴 | 이동·화면 선택 |
| `Selection` | `ConfirmSelection` 호출 | 화면 선택 |
| `Grab` | 리그에서 연습 물체를 잡은 뒤 취소되지 않은 해제로 `_placementTarget` 반경 안에 놓음 | 잡기·화면 선택 |
| `Return` | `ReturnToStart`가 리그 방향과 위치를 시작점으로 이동시키는 데 성공 | 화면 선택 |
| `Tablet` | 정해진 입력 순서를 수행하고 마지막 입력을 놓음 | 태블릿·화면 선택 |
| `Complete` | `OnCompleted` 발생 | 기존 설정에 따라 이동·태블릿·잡기 복원 |

이동 판정에는 `_targetRadius`, 배치 판정에는 `_placementRadius`를 사용한다. 잡기 완료는 XRI 해제 콜백 다음 프레임에 반영해 상호작용 대상을 처리 도중 숨기지 않는다.

태블릿 연습은 왼손 컨트롤러의 입력을 관찰한다. 각 입력 사이에 버튼과 스틱의 중립 상태가 필요하며, 연결이나 추적이 끊기면 같은 단계에서 기다린다. 태블릿의 내부 학습 진행률을 완료 조건으로 사용하지 않는다.

건너뛰기 확인창을 열면 Tutorial Controller를 일시정지하고 화면 선택용 입력만 허용한다. 취소하면 현재 단계를 재개하며, 태블릿 단계는 입력 순서를 처음부터 다시 안내한다. 확정하면 튜토리얼을 정리하고 `Ready`로 전환한다.

`FactoryIntroInputController.Bind`는 이동 Provider, 태블릿 활성 상태, 오른손 Grip·UI 입력과 레이 표시 상태를 보관한다. 튜토리얼 중에도 추적과 오른손 레이를 유지하며, 완료 화면의 버튼을 사용할 수 있도록 씬을 떠날 때까지 레이를 켜 둔다. `Release`는 보관한 입력을 복원하고 자신이 만든 InputAction·EventSystem 또는 추가한 UI 모듈을 정리한다.

## 엔딩 파일과 실행 흐름

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [FactoryEndingManager](../client/Assets/Scripts/FactoryEnding/FactoryEndingManager.cs) | 플레이어를 엔딩 시작점으로 이동시키고 복구 연출이 끝날 때까지 기다린 뒤 복습 화면을 연다. 현재 상태가 복습일 때만 완료를, 완료 상태일 때만 재시작·종료를 처리한다. 재시작은 입력 콜백이 끝난 뒤 시작 씬을 로드하고 실패하면 완료 화면을 복원한다. |
| [FactoryRestorationController](../client/Assets/Scripts/FactoryEnding/FactoryRestorationController.cs) | 씬에 연결된 Renderer·광원·회전 부품·파편의 초기 상태를 기준으로 `RestoreFactory()` 코루틴을 실행한다. 진행도에 따라 색상·광원 세기·회전·파편 크기를 보간하고 복구된 장식을 표시한다. Manager는 이 코루틴의 종료를 기다려 복습 단계로 넘어간다. |
| [FactoryEndingPanelController](../client/Assets/Scripts/FactoryEnding/FactoryEndingPanelController.cs) | Manager의 상태에 맞춰 복구 안내·복습 카드·완료 화면과 사용 가능한 버튼을 표시한다. 버튼은 완료·재시작·종료 요청 이벤트를 발생시키며 실제 씬 이동과 종료 판단은 Manager에 맡긴다. 오른손 Trigger를 UI 클릭에 연결하고 비활성화 시 이전 레이 설정을 복원한다. |

`FactoryEndingManager.Start`는 플레이어를 `_entryPoint`로 이동하고 복구 연출을 시작한다. 연출이 끝나면 여섯 개념의 복습 카드를 표시한다. 완료 요청은 `Recap`에서만, 재시작·종료 요청은 `Complete`에서만 처리한다.

`FactoryRestorationController`는 초기 부품 회전과 파편 크기를 저장하고 진행도에 따라 보간한다. Renderer 색상은 `MaterialPropertyBlock`의 `_BaseColor`, `_EmissionColor`로 갱신한다.

재시작은 `_restartSceneName`을 Single 모드로 로드하며 기본값은 `FactoryIntro`다. 로드 전에 패널을 비활성화해 UI 입력을 복원하고, 로드 실패 시 완료 화면을 다시 활성화한다. 종료는 플레이어에서 `Application.Quit`를 호출하고 에디터에서는 안내 문구를 표시한다.

공장에서는 `CNNModelDesignLab`의 정상 학습 완료·결과 표시 후 뒤쪽 출구를 안내한다. `FactoryLearningFlowManager`가 플레이어의 출구 진입을 확인해 `FactoryEnding`을 Single 모드로 연다. 엔딩 씬은 별도의 학습 결과나 양피지 수집 데이터를 받지 않고 기존 복구 연출을 자동 시작한다. 이 연결은 Unity 실행·테스트·빌드·컴파일 없이 정적으로 반영했다.

## 씬 구성 도구와 참조

| 파일 | 역할 |
| --- | --- |
| [FactoryStorySceneBuilder](../client/Assets/Editor/FactoryStorySceneBuilder.cs) | `CreateMissingScenes()`에서 오프닝·엔딩 파일의 존재 여부를 확인하고 없는 씬만 각 Builder로 생성한다. Build Settings에는 누락된 씬 항목을 추가해 이후 Manager가 이름으로 로드할 수 있게 한다. |
| [FactoryIntroSceneBuilder](../client/Assets/Editor/FactoryIntroSceneBuilder.cs) | 공용 리그, 오프닝 배경·Canvas, 이동 표식·연습 물체를 씬에 만든다. 시작점과 연습 판정 대상은 Manager·TutorialController에, 표시 대상과 버튼 콜백은 PresentationController·Manager에 연결해 저장한다. |
| [FactoryEndingSceneBuilder](../client/Assets/Editor/FactoryEndingSceneBuilder.cs) | 공용 리그와 엔딩 시작점, 손상·복구 대상, 복습 카드와 완료 버튼을 생성한다. 복구에 사용할 Renderer·광원·부품은 RestorationController에, 화면과 버튼은 PanelController에 연결하고 EndingManager가 두 기능을 조율하도록 참조를 저장한다. |

두 씬 생성기는 대상 씬이 이미 있으면 새로 덮어쓰지 않는다. 생성할 때 공용 [XRPlayerRig](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)와 [Noto Sans KR](../client/Assets/Fonts/NotoSansCJKkr-Regular.otf)를 참조하고, 생성 후 이전 활성 씬을 복원한다.

장면의 기본 배치와 UI 참조는 씬에 직렬화되어 있다. 오프닝의 진행 표시·홀로그램 장식·건너뛰기 확인창은 `FactoryIntroPresentationController`가 런타임에 생성한다. 습격 알람은 해당 Controller의 `_alarmClip`과 `_audio` 참조로 연결한다.
