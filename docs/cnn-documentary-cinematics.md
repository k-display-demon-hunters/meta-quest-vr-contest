# CNN 다큐멘터리 오프닝·엔딩

## 실행 경로

- `Assets/Scenes/CNNArchiveOpening.unity`: 새 시작 씬. 자동 재생 20초 후 기존 `FactoryEnvironment`로 진입한다. 기존 공장·튜토리얼·학습 순서는 유지한다.
- `Assets/Scenes/CNNArchiveEnding.unity`: 새 종료 씬. `LearningStageCatalog`의 Exit에서 로드한다. 20초 후 크레딧, 트리거 길게 누르기로 새 시작 씬으로 돌아간다.
- 기존 `FactoryIntro`, `FactoryEnding` 씬과 캐릭터·프리팹은 보존했다. 신규 씬에는 사람 모형을 배치하지 않았다.
- Build Settings의 첫 씬을 `CNNArchiveOpening`으로 지정했다. 직접 열어 Play해도 기존 XRPlayerRig가 생성된다.

## 연출과 입력

오프닝은 질문 → 2026에서 1989로 숫자 지우기/타이핑과 연표 이동 → 작은 우체국 사진판 → 6.7~7.0초 정적 → 7초 노출 번쩍임과 공간 음향 개방 → 봉투 인식 → 눈앞의 봉투와 큰 숫자 → 숫자 획의 16×16 격자 빛 조각 → 공장 문 열림이다. 정면 기록판은 0.6초 동안 커지고, 그 앞과 양옆·뒤에 3D 창구·분류 선반·전화·프린터·컨베이어·우편물이 배치된다. CRT 색은 모니터와 인식 상자에 집중하고 사진의 자연스러운 색을 남겼다.

엔딩은 8개 연도/응용 장면을 짧게 통과한다. 중앙 사진판과 양옆 62° 사진판, 가까운 3D 소품을 함께 보여준다. 14~15초는 무음·검정, 주변 인식 상자가 정면에 모인 후 16.6초 실제 설계 결과의 층 구조가 나타난다. 마지막 문구는 `그 시선의 끝에, / 당신의 CNN이 있다.`로 바꿨다. 원안의 `60년`은 1989→2026 연표와 맞지 않아 사용하지 않는다. 몽타주 연도는 응용 분야를 보여주는 연출용이며 최초 발명·상용화 연도를 주장하지 않는다.

카메라·XR Origin의 위치와 회전은 시퀀스가 변경하지 않는다. 시작 때 무대 전체를 현재 관찰자 위치·정면에 맞춘다. 기존 `FactoryIntroInputController`의 이동 잠금을 재사용하고 종료·비활성화 때 복원한다. 오른손 또는 왼손 트리거를 연속 2초 누르면 건너뛴다. 놓으면 진행률이 초기화되며, 한 번의 길게 누르기로 건너뛰기와 재시작이 동시에 발생하지 않는다. 에디터에서는 Space 길게 누르기도 가능하다. OS 마우스를 움직이는 검증 기능은 없다.

## 책임과 파일

공용 코드는 `Assets/Scripts/Core`에 둔다. 다른 씬의 기존 코드는 이동하지 않는다.

| 파일 | 역할 |
| --- | --- |
| CinematicSequenceManager.cs | 20초 재생·완료·건너뛰기·씬 전환, 모듈 이벤트 연결 |
| CinematicPresentationManager.cs | 글자·시각·음향의 시점 조율, 햅틱 요청 |
| CinematicWorldController.cs | 기록판 확장, 연표·봉투·인식 상자·픽셀·문·엔딩 모델 표시 |
| CinematicTypeController.cs | 55ms/공백30ms 타이핑, 강조색, 커서, 자막 띠와 길게 누르기 진행 표시 |
| CinematicInputController.cs | XR 트리거 연속 누름, 에디터 Space, 기존 이동 잠금, 햅틱 |
| CinematicAudioController.cs | 위치별 소리, 정적, 음성 시작, 내레이션 중 군중 −6dB, 12~14초 저역통과 |
| CinematicArchiveController.cs | 실제 VideoClip 또는 생성 정지 이미지 재생, 영상 오류 시 정지 이미지 유지 |
| CinematicText.cs | ScriptableObject 문구·영상/음성/효과음 슬롯·출처 데이터 |
| CinematicModelResult.cs | 이번 체험에서 학습을 마친 모델의 층 라벨만 보관하는 세션 데이터 |
| Assets/Editor/CinematicSceneBuilder.cs | 새 씬 두 개와 미디어 연결·재질·정적 공간 메시 생성 |
| Assets/Shaders/Common/ArchivePhoto.shader | 사진 색/흑백, 약한 필름 그레인·비네트, XR single-pass 대응 |

`CNNModelDesignLabManager`가 학습 완료 시 INPUT → 사용자가 놓은 실제 층 → OUTPUT 라벨을 공용 결과 에셋에 기록한다. Conv2D의 필터 수·커널 크기, Dense의 노드 수도 표시한다. 기존 학습 엔진·모델 JSON·파일 저장 형식은 변경하지 않는다. 새 오프닝에서 결과를 초기화한다. 엔딩만 단독 실행하여 결과가 없으면 `구조 예시 · 설계실 결과 없음`을 명시한다. 데이터는 디스크에 저장하지 않는다.

오프닝에서 공장으로 실제 비동기 전환할 때 발견한 기존 오류도 수정했다. `FactoryEnvironmentManager`의 `SetActiveScene` 요청을 아직 로딩 중인 `OnEnable`에서 로딩 완료 후 `Start`로 옮겼다. 학습 단계·이동 코드는 변경하지 않았다. 음원 정리는 씬 루트 해제 순서에 따라 AudioSource가 먼저 파괴되는 경우를 처리한다.

## 슬롯 편집

`Assets/Data/Common/CinematicText.asset` Inspector에서 수정한다.

- Opening / Ending: Start, End, Text와 선택적 Voice. `{C:강조}`와 `{A:강조}`는 각각 Cyan/Amber이다. 기본 시퀀스는 20초이므로 녹음은 각 줄의 시간 안에 맞춘다.
- PostOffice: `PO_1989_hall` 기록판. Video에 소유/사용 허가가 있는 영상 파일을 넣으면 Still 대신 재생한다. StartSeconds와 Duration을 설정한다.
- Montage: `END_1998`~`END_2026` 8개 슬롯. 기본 Still은 하나의 사진 아틀라스를 UV로 나누어 쓰며 원본 비율을 유지한다. Video를 넣으면 전체 영상 UV로 재생한다.
- Source / AudioCredits: 영상·음성·음악을 교체할 때 실제 출처와 사용 조건을 함께 적고 `docs/ASSET_CREDITS.md`도 갱신한다.
- Type / Hit / BigHit / Rewind / Ring / Stamp / Machine / Printer / Crowd / Footsteps / Radio / Clerk / CRT / Beep / Pad: 선택적 AudioClip이다. 비어 있으면 해당 소리는 재생하지 않는다.

실제 군중·프린터 녹음과 직접 합성한 기계·전환 효과음은 연결되어 있다. 내레이션, 직원 대사, 라디오 가요, 배경 음악은 제공된 녹음이 없어 비어 있다. 생성 사진은 실제 1989년 기록영상이 아니며, 사람 없는 공간 재구성이다. 이미지가 움직이는 사람/기계의 실사 영상으로 변환된 것은 아니다. 본문은 C# 시간 시퀀스로 평가하며 Unity Timeline 에셋을 사용하지 않는다.

씬을 다시 만들 때는 `Tools > Factory > Create Documentary Opening and Ending`을 실행한다. 기존 데이터 에셋의 문구·미디어 설정은 유지하며, 새 두 씬의 수동 배치는 재생성된다. 기존 다른 씬은 다시 생성하지 않는다.

## 렌더링·검증 범위

우체국의 정적 기본 도형은 재질별 메시로 합쳐 저장한다. 픽셀·봉투 등의 재질은 GPU instancing을 허용하며 영상 플레이어는 한 개만 사용한다. 포스트프로세싱 블룸·실시간 그림자·카메라 흔들기를 추가하지 않는다. 라이트맵 베이크 대신 단일 방향광과 환경광을 사용한다.

Editor D3D11 배치 Play에서 주요 시점의 실제 카메라 렌더를 저장하고, 자연 재생·공장 이동·엔딩·가상 XR 트리거·정적 구간·카메라 유지·모델 결과 표시를 검증한다. Quest 실기기 스테레오, 실제 손 컨트롤러 진동, 헤드폰 청감, APK와 72fps는 이 검증과 별개다. 임시 검증 코드는 완료 후 삭제한다.

최종 결과(2026-11-09): 위 기본 경로의 Play 검증 PASS ALL, 렌더 22장 확인, 런타임 예외 없음. 임시 검증 코드 삭제 후 배치 컴파일 exit 0. 가상 XR 트리거와 샘플 모델 결과를 사용했으며 Quest 실기기·모델 재학습은 실행하지 않았다.
