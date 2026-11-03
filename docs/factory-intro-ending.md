# 공장 오프닝·입구 안내와 엔딩

실행 흐름은 FactoryIntro → FactoryEnvironment → FactoryEnding이다. 공장 내부는 정문·튜토리얼실·세 체험실·후문으로 구성하며, 튜토리얼과 학습 콘텐츠는 Additive로 로드한다. 모델 제작을 완료하고 후문에 진입하면 FactoryEnding을 Single로 실행한다. 공장 동선·셔터·학습 연결은 [공장 환경](factory-environment.md)을 따른다.

## 오프닝·엔딩 폴더와 책임

오프닝과 엔딩은 에셋 관리상 OpeningEnding으로 묶으며 실제 씬 파일은 분리한다. 이동한 .meta와 GUID는 유지한다.

| 위치 | 내용 / namespace |
| --- | --- |
| Scripts/OpeningEnding/ | 공통 마을·디스플레이·입력 제어 / OpeningEnding |
| Scripts/OpeningEnding/VillageResident/ | 주민 동작 Controller·MotionLibrary / OpeningEnding |
| Scripts/OpeningEnding/Intro/ | 오프닝 Manager·Presentation·Cinematic·Entry·악령 Controller / OpeningEnding.Intro |
| Scripts/OpeningEnding/Ending/ | 엔딩 Manager·Panel·Restoration과 CNN 과정 애니메이션·타일 메시·연산 표본 / OpeningEnding.Ending |
| Prefabs/OpeningEnding/Ending/ | 엔딩 CNN 과정 UI 프리팹 |
| Prefabs/OpeningEnding/ | 공유 VillageOpening, PixelWraith, 오프닝 전용 FactoryEntrance |
| Materials/OpeningEnding/ | 마을·화면·악령 및 입장 경로 재질. 엔딩 재질은 Ending/ 하위 |
| Meshes/OpeningEnding/ | 마을 메시와 QuaterniusVillageMotion.asset |
| Shaders/OpeningEnding/, Audio/OpeningEnding/ | 방송·하늘 셰이더와 오프닝 음원. 엔딩 타일 셰이더는 Shaders/OpeningEnding/Ending/ 하위 |
| Editor/ | 씬 구성 도구. 새 에셋 경로·입력·표시·입장·음원 참조 연결 |

FactoryIntroManager는 진행 단계와 Controller 호출 순서를 관리한다. PresentationController는 안내창과 버튼을, CinematicController는 기존 18초 연출과 효과음·진동을, EntryController는 문·경로·왼손 표시·입장 판정을 담당한다. 공통 FactoryIntroInputController는 기존 이름을 유지하며 오프닝·엔딩의 입력 잠금과 복원을 담당한다. A 입력과 UI 버튼은 로컬 OnContinueRequested 이벤트로 Manager에 요청하며 활성 기간에 대칭 구독·해제한다.

## 오프닝 진행

| 단계 | 안내 / 조작 |
| --- | --- |
| 첫 화면 | 공장을 확대해 중앙을 채우고 디스플레이 좌상·좌하·우측, 공격 중인 악령을 우상단에 둔 정지 구도. 아래에 “CNN 체험으로 배우는 / AI 업스케일링” 두 줄 제목과 둥근 [GAME START] 버튼. 오른손 레이와 Trigger로 선택하며 A로 시작하지 않는다. 이동·잡기는 보류한다. |
| 습격 | GAME START 선택 즉시 원래 마을 배치로 전환하고 기존 18초 연출을 0초부터 시작한다. 4.3~12초에 빨간 “CNN 공장에 이상 발생!” 경고. 선택 버튼은 없다. |
| 복구 요청 | “마을에 선명함을 되찾아 주세요” / “공장 안의 미션을 완료해 멈춰 버린 CNN 공장을 복구해 주세요!” [공장 수리하기] |
| 문 열기 | 안내창을 숨기고 무거운 기계 문소리에 맞춰 3.2초 동안 양쪽 문을 연다. |
| 직접 입장 | “왼쪽 조이스틱을 앞으로 밀어 빛나는 길을 따라가세요.” 왼손 컨트롤러·조이스틱과 바닥 경로를 강조한다. 입구 판정선에 도착하면 페이드로 공장 내부를 로드한다. |

첫 화면은 GAME START 버튼으로 진행하고, 복구 요청·재시도 화면은 UI 선택 또는 오른손 A로 진행한다. 같은 프레임의 A와 UI 클릭은 한 단계만 진행한다. A가 안내 진행에 쓰이는 동안 기존 정면 복원 동작은 보류된다. 연출 중에는 진행 입력을 받지 않는다. 씬 로드 실패 시 [다시 시도]를 제공한다.

CNN 설명은 디스플레이 장치의 물리적 해상도가 아닌 입력 이미지의 해상도 개선을 뜻한다. [SRCNN 원 논문](https://arxiv.org/abs/1501.00092)에 근거한 게임 설정이며 원본 세부 정보의 완전한 복원을 보장한다는 표현은 사용하지 않는다.

## 연출·입장 공간

마을 배치, 주민 외형·동작, 세 디스플레이와 방송 콘텐츠, 공장·악령의 기존 큰 형태를 유지한다. 주민은 Quaternius 동작 데이터와 기존 관절을 사용한다. TV의 뉴스·축구·SF 방송은 프로젝트 셰이더 그래픽이며 외부 방송 영상을 사용하지 않는다.

밝고 평화로운 시작에서 습격과 함께 어두워지는 대비를 유지한다. 외벽은 밝은 회백색·따뜻한 회색, 바닥은 콘크리트·포장 톤, 금속은 짙은 청회색, 지붕은 차분한 벽돌색으로 정리했다. 잔디·나무·주민 피부와 옷·방송·악령 색은 유지한다. 낮은 중립적인 따뜻한 햇빛, 습격은 어두운 청회색 조명이며 엔딩 복구 때 같은 낮 팔레트로 돌아온다. 실사 텍스처나 외부 모델을 추가하지 않았다.

| 연출 시간 | 사건 |
| --- | --- |
| 0~4초 | 시작 화면의 배치를 원래 마을 배치로 복원하고 밝은 마을·공장·방송을 0초부터 재생한다. |
| 4.3초 | 악령 등장과 붉은 경고창, 경고 비프·긴장음·잡음, 짧은 양손 진동 |
| 7.2 / 8.2초 | 공격 충전 / 코어 타격에 맞춘 진동. 기존 파편·충격파·연기를 유지한다. |
| 10.5초 이후 | 디스플레이 고장·화질 손실, 짧은 진동. 화면별 고장 시간은 기존 10.5/10.85/11.2초다. |
| 12~18초 | 경고창이 사라지고 손상된 마을을 보여준 뒤 복구 요청을 표시한다. |

FactoryEntrance.prefab은 기존 입구 표시 위치(z=16.5 부근)에 미닫이문·충돌체와 바닥 경로를 추가한다. 문 중심 z=16.45, 입장 판정선 z=16.9다. 플레이어 카메라가 문 너머 영역에 들어갔는지 EntryController가 판정한다. 바닥 경로·손 강조 오브젝트는 프리팹에 저장하며 입장 단계에만 표시한다. 공장 씬으로 넘어갈 때 경로와 컨트롤러 강조, 오프닝 입력 상태를 정리한다. 카메라를 연출 경로로 강제 이동하거나 흔들지 않는다.

기존 VillageOpening.wav에 자체 합성 WraithAttackWarning.wav와 FactoryDoorOpen.wav를 추가했다. 외부 샘플 없이 합성한 제작 방법과 출처는 [ASSET_CREDITS.md](ASSET_CREDITS.md)에 기록한다. 진동은 연결된 기기의 지원 여부를 확인하며 짧은 임펄스로만 재생하고 비활성화 시 정리한다.

## 시작 화면 구성

VillageOpening 프리팹에는 공장 상단의 기존 픽셀 글자 앞에 불투명 간판과 `CNN FACTORY` 월드 공간 Text를 배치한다. 주아체 148 크기를 사용하며 UI Raycast를 받지 않는다. Unity Play에서 간판 글자 전체가 악령에 가리지 않는 배치를 확인하고 캡처했다.

타이틀에서는 Cipher를 볼륨 0.3으로 반복 재생한다. GAME START 선택 후 기존 오프닝 음향을 0에서 0.7까지 올리면서 Cipher를 1.5초에 걸쳐 줄인 뒤 정지한다. 공용 XRPlayerRig의 `Core.AudioManager`가 전환 코루틴과 음악용 소스 두 개·효과음 소스 한 개를 소유한다. IntroManager가 리그의 AudioManager를 CinematicController에 전달하며, CinematicController는 클립·재생 시점·목표 볼륨만 요청한다. 태블릿의 SettingsController는 TabletManager가 전달한 음량 변경 콜백을 호출한다. AudioManager는 Singleton이나 별도 DontDestroyOnLoad 오브젝트를 만들지 않고 기존 리그 수명을 따른다. 공용 AudioManager 전환 이전 Play에서 Cipher 재생, 전환 시작 시 두 소스의 재생 상태, 전환 후 Cipher 정지와 오프닝 볼륨 0.7을 확인했다. 최종 체감 음량과 실제 청취 품질은 Quest 확인 대상이다.

공장 중심의 클로즈업을 위해 제목 화면에서 마을의 위치·크기를 변경하고 시작 시 원래 값으로 복원한다. 악령의 공격 광선, 두 겹의 일그러진 아우라, 공장 코어의 충격 고리와 파편은 한 시점의 고정 자세로 표시한다. 제목에 주아체를 사용하고 첫 줄 66, 둘째 줄 104, GAME START 90 크기로 표시한다. 버튼에는 Unity 내장 둥근 UI 스프라이트를 사용한다. 주아체의 원본 배포 라이선스와 출처는 `ASSET_CREDITS.md`에 기록한다.

사용자가 제공한 콘티의 공간 배치를 기존 프로젝트의 공장·방송 디스플레이·악령 오브젝트로 표현한다. 새 이미지나 외부 에셋을 생성하지 않는다. CinematicController가 디스플레이 부모 프레임의 원래 위치·회전·크기를 보관하고 제목 화면에서 세 프레임을 좌상·좌하·우측으로 배치한다. 악령은 우상단의 고정 자세로 표시하며 대기 중 마을·방송·악령 시간은 진행하지 않는다. VR 머리 추적은 유지한다. PresentationController는 기존 버튼을 GAME START로 표시하고 제목 화면의 불투명 배경과 A 힌트를 숨긴다. 버튼 선택 후 원래 프레임 배치를 복원하고 악령을 숨긴 뒤 기존 습격 연출을 시작한다. 문 열기·입장 판정·페이드 씬 로딩은 이번 시작 화면 변경에 포함하지 않는다.

## 안내창 가독성

오프닝은 짧은 제목과 1~2줄 본문, 큰 단일 진행 버튼을 사용한다. 기본 제목 44, 본문 40, 버튼 46이며 버튼 영역은 600×100이다. A 안내는 별도 작은 캡션으로 분리한다. 첫 화면·경고·이동 안내는 배경 높이를 줄이고, 복구 요청은 반투명 어두운 배경과 흰 글자를 사용한다. 경고 배경은 초당 한 주기의 완만한 붉은색 변화로 표시한다. 이동 안내는 보행 중 시야 아래쪽에 유지한다. 사용자에게 보이는 입력 명칭은 “조이스틱”으로 통일한다.

## 공장 내부 조작 안내와 미션 시작

FactoryPlayerController가 플레이어를 정문에 배치하고 FactoryEnvironmentManager가 튜토리얼실로 안내한다. 공장은 AreaChanged로 튜토리얼실 진입을 받아 입구 셔터를 닫는다. 로더가 튜토리얼을 Additive 로드하고 콘텐츠를 배치한 뒤 ContentLoaded를 발행하면 LearningSessionManager는 공통 진행률·진행 가능 상태 이벤트를 구독한다. FactorySceneLoadController는 Unity의 sceneLoaded 알림에서 다른 체험과 동일하게 Tutorial 단계의 ContentAnchor로 콘텐츠 루트를 배치한다. FactoryTutorialManager는 Start에서 공용 XR 리그를 확보하고 안내 UI와 입력을 초기화한다. 안내 패널의 루트 기준 로컬 위치 (0, 1.65, 2.5)는 FactoryTutorial 씬에 저장하며, 튜토리얼이 완료될 때까지 다음 실습실로 진행하지 않는다.

FactoryTutorial은 준비 안내 → 버튼 선택 → 이동·방향 전환 → 잡기 → 정면 맞추기 → 태블릿 역할 → 태블릿 항목 이동 → X 선택 → Y 뒤로 → 메뉴 설정 → 준비 완료의 11개 안내 화면으로 구성한다. 조작 안내는 [이전]·[다음], 마지막은 [미션 시작]이다. 시작 안내는 A로도 진행한다. 건너뛰기 버튼은 없다.

[미션 시작] 뒤 확대 컨트롤러·태블릿 연습 그림을 숨기고 “CNN 공장 복구 미션 / 양피지에서 원리를 배우고, 세 실습실의 미션을 완료하세요!”를 표시한다. [확인]을 눌러야 IsComplete가 설정된다. FactoryTutorialManager는 ILearningExperience를 구현하며 입력 복원·안내 숨김 후 ProgressChanged(1)과 ExitAvailabilityChanged(true)를 순서대로 알린다. 세션은 공통 진행률 경로로 완료를 기록하고, 로더는 튜토리얼실에서 ROOM 1로 연결되는 통로 진입 시 첫 학습 콘텐츠를 준비한다. 튜토리얼 완료 후 출구를 열고 통로에서 다음 콘텐츠를 준비한 뒤 ROOM #1로 가는 경로를 표시하며, 튜토리얼이 이전·현재·다음 유지 범위를 벗어나면 EndSession으로 정리한 뒤 언로드한다. 튜토리얼도 전체 진행률에 동일 비중으로 포함된다. ROOM #1 입구 셔터는 해당 위치의 양피지 수집 조건에 따라 열린다.

튜토리얼 제목/본문/버튼 크기는 48/40/42, 버튼 영역은 320×96이다. 진행 번호를 제목에서 분리하고 글은 짧게 유지한다. 확대 Touch Plus 모델은 해당 버튼·조이스틱을 노란색으로 강조한다. 태블릿 그림은 연습 도식이며 실제 태블릿도 기존 입력을 유지한다. 실제 동작 성공을 페이지 진행 조건으로 검사하지는 않는다. 방별 양피지 수집 조건과 체험 완료 조건은 [공장 환경](factory-environment.md)을 따른다.

## 엔딩

공장 후문에서 FactoryEnding으로 전환하면 **공장 복구 → CNN 업스케일링 → 마을 전체 화질 개선 → 주민 감사 인사 → 마을 감상** 순서로 진행한다. 복구된 시설과 영상 화질을 구분한다. 공장이 다시 움직이고 밝아져도 모든 디스플레이는 저해상도 영상을 유지하며, 사용자가 [AI 업스케일링 시작]을 선택한 뒤에만 화질 개선을 진행한다.

| 단계 | 길이 | 연출 |
| --- | --- | --- |
| 공장 복구 | 12초 | 코어·조명·시설 복구, 낮 조명으로 복귀. 영상은 저화질이며 주민은 화면을 관찰 |
| 디스플레이 집중 | 2.5초 | 기존 뉴스 디스플레이를 정면으로 부드럽게 이동. 카메라는 이동하지 않음 |
| 시작 대기 | 사용자 선택까지 | “CNN 공장, 다시 가동!”와 큰 [AI 업스케일링 시작] 버튼 |
| 이미지 펼치기 | 4초 | 동일 뉴스 장면의 16×9 픽셀 타일이 화면에서 앞으로 떨어져 나와 왼쪽 입력 평면에 정렬 |
| 합성곱 | 12초 | 두께 있는 타일 위 3×3 필터, 첫 위치의 곱·합 계산식 한 번, 9개 정보가 결과 셀로 모이는 이동. 이후 필터가 이동하며 14×7 특징 맵을 생성 |
| 특징 맵 | 8초 | 서로 다른 필터의 특징 맵 3개가 깊이·회전을 가진 층으로 펼쳐짐 |
| 고해상도 예측 | 8초 | 특징을 전달하는 흐름을 따라 더 촘촘한 32×18 영상 타일이 생성되고 중앙에 모임 |
| 화면에 적용 | 5초 | 결과 타일이 왼쪽 열부터 원래 디스플레이로 돌아가며 같은 위치의 고화질 비교선과 동기화. 도착한 타일을 숨겨 완성 화면이 다시 저화질로 바뀌는 순간을 방지 |
| 결과 감상 | 2초 | 설명창 없이 선명해진 화면 유지 |
| 마을 적용·축하 | 10초 | 대상 화면은 고화질 상태로 3초에 걸쳐 원래 자리로 돌아감. 다른 두 화면은 순서대로 선명해지고 주민들이 축하. 공장 복구를 반복하지 않음 |
| 주민 접근 | 3초 | 기존 주민 한 명이 실제 마지막 위치에서 사용자 앞 왼쪽으로 걸어와 대화 |
| 감사 인사 | 사용자 선택까지 | 2페이지: “당신 덕분에 마을의 디스플레이가 / 다시 선명해졌어요!” → “CNN 공장을 수리해 주셔서 / 정말 감사합니다!”; [다음] → [마을 바라보기] |
| 주민 퇴장·마을 감상 | 2.5초 + 6초 | 주민이 옆으로 비켜난 뒤 UI 없이 고화질 디스플레이와 마을을 감상 |
| 완료 | 사용자 선택까지 | 아래쪽에 “선명함을 되찾은 마을”, [처음으로]·[종료] 표시 |

### 시각 구성과 입력

이동·잡기는 기존 공통 입력 제어로 잠그고 머리 추적은 유지한다. 시작·대화·완료 단계에서만 오른손 레이와 Trigger로 버튼을 선택한다. 처리 중에는 큰 설명 패널과 버튼을 숨기고 `합성곱`, `특징 맵`, `고해상도 예측` 단계 제목과 첫 계산식만 표시한다. 마을 감상에서도 카메라의 위치·방향을 강제로 변경하지 않는다.

시작·완료 버튼은 70 크기, 대화 버튼은 60 크기 글자와 둥근 배경을 사용한다. 초대 제목은 60, 주민 대사는 44다. 기존 Noto 폰트의 행 높이를 고려한 텍스트 영역을 배치한다. 주민은 (-1.1, 0, 3.2)로 접근하고 대화창은 오른쪽 (0.7, 1.25, 2.9)에 표시한다. 감사 인사 뒤 주민은 (-2.6, 0, 4.2)로 이동한다. 대사 페이지를 넘겨도 주민 동작 시간을 초기화하지 않는다.

### 연산·영상의 범위

픽셀 타일의 앞면은 기존 `VillageDisplay` 셰이더의 뉴스 채널·재생 3초·위상 0을 재사용한다. 엔딩 비교의 저해상도 격자는 16×9이며, 오프닝 손상 연출의 18×10 격자는 유지한다. 기존 `OpeningBroadcast.png`의 셀 중심에서 얻은 16×9 밝기 표본과 예시 필터로 곱·합 및 3개 특징 맵을 계산한다. 원본 PNG를 수정·복제하거나 런타임 GPU 읽기를 수행하지 않는다.

고해상도 타일 생성·반환과 셰이더 비교는 원리를 설명하는 연출이다. 모델 제작 체험의 학습된 가중치나 서버 추론 결과를 연결하지 않으며 원본 정보의 완전한 복원을 보장하지 않는다. 과정에는 작은 `원리 시각화` 표시를 둔다. 새로운 이미지·영상·음원 파일은 생성하지 않는다.

### 엔딩 책임과 연결

- `FactoryEndingManager`: 공장 복구부터 대화·감상까지 단계와 시간, 사용자 요청 구독·해제, 입력 잠금, 재시작·종료 순서.
- `FactoryEndingPanelController`: 초대·주민 대화·완료 UI의 문구·배치와 버튼 요청 이벤트, 내부 과정 애니메이션 호출. 과정 중에는 설명 패널을 숨김.
- `EndingUpscalingAnimationController` (`Scripts/OpeningEnding/Ending/`): 픽셀 펼치기, 필터 이동, 정보 전달, 특징 맵의 펼침, 결과 영상 조립·반환 애니메이션.
- `EndingPixelTileMesh`: 재사용하는 결합 메시의 면·모서리·두께·UV·색상 갱신. 타일마다 GameObject를 만들지 않으며 할당한 메시를 종료 시 정리.
- `EndingUpscalingSample`: 기존 방송 이미지에서 추출한 밝기 표본과 예시 합성곱 계산.
- `EndingUpscalingProcess.prefab`: 단계 제목·계산식·원리 표시와 두 재질 참조를 미리 저장. 예전 작은 UI 격자는 제거.
- `FactoryRestorationController`: 공장 복구와 업스케일링 대기 환경, 대상 디스플레이 집중·복귀, 고화질 전환과 축하, 마을의 주민 대화 요청. 연속 환경 시간을 보존해 단계마다 시설·구름을 초기화하지 않음.
- `FactoryVillageController`: 기존 주민의 접근·대화·퇴장과 마을 내부 동작. 대화하는 주민은 일반 보행 루프에서 제외하고, 화면 감상 중에도 옆 위치를 유지.
- `FactoryVillageDisplayController` / `VillageDisplay.shader`: 같은 프레임을 사용한 저해상도·고해상도 비교와 이후 방송 재생. 새 비교 속성의 기본값 0은 오프닝 동작을 유지.
- `FactoryEndingSceneBuilder` / `EndingUpscalingVisualsBuilder`: 초대·대화·완료 버튼, 설명창, 과정 프리팹 참조 연결. 현재 `FactoryEnding.unity`에 저장되어 있어 구성 도구를 다시 실행할 필요가 없음.

2026-10-31 후속 수정: 코드·씬·프리팹·재질 참조, 단계와 대화 동작의 시간 연속성, 폰트의 행 높이·주요 문구 폭과 정적 배치를 검토했다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 별도로 수행하지 않았다. 이전 사용자 요청으로 실행한 에디터의 결과를 이번 수정의 검증 결과로 간주하지 않는다. 실제 Quest의 시야·입력·프레임 시간과 연출 품질은 실행 확인 전이다.

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

2026-10-30 시작 화면: Unity 6000.3.24f1에서 컴파일 오류 없이 Play로 확인했다. 공장·디스플레이 3개·우상단 악령·GAME START 배치를 캡처하고, 버튼의 실제 onClick 이벤트를 호출해 18초 연출 후 미션 안내로 넘어가는 것을 확인했다. Quest 실기기 Trigger 입력은 이번 확인에 포함하지 않았다. 캡처용 눈높이 1.65m와 손을 내린 자세는 Play 중에만 적용했으며 태블릿·컨트롤러 파일은 변경하지 않았다. 연결 도구의 일시적인 eval 시간 초과가 있었으나 연출 실행 중 추가 오류는 없었다. APK 빌드·설치는 수행하지 않았다.

2026-10-30 오프닝·엔딩 통합: 이동 파일·폴더 165개 중 .meta 87개의 내용을 원본과 대조해 보존을 확인했고, 통합 경로의 GUID 중복이 없음을 확인했다. 이전 namespace와 에디터 에셋 경로 참조를 검색하고 씬·프리팹의 타입명 및 UnityEvent 대상 타입 변경을 검토했다. Unity 실행·Play·테스트·빌드·컴파일은 수행하지 않았다.

2026-10-30 Kdy 후속 정리: fetch 후 로컬 `main`과 최신 `origin/main`의 커밋·파일 차이가 없음을 확인하고 새 작업 브랜치에서 정리했다. 삭제 대상 코드의 타입 참조가 해당 9개 파일 내부에만 있고 씬·프리팹 등에서 스크립트 GUID를 참조하지 않음을 정적으로 확인했다. 삭제 후 유지 파일의 타입·GUID 참조와 변경 목록을 확인했다. 사용 중인 에셋 6개 및 `.meta`, 씬·프리팹·에디터 도구는 변경하지 않았다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다.

2026-10-24 Kdy 정리: 최신 `main`에서 별도 브랜치와 워크트리를 만들고 코드 타입 참조와 Unity GUID 참조를 정적으로 검토했다. 삭제 대상 타입·GUID를 유지 파일에서 참조하지 않는지, 보존한 9개 스크립트의 의존성이 연결되는지, 지정한 에셋과 재사용 후보의 내용 및 보존 파일의 `.meta`가 유지되는지 확인했다. Unity 실행·Play·테스트·빌드·컴파일은 수행하지 않았다.

2026-10-23 후속 변경: Quaternius 무료 Standard 원본의 동봉 CC0 표기와 실제 7개 동작 데이터를 확인해 기존 주민 관절에 연결했다. 관람 공간·벤치·키오스크·주민 경로를 수정하고 시작 UI 즉시 숨김, 같은 마을의 23초 복구 엔딩을 반영했다. 코드·직렬화 참조·정적 배치 그림을 검토했다. 정적 배치 그림의 화면 색은 자리 표시이며 Unity 렌더링 결과가 아니다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성은 수행하지 않았다. 동작 전환의 실제 표시, Quest 성능과 입력은 실행 확인 전이다.

2026-10-23: 사용자가 앞선 18초 오프닝의 정상 동작을 직접 확인했다고 알려 주었다. 이후 방송 화면 교체·타이틀의 주민 보행·악령 반응 강화·제출용 출처·저작권 Markdown 문서를 추가했다. 이번 후속 변경은 소스·씬·에셋 참조를 정적으로 검토하며, 앞선 실행 확인을 후속 코드의 실행 결과로 간주하지 않는다. Unity·Play·테스트·빌드·컴파일은 새로 수행하지 않았다.

2026-10-22 마을 오프닝: 기존 `feature/scene-ui-and-guidance`에서 분기한 작업으로, 소스와 에셋·씬 연결을 정적으로 검토한다. Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본 생성 및 씬 구성 도구 실행은 수행하지 않았다. 18초 화면 구성, 실제 렌더링·효과음 청취, Quest 성능과 입력은 실행 확인 전이다.

2026-10-22: 최신 main에서 작업 브랜치를 만들고 참고 브랜치를 일반 Merge했다. 기존 방 진입, 학습 완료 조건, 이동 속도 3m/s, XR 설정과 태블릿 파일이 유지되는지 정적으로 대조했다. 안내 버튼 문제를 검토하면서 Single 씬 전환으로 UI 모듈이 사라진 뒤 persistent ray가 새 모듈에 등록되지 않는 경로를 소스에서 확인했다. 튜토리얼은 별도 Additive 씬으로 분리하고 공용 입력 코드에 등록 복원을 반영했다. 씬 로컬 참조·프리팹 GUID·변경 내용을 정적으로 검토했으며, 사용자 지시에 따라 Unity 실행·Play·테스트·빌드·컴파일·프로젝트 복사본은 수행하지 않았다. 실제 장면 표시와 버튼 입력 복구·Quest 컨트롤러 동작은 실행 확인 전이다.

2026-10-30 공용 오디오 전환: 코드와 씬·프리팹의 AudioSource 사용처를 검색해 실제 재생을 Core/AudioManager.cs로 모았다. 기존 오프닝 씬의 직접 AudioSource는 제거했다. 사용자 허가 후 Unity 6000.3.24f1에서 컴파일·Play 확인을 완료했다. 간판은 공장 재질과 맞춘 청록 바탕·크림 글자로 변경했다.


### 공용 AudioManager 검증 (2026-10-30)

- 컴파일 오류 없음. XRPlayerRig에 AudioManager 1개와 음악용 AudioSource 2개·효과음용 1개가 생성되는 것을 확인했다.
- Cipher: loop=true, volume=0.3, 재생 시간이 진행되는 것을 확인했다.
- 실제 GAME START 버튼의 onClick을 실행했다. 0.40초에 오프닝/Cipher 볼륨 0.125/0.247, 0.81초에 0.389/0.133, 1.21초에 0.633/0.029, 1.60초에 0.7/0으로 전환됐으며 Cipher는 정지하고 오프닝은 재생 중이었다.
- 태블릿의 실제 Volume Row 슬라이더를 0과 0.75로 변경해 AudioListener.volume이 동일하게 바뀌는 것을 확인했다. 저장하지 않고 기존 값 0.49로 복원했다.
- 문 효과음 요청이 AudioManager의 효과음 소스를 재생하는 것을 확인했다. 연출 후 미션 안내 표시도 확인했다.
- Play 종료 중 파괴된 AudioSource에 접근하는 MissingReferenceException을 발견했다. CinematicController는 리그의 AudioManager가 살아 있고 활성 상태일 때만 정지 요청하도록 수정했으며, 재컴파일 후 정상 Play 종료에서 재발하지 않았다.
- 연결 도구의 eval 시간 초과가 별도로 발생했다. 에디터 포커스를 복구한 뒤 검증했으며 이를 게임 실행 오류와 구분했다. 실제 청취 평가·Quest 실기기 입력·APK 빌드는 이번 검증에 포함하지 않았다.
- 청록 바탕·크림 글자의 CNN FACTORY 간판은 Game 뷰 캡처로 확인했다. 눈높이와 손 내림은 에디터 Play 중 캡처용 조정이며 리그 에셋의 추적 자세는 수정하지 않았다.

AudioManager의 공개 요청은 `PlayMusic(clip, loop, volume, fadeSeconds)`, `PlayEffect(clip, volume)`, `StopMusic()`, `StopEffects()`, `SetMasterVolume(volume)`이다. 음악·효과음 선택과 재생 시점은 각 씬이 소유하고, AudioManager는 씬 단계나 UI를 참조하지 않는다. 태블릿 설정 저장 형식과 키는 유지한다.
