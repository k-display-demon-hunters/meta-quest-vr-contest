# 공장 오프닝·입구 안내와 엔딩

실제 실행 흐름은 `FactoryIntro → FactoryEnvironment → FactoryTutorial → CNNPixelLab → CNNFeatureMapLab → CNNModelDesignLab → FactoryEnding`이다. 튜토리얼과 세 학습 씬은 공장을 유지한 채 Additive로 실행한다. 방의 표시 이름은 Pixel Lab, Feature Map Lab, Model Design Lab이며 학습 씬 파일명과 완료 조건은 유지한다.

## 시작 화면과 18초 마을 오프닝

시작하기를 한 번 누르면 평화로운 마을에서 공장 습격과 화면 화질 저하로 이어지는 18초 오프닝을 재생하고, 기존 `FactoryEnvironment` 진입 흐름으로 자동 전환한다. [화면 설계서](screen-design.pdf) 3쪽의 핵심 사건은 화질 개선 공장의 정상 가동 → 악령 습격과 시설 파괴 → 세상의 사진·영상 저화질화 → 복구 임무다. 마을과 화면을 보는 주민, 15~20초 애니메이션이라는 구체적인 연출은 사용자 요청을 반영했다. 화면 콘텐츠의 화질이 나빠지며 마을의 지형·건물 전체를 픽셀화하지 않는다.

시작 버튼을 누르기 전부터 주민의 산책·시청·대화 동작과 방송, 구름·기계 동작을 갱신한다. 시작 패널은 광장 아래쪽에 작게 배치해 움직이는 마을이 보이게 한다. 버튼을 누르면 안내 Canvas 전체와 버튼·선택용 레이저를 즉시 숨기고 대기 중이던 주민 위치·걷기 주기·방송 시간을 이어서 18초 사건 타임라인을 시작한다. 사건 중에는 안내 자막을 표시하지 않는다.

| 재생 시간 | 화면과 동작 |
| --- | --- |
| 0~4초 | 밝은 마을과 공장, 움직이는 구름·주민·코어 회전부, 선명한 디스플레이를 보여 준다. 안내창과 버튼은 이미 사라진 상태다. |
| 4~7초 | 하늘과 조명이 어두워지고 보라색 균열에서 악령이 나타나 공장으로 접근한다. 주민들이 시간차를 두고 걷기를 멈추고 악령을 올려다보거나 손을 들어 가리키며 뒤로 물러난다. |
| 7~10.5초 | 악령이 공격을 충전해 8.2초에 코어를 타격한다. 주민은 몸을 숙이고 머리를 보호한다. 충격파·파편·연기, 멈추는 회전부, 끊어지는 공급 조명이 손상을 보여 준다. |
| 10.5~14초 | 방송 화면에 차례로 노이즈가 생기고 마지막 방송 프레임이 큰 픽셀과 흐린 색으로 변한다. 주민들이 망가진 화면을 바라보며 팔을 벌리거나 고개를 젓는다. |
| 14~18초 | 손상된 공장과 입구 표시를 보여 준다. 연출 종료 후 페이드 전환으로 실제 공장 입구에 진입한다. |

카메라를 연출용 경로로 이동시키거나 흔들지 않는다. 시작 지점에 배치한 공용 XR 리그에서 머리 추적을 유지하며, 마을 오브젝트와 빛·화면을 움직여 사건을 표현한다. 공용 카메라는 원래 단색 배경을 사용하므로 오프닝 동안만 Skybox 모드로 바꾸고, 씬을 떠날 때 원래 Clear Flags를 복원한다.

| 파일·클래스 | 책임 |
| --- | --- |
| `Scripts/FactoryIntro/FactoryIntroManager.cs` | 타이틀의 평화로운 마을 갱신 → 18초 이야기 → 공장 로드를 조율하고 중복 시작을 막는다. 공장 로드 실패 시 재시도 버튼을 표시한다. |
| `Scripts/FactoryIntro/FactoryIntroPresentationController.cs` | 제목·버튼을 표시하고, 시작 시 안내 Canvas 전체를 즉시 숨겨 UI 입력도 막는다. 로드 실패 시에는 같은 패널에 재시도를 표시한다. |
| `Scripts/FactoryIntro/FactoryIntroCinematicController.cs` | 마을·화면·악령의 공통 시간, 균열·충격파·파편, 하늘·조명, 오디오를 제어한다. 씬 종료 때 오디오와 생성한 하늘 재질·파편 메시를 정리한다. |
| `Scripts/FactoryIntro/FactoryVillageController.cs` | 구름과 주민의 보행·관절·시선·등장/충격/화면 고장 반응, 코어·회전부, 공장 손상과 공급 조명, 입구 표시를 제어한다. |
| `Scripts/FactoryIntro/FactoryVillageDisplayController.cs` | 대기/이야기 중 방송 시간을 이어서 전달하며 디스플레이별 고장 시점과 재생 정지·화질 손실·노이즈 값을 셰이더에 전달한다. |
| `Scripts/FactoryIntro/FactoryWraithController.cs` | 기존 악령의 부유·파편 회전·공격선을 담당하며, 오프닝에서는 외부에서 전달한 시간과 공격 목표로 동작한다. 시작 화면에서는 악령이 비활성이다. |
| `Scripts/FactoryIntro/FactoryIntroInputController.cs` | 이야기 중 이동·잡기를 잠시 보류한다. 시작·완료·재시도 패널에서만 오른손 UI ray를 표시하고 연출 중에는 숨긴다. 씬 전환 전에 원래 입력 상태를 복원한다. 태블릿 컴포넌트나 입력은 변경하지 않는다. |
| `Scripts/Core/SceneTransitionController.cs` | persistent 리그에서 페이드아웃 → Single 로드 → 페이드인을 수행한다. 씬 로드에 성공하면 페이드인 후 자신을 정리한다. 제거한 Kdy 참고 씬에 대한 전환 예외도 정리했다. |

`FactoryIntro.unity`는 공용 XR 리그와 시작 UI, 기존 `PixelWraith`를 유지하고, 배경을 `Prefabs/FactoryIntro/VillageOpening.prefab`으로 교체한다. 마을은 씬에서 참조하는 직렬화된 프리팹이며, 실행 중에 마을 전체를 새로 생성하지 않는다. 고정 배경은 재질별 메시로 묶고 움직이는 부품은 따로 배치한다. 씬 전용 메시·재질·셰이더는 각각 `Meshes/FactoryIntro`, `Materials/FactoryIntro`, `Shaders/FactoryIntro`에 둔다. 화면과 조명은 캐시한 `MaterialPropertyBlock`으로 표시 값을 갱신하고, 충격 파편은 작은 메시 하나와 기존 효과 재질을 공유한다. 성능 측정이나 Quest 실행 확인은 아직 수행하지 않았다.

마을의 세 디스플레이는 검은 베젤·스피커·표시등이 있는 TV 장치로 구성한다. `Materials/FactoryIntro/VillageDisplay.mat`과 `VillageDisplay.shader`로 자체 제작한 가상 뉴스·축구 중계·SF 영화 화면을 각각 재생한다. 뉴스의 진행자와 속보 띠, 축구의 선수와 공·점수판, 영화의 우주선·행성 등 방송 내용이 움직인다. 실제 방송사 로고, 실제 경기·영화 클립, 외부 영상·이미지·글꼴 텍스처는 사용하지 않는다. 기존 `ColorPhoto.png`는 현재 오프닝에서 참조하지 않으며 이전 사용 이력으로만 보관한다.

관람 공간은 중앙 보행로 바깥의 두 목조 지붕 아래에 배치하고, 작은 뉴스 안내판은 길가에 둔다. 네 벤치는 방송 화면을 향하며 좌면은 포장면에서 약 47cm 높이다. 관람용 화면은 지붕 구조물의 지지대에 연결하고, 뉴스 안내판은 바닥 받침과 차양을 갖춘다. 산책 주민은 중앙 보행로 가장자리에, 시청 주민은 해당 화면 앞 관람 공간에 배치한다. 공장 입구와 코어를 가리지 않도록 중앙 시야를 비운다.

주민 외형은 기존 메시·재질을 유지한다. Quaternius **Universal Animation Library Standard**의 실제 관절 곡선을 주민 비율에 맞춘 Unity 직렬화 에셋 `Meshes/Common/QuaterniusVillageMotion.asset`으로 변환한다. 원본은 30fps이며, 재생 중에는 프레임 사이 회전을 보간한다. `Scripts/Core/VillageResidentMotionLibrary.cs`가 동작 데이터를 제공하고 `VillageResidentMotionController.cs`가 기존 허리·머리·팔·다리와 보강한 팔꿈치를 구동한다. 새 캐릭터 팩이나 런타임 모델 임포트 패키지는 사용하지 않는다.

대기·걷기는 `Idle_Loop`·`Walk_Loop`, 대화와 화면 고장 반응은 `Idle_Talking_Loop`, 악령 출현·타격 반응은 `Hit_Chest`·`Hit_Head`·`Crouch_Idle_Loop`, 엔딩 축하는 `Dance_Loop`를 혼합한다. 무료 Standard에는 전용 Surprise 클립이 없으므로 충격·웅크림 동작과 시선·몸 방향 전환으로 놀람을 표현한다. 출처와 CC0 확인은 제출용 Markdown에 기록한다.

각 화면은 10.5/10.85/11.2초에 재생을 멈춘다. 선명하던 동일 방송의 마지막 프레임에 큰 픽셀·흐림·색 손실·노이즈를 적용한다. 타이틀의 방송은 `EvaluateIdle()`, 사건 시작은 `BeginStory()`, 사건 중 표시는 `Evaluate()`를 사용하므로 시작 버튼을 눌러도 방송이 처음으로 되돌아가지 않는다. 전체 화면 후처리나 별도 디스플레이 카메라는 사용하지 않는다.

외부에서 가져온 에셋과 직접 생성·합성한 음성·음원·이미지·영상 등의 출처와 저작권은 [제출용 에셋 출처·저작권 목록](ASSET_CREDITS.md)에 Markdown으로 정리한다. 에셋명·사용처·출처 또는 제작 방법·저작권·라이선스·수정 여부를 기록하며, Unity 내장 에셋과 기본 패키지는 표기 대상에서 제외한다. 런타임 원본은 기존 `Assets` 배치에 유지한다.

`Audio/FactoryIntro/VillageOpening.wav`는 직접 합성한 18초 스테레오 효과음이다. 32kHz·16-bit PCM이며, 따뜻한 차임에서 불길한 저음·충전음으로 이어지고 8.2초 타격, 10.5초 화면 이상, 마지막 복구 요청의 여운에 맞춘다. `AudioSource.playOnAwake`는 끄고 시작 버튼 뒤에 재생한다. 발화와 외부 제공 음원은 없다.

입력 잠금과 복원은 기존 `FactoryIntroInputController`가 담당한다. 이동·잡기를 보류하되 머리·손 추적은 유지한다. `SetUIVisible()`은 연출 중 UI ray와 선을 함께 숨겨 보이지 않는 클릭도 차단하며, 패널을 다시 표시하면 ray를 활성화한다. 공장을 열기 전에 저장해 둔 입력 상태를 복원한다. 연출용 하늘·주변광은 이전 값을 보관하고, 같은 씬에서 비활성화될 때 복원한다. 다음 씬이 활성화된 뒤에는 그 씬의 환경 설정을 덮어쓰지 않는다. 현재 학습 흐름은 유지하며, 사용하지 않는 `kdy_scene1~17`은 아래 정리 범위에 따라 제거했다.

## 실제 공장 입구에서 한 번 안내

`FactoryLearningFlowManager`는 `FactoryPlayerController`에 요청해 실제 공장 입구에 플레이어를 배치한 뒤 `FactorySceneLoadController`에 요청해 `FactoryTutorial`을 Additive로 한 번 불러온다. 튜토리얼 로드·초기화·완료 대기·언로드는 Controller가 담당한다. Controller는 튜토리얼 언로드 후 안내판도 표시한다. Manager는 튜토리얼 완료를 기다리지 않고 학습 진입을 허용하며, 첫 학습 진입 시 종료·언로드를 요청한 뒤 학습 씬을 로드한다. 공장 배경과 공용 리그는 그대로 유지하며, 튜토리얼 씬에는 별도 리그·카메라·배경·EventSystem을 두지 않는다.

| 파일·클래스 | 책임 |
| --- | --- |
| `Scenes/FactoryTutorial.unity` | 공장 입구에 추가로 표시하는 안내 씬이다. 공장과 공용 리그가 로드된 상태에서 실행한다. |
| `Scripts/FactoryTutorial/FactoryTutorialManager.cs` | 안내 페이지 순서·시작·완료와 안내 중 ray 유지를 담당한다. `IsComplete`로 공장 진행 코드에 완료 여부를 제공한다. |
| `Scripts/FactoryTutorial/FactoryTutorialPanelController.cs` | 안내 Canvas와 이전/다음/안내 마치기 버튼을 구성하고 페이지 내용을 표시한다. |

이동·선택·잡기·정면 복원·태블릿 설명의 다섯 페이지를 이전/다음/안내 마치기로 넘긴다. 고개 회전, 이동 목표 도착, 잡기 성공, 태블릿 입력 순서 등 수행 판정은 없다. 안내 중에는 ray를 켠 상태로 유지하며, 안내를 완료하지 않고 첫 학습에 진입하면 튜토리얼을 종료·언로드한다. 이동·잡기·태블릿 조작을 페이지별로 제한하지 않는다.

공장 SceneLoadController는 `FactoryTutorialManager.IsComplete` 또는 첫 학습 진입에 따른 종료 요청을 기다린 뒤 튜토리얼 씬을 언로드하고 고정 표지판을 표시한다. Manager는 시작 시 첫 바닥 경로를 표시한다. 공장의 공용 EventSystem은 안내 씬과 함께 언로드되지 않으므로 이후 학습 UI에도 사용한다. 공장 세션 안에서는 방 재방문·Additive 학습 전환으로 안내를 다시 열지 않는다. 엔딩 후 처음부터 새 체험을 시작하면 다시 한 번 표시한다. 별도 저장 키나 전역 상태는 추가하지 않았다.

기존 `FactoryTutorialController.cs`는 `FactoryTutorialManager.cs`로 이름을 바꾸고 Panel 코드와 함께 `Scripts/FactoryTutorial`로 옮겼으며 각 `.meta` GUID를 보존했다. 태블릿 코드·프리팹·폰트·저장 형식은 변경하지 않았다.

## 공장 복구와 엔딩

`FactoryEndingManager`는 기존대로 정상 학습 완료 뒤 공장 출구를 통해 진입하며, 23초 마을 복구 연출 → 축하 메시지 → 처음으로/앱 종료를 조율한다. 별도 개념 복습 카드는 제거한다. 참고 브랜치 전용의 간판 검사나 가상 학습 조건에 의존하지 않는다.

엔딩은 인트로와 같은 `VillageOpening.prefab`을 참조해 같은 마을·공장·주민·방송 화면에서 진행한다. 마을 에셋을 엔딩용으로 복사하지 않는다. 기존 에셋의 경로와 GUID는 유지한다. 카메라를 강제로 이동시키지 않고 공용 XR 리그의 머리 추적을 유지한다.

| 재생 시간 | 화면과 동작 |
| --- | --- |
| 0~3초 | 어두운 공장, 멈춘 기계와 손상된 방송 화면을 보여 준다. 안내창과 버튼은 숨긴다. |
| 3~8초 | 코어에 빛이 모이고 파손된 시설이 복구되며 공장 조명이 켜진다. |
| 8~13초 | 공급 조명을 따라 빛이 마을로 퍼지고 세 방송 화면이 차례로 선명해진다. |
| 13~19초 | 주민이 복구된 화면을 보고 안도·축하 동작을 보인다. 하늘과 공원도 밝아진다. |
| 19~23초 | 기계와 방송, 주민의 일상 동작이 이어지는 평화로운 마을을 보여 준다. |
| 23초 이후 | `축하합니다. 공장에 빛이 돌아왔어요.` 메시지와 처음으로/앱 종료 버튼을 표시한다. |

`FactoryRestorationController`는 공장·화면 복구, 빛의 이동과 확산, 하늘·조명을 제어한다. `FactoryEndingPanelController`는 연출 중 Canvas를 숨기고 완료 후 축하 메시지와 공통 버튼 피드백을 표시한다. `FactoryEndingManager`가 플레이어 배치·입력 잠금·재시작을 조율한다.

## 공통 UI와 씬 구성 도구

공통 표시 값은 `Scripts/Core/SceneUIStyle.cs`, uGUI hover/누름 외곽선은 `SceneButtonFeedbackController.cs`가 담당한다. 버튼은 어두운 공장 패널·청록 강조·흰 글자로 맞췄다. XRI 3D 버튼은 각 씬 Controller에서 같은 상태 색상을 사용한다. 계산과 학습 데이터의 의미 색상은 유지한다. 태블릿에는 이 스타일을 적용하지 않는다.

`XRPlayerInputController`는 `sceneLoaded`와 `EnsureRayReady()`에서 오른손 ray의 UI 모듈 등록을 확인하고, 이전 씬의 모듈이 사라져 등록이 끊겼으면 복원한다. persistent 리그와 씬별 EventSystem의 수명이 달라 생기는 입력 누락을 공용 입력 코드에서 처리한다.

`FactoryIntroSceneBuilder`는 마을 프리팹과 악령, 연출 Controller, 하늘·효과 재질, 오디오와 UI 참조를 연결한다. `FactoryEndingSceneBuilder`는 같은 마을 프리팹과 복구 Controller, 하늘·효과 재질을 연결한다. 필수 자산을 확인한 뒤 새 씬을 만들며, 기존 씬이 있으면 덮어쓰지 않는다. 씬을 열 때 자동 재생성하지 않는다. 기존 에셋의 경로와 GUID를 유지하며 기타 참고 씬·에셋을 일괄 개명하지 않는다.

## Kdy 참고 파일 정리

2026-10-24: 현재 실행 흐름과 별개인 `Scenes/kdy_scene1~17.unity`, `Prefabs/Common/KdyLessonTablet.prefab`·`KdyFactoryHall.prefab`·`KdyCoreFault.prefab`, `Materials/Common/KdyLessonFilter.mat`·`KdyLessonPhoto.mat`·`KdyBlack.mat`·`KdyCyan.mat`·`KdyFloor.mat`·`KdySteelEdge.mat`·`KdyLessonPixel.mat`, `Textures/Common/KdyFactoryInput.png`를 해당 `.meta`와 함께 제거했다. 스크립트는 아래 보존 대상 9개를 제외한 Kdy 스크립트 23개를 제거했다. 비게 된 `Scripts/kdy_scene*` 폴더 11개와 `Prefabs/Common` 폴더 및 각 폴더 `.meta`도 제거했다. `Textures/Common`에는 태블릿에서 사용하는 `LearningParchment.png`가 있으므로 유지한다. 전체 삭제 범위는 원본 파일 51개와 `.meta` 63개다.

현재 오프닝에서 사용하는 에셋 6개는 경로·내용·GUID를 유지한다. 프리팹은 `Prefabs/kdy_scene2/PixelWraith.prefab`이며, 재질은 `Materials/Common`의 `KdyAmber.mat`, `KdySteel.mat`, `KdyViolet.mat`, `KdyWraith.mat`와 `Materials/kdy_scene2/KdyAttackRay.mat`다. 이전 공장 배경과 그 전용 재질은 현재 씬 및 보존 코드의 의존성이 없어 제거했다.

2026-10-30: 이전 정리에서 재사용 참고용으로 남긴 스크립트 9개도 현재 실행 흐름에서 사용하지 않아 제거했다. 삭제 대상은 `Scripts/kdy_scene6/KdyConvolutionController.cs`, `Scripts/kdy_scene9/KdyActivationLeverController.cs`, `Scripts/kdy_scene16/KdySignComparisonController.cs`와 `Scripts/Core/`의 `KdyLessonGrabController.cs`, `KdyLessonProgressController.cs`, `KdyNetworkConfiguration.cs`, `KdyFactorySignExampleData.cs`, `KdyTrainingExampleData.cs`, `KdySignExampleData.cs`다. 각 스크립트의 `.meta`와 비게 된 `Scripts/kdy_scene6`, `Scripts/kdy_scene9`, `Scripts/kdy_scene16` 폴더 및 폴더 `.meta`를 함께 제거했다. 이번 삭제 범위는 스크립트 9개와 `.meta` 12개다.

현재 오프닝에서 사용하는 위 에셋 6개와 해당 `.meta`, `Prefabs/kdy_scene2`·`Materials/kdy_scene2` 폴더는 유지한다. `FactoryIntro` 씬과 `PixelWraith` 프리팹의 GUID 참조, `FactoryIntroSceneBuilder`의 에셋 경로도 그대로 유지한다. 이전 Kdy 씬과 참고 스크립트는 현재 프로젝트에 남아 있지 않다.

## 확인 범위

2026-10-30 Kdy 후속 정리: fetch 후 로컬 `main`과 최신 `origin/main`의 커밋·파일 차이가 없음을 확인하고 새 작업 브랜치에서 정리했다. 삭제 대상 코드의 타입 참조가 해당 9개 파일 내부에만 있고 씬·프리팹 등에서 스크립트 GUID를 참조하지 않음을 정적으로 확인했다. 삭제 후 유지 파일의 타입·GUID 참조와 변경 목록을 확인했다. 사용 중인 에셋 6개 및 `.meta`, 씬·프리팹·에디터 도구는 변경하지 않았다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다.

2026-10-24 Kdy 정리: 최신 `main`에서 별도 브랜치와 워크트리를 만들고 코드 타입 참조와 Unity GUID 참조를 정적으로 검토했다. 삭제 대상 타입·GUID를 유지 파일에서 참조하지 않는지, 보존한 9개 스크립트의 의존성이 연결되는지, 지정한 에셋과 재사용 후보의 내용 및 보존 파일의 `.meta`가 유지되는지 확인했다. Unity 실행·Play·테스트·빌드·컴파일은 수행하지 않았다.

2026-10-23 후속 변경: Quaternius 무료 Standard 원본의 동봉 CC0 표기와 실제 7개 동작 데이터를 확인해 기존 주민 관절에 연결했다. 관람 공간·벤치·키오스크·주민 경로를 수정하고 시작 UI 즉시 숨김, 같은 마을의 23초 복구 엔딩을 반영했다. 코드·직렬화 참조·정적 배치 그림을 검토했다. 정적 배치 그림의 화면 색은 자리 표시이며 Unity 렌더링 결과가 아니다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다. 동작 전환의 실제 표시, Quest 성능과 입력은 실행 확인 전이다.

2026-10-23: 사용자가 앞선 18초 오프닝의 정상 동작을 직접 확인했다고 알려 주었다. 이후 방송 화면 교체·타이틀의 주민 보행·악령 반응 강화·제출용 출처·저작권 Markdown 문서를 추가했다. 이번 후속 변경은 소스·씬·에셋 참조를 정적으로 검토하며, 앞선 실행 확인을 후속 코드의 실행 결과로 간주하지 않는다. Unity·Play·테스트·빌드·컴파일은 새로 수행하지 않았다.

2026-10-22 마을 오프닝: 기존 `feature/scene-ui-and-guidance`에서 분기한 작업으로, 소스와 에셋·씬 연결을 정적으로 검토한다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성 및 씬 구성 도구 실행은 수행하지 않았다. 18초 화면 구성, 실제 렌더링·효과음 청취, Quest 성능과 입력은 실행 확인 전이다.

2026-10-22: 최신 main에서 작업 브랜치를 만들고 참고 브랜치를 일반 Merge했다. 기존 방 진입, 학습 완료 조건, 이동 속도 3m/s, XR 설정과 태블릿 파일이 유지되는지 정적으로 대조했다. 안내 버튼 문제를 검토하면서 Single 씬 전환으로 UI 모듈이 사라진 뒤 persistent ray가 새 모듈에 등록되지 않는 경로를 소스에서 확인했다. 튜토리얼은 별도 Additive 씬으로 분리하고 공용 입력 코드에 등록 복원을 반영했다. 씬 로컬 참조·프리팹 GUID·변경 내용을 정적으로 검토했으며, 사용자 지시에 따라 Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본은 수행하지 않았다. 실제 장면 표시와 버튼 입력 복구·Quest 컨트롤러 동작은 실행 확인 전이다.

## 미션 중심 입구 안내 (2026-10-26)

FactoryTutorial은 전체 미션 → 태블릿의 역할 → 진행률·지도 → 양피지 수집 → 이동·선택·잡기·정면 복원 → 태블릿 Trigger·스틱·X·Y·Menu → 첫 목적지의 14단계로 구성한다. 이전·다음·건너뛰기를 제공한다. 세 방에서는 각 방의 CNN 역할을 별도 소개한 뒤 체험을 시작한다.

`FactoryTutorialVisualController`는 확대된 좌·우 Touch Plus 모델과 태블릿 연습 그림을 표시한다. 노란 표식으로 버튼 위치를 안내하고 실제 입력에 애니메이션과 상태 문구가 반응한다. 태블릿 그림은 실제 UI의 복제 화면이 아닌 조작 연습 도식이며 같은 입력으로 선택·뒤로·설정 동작을 보여준다. 실제 태블릿도 기존 입력을 유지한다. 안내 화면 이동 버튼은 연습 성공 여부에 관계없이 사용할 수 있다.

사용자 허가 후 이번 구현을 컴파일하고 Play에서 14페이지 전환·건너뛰기와 세 방의 시작 안내를 확인했다. 카메라 렌더를 확인해 확대 모델의 배경 겹침·기울기와 태블릿 그림의 정렬을 수정했다. 버튼 표식은 공식 모델의 centimetre 단위를 보정한다. 버튼 콜백 호출과 개발자 이동으로 확인한 결과이며, 실제 Quest 입력·전체 체험 완료·APK 검증은 수행하지 않았다. 상세 확인 범위는 factory-environment.md를 참고한다.
