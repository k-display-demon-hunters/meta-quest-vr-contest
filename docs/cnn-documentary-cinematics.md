# CNN 다큐멘터리 오프닝·엔딩

## 실행 경로

- `Assets/Scenes/CNNArchiveOpening.unity`: 시작 씬. 시작 버튼을 누르면 재생하고 35초 후 기존 `FactoryEnvironment`로 진입한다. 기존 공장·튜토리얼·학습 순서는 유지한다.
- `Assets/Scenes/CNNArchiveEnding.unity`: 종료 씬. `LearningStageCatalog`의 Exit에서 로드한다. 20초 재생 후 또는 건너뛴 뒤 완료 화면을 표시한다.
- Build Settings의 첫 씬을 `CNNArchiveOpening`으로 지정했다. 직접 열어 Play해도 기존 XRPlayerRig가 생성된다.

## 연출과 입력

오프닝은 시작 버튼 → 질문 → 되감기 소리에 맞춘 2026~1989 연도 카운트다운 → 사용자를 둘러싼 3D 우체국 실내 → 창구의 봉투 들어 올리기 → 2.8초 3D 도장 동작 → 3D 봉투 우편번호에서 숫자 2 확대 → 16×16 격자의 빛 조각 → 공장 문 열림 순이다. 연도 카운트다운은 1989에 가까워질수록 느려지며 낮은 펄스가 박자를 따라간다. 우체국 구간은 고정된 실내 공간에서 고개를 돌려 창구·우편 분류 선반·창문·작성대·소포를 둘러보는 방식이다. 기존 사진 확대와 창구 줌인은 사용하지 않는다. 봉투·우표·우편번호·도장·소인은 기본 도형으로 구성한 입체 오브젝트다. 도장은 접촉 시점에 소리를 내고 소인을 남기며, 봉투가 관찰자 앞에서 회전·확대되어 첫 숫자 2를 보여준다. 이어서 흰 숫자 2는 위에서부터 민트색 픽셀로 전환된다. 장소와 얀 르쿤 연구팀 표기는 화면에만 표시한다. 오른손 XR 레이로 시작 버튼을 가리키고 트리거를 눌러 재생한다.

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
| `Presentation/CinematicArchiveController.cs` | 엔딩의 이미지·영상 표시와 영상 플레이어 자원 관리 |
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
- **PostOffice / PostOfficeStamp / PostOfficeEnvelope**는 이전 미국 우체국 전경, 도장 영상, 봉투 사진을 보관하는 슬롯이다. 현재 오프닝은 이 미디어를 표시하거나 영상을 준비·재생하지 않는다. **OpeningCrowd / OpeningScore**는 군중 소리와 음악이다.
- **Ending Schedule → Montage** 항목에는 미디어, 연도, 설명, 재생 길이와 기존 소품 인덱스가 함께 있다. 소품 인덱스는 연결된 씬 오브젝트를 가리킨다. 몽타주 종료 이후의 인식·모델 시점은 같은 일정 데이터에서 조정한다.
- 미디어 출처는 해당 슬롯의 Source와 `docs/ASSET_CREDITS.md`에서 관리한다.

오프닝 내레이션은 사용자 녹화에서 대사별로 추출했다. 군중 소리는 3D 우체국이 나타날 때 파일의 0초부터 재생한다. 음악은 시작 버튼을 누른 뒤 재생하며 되감기와 도장 구간에 작아진다. 도장이 닿는 순간 기존 ArchiveStamp 효과음을 재생한다. 엔딩의 TTS와 전환 연출은 기존 경로를 사용한다.

오프닝은 헤드셋 눈높이에서 1.15m 아래를 무대 기준으로 삼는다. 우체국 위치·연구 설명과 연표의 연도 표시는 눈높이 부근에 배치한다. 사진·영상 위에서는 설명과 내레이션 뒤에 어두운 배경을 표시한다.

손글씨 숫자와 픽셀이 나오는 구간의 자막은 무대 정면 2.5m, 눈높이보다 70cm 아래에 표시한다. 숫자 중심은 눈높이보다 20cm 위에 두어 숫자와 하단 자막이 겹치지 않게 한다. 오프닝 내레이션 캔버스는 다른 연출 캔버스보다 나중에 그린다. 마지막 공장 입구는 체험룸의 콘크리트, 아연도금 셔터, 금속 간판 재질을 사용한다. 셔터가 양쪽 벽 뒤로 열리면 조명이 있는 복도가 드러난다. 바닥의 두 레일과 청록색 화살표가 입구 방향을 가리킨다.

씬을 다시 만들 때는 `Tools > Factory > Create Documentary Opening and Ending`을 실행한다. 기존 데이터 에셋의 문구·미디어 설정은 유지하며, 새 두 씬의 수동 배치는 재생성된다. 기존 다른 씬은 다시 생성하지 않는다.

## 렌더링·검증 범위

픽셀·봉투 등의 재질은 GPU instancing을 허용하며 영상 플레이어는 한 개만 사용한다. 포스트프로세싱 블룸·실시간 그림자·카메라 흔들기를 추가하지 않는다. 라이트맵 베이크 대신 단일 방향광과 환경광을 사용한다.

Editor D3D11 배치 Play에서 주요 시점의 실제 카메라 렌더를 저장하고, 자연 재생·공장 이동·엔딩·가상 XR 트리거·정적 구간·카메라 유지·모델 결과 표시를 검증한다. Quest 실기기 스테레오, 실제 손 컨트롤러 진동, 헤드폰 청감, APK와 72fps는 이 검증과 별개다. 임시 검증 코드는 완료 후 삭제한다.

폴더 정리 이전의 검증 결과(2026-11-09): 위 기본 경로의 Play 검증 PASS ALL, 렌더 22장 확인, 런타임 예외 없음. 임시 검증 코드 삭제 후 배치 컴파일 exit 0. 가상 XR 트리거와 샘플 모델 결과를 사용했으며 Quest 실기기·모델 재학습은 실행하지 않았다.

폴더 정리 후에는 Unity GUID와 씬·코드 참조를 정적으로 확인했다.

이번 구조 이관에서는 씬 오브젝트와 미디어를 유지하고 스크립트 연결을 변경했다. Unity 실행·컴파일·빌드·Quest 재생 검증은 수행하지 않았다.

## 3D 우체국 첫 적용

`CNNArchiveOpening` 씬의 `Post office interior`는 무대의 자식이며, `OpeningVisualController._postOffice`에 연결되어 있다. `OpeningSequence`가 Arrival부터 Digit 직전까지 표시한다. 도장·봉투 관찰 동안에도 같은 우체국 공간을 유지한다. 종료·건너뛰기에서도 함께 숨긴다. 기존 관찰자 정면 정렬, 내레이션·군중 소리·전체 일정은 유지한다. 카메라와 우체국 공간은 이동·확대하지 않는다. 봉투 소품만 위치·회전·크기를 보간한다.

세 창구, 봉투·도장, 분류 선반, 작성대·소포, 양옆 창문과 벽·천장·바닥은 Unity 기본 도형으로 구성한다. 새 단색 재질은 `Assets/Materials/CNNArchiveOpening/`에 있으며 외부 이미지·모델은 추가하지 않았다. 이 공간은 당시 우체국 분위기를 표현한 단순화된 실내이며 특정 실제 장소의 복원은 아니다. `CinematicSceneBuilder.BuildPostOffice`도 동일한 배치를 생성한다.

우체국 재질 8개는 `Universal Render Pipeline/Simple Lit`의 무광 설정(`_SpecularHighlights = 0`)을 사용한다. 기존 Lit 재질의 광택·환경 반사 경로에서 화면 전체에 나타나던 흰 점무늬를 제거했다. 조명에 따른 면별 명암은 유지한다. 프로젝트의 PC/Mobile 렌더러 설정은 변경하지 않았다.

도장·봉투 3D 전환 이전 검증(2026-11-12): Unity 6000.3.24f1, D3D11 에디터 Play에서 시작 대기 → 자동 시작 → 우체국 → 도장 영상 → 봉투 → 공장 입구까지 재생했다. 런타임 품질 설정은 Mobile이었다. 우체국 8~13초 캡처에서 흰 점무늬 제거를 확인했고, 도장 영상은 14.21초에 frame 6, 15.40초에 frame 41로 진행했으며 16.8초 이후 봉투 장면으로 전환됐다. 검증 중 컴파일 오류·런타임 예외는 발견하지 않았다. Windows 영상 디코더의 기존 H.264 타임스탬프·색상 정보 경고는 남아 있다. 임시 검증 스크립트는 확인 후 삭제했다. Quest 실기기의 스테레오·프레임 성능·청감은 별도 확인이 필요하다.

### 3D 도장·봉투 구간

`OpeningVisualController`의 `_postalEnvelope`, `_postalStamp`, `_postmark`, `_postalOutline`은 씬에 저장된 봉투 루트, 움직이는 도장, 소인, 첫 숫자 인식 테두리에 연결되어 있다. 우편번호 `20501`의 획도 얇은 3D 도형이며 사진·동영상·새 폰트를 사용하지 않는다. 중앙 창구에 있던 고정 봉투와 도장은 이 소품으로 교체했다. `CinematicSceneBuilder.BuildPostalProps`가 같은 배치와 참조를 재생성한다.

- Arrival~Zoom: 봉투는 중앙 창구 위에 놓여 있다.
- Zoom~StampShot: 봉투와 도장이 관찰자 앞에 올라오며, 봉투 면이 정면으로 기울어진다.
- StampShot~Envelope: 도장이 봉투 면으로 내려갔다 올라온다. `StampContactOffset`에서 소인 표시와 기존 도장 효과음이 동시에 시작된다.
- Envelope~Digit: 도장은 숨기고 첫 숫자에 인식 테두리를 표시한다. 봉투를 확대하며 숫자 2의 중심을 기존 숫자 연출 위치에 맞춘다.
- Digit 이후 및 종료·건너뛰기: 우체국과 우편 소품을 숨기고 기존 숫자·픽셀 연출로 이어진다.

오프닝은 `CinematicArchiveController`에 사진·영상 표시나 영상 준비를 요청하지 않는다. 기존 미디어 파일과 데이터 슬롯은 보존했다. 기존 내레이션·도장 효과음·일정 길이는 유지한다. 봉투는 정면 1.65m의 창구 기둥 앞에서 확대하며, 첫 숫자의 관찰 방향을 다음 숫자 연출 중심에 맞춘다.

3D 도장·봉투 검증(2026-11-12): Unity 6000.3.24f1 D3D11 에디터의 Mobile 품질에서 시작 버튼의 기존 처리 함수를 호출한 뒤 23초까지 자연 재생했다. 첫 실행 캡처에서 봉투가 너무 높고 확대 중 창구 기둥에 가리는 문제를 발견해 위치·확대 배율과 숫자·픽셀의 높이를 보정하고 처음부터 다시 확인했다. 15.32초에는 소인이 없고, 15.46초에는 도장이 접촉하며 소인이 표시됐고, 16.30초에는 도장이 물러난 뒤 소인이 남았다. 17~20.72초에는 첫 숫자 테두리와 봉투 확대, 20.86~21.30초에는 우체국·봉투가 숨겨진 숫자 2, 22.20초에는 픽셀 전환을 확인했다. 캡처 12장과 상태 로그에서 영상 클립 미연결·미재생을 확인했으며, Play 중 오류·런타임 예외와 스크립트 컴파일 오류는 발견하지 않았다. 에디터 시작 로그에는 Meta XR 관련 경고와 중복 CancellationTokenSource.Dispose 메시지가 남아 있다. 임시 검증 스크립트는 확인 후 삭제했다. 이번 검증 범위는 숫자·픽셀 전환까지이며 공장 씬 전환·엔딩·Quest 실기기·청감 검증은 포함하지 않는다.
