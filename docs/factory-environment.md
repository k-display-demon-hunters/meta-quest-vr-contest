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

거친 콘크리트·녹슨 철제 보·금속 천장·배관·전기함·노란 안전 표식을 기존 재질로 구성한다. 전기함은 벽 쪽 서비스 공간에 두어 문 사이 동선과 체험 앞 공간을 비운다. 간판은 기존 공장과 같은 World Space Canvas·uGUI Text로 금속 간판판 앞면에 저장한다. Noto Sans CJK KR 폰트와 기본 UI 재질을 사용하며, 벽과 간판판의 깊이 판정을 받는다. TextMesh의 기본 폰트 재질은 사용하지 않는다. 문 간판의 기존 프레임·인쇄면을 유지하고 정문·후문 큰 간판에도 금속 프레임과 어두운 인쇄면을 배치한다.

## 콘텐츠 기준점

| 체험 | 루트 위치 X, Y, Z | Y 회전 | 표시 방향 |
| --- | --- | --- | --- |
| ROOM #1 | -22, 0, -4 | -90° | 서쪽에 배치하고 동쪽의 방 중앙에서 관찰 |
| ROOM #2 | -2, 0, 11 | 0° | 북쪽에 배치하고 남쪽의 방 중앙에서 관찰 |
| ROOM #3 | 23, 0, 12 | 90° | 동쪽에 배치하고 서쪽의 방 중앙에서 관찰 |

기준점은 벽 표면이 아니라 콘텐츠 루트의 위치다. 원래 체험의 자식 로컬 좌표를 유지하며, SceneLoadController가 루트의 위치와 회전을 함께 적용한다. 픽셀 보드와 특징 맵은 원래 로컬 Z≈3 위치를 사용하므로 각각 서쪽·북쪽 벽 안쪽 약 3m에 표시된다. 실제 표시와 사용자 잡기·확대 범위는 Unity 실행 검토 전이다.

튜토리얼 안내 기준점은 (0, 0, -1.5), 안내 패널은 그 앞 3m·높이 1.65m에 표시한다. 정문 시작점은 (0, 0, -17)이다. 콘텐츠 로드·전환 시 플레이어 위치와 방향을 바꾸지 않는다.

## 문과 안내선

튜토리얼실과 세 체험실에 각각 입구·출구 셔터가 있고, 후문 공간에는 입구 셔터가 있다. 총 9개다. 셔터 패널은 Collider와 함께 1.6초 동안 위아래로 3.3m 이동한다. 시작할 때는 튜토리얼실 입구만 연다.

1. 현재 목적지까지 고정된 청록색 바닥선과 화살표를 표시한다.
2. 카메라가 방 경계에서 1.2m 안쪽의 진입 영역에 들어오면 입구를 닫고 경로를 숨긴다.
3. 체험 중에는 입구·출구가 모두 닫힌다. 셔터가 닫히는 동안 플레이어가 문턱으로 되돌아오면 패널 이동을 보류한다.
4. 현재 체험이 완료되면 다음 콘텐츠를 비활성 상태로 준비한다. 준비 후에도 현재 체험의 완료 조건을 다시 확인한다.
5. 준비가 끝나면 현재 방 출구와 다음 방 입구를 열고, 다음 방까지의 안내선을 표시한다.
6. 다음 방에 들어오면 입구를 닫고 준비한 콘텐츠를 활성화한다. 이전 체험의 잡은 물체를 해제하고 이전 씬을 정리한다.
7. 후문에 진입하면 입구와 안내선을 정리하고 잡은 학습 물체를 해제한 뒤 기존 FactoryEnding을 Single로 연다. Single 전환이 공장과 학습 씬을 정리하며 로더는 중복 언로드를 하지 않는다. 전환 요청 실패 시 기존 공간·체험을 유지하고 후문 입구와 경로를 다시 표시해 재시도한다.

스틱 이동은 공용 리그의 기존 CharacterController와 벽·셔터 Collider를 사용한다. 에디터 FPS의 WASD는 시뮬레이터의 몸 추적 위치를 직접 바꾸므로 스틱 이동의 충돌 확인을 대신하지 않는다.

출구 개방은 방문 단위로 유지된다. 개방 후 기존 체험의 초기화 버튼을 눌러도 출구를 다시 닫지 않는다. 이전 방을 다시 실행하는 흐름은 없다. 경로는 사용자 위치에 따라 재탐색하지 않는다.

경로 디자인은 이전 공장의 값을 사용한다. 바닥선 폭 0.12m·두께 0.012m·중심 Y=0.025m, 화살촉 날개 폭 0.09m·뒤쪽 길이 0.5m·좌우 폭 0.25m다. 각 구간 시작에서 1.6m 뒤에 첫 화살표를 두고 3.5m 간격으로 배치한다. FactoryRoute.mat의 기존 청록색 (0.08, 1, 0.8)과 그림자·프로브 비사용 설정을 복원하며, 다섯 경로의 경유점과 표시 전환은 유지한다.

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
| [FactoryRouteGuideController](../client/Assets/Scripts/FactoryEnvironment/FactoryRouteGuideController.cs) | 목적지별 5개 고정 경로의 표시 전환 |
| [FactoryPlayerController](../client/Assets/Scripts/FactoryEnvironment/FactoryPlayerController.cs) | 정문 최초 배치, XR 리그·ray 연결, 언로드할 씬의 선택 해제 |
| [FactoryMapController](../client/Assets/Scripts/FactoryEnvironment/FactoryMapController.cs) | 공장 바닥·벽을 읽어 새 구역 이름으로 TabletMapData 게시 |

공장 Manager는 같은 루트의 Player·SceneLoad·Navigation을 사용하고 하위 경로 Controller를 연결한다. 개별 학습의 입력·계산·완료 상태는 해당 학습 Manager가 소유한다. 셔터 Controller는 공장 Manager나 학습 컴포넌트를 탐색하지 않는다.

## 로딩 수명

다음 학습 씬의 sceneLoaded 시점에 모든 루트를 비활성화해 Start 이전에 배치한다. 기존 체험은 다음 방에 들어갈 때까지 유지한다. 준비된 콘텐츠는 위치·회전을 적용하고 공장 조명을 사용하도록 자체 조명을 끈다. Rigidbody의 위치·회전도 함께 맞춘다.

공장은 CNNPixelLab 관리용 씬만 참조한다. CNNScreenLab·CNNConvolutionLab의 로드와 선택 데이터 전달은 기존 CNNPixelLabManager·PixelLabSceneLoadController가 담당한다. 공장에서는 ConvolutionCompleted를 읽고, 다음 방 진입 시 EndSession을 기다린다. 학습 로드 실패 시 기존 체험과 닫힌 출구를 유지하고 오류를 기록한 뒤 3초 간격으로 재시도한다.

씬 로딩 중 공장 오브젝트가 제거되면 로드 완료 콜백이 뒤늦게 생긴 씬을 정리한다. 이미 로드된 튜토리얼·현재 체험·준비 중인 체험은 로더의 종료 처리에서 언로드한다.

## 지도·양피지

미니맵은 기존 TabletMapData와 TabletMinimapController를 사용한다. 이름이 structural slab으로 끝나는 바닥 Collider와 concrete를 포함하는 벽 Collider의 XZ 영역을 읽는다. 셔터 상태를 동적으로 지도에 그리는 기능은 없다.

새 FactoryEnvironment에는 양피지 오브젝트를 배치하지 않았다. 공장 Manager의 양피지 열람 선행 조건도 제거했다. 양피지 에셋과 열람·수집·태블릿 코드는 유지한다. 기존 튜토리얼에는 양피지와 태블릿 목표 안내가 남아 있지만, 이 공간 씬에는 수집물이나 목표 진행률 게시가 연결되어 있지 않다.

## 씬·에셋 관리

씬 경로와 .meta GUID는 유지한다. 런타임 코드는 Scripts/FactoryEnvironment, 재질과 결합 메시는 각각 Materials/FactoryEnvironment, Meshes/FactoryEnvironment에 있다. 새로운 폴더 이동이나 이름 변경은 하지 않았다.

EscapeLayout_*.asset 74개는 이 공간의 기본 도형을 부모·재질별로 결합한 Unity 메시다. 동일한 셔터 형태는 같은 메시 에셋을 참조한다. 구조물 Collider는 별도 오브젝트로 보존하고, 움직이는 패널의 메시와 Collider는 같은 이동 루트 아래에 둔다. 씬에는 MeshRenderer 154개와 간판용 Canvas·uGUI Text 각각 22개가 저장되어 있다. 실제 Quest 성능 측정값은 아니다.

기존 FactoryDecoration_*.asset는 삭제하지 않았으며 새 씬에서는 참조하지 않는다. 기존 ParchmentSceneSetup은 이전 공장 계층·좌표를 대상으로 하는 도구이므로 새 공간의 배치 도구가 아니다.

## 확인 범위

2026-10-30: 최신 원격 main의 픽셀 체험 분리 구조를 기준으로 씬 직렬화 참조, 방·통로·셔터의 배치 값, 콘텐츠 루트 회전과 로딩·완료 판정 연결을 정적으로 검토했다. Unity 실행·Play·컴파일·테스트·빌드·프로젝트 복사본 생성은 수행하지 않았다. 실제 렌더링, 간판 크기, XR 이동·충돌·조작, 콘텐츠 펼침·확대 시 벽 여유와 Quest 성능은 실행 확인 전이다.

간판 깊이 판정·금속 간판판, 이전 바닥 경로 디자인, 후문에서 기존 엔딩 씬을 여는 세 항목의 후속 수정은 소스·씬·메시·재질 참조를 정적으로 검토했다. 이 후속 수정에 대한 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.
