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
└─ FeatureMaps/
   ├─ FeatureMapsManager.cs
   ├─ InputMapController.cs
   ├─ ConvolutionSimulation.cs
   ├─ MapDisplayController.cs
   ├─ MapCell.cs
   ├─ MapPanel.cs
   ├─ MapLayer.cs
   └─ Interaction/
      ├─ InteractionManager.cs
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
| `FeatureMaps/Interaction/` | 특징 맵의 하위 기능: 필터 이동·스캔, XR 잡기와 크기 조절 | `CNNFeatureMaps.FeatureMaps.Interaction` |

## 파일과 클래스 역할

체험 흐름은 `CNNFeatureMapLabManager`에서 읽는다. 씬 Manager는 `FeatureMapsManager`, `LessonUIManager` 두 진입점만 참조하고, 단계·채널 전환과 모듈 간 이벤트 연결을 담당한다. `LessonUIManager`가 요청별 이벤트를 제공하고 씬 Manager의 `HandleNextRequested()` 등 전용 처리 함수가 이를 구독한다. `CanPerformAction()`이 준비 여부와 잡기 상태를 확인하며, 단계 전환은 `MoveToPreviousStep()`·`MoveToNextStep()` → `ApplyStep()` 순서다. 단계별 픽셀 처리는 FeatureMapsManager의 `SelectPixel()`, 계산은 ConvolutionSimulation, 화면 표시는 MapDisplayController, 안내 문구와 버튼 표시는 Lesson 내부 담당에서 확인한다.

최상위 모듈은 FeatureMaps와 Lesson이다. Interaction은 FeatureMaps의 하위 기능이며, FeatureMapsManager가 InteractionManager를 통해 잡기·필터와 계산·표시를 연결한다. 씬 Manager와 Lesson은 Interaction을 직접 참조하지 않는다. 잡힌 하위 물체 조회 함수도 FeatureMapsManager가 `Func<Transform, bool>`로 표시 담당에 전달한다. 내부 구현의 씬 Manager 역참조는 없으며, FeatureMaps와 Lesson 사이에는 진입점의 이벤트로 상태·계산 결과를 전달한다. 이 경계는 코드 의존 관계로 유지하며 별도 asmdef로 강제하지는 않는다.

| 파일·클래스 | 역할과 처리 방식 |
|---|---|
| [CNNFeatureMapLabManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapLabManager.cs) | 씬의 단계·채널·마지막 단계 건너뛰기 상태를 관리하고 두 모듈 진입점을 초기화·연결한다. 각 모듈의 이벤트를 다른 진입점의 메서드에 연결하며, 단계 전환과 복원 순서를 조율한다. FeatureMapsManager가 제공하는 입력 수정 횟수와 잡기 상태를 조합해 공장에 다음 체험 진입 가능 여부를 제공한다. |
| [LessonStep.cs](../client/Assets/Scripts/CNNFeatureMapLab/LessonStep.cs) | 네 체험 단계를 `BuildFeatureMap`, `CompareFilters`, `TraceReceptiveField`, `EditInput`으로 정의한다. 씬 Manager가 현재 단계를 관리하며, FeatureMapsManager·MapDisplayController는 단계별 동작·표시에, LessonUIManager는 Content 선택에 사용한다. |
| [LessonUIManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonUIManager.cs) | Lesson UI의 단일 진입점이다. GetContent()가 단계에 맞는 LessonContent를 선택하고 Guide·버튼 묶음·필터 카드에 전달한다. 버튼 묶음의 요청은 Previous·Next·Scan·Filter·Spread·Restore·Enlarge·Shrink별 인자 없는 이벤트로 나누어 제공한다. LessonAction은 Lesson 내부에서만 사용하며 씬 Manager로 전달하지 않는다. 체험 단계와 계산 상태는 소유하지 않는다. |
| [LessonContent.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonContent.cs) | 단계별 제목·설명·기본 안내문, ShowFilterCard, 버튼 상태 사전 Buttons를 담는 데이터 클래스다. 내부 ButtonState는 Available·Label·ScanningLabel을 보관한다. 필터·상황별 문구와 문자열 서식도 제공하며 단계 선택이나 표시 로직은 포함하지 않는다. |
| [LessonGuide.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonGuide.cs) | 제목·설명·결과 TextMesh 3개만 소유한다. ShowContent()에서 전달받은 LessonContent를 표시하고, 상황별 메서드에서 계산값·좌표·횟수를 문자열 서식에 대입한다. Content의 단계 선택은 담당하지 않는다. |
| [LessonButton.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonButton.cs) | Button + Image 루트에서 프리팹에 저장된 LessonAction과 자식 Label의 uGUI Text를 참조한다. Button.onClick을 받아 OnActionRequested 이벤트로 명령을 알린다. 씬 Manager를 참조하지 않는다. 사용 가능 여부는 Button.interactable로 전달하고 호버·눌림·비활성 색상은 Button의 Color Tint 전환이 담당한다. |
| [LessonButtonGroupController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/LessonButtonGroupController.cs) | 프리팹에 배치된 버튼 8개의 요청 이벤트를 모아 상위로 전달한다. 전달받은 LessonContent.Buttons에서 각 버튼의 LessonAction에 해당하는 상태를 읽어 사용 가능 여부와 문구를 적용한다. 스캔 중에는 ScanningLabel을 사용하고 펼치기 버튼에는 다음 층 번호를 대입한다. Label이 null이면 프리팹의 기존 문구를 유지한다. |
| [FilterCardController.cs](../client/Assets/Scripts/CNNFeatureMapLab/Lesson/FilterCardController.cs) | 필터 이름과 가중치 TextMesh를 소유한다. 채널에 맞는 LessonContent 문구를 표시하고 LessonUIManager가 전달한 표시 여부를 적용한다. 패딩 설명은 프리팹의 고정 문구다. |
| [FeatureMapsManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/FeatureMapsManager.cs) | FeatureMaps의 단일 진입점이다. InputMapController·MapDisplayController·하위 InteractionManager를 참조하고 시뮬레이션·방문 기록·입력 수정 횟수를 소유한다. 계산·표시와 조작의 연결, 필터 좌표 적용·연결선 대상 지정·복원 순서를 내부에서 조율한다. 단계 적용·스캔 변경·안내에 필요한 계산 결과를 외부 이벤트로 알린다. |
| [InputMapController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/InputMapController.cs) | 입력 맵 프리팹에서 InputPixels.png를 참조한다. ReadInput()이 읽기 가능 여부와 8×8 크기를 검증하고 위에서 아래 순서의 흑백 픽셀 배열을 직접 반환한다. 읽기에 실패하면 오류를 기록하고 null을 반환한다. 시작과 원본 복원에 같은 변환을 사용한다. |
| [ConvolutionSimulation.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/ConvolutionSimulation.cs) | 전달받은 8×8 픽셀 배열을 복사해 보관하고, 고정 커널로 세 층의 합성곱·ReLU 값과 A/B 반응을 계산한다. 텍스처를 직접 읽지 않는다. 픽셀을 수정하면 전체 결과를 다시 계산하며, 픽셀을 선택하면 그 위치와 연결되는 수용 영역 마스크를 만든다. FeatureMaps 내부에서 값을 읽어 표시하고, 안내에 필요한 결과는 FeatureMapsManager의 이벤트로 전달한다. |
| [MapDisplayController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapDisplayController.cs) | Workspace의 컴포넌트로 층·출력 참조와 하위 셀·맵을 관리한다. `UpdateStepVisibility()`에서 단계별 표시를, `ApplyStepLayout()`·`SpreadNextLayer()`에서 층 펼침과 순서를 제어한다. 단계·채널·필터 위치·방문 배열로 셀의 표시 상태를 정하고 계산값에 따라 색상과 A/B 출력 막대를 갱신한다. |
| [MapCell.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapCell.cs) | 입력 픽셀 또는 특징 맵의 한 칸이다. 층·채널·행·열로 계산값을 조회하고 방문 여부·선택 강조를 합쳐 `MaterialPropertyBlock`으로 색상과 발광을 갱신한다. 격자에서 꺼낸 상태를 기록하며 씬 Manager를 참조하지 않는다. |
| [MapPanel.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapPanel.cs) | 한 채널의 맵에 속한 MapCell를 수집하고 상위 MapLayer의 층 번호와 자신의 채널 번호를 각 MapCell에 전달한다. 맵 전체를 꺼냈는지 기록해 MapLayer가 해당 맵을 자동 정렬할지 판단하게 한다. 맵의 위치가 바뀌어도 MapCell의 계산 인덱스는 유지한다. |
| [MapLayer.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/MapLayer.cs) | 같은 합성곱 층의 MapPanel들을 모아 펼침·접힘 목표 위치로 이동시킨다. 이미 꺼낸 맵은 자동 배치에서 제외한다. 초기화 때 전달받은 `Func<Transform, bool>`로 하위 물체의 잡기 상태를 조회하여 해당 맵의 이동을 기다린다. Interaction 구현 클래스를 직접 참조하지 않는다. |
| [InteractionManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/Interaction/InteractionManager.cs) | FeatureMaps 아래 Interaction 하위 기능의 진입점이다. FeatureMapsManager가 이 컴포넌트를 소유한다. InteractionController와 FilterController를 관리하고 잡기 상태·크기 변경·자세 복원·필터 명령을 제공한다. LateUpdate에서 필터 이동·스캔·연결선을 갱신하고 대상 Transform, 필터 좌표, 스캔 변경, 복원 완료를 이벤트로 알린다. |
| [FilterController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/Interaction/FilterController.cs) | 입력 프리팹 루트의 컴포넌트로 필터 도구와 연결선 재질을 직접 참조한다. 자신의 Transform을 입력 좌표계로 사용해 필터 위치를 행·열로 바꾸고 격자에 맞춘다. InteractionManager에 스캔 위치 변경 여부를 반환하며, 전달받은 연결 대상 Transform을 추적한다. 연결선은 자신의 하위 오브젝트로 생성한다. |
| [InteractionController.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/Interaction/InteractionController.cs) | 초기화 때 Workspace의 잡기 대상을 수집하고 자신을 전달한다. 현재 잡은 대상과 확대·축소 대상을 관리하며, 부모·자식 동시 잡기와 잡는 중 크기 변경을 거절한다. `ResetPoses()`로 초기 자세를 복원하고 잡기·활성화 이벤트를 InteractionManager에 전달한다. |
| [GrabTarget.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/Interaction/GrabTarget.cs) | 노드·맵·층·필터에 Rigidbody와 잡기 컴포넌트를 구성하고 초기 자세를 저장한다. 잡기 허용·선택 시작·해제·활성화는 InteractionController로 전달한다. 자세 복원과 크기 배율을 처리하며 필터는 크기 변경 대상에서 제외한다. 셀·맵·층의 구현 클래스를 참조하지 않는다. |
| [GrabInteractable.cs](../client/Assets/Scripts/CNNFeatureMapLab/FeatureMaps/Interaction/GrabInteractable.cs) | XRI가 잡기 대상을 초기화하기 전에 `GrabTarget`에 지정된 Collider 목록을 사용하도록 한다. 맵·층의 잡기 컴포넌트가 하위 노드의 Collider까지 자동 수집하는 것을 막아, 같은 공간에서도 노드·맵·층을 각각 선택할 수 있게 한다. |

## 초기화와 실행 흐름

1. `CNNFeatureMapLabManager.Awake()`가 FeatureMapsManager를 초기화한다. FeatureMapsManager가 하위 InteractionManager를 초기화하고 잡힌 하위 물체 조회 함수를 MapDisplayController에 전달한다. Interaction 내부는 잡기 대상과 필터를 연결하고, 표시 담당은 셀·맵·층의 계산 인덱스를 연결한다. 입력 맵의 층 인덱스는 `-1`이다. 씬 Manager의 실행 순서 `-100`으로 자식 잡기 컴포넌트보다 먼저 참조를 연결한다.
2. 씬 Manager의 `OnEnable()`은 FeatureMaps와 Lesson 사이의 이벤트를 연결하고, FeatureMapsManager의 `OnEnable()`은 Interaction 이벤트를 내부 처리 메서드에 연결한다. 각각 `OnDisable()`에서 해제한다. 버튼 요청은 `LessonButton → LessonButtonGroupController → LessonUIManager의 요청별 이벤트 → CNNFeatureMapLabManager의 요청별 처리 함수` 순서다. 예를 들어 다음 버튼은 `OnNextRequested → HandleNextRequested()`로 연결된다. 씬 Manager는 LessonAction과 버튼 구현을 참조하지 않는다. 잡기·활성화는 `GrabTarget → InteractionController → InteractionManager → FeatureMapsManager` 내부에서 처리한다.
3. 씬 Manager의 `Start()`가 `FeatureMapsManager.TryStart()`로 입력 배열과 시뮬레이션을 준비하고 첫 단계를 적용한다. 입력 준비에 실패하면 체험을 시작하지 않는다. InputMapController가 검증하는 입력은 Read/Write가 활성화된 8×8 텍스처다.
4. 첫 두 단계에서 InteractionManager의 `LateUpdate()`가 필터 이동·스캔을 처리한다. FeatureMapsManager는 좌표 변경을 받아 방문 기록·선택 영역·출력 셀·계산 결과를 갱신하고 연결선 대상을 InteractionManager에 지정한다. 외부로는 계산 결과와 스캔 변경을 알리며, 씬 Manager가 이를 LessonUIManager에 연결한다.
5. 입력 픽셀 활성화는 FeatureMapsManager의 단계 분기로 이어진다. 필터 단계에서는 하위 InteractionManager에 위치를 지정하고, 수용 영역 단계에서는 영향 범위를 표시하며, 입력 변경 단계에서는 전체 층을 다시 계산한다.

InteractionController와 ConvolutionSimulation은 일반 C# 객체다. 두 모듈 진입점과 하위 InteractionManager·MapDisplayController·FilterController·InputMapController는 프리팹에 연결된 컴포넌트다. 씬 Manager는 FeatureMapsManager와 LessonUIManager만 참조한다.

## Lesson UI의 데이터와 표시

`LessonUIManager.ShowLesson()`은 `GetContent(step)`으로 단계별 LessonContent를 한 번 선택한다. 선택한 Content는 `LessonGuide.ShowContent()`와 `LessonButtonGroupController.UpdateButtons()`에 전달하고, `ShowFilterCard`는 FilterCardController의 표시 여부로 전달한다. 버튼만 갱신할 때도 LessonUIManager가 Content를 선택하므로 Guide와 버튼 묶음에서 단계를 다시 판단하지 않는다.

LessonContent의 `Buttons`는 LessonAction을 키로 사용하는 상태 사전이다. 버튼 묶음은 각 ButtonState의 사용 가능 여부와 문구를 적용하며, 스캔 중 문구와 펼칠 층 번호만 현재 상태에 맞춰 갱신한다. 필터 단계의 기본 Readout은 null이므로 Guide는 기본 Content 표시 시 결과 문구를 덮어쓰지 않고, 뒤이어 전달되는 `ShowPatch()`로 계산 안내를 갱신한다.

## 단계와 상태

씬 Manager의 `_step`은 `LessonStep`이다. `ApplyStep()`은 FeatureMapsManager에 단계와 채널을 전달한다. FeatureMapsManager의 `ShowStep()`이 스캔 중지·맵 표시·필터 표시 여부를 적용한 뒤 `OnStepApplied` 이벤트를 발생시킨다. 이 이벤트에 연결된 LessonUIManager가 기본 안내·버튼·카드를 갱신한다. 이후 두 필터 단계에서는 현재 위치의 패치를 적용해 계산 안내를 갱신한다. 기본 안내가 계산 안내를 덮어쓰지 않도록 이 순서를 유지한다.

| 단계 | 픽셀 입력 처리 | 계산·표시 동작 |
|---|---|---|
| `BuildFeatureMap` — 특징 맵 만들기 | 필터 위치 요청 → `SelectFilterPosition()` → `ApplyPatch()` | 입력과 선택한 첫 층 채널을 표시한다. `_visited[channel, row, column]`으로 방문한 출력 위치를 구분한다. |
| `CompareFilters` — 필터 비교 | 필터 위치 요청 → `SelectFilterPosition()` → `ApplyPatch()` | 첫 층의 두 맵을 펼치고 선택 채널의 필터와 출력 위치를 표시한다. |
| `TraceReceptiveField` — 수용 영역 추적 | FeatureMapsManager의 `SelectPixel()` 수용 영역 분기 | 세 층을 표시한다. 선택한 입력 픽셀을 포함하는 수용 영역을 층별로 강조한다. |
| `EditInput` — 입력 변경 | FeatureMapsManager의 `SelectPixel()` 입력 변경 분기 | 입력 픽셀을 0 또는 1로 바꾸고 전체 특징 맵과 A/B 반응을 다시 표시한다. |

씬 Manager는 `_step`, `_channel`, `_lastStepSkipped`를 관리한다. FeatureMapsManager는 전달받은 단계·채널과 시뮬레이션·방문 배열·`EditCount`를 관리하며, 필터의 행·열과 스캔 상태는 Interaction 내부에서 관리한다. 다음에 펼칠 층은 FeatureMapsManager의 `NextSpreadLayer`로 읽어 버튼에 전달한다. `Restore` 명령은 FeatureMapsManager.TryRestore()에 원본 입력·방문 기록·수정 횟수 초기화와 배치 복원을 요청한다. FeatureMapsManager 내부에서 층 애니메이션 정리 → InteractionManager의 자세 복원 → 복원 완료 이벤트에 따른 셀·맵 분리 상태 초기화 순서를 처리하며, 성공하면 씬 Manager가 현재 단계를 다시 적용한다.

마지막 단계의 다음 버튼은 `단계 마치기`로 표시된다. 입력을 수정하지 않고 이 버튼을 누르면 `_lastStepSkipped`가 참이 된다. 공장 진행에서 읽는 `ReadyForModelDesign`은 초기화 완료, 마지막 단계, 입력 수정 1회 이상 또는 건너뛰기, 잡은 대상 없음이 모두 충족될 때 참이다. FactoryLearningFlowManager가 CNNFeatureMapLabManager에서 이 값을 직접 읽는다. 공장 씬 전환은 [공장 환경 구현](factory-environment.md)에서 다룬다.

## 계산 데이터

`InputMapController.ReadInput()`이 Unity 텍스처의 아래에서 위로 향하는 행을 뒤집어, 위에서 아래로 읽는 `input[row, column]` 배열을 만든다. `ConvolutionSimulation`의 생성자와 `RestoreInput()`은 8×8 배열을 받아 내부 `_input`에 복사한다. 시뮬레이션의 픽셀 수정은 전달된 원본 배열에 영향을 주지 않는다.

- 입력 크기는 8×8이고 합성곱 층의 출력 채널은 `2 → 3 → 5`다. 각 층은 3×3 커널, stride 1, zero padding, ReLU를 적용해 공간 크기를 유지한다.
- `ReadKernelWeight()`는 채널 번호를 기준으로 좌우 변화, 위아래 변화, 평균 필터를 반복 선택한다. 첫 층의 두 필터는 각각 `dx / 3`, `dy / 3`이다.
- `ComputeConvolutions()`는 이전 층의 모든 입력 채널을 합산하고 입력 채널 수로 정규화한다. 경계 밖의 항은 건너뛰어 0 패딩으로 처리한다.
- `SelectPixel()`은 가중치와 활성값에 관계없이 연결되는 공간 위치를 다음 층으로 전파한다. 강조는 구조적 수용 영역을 뜻하며, 실제 값의 변화량이나 중요도를 계산한 결과가 아니다.
- `CountAffectedPositions(0)`은 선택 후 첫 층의 영향 마스크에서 공간 위치 수를 센다. 수용 영역 안내는 이 값을 사용하며 채널 수를 곱하지 않는다.
- `ToggleInput()`은 선택값이 0.5 이상이면 0, 미만이면 1로 바꾼다. 복원 시 FeatureMapsManager가 InputMapController에서 원본 배열을 다시 받아 `RestoreInput()`에 전달한다. 입력 읽기에 실패하면 기존 계산값과 체험 기록을 유지한다.
- `ReadOutput()`은 마지막 층의 채널 평균을 두 점수로 합산한 뒤 로지스틱 함수로 A/B 반응을 만든다. 학습된 분류 모델의 클래스 확률은 아니다.

첫 단계의 미방문 노드는 표시만 어둡게 처리한다. 계산 결과는 데이터 생성 시 전체 위치에 대해 보유한다. 방문 기록은 두 필터 단계에서 필터를 직접 이동하거나 입력 픽셀을 선택하거나 자동 이동할 때 모두 누적되며, 첫 단계의 누적 표시와 선택 채널의 `체험 위치 수 / 64` 안내에 사용한다. 두 필터 단계 적용 시 현재 필터 위치도 기록한다. 복원으로 기록을 지운 뒤 현재 단계를 다시 적용하면 두 필터 단계에서는 현재 위치 한 칸이 다시 기록된다. 노드·맵·층의 Transform을 옮겨도 계산 인덱스와 연결 관계는 바뀌지 않는다.

`처음 상태` 버튼은 입력값·방문 기록·수정 횟수와 물체의 위치·회전·크기를 복원하고 현재 단계·채널을 다시 표시한다. 첫 단계로 이동하는 버튼은 아니다. 현재 `TryRestore()`는 원본 텍스처를 다시 읽어 배열로 변환하며, 텍스처 자체는 픽셀 수정으로 변경되지 않는다.

## 잡기와 배치 처리

`GrabTarget`은 `Rigidbody`와 `GrabInteractable`을 구성하고 InteractionController의 잡기 허용 여부를 XRI Select Filter로 연결한다. 선택 시작 시 잡기 상태를 기록하고 InteractionManager가 대상 Transform을 이벤트로 알린다. 이 이벤트에 연결된 FeatureMapsManager가 해당 셀·맵을 분리 상태로 표시한다. 해제 시에는 Interaction 내부에서 크기 변경 대상을 갱신한다. 필터는 회전 추적과 크기 변경 대상에서 제외한다.

`InteractionController.CanGrab()`은 이미 잡은 대상의 부모나 자식을 동시에 잡지 못하게 한다. Manager는 잡은 대상이 있는 동안 안내 버튼 명령을 처리하지 않는다. `MapLayer.Update()`는 분리되지 않은 맵만 목표 배치로 보간하고, 전달받은 조회 함수로 하위 물체가 잡혀 있는지 확인해 해당 맵의 이동을 기다린다.

## 프리팹과 참조

[CNNFeatureMapWorkspace.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/CNNFeatureMapWorkspace.prefab)이 입력·층·출력·안내 UI를 조립한다. 아래 프리팹은 모두 `client/Assets/Prefabs/CNNFeatureMapLab/`에 있다.

| 프리팹 | 역할 |
|---|---|
| `CNNFeatureMapWorkspace` | 씬 Manager가 같은 루트의 FeatureMapsManager·LessonUIManager를 참조한다. FeatureMapsManager는 입력·표시 담당과 하위 InteractionManager를, InteractionManager는 필터 담당을, LessonUIManager는 Guide·버튼 묶음·필터 카드를 참조한다. MapDisplayController는 층·출력 참조를, LessonGuide는 주요 안내문 3개를 소유한다. |
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

입력 이미지는 `CNNFeatureMapInput` 루트의 InputMapController `_inputImage`에 지정한다. Workspace의 FeatureMapsManager `_inputMap`은 해당 입력 프리팹 인스턴스의 InputMapController를, `_display`는 Workspace의 MapDisplayController를 참조한다. InteractionManager `_filter`는 입력 프리팹 인스턴스의 FilterController를 참조한다. FeatureMapsManager의 `_interaction`을 하위 InteractionManager에 연결하고, 씬 Manager의 `_featureMaps`·`_lessonUI`는 두 모듈 진입점에 연결한다.

Lesson UI의 위치·회전·크기·폰트 재질은 프리팹에 저장한다. 입력·층·출력 이름표, 도움말, 패딩 설명은 런타임 코드에서 참조하거나 크기를 다시 조절하지 않는다. 주요 안내문·버튼·필터 카드도 실행 중에는 문자열과 표시 상태만 갱신하며, 버튼 생성이나 텍스트 복제·자동 크기 조절을 수행하지 않는다. 주요 안내문과 필터 카드·고정 캡션의 TextMesh는 유지하고, 버튼의 글자만 uGUI Text로 구성한다.
