# 공장 환경과 체험 연결

[FactoryEnvironment.unity](../client/Assets/Scenes/FactoryEnvironment.unity)는 공장 환경, 체험 배치 기준점, 수집형 양피지를 담는다. 공장을 유지하면서 픽셀·합성곱, 피처맵, 모델 제작 씬을 순서대로 불러온다. 버튼과 양피지 사용법은 [조작법](xr-simulator.md)을 참고한다.

## 파일과 클래스 역할

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [FactoryLearningFlowManager](../client/Assets/Scripts/FactoryEnvironment/FactoryLearningFlowManager.cs) | 현재 체험 단계와 각 씬의 완료 상태를 읽어 전환 안내판의 사용 가능 여부를 정한다. 전환 요청이 오면 잡기·로딩 상태를 확인하고 다음 씬을 Additive로 열어 공장 기준점에 콘텐츠를 배치한 뒤 이전 체험을 닫는다. 플레이어·안내판 이동과 태블릿 지도 게시도 조율하며 계산·퀴즈 판정은 각 체험 Manager에 맡긴다. |
| [FactoryFlowPortalController](../client/Assets/Scripts/FactoryEnvironment/FactoryFlowPortalController.cs) | Manager가 초기화할 때 전환 안내판과 XRI 선택 버튼을 생성한다. `Show()`로 전달받은 단계 이름·안내·사용 가능 여부를 화면에 반영하고, 허용된 Activate 입력을 받으면 초기화 때 받은 전환 콜백을 호출한다. 어떤 씬을 열지는 Manager가 결정한다. |
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
      → CNNPixelLab
      → CNNFeatureMapLab
      → CNNModelDesignLab
```

`FactoryLearningFlowManager.Start`는 지도를 태블릿에 전달하고 전환 안내판을 생성한다. 이후 `Advance`와 `LoadLesson`이 다음 순서로 체험을 전환한다.

1. 현재 단계의 완료 조건을 확인한다. 로딩 중이거나 XRI Interactor가 물체를 선택한 상태면 전환하지 않는다.
2. `_contentAnchors`의 다음 기준점과 대상 씬 등록 여부를 확인하고 씬을 Additive로 로드한다.
3. `CNNPixelLabManager`, `CNNFeatureMapLabManager`, `CNNModelDesignLabManager` 중 하나가 포함된 루트를 체험 콘텐츠로 찾는다.
4. 콘텐츠 외의 루트와 콘텐츠 내부 조명을 비활성화하고 공장의 바닥·조명을 사용한다. 콘텐츠를 기준점에 배치하고 Rigidbody 위치를 맞춘다. 픽셀 체험은 `RebaseDock`으로 원위치 복귀 기준도 갱신한다.
5. 다음 콘텐츠 준비가 끝나면 이전 체험을 언로드한다. 콘텐츠 루트를 찾지 못하면 새 씬만 언로드하고 기존 체험을 유지한다.
6. 공장 씬을 활성 씬으로 유지하고 안내판과 플레이어를 새 체험 위치로 이동한다. 플레이어 이동에는 카메라의 추적 높이를 보존한다.

| 현재 단계 | 다음 단계로 넘어가는 조건 |
| --- | --- |
| 공장 진입 | 시작 버튼을 사용할 수 있다. |
| 픽셀·합성곱 | `CNNPixelLabManager.ConvolutionCompleted`가 참이어야 한다. |
| 피처맵 | `CNNFeatureMapGuide.ReadyForModelDesign`이 참이어야 한다. |
| 모델 제작 | 공장 Manager가 연결하는 마지막 단계다. |

각 체험의 계산과 입력은 해당 씬의 Manager·Controller가 담당한다. 픽셀 체험의 계산 결과를 피처맵 씬으로 전달하지 않으며, 피처맵은 자체 예제 데이터를 사용한다. 자세한 구현은 [픽셀·합성곱](pixel-inspection.md), [피처맵](cnn-feature-map-explorer.md), [모델 제작](MODEL_DESIGN_API.md)에 정리한다.

공용 [XRPlayerRig](../client/Assets/Prefabs/XR/XRPlayerRig.prefab)는 `PersistentXRPlayerRig`가 유지한다. 공장 Manager가 파괴되면 자신이 로드한 체험 씬을 언로드한다.

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
| 0 | 이미지와 픽셀, 밝기·RGB·채널 | 합성곱 실습실 |
| 1 | 합성곱과 패딩 | MLP 실습실 |
| 2 | 필터와 특징 추출 | MLP 실습실 |
| 3 | 특징 맵 | MLP 실습실 |
| 4 | 활성화 함수와 ReLU | MLP 실습실 |

특징 맵 문서의 표시 전환은 `TabletParchmentController.ToggleFeatureMap`이 삽화 Image와 문구를 갱신하는 기능이다. 모델 계산 데이터에는 영향을 주지 않는다.

## 씬 구성과 에디터 도구

공장 루트는 `Factory Environment`다. 각 실의 `CONTENT ZONE` Transform을 체험 배치 기준으로 사용하며, `FactoryLearningFlowManager._contentAnchors` 배열은 픽셀·피처맵·모델 제작 순서로 연결한다. `_font`는 전환 안내판의 글꼴 참조다.

| 도구 | 역할과 의존성 |
| --- | --- |
| [ParchmentSceneSetup](../client/Assets/Editor/ParchmentSceneSetup.cs) | `Install`에서 공장 씬을 열고 `03 Original image inspection room`, `05 Model assembly room`의 `Learning Parchments`를 구성한다. 문서 인덱스, Sprite·글꼴·Collider·Interactable을 연결하고 공용 리그를 확인한 뒤 씬을 저장한다. |
| [FactoryDecorationMeshCombiner](../client/Assets/Editor/FactoryDecorationMeshCombiner.cs) | 고정 장식의 MeshRenderer를 부모·재질·렌더러 설정별로 묶어 메시 에셋을 만든다. 그룹 크기와 추가 광원 수를 제한하며, 스크립트·물리·프리팹 등 개별 동작에 의존하는 대상은 제외한다. 원본 Renderer는 끄고 Transform을 유지한다. |

양피지는 [LearningParchment.png](../client/Assets/Textures/Common/LearningParchment.png)와 [Nanum Pen Script](../client/Assets/Fonts/NanumPenScript-Regular.ttf)를 참조한다. 본문은 텍스처와 별도로 표시한다. 폰트 배포 조건은 동봉된 [OFL](../client/Assets/Fonts/NanumPenScript-OFL.txt)에 있다.

공장 전용 렌더링 에셋은 [Materials](../client/Assets/Materials/FactoryEnvironment), [Textures](../client/Assets/Textures/FactoryEnvironment), [Meshes](../client/Assets/Meshes/FactoryEnvironment)에 있다.
