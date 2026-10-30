# 공장 공간과 체험 연결

[FactoryEnvironment.unity](../client/Assets/Scenes/FactoryEnvironment.unity)는 정문 앞마당, 튜토리얼실, 세 체험실, 연결 복도와 후문 하역 공간으로 구성한다. 중앙 복도를 공유하던 이전 구조를 순차적으로 통과하는 방 구조로 교체했다. 공장을 직접 실행하면 정문 앞에서 시작한다.

이 씬은 공간과 방별 진행을 담당한다. 기존 FactoryIntro·FactoryEnding 씬과 연출 에셋은 수정하지 않았다. 모델 제작을 완료하고 후문 진입 영역에 들어오면 기존 FactoryEnding을 Single로 로드한다. 정문·후문용 새로운 연출이나 오프닝용 외관 우선 로딩은 이 공간 씬에 구현되어 있지 않다. 단독 실행에서는 공장 배경 전체를 로드하고 학습 콘텐츠만 필요할 때 Additive로 로드한다.

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

동쪽 입구에서 1-1을 시작한다. 픽셀·RGB 관찰과 R·G 계산 학습을 마치고 다음 공간으로를 누른 뒤 합성곱 입구 양피지를 수집하면 CNNPixelLabManager.ScreenCompleted를 받아 내부 문과 왼쪽 공간까지의 안내선을 연다. 카메라가 서쪽 진입 영역(X=-21.4보다 안쪽)에 들어오면 StartFactoryConvolution을 요청한다. B 퀴즈용 합성곱 씬이 준비·활성화되어 ConvolutionStarted가 전달되면 내부 문을 닫고 안내선을 숨긴다. 로드 실패 시 스크린과 열린 통로를 유지하고 3초 뒤 재시도한다. 최종 ConvolutionCompleted 판정 후 북쪽 출구로 ROOM #2에 진행하는 조건은 유지한다.

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

기준점은 벽 표면이 아니라 콘텐츠 루트의 위치다. 원래 체험의 자식 로컬 좌표를 유지하며, FactorySceneLoadController가 기준점의 위치·회전·배율을 루트에 적용한다. ROOM #3 기준점만 균일 배율 1.5이고 위치 (23, 0, 12)와 Y 회전 90°는 유지한다. 다른 기준점은 배율 1이다. 스크린 보드는 로컬 Z=1.8에 있어 중심 X=-19.4, Z 범위 -6.8~0.4에 표시된다. 중앙 벽 동쪽 면 X=-19.84와 약 0.44m 떨어지고 아치 개구부를 비킨다. 보드의 배율만 2→3으로 바꾸어 화면은 4.8×2.7m에서 7.2×4.05m로 1.5배 확대했다. 보드 중심 높이를 3.125m로 맞춰 기존 하단 높이 1.1m와 조작 UI를 유지한다. 필터 루트만 1.5배로 맞춰 보드 선택 프레임·3×3 표시의 비율을 유지하며, 컨볼루션·다른 방·문·경로는 그대로다. 특징 맵은 기존 로컬 Z≈3 위치를 사용한다. ROOM #1 내부 경로는 스크린 앞에서 동쪽 X=-13.6으로 빠져 RGB 전개 범위 바깥을 따라 남쪽으로 이동한 뒤 Z=-8.8에서 아치를 향하도록 배치한다. 실제 표시와 사용자 잡기·확대 범위는 Unity 실행 검토 전이다.

튜토리얼 안내 기준점은 (0, 0, -1.5), 안내 패널은 그 앞 3m·높이 1.65m에 표시한다. 정문 시작점은 (0, 0, -17)이다. 콘텐츠 로드·전환 시 플레이어 위치와 방향을 바꾸지 않는다.

## 문과 안내선

튜토리얼실과 세 체험실에 각각 입구·출구 셔터가 있고, 후문 공간에는 입구 셔터가 있다. ROOM #1 내부 아치 셔터를 포함해 총 10개다. 셔터 패널은 Collider와 함께 1.6초 동안 위아래로 3.3m 이동한다. 시작할 때는 튜토리얼실 입구만 연다.

1. 현재 목적지까지 고정된 청록색 바닥선과 화살표를 표시한다.
2. 카메라가 방 경계에서 1.2m 안쪽의 진입 영역에 들어오면 입구를 닫고 경로를 숨긴다.
3. 체험 중에는 입구·출구가 모두 닫힌다. 셔터가 닫히는 동안 플레이어가 문턱으로 되돌아오면 패널 이동을 보류한다.
4. 현재 체험이 완료되면 다음 콘텐츠를 비활성 상태로 준비한다. 준비 후에도 현재 체험의 완료 조건을 다시 확인한다.
5. 준비가 끝나면 현재 방 출구와 다음 방 입구를 열고, 다음 방까지의 안내선을 표시한다.
6. 다음 방에 들어오면 입구를 닫고 준비한 콘텐츠를 활성화한다. 이전 체험의 잡은 물체를 해제하고 이전 씬을 정리한다.
7. 후문에 진입하면 입구와 안내선을 정리하고 잡은 학습 물체를 해제한 뒤 기존 FactoryEnding을 Single로 연다. Single 전환이 공장과 학습 씬을 정리하며 로더는 중복 언로드를 하지 않는다. 전환 요청 실패 시 기존 공간·체험을 유지하고 후문 입구와 경로를 다시 표시해 재시도한다.

스틱 이동은 공용 리그의 기존 CharacterController와 벽·셔터 Collider를 사용한다. 에디터 FPS의 WASD는 시뮬레이터의 몸 추적 위치를 직접 바꾸므로 스틱 이동의 충돌 확인을 대신하지 않는다.

출구 개방은 방문 단위로 유지된다. 개방 후 기존 체험의 초기화 버튼을 눌러도 출구를 다시 닫지 않는다. 이전 방을 다시 실행하는 흐름은 없다. 경로는 사용자 위치에 따라 재탐색하지 않는다.

바닥선은 폭 0.12m·두께 0.012m·중심 Y=0.025m를 유지한다. 화살촉은 직전 크기의 0.75배로 줄여 날개 폭 0.0675m, 뒤쪽 길이 0.75m·좌우 폭 0.375m(각 날개 약 0.839m)다. 각 구간 시작에서 1.6m 뒤에 첫 화살표를 두고, 직전 간격 5.5m의 1.5배인 8.25m 간격으로 배치한다. 현재 목적지 경로 5개의 화살촉은 19개이고 ROOM #1 내부 경로에 4개가 더 있다. FactoryRoute.mat의 청록색 (0.08, 1, 0.8)과 그림자·프로브 설정은 유지한다. ROOM #1 분리에 필요한 경유점·내부 경로 표시만 추가 조정했다.

## 기존 완료 조건

| 공간 | 판정 |
| --- | --- |
| 튜토리얼 | FactoryTutorialManager.IsComplete. 기존 안내 마치기·건너뛰기 유지 |
| ROOM #1 | CNNPixelLabManager.ConvolutionCompleted |
| ROOM #2 | CNNFeatureMapLabManager.ReadyForModelDesign |
| ROOM #3 | CNNModelDesignLabManager.ReadyForFactoryExit |

완료 조건 자체는 변경하지 않았다. 특징 맵의 기존 마지막 단계 건너뛰기도 완료로 인정한다. 모델 제작은 정상 학습 결과 표시와 작업 종료가 필요하다. 별도의 0~100% 계산이나 태블릿의 목표 진행률 연결은 추가하지 않았다.

## 파일과 책임

| 파일 | 현재 책임 |
| --- | --- |
| [FactoryLearningFlowManager](../client/Assets/Scripts/FactoryEnvironment/FactoryLearningFlowManager.cs) | 방 순서, 기존 학습 완료 판정 조회, 콘텐츠 준비·확정과 셔터·경로 표시의 실행 순서 |
| [FactoryNavigationData](../client/Assets/Scripts/FactoryEnvironment/FactoryNavigationData.cs) | 정문·튜토리얼 기준점, 목적지 ID, 진입 영역, 경로, 학습 씬 이름, 콘텐츠 기준점, 입구·출구 참조 |
| [FactorySceneLoadController](../client/Assets/Scripts/FactoryEnvironment/FactorySceneLoadController.cs) | 튜토리얼과 학습 씬의 Additive 로드, 준비 중 비활성화, 배치·Rigidbody 동기화, 확정·언로드, 기존 FactoryEnding의 Single 전환 |
| [FactoryShutterController](../client/Assets/Scripts/FactoryEnvironment/FactoryShutterController.cs) | 셔터 패널과 Collider의 상하 이동, 닫힐 때 문턱의 플레이어 확인 |
| [FactoryAreaEntryController](../client/Assets/Scripts/FactoryEnvironment/FactoryAreaEntryController.cs) | 카메라 위치가 방 진입 BoxCollider 내부인지 로컬 좌표로 판정 |
| [FactoryRouteGuideController](../client/Assets/Scripts/FactoryEnvironment/FactoryRouteGuideController.cs) | 목적지별 5개 고정 경로와 ROOM #1 내부 경로의 표시 전환 |
| [FactoryPlayerController](../client/Assets/Scripts/FactoryEnvironment/FactoryPlayerController.cs) | 정문 최초 배치, XR 리그·ray 연결, 언로드할 씬의 선택 해제 |
| [FactoryMapController](../client/Assets/Scripts/FactoryEnvironment/FactoryMapController.cs) | 공장 바닥·벽을 읽어 새 구역 이름으로 TabletMapData 게시 |

공장 Manager는 같은 루트의 Player·SceneLoad·Navigation을 사용하고 하위 경로 Controller를 연결한다. 개별 학습의 입력·계산·완료 상태는 해당 학습 Manager가 소유한다. 셔터 Controller는 공장 Manager나 학습 컴포넌트를 탐색하지 않는다.

## 로딩 수명

다음 학습 씬의 sceneLoaded 시점에 모든 루트를 비활성화해 Start 이전에 배치한다. 기존 체험은 다음 방에 들어갈 때까지 유지한다. 준비된 콘텐츠는 기준점의 위치·회전·배율을 적용하고 공장 조명을 사용하도록 자체 조명을 끈다. Rigidbody의 위치·회전도 함께 맞춘다.

공장은 CNNPixelLab 관리용 씬만 참조한다. CNNScreenLab·CNNConvolutionLab의 로드와 선택 데이터 전달은 기존 CNNPixelLabManager·PixelLabSceneLoadController가 담당한다. 공장은 관리용 루트를 활성화하기 전에 ConfigureFactoryConvolution(Pose)으로 1-2 기준점을 지정한다. ScreenCompleted·ConvolutionStarted 이벤트를 활성 기간에 구독·해제하고 WaitingForFactoryConvolution 상태에서 서쪽 공간 진입 시 StartFactoryConvolution을 요청한다. 내부 체험 Manager를 직접 참조하지 않는다. 최종 ConvolutionCompleted를 읽고 다음 방 진입 시 EndSession을 기다린다. 학습 로드 실패 시 기존 체험과 닫힌 출구를 유지하고 오류를 기록한 뒤 3초 간격으로 재시도한다.

씬 로딩 중 공장 오브젝트가 제거되면 로드 완료 콜백이 뒤늦게 생긴 씬을 정리한다. 이미 로드된 튜토리얼·현재 체험·준비 중인 체험은 로더의 종료 처리에서 언로드한다.

## 지도·양피지

미니맵은 기존 TabletMapData와 TabletMinimapController를 사용한다. 이름이 structural slab으로 끝나는 바닥 Collider와 concrete를 포함하는 벽 Collider의 XZ 영역을 읽는다. 셔터 상태를 동적으로 지도에 그리는 기능은 없다.

양피지는 클릭 즉시 수집하고 열람창을 연다. 문별 배분과 위치 안내는 ParchmentLesson.Location을 사용한다. ROOM #1~#3 입구 및 ROOM #1 내부문은 기존 진행 조건에 해당 위치의 전체 수집 조건을 추가한다. 출구는 기존 조건을 유지한다. 현재 배분은 None이며 실제 배치는 임시 위치에 남아 있다. 자세한 연결은 [공용 양피지](parchment.md)를 따른다.

## 씬·에셋 관리

씬 경로와 .meta GUID는 유지한다. 공장 공간·진행 코드는 Scripts/FactoryEnvironment, 재질과 결합 메시는 각각 Materials/FactoryEnvironment, Meshes/FactoryEnvironment에 있다. 공용 양피지 코드는 Scripts/Core/Parchment에 두며, 기존 ParchmentReaderController와 ParchmentLesson은 .meta GUID를 유지해 해당 폴더로 이동했다. 공장 배치와 공간 전용 에셋의 경로는 유지한다.

EscapeLayout_*.asset 74개는 이 공간의 기본 도형을 부모·재질별로 결합한 Unity 메시다. 동일한 셔터 형태는 같은 메시 에셋을 참조한다. 구조물 Collider는 별도 오브젝트로 보존하고, 움직이는 패널의 메시와 Collider는 같은 이동 루트 아래에 둔다. ROOM #1 벽·아치 프레임·셔터·셔터 슬랫·내부 경로 메시 5개와 새 가구 메시 33개를 사용한다. 이전 장식 22개와 전기함 전용 20개 Renderer를 제거해 씬에는 MeshRenderer 174개와 간판용 Canvas·uGUI Text 각각 22개가 저장되어 있다. 실제 Quest 성능 측정값은 아니다.

기존 FactoryDecoration_*.asset는 삭제하지 않았으며 새 씬에서는 참조하지 않는다. 기존 ParchmentSceneSetup은 이전 공장 계층·좌표를 대상으로 하는 도구이므로 새 공간의 배치 도구가 아니다.

## 확인 범위

2026-10-30: 최신 원격 main의 픽셀 체험 분리 구조를 기준으로 씬 직렬화 참조, 방·통로·셔터의 배치 값, 콘텐츠 루트 회전과 로딩·완료 판정 연결을 정적으로 검토했다. Unity 실행·Play·컴파일·테스트·빌드·프로젝트 복사본 생성은 수행하지 않았다. 실제 렌더링, 간판 크기, XR 이동·충돌·조작, 콘텐츠 펼침·확대 시 벽 여유와 Quest 성능은 실행 확인 전이다.

간판 깊이 판정·금속 간판판, 이전 바닥 경로 디자인, 후문에서 기존 엔딩 씬을 여는 세 항목의 후속 수정은 소스·씬·메시·재질 참조를 정적으로 검토했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

화살촉 간격·길이와 세 방의 벽 쪽 장식 추가는 기존 씬 오브젝트·체험 기준점·문·경로 표시 연결을 정적으로 대조했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

ROOM #1 벽·아치 셔터·두 체험 기준점과 Screen/Convolution 전환 신호, 화살촉 크기·간격, ROOM #3 기준점의 1.5배 배율은 소스·씬·메시 참조와 기존 오브젝트별 변경 범위를 정적으로 대조했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

캐비닛·선반·박스·연구 데스크 교체는 씬의 장식 참조와 가구 표시 경계, 체험·문·경로의 위치를 정적으로 대조했다. 기존 체험·문·경로·건축 오브젝트와 스크립트는 수정하지 않았다. 이 교체에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

공장 코드의 합성곱 진입문은 `FactoryNavigationData.ConvolutionEntranceDoor`로 참조한다. FlowManager의 `_convolutionArea`·`_convolutionAnchor`는 합성곱 구역과 콘텐츠 기준점이며, RouteGuide의 `SetConvolutionRouteVisible`은 해당 진입 경로를 표시한다. 화면에 표시하는 ROOM 번호와 씬 오브젝트 이름은 유지한다.
