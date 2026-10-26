# 공장 환경과 체험 연결

[FactoryEnvironment.unity](../client/Assets/Scenes/FactoryEnvironment.unity)는 공장 환경, 체험 배치 기준점, 수집형 양피지를 담는다. 입구에서 바닥 안내선을 따라 직접 이동해 학습 순서에 맞는 방에 들어가면 픽셀·합성곱, 피처맵, 모델 제작 씬을 차례로 불러온다. 마지막 학습을 완료한 뒤 뒤쪽 출구로 가면 엔딩을 연다. 체험과 양피지 사용법은 [조작법](xr-simulator.md)을 참고한다.

## 파일과 클래스 역할

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [FactoryLearningFlowManager](../client/Assets/Scripts/FactoryEnvironment/FactoryLearningFlowManager.cs) | 플레이어 입구 배치를 요청하고 튜토리얼 실행 요청·결과 확인, 학습 순서·완료 조건, 안내판·경로 표시를 조율한다. 영역 진입을 확인해 Controller에 단계 전환을 요청하며, 다음 체험 준비 후 완료 조건을 재확인해 전환을 확정하거나 취소한다. 물체를 잡고 있어도 전환을 허용한다. |
| [FactoryNavigationData](../client/Assets/Scripts/FactoryEnvironment/FactoryNavigationData.cs) | 목적지 enum `FactoryDestinationId`와 목적지별 경로·진입 영역·씬 이름·콘텐츠 기준점을 한 컴포넌트에서 관리한다. Manager와 SceneLoadController는 `Awake()`의 `GetComponent`, RouteGuide는 `GetComponentInParent`로 같은 인스턴스를 찾아 보관한다. `Destinations`는 기존 배열의 읽기 전용 뷰인 `ReadOnlySpan`을 반환해 순회 시 반복자 객체를 할당하지 않는다. Singleton이나 전역 데이터는 사용하지 않는다. |
| [FactoryAreaEntryController](../client/Assets/Scripts/FactoryEnvironment/FactoryAreaEntryController.cs) | `Bind()`로 전달된 플레이어 카메라가 BoxCollider 영역 안에 있는지 로컬 좌표로 판정한다. 리그의 물리 충돌이나 Rigidbody·Trigger 이벤트에 의존하지 않으며 손·잡은 물체의 진입은 판정하지 않는다. |
| [FactoryRouteGuideController](../client/Assets/Scripts/FactoryEnvironment/FactoryRouteGuideController.cs) | 씬에 저장한 네 개의 고정 바닥 경로 중 현재 단계의 오브젝트를 표시한다. 플레이어 이동에 따라 경로의 위치나 형태를 다시 계산하지 않는다. |
| [FactorySceneLoadController](../client/Assets/Scripts/FactoryEnvironment/FactorySceneLoadController.cs) | 공유 목적지 데이터에서 튜토리얼·학습·엔딩 씬 이름을 읽어 Additive/Single 로드를 담당한다. 튜토리얼 초기화·완료 대기·언로드와 체험 콘텐츠 배치·조명 정리·Rigidbody 동기화, 준비한 씬의 확정·취소, 이전 씬 언로드와 파괴 시 정리를 처리한다. 로딩 결과와 오류를 Manager에 제공하며 학습 완료 조건은 판단하지 않는다. |
| [FactoryPlayerController](../client/Assets/Scripts/FactoryEnvironment/FactoryPlayerController.cs) | Manager의 입구 배치 요청 때 유지된 XR 리그와 Interactor를 찾고, 추적 높이를 보존해 위치·방향을 맞춘다. 에디터 눈높이 보정, ray 입력 준비, 카메라·리그 참조와 언로드할 학습 씬의 물체 선택 해제를 담당한다. |
| [FactoryMapController](../client/Assets/Scripts/FactoryEnvironment/FactoryMapController.cs) | 공장 루트 아래 BoxCollider의 이름·높이·월드 경계를 읽어 바닥과 벽을 구분한다. 방 이름과 양피지 문서 인덱스·참조를 모아 `TabletMapData`를 만들고 태블릿에 게시한다. 시작·재활성화 때 읽어 게시하고 비활성화 때 자신이 게시한 지도를 해제한다. |
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

`FactoryLearningFlowManager.Start`는 `FactoryPlayerController.PlaceAtEntrance()`를 호출해 유지된 XR 리그를 FactoryNavigationData의 `Entrance` 기준점 `(0, 0, -20)`에 한 번 배치한다. 카메라 추적 높이를 보존하고 입구 방향을 맞추며, 에디터 시뮬레이터의 초기 눈높이 보정을 유지한다. Controller가 제공한 카메라를 진입 영역에 연결한 뒤 `FactorySceneLoadController.RunTutorial()`을 요청한다. Controller가 `FactoryTutorial`을 Additive로 로드하고 초기화한 뒤 `FactoryTutorialManager.IsComplete`를 기다려 언로드한다. SceneLoadController는 성공한 뒤 씬에 미리 배치한 안내판을 켠다. Manager는 시작 시 첫 경로를 켜고 튜토리얼 완료 여부와 관계없이 학습 진입을 허용한다. 첫 학습 진입 시 `EndTutorial()`을 요청하고, 튜토리얼 언로드가 끝나면 학습 씬을 로드한다. 튜토리얼이 로딩 중이면 로드 완료 후 초기화하지 않고 바로 언로드한다. 튜토리얼 로딩 실패 또는 완료 전 안내 오브젝트 해제는 Controller에서 로그로 알린다. 안내는 공장 배경을 유지한 입구에서 수행 판정 없는 다섯 페이지로 진행한다. 안내 씬에는 리그·카메라·배경·EventSystem을 중복 배치하지 않으며 공장의 공용 EventSystem은 안내 언로드 뒤에도 유지한다. 안내 코드와 입력 복원은 [오프닝·입구 안내](factory-intro-ending.md)에 정리한다. 이후 체험은 다음 순서로 진행한다.

1. 현재 체험이 미완료면 그 방을, 완료했으면 다음 방을 목적지로 삼는다. 바닥선을 따라 직접 이동하며 목적지 영역 안에서는 선을 숨긴다. 순서보다 앞선 방에 들어가도 체험을 불러오지 않는다.
2. 플레이어가 허용된 다음 영역에 머물고 물체를 잡지 않았으면 대상 씬을 Additive로 로드한다. 영역 안에서 물체를 놓아도 전환할 수 있다. 로딩 중 중복 요청은 차단한다.
3. `CNNPixelLabManager`, `CNNFeatureMapLabManager`, `CNNModelDesignLabManager` 중 하나가 포함된 루트를 체험 콘텐츠로 찾는다.
4. 콘텐츠 외의 루트와 콘텐츠 내부 조명을 비활성화하고 공장의 바닥·조명을 사용한다. 콘텐츠를 기준점에 배치하고 Rigidbody 위치를 맞춘다. 픽셀 체험은 보드 기준 로컬 좌표로 필터 복귀 위치와 회전을 관리하므로 공장에서 별도로 보정하지 않는다.
5. 다음 콘텐츠 준비가 끝나면 완료 조건을 다시 확인하고, 이전 학습 씬에서 잡고 있던 물체를 XRI `SelectExit`로 놓은 뒤 이전 체험을 언로드한다. 엔딩 전환도 같은 선택 해제를 수행하며, 태블릿처럼 다른 씬에 속한 물체는 해제하지 않는다. 씬 로드에 실패하면 기존 체험을 유지한다. 씬 내부의 필수 콘텐츠 구성 누락은 런타임 복구 대상으로 처리하지 않는다. 로드 실패는 Console에 알리고, 해당 영역에 머무르면 다음 진입 검사 주기(0.1초)에 다시 로딩을 요청한다.
6. 공장 씬을 활성 씬으로 유지한다. 체험 전환 때 플레이어를 이동시키지 않는다. 현재 방을 잠깐 나가도 체험은 유지되며, 다음 체험으로 진행한 뒤 이전 체험의 재실행·진행 복원은 제공하지 않는다.
7. 모델 학습 성공·결과 표시 후에는 뒤쪽 출구를 안내한다. 출구 진입 시 `FactoryEnding`을 Single로 열어 공장과 체험을 정리한다. 이 전환에서는 Controller의 `IsLeaving` 상태로 파괴 콜백의 중복 언로드를 막는다.

| 현재 단계 | 다음 단계로 넘어가는 조건 |
| --- | --- |
| 공장 진입 | `FactoryTutorial`의 안내 완료·언로드 후 Pixel Lab에 들어가면 `CNNPixelLab`을 불러온다. |
| 픽셀·합성곱 | `CNNPixelLabManager.ConvolutionCompleted`가 참이면 Feature Map Lab을 안내한다. |
| 피처맵 | `CNNFeatureMapGuide.ReadyForModelDesign`이 참이면 Model Design Lab을 안내한다. |
| 모델 제작 | `CNNModelDesignLabManager.ReadyForFactoryExit`가 참이면 뒤쪽 출구를 안내한다. 정상 학습 결과 표시가 필요하며 실행 중에는 출구 전환을 보류한다. 재학습 시작 시 완료 상태를 초기화한다. |

각 체험의 계산과 입력은 해당 씬의 Manager·Controller가 담당한다. 픽셀 체험의 계산 결과를 피처맵 씬으로 전달하지 않으며, 피처맵은 자체 예제 데이터를 사용한다. 자세한 구현은 [픽셀·합성곱](pixel-inspection.md), [피처맵](cnn-feature-map-explorer.md), [모델 제작](MODEL_DESIGN_API.md)에 정리한다.

공용 [XRPlayerRig](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)는 `PersistentXRPlayerRig`가 유지한다. 공장 루트의 `FactorySceneLoadController`가 파괴되면 자신이 로드한 튜토리얼·현재 체험·준비 중인 체험 씬을 언로드하되, 엔딩 Single 전환은 해당 전환이 정리를 담당한다.

## 바닥 안내선과 이동

바닥 안내는 씬에 배치한 네 개의 고정 경로 오브젝트다. 입구→Pixel Lab, Pixel Lab→Feature Map Lab, Feature Map Lab→Model Design Lab, Model Design Lab→출구 순서이며 기존 경유점의 안전한 복도·출입구 동선을 사용한다. Model Design Lab의 랙 북쪽과 출하 구역 서쪽 입구를 우회한다. 선과 화살표는 고정 위치에 있고 사용자 이동으로 시작점이나 형태가 바뀌지 않는다. 현재 목적지 영역에서는 숨기고, 영역 밖이나 다음 단계에서는 해당 경로를 표시한다. 경로를 벗어난 사용자 위치와 연결하는 자동 재탐색은 제공하지 않는다.

각 목적지의 `Content`, `Pixels`, `Maps`, `Model` 프로퍼티는 로드된 콘텐츠와 학습 컴포넌트의 런타임 참조를 보관한다. Manager의 `FindContent()`가 목적지에 맞는 학습 컴포넌트를 한 번 찾아 콘텐츠 루트와 함께 저장하며, Feature Map은 완료 판정에 사용하는 Guide로 루트를 식별한다. SceneLoadController는 `PrepareLesson()`으로 씬을 로드해 `PreparedScene`을 제공하고, 탐색이 끝난 뒤 `PlacePreparedContent()`로 저장된 콘텐츠를 배치한다. Manager는 완료 판정과 상세 화면 정리에 저장된 참조를 사용하며 전환 후 재탐색하지 않는다. SceneLoadController는 기존 씬 언로드·준비 취소·종료 시 해당 런타임 참조를 비운다.

`FactoryNavigationData._destinations`의 각 목적지 항목에 경로를 연결한다. 배열 순서는 동작에 영향을 주지 않으며 `FactoryDestinationId`로 조회한다. Controller는 `Start()`에서 모든 경로를 숨겨 초기화한다. Inspector의 필수 연결은 런타임에서 재검증하지 않는다. 모든 경로는 같은 `FactoryRoute.mat`을 사용하며 Collider와 그림자는 없다. `SetDestination()`은 표시 상태만 바꾼다.

방 간판은 `FactoryEnvironment.unity`에 World Space Canvas와 Text로 미리 배치한다. 기존 간판 프레임과 위치를 유지하고 인쇄 면은 `RoomSignSurface.mat`의 공통 어두운 배경을 사용한다. 방 이름은 Pixel Lab, Feature Map Lab, Model Design Lab이며, 런타임 생성이나 재질 덮어쓰기 없이 씬에 저장된 상태로 표시한다. 간판 문구·배치·스타일은 씬에서 편집한다. 기존 간판 PNG/GUID나 공장 내부 탐색용 오브젝트명을 바꾸지 않는다. `FactoryMapController`는 같은 새 방 이름을 지도 데이터로 전달한다. 태블릿 구현은 변경하지 않아 양피지 인벤토리의 기존 `ParchmentLesson.Room` 문구는 이번 수정 범위에서 제외한다.

공용 스틱 이동 속도는 5m/s다. 플레이어의 벽 충돌은 이번 변경에 포함하지 않는다. 에디터 FPS 모드의 WASD 몸 이동 속도도 5m/s다.

입구 안내판 `Factory route instruction`은 공장 루트 아래 `(0, 1.4, -17)`에 비활성 상태로 저장한다. 배경 Cube는 `(3.6, 0.45, 0.05)` 크기로 방 간판과 같은 `RoomSignSurface.mat`을 참조한다. TextMesh는 기존 Noto Sans 글꼴과 `바닥의 안내선을 따라 이동하세요.` 문구를 사용하고 Collider나 입력 컴포넌트는 두지 않는다. SceneLoadController는 튜토리얼 완료 또는 첫 학습 진입에 따른 언로드 후 안내판을 활성화하며, 런타임 오브젝트·재질 생성은 하지 않는다.

## 공장 지도 데이터

지도 연결은 다음 순서다.

```text
FactoryMapController.ReadMap()
  → TabletMapData
  → TabletManager.SetMapData
  → TabletMinimapController
```

`FactoryMapController`는 BoxCollider의 월드 X/Z 경계로 평면 영역을 만든다. 오브젝트 이름과 Collider가 지도 생성 규칙에 사용된다.

| 대상 | 읽는 규칙 |
| --- | --- |
| 바닥 | 이름이 `structural slab`으로 끝나는 BoxCollider |
| 벽·기둥 | 이름에 `concrete`가 포함되고 높이가 1 이상이며 하단 Y가 0.6 미만인 BoxCollider |
| 방 이름 | 바닥 이름의 `Exterior`, `Security`, `Spine`, `Inspection`, `Feature`, `Assembly`, `Outbound` 접두사로 결정 |
| 양피지 | 비활성 오브젝트를 포함한 `ParchmentPickupController`의 참조와 `LessonIndex` |

바닥과 벽은 읽는 시점의 정적 데이터다. 양피지 마커는 원본 Transform의 위치와 활성 상태를 참조한다. `FactoryMapController`는 시작 및 재활성화 때 데이터를 읽어 게시하고, 비활성화될 때 자신이 게시한 데이터를 해제한다.

`TabletMinimapController`는 같은 데이터를 주변 지도와 전체 지도에 사용한다. 카메라의 X/Z 위치·방향과 획득 상태는 0.1초 간격으로 갱신한다. 마커는 원본이 활성 상태이고 `TabletManager.HasParchment`가 거짓일 때 표시한다. 지도 도형은 `MaskableGraphic`으로 그리며 공장 전용 카메라나 RenderTexture에 의존하지 않는다.

## 양피지 수집과 문서

`ParchmentPickupController`는 `XRSimpleInteractable.activated`와 `selectEntered`에서 같은 `TryCollect` 경로를 사용한다. 입력을 보낸 Interactor가 3m 이내인지 확인하고 해당 리그의 `TabletManager`를 찾는다. `CollectParchment`가 성공했거나 이미 보유한 문서이면 다음 프레임에 양피지를 비활성화해 XRI 콜백 처리를 마친다.

획득 상태는 `TabletParchmentController`의 배열에 보관된다. 공용 리그가 유지되는 씬 이동에서는 상태가 남고, 앱을 다시 시작하면 초기화된다. 공장을 다시 로드하면 각 양피지가 `Start`에서 획득 상태를 확인해 이미 수집한 오브젝트를 숨긴다.

`LessonIndex`는 `ParchmentLesson.All`의 0부터 시작하는 인덱스다. 각 문서는 세 페이지로 구성된다.

| 인덱스 | 주제 | 배치 방 |
| --- | --- | --- |
| 0 | 이미지와 픽셀, 밝기·RGB·채널 | Pixel Lab 접근 동선 |
| 1 | 합성곱과 패딩 | Pixel Lab 접근 동선 |
| 2 | 필터와 특징 추출 | Feature Map Lab 접근 동선 |
| 3 | 특징 맵 | Feature Map Lab 접근 동선 |
| 4 | 활성화 함수와 ReLU | Model Design Lab 접근 동선 |

특징 맵 문서의 표시 전환은 `TabletParchmentController.ToggleFeatureMap`이 삽화 Image와 문구를 갱신하는 기능이다. 모델 계산 데이터에는 영향을 주지 않는다.

## 씬 구성과 에디터 도구

공장 루트는 `Factory Environment`다. 같은 루트에 `FactoryPlayerController`와 `FactoryMapController`를 미리 배치한다. Manager는 `Awake()`에서 같은 오브젝트의 `FactoryPlayerController`와 `FactorySceneLoadController`를 `GetComponent`로 가져와 보관한다. Manager는 같은 `Awake()`에서 자식 계층의 `FactoryRouteGuideController`를 비활성 오브젝트까지 포함해 한 번 찾아 보관한다. Map Controller는 자신이 붙은 `gameObject`를 공장 루트로 사용한다. 지도 게시·해제는 Map Controller가 독립적으로 관리한다. 기존 `08 Content anchors and future scene links` 아래에 `Factory room navigation`을 추가하고 안내선 Controller와 세 방·출구의 진입 영역을 배치한다. 기존 입구 기준점 `ENTRY FROM FACTORY INTRO - exterior approach`를 FactoryNavigationData의 `_entrance`에 연결한다. PlayerController는 플레이어 배치에, SceneLoadController는 튜토리얼 초기화에 같은 `Entrance` 프로퍼티를 사용한다. 각 실의 `CONTENT ZONE` Transform은 기존 콘텐츠 배치를 유지한다. 공장 루트의 `FactoryNavigationData`에 PixelLab·FeatureMapLab·ModelDesignLab·Exit의 경로, 진입 영역, 씬 이름, 콘텐츠 기준점을 묶어 설정한다. Exit의 씬은 `FactoryEnding`이며 콘텐츠 기준점은 비워 둔다. 튜토리얼 씬 이름도 같은 컴포넌트의 `_tutorialSceneName`에서 설정한다. Manager와 PlayerController, SceneLoadController는 같은 공장 루트에서, RouteGuide는 부모 계층에서 `FactoryNavigationData`을 `Awake()`에 한 번 가져와 보관한다. SceneLoadController의 `_routeInstruction`은 미리 배치한 안내판을 참조하며 튜토리얼 완료 후 표시한다. 필수 참조와 목적지 항목은 Inspector에서 올바르게 연결하며, 데이터 공유 참조·목적지 누락·중복·필수 연결에 대한 런타임 검증은 하지 않는다. 단계 전환은 enum별 명시적 분기로 결정하며 enum을 배열 인덱스로 변환하거나 증가시키지 않는다.

| 도구 | 역할과 의존성 |
| --- | --- |
| [ParchmentSceneSetup](../client/Assets/Editor/ParchmentSceneSetup.cs) | `Install`에서 공장 씬을 열고 `03 Original image inspection room`, `05 Model assembly room`의 `Learning Parchments`를 구성한다. 문서 인덱스, Sprite·글꼴·Collider·Interactable을 연결하고 공용 리그를 확인한 뒤 씬을 저장한다. |
| [FactoryDecorationMeshCombiner](../client/Assets/Editor/FactoryDecorationMeshCombiner.cs) | 고정 장식의 MeshRenderer를 부모·재질·렌더러 설정별로 묶어 메시 에셋을 만든다. 그룹 크기와 추가 광원 수를 제한하며, 스크립트·물리·프리팹 등 개별 동작에 의존하는 대상은 제외한다. 원본 Renderer는 끄고 Transform을 유지한다. |

양피지는 [LearningParchment.png](../client/Assets/Textures/Common/LearningParchment.png)와 [Nanum Pen Script](../client/Assets/Fonts/NanumPenScript-Regular.ttf)를 참조한다. 본문은 텍스처와 별도로 표시한다. 폰트 배포 조건은 동봉된 [OFL](../client/Assets/Fonts/NanumPenScript-OFL.txt)에 있다.

공장 전용 렌더링 에셋은 [Materials](../client/Assets/Materials/FactoryEnvironment), [Textures](../client/Assets/Textures/FactoryEnvironment), [Meshes](../client/Assets/Meshes/FactoryEnvironment)에 있다.

## 이번 변경의 확인 범위

방 진입·고정 바닥선·튜토리얼 Additive 로드와 완료 대기·언로드·간판·출구 엔딩 연결과 속도는 소스 및 씬·에셋 참조만 정적으로 검토했다. 사용자 지시에 따라 Unity 실행, 테스트, 빌드, 컴파일은 수행하지 않았다. 안내선의 실제 표시와 안내 버튼 입력 복구·컨트롤러 조작·씬 진행은 실행 확인 전이다.

## 미션·방 소개·수집 동선 개편 (2026-10-26)

입구에서 전체 미션과 태블릿 역할을 안내한다. 양피지 5개 수집과 세 방 체험 완료를 합쳐 8개 목표의 진행률을 실제 상태로 표시한다. 태블릿의 미션 확인 버튼은 안내를 보여주며 수동으로 완료 수치를 올리지 않는다. 공장 이동은 실제 이동으로 진행하며 태블릿의 단계 항목은 해당 방의 이동 안내를 제공한다.

양피지 1·2는 Pixel Lab 접근 동선, 3·4는 Feature Map Lab 접근 동선, 5는 Model Design Lab 접근 동선에 배치했다. 문서 순서·GUID·인벤토리 인덱스는 유지한다. 미수집 양피지가 있어도 방 입장을 막지 않고 지도에서 재방문하도록 안내한다. 진행률과 수집 상태는 현재 실행 세션 기준이다.

각 목적지에 들어가면 이 방의 CNN 역할과 첫 행동을 안내하고 `체험 시작`으로 해당 씬을 로드한다. 안내 중 방 밖으로 나가면 취소되어 재진입 시 다시 설명한다. 오른손 레이는 소개 중 사용할 수 있도록 유지하고 종료 시 원래 상태로 복원한다. 체험 루트는 중앙 복도에서 들어오는 방향(왼쪽 방은 -X, 오른쪽 방은 +X)에 정면이 맞도록 회전하며 사용자 머리를 강제로 회전시키지 않는다. 픽셀 필터의 원래 거치 위치도 같은 회전·이동을 적용한다.

사용자 허가 후 Unity 6000.3.24f1에서 컴파일·Play를 확인했다. 튜토리얼 14페이지 버튼 콜백, XRI activated 콜백을 통한 양피지 수집과 태블릿 1/5·12% 갱신, 세 방의 소개 → 체험 시작 → Additive 로드·이전 체험 언로드를 확인했다. 모델 제작 방에서는 소개 도중 퇴실 시 취소되고 재진입하면 다시 안내되는 것도 확인했다.

검증은 에디터에서 리그 위치를 설정하고 버튼 콜백을 호출했으며, 체험 간 진행에는 기존 개발자 건너뛰기를 사용했다. 실제 Quest 조작으로 모든 체험을 완료한 검증은 아니다. 카메라 렌더로 각 방의 입구 방향 배치를 확인했으며, 공장 흐름에서 로컬 마우스 미리보기가 XR 카메라를 숨기던 문제를 수정했다. Quest에서의 가독성·잡기·성능, APK 빌드·기기 설치는 이번 검증에 포함하지 않았다.

## main 병합 적용 범위 (2026-10-27)

`feature/cnn-model-design-modules`에 `main`을 병합하면서 FactoryEnvironment 씬과 전용 스크립트는 작업 브랜치 버전을 유지했다. 위의 main 측 미션·진행률 갱신, 방 입장 소개, 동적 안내판과 배치 방향 변경 기록은 이력으로 보존하며 이번 병합에서 해당 구현을 이식하지 않았다. 단계 건너뛰기 등 제외한 기능의 재구현은 하지 않았다. 별도 FactoryTutorial과 공용 코드의 main 변경은 병합에 포함된다. 이번 병합은 정적 비교만 수행했으며 위의 과거 실행 검증 결과가 현재 병합본을 검증한 것은 아니다.
