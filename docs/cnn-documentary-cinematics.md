# CNN 다큐멘터리 오프닝·엔딩

## 실행 경로

- `Assets/Scenes/CNNArchiveOpening.unity`: 시작 씬. 시작 버튼을 누르면 재생하고 35초 후 기존 `FactoryEnvironment`로 진입한다. 기존 공장·튜토리얼·학습 순서는 유지한다.
- `Assets/Scenes/CNNArchiveEnding.unity`: 새 종료 씬. `LearningStageCatalog`의 Exit에서 로드한다. 20초 후 크레딧, 트리거 길게 누르기로 새 시작 씬으로 돌아간다.
- Build Settings의 첫 씬을 `CNNArchiveOpening`으로 지정했다. 직접 열어 Play해도 기존 XRPlayerRig가 생성된다.

## 연출과 입력

오프닝은 시작 버튼 → 질문 → 되감기 소리에 맞춘 2026~1989 연도 카운트다운 → 미국 우체국 사진의 등장과 확대 → 오른쪽 창구 줌인 → 2.8초 도장 컷 → 봉투 우편번호에서 숫자 2 확대 → 16×16 격자의 빛 조각 → 공장 문 열림 순이다. 연도 카운트다운은 1989에 가까워질수록 느려진다. 장소와 얀 르쿤 연구팀 표기는 화면에만 표시한다. 오른손 XR 레이로 시작 버튼을 가리키고 트리거를 눌러 재생한다.

엔딩은 8개 연도/응용 장면을 짧게 통과한다. 중앙 사진판과 양옆 62° 사진판, 가까운 3D 소품을 함께 보여준다. 14~15초는 무음·검정, 주변 인식 상자가 정면에 모인 후 16.6초 실제 설계 결과의 층 구조가 나타난다. 마지막 문구는 `그 시선의 끝에, / 당신의 CNN이 있다.`로 바꿨다. 원안의 `60년`은 1989→2026 연표와 맞지 않아 사용하지 않는다. 몽타주 연도는 응용 분야를 보여주는 연출용이며 최초 발명·상용화 연도를 주장하지 않는다.

카메라·XR Origin의 위치와 회전은 시퀀스가 변경하지 않는다. 시작 때 무대 전체를 현재 관찰자 위치·정면에 맞춘다. `CinematicMovementController`로 이동·잡기와 레이 표시를 제어하고 종료·비활성화 때 복원한다. 오른손 또는 왼손 트리거를 연속 2초 누르면 건너뛴다. 놓으면 진행률이 초기화되며, 한 번의 길게 누르기로 건너뛰기와 재시작이 동시에 발생하지 않는다. 에디터에서는 Space 길게 누르기도 가능하다.

## 책임과 파일

연출 코드는 `Assets/Scripts/OpeningEnding/`에 둔다. 문구와 모델 결과 에셋은 `Assets/Data/OpeningEnding/`에 두고, 오디오·재질·메시·텍스처·셰이더도 각 에셋 종류의 `OpeningEnding/`에 둔다.

| 파일 | 역할 |
| --- | --- |
| CinematicSequenceManager.cs | 시작 대기, 오프닝 35초·엔딩 20초 재생·완료·건너뛰기·씬 전환, 모듈 이벤트 연결 |
| CinematicPresentationManager.cs | 글자·시각·음향의 시점 조율, 햅틱 요청 |
| CinematicWorldController.cs | 기록판 확장, 연표·봉투·인식 상자·픽셀·문·엔딩 모델 표시 |
| CinematicTypeController.cs | 55ms/공백30ms 타이핑, 강조색, 커서, 자막 띠와 길게 누르기 진행 표시 |
| CinematicInputController.cs | XR 트리거 연속 누름, 에디터 Space, 기존 이동 잠금, 햅틱 |
| CinematicMovementController.cs | 연출 동안 이동·잡기·레이 상태를 제한하고 종료 시 복원 |
| CinematicAudioController.cs | 위치별 소리, 정적, 음성 시작, 오프닝 음악·군중 음량 조절, 도장 접촉음 |
| CinematicArchiveController.cs | 실제 VideoClip 또는 생성 정지 이미지 재생, 영상 오류 시 정지 이미지 유지 |
| CinematicText.cs | ScriptableObject 문구·영상/음성/효과음 슬롯·출처 데이터 |
| CinematicModelResult.cs | 이번 체험에서 학습을 마친 모델의 층 라벨만 보관하는 세션 데이터 |
| Assets/Editor/CinematicSceneBuilder.cs | 오프닝·엔딩 씬 생성과 미디어·재질 연결 |
| Assets/Shaders/OpeningEnding/ArchivePhoto.shader | 사진 색/흑백, 약한 필름 그레인·비네트, XR single-pass 대응 |

`CNNModelDesignLabManager`가 학습 완료 시 INPUT → 사용자가 놓은 실제 층 → OUTPUT 라벨을 공용 결과 에셋에 기록한다. Conv2D의 필터 수·커널 크기, Dense의 노드 수도 표시한다. 기존 학습 엔진·모델 JSON·파일 저장 형식은 변경하지 않는다. 새 오프닝에서 결과를 초기화한다. 엔딩만 단독 실행하여 결과가 없으면 `구조 예시 · 설계실 결과 없음`을 명시한다. 데이터는 디스크에 저장하지 않는다.

오프닝에서 공장으로 실제 비동기 전환할 때 발견한 기존 오류도 수정했다. `FactoryEnvironmentManager`의 `SetActiveScene` 요청을 아직 로딩 중인 `OnEnable`에서 로딩 완료 후 `Start`로 옮겼다. 학습 단계·이동 코드는 변경하지 않았다. 음원 정리는 씬 루트 해제 순서에 따라 AudioSource가 먼저 파괴되는 경우를 처리한다.

## 슬롯 편집

`Assets/Data/OpeningEnding/CinematicText.asset` Inspector에서 수정한다.

- Opening / Ending: Start, End, Text와 선택적 Voice. `{C:강조}`와 `{A:강조}`는 각각 Cyan/Amber이다. 오프닝은 35초, 엔딩은 20초다. 음성은 각 줄의 시간 안에 맞춘다.
- PostOffice / PostOfficeStamp / PostOfficeEnvelope: 미국 우체국 전경, 도장 동작 클립, 인식 표시가 있는 봉투 사진. OpeningCrowd와 OpeningScore는 오프닝의 군중 소리와 음악이다.
- Montage: `END_1998`~`END_2026` 8개 슬롯. 기본 Still은 하나의 사진 아틀라스를 UV로 나누어 쓰며 원본 비율을 유지한다. Video를 넣으면 전체 영상 UV로 재생한다.
- Source / AudioCredits: 영상·음성·음악을 교체할 때 실제 출처와 사용 조건을 함께 적고 `docs/ASSET_CREDITS.md`도 갱신한다.
- Type / Hit / BigHit / Rewind / Stamp / Beep / Pad: 선택적 AudioClip이다. 비어 있으면 해당 소리는 재생하지 않는다.

오프닝 내레이션은 사용자 녹화에서 대사별로 추출했다. 군중 소리는 우체국 사진이 나타날 때 파일의 0초부터 재생한다. 음악은 시작 버튼을 누른 뒤 재생하며 되감기와 도장 구간에 작아진다. 도장이 닿는 순간 기존 ArchiveStamp 효과음을 재생한다. 엔딩은 기존 연출을 사용한다.

씬을 다시 만들 때는 `Tools > Factory > Create Documentary Opening and Ending`을 실행한다. 기존 데이터 에셋의 문구·미디어 설정은 유지하며, 새 두 씬의 수동 배치는 재생성된다. 기존 다른 씬은 다시 생성하지 않는다.

## 렌더링·검증 범위

픽셀·봉투 등의 재질은 GPU instancing을 허용하며 영상 플레이어는 한 개만 사용한다. 포스트프로세싱 블룸·실시간 그림자·카메라 흔들기를 추가하지 않는다. 라이트맵 베이크 대신 단일 방향광과 환경광을 사용한다.

Editor D3D11 배치 Play에서 주요 시점의 실제 카메라 렌더를 저장하고, 자연 재생·공장 이동·엔딩·가상 XR 트리거·정적 구간·카메라 유지·모델 결과 표시를 검증한다. Quest 실기기 스테레오, 실제 손 컨트롤러 진동, 헤드폰 청감, APK와 72fps는 이 검증과 별개다. 임시 검증 코드는 완료 후 삭제한다.

폴더 정리 이전의 검증 결과(2026-11-09): 위 기본 경로의 Play 검증 PASS ALL, 렌더 22장 확인, 런타임 예외 없음. 임시 검증 코드 삭제 후 배치 컴파일 exit 0. 가상 XR 트리거와 샘플 모델 결과를 사용했으며 Quest 실기기·모델 재학습은 실행하지 않았다.

폴더 정리 후에는 Unity GUID와 씬·코드 참조를 정적으로 확인했다.
