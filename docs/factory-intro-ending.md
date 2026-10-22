# 공장 오프닝·입구 안내와 엔딩

실제 실행 흐름은 `FactoryIntro → FactoryEnvironment → CNNPixelLab → CNNFeatureMapLab → CNNModelDesignLab → FactoryEnding`이다. 세 학습 씬은 공장을 유지한 채 Additive로 실행한다. 방의 표시 이름은 Pixel Lab, Feature Map Lab, Model Design Lab이며 실제 씬 파일명과 완료 조건은 유지한다.

## 시작 화면과 악령 연출

| 파일·클래스 | 책임 |
| --- | --- |
| `Scripts/FactoryIntro/FactoryIntroManager.cs` | 타이틀 → 이야기 → 공장 로드를 조율한다. 시작하기를 한 번 누르면 악령 연출과 자막을 진행하고 자동으로 공장을 연다. 실패 시 재시도 버튼을 표시한다. |
| `Scripts/FactoryIntro/FactoryIntroPresentationController.cs` | 제목·자막·버튼과 악령/코어 손상 표시를 담당한다. 이야기 중 패널을 아래로 축소해 연출을 가리지 않는다. |
| `Scripts/FactoryIntro/FactoryWraithController.cs` | 기존 악령의 부유·파편 회전·공격선·등장과 퇴장 동작을 담당한다. 시작 화면에서는 악령 인스턴스가 비활성이고 시작하기 이후 활성화한다. |
| `Scripts/FactoryIntro/FactoryIntroInputController.cs` | 이야기 중 이동·잡기를 잠시 보류하고 오른손 Trigger UI를 유지하며, 공장을 열기 전에 입력을 복원한다. 태블릿 컴포넌트나 입력은 변경하지 않는다. |
| `Scripts/Core/SceneTransitionController.cs` | persistent 리그에서 페이드아웃 → Single 로드 → 페이드인을 수행한다. 일반 씬 전환은 페이드인 후 자신을 정리한다. 기존 참고 씬 연결은 유지한다. |

`FactoryIntro.unity`는 기존 공용 XR 리그와 시작 UI를 유지하고, 참고 브랜치의 `KdyFactoryHall`, `KdyCoreFault`, `PixelWraith` 프리팹을 사용한다. 이전 임시 공장 배경과 연습 블록·표식은 씬에서 제거했다. 기존 `kdy_scene1~17`의 이름과 별도 학습 체계는 변경하지 않으며, 일반 실행 흐름에 새로 연결하지 않는다.

## 실제 공장 입구에서 한 번 안내

`FactoryLearningFlowManager.Start()`가 실제 공장 입구에 플레이어를 배치한 뒤 `FactoryTutorialController`와 `FactoryTutorialPanelController`를 연결한다. 이동·선택·잡기·정면 복원·태블릿 설명의 다섯 페이지를 이전/다음/안내 마치기로 넘긴다. 고개 회전, 이동 목표 도착, 잡기 성공, 태블릿 입력 순서 등 수행 판정은 없다. 안내 중에는 ray 버튼을 유지하고 학습 씬 진입만 보류한다. 이동·잡기·태블릿 조작을 페이지별로 제한하지 않는다.

안내를 끝내면 패널을 숨기고 첫 바닥 경로와 고정 표지판을 표시한다. 완료 상태는 공장 Manager 인스턴스에 남으므로 방 재방문·Additive 학습 전환으로 안내가 반복되지 않는다. 엔딩 후 처음부터 새 체험을 시작하면 다시 한 번 표시한다. 별도 저장 키나 전역 상태는 추가하지 않았다.

기존 `FactoryTutorialController.cs`와 `.meta`는 `Scripts/FactoryIntro`에서 `Scripts/FactoryEnvironment`로 옮기고 GUID를 보존했다. 태블릿 코드·프리팹·폰트·저장 형식은 변경하지 않았다.

## 공장 복구와 엔딩

`FactoryEndingManager`는 기존대로 정상 학습 완료 뒤 공장 출구를 통해 진입하며, 복구 → 개념 복습 → 체험 완료 → 처음으로/앱 종료를 조율한다. 참고 브랜치 전용의 간판 검사나 가상 학습 조건에 의존하지 않는다.

`FactoryRestorationController`는 참고 엔딩의 공장 세트, 여덟 개 점등 표면, 세 창문 풍경, 악령을 사용한다. 재질 밝기 증가·순차 점등·풍경 선명화·악령 축소를 실행하고 생성한 창문 재질·텍스처는 파괴 시 정리한다. 복구 중에는 UI Canvas를 숨겨 장면을 보여 주고, 종료되면 `FactoryEndingPanelController`가 기존 여섯 개념의 복습 카드와 공통 버튼 피드백을 표시한다.

## 공통 UI와 씬 구성 도구

공통 표시 값은 `Scripts/Core/SceneUIStyle.cs`, uGUI hover/누름 외곽선은 `SceneButtonFeedbackController.cs`가 담당한다. 버튼은 어두운 공장 패널·청록 강조·흰 글자로 맞췄다. XRI 3D 버튼은 각 씬 Controller에서 같은 상태 색상을 사용한다. 계산과 학습 데이터의 의미 색상은 유지한다. 태블릿에는 이 스타일을 적용하지 않는다.

`FactoryIntroSceneBuilder`와 `FactoryEndingSceneBuilder`도 새 프리팹과 현재 클래스 참조에 맞췄다. 기존 씬을 덮어쓰거나 열 때 자동 재생성하지 않는다. 수정한 두 연출 스크립트와 악령 프리팹만 파일 이름의 이니셜을 제거했고, GUID는 보존했다. 기타 참고 씬·에셋을 일괄 개명하지 않았다.

## 확인 범위

2026-10-22: 최신 main에서 작업 브랜치를 만들고 참고 브랜치를 일반 Merge했다. 기존 방 진입, 학습 완료 조건, 이동 속도 3m/s, XR 설정, 씬 등록과 태블릿 파일이 유지되는지 정적으로 대조했다. 씬 로컬 참조·프리팹 GUID·변경 내용을 정적으로 검토했다. 사용자 지시에 따라 Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본은 수행하지 않았다. 실제 장면 표시와 Quest 컨트롤러 동작은 실행 확인 전이다.
