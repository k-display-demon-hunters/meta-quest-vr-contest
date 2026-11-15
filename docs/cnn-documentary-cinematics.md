# CNN 다큐멘터리 오프닝·엔딩

## 실행 경로

- `Assets/Scenes/CNNArchiveOpening.unity`: 시작 씬. 시작 버튼을 누르면 재생하고 35초 후 기존 `FactoryEnvironment`로 진입한다. 기존 공장·튜토리얼·학습 순서는 유지한다.
- `Assets/Scenes/CNNArchiveEnding.unity`: 종료 씬. `LearningStageCatalog`의 Exit에서 공용 SceneTransitionController로 로드한다. 게이트 진입으로 기존 공간을 가리고 씬 활성화 후 도착 연출을 실행한다. 도착 연출이 끝나면 약 30초 본편을 시작하며, 재생 완료 또는 건너뛰기 후 완료 화면을 표시한다.
- Build Settings의 첫 씬을 `CNNArchiveOpening`으로 지정했다. 직접 열어 Play해도 기존 XRPlayerRig가 생성된다.

## 연출과 입력

오프닝은 시작 버튼 → 질문 → 되감기 소리에 맞춘 2026~1989 연도 카운트다운 → 사용자를 둘러싼 3D 우체국 실내 → 창구의 봉투 들어 올리기 → 2.8초 3D 도장 동작 → 3D 봉투 우편번호에서 숫자 2 확대 → 16×16 격자의 빛 조각 → 공장 문 열림 순이다. 연도 카운트다운은 1989에 가까워질수록 느려지며 낮은 펄스가 박자를 따라간다. 우체국 구간은 고정된 실내 공간에서 고개를 돌려 창구·우편 분류 선반·창문·작성대·소포를 둘러보는 방식이다. 기존 사진 확대와 창구 줌인은 사용하지 않는다. 봉투·우표·우편번호·도장·소인은 기본 도형으로 구성한 입체 오브젝트다. 도장은 접촉 시점에 소리를 내고 소인을 남기며, 봉투가 관찰자 앞에서 회전·확대되어 첫 숫자 2를 보여준다. 이어서 흰 숫자 2는 위에서부터 민트색 픽셀로 전환된다. 장소와 얀 르쿤 연구팀 표기는 화면에만 표시한다. 오른손 XR 레이로 시작 버튼을 가리키고 트리거를 눌러 재생한다.

엔딩은 모델 카드로 시작한 뒤 1989년 손글씨 우편번호 연구, 1998년 은행 수표 금액 인식, 2016년 사진 속 얼굴 감지, 2019년 카메라 글자 인식과 번역, 2026년 업스케일링을 중앙 사진판으로 차례로 보여준다. 다섯 컷은 같은 16:9 프레임에 맞춰 채우고, 연도는 프레임 왼쪽 위 바깥, 자막은 아래 가운데에 고정한다. 한 세트의 초록 꺾쇠가 첫 모델에서 조여 든 뒤 사진의 인식 대상 사이를 곡선으로 이동한다. 야구 비교선이 지나갈 때는 꺾쇠를 숨기며, 마지막 대사가 시작될 때 꺾쇠를 거둔다. 연도의 바뀌는 자리만 굴러가고, 사진은 프레임 안에서 최대 4% 확대하며 다음 사진과 0.4초 겹쳐 전환한다. 첫 모델 자막은 학습 기록 유무에 관계없이 같은 문장을 사용한다. 2026년 컷은 야구 중계풍 이미지 위로 비교선을 움직여 저해상도 샘플에서 선명한 샘플로 바뀌는 모습을 보여준다. 첫 모델, 다섯 사진 컷과 마지막 대사에는 사용자 화면 녹화에서 추출한 내레이션이 연결돼 있다. 배경음악은 기존 곡의 중간 부분을 겹쳐 이어 재생하고 마지막 대사 구간의 끝 2.2초 동안 줄인다. 지나간 사진은 각 전환에서 1.2초 동안 뒤로 0.6 m, 위로 0.15 m 물러나며 오른쪽으로 0.35 m 비켜가고, 새로 물러나는 사진은 불투명도 35%에 도달한다. 마지막 대사에서 선명해진 야구 사진을 중앙에 두고 1989·1998 사진을 왼쪽에, 2016·2019 사진을 오른쪽에 배치한다. 다섯 사진과 연도 라벨은 검은 구간 직전 0.8초 동안 흐려진다. 씬에 저장된 바닥 격자와 먼지 오브젝트를 무대 기준으로 유지하면서 1.5초 동안 무음으로 쉰 후 완료 화면을 표시한다. 기록이 있으면 카드 위에 “내가 만든 모델”을 표시하고, 테스트 정확도가 있으면 카드 아래에 정수 백분율로 표시한다. 기록이 비어 있으면 “CNN의 기본 구조” 아래에 INPUT → CONV → MAX POOL → FLATTEN → DENSE → OUTPUT 예시를 표시한다. 장면은 AI 이미지로 재구성했으며, 연도는 해당 시기의 연구나 활용 사례를 가리킨다. 카메라 번역에서 CNN과 연결되는 부분은 번역 전의 글자 인식이다.

카메라·XR Origin의 위치와 회전은 시퀀스가 변경하지 않는다. 시작 때 무대 전체를 현재 관찰자 위치·정면에 맞춘다. 오프닝 시작 대기 화면과 엔딩 완료 화면에는 양손 컨트롤러 모델과 오른손 레이가 보인다. 손목 태블릿은 씬에 들어갈 때 숨기고, 컨트롤러 모델과 레이는 재생 중에 숨긴다. `CinematicMovementController`로 이동·잡기와 레이 표시를 제어하고 씬 종료·비활성화 때 입력과 표시 상태를 복원한다. 오른손 또는 왼손 트리거를 연속 2초 누르면 건너뛴다. 트리거를 누르면 진행 막대가 채워지고 놓으면 초기화된다. 에디터에서는 Space 길게 누르기도 가능하다.

엔딩 완료 화면에는 `CNN 공장 · 체험 완료`와 `처음부터 다시 시작` 버튼이 표시된다. 오른손 레이로 버튼을 선택하거나 트리거를 다시 2초간 누르면 오프닝 시작 화면으로 돌아간다. 트리거를 누른 채 엔딩이 완료되면 컨트롤러 모델을 먼저 표시하고, 트리거를 놓을 때 레이를 켠다.

새 오프닝 씬이 활성화되어 `CinematicSequenceManager.Start`가 실행되면, 이동·입력 설정을 적용하기 전에 공용 `ParchmentManager.ResetForNewSession()`을 호출한다. 열려 있는 양피지와 펼침 대기·이동 차단을 정리하고 수집·열람 완료 기록을 모두 초기화한다. 다음 공장 진입에서는 양피지를 다시 수집하고 읽어야 하며 태블릿 수집 목록도 새 기록을 반영한다. 초기화는 오프닝 진입에만 연결하며 엔딩 진입·일반 방 이동·AR 왕복에서는 기록을 유지한다. 오프닝 로드 실패 시에는 기존 기록을 유지한다. 상세는 [공용 양피지](parchment.md)를 따른다. 이 변경은 코드 경로만 정적으로 확인했으며 Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.

컨트롤러 모델과 태블릿 표시는 공용 `PersistentXRPlayerRig.SetControllerModelsVisible()`, `SetTabletVisible()`로 요청한다. `CinematicMovementController`는 리그의 표시 상태를 저장하고 씬 종료 시 복원한다. 공용 리그는 컨트롤러 모델 표시를 처리하고 태블릿 요청은 `TabletManager.SetAvailable()`로 전달한다. 태블릿 모듈이 자신의 표시와 입력 활성화를 관리하며, 표시를 숨긴 동안에도 양손 위치 추적은 계속 동작한다.

## 책임과 파일

연출 코드는 `Assets/Scripts/OpeningEnding/`에 둔다. `CinematicSequenceManager`가 씬의 시작 대기, 재생, 완료, 전환 및 재시도 상태를 소유하고 연출·조작 모듈의 이벤트를 연결한다. 오프닝이 완료되면 공장으로 전환하고, 엔딩이 완료되면 재시작 요청을 기다린다.

| 경로 | 책임 |
| --- | --- |
| `Presentation/CinematicPresentationManager.cs` | 재생 시간 관리, 오프닝·엔딩 실행 선택, 자막·음성 연결과 재생 정리 |
| `Presentation/Opening/OpeningSequence.cs` | 되감기, 우체국, 도장, 봉투, 숫자, 픽셀, 공장 문의 실행 시점 결정 |
| `Presentation/Ending/EndingSequence.cs` | 몽타주, 인식 프레임과 모델 표시 시점 결정 |
| `Presentation/Opening/OpeningVisualController.cs` | 오프닝 연출 오브젝트의 표시·배치·보간 |
| `Presentation/Ending/EndingVisualController.cs` | 엔딩 시각 표시의 진입점. 무대 정렬과 내부 표시 담당의 구성·요청 연결, 잔상 객체 수명 관리 |
| `Presentation/Ending/EndingModelController.cs` | 모델 기록을 카드 문구·색·배치로 변환하고 모델 등장 연출 |
| `Presentation/Ending/EndingPhotoFraming.cs` | 사진 비율과 인식 영역에 따른 크롭 계산 |
| `Presentation/Ending/EndingRecognitionController.cs` | 인식 꺾쇠의 표시·이동·크기·색 애니메이션 |
| `Presentation/Ending/EndingArchiveTrail.cs` | 저장된 사진판·연도 라벨의 잔상 연출. 전용 런타임 재질만 생성·해제 |
| `Presentation/Ending/EndingBackgroundController.cs` | 기존 시드로 생성한 단일 먼지 메시의 정점 갱신·수명 관리 |
| `Presentation/Ending/EndingQuietController.cs` | 정적 구간의 카메라 배경과 안내 표시, 카메라 상태 복원 |
| `Presentation/Ending/EndingYearRoll.cs` | 저장된 연도 Text의 숫자 굴림 효과 |
| `Presentation/Ending/EndingFocusTarget.cs` | 표시 담당 사이에서 전달하는 인식 위치·크기 값 |
| `Presentation/CinematicArchiveController.cs` | 엔딩의 이미지·영상 표시와 영상 플레이어 자원 관리 |
| `Presentation/CinematicAudioController.cs` | 재생 요청에 따른 음원·믹싱 처리 |
| `Presentation/CinematicTypeController.cs` | 전달받은 자막의 타이핑·강조·커서·배경 표시 |
| `Interaction/CinematicInteractionManager.cs` | 버튼과 트리거 요청, 조작 상태 연결 |
| `Interaction/CinematicInputController.cs` | XR 트리거·에디터 Space 입력과 햅틱 |
| `Interaction/CinematicMovementController.cs` | 이동·잡기·레이·컨트롤러·태블릿 상태 저장 및 복원 |
| `Interaction/CinematicControlsController.cs` | 시작·재시작 버튼, 길게 누르기 막대와 기존 실패 안내 표시 |
| `Assets/Scripts/Core/SceneTransitionController.cs` | 선로딩과 활성화 대기·씬 활성화·성공 상태만 담당 |
| `Assets/Editor/CinematicSceneBuilder.cs` | 장면 생성과 컴포넌트 참조 연결 |

`CinematicModelResult.cs`는 세션의 모델 층 라벨을 보관한다. `CNNModelDesignLabManager`가 학습 완료 시 INPUT → 사용자가 놓은 실제 층 → OUTPUT 라벨을 기록한다. Conv2D의 필터 수·커널 크기와 Dense의 노드 수도 라벨에 기록되지만 엔딩 카드는 층 이름만 읽는다. 층별 출력 크기는 엔딩 기록에 없다. 학습 완료 시 테스트 세트에서 이미 계산된 정확도를 선택 값으로 함께 기록한다. 새 오프닝에서 결과를 초기화한다.

### 전환 확인

오프닝 시퀀스의 상태 로그는 유지한다. 전환은 OpeningEnding의 진입점과 Core 로더로 분리했으며 이전 FadeOut/Loading/FadeIn 진단 로그는 사용하지 않는다. 로딩 예외는 Core에서 기록하고, 실패 안내는 기존 호출자가 처리한다.

## 슬롯 편집

`Assets/Data/OpeningEnding/CinematicText.asset` Inspector에서 문구, 미디어와 시간을 수정한다.

- **Opening Schedule**의 길이 값을 수정하면 뒤쪽 구간의 시작 시점이 누적 길이를 따라 이동한다. 도장 소리와 픽셀의 회전·집결은 각 기준 구간의 오프셋으로 지정한다. 오프닝 연출의 실행 순서는 `OpeningSequence.cs`에서 확인할 수 있다.
- **Opening / Ending**의 각 자막은 Anchor와 시작·종료 오프셋, 문구, 선택적 음성을 갖는다. Anchor가 Start이면 씬 재생 시작을 기준으로 한다. `{C:강조}`와 `{A:강조}`는 Cyan과 Amber 강조다.
- **PostOffice**는 엔딩 초기화에서 기록판에 연결하는 미국 우체국 전경 슬롯이다. **OpeningCrowd / OpeningScore**는 오프닝의 군중 소리와 음악이다.
- **Ending Schedule → Montage** 항목에는 미디어, 연도, 설명, 재생 길이와 인식 영역(Focus)이 있다. Focus의 X·Y는 Clip UV로 지정한 사진의 왼쪽 아래를 원점으로 하며 Width·Height와 함께 0~1로 입력한다. 16:9 크롭과 꺾쇠 위치는 이 영역에 맞춰 계산된다. 첫 모델·마지막 대사·정적 구간의 길이는 같은 일정 데이터에서 조정한다. 첫 모델의 음성은 Intro의 Voice, 마지막 대사 음성은 Outro의 Voice에 연결한다. 얼굴·간판·야구 음성은 Ending의 Element 2·3·4 각각의 Voice에 연결한다. 사진 자막의 시각은 몽타주 시작을 기준으로 하며 음성 길이가 컷 길이를 바꾸지 않는다.
- 미디어 출처는 해당 슬롯의 Source와 `docs/ASSET_CREDITS.md`에서 관리한다.

오프닝 내레이션은 사용자 녹화에서 대사별로 추출했다. 군중 소리는 3D 우체국이 나타날 때 파일의 0초부터 재생한다. 음악은 시작 버튼을 누른 뒤 재생하며 되감기와 도장 구간에 작아진다. 도장이 닿는 순간 기존 ArchiveStamp 효과음을 재생한다. 엔딩은 사용자 녹화에서 추출한 내레이션과 배경음악을 재생한다.

오프닝은 헤드셋 눈높이에서 0.75m 아래를 무대 기준으로 삼는다. 시작 대기 중에는 추적된 머리 위치·정면에 정렬하고, 시작 버튼을 누를 때 다시 정렬한 뒤 재생 중 무대는 고정한다. 씬 초기화 시 아직 머리 추적이 준비되지 않은 경우의 높이를 재생 전체에 고정하지 않는다. 카메라·XR Origin은 움직이지 않는다. 우체국 위치·연구 설명과 연표의 연도 표시는 눈높이 부근에 배치한다.

우체국·봉투·숫자·픽셀·공장 입구 구간의 자막은 무대 정면 2.5m, 눈높이보다 25cm 아래(약 5.7° 아래)에 표시한다. 숫자 중심은 눈높이보다 50cm 위에 두고 숫자·픽셀의 표시 폭을 줄여 자막과 별도 영역을 사용한다. 오프닝 내레이션 캔버스는 다른 연출 캔버스보다 나중에 그린다. 두 줄 자막 배경은 1740×200, 글자 영역은 1660×190이다. 마지막 공장 입구는 정면 8.5m에 배치하며 `CNN FACTORY` 간판, 회백색 외벽 패널·청회색 마감, 관찰창·환기구·문 점검창·손잡이·출입 패널·안전 볼라드와 청록색 라인 조명을 사용한다. 기존 체험룸의 텍스처를 참조하는 오프닝 전용 무광 Simple Lit 재질을 사용한다. 셔터가 양쪽 벽 뒤로 열리면 조명이 있는 복도가 드러난다. 바닥의 두 레일과 청록색 화살표가 입구 방향을 가리킨다.

씬을 다시 만들 때는 `Tools > Factory > Create Documentary Opening and Ending`을 실행한다. 기존 데이터 에셋의 문구·미디어 설정은 유지하며, 새 두 씬의 수동 배치는 재생성된다. 기존 다른 씬은 다시 생성하지 않는다. 오프닝 우체국·높이·음향 배치만 갱신할 때는 `Tools > Factory > Revise Opening Post Office`를 사용한다. 이 명령은 오프닝 씬의 우체국 루트를 다시 만들고 참조를 연결하며 엔딩 씬은 저장하지 않는다.

### 눈높이·우체국 인물·음향 수정 (2026-11-13)

- `OpeningVisualController`는 대기 중/재생 직전 관찰자 정렬과 기존 시각 연출을 담당한다. `CinematicSequenceManager → CinematicPresentationManager → OpeningVisualController → PostOfficeCrowdController` 순서로 제어한다. `Scripts/OpeningEnding/Presentation/Opening/PostOfficeCrowdController.cs`는 전달받은 우체국 경과 시간으로 개별 인물의 보행 위치·방향과 Idle/Walk 동작을 샘플링한다. 별도 전역 상태·이벤트·패키지는 추가하지 않는다.
- 우체국에는 Quaternius의 Suit와 Casual_2를 사용한 직원 2명·손님 2명·이동 인물 3명이 있다. 중앙 봉투를 가리는 정면 통로는 비워 두고 양쪽 통로와 창구 뒤에서 이동한다. 외부 인물 원본은 `Meshes/CNNArchiveOpening/`, 변환한 무광 표시 재질은 `Materials/CNNArchiveOpening/`에 둔다. Unity Legacy Animation에서 Idle과 Walk만 임포트한다.
- 기존 `ArchiveMaterials.png`의 목재·테라조·회벽·종이 영역을 우체국 재질에서 참조한다. 창구 서비스 명판, 패널 몰딩, 창문 가로대·창턱, 벽시계, CRT·키보드·전화기, 서류 더미·분류표를 기본 도형과 기존 폰트로 구성한다. 제작은 `Editor/CinematicSceneBuilder.Opening.cs`가 담당한다.
- Simple Lit의 `_SpecularHighlights`는 일반 Lit의 불리언 토글과 달리 enum이다. `1 (NoSpecular)`로 지정하고 `_SPECULAR_COLOR`/`_SPECGLOSSMAP`을 끈다. 실시간 그림자를 쓰지 않는 오프닝 전용 재질은 `_ReceiveShadows=0`과 `_RECEIVE_SHADOWS_OFF`로 그림자 수신도 끈다. 에디터 캡처 비교에서 그림자 수신을 켰을 때 나타나던 검은 점무늬가 이 설정으로 사라졌다. 프로젝트 전체 렌더러 설정은 바꾸지 않는다.
- 기존 오프닝 음성 6개와 엔딩 TTS 4개를 유지한다. `CinematicAudioController.Bind`에서 내레이션을 비공간화(2D), 음량 1, 우선순위 0으로 설정한다. 오프닝 군중·음악도 거리 감쇠 없는 2D로 재생하며, 군중은 내레이션 중 0.24/그 외 0.46, 음악은 내레이션 중 0.12/기본 0.38로 믹싱한다. 사용자 마스터 볼륨은 덮어쓰지 않는다.
- 기존 음성·TTS·Suno 음악의 배포 권한은 일부 미확인이다. 사용자 답변과 확인한 라이선스·원출처는 `ASSET_CREDITS.md`의 2026-11-13 기록을 따른다. 재생 가능 여부와 배포 권한 확인은 별개다.

검증(2026-11-13): 사용자 승인 후 현재 프로젝트의 Unity 6000.3.24f1/D3D11, Mobile 품질에서 컴파일·씬 반영·Play를 실행했다. 오프닝의 35초 자연 재생과 FactoryEnvironment 로드·암전 해제를 확인했다. 최종 인물 크기·그림자 수신 수정 후 10.5/11.5/19/33초 화면에서 인물 이동, 봉투와 두 줄 자막의 분리, 검은 점무늬 제거를 확인했고 관찰 카메라 위치는 유지됐다. 음성 01~06과 군중 음향의 클립·재생 상태·시간·2D 설정을 확인했다. 엔딩은 1/15.2/17.1/18.7초를 샘플링해 기존 TTS 4개의 재생 상태·2D·음량 1을 확인했다. 프로젝트 컴파일 오류는 없었으며 기존 다른 기능의 미사용 필드·미대기 비동기 호출 경고, Meta XR 초기화 경고는 남아 있다. 이 검증은 에디터 카메라 캡처·재생 상태 확인이며 Quest 스테레오·실기기 성능·헤드폰 청감이나 배포 권한 검증을 대신하지 않는다. 테스트 파일·프로젝트 복사본·APK는 생성하지 않았다.

## 렌더링·검증 범위

픽셀·봉투 등의 재질은 GPU instancing을 허용하며 영상 플레이어는 한 개만 사용한다. 포스트프로세싱 블룸·실시간 그림자·카메라 흔들기를 추가하지 않는다. 라이트맵 베이크 대신 단일 방향광과 환경광을 사용한다.

Editor D3D11 배치 Play에서 주요 시점의 실제 카메라 렌더를 저장하고, 자연 재생·공장 이동·엔딩·가상 XR 트리거·정적 구간·카메라 유지·모델 결과 표시를 검증한다. Quest 실기기 스테레오, 실제 손 컨트롤러 진동, 헤드폰 청감, APK와 72fps는 이 검증과 별개다. 임시 검증 코드는 완료 후 삭제한다.

폴더 정리 이전의 검증 결과(2026-11-09): 위 기본 경로의 Play 검증 PASS ALL, 렌더 22장 확인, 런타임 예외 없음. 임시 검증 코드 삭제 후 배치 컴파일 exit 0. 가상 XR 트리거와 샘플 모델 결과를 사용했으며 Quest 실기기·모델 재학습은 실행하지 않았다.

폴더 정리 후에는 Unity GUID와 씬·코드 참조를 정적으로 확인했다.

이번 구조 이관에서는 씬 오브젝트와 미디어를 유지하고 스크립트 연결을 변경했다. Unity 실행·컴파일·빌드·Quest 재생 검증은 수행하지 않았다.

## 3D 우체국 첫 적용

`CNNArchiveOpening` 씬의 `Post office interior`는 무대의 자식이며, `OpeningVisualController._postOffice`에 연결되어 있다. `OpeningSequence`가 Arrival부터 Digit 직전까지 표시한다. 도장·봉투 관찰 동안에도 같은 우체국 공간을 유지한다. 종료·건너뛰기에서도 함께 숨긴다. 기존 관찰자 정면 정렬, 내레이션·군중 소리·전체 일정은 유지한다. 카메라와 우체국 공간은 이동·확대하지 않는다. 봉투 소품만 위치·회전·크기를 보간한다.

세 창구, 봉투·도장, 분류 선반, 작성대·소포, 양옆 창문과 벽·천장·바닥은 Unity 기본 도형으로 구성한다. 새 단색 재질은 `Assets/Materials/CNNArchiveOpening/`에 있으며 외부 이미지·모델은 추가하지 않았다. 이 공간은 당시 우체국 분위기를 표현한 단순화된 실내이며 특정 실제 장소의 복원은 아니다. `CinematicSceneBuilder.BuildPostOffice`도 동일한 배치를 생성한다.

우체국 재질 8개는 `Universal Render Pipeline/Simple Lit`을 사용한다. 현재 무광 설정은 `_SpecularHighlights = 1 (NoSpecular)`이며 그림자 수신을 끈다. 초기 적용의 값 `0`은 Simple Lit에서 반사광을 켜는 값이므로 2026-11-13 수정에서 바로잡았다. 조명에 따른 면별 명암은 유지한다. 프로젝트의 PC/Mobile 렌더러 설정은 변경하지 않았다.

도장·봉투 3D 전환 이전 검증(2026-11-12): Unity 6000.3.24f1, D3D11 에디터 Play에서 시작 대기 → 자동 시작 → 우체국 → 도장 영상 → 봉투 → 공장 입구까지 재생했다. 런타임 품질 설정은 Mobile이었다. 우체국 8~13초 캡처에서 흰 점무늬 제거를 확인했고, 도장 영상은 14.21초에 frame 6, 15.40초에 frame 41로 진행했으며 16.8초 이후 봉투 장면으로 전환됐다. 검증 중 컴파일 오류·런타임 예외는 발견하지 않았다. Windows 영상 디코더의 기존 H.264 타임스탬프·색상 정보 경고는 남아 있다. 임시 검증 스크립트는 확인 후 삭제했다. Quest 실기기의 스테레오·프레임 성능·청감은 별도 확인이 필요하다.

### 3D 도장·봉투 구간

`OpeningVisualController`의 `_postalEnvelope`, `_postalStamp`, `_postmark`, `_postalOutline`은 씬에 저장된 봉투 루트, 움직이는 도장, 소인, 첫 숫자 인식 테두리에 연결되어 있다. 우편번호 `20501`의 획도 얇은 3D 도형이며 사진·동영상·새 폰트를 사용하지 않는다. 중앙 창구에 있던 고정 봉투와 도장은 이 소품으로 교체했다. `CinematicSceneBuilder.BuildPostalProps`가 같은 배치와 참조를 재생성한다.

- Arrival~Zoom: 봉투는 중앙 창구 위에 놓여 있다.
- Zoom~StampShot: 봉투와 도장이 관찰자 앞에 올라오며, 봉투 면이 정면으로 기울어진다.
- StampShot~Envelope: 도장이 봉투 면으로 내려갔다 올라온다. `StampContactOffset`에서 소인 표시와 기존 도장 효과음이 동시에 시작된다.
- Envelope~Digit: 도장은 숨기고 첫 숫자에 인식 테두리를 표시한다. 봉투를 확대하며 숫자 2의 중심을 기존 숫자 연출 위치에 맞춘다.
- Digit 이후 및 종료·건너뛰기: 우체국과 우편 소품을 숨기고 기존 숫자·픽셀 연출로 이어진다.

오프닝은 `CinematicArchiveController`에 사진·영상 표시나 영상 준비를 요청하지 않는다. 기존 내레이션·도장 효과음·일정 길이는 유지한다. 봉투는 정면 2m의 창구 기둥 앞에서 최대 1.8배로 확대하며, 첫 숫자의 관찰 방향을 다음 숫자 연출 중심에 맞춘다.

3D 도장·봉투 검증(2026-11-12): Unity 6000.3.24f1 D3D11 에디터의 Mobile 품질에서 시작 버튼의 기존 처리 함수를 호출한 뒤 23초까지 자연 재생했다. 첫 실행 캡처에서 봉투가 너무 높고 확대 중 창구 기둥에 가리는 문제를 발견해 위치·확대 배율과 숫자·픽셀의 높이를 보정하고 처음부터 다시 확인했다. 15.32초에는 소인이 없고, 15.46초에는 도장이 접촉하며 소인이 표시됐고, 16.30초에는 도장이 물러난 뒤 소인이 남았다. 17~20.72초에는 첫 숫자 테두리와 봉투 확대, 20.86~21.30초에는 우체국·봉투가 숨겨진 숫자 2, 22.20초에는 픽셀 전환을 확인했다. 캡처 12장과 상태 로그에서 영상 클립 미연결·미재생을 확인했으며, Play 중 오류·런타임 예외와 스크립트 컴파일 오류는 발견하지 않았다. 에디터 시작 로그에는 Meta XR 관련 경고와 중복 CancellationTokenSource.Dispose 메시지가 남아 있다. 임시 검증 스크립트는 확인 후 삭제했다. 이번 검증 범위는 숫자·픽셀 전환까지이며 공장 씬 전환·엔딩·Quest 실기기·청감 검증은 포함하지 않는다.

### 오프닝 자막 가림 방지·공장 입구 개선

오프닝의 `CinematicTypeController._drawOverScene`만 켜서 자막과 배경에 별도 UI 재질을 사용한다. 폰트가 참조하는 Unity 내장 `GUI/Text Shader` 재질을 복제하고 렌더 큐를 Overlay(4000)로 지정한다. 이 셰이더의 고정 `ZTest Always`로 도장·봉투·픽셀 등 3D 오브젝트가 자막을 가리지 않게 한다. 기본 `UI/Default`의 캔버스 깊이 설정을 재정의하는 방식은 사용하지 않는다. 재질은 씬 해제 때 정리한다. 자막은 80에서 62, 우체국 위치 설명은 54에서 42로 줄였다. 두 줄 자막 공간과 대비 배경을 유지하며 연출 구간의 읽는 높이를 고정했다. 우체국 위치 설명은 창구 기둥보다 앞인 정면 1.2m로 옮기되 배율과 높이를 함께 조정해 관찰 방향을 유지한다. 엔딩은 기존 표시 설정을 사용한다.

오프닝 입구 간판은 `CNN FACTORY`와 `LEARN / BUILD / EXPLORE`로 구성한다. 문 디테일은 각 문 루트의 자식이므로 기존 열림 동작과 함께 움직인다. `BuildOpeningEntrance`, `BuildOpeningFacadeDetails`, `BuildOpeningDoorDetails`가 씬과 같은 입구를 생성한다. 입구 표면은 `Materials/CNNArchiveOpening/Entrance*.mat` 10개를 사용한다. 기존 텍스처를 참조하되 무광 Simple Lit로 설정하여 Lit 재질에서 나타난 흰 점무늬를 제거한다. 다른 씬이 사용하는 원본 재질과 엔딩의 기존 FactoryTitle 데이터, 음성·대사·시간표는 수정하지 않았다.

후속 전체 검증: Unity 6000.3.24f1 D3D11, Mobile 품질의 에디터에서 기존 시작 처리 함수로 오프닝을 시작해 35초 전체를 자연 재생하고 FactoryEnvironment의 로드·암전 해제까지 확인했다. 25개 구간 시점과 시작·전환 후 화면을 캡처했다. 여섯 자막의 완성 문구가 화면 안에 보이고, 우체국·봉투 확대·픽셀·공장 구간에서 3D 물체에 가려지지 않는 것을 확인했다. 도장 동작 구간의 기존 무자막 시간은 유지한다. 공장 입구의 흰 점무늬, 자막 배경의 폰트 아틀라스 무늬, 처마의 보조 간판 가림을 보정한 뒤 처음부터 재검증했다. 최종 재생 로그에서 런타임 오류·예외는 없었으며 스크립트·셰이더 컴파일 오류도 발견하지 않았다. 원래 음성 01~06이 각각 약 0.5, 8.1, 17.05, 22.05, 26.25, 31.05초에 재생됨을 상태 로그로 확인했다. 음성·대사·일정 데이터와 엔딩 씬에는 변경이 없다. 임시 검증 스크립트와 메타 파일은 완료 후 삭제했다. Quest 실기기 스테레오·성능과 직접 청감은 이 에디터 검증에 포함하지 않는다.

## 공용 게이트 전환

OpeningEnding/CinematicTransitionController가 전환의 진입점이다. 공용 SceneTransition 프리팹을 영속 XR 리그 아래에 생성하고, Core/SceneTransitionController에 선로딩과 활성화를 요청한다. Core는 화면·카메라·OpeningEnding을 참조하지 않는다.

Presentation/CinematicTransitionVisualController는 프리팹에 저장된 기본 도형의 청록색 사각 게이트 4개와 청회색 가림 구를 제어한다. 프리팹에는 두 전환 Controller와 시각 Controller, 도형과 공용 재질 참조를 보관한다. 0.4초 동안 게이트를 펼치고 1초까지 주변을 가린 뒤 씬을 활성화한다. 대기 중 프레임은 계속 흐르며, 완료 후 0.6초 동안 통로를 넓히며 다음 공간을 드러낸다. 카메라는 연출로 이동·회전하지 않는다. 가림 구는 머리 위치를 따라가고 안쪽 면을 모든 방향에서 표시한다. 재질·셰이더는 Common 원본을 공유한다.

오프닝 시작 버튼에서 공장 선로딩을 시작하고, 종료·건너뛰기에서 준비한 전환을 실행한다. 공장 후문과 엔딩 재시작도 같은 진입점을 사용한다. 실패하면 도착 연출로 원래 씬을 다시 보여 주고 기존 호출자가 실패 안내를 처리한다. 전환 성공 시 인스턴스와 전용 가림 재질을 정리한다.

엔딩은 진입 전환이 완료될 때까지 초기화 상태로 기다린 뒤 약 30초 본편과 음향·입력을 시작한다. 엔딩 시간표는 데이터 에셋의 Ending Schedule을 읽는다. 씬 단독 실행은 진입 전환 없이 기존 본편을 시작한다.

이번 변경은 정적 검토만 수행했다. Unity 실행·컴파일·테스트·빌드와 Quest 양안 표시·실기기 성능 검증은 수행하지 않았다.

## 시작·재시작 버튼 편집

오프닝의 `Start Button`과 엔딩의 `Restart Button`은 씬에 저장된 기본 Unity UI로 구성한다. 버튼 루트의 Image에서 금색 테두리와 둥근 Sprite를, 자식 `Background`의 Image에서 배경색과 여백을 수정한다. 자식 `Play Icon`은 기존 폰트의 ▶ 글자로, RectTransform과 Text에서 위치·크기·색을 조정한다. 버튼 문구는 기존 `Start Label` 또는 `Button Label`에서 수정한다. 루트의 Button은 테두리 Image를 Target Graphic으로 사용하며 클릭 연결은 기존 Interaction 모듈이 담당한다.

씬 생성 도구도 같은 UI 계층을 저장한다. 이 변경은 씬 직렬화와 코드 참조를 정적으로 확인했으며 Unity 실행·컴파일·빌드·실기기 확인은 수행하지 않았다.


## 엔딩 고정 요소와 소유 관계

`CNNArchiveEnding.unity`의 `Archive surface`와 `Year`에서 중앙 사진판과 연도 표시의 위치·크기·글꼴을 편집한다. 초기화는 이 고정 배치를 다시 지정하지 않는다. 무대의 관찰자 정렬과 재생 중 애니메이션은 각 담당이 적용한다.

`Ending archive trail` 아래에는 다섯 `Retired photo <연도>`가 저장되어 있다. 각 루트는 `Archive still`과 연도 Canvas·Text를 가진다. 기본 상태는 숨김이며 Scene 뷰에서 해당 루트를 켜서 편집한다. 연결된 `EndingHistory<연도>.mat`에는 기존 사진과 크롭이 저장되어 있다. 실행 시 현재 Montage 데이터로 텍스처·크롭·연도를 갱신하고, 재질 인스턴스를 만들어 잔상별 투명도를 제어한다. 사진판과 라벨의 오브젝트 수명은 씬이 관리한다.

`Ending background/Floor grid`는 기존 26개 선을 합친 `Meshes/CNNArchiveEnding/EndingFloorGrid.asset` 하나를 참조한다. `Ending background/Dust`는 Renderer 하나를 가지며, `EndingBackgroundController`가 기존 난수 시드 321로 64개 입자를 단일 메시로 생성하고 기존 정점 애니메이션과 `Time.unscaledTime`을 적용한다. 생성한 먼지 메시만 파괴 시 해제한다. 먼지는 실행 시 표시되며, 격자·먼지의 재질은 에셋으로 연결한다. 배경 Renderer는 기존처럼 총 2개다.

`EndingVisualController`가 `EndingArchiveTrail`을 생성·보관하고 파괴 시 전용 재질을 해제한다. `EndingSequence`는 잔상 내부 객체를 받지 않고 시각 진입점의 준비·사진 설정·시간 갱신 메서드를 호출한다. 모델 카드와 인식 프레임은 각 담당이 표시 상태를 소유하고, 카메라 배경 복원은 `EndingQuietController`가 담당한다. `EndingYearRoll`은 씬에 미리 연결되어 있다.

씬 생성 도구도 동일한 고정 요소와 직렬화 참조를 저장한다. 이번 책임 분리와 씬 배치는 정적으로 검토했으며 Unity 실행·컴파일·빌드 및 Quest 성능·시각 검증은 수행하지 않았다.
