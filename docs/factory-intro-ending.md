# 공장 오프닝·입구 안내와 엔딩

실행 흐름은 FactoryIntro → FactoryEnvironment + FactoryTutorial → CNNPixelLab → CNNFeatureMapLab → CNNModelDesignLab → FactoryEnding이다. 공장에 들어간 뒤 조작 안내와 최종 미션 안내를 확인해야 실습 진입이 시작된다. 공장 배경과 XR 리그는 기존 구조를 유지한다.

## 오프닝·엔딩 폴더와 책임

오프닝과 엔딩은 에셋 관리상 OpeningEnding으로 묶으며 실제 씬 파일은 분리한다. 이동한 .meta와 GUID는 유지한다.

| 위치 | 내용 / namespace |
| --- | --- |
| Scripts/OpeningEnding/ | 공통 마을·디스플레이·입력 제어 / OpeningEnding |
| Scripts/OpeningEnding/VillageResident/ | 주민 동작 Controller·MotionLibrary / OpeningEnding |
| Scripts/OpeningEnding/Intro/ | 오프닝 Manager·Presentation·Cinematic·Entry·악령 Controller / OpeningEnding.Intro |
| Scripts/OpeningEnding/Ending/ | 엔딩 Manager·Panel·Restoration / OpeningEnding.Ending |
| Prefabs/OpeningEnding/ | 공유 VillageOpening, PixelWraith, 오프닝 전용 FactoryEntrance |
| Materials/OpeningEnding/ | 마을·화면·악령 및 입장 경로 재질. 기존 엔딩 재질은 Ending/ 하위 |
| Meshes/OpeningEnding/ | 마을 메시와 QuaterniusVillageMotion.asset |
| Shaders/OpeningEnding/, Audio/OpeningEnding/ | 방송·하늘 셰이더와 오프닝 음원 |
| Editor/ | 씬 구성 도구. 새 에셋 경로·입력·표시·입장·음원 참조 연결 |

FactoryIntroManager는 진행 단계와 Controller 호출 순서를 관리한다. PresentationController는 안내창과 버튼을, CinematicController는 기존 18초 연출과 효과음·진동을, EntryController는 문·경로·왼손 표시·입장 판정을 담당한다. 공통 FactoryIntroInputController는 기존 이름을 유지하며 오프닝·엔딩의 입력 잠금과 복원을 담당한다. A 입력과 UI 버튼은 로컬 OnContinueRequested 이벤트로 Manager에 요청하며 활성 기간에 대칭 구독·해제한다.

## 오프닝 진행

| 단계 | 안내 / 조작 |
| --- | --- |
| 첫 화면 | “CNN 공장에 오신 것을 환영합니다” / “오른손 A 버튼을 눌러 시작하세요.” 이동·잡기와 선택 레이를 잠시 보류한다. |
| 세계관 | “선명한 화면의 비밀” / “이 마을의 사진과 영상은 CNN 공장의 AI 업스케일링으로 선명해져요.” [다음] |
| 기술 설명 | “작은 이미지를 더 선명하게” / “CNN은 학습한 이미지의 특징을 활용해 저해상도 이미지를 고해상도로 바꿔요.” [계속] |
| 습격 | 설명 종료 후 기존 18초 연출. 4.3~12초에 빨간 “CNN 공장에 이상 발생!” 경고. 선택 버튼은 없다. |
| 복구 요청 | “마을에 선명함을 되찾아 주세요” / “공장 안의 미션을 완료해 멈춰 버린 CNN 공장을 복구해 주세요!” [공장 수리하기] |
| 문 열기 | 안내창을 숨기고 무거운 기계 문소리에 맞춰 3.2초 동안 양쪽 문을 연다. |
| 직접 입장 | “왼쪽 조이스틱을 앞으로 밀어 빛나는 길을 따라가세요.” 왼손 컨트롤러·조이스틱과 바닥 경로를 강조한다. 입구 판정선에 도착하면 페이드로 공장 내부를 로드한다. |

세계관·기술·복구 요청·재시도 화면은 UI 선택 또는 오른손 A로 진행한다. 같은 프레임의 A와 UI 클릭은 한 단계만 진행한다. A가 안내 진행에 쓰이는 동안 기존 정면 복원 동작은 보류된다. 연출 중에는 진행 입력을 받지 않는다. 씬 로드 실패 시 [다시 시도]를 제공한다.

CNN 설명은 디스플레이 장치의 물리적 해상도가 아닌 입력 이미지의 해상도 개선을 뜻한다. [SRCNN 원 논문](https://arxiv.org/abs/1501.00092)에 근거한 게임 설정이며 원본 세부 정보의 완전한 복원을 보장한다는 표현은 사용하지 않는다.

## 연출·입장 공간

마을 배치, 주민 외형·동작, 세 디스플레이와 방송 콘텐츠, 공장·악령의 기존 큰 형태를 유지한다. 주민은 Quaternius 동작 데이터와 기존 관절을 사용한다. TV의 뉴스·축구·SF 방송은 프로젝트 셰이더 그래픽이며 외부 방송 영상을 사용하지 않는다.

밝고 평화로운 시작에서 습격과 함께 어두워지는 대비를 유지한다. 외벽은 밝은 회백색·따뜻한 회색, 바닥은 콘크리트·포장 톤, 금속은 짙은 청회색, 지붕은 차분한 벽돌색으로 정리했다. 잔디·나무·주민 피부와 옷·방송·악령 색은 유지한다. 낮은 중립적인 따뜻한 햇빛, 습격은 어두운 청회색 조명이며 엔딩 복구 때 같은 낮 팔레트로 돌아온다. 실사 텍스처나 외부 모델을 추가하지 않았다.

| 연출 시간 | 사건 |
| --- | --- |
| 0~4초 | 기존 밝은 마을·공장·방송을 보여준다. 대기 중 움직임을 이어받는다. |
| 4.3초 | 악령 등장과 붉은 경고창, 경고 비프·긴장음·잡음, 짧은 양손 진동 |
| 7.2 / 8.2초 | 공격 충전 / 코어 타격에 맞춘 진동. 기존 파편·충격파·연기를 유지한다. |
| 10.5초 이후 | 디스플레이 고장·화질 손실, 짧은 진동. 화면별 고장 시간은 기존 10.5/10.85/11.2초다. |
| 12~18초 | 경고창이 사라지고 손상된 마을을 보여준 뒤 복구 요청을 표시한다. |

FactoryEntrance.prefab은 기존 입구 표시 위치(z=16.5 부근)에 미닫이문·충돌체와 바닥 경로를 추가한다. 문 중심 z=16.45, 입장 판정선 z=16.9다. 플레이어 카메라가 문 너머 영역에 들어갔는지 EntryController가 판정한다. 바닥 경로·손 강조 오브젝트는 프리팹에 저장하며 입장 단계에만 표시한다. 공장 씬으로 넘어갈 때 경로와 컨트롤러 강조, 오프닝 입력 상태를 정리한다. 카메라를 연출 경로로 강제 이동하거나 흔들지 않는다.

기존 VillageOpening.wav에 자체 합성 WraithAttackWarning.wav와 FactoryDoorOpen.wav를 추가했다. 외부 샘플 없이 합성한 제작 방법과 출처는 [ASSET_CREDITS.md](ASSET_CREDITS.md)에 기록한다. 진동은 연결된 기기의 지원 여부를 확인하며 짧은 임펄스로만 재생하고 비활성화 시 정리한다.

## 안내창 가독성

오프닝은 짧은 제목과 1~2줄 본문, 큰 단일 진행 버튼을 사용한다. 기본 제목 44, 본문 40, 버튼 46이며 버튼 영역은 600×100이다. A 안내는 별도 작은 캡션으로 분리한다. 첫 화면·경고·이동 안내는 배경 높이를 줄이고, 복구 요청은 반투명 어두운 배경과 흰 글자를 사용한다. 경고 배경은 초당 한 주기의 완만한 붉은색 변화로 표시한다. 이동 안내는 보행 중 시야 아래쪽에 유지한다. 사용자에게 보이는 입력 명칭은 “조이스틱”으로 통일한다.

## 공장 내부 조작 안내와 미션 시작

FactoryLearningFlowManager는 플레이어를 실제 공장 내부 입구에 배치하고 FactorySceneLoadController.RunTutorial을 기다린다. 튜토리얼 동안 방 진입 판정과 첫 실습실 경로 표시를 보류한다.

FactoryTutorial은 준비 안내 → 버튼 선택 → 이동·방향 전환 → 잡기 → 정면 맞추기 → 태블릿 역할 → 태블릿 항목 이동 → X 선택 → Y 뒤로 → 메뉴 설정 → 준비 완료의 11개 안내 화면으로 구성한다. 조작 안내는 [이전]·[다음], 마지막은 [미션 시작]이다. 시작 안내는 A로도 진행한다. 건너뛰기 버튼은 없다.

[미션 시작] 뒤 확대 컨트롤러·태블릿 연습 그림을 숨기고 “CNN 공장 복구 미션 / 양피지에서 원리를 배우고, 세 실습실의 미션을 완료하세요!”를 표시한다. [확인]을 눌러야 IsComplete가 설정된다. SceneLoadController는 성공 완료 여부를 TutorialCompleted로 제공한다. 완료된 경우에만 튜토리얼을 닫고 공장 안내판·첫 경로·실습 진입을 활성화한다.

튜토리얼 제목/본문/버튼 크기는 48/40/42, 버튼 영역은 320×96이다. 진행 번호를 제목에서 분리하고 글은 짧게 유지한다. 확대 Touch Plus 모델은 해당 버튼·조이스틱을 노란색으로 강조한다. 태블릿 그림은 연습 도식이며 실제 태블릿도 기존 입력을 유지한다. 실제 동작 성공을 페이지 진행 조건으로 검사하지는 않는다. 다음 체험의 양피지 조건과 완료 조건은 기존대로 유지한다.

## 엔딩

FactoryEndingManager는 기존 23초 복구 연출 → 축하 메시지 → 처음으로/앱 종료 흐름을 유지한다. 공유 VillageOpening.prefab을 복제하지 않고 참조한다. 0~3초 손상 상태, 3~8초 코어·시설 복구, 8~13초 화면 복구, 13~19초 주민 축하, 19~23초 평화로운 일상 순서다. FactoryRestorationController의 복구 조명은 오프닝과 같은 중립적인 낮 팔레트로 복귀한다.

## 이번 변경의 확인 범위

2026-10-30: 코드·씬/프리팹 직렬화 참조, 새 음원 참조, 단계 전환 및 입력 해제 경로를 정적으로 검토했다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다. 실제 Quest에서의 가독성·문 통과·진동·음량·재질 외관은 실행 확인 전이다. 이전 실행 확인 결과를 이번 변경 결과로 간주하지 않는다.

## 이전 정리 기록

다음은 과거 에셋·코드 정리 당시의 기록이며 현재 동작과 배치는 위 설명을 기준으로 한다.

## Kdy 참고 파일 정리

2026-10-24: 현재 실행 흐름과 별개인 `Scenes/kdy_scene1~17.unity`, `Prefabs/Common/KdyLessonTablet.prefab`·`KdyFactoryHall.prefab`·`KdyCoreFault.prefab`, `Materials/Common/KdyLessonFilter.mat`·`KdyLessonPhoto.mat`·`KdyBlack.mat`·`KdyCyan.mat`·`KdyFloor.mat`·`KdySteelEdge.mat`·`KdyLessonPixel.mat`, `Textures/Common/KdyFactoryInput.png`를 해당 `.meta`와 함께 제거했다. 스크립트는 아래 보존 대상 9개를 제외한 Kdy 스크립트 23개를 제거했다. 비게 된 `Scripts/kdy_scene*` 폴더 11개와 `Prefabs/Common` 폴더 및 각 폴더 `.meta`도 제거했다. `Textures/Common`에는 태블릿에서 사용하는 `LearningParchment.png`가 있으므로 유지한다. 전체 삭제 범위는 원본 파일 51개와 `.meta` 63개다.

2026-10-24 정리 당시에는 오프닝에서 사용하는 에셋 6개의 경로·내용·GUID를 유지했다. 프리팹은 `Prefabs/kdy_scene2/PixelWraith.prefab`이며, 재질은 `Materials/Common`의 `KdyAmber.mat`, `KdySteel.mat`, `KdyViolet.mat`, `KdyWraith.mat`와 `Materials/kdy_scene2/KdyAttackRay.mat`다. 이전 공장 배경과 그 전용 재질은 현재 씬 및 보존 코드의 의존성이 없어 제거했다.

2026-10-30: 이전 정리에서 재사용 참고용으로 남긴 스크립트 9개도 현재 실행 흐름에서 사용하지 않아 제거했다. 삭제 대상은 `Scripts/kdy_scene6/KdyConvolutionController.cs`, `Scripts/kdy_scene9/KdyActivationLeverController.cs`, `Scripts/kdy_scene16/KdySignComparisonController.cs`와 `Scripts/Core/`의 `KdyLessonGrabController.cs`, `KdyLessonProgressController.cs`, `KdyNetworkConfiguration.cs`, `KdyFactorySignExampleData.cs`, `KdyTrainingExampleData.cs`, `KdySignExampleData.cs`다. 각 스크립트의 `.meta`와 비게 된 `Scripts/kdy_scene6`, `Scripts/kdy_scene9`, `Scripts/kdy_scene16` 폴더 및 폴더 `.meta`를 함께 제거했다. 이번 삭제 범위는 스크립트 9개와 `.meta` 12개다.

2026-10-30 참고 스크립트 삭제 당시에는 오프닝에서 사용하는 위 에셋 6개와 해당 `.meta`, `Prefabs/kdy_scene2`·`Materials/kdy_scene2` 폴더는 유지한다. `FactoryIntro` 씬과 `PixelWraith` 프리팹의 GUID 참조, `FactoryIntroSceneBuilder`의 에셋 경로도 그대로 유지한다. 이전 Kdy 씬과 참고 스크립트는 현재 프로젝트에 남아 있지 않다.

## 에셋 이름과 주민 코드 배치 정리 (2026-10-30)

현재 악령 프리팹은 `Prefabs/OpeningEnding/PixelWraith.prefab`에 둔다. 이 프리팹의 재질은 `Materials/OpeningEnding`의 `WraithAmber.mat`, `WraithSteel.mat`, `WraithViolet.mat`, `WraithBody.mat`, `WraithAttackRay.mat`이다. 기존 `Kdy` 재질 이름을 역할에 맞게 변경하고 `Common` 및 `kdy_scene2`에서 이동했다. 비게 된 `Prefabs/kdy_scene2`·`Materials/kdy_scene2`와 폴더 `.meta`는 제거했다. 위 과거 정리 기록의 경로는 당시 배치를 뜻한다.

인트로·엔딩 주민 동작 코드는 `Scripts/OpeningEnding/VillageResident/`의 `VillageResidentMotionController.cs`와 `VillageResidentMotionLibrary.cs`로 모았다. 클래스명·동작 구현은 유지하고 네임스페이스는 OpeningEnding으로 통합했다. 이동한 에셋·스크립트의 `.meta`와 GUID를 보존하고, 재질 내부 이름과 `FactoryIntroSceneBuilder`의 에셋 로드 경로를 갱신했다.

## 확인 범위

2026-10-30 오프닝·엔딩 통합: 이동 파일·폴더 165개 중 .meta 87개의 내용을 원본과 대조해 보존을 확인했고, 통합 경로의 GUID 중복이 없음을 확인했다. 이전 namespace와 에디터 에셋 경로 참조를 검색하고 씬·프리팹의 타입명 및 UnityEvent 대상 타입 변경을 검토했다. Unity 실행·Play·테스트·빌드·컴파일은 수행하지 않았다.

2026-10-30 Kdy 후속 정리: fetch 후 로컬 `main`과 최신 `origin/main`의 커밋·파일 차이가 없음을 확인하고 새 작업 브랜치에서 정리했다. 삭제 대상 코드의 타입 참조가 해당 9개 파일 내부에만 있고 씬·프리팹 등에서 스크립트 GUID를 참조하지 않음을 정적으로 확인했다. 삭제 후 유지 파일의 타입·GUID 참조와 변경 목록을 확인했다. 사용 중인 에셋 6개 및 `.meta`, 씬·프리팹·에디터 도구는 변경하지 않았다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다.

2026-10-24 Kdy 정리: 최신 `main`에서 별도 브랜치와 워크트리를 만들고 코드 타입 참조와 Unity GUID 참조를 정적으로 검토했다. 삭제 대상 타입·GUID를 유지 파일에서 참조하지 않는지, 보존한 9개 스크립트의 의존성이 연결되는지, 지정한 에셋과 재사용 후보의 내용 및 보존 파일의 `.meta`가 유지되는지 확인했다. Unity 실행·Play·테스트·빌드·컴파일은 수행하지 않았다.

2026-10-23 후속 변경: Quaternius 무료 Standard 원본의 동봉 CC0 표기와 실제 7개 동작 데이터를 확인해 기존 주민 관절에 연결했다. 관람 공간·벤치·키오스크·주민 경로를 수정하고 시작 UI 즉시 숨김, 같은 마을의 23초 복구 엔딩을 반영했다. 코드·직렬화 참조·정적 배치 그림을 검토했다. 정적 배치 그림의 화면 색은 자리 표시이며 Unity 렌더링 결과가 아니다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다. 동작 전환의 실제 표시, Quest 성능과 입력은 실행 확인 전이다.

2026-10-23: 사용자가 앞선 18초 오프닝의 정상 동작을 직접 확인했다고 알려 주었다. 이후 방송 화면 교체·타이틀의 주민 보행·악령 반응 강화·제출용 출처·저작권 Markdown 문서를 추가했다. 이번 후속 변경은 소스·씬·에셋 참조를 정적으로 검토하며, 앞선 실행 확인을 후속 코드의 실행 결과로 간주하지 않는다. Unity·Play·테스트·빌드·컴파일은 새로 수행하지 않았다.

2026-10-22 마을 오프닝: 기존 `feature/scene-ui-and-guidance`에서 분기한 작업으로, 소스와 에셋·씬 연결을 정적으로 검토한다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성 및 씬 구성 도구 실행은 수행하지 않았다. 18초 화면 구성, 실제 렌더링·효과음 청취, Quest 성능과 입력은 실행 확인 전이다.

2026-10-22: 최신 main에서 작업 브랜치를 만들고 참고 브랜치를 일반 Merge했다. 기존 방 진입, 학습 완료 조건, 이동 속도 3m/s, XR 설정과 태블릿 파일이 유지되는지 정적으로 대조했다. 안내 버튼 문제를 검토하면서 Single 씬 전환으로 UI 모듈이 사라진 뒤 persistent ray가 새 모듈에 등록되지 않는 경로를 소스에서 확인했다. 튜토리얼은 별도 Additive 씬으로 분리하고 공용 입력 코드에 등록 복원을 반영했다. 씬 로컬 참조·프리팹 GUID·변경 내용을 정적으로 검토했으며, 사용자 지시에 따라 Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본은 수행하지 않았다. 실제 장면 표시와 버튼 입력 복구·Quest 컨트롤러 동작은 실행 확인 전이다.
