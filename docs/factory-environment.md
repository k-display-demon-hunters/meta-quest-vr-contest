# 공장 환경과 체험 연결

[FactoryEnvironment.unity](../client/Assets/Scenes/FactoryEnvironment.unity)는 공장 환경, 체험 배치 기준점, 수집형 양피지를 담는다. 입구에서 바닥 안내선을 따라 직접 이동해 학습 순서에 맞는 방에 들어가면 픽셀·합성곱, 피처맵, 모델 제작 씬을 차례로 불러온다. 마지막 학습을 완료한 뒤 뒤쪽 출구로 가면 엔딩을 연다. 체험과 양피지 사용법은 [조작법](xr-simulator.md)을 참고한다.

## 파일과 클래스 역할

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [FactoryLearningFlowManager](../client/Assets/Scripts/FactoryEnvironment/FactoryLearningFlowManager.cs) | 최초 입구 배치와 튜토리얼 Additive 로드·완료 대기·언로드, 학습 순서·완료 조건, 목적지와 태블릿 지도 게시를 조율한다. 목적지 영역에 플레이어가 머물면 잡기·로딩 상태를 확인하고 체험을 Additive로 불러온다. 다음 체험 준비 후 이전 체험을 닫으며, 완료 후 출구 진입은 엔딩 Single 로드로 연결한다. |
| [FactoryAreaEntryController](../client/Assets/Scripts/FactoryEnvironment/FactoryAreaEntryController.cs) | `Bind()`로 전달된 플레이어 카메라가 BoxCollider 영역 안에 있는지 로컬 좌표로 판정한다. 리그의 물리 충돌이나 Rigidbody·Trigger 이벤트에 의존하지 않으며 손·잡은 물체의 진입은 판정하지 않는다. |
| [FactoryRouteGuideController](../client/Assets/Scripts/FactoryEnvironment/FactoryRouteGuideController.cs) | 씬에 저장한 네 개의 고정 바닥 경로 중 현재 단계의 오브젝트를 표시한다. 플레이어 이동에 따라 경로의 위치나 형태를 다시 계산하지 않는다. |
| [FactoryFlowPortalController](../client/Assets/Scripts/FactoryEnvironment/FactoryFlowPortalController.cs) | 입구에 `바닥의 안내선을 따라 이동하세요.`라는 고정 안내판을 생성한다. 다음 버튼이나 상호작용 컴포넌트는 생성하지 않는다. |
| [FactoryMapReader](../client/Assets/Scripts/FactoryEnvironment/FactoryMapReader.cs) | 공장 루트 아래 BoxCollider의 이름·높이·월드 경계를 읽어 바닥과 벽을 구분한다. 여기에 방 이름, 양피지 컴포넌트의 문서 인덱스·참조를 모아 `TabletMapData`로 반환한다. Manager는 이 데이터를 태블릿에 전달하므로 지도 UI가 공장의 계층 구조를 직접 해석할 필요가 없다. |
| [ParchmentPickupController](../client/Assets/Scripts/FactoryEnvironment/ParchmentPickupController.cs) | XRI Activate와 선택 시작을 같은 수집 요청으로 처리한다. 입력을 보낸 Interactor와의 거리를 확인하고 해당 리그의 `TabletManager.CollectParchment()`를 호출한 뒤, 획득했거나 이미 보유한 양피지를 다음 프레임에 비활성화한다. 평상시에는 양피지의 표시 애니메이션을 갱신한다. |
| [TabletMapData](../client/Assets/Scripts/Core/Tablet/TabletMapData.cs) | 판독한 바닥·벽 목록과 지도 경계를 읽기 전용 데이터로 묶는다. 양피지는 원본 컴포넌트 참조와 문서 인덱스를 보관해 위치·활성 상태를 조회할 수 있게 한다. 공장 배치를 읽는 코드와 지도 그래픽을 그리는 코드 사이의 전달 형식이다. |
| [TabletMinimapController](../client/Assets/Scripts/Core/Tablet/TabletMinimapController.cs) | 전달받은 바닥·벽의 XZ 영역을 태블릿 좌표로 투영해 uGUI 도형으로 그린다. 카메라 위치·방향과 수집 여부를 주기적으로 읽어 플레이어 표시와 남은 양피지를 갱신한다. 같은 데이터로 플레이어 주변 지도와 전체 공장 지도를 구성한다. |
| [TabletParchmentController](../client/Assets/Scripts/Core/Tablet/TabletParchmentController.cs) | 문서 인덱스별 획득 배열과 선택 문서·페이지를 보관한다. 수집되면 인벤토리를 갱신하고, 보유한 항목을 선택하면 `ParchmentLesson`의 페이지를 화면에 연결한다. 수집 여부는 리그의 메모리에 남고 지도도 같은 상태를 읽어 획득한 양피지 마커를 숨긴다. |
| [ParchmentLesson](../client/Assets/Scripts/Core/Tablet/ParchmentLesson.cs) | 문서별 제목·부서·배치 방·본문 페이지를 정적 목록으로 제공한다. 수집 오브젝트의 `LessonIndex`가 이 목록의 항목을 가리키며, 태블릿의 인벤토리 설명과 열람 화면이 같은 데이터를 사용한다. 획득 여부나 현재 읽는 페이지는 Controller가 관리한다. |

## 체험 실행 흐름

```text
FactoryIntro
  → FactoryEnvironment
      → FactoryTutorial (안내 완료 후 언로드)
      → CNNPixelLab
      → CNNFeatureMapLab
      → CNNModelDesignLab
      → 뒤쪽 출구 → FactoryEnding
```

`FactoryLearningFlowManager.Start`는 유지된 XR 리그를 씬의 `_entrance` 기준점 `(0, 0, -20)`에 한 번 배치한다. 카메라 추적 높이를 보존하고 입구 방향을 맞추며, 에디터 시뮬레이터의 초기 눈높이 보정을 유지한다. 진입 영역을 연결하고 지도를 태블릿에 게시한 뒤 `FactoryTutorial`을 Additive로 한 번 불러온다. `FactoryTutorialManager.IsComplete`가 될 때까지 방 진입에 따른 학습 로드를 보류하고, 완료 후 안내 씬을 언로드한 다음 첫 경로를 켠다. 안내는 공장 배경을 유지한 입구에서 수행 판정 없는 다섯 페이지로 진행한다. 안내 씬에는 리그·카메라·배경·EventSystem을 중복 배치하지 않으며 공장의 공용 EventSystem은 안내 언로드 뒤에도 유지한다. 안내 코드와 입력 복원은 [오프닝·입구 안내](factory-intro-ending.md)에 정리한다. 이후 체험은 다음 순서로 진행한다.

1. 현재 체험이 미완료면 그 방을, 완료했으면 다음 방을 목적지로 삼는다. 바닥선을 따라 직접 이동하며 목적지 영역 안에서는 선을 숨긴다. 순서보다 앞선 방에 들어가도 체험을 불러오지 않는다.
2. 플레이어가 허용된 다음 영역에 머물고 물체를 잡지 않았으면 대상 씬 등록 여부를 확인하고 Additive로 로드한다. 영역 안에서 물체를 놓아도 전환할 수 있다. 로딩 중 중복 요청은 차단한다.
3. `CNNPixelLabManager`, `CNNFeatureMapLabManager`, `CNNModelDesignLabManager` 중 하나가 포함된 루트를 체험 콘텐츠로 찾는다.
4. 콘텐츠 외의 루트와 콘텐츠 내부 조명을 비활성화하고 공장의 바닥·조명을 사용한다. 콘텐츠를 기준점에 배치하고 Rigidbody 위치를 맞춘다. 픽셀 체험은 `RebaseDock`으로 원위치 복귀 기준도 갱신한다.
5. 다음 콘텐츠 준비가 끝나면 완료 조건과 잡기 상태를 다시 확인하고 이전 체험을 언로드한다. 콘텐츠를 준비하지 못하면 기존 체험을 유지한다. 로드 실패는 Console에 알리고, 해당 영역에서 나갔다 다시 들어오면 재시도한다.
6. 공장 씬을 활성 씬으로 유지한다. 체험 전환 때 플레이어를 이동시키지 않는다. 현재 방을 잠깐 나가도 체험은 유지되며, 다음 체험으로 진행한 뒤 이전 체험의 재실행·진행 복원은 제공하지 않는다.
7. 모델 학습 성공·결과 표시 후에는 뒤쪽 출구를 안내한다. 출구 진입 시 `FactoryEnding`을 Single로 열어 공장과 체험을 정리한다. 이 전환에서는 Manager의 파괴 콜백이 체험을 중복 언로드하지 않는다.

| 현재 단계 | 다음 단계로 넘어가는 조건 |
| --- | --- |
| 공장 진입 | `FactoryTutorial`의 안내 완료·언로드 후 Pixel Lab에 들어가면 `CNNPixelLab`을 불러온다. |
| 픽셀·합성곱 | `CNNPixelLabManager.ConvolutionCompleted`가 참이면 Feature Map Lab을 안내한다. |
| 피처맵 | `CNNFeatureMapGuide.ReadyForModelDesign`이 참이면 Model Design Lab을 안내한다. |
| 모델 제작 | `CNNModelDesignLabManager.ReadyForFactoryExit`가 참이면 뒤쪽 출구를 안내한다. 정상 학습 결과 표시가 필요하며 실행 중에는 출구 전환을 보류한다. 재학습 시작 시 완료 상태를 초기화한다. |

각 체험의 계산과 입력은 해당 씬의 Manager·Controller가 담당한다. 픽셀 체험의 계산 결과를 피처맵 씬으로 전달하지 않으며, 피처맵은 자체 예제 데이터를 사용한다. 자세한 구현은 [픽셀·합성곱](pixel-inspection.md), [피처맵](cnn-feature-map-explorer.md), [모델 제작](MODEL_DESIGN_API.md)에 정리한다.

공용 [XRPlayerRig](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)는 `PersistentXRPlayerRig`가 유지한다. 공장 Manager가 파괴되면 자신이 로드한 체험 씬을 언로드하되, 엔딩 Single 전환은 해당 전환이 정리를 담당한다.

## 바닥 안내선과 이동

바닥 안내는 씬에 배치한 네 개의 고정 경로 오브젝트다. 입구→Pixel Lab, Pixel Lab→Feature Map Lab, Feature Map Lab→Model Design Lab, Model Design Lab→출구 순서이며 기존 경유점의 안전한 복도·출입구 동선을 사용한다. Model Design Lab의 랙 북쪽과 출하 구역 서쪽 입구를 우회한다. 선과 화살표는 고정 위치에 있고 사용자 이동으로 시작점이나 형태가 바뀌지 않는다. 현재 목적지 영역에서는 숨기고, 영역 밖이나 다음 단계에서는 해당 경로를 표시한다. 경로를 벗어난 사용자 위치와 연결하는 자동 재탐색은 제공하지 않는다.

`FactoryRouteGuideController._routes`에는 네 경로를 순서대로 연결한다. 모든 경로는 같은 `FactoryRoute.mat`을 사용하며 Collider와 그림자는 없다. `SetDestination()`은 표시 상태만 바꾼다.

`FactoryRoomSignController`는 기존 간판 프레임과 위치를 유지하고 인쇄 면을 공통 어두운 배경과 새 방 이름으로 표시한다. 기존 간판 PNG/GUID나 공장 내부 탐색용 오브젝트명을 바꾸지 않는다. `FactoryMapReader`는 같은 새 방 이름을 지도 데이터로 전달한다. 태블릿 구현은 변경하지 않아 양피지 인벤토리의 기존 `ParchmentLesson.Room` 문구는 이번 수정 범위에서 제외한다.

공용 스틱 이동 속도는 3m/s다. 플레이어의 벽 충돌은 이번 변경에 포함하지 않는다. 에디터 WASD 가상 기기 이동은 기존 방식과 속도를 유지한다.

## 공장 지도 데이터

지도 연결은 다음 순서다.

```text
FactoryMapReader.Read(공장 루트)
  → TabletMapData
  → TabletManager.SetMapData
  → TabletMinimapController
```

`FactoryMapReader`는 BoxCollider의 월드 X/Z 경계로 평면 영역을 만든다. 오브젝트 이름과 Collider가 지도 생성 규칙에 사용된다.

| 대상 | 읽는 규칙 |
| --- | --- |
| 바닥 | 이름이 `structural slab`으로 끝나는 BoxCollider |
| 벽·기둥 | 이름에 `concrete`가 포함되고 높이가 1 이상이며 하단 Y가 0.6 미만인 BoxCollider |
| 방 이름 | 바닥 이름의 `Exterior`, `Security`, `Spine`, `Inspection`, `Feature`, `Assembly`, `Outbound` 접두사로 결정 |
| 양피지 | 비활성 오브젝트를 포함한 `ParchmentPickupController`의 참조와 `LessonIndex` |

바닥과 벽은 읽는 시점의 정적 데이터다. 양피지 마커는 원본 Transform의 위치와 활성 상태를 참조한다. `FactoryLearningFlowManager`는 시작 및 재활성화 때 데이터를 읽어 게시하고, 비활성화될 때 자신이 게시한 데이터를 해제한다.

`TabletMinimapController`는 같은 데이터를 주변 지도와 전체 지도에 사용한다. 카메라의 X/Z 위치·방향과 획득 상태는 0.1초 간격으로 갱신한다. 마커는 원본이 활성 상태이고 `TabletManager.HasParchment`가 거짓일 때 표시한다. 지도 도형은 `MaskableGraphic`으로 그리며 공장 전용 카메라나 RenderTexture에 의존하지 않는다.

## 양피지 수집과 문서

`ParchmentPickupController`는 `XRSimpleInteractable.activated`와 `selectEntered`에서 같은 `TryCollect` 경로를 사용한다. 입력을 보낸 Interactor가 3m 이내인지 확인하고 해당 리그의 `TabletManager`를 찾는다. `CollectParchment`가 성공했거나 이미 보유한 문서이면 다음 프레임에 양피지를 비활성화해 XRI 콜백 처리를 마친다.

획득 상태는 `TabletParchmentController`의 배열에 보관된다. 공용 리그가 유지되는 씬 이동에서는 상태가 남고, 앱을 다시 시작하면 초기화된다. 공장을 다시 로드하면 각 양피지가 `Start`에서 획득 상태를 확인해 이미 수집한 오브젝트를 숨긴다.

`LessonIndex`는 `ParchmentLesson.All`의 0부터 시작하는 인덱스다. 각 문서는 세 페이지로 구성된다.

| 인덱스 | 주제 | 배치 방 |
| --- | --- | --- |
| 0 | 이미지와 픽셀, 밝기·RGB·채널 | Pixel Lab |
| 1 | 합성곱과 패딩 | Model Design Lab |
| 2 | 필터와 특징 추출 | Model Design Lab |
| 3 | 특징 맵 | Model Design Lab |
| 4 | 활성화 함수와 ReLU | Model Design Lab |

특징 맵 문서의 표시 전환은 `TabletParchmentController.ToggleFeatureMap`이 삽화 Image와 문구를 갱신하는 기능이다. 모델 계산 데이터에는 영향을 주지 않는다.

## 씬 구성과 에디터 도구

공장 루트는 `Factory Environment`다. 기존 `08 Content anchors and future scene links` 아래에 `Factory room navigation`을 추가하고 안내선 Controller와 세 방·출구의 진입 영역을 배치한다. 기존 입구 기준점 `ENTRY FROM FACTORY INTRO - exterior approach`를 `_entrance`에 연결한다. 각 실의 `CONTENT ZONE` Transform은 기존 콘텐츠 배치를 유지한다. `_contentAnchors`는 픽셀·피처맵·모델 제작 순서이며, `_areas`는 같은 세 방 뒤에 출구 영역을 연결한다. `_routeGuide`는 안내선 Controller, `_font`는 고정 안내판 글꼴, `_endingSceneName`은 `FactoryEnding`이다.

| 도구 | 역할과 의존성 |
| --- | --- |
| [ParchmentSceneSetup](../client/Assets/Editor/ParchmentSceneSetup.cs) | `Install`에서 공장 씬을 열고 `03 Original image inspection room`, `05 Model assembly room`의 `Learning Parchments`를 구성한다. 문서 인덱스, Sprite·글꼴·Collider·Interactable을 연결하고 공용 리그를 확인한 뒤 씬을 저장한다. |
| [FactoryDecorationMeshCombiner](../client/Assets/Editor/FactoryDecorationMeshCombiner.cs) | 고정 장식의 MeshRenderer를 부모·재질·렌더러 설정별로 묶어 메시 에셋을 만든다. 그룹 크기와 추가 광원 수를 제한하며, 스크립트·물리·프리팹 등 개별 동작에 의존하는 대상은 제외한다. 원본 Renderer는 끄고 Transform을 유지한다. |

양피지는 [LearningParchment.png](../client/Assets/Textures/Common/LearningParchment.png)와 [Nanum Pen Script](../client/Assets/Fonts/NanumPenScript-Regular.ttf)를 참조한다. 본문은 텍스처와 별도로 표시한다. 폰트 배포 조건은 동봉된 [OFL](../client/Assets/Fonts/NanumPenScript-OFL.txt)에 있다.

공장 전용 렌더링 에셋은 [Materials](../client/Assets/Materials/FactoryEnvironment), [Textures](../client/Assets/Textures/FactoryEnvironment), [Meshes](../client/Assets/Meshes/FactoryEnvironment)에 있다.

## 이번 변경의 확인 범위

방 진입·고정 바닥선·튜토리얼 Additive 로드와 완료 대기·언로드·간판·출구 엔딩 연결과 속도는 소스 및 씬·에셋 참조만 정적으로 검토했다. 사용자 지시에 따라 Unity 실행, 테스트, 빌드, 컴파일은 수행하지 않았다. 안내선의 실제 표시와 안내 버튼 입력 복구·컨트롤러 조작·씬 진행은 실행 확인 전이다.
