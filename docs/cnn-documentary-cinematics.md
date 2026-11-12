# CNN 다큐멘터리 오프닝·엔딩

## 실행 경로

- `Assets/Scenes/CNNArchiveOpening.unity`: 시작 씬. 시작 버튼을 누르면 재생하고 35초 후 기존 `FactoryEnvironment`로 진입한다. 기존 공장·튜토리얼·학습 순서는 유지한다.
- `Assets/Scenes/CNNArchiveEnding.unity`: 종료 씬. `LearningStageCatalog`의 Exit에서 로드한다. 20초 재생 후 또는 건너뛴 뒤 완료 화면을 표시한다.
- Build Settings의 첫 씬을 `CNNArchiveOpening`으로 지정했다. 직접 열어 Play해도 기존 XRPlayerRig가 생성된다.

## 연출과 입력

오프닝은 시작 버튼 → 질문 → 되감기 소리에 맞춘 2026~1989 연도 카운트다운 → 미국 우체국 사진의 등장과 확대 → 오른쪽 창구 줌인 → 2.8초 도장 컷 → 봉투 우편번호에서 숫자 2 확대 → 16×16 격자의 빛 조각 → 공장 문 열림 순이다. 연도 카운트다운은 1989에 가까워질수록 느려지며 낮은 펄스가 박자를 따라간다. 우체국 사진에서는 인물 일부에만 작은 움직임을 더한다. 도장 컷은 약 12% 확대해 우편번호를 보여주고, 흰 숫자 2는 위에서부터 민트색 픽셀로 전환된다. 장소와 얀 르쿤 연구팀 표기는 화면에만 표시한다. 오른손 XR 레이로 시작 버튼을 가리키고 트리거를 눌러 재생한다.

엔딩은 8개 연도/응용 장면을 짧게 통과한다. 중앙 사진판과 양옆 62° 사진판, 가까운 3D 소품을 함께 보여준다. 14~15초는 무음·검정, 주변 인식 상자가 정면에 모인 후 16.6초 실제 설계 결과의 층 구조가 나타난다. 마지막 문구는 `그 시선의 끝에, / 당신의 CNN이 있다.`로 바꿨다. 원안의 `60년`은 1989→2026 연표와 맞지 않아 사용하지 않는다. 몽타주 연도는 응용 분야를 보여주는 연출용이며 최초 발명·상용화 연도를 주장하지 않는다.

카메라·XR Origin의 위치와 회전은 시퀀스가 변경하지 않는다. 시작 때 무대 전체를 현재 관찰자 위치·정면에 맞춘다. 오프닝 시작 대기 화면과 엔딩 완료 화면에는 양손 컨트롤러 모델과 오른손 레이가 보인다. 손목 태블릿은 씬에 들어갈 때 숨기고, 컨트롤러 모델과 레이는 재생 중에 숨긴다. `CinematicMovementController`로 이동·잡기와 레이 표시를 제어하고 씬 종료·비활성화 때 입력과 표시 상태를 복원한다. 오른손 또는 왼손 트리거를 연속 2초 누르면 건너뛴다. 트리거를 누르면 진행 막대가 채워지고 놓으면 초기화된다. 에디터에서는 Space 길게 누르기도 가능하다.

엔딩 완료 화면에는 `CNN 공장 · 체험 완료`와 `처음부터 다시 시작` 버튼이 표시된다. 오른손 레이로 버튼을 선택하거나 트리거를 다시 2초간 누르면 오프닝 시작 화면으로 돌아간다. 트리거를 누른 채 엔딩이 완료되면 컨트롤러 모델을 먼저 표시하고, 트리거를 놓을 때 레이를 켠다.

컨트롤러 모델과 태블릿 표시는 공용 `PersistentXRPlayerRig.SetControllerModelsVisible()`, `SetTabletVisible()`로 요청한다. `CinematicMovementController`는 리그의 표시 상태를 저장하고 씬 종료 시 복원한다. 공용 리그는 컨트롤러 모델 표시를 처리하고 태블릿 요청은 `TabletManager.SetAvailable()`로 전달한다. 태블릿 모듈이 자신의 표시와 입력 활성화를 관리하며, 표시를 숨긴 동안에도 양손 위치 추적은 계속 동작한다.

## 책임과 파일

연출 코드는 `Assets/Scripts/OpeningEnding/`에 둔다. `CinematicSequenceManager`가 씬의 시작 대기, 재생, 완료, 전환 및 재시도 상태를 소유하고 연출·조작 모듈의 이벤트를 연결한다. 오프닝이 완료되면 공장으로 전환하고, 엔딩이 완료되면 재시작 요청을 기다린다.

| 경로 | 책임 |
| --- | --- |
| `Presentation/CinematicPresentationManager.cs` | 재생 시간 관리, 오프닝·엔딩 실행 선택, 자막·음성 연결과 재생 정리 |
| `Presentation/Opening/OpeningSequence.cs` | 되감기, 우체국, 도장, 봉투, 숫자, 픽셀, 공장 문의 실행 시점 결정 |
| `Presentation/Ending/EndingSequence.cs` | 몽타주, 정적, 인식 상자와 모델 표시 시점 결정 |
| `Presentation/Opening/OpeningVisualController.cs` / `Presentation/Ending/EndingVisualController.cs` | 씬에 저장된 연출 오브젝트의 표시·배치·보간 |
| `Presentation/CinematicArchiveController.cs` | 이미지·영상 표시와 영상 플레이어 자원 관리 |
| `Presentation/CinematicAudioController.cs` | 재생 요청에 따른 음원·믹싱 처리 |
| `Presentation/CinematicTypeController.cs` | 전달받은 자막의 타이핑·강조·커서·배경 표시 |
| `Interaction/CinematicInteractionManager.cs` | 버튼과 트리거 요청, 조작 상태 연결 |
| `Interaction/CinematicInputController.cs` | XR 트리거·에디터 Space 입력과 햅틱 |
| `Interaction/CinematicMovementController.cs` | 이동·잡기·레이·컨트롤러·태블릿 상태 저장 및 복원 |
| `Interaction/CinematicControlsController.cs` | 시작·재시작 버튼, 길게 누르기 막대와 기존 실패 안내 표시 |
| `Assets/Scripts/Core/SceneTransitionController.cs` | 영속 XR 리그 아래에서 기존 암전·씬 로드·암전 해제를 실행 |
| `Assets/Editor/CinematicSceneBuilder.cs` | 장면 생성과 컴포넌트 참조 연결 |

`CinematicModelResult.cs`는 세션의 모델 층 라벨을 보관한다. `CNNModelDesignLabManager`가 학습 완료 시 INPUT → 사용자가 놓은 실제 층 → OUTPUT 라벨을 기록한다. Conv2D의 필터 수·커널 크기와 Dense의 노드 수도 표시한다. 새 오프닝에서 결과를 초기화한다. 엔딩만 단독 실행하여 결과가 없으면 `구조 예시 · 설계실 결과 없음`을 표시한다.

### 오프닝 진단 로그

Quest Logcat에서 Unity 태그와 `[Opening]`을 검색한다. `run`은 같은 오프닝 실행의 상태·구간·영상·전환을 연결하고, `transition`은 공장 전환 시도를 구별한다. 정상 전환은 `LoadCheck → FadeOutCompleted → LoadStarted → SceneLoaded → LoadCompleted → FadeInCompleted` 순서로 확인한다. 로딩 중에는 진행률이 10%p 이상 변하거나 마지막 기록 이후 5초가 지나면 활성 씬, 카메라 활성 상태, 암전 알파를 함께 기록한다. 전환 로그는 오프닝 씬이 해제된 뒤에도 XR 리그에서 이어진다.

오프닝에서 공장으로 실제 비동기 전환할 때 발견한 기존 오류도 수정했다. `FactoryEnvironmentManager`의 `SetActiveScene` 요청을 아직 로딩 중인 `OnEnable`에서 로딩 완료 후 `Start`로 옮겼다. 학습 단계·이동 코드는 변경하지 않았다. 음원 정리는 씬 루트 해제 순서에 따라 AudioSource가 먼저 파괴되는 경우를 처리한다.

## 슬롯 편집

`Assets/Data/OpeningEnding/CinematicText.asset` Inspector에서 문구, 미디어와 시간을 수정한다.

- **Opening Schedule**의 길이 값을 수정하면 뒤쪽 구간의 시작 시점이 누적 길이를 따라 이동한다. 도장 소리와 픽셀의 회전·집결은 각 기준 구간의 오프셋으로 지정한다. 오프닝 연출의 실행 순서는 `OpeningSequence.cs`에서 확인할 수 있다.
- **Opening / Ending**의 각 자막은 Anchor와 시작·종료 오프셋, 문구, 선택적 음성을 갖는다. Anchor가 Start이면 씬 재생 시작을 기준으로 한다. `{C:강조}`와 `{A:강조}`는 Cyan과 Amber 강조다.
- **PostOffice / PostOfficeStamp / PostOfficeEnvelope**는 미국 우체국 전경, 도장 영상, 봉투 사진이다. **OpeningCrowd / OpeningScore**는 군중 소리와 음악이다.
- **Ending Schedule → Montage** 항목에는 미디어, 연도, 설명, 재생 길이와 기존 소품 인덱스가 함께 있다. 소품 인덱스는 연결된 씬 오브젝트를 가리킨다. 몽타주 종료 이후의 인식·모델 시점은 같은 일정 데이터에서 조정한다.
- 미디어 출처는 해당 슬롯의 Source와 `docs/ASSET_CREDITS.md`에서 관리한다.

오프닝 내레이션은 사용자 녹화에서 대사별로 추출했다. 군중 소리는 우체국 사진이 나타날 때 파일의 0초부터 재생한다. 음악은 시작 버튼을 누른 뒤 재생하며 되감기와 도장 구간에 작아진다. 도장이 닿는 순간 기존 ArchiveStamp 효과음을 재생한다. 엔딩의 TTS와 전환 연출은 기존 경로를 사용한다.

오프닝은 헤드셋 눈높이에서 1.15m 아래를 무대 기준으로 삼는다. 우체국 위치·연구 설명과 연표의 연도 표시는 눈높이 부근에 배치한다. 사진·영상 위에서는 설명과 내레이션 뒤에 어두운 배경을 표시한다.

손글씨 숫자와 픽셀이 나오는 구간의 자막은 무대 정면 2.5m, 눈높이보다 12cm 위에 표시한다. 오프닝 내레이션 캔버스는 다른 연출 캔버스보다 나중에 그린다. 마지막 공장 입구는 체험룸의 콘크리트, 아연도금 셔터, 금속 간판 재질을 사용한다. 셔터가 양쪽 벽 뒤로 열리면 조명이 있는 복도가 드러난다. 바닥의 두 레일과 청록색 화살표가 입구 방향을 가리킨다.

씬을 다시 만들 때는 `Tools > Factory > Create Documentary Opening and Ending`을 실행한다. 기존 데이터 에셋의 문구·미디어 설정은 유지하며, 새 두 씬의 수동 배치는 재생성된다. 기존 다른 씬은 다시 생성하지 않는다.

## 렌더링·검증 범위

픽셀·봉투 등의 재질은 GPU instancing을 허용하며 영상 플레이어는 한 개만 사용한다. 포스트프로세싱 블룸·실시간 그림자·카메라 흔들기를 추가하지 않는다. 라이트맵 베이크 대신 단일 방향광과 환경광을 사용한다.

Editor D3D11 배치 Play에서 주요 시점의 실제 카메라 렌더를 저장하고, 자연 재생·공장 이동·엔딩·가상 XR 트리거·정적 구간·카메라 유지·모델 결과 표시를 검증한다. Quest 실기기 스테레오, 실제 손 컨트롤러 진동, 헤드폰 청감, APK와 72fps는 이 검증과 별개다. 임시 검증 코드는 완료 후 삭제한다.

폴더 정리 이전의 검증 결과(2026-11-09): 위 기본 경로의 Play 검증 PASS ALL, 렌더 22장 확인, 런타임 예외 없음. 임시 검증 코드 삭제 후 배치 컴파일 exit 0. 가상 XR 트리거와 샘플 모델 결과를 사용했으며 Quest 실기기·모델 재학습은 실행하지 않았다.

폴더 정리 후에는 Unity GUID와 씬·코드 참조를 정적으로 확인했다.

이번 구조 이관에서는 씬 오브젝트와 미디어를 유지하고 스크립트 연결을 변경했다. Unity 실행·컴파일·빌드·Quest 재생 검증은 수행하지 않았다.
