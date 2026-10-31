# 공장 공간과 체험 연결

[FactoryEnvironment.unity](../client/Assets/Scenes/FactoryEnvironment.unity)는 정문 앞마당, 튜토리얼실, 세 체험실, 연결 복도와 후문 하역 공간으로 구성한다. 중앙 복도를 공유하던 이전 구조를 순차적으로 통과하는 방 구조로 교체했다. 공장을 직접 실행하면 정문 앞에서 시작한다.

공장 공간은 FactoryEnvironmentManager가, 학습 단계·누적 완료·전환 순서·모듈 이벤트 연결은 Core의 LearningSessionManager가 담당한다. 실제 씬 로드·배치·활성화·언로드는 FactoryEnvironment의 FactorySceneLoadController가 담당한다. Core 세션은 공장 씬에 배치되어 그 씬과 함께 종료되며 영속 객체가 아니다. 기존 FactoryIntro·FactoryEnding 씬과 연출 에셋은 유지한다. 모델 제작을 마치고 후문 진입 영역에 들어오면 기존 FactoryEnding을 Single로 로드한다. 공장을 단독 실행하면 배경 전체를 로드하고 학습 콘텐츠는 필요할 때 Additive로 로드한다.

## 배치

아래의 위쪽은 Unity +Z, 오른쪽은 +X다. 크기는 바닥의 X×Z, 단위는 m다.

| 공간 | 중심 X, Z | 크기 | 출입 |
| --- | --- | --- | --- |
| 정문 앞마당 | 0, -14 | 20×12 | 북쪽 복도1 |
| 튜토리얼실 | 0, 0 | 18×8 | 남쪽 입구 → 서쪽 출구 |
| ROOM #1 · Pixel Lab | -20, -4 | 16×16 | 동쪽 입구 → 북쪽 출구 |
| ROOM #2 · Feature Map Lab | -2, 12 | 20×12 | 서쪽 입구 → 동쪽 출구 |
| ROOM #3 · Model Design Lab | 20, 12 | 18×14 | 서쪽 입구 → 북쪽 출구 |
| 후문 하역 공간 | -5, 35 | 30×18 | 동쪽 입구 |

동선은 정문 → 복도1 → 튜토리얼실 → 짧은 연결 통로 → ROOM #1 → ㄱ자 복도2 → ROOM #2 → 복도3 → ROOM #3 → 꺾인 복도4 → 후문이다. 복도는 폭 3.6m, 셔터 통과 폭은 3.2m다. 방과 복도는 바닥 상단 Y=0, 벽 높이 6.8m, 천장 하단 Y=6.8m로 닫혀 있다. 정문·후문 앞마당은 지붕 없는 공간이며 출입구에 캐노피를 둔다.

거친 콘크리트·녹슨 철제 보·금속 천장·배관·노란 안전 표식을 기존 재질로 구성한다. 기존 독립 전기함 5개와 P1·P2·P3 작업대·랙은 씬에서 제거하고 아래의 캐비닛·선반·박스·데스크로 교체했다. 전기함 옆 수직 배관은 건축 요소로 보존한다. 간판은 기존 공장과 같은 World Space Canvas·uGUI Text로 금속 간판판 앞면에 저장한다. Noto Sans CJK KR 폰트와 기본 UI 재질을 사용하며, 벽과 간판판의 깊이 판정을 받는다. TextMesh의 기본 폰트 재질은 사용하지 않는다. 문 간판의 기존 프레임·인쇄면을 유지하고 정문·후문 큰 간판에도 금속 프레임과 어두운 인쇄면을 배치한다.

## ROOM #1 내부 분리

ROOM #1의 외곽 16×16m는 유지하고 X=-20에 남북 방향 콘크리트 벽을 두어 동쪽의 체험 1-1(CNNScreenLab)과 서쪽의 체험 1-2(CNNConvolutionLab) 공간을 명목상 8×16m씩 나눈다. 통로 중심은 (-20, 0, -8.8)이며 폭 3.2m·최고 높이 3.2m의 아치다. 벽은 천장까지 이어지고 아치형 금속 셔터가 통로를 닫는다. 같은 FactoryShutterController로 기존 문과 동일하게 1.6초 동안 3.3m 상하 이동한다. 벽·아치 상부와 셔터에 충돌 영역을 저장한다.

동쪽 입구에서 PixelLab의 픽셀 관찰을 시작한다. RGB 관찰을 마치고 다음으로를 누르면 CNNPixelLabManager가 선택 데이터를 보관하고 내부 진행을 시작한다. 문서 01 수집을 확인해 Factory의 공개 공간 제어 기능으로 내부 문·안내선을 열고, 카메라가 서쪽 진입 영역(X=-21.4보다 안쪽)에 들어오면 합성곱 씬으로 교체한다. 성공 후 내부 문·안내선을 닫으며 로드 실패는 PixelLab 내부에서 3초 뒤 재시도한다. Core의 현재 단계는 이 과정 내내 PixelLab이다. 합성곱까지 완료하면 PixelLab의 ProgressChanged와 ExitAvailabilityChanged를 알리고 Core 세션이 다음 특징 맵 체험을 준비한다.

ROOM #1 진입 경로는 오른쪽 체험으로 끝나고 다음 방 경로는 왼쪽에서 시작하며, 두 체험 사이에는 남쪽 아치를 지나는 별도 경로를 둔다.

## 공장 가구 배치

사용자 배치도의 빨강은 캐비닛, 보라는 박스, 민트는 선반, 노랑은 연구 데스크다. 초록 체험 위치와 기존 벽·문·경로는 이동하지 않는다. 이전 P1·P2·P3 장식과 전기함은 씬에서 제거했다.

| 공간 | 가구와 배치 | 수량 | 최대 높이 |
| --- | --- | --- | --- |
| 튜토리얼 | 동쪽 벽의 작은 수납칸 캐비닛, 북서·남서 벽 철제 선반 | 캐비닛 6칸(문 18개), 선반 2개 | 캐비닛 2.2m, 선반 2.55m |
| ROOM #1 | 북서 벽 캐비닛 2칸, 북동 단독 캐비닛, 동쪽 구획 북벽 선반, 동남쪽 L 배치 데스크 | 캐비닛 3칸, 선반 1개, 데스크 2개 | 데스크·모니터 1.71m |
| ROOM #2 | 북서에 떨어진 박스, 남벽 왼쪽 데스크·중앙 선반·오른쪽 캐비닛 | 박스 5개, 데스크 1개, 선반 1개, 캐비닛 3칸 | 박스 약 0.71m |
| ROOM #3 | 북서 벽 캐비닛, 북동 적층 박스 | 캐비닛 3칸, 박스 6개(3·2·1층) | 적층 1.75m |

캐비닛은 녹슨 몸체·도장 문·통풍구·손잡이·명찰판, 선반은 철제 기둥·가로 받침·후면 교차 보강대와 수납 상자로 구성한다. 데스크에는 서랍·컴퓨터 본체·모니터·키보드·문서를 배치한다. 모니터 표시는 기본 도형으로 만든 고정 장식이며 별도 체험이나 UI 입력을 추가하지 않는다.

새 가구는 방·재질별 33개 결합 메시(FactoryFurnishing_*)와 기존 재질 및 새 재질 5개(FurnishingLockerCream·FurnishingLockerOlive·FurnishingCardboard·FurnishingDeskTop·FurnishingDisplay)를 사용한다. 새 메시·재질은 기존 종류별 FactoryEnvironment 폴더에 둔다. 캐비닛·선반·데스크·개별 박스·적층 묶음에 고정 BoxCollider 28개를 두고 Rigidbody·XR 잡기·광원·동작 스크립트는 추가하지 않는다.

ROOM #1 동쪽 데스크는 X[-13.155,-12.405], Z[-10,-6.5]이며 내부 경로 중심 X=-13.6과 0.445m 떨어진다. 남쪽 데스크는 Z[-11.4,-10.5]로 아치 진입 경로 Z=-8.8을 비운다. ROOM #3 적층 박스는 X[25.992,28.208], Z[16.838,18.454]이며 현재 모델 표시의 북쪽 끝 Z≈16.04보다 약 0.80m 북쪽에 있다. 북쪽 출구 (22,19)와 프레임 구간도 비운다. 가구별 표시 경계끼리는 겹치지 않으며, 선반 속 상자와 데스크 위 부품·적층 상자는 각 가구를 이루는 구성 요소다. 사용자가 체험물을 직접 옮긴 이후의 임의 위치까지 제한하는 기능은 추가하지 않았다.

## 콘텐츠 기준점

| 체험 | 루트 위치 X, Y, Z | Y 회전 | 표시 방향 |
| --- | --- | --- | --- |
| ROOM #1 · 1-1 스크린 | -17.6, 0, -3.2 | -90° | 동쪽 공간에서 중앙 벽 방향으로 관찰 |
| ROOM #1 · 1-2 컨볼루션 | -23, 0, -1.5 | -90° | 서쪽 공간에서 서쪽 벽 방향으로 관찰 |
| ROOM #2 | -2, 0, 11 | 0° | 북쪽에 배치하고 남쪽의 방 중앙에서 관찰 |
| ROOM #3 | 23, 0, 12 | 90° | 동쪽에 배치하고 서쪽의 방 중앙에서 관찰 |

기준점은 벽 표면이 아니라 콘텐츠 루트의 위치다. 원래 체험의 자식 로컬 좌표를 유지하며, FactorySceneLoadController가 FactoryNavigationData의 ContentAnchor에서 위치·회전·배율을 직접 읽어 콘텐츠 루트에 적용한다. ROOM #3 기준점만 균일 배율 1.5이고 위치 (23, 0, 12)와 Y 회전 90°는 유지한다. 다른 기준점은 배율 1이다. 스크린 보드는 로컬 Z=1.8에 있어 중심 X=-19.4, Z 범위 -6.8~0.4에 표시된다. 중앙 벽 동쪽 면 X=-19.84와 약 0.44m 떨어지고 아치 개구부를 비킨다. 보드의 배율만 2→3으로 바꾸어 화면은 4.8×2.7m에서 7.2×4.05m로 1.5배 확대했다. 보드 중심 높이를 3.125m로 맞춰 기존 하단 높이 1.1m와 조작 UI를 유지한다. 필터 루트만 1.5배로 맞춰 보드 선택 프레임·3×3 표시의 비율을 유지하며, 컨볼루션·다른 방·문·경로는 그대로다. 특징 맵은 기존 로컬 Z≈3 위치를 사용한다. ROOM #1 내부 경로는 스크린 앞에서 동쪽 X=-13.6으로 빠져 RGB 전개 범위 바깥을 따라 남쪽으로 이동한 뒤 Z=-8.8에서 아치를 향하도록 배치한다. 실제 표시와 사용자 잡기·확대 범위는 Unity 실행 검토 전이다.

튜토리얼 안내 기준점은 (0, 0, -1.5), 안내 패널은 그 앞 3m·높이 1.65m에 표시한다. 정문 시작점은 (0, 0, -17)이다. 걸어서 진입하는 콘텐츠 로드·전환은 플레이어 위치와 방향을 바꾸지 않는다. 태블릿의 단계 이동은 별도 도착점으로 이동한다.

## 문과 안내선

튜토리얼실과 세 체험실에 각각 입구·출구 셔터가 있고, 후문 공간에는 입구 셔터가 있다. ROOM #1 내부 아치 셔터를 포함해 총 10개다. 셔터 패널은 Collider와 함께 1.6초 동안 위아래로 3.3m 이동한다. 시작할 때는 튜토리얼실 입구만 연다.

1. 현재 목적지까지 고정된 청록색 바닥선과 화살표를 표시한다.
2. 카메라가 방 경계에서 1.2m 안쪽의 진입 영역에 들어오면 입구를 닫고 경로를 숨긴다.
3. 체험 중에는 입구·출구가 모두 닫힌다. 셔터가 닫히는 동안 플레이어가 문턱으로 되돌아오면 패널 이동을 보류한다.
4. 현재 체험에서 진행 가능 상태를 통지받으면 Core 세션이 다음 콘텐츠를 비활성 상태로 준비한다. 준비 후에도 최신 이벤트로 받은 진행 가능 상태를 확인한다.
5. 준비가 끝나면 현재 방 출구를 열고 다음 방까지의 안내선을 표시한다. 다음 학습 방의 입구는 해당 위치의 양피지 전체 수집 조건도 만족해야 열린다. 후문 입구는 학습 완료 후 준비가 끝나면 열린다.
6. 다음 방에 들어오면 입구를 닫고 준비한 콘텐츠를 활성화한다. 이전 체험의 잡은 물체를 해제하고 이전 씬을 정리한다.
7. 후문에 진입하면 입구와 안내선을 정리하고 잡은 학습 물체를 해제한 뒤 기존 FactoryEnding을 Single로 연다. Single 전환이 공장과 학습 씬을 정리하며 로더는 중복 언로드를 하지 않는다. 전환 요청 실패 시 기존 공간·체험을 유지하고 후문 입구와 경로를 다시 표시해 재시도한다.

스틱 이동은 공용 리그의 기존 CharacterController와 벽·셔터 Collider를 사용한다. 에디터 FPS의 WASD는 시뮬레이터의 몸 추적 위치를 직접 바꾸므로 스틱 이동의 충돌 확인을 대신하지 않는다.

출구 개방은 방문 단위로 유지된다. 개방 후 기존 체험의 초기화 버튼을 눌러도 출구를 다시 닫지 않는다. 태블릿의 학습 단계 앱으로 이전 체험도 새로 시작할 수 있으며 완료 기록은 유지한다. 경로는 사용자 위치에 따라 재탐색하지 않는다.

바닥선은 폭 0.12m·두께 0.012m·중심 Y=0.025m를 유지한다. 화살촉은 직전 크기의 0.75배로 줄여 날개 폭 0.0675m, 뒤쪽 길이 0.75m·좌우 폭 0.375m(각 날개 약 0.839m)다. 각 구간 시작에서 1.6m 뒤에 첫 화살표를 두고, 직전 간격 5.5m의 1.5배인 8.25m 간격으로 배치한다. 현재 목적지 경로 5개의 화살촉은 19개이고 ROOM #1 내부 경로에 4개가 더 있다. FactoryRoute.mat의 청록색 (0.08, 1, 0.8)과 그림자·프로브 설정은 유지한다. ROOM #1 분리에 필요한 경유점·내부 경로 표시만 추가 조정했다.

## 완료 사실과 진행 가능 상태

| 학습 단계 | 현재 진행 가능 조건 |
| --- | --- |
| Tutorial | 11개 조작 안내와 미션 확인을 마치고 입력 정리가 끝남. 건너뛰기 버튼은 없음 |
| PixelLab | 픽셀 관찰에 이어 합성곱까지 완료했고 내부 전환·종료 중이 아닌 활성 상태 |
| FeatureMap | 초기화 완료·마지막 단계·입력 수정 1회 이상 또는 건너뛰기·잡은 대상 없음 |
| ModelDesign | 정상 학습 결과 반영·작업 종료·활성 상태 |

세 체험은 ILearningExperience의 Progress(0~1), CanExit, ProgressChanged(float), ExitAvailabilityChanged(bool), EndSession()으로 Core 세션과 통신한다. LearningProgress는 체험별 최대 도달 진행률을 보관하며 1에 도달하면 완료를 기록한다. 전체 진행률은 세 체험의 동일 비중 평균이다. 재방문·초기화로 누적 기록은 감소하지 않는다. 튜토리얼은 기존 완료 이벤트를 사용하며 전체 진행률에서 제외한다.

학습 순서와 이름, 씬 이름, 진행률 포함 여부, 입장 문서와 체험 소속 문서는 Core LearningStageCatalog에 정의한다. LearningStageId 하나로 학습 체험과 공장 목적지를 식별하며 별도 RoomId는 사용하지 않는다. 값은 None 0, PixelLab 1, FeatureMap 2, ModelDesign 3, Exit 4, Tutorial 5다. 스크린과 합성곱은 PixelLab 내부 구현이며 외부에는 하나의 완료 기록만 제공한다. 진행률 대상은 PixelLab·FeatureMap·ModelDesign 세 체험이고 완료 수에 따라 0%, 33%, 67%, 100%로 표시한다. Tutorial과 Exit는 진행률에 포함하지 않는다.

누적 완료는 공장 씬 수명의 LearningSessionManager가 소유한 LearningProgress에만 저장한다. 태블릿은 읽기 전용 LearningProgressSnapshot을 표시한다. 공장 세션 안의 재방문·체험 초기화·태블릿 비활성화는 기록을 지우지 않으며, 공장 씬이 종료되면 세션과 학습 기록을 폐기한다. 재진입은 새 세션이다. 기존 공용 양피지의 수집 기록 수명은 별개다.

## 파일과 책임

| 파일 | 현재 책임 |
| --- | --- |
| [LearningSessionManager](../client/Assets/Scripts/Core/Learning/LearningSessionManager.cs) | 공장 씬 수명의 학습 세션. 단계·누적 완료·진행 가능 상태, 씬 전환 순서·재시도, 체험·공장·태블릿·양피지 이벤트 연결 |
| [FactorySceneLoadController](../client/Assets/Scripts/FactoryEnvironment/FactorySceneLoadController.cs) | 학습 씬의 비동기 준비·활성화·취소·언로드와 엔딩 로드. 준비된 씬의 진입점 및 로딩 결과 소유 |
| [LearningStageCatalog](../client/Assets/Scripts/Core/Learning/LearningStageCatalog.cs) | 단계 순서·이름·씬 이름·진행률 포함 여부·입장 문서와 체험 소속 문서의 단일 정의 |
| [LearningProgress](../client/Assets/Scripts/Core/Learning/LearningProgress.cs) / [LearningProgressSnapshot](../client/Assets/Scripts/Core/Learning/LearningProgressSnapshot.cs) | 체험별 최대 진행률과 완료 기록 / 외부에 전달할 읽기 전용 상태 복사 |
| [FactoryEnvironmentManager](../client/Assets/Scripts/FactoryEnvironment/FactoryEnvironmentManager.cs) | 공장 공간 진입점. 영역·지도 이벤트, 문·경로·플레이어 제어, 콘텐츠 배치 값 제공. 체험 Manager·태블릿·양피지 기록을 직접 조율하지 않음 |
| [FactoryNavigationData](../client/Assets/Scripts/FactoryEnvironment/FactoryNavigationData.cs) | 공통 LearningStageId로 구분한 방의 진입 영역·경로·콘텐츠 기준점·입출구와 정문·튜토리얼 기준점 |
| [FactoryShutterController](../client/Assets/Scripts/FactoryEnvironment/FactoryShutterController.cs) | 셔터 패널과 Collider 이동, 문턱의 플레이어가 지나갈 때 닫힘 보류 |
| [FactoryAreaEntryController](../client/Assets/Scripts/FactoryEnvironment/FactoryAreaEntryController.cs) | 카메라 위치를 로컬 좌표로 판정하고 내부 여부가 달라질 때 PresenceChanged 발생 |
| [FactoryRouteGuideController](../client/Assets/Scripts/FactoryEnvironment/FactoryRouteGuideController.cs) | 방별 고정 경로와 ROOM #1 내부 경로 표시 |
| [FactoryPlayerController](../client/Assets/Scripts/FactoryEnvironment/FactoryPlayerController.cs) | 정문·단계 도착점으로 XR 리그 이동, ray 준비, 언로드할 씬의 잡은 물체 해제 |
| [FactoryMapController](../client/Assets/Scripts/FactoryEnvironment/FactoryMapController.cs) | 공장 바닥·벽·양피지 위치에서 TabletMapData 생성과 변경 통지 |

Core 세션은 각 모듈의 진입점을 연결한다. FactoryEnvironmentManager는 공장의 내부 Controller를 연결하며, 각 체험 Manager는 자기 체험의 입력·계산·하위 모듈을 조율한다. 개별 문·영역·경로 참조와 씬 배치는 공장에 남는다. LearningSessionManager는 Core/Learning에서 이벤트 연결과 학습 흐름을 조율하며, FactoryEnvironment의 FactorySceneLoadController에 실제 씬 로드를 요청한다. 로더를 이동하면서 스크립트 meta GUID와 씬의 참조를 보존했다.

## 로딩 수명

FactorySceneLoadController는 카탈로그의 씬 이름으로 콘텐츠를 준비하고 sceneLoaded에서 루트를 비활성화한다. Core 세션은 학습 단계 ID로 준비를 요청하고, 튜토리얼 초기화에 필요한 배치 값을 전달하며 체험 초기화와 이벤트 연결을 마친 후 활성화한다. 로더는 씬에 연결된 FactoryNavigationData의 ContentAnchor에서 위치·회전·배율을 직접 읽고 일반 학습 콘텐츠의 자체 조명을 끈다. 튜토리얼 루트는 원점·기본 회전·배율 1로 배치하고 자체 조명을 유지한다. 두 경우 모두 Rigidbody 위치·회전을 동기화한다. 기존 체험은 다음 방에 들어갈 때까지 유지한다.

CNNPixelLabManager가 공장 실행 시 FactoryEnvironmentManager의 공개 공간 기능과 ParchmentManager의 수집 조회를 직접 연결한다. 문·진입 영역·안내 경로는 공장 씬에 유지하며 내부 통로 개방 조건·진입 대기·Screen/Convolution 전환은 PixelLab이 조율한다. Learning은 통로 설정이나 내부 로딩 상태를 전달·조회하지 않는다. 초기 Screen 로딩 실패와 내부 전환 실패는 PixelLab에서 3초 뒤 재시도한다.

튜토리얼도 동일한 완료·진행 가능 이벤트를 사용한다. 로더가 IsComplete를 매 프레임 조회하며 기다리지 않는다. 영역 감지는 공장 내부 Controller가 수행하고 세션은 영역 변화 이벤트와 최초 상태를 사용한다. 진행 조건 변경·양피지 수집·영역 변화·로딩 종료에 따라 전환을 판단하며, 실패한 자연 진행은 3초 후 재시도한다. 재시도는 세션의 코루틴으로 관리하고 단계 이동·세션 종료 시 취소한다.

준비 중 진행 가능 상태가 사라지면 준비한 씬을 취소한다. 씬 교체 전에는 구독을 해제하고 각 체험의 종료와 잡은 물체 해제를 처리한다. 로딩 중 공장 씬이 종료되면 로더의 완료 콜백이 뒤늦게 로드된 씬을 정리한다. 엔딩의 Single 전환에서는 Unity의 씬 정리와 중복 언로드하지 않는다.

## 지도·양피지

미니맵 형상은 FactoryMapController가 생성한다. 이름이 structural slab으로 끝나는 바닥 Collider와 concrete를 포함하는 벽 Collider의 XZ 영역, 실제 Pickup 위치를 읽는다. FactoryEnvironmentManager가 MapChanged와 초기 MapData를 공개하고 Core 세션이 태블릿에 연결한다. 셔터 상태를 동적으로 지도에 그리는 기능은 없다.

양피지는 클릭 즉시 수집하고 열람창을 연다. 본문·페이지는 ParchmentCatalog에, 체험별 전체 문서 목록은 LearningStageCatalog의 ParchmentIds에 둔다. 바깥문 입장 조건은 EntranceParchmentIds만 사용한다. PixelLab은 전체 문서 {4, 0} 중 바깥문에서 4를 요구하고, 내부 문서 0은 PixelLab이 진행 중에 확인한다. 세션은 ParchmentManager.Collected와 HasCollectedAll로 바깥문 진입 조건을 판단하고 공장에 문 제어를 요청한다. 수집 기록은 ParchmentManager가 소유한다. 문서별 배분·지급·실제 위치는 [공용 양피지](parchment.md)를 따른다.

## 씬·에셋 관리

씬 경로와 .meta GUID는 유지한다. 학습 세션·공통 계약은 Scripts/Core/Learning, 씬 로더와 공장 공간 코드는 Scripts/FactoryEnvironment, 재질과 결합 메시는 각각 Materials/FactoryEnvironment, Meshes/FactoryEnvironment에 있다. 공용 양피지 코드는 Scripts/Core/Parchment에 두며, 기존 ParchmentReaderController와 ParchmentLesson은 .meta GUID를 유지해 해당 폴더로 이동했다. 공장 배치와 공간 전용 에셋의 경로는 유지한다.

EscapeLayout_*.asset 74개는 이 공간의 기본 도형을 부모·재질별로 결합한 Unity 메시다. 동일한 셔터 형태는 같은 메시 에셋을 참조한다. 구조물 Collider는 별도 오브젝트로 보존하고, 움직이는 패널의 메시와 Collider는 같은 이동 루트 아래에 둔다. ROOM #1 벽·아치 프레임·셔터·셔터 슬랫·내부 경로 메시 5개와 새 가구 메시 33개를 사용한다. 이전 장식 22개와 전기함 전용 20개 Renderer를 제거해 씬에는 MeshRenderer 174개와 간판용 Canvas·uGUI Text 각각 22개가 저장되어 있다. 실제 Quest 성능 측정값은 아니다.

기존 FactoryDecoration_*.asset는 삭제하지 않았으며 새 씬에서는 참조하지 않는다. 기존 ParchmentSceneSetup은 이전 공장 계층·좌표를 대상으로 하는 도구이므로 새 공간의 배치 도구가 아니다.

## 확인 범위

2026-10-30: 최신 원격 main의 픽셀 체험 분리 구조를 기준으로 씬 직렬화 참조, 방·통로·셔터의 배치 값, 콘텐츠 루트 회전과 로딩·완료 판정 연결을 정적으로 검토했다. Unity 실행·Play·컴파일·테스트·빌드·프로젝트 복사본 생성은 수행하지 않았다. 실제 렌더링, 간판 크기, XR 이동·충돌·조작, 콘텐츠 펼침·확대 시 벽 여유와 Quest 성능은 실행 확인 전이다.

간판 깊이 판정·금속 간판판, 이전 바닥 경로 디자인, 후문에서 기존 엔딩 씬을 여는 세 항목의 후속 수정은 소스·씬·메시·재질 참조를 정적으로 검토했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

화살촉 간격·길이와 세 방의 벽 쪽 장식 추가는 기존 씬 오브젝트·체험 기준점·문·경로 표시 연결을 정적으로 대조했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

ROOM #1 벽·아치 셔터·두 체험 기준점과 Screen/Convolution 전환 신호, 화살촉 크기·간격, ROOM #3 기준점의 1.5배 배율은 소스·씬·메시 참조와 기존 오브젝트별 변경 범위를 정적으로 대조했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

캐비닛·선반·박스·연구 데스크 교체는 씬의 장식 참조와 가구 표시 경계, 체험·문·경로의 위치를 정적으로 대조했다. 기존 체험·문·경로·건축 오브젝트와 스크립트는 수정하지 않았다. 이 교체에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

공장 코드의 합성곱 진입문은 FactoryNavigationData.ConvolutionEntranceDoor로 참조한다. 합성곱 영역·콘텐츠 기준점·학습 단계별 플레이어 도착점은 FactoryEnvironmentManager에 연결한다. RouteGuide의 SetConvolutionRouteVisible은 해당 진입 경로를 표시한다. 화면의 ROOM 번호와 기존 공간 오브젝트 배치는 유지한다.

## 태블릿 자유 이동

LearningSessionManager가 태블릿의 StageMoveRequested(LearningStageId)를 받아 선택한 단계까지의 ParchmentIds 문서를 카탈로그 순서대로 지급한 뒤 이동을 처리한다. 지급은 이동 요청 기준이며 이동 거절·실패·이전 단계 복귀에도 회수하지 않는다. 문서 지급은 체험 완료를 뜻하지 않는다.

이동 시 예약된 다음 체험과 재시도를 취소하고, PixelLab 내부 진행·중첩 씬의 종료를 기다린 뒤 현재 체험을 정리한다. 대상 체험을 준비·배치·연결·활성화하고 FactoryEnvironmentManager를 통해 문·안내선과 플레이어 위치를 갱신한다. 플레이어 이동은 실제 카메라 높이를 유지한다. 도착점은 Pixel Arrival(-15,0,-3.2 / Y -90°), Feature Map Arrival(-2,0,10 / Y 0°), Model Design Arrival(20,0,12 / Y 90°)이며 LearningStageId와 도착점 쌍으로 연결한다. 합성곱 전용 텔레포트 도착점은 사용하지 않는다.

태블릿 이동 대상은 PixelLab·FeatureMap·ModelDesign 세 체험이다. PixelLab은 항상 픽셀 관찰부터 시작하고 합성곱까지 내부에서 진행한다. 재방문은 새 체험이며 픽셀 선택·필터·모델 편집 중간 상태는 복원하지 않는다. 로딩 중에는 중복 전환을 시작하지 않고 실패 시 태블릿에 재선택 안내를 표시한다. 걸어서 입장하는 흐름은 바깥문 문서 조건과 PixelLab 내부 문서 조건을 구분해 적용한다.

태블릿 카드의 순서·요청 ID는 LearningStageCatalog.ProgressStages에 연결하고 고정 제목 Text는 기존 프리팹을 유지한다. 누적 완료와 진행률 계산은 Core가 맡고 TabletLearningController는 LearningProgressSnapshot과 이동 안내를 표시한다. 세션 연결 해제 시 빈 스냅샷을 전달한다. 공장 씬 재진입 때 학습 기록은 새로 시작하며 기존 ParchmentManager의 수집 기록 수명은 별도로 유지한다.

## Core 분리 확인 범위

이번 변경은 소스와 씬 직렬화 연결을 정적으로 검토했다. 위의 이전 작업 기록은 각 작업 당시의 확인 범위이며 이번 Core 분리의 실행 결과가 아니다. Unity 실행·Play·컴파일·테스트·빌드는 수행하지 않았다. 튜토리얼 완료, 내부 문 전환, 로드 실패 재시도, 자유 이동·재진입, 종료 중 비동기 정리는 실행 확인 전이다.
