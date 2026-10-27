# CNN 특징 맵 구현

[CNNFeatureMapLab.unity](../client/Assets/Scenes/CNNFeatureMapLab.unity)는 고정 필터로 특징 맵을 계산하고, 입력 픽셀의 수용 영역과 입력 변경에 따른 결과를 보여준다. 런타임 코드는 `client/Assets/Scripts/CNNFeatureMapLab/`에 있으며, 전체 조율은 `CNNFeatureMaps`, 하위 기능은 폴더에 맞춘 네임스페이스를 사용한다. 컨트롤러 입력과 체험 버튼 사용법은 [조작법](xr-simulator.md)을 참고한다.

## 폴더 구성

```text
CNNFeatureMapLab/
├─ CNNFeatureMapLabManager.cs
├─ LessonStep.cs
├─ Lesson/
│  ├─ LessonUIManager.cs
│  ├─ LessonContent.cs
│  ├─ LessonGuide.cs
│  ├─ LessonButton.cs
│  ├─ LessonButtonGroupController.cs
│  └─ FilterCardController.cs
├─ FeatureMaps/
│  ├─ InputMapController.cs
│  ├─ ConvolutionSimulation.cs
│  ├─ MapDisplayController.cs
│  ├─ MapCell.cs
│  ├─ MapPanel.cs
│  └─ MapLayer.cs
└─ Interaction/
   ├─ FilterController.cs
   ├─ InteractionController.cs
   ├─ GrabTarget.cs
   └─ GrabInteractable.cs
```

| 위치 | 역할 | 네임스페이스 |
|---|---|---|
| 루트 Manager·LessonStep | 전체 단계 정의·진행 상태와 기능 호출 순서 조율 | `CNNFeatureMaps` |
| `Lesson/` | 안내문, 버튼 입력과 표시 | `CNNFeatureMaps.Lesson` |
| `FeatureMaps/` | 특징 맵 계산, 노드·맵·층 구성과 시각화 | `CNNFeatureMaps.FeatureMaps` |
| `Interaction/` | 필터 이동·스캔, XR 잡기와 크기 조절 | `CNNFeatureMaps.Interaction` |

## 파일과 클래스 역할

체험 흐름은 `CNNFeatureMapLabManager`에서 읽는다. `Perform()`은 버튼 명령을, `SelectPixel()`은 단계별 픽셀 입력을 분기한다. 단계 전환은 `MoveToPreviousStep()`·`MoveToNextStep()` → `ApplyStep()` 순서다. 계산의 세부 내용은 ConvolutionSimulation, 화면의 표시 조건은 MapDisplayController, 단계별 문구 데이터와 문자열 서식은 LessonContent, 주요 안내문 선택·값 대입은 LessonGuide, 버튼 묶음은 LessonButtonGroupController, 필터 카드는 FilterCardController에서 확인한다.

| 파일·클래스 | 역할과 처리 방식 |
|---|---|
| [CNNFeatureMapLabManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapLabManager.cs) | 씬의 노드·층·안내판을 모으고 InputMapController가 제공한 픽셀 배열로 시뮬레이션을 생성한다. 단계 전환과 단계별 입력 처리 메서드에서 FilterController·MapDisplayController·LessonUIManager를 조율한다. 마지막 단계의 입력 수정 또는 건너뛰기 여부와 잡기 상태를 조합해 공장에 다음 체험 진입 가능 여부를 제공한다. |
| [LessonStep.cs](../client/Assets/Scripts/CNNFeatureMapLab/LessonStep.cs) | 네 체험 단계를 `BuildFeatureMap`, `CompareFilters`, `TraceReceptiveField`, `EditInput`으로 정의한다. Manager가 현재 단계를 관리하고 LessonGuide·MapDisplayController가 전달받아 사용한다. |
| [LessonUIManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonUIManager.cs) | Lesson UI의 단일 진입점이다. 씬 Manager가 전달한 상태로 Guide·버튼 묶음·필터 카드를 갱신하고, 버튼 묶음의 요청 이벤트를 씬 Manager로 전달한다. 체험 단계와 계산 상태는 소유하지 않는다. |
| [LessonContent.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonContent.cs) | 제목·설명·안내문을 담는 데이터 클래스이며 단계별 객체를 제공한다. 필터·버튼·상황별 문구와 문자열 서식도 보관한다. 단계 판단이나 문구 선택·값 대입 로직은 포함하지 않는다. |
| [LessonGuide.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonGuide.cs) | 제목·설명·결과 TextMesh 3개만 소유한다. 단계와 상황에 맞는 LessonContent를 선택하고 계산값·좌표·횟수를 대입해 안내문을 갱신한다. |
| [LessonButton.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonButton.cs) | Button + Image 루트에서 프리팹에 저장된 LessonAction과 자식 Label의 uGUI Text를 참조한다. Button.onClick을 받아 OnActionRequested 이벤트로 명령을 알린다. 씬 Manager를 참조하지 않는다. 사용 가능 여부는 Button.interactable로 전달하고 호버·눌림·비활성 색상은 Button의 Color Tint 전환이 담당한다. |
| [LessonButtonGroupController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonButtonGroupController.cs) | 프리팹에 배치된 버튼 8개의 요청 이벤트를 모아 상위로 전달하고, 전달받은 단계·스캔 상태·펼칠 층에 맞춰 각 버튼의 문구와 사용 가능 상태를 갱신한다. 버튼 배열 순서 대신 각 버튼의 LessonAction으로 분기한다. |
| [FilterCardController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/FilterCardController.cs) | 필터 이름과 가중치 TextMesh를 소유한다. 채널에 맞는 LessonContent 문구를 표시하고 LessonUIManager가 전달한 표시 여부를 적용한다. 패딩 설명은 프리팹의 고정 문구다. |
| [InputMapController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/InputMapController.cs) | 입력 맵 프리팹에서 InputPixels.png를 참조한다. TryReadInput()이 읽기 가능 여부와 8×8 크기를 검증하고 위에서 아래 순서의 흑백 픽셀 배열을 제공한다. 시작과 원본 복원에 같은 변환을 사용한다. |
| [ConvolutionSimulation.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/ConvolutionSimulation.cs) | 전달받은 8×8 픽셀 배열을 복사해 보관하고, 고정 커널로 세 층의 합성곱·ReLU 값과 A/B 반응을 계산한다. 텍스처를 직접 읽지 않는다. 픽셀을 수정하면 전체 결과를 다시 계산하며, 픽셀을 선택하면 그 위치와 연결되는 수용 영역 마스크를 만든다. MapCell과 안내판은 이 객체에서 값·가중치·강조 여부를 읽어 표시한다. |
| [MapDisplayController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapDisplayController.cs) | Workspace의 컴포넌트로 층·출력 참조와 하위 셀·맵을 관리한다. `UpdateStepVisibility()`에서 단계별 표시를, `ApplyStepLayout()`·`SpreadNextLayer()`에서 층 펼침과 순서를 제어한다. 단계·채널·필터 위치·방문 배열로 셀의 표시 상태를 정하고 계산값에 따라 색상과 A/B 출력 막대를 갱신한다. |
| [MapCell.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapCell.cs) | 입력 픽셀 또는 특징 맵의 한 칸이다. 층·채널·행·열로 계산값을 조회하고 방문 여부·선택 강조를 합쳐 `MaterialPropertyBlock`으로 색상과 발광을 갱신한다. 입력 노드 선택은 Manager로 전달하며, 격자에서 꺼낸 상태는 별도로 기록한다. |
| [MapPanel.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapPanel.cs) | 한 채널의 맵에 속한 MapCell를 수집하고 상위 MapLayer의 층 번호와 자신의 채널 번호를 각 MapCell에 전달한다. 맵 전체를 꺼냈는지 기록해 MapLayer가 해당 맵을 자동 정렬할지 판단하게 한다. 맵의 위치가 바뀌어도 MapCell의 계산 인덱스는 유지한다. |
| [MapLayer.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapLayer.cs) | 같은 합성곱 층의 MapPanel들을 모아 펼침·접힘 목표 위치로 이동시킨다. 이미 꺼낸 맵은 자동 배치에서 제외하고, 하위 물체를 잡고 있으면 그 맵의 이동을 기다린다. 층 단위 배치와 개별 맵·노드의 직접 이동이 충돌하지 않게 한다. |
| [FilterController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Interaction/FilterController.cs) | 입력 프리팹 루트의 컴포넌트로 필터 도구와 연결선 재질을 직접 참조한다. 초기화 때 입력 좌표계와 표시 담당의 셀 목록을 받는다. 필터 위치를 입력 평면 기준 행·열로 바꾸고 격자에 맞추며, 스캔 위치 변경 여부를 Manager에 반환한다. 연결선은 자신의 하위 오브젝트로 생성한다. |
| [InteractionController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Interaction/InteractionController.cs) | 초기화 때 Workspace의 잡기 대상을 수집하고 자신을 전달한다. 현재 잡은 대상과 확대·축소 대상을 관리하며, 부모·자식 동시 잡기와 잡는 중 크기 변경을 거절한다. `ResetPoses()`로 대상들의 초기 자세를 복원한다. Manager와 MapLayer는 이 객체의 상태를 읽어 버튼 실행이나 자동 배치를 보류한다. |
| [GrabTarget.cs](../client/Assets/Scripts/CNNFeatureMapLab/Interaction/GrabTarget.cs) | 노드·맵·층·필터에 Rigidbody와 잡기 컴포넌트를 구성하고 초기 자세를 저장한다. 잡기 허용·선택 시작·해제·크기 변경 대상 선택은 InteractionController를 직접 호출한다. 셀·맵의 분리 상태, 초기 자세 복원과 크기 배율도 처리하며 필터는 크기 변경 대상에서 제외한다. 입력 픽셀 선택은 MapCell을 통해 Manager의 단계별 처리로 전달한다. |
| [GrabInteractable.cs](../client/Assets/Scripts/CNNFeatureMapLab/Interaction/GrabInteractable.cs) | XRI가 잡기 대상을 초기화하기 전에 `GrabTarget`에 지정된 Collider 목록을 사용하도록 한다. 맵·층의 잡기 컴포넌트가 하위 노드의 Collider까지 자동 수집하는 것을 막아, 같은 공간에서도 노드·맵·층을 각각 선택할 수 있게 한다. |

## 초기화와 실행 흐름

1. `CNNFeatureMapLabManager.Awake()`가 InteractionController와 프리팹의 MapDisplayController를 초기화한다. InteractionController는 잡기 대상들을 연결하고, MapDisplayController는 셀·맵을 수집해 초기화하고 층에 InteractionController를 전달한다. 각 MapPanel은 층·채널을 하위 MapCell에 전달한다. 입력 맵의 층 인덱스는 `-1`이다. Manager의 실행 순서 `-100`으로 자식 잡기 컴포넌트의 초기화보다 먼저 참조를 연결한다.
2. `Start()`가 `_inputMap.TryReadInput()`으로 입력 배열을 받아 `ConvolutionSimulation`을 만든다. InputMapController가 자신의 `_inputImage`를 검증하고 변환한다. 입력은 Read/Write가 활성화된 8×8 텍스처여야 한다. 입력 준비에 실패하면 체험을 시작하지 않는다.
3. Manager가 프리팹의 FilterController에 InputMapController의 Transform과 MapDisplayController의 셀 목록을 전달해 초기화하고 첫 단계를 적용한다. LessonUIManager가 전달받은 상태로 Guide·버튼 묶음·필터 카드를 갱신한다. 버튼 요청은 `LessonButton → LessonButtonGroupController → LessonUIManager → CNNFeatureMapLabManager.Perform()` 순서로 전달한다. 각 구독자는 `OnEnable()`에서 이벤트를 구독하고 `OnDisable()`에서 해제한다.
4. 첫 두 단계의 `LateUpdate()`에서 필터 이동·스캔으로 행·열이 바뀌면 `ApplyPatch()`를 호출한다. Manager가 방문 기록을 저장하고 선택 영역, 출력 뉴런, 합성곱 합과 ReLU 결과를 갱신한다.
5. 입력 픽셀 선택은 `SelectPixel()`의 단계 분기에 따라 `SelectFilterPosition()`, `SelectReceptiveField()`, `EditInputPixel()`로 전달된다. 입력값을 바꾸면 전체 층을 다시 계산한다.

InteractionController와 ConvolutionSimulation은 일반 C# 객체다. MapDisplayController·FilterController·InputMapController는 프리팹에 연결된 컴포넌트다. Manager는 기능 담당과 LessonUIManager를 참조하고, 각 기능의 Renderer·Transform·이미지·재질 참조는 해당 담당이 소유한다.

## 단계와 상태

Manager의 `_step`은 `LessonStep`이다. `IsFilterStep`은 특징 맵 만들기와 필터 비교 단계를 뜻한다. `ApplyStep()`은 스캔·선택을 정리하고 MapDisplayController의 콘텐츠 표시와 필터 도구·노드 상태를 갱신한다. Lesson UI는 LessonUIManager.ShowLesson()에 단계·채널·펼칠 층·스캔 상태를 전달하여 주요 안내문·필터 카드·버튼을 함께 갱신한다. 필터 비교에서는 첫 층을 펼치고, 두 필터 단계에서는 현재 위치의 패치를 적용한다.

| 단계 | 픽셀 입력 처리 | 계산·표시 동작 |
|---|---|---|
| `BuildFeatureMap` — 특징 맵 만들기 | `SelectFilterPosition()` → `ApplyPatch()` | 입력과 선택한 첫 층 채널을 표시한다. `_visited[channel, row, column]`으로 방문한 출력 위치를 구분한다. |
| `CompareFilters` — 필터 비교 | `SelectFilterPosition()` → `ApplyPatch()` | 첫 층의 두 맵을 펼치고 선택 채널의 필터와 출력 위치를 표시한다. |
| `TraceReceptiveField` — 수용 영역 추적 | `SelectReceptiveField()` | 세 층을 표시한다. 선택한 입력 픽셀을 포함하는 수용 영역을 층별로 강조한다. |
| `EditInput` — 입력 변경 | `EditInputPixel()` | 입력 픽셀을 0 또는 1로 바꾸고 전체 특징 맵과 A/B 반응을 다시 표시한다. |

`_step`, `_channel`, `_visited`, `_editCount`, `_lastStepSkipped`는 Manager가 관리하고, 필터의 행·열과 스캔 상태는 FilterController가 관리한다. 다음에 펼칠 층은 MapDisplayController의 `NextSpreadLayer`에서 읽어 버튼에 전달한다. `Restore` 명령은 원본 입력과 체험 기록을 초기화한 뒤 `ResetLayout()`을 호출한다. 이 메서드는 표시 담당의 층 애니메이션 정리 → 상호작용 담당의 자세 복원 → 현재 단계 재적용 순서를 조율한다. `ResetLayout()`만 호출하면 입력값과 체험 기록은 유지한다.

마지막 단계의 다음 버튼은 `단계 마치기`로 표시된다. 입력을 수정하지 않고 이 버튼을 누르면 `_lastStepSkipped`가 참이 된다. 공장 진행에서 읽는 `ReadyForModelDesign`은 초기화 완료, 마지막 단계, 입력 수정 1회 이상 또는 건너뛰기, 잡은 대상 없음이 모두 충족될 때 참이다. FactoryLearningFlowManager가 CNNFeatureMapLabManager에서 이 값을 직접 읽는다. 공장 씬 전환은 [공장 환경 구현](factory-environment.md)에서 다룬다.

## 계산 데이터

`InputMapController.TryReadInput()`이 Unity 텍스처의 아래에서 위로 향하는 행을 뒤집어, 위에서 아래로 읽는 `input[row, column]` 배열을 만든다. `ConvolutionSimulation`의 생성자와 `RestoreInput()`은 8×8 배열을 받아 내부 `_input`에 복사한다. 시뮬레이션의 픽셀 수정은 전달된 원본 배열에 영향을 주지 않는다.

- 입력 크기는 8×8이고 합성곱 층의 출력 채널은 `2 → 3 → 5`다. 각 층은 3×3 커널, stride 1, zero padding, ReLU를 적용해 공간 크기를 유지한다.
- `ReadKernelWeight()`는 채널 번호를 기준으로 좌우 변화, 위아래 변화, 평균 필터를 반복 선택한다. 첫 층의 두 필터는 각각 `dx / 3`, `dy / 3`이다.
- `ComputeConvolutions()`는 이전 층의 모든 입력 채널을 합산하고 입력 채널 수로 정규화한다. 경계 밖의 항은 건너뛰어 0 패딩으로 처리한다.
- `SelectPixel()`은 가중치와 활성값에 관계없이 연결되는 공간 위치를 다음 층으로 전파한다. 강조는 구조적 수용 영역을 뜻하며, 실제 값의 변화량이나 중요도를 계산한 결과가 아니다.
- `CountAffectedPositions(0)`은 선택 후 첫 층의 영향 마스크에서 공간 위치 수를 센다. 수용 영역 안내는 이 값을 사용하며 채널 수를 곱하지 않는다.
- `ToggleInput()`은 선택값이 0.5 이상이면 0, 미만이면 1로 바꾼다. 복원 시 Manager가 InputMapController에서 원본 배열을 다시 받아 `RestoreInput()`에 전달한다. 입력 읽기에 실패하면 기존 계산값과 체험 기록을 유지한다.
- `ReadOutput()`은 마지막 층의 채널 평균을 두 점수로 합산한 뒤 로지스틱 함수로 A/B 반응을 만든다. 학습된 분류 모델의 클래스 확률은 아니다.

첫 단계의 미방문 노드는 표시만 어둡게 처리한다. 계산 결과는 데이터 생성 시 전체 위치에 대해 보유한다. 노드·맵·층의 Transform을 옮겨도 계산 인덱스와 연결 관계는 바뀌지 않는다.

## 잡기와 배치 처리

`GrabTarget`은 `Rigidbody`와 `GrabInteractable`을 구성하고 InteractionController의 잡기 허용 여부를 XRI Select Filter로 연결한다. 선택 시작 시 MapCell과 MapPanel을 분리 상태로 표시하며, 해제 시 크기 변경 대상을 갱신한다. 필터는 회전 추적과 크기 변경 대상에서 제외한다.

`InteractionController.CanGrab()`은 이미 잡은 대상의 부모나 자식을 동시에 잡지 못하게 한다. Manager는 잡은 대상이 있는 동안 안내 버튼 명령을 처리하지 않는다. `MapLayer.Update()`는 분리되지 않은 맵만 목표 배치로 보간하고, 하위 물체가 잡혀 있으면 해당 맵의 이동을 기다린다.

## 프리팹과 참조

[CNNFeatureMapWorkspace.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/CNNFeatureMapWorkspace.prefab)이 입력·층·출력·안내 UI를 조립한다. 아래 프리팹은 모두 `client/Assets/Prefabs/CNNFeatureMapLab/`에 있다.

| 프리팹 | 역할 |
|---|---|
| `CNNFeatureMapWorkspace` | Manager가 입력·표시·필터 담당과 LessonUIManager를 참조한다. 같은 루트의 MapDisplayController가 층·출력 참조를, LessonUIManager가 Guide·버튼 묶음·필터 카드 참조를 소유한다. LessonGuide는 주요 안내문 3개를 참조한다. |
| `CNNFeatureMapInput` | 입력 노드 64개, 입력 손잡이, 필터 도구를 구성한다. 루트의 InputMapController가 이미지를, FilterController가 하위 필터 잡기 대상과 연결선 재질을 참조한다. |
| `CNNFeatureMapInputPixel` | 입력 픽셀의 MapCell와 잡기 콜라이더를 구성한다. |
| `CNNFeatureMapNeuron` | 특징 맵의 개별 MapCell와 잡기 콜라이더를 구성한다. |
| `CNNFeatureMapPanel` | 뉴런 64개와 맵 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer2Channels` | 첫 층의 맵 2개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer3Channels` | 두 번째 층의 맵 3개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer5Channels` | 세 번째 층의 맵 5개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapOutput` | A/B 반응을 표시하는 출력 Renderer와 캡션이다. |
| `CNNFeatureMapLessonControls` | 주요 안내문, 고정 도움말, 확대·축소를 포함한 버튼 8개와 LessonButtonGroupController를 미리 배치한다. 월드 공간 Canvas 아래 CNNFeatureMapLessonButton 인스턴스 8개를 사용하며, 각 인스턴스는 명령·글자·위치를 재정의한다. Canvas에는 TrackedDeviceGraphicRaycaster와 GraphicRaycaster를 저장해 공용 XR UI 입력과 마우스 입력을 받는다. |
| `CNNFeatureMapLessonButton` | RectTransform 루트에 LessonButton·Button·Image·CanvasRenderer를 두고 자식 Label에 uGUI Text를 배치한다. 배경과 글자는 한 버튼 계층으로 이동하며, 메시·Collider·XRSimpleInteractable을 사용하지 않는다. |
| `CNNFeatureMapFilter` | 필터 도구와 잡기 대상을 구성한다. |
| `CNNFeatureMapFilterCard` | FilterCardController가 필터 이름·가중치를 표시하고 패딩 안내는 고정 캡션으로 배치한다. |

MapCell의 `_row`·`_column`, MapPanel의 `_channel`, MapLayer의 `_layerIndex`가 계산 배열과의 대응을 결정한다. 각 LessonButton의 `_action`에 `Previous`, `Next`, `Scan`, `Filter`, `Spread`, `Restore`, `Enlarge`, `Shrink` 중 해당 명령을 지정하고 `_label`에 자식 uGUI Text를 연결한다. LessonButtonGroupController의 `_buttons`는 이 8개 버튼을 참조한다. MapDisplayController의 `_outputs`·`_outputRoots`는 A/B 순서, `_layerRoots`는 첫 층부터 순서대로 지정한다. 각 층 루트에는 MapLayer가 있어야 한다.

공용 XR 입력과 태블릿은 `client/Assets/Prefabs/XR/XRPlayerRig.prefab`을 사용한다. 입력 텍스처와 재질은 각각 `client/Assets/Textures/CNNFeatureMapLab/`, `client/Assets/Materials/CNNFeatureMapLab/`에 있다.

입력 이미지는 `CNNFeatureMapInput` 루트의 InputMapController `_inputImage`에 지정한다. Workspace의 Manager `_inputMap`은 해당 입력 프리팹 인스턴스의 컴포넌트를 참조한다.

Lesson UI의 위치·회전·크기·폰트 재질은 프리팹에 저장한다. 입력·층·출력 이름표, 도움말, 패딩 설명은 런타임 코드에서 참조하거나 크기를 다시 조절하지 않는다. 주요 안내문·버튼·필터 카드도 실행 중에는 문자열과 표시 상태만 갱신하며, 버튼 생성이나 텍스트 복제·자동 크기 조절을 수행하지 않는다. 주요 안내문과 필터 카드·고정 캡션의 TextMesh는 유지하고, 버튼의 글자만 uGUI Text로 구성한다.
