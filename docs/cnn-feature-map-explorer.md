# CNN 특징 맵 구현

[CNNFeatureMapLab.unity](../client/Assets/Scenes/CNNFeatureMapLab.unity)는 고정 필터로 특징 맵을 계산하고, 입력 픽셀의 수용 영역과 입력 변경에 따른 결과를 보여준다. 런타임 코드는 `client/Assets/Scripts/CNNFeatureMapLab/`의 `CNNFeatureMaps` 네임스페이스에 있다. 컨트롤러 입력과 체험 버튼 사용법은 [조작법](xr-simulator.md)을 참고한다.

## 파일과 클래스 역할

| 파일·클래스 | 역할과 처리 방식 |
|---|---|
| [CNNFeatureMapLabManager.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapLabManager.cs) | 씬의 노드·층·안내판을 모으고 입력 텍스처로 계산 데이터를 만든다. 버튼 명령과 픽셀 선택을 받아 현재 단계·필터 채널·방문 기록을 바꾸고, FilterController와 DisplayController를 호출해 선택 영역과 계산 결과를 화면에 반영한다. 마지막 단계의 입력 수정 여부와 잡기 상태를 조합해 공장에 다음 체험 진입 가능 여부를 제공한다. |
| [CNNFeatureMapExampleData.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapExampleData.cs) | 8×8 입력을 배열로 보관하고, 고정 커널로 세 층의 합성곱·ReLU 값과 A/B 반응을 계산한다. 픽셀을 수정하면 전체 결과를 다시 계산하며, 픽셀을 선택하면 그 위치와 연결되는 수용 영역 마스크를 만든다. Node와 안내판은 이 객체에서 값·가중치·강조 여부를 읽어 표시한다. |
| [CNNFeatureMapFilterController.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapFilterController.cs) | 필터 도구의 위치를 입력 평면 기준 행·열로 바꾸고 격자에 맞춘다. 자동 스캔 중에는 위치를 순서대로 바꾸며, 위치 변경 여부를 Manager에 반환해 방문 기록과 계산 설명을 갱신하게 한다. 선택한 채널의 출력 뉴런까지 연결선도 표시한다. |
| [CNNFeatureMapInteractionController.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapInteractionController.cs) | 현재 잡은 대상 목록과 마지막으로 선택한 확대·축소 대상을 보관한다. 새 대상이 잡힌 물체의 부모나 자식이면 잡기를 거절하고, 잡은 물체가 있는 동안에는 크기 변경도 거절한다. Manager와 Layer는 이 상태를 읽어 버튼 실행이나 자동 배치를 보류한다. |
| [CNNFeatureMapDisplayController.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapDisplayController.cs) | Manager가 넘긴 단계·채널·필터 위치·방문 배열로 각 Node의 계산 완료 표시와 강조 상태를 정한다. 이어 Node가 계산 데이터에서 값을 읽어 색상을 갱신하도록 호출하고, A/B 반응값은 출력 막대의 높이와 색상으로 바꾼다. |
| [CNNFeatureMapGuide.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapGuide.cs) | Manager가 전달한 현재 단계와 계산 내용을 제목·설명·커널 카드·버튼 상태로 표현한다. 단계에 필요한 입력·층·출력만 보이게 하고, 확대·축소 버튼을 생성해 Manager의 크기 변경 명령에 연결한다. 공장에 제공하는 준비 상태도 Manager에서 읽는다. |
| [CNNFeatureMapButton.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapButton.cs) | 각 안내 버튼에 저장된 `CNNFeatureMapAction`을 XRI Activate 이벤트와 연결한다. 비활성 상태와 연속 입력을 걸러낸 뒤 `Manager.Perform()`을 호출하므로, 버튼 자체가 단계나 계산 데이터를 바꾸지는 않는다. |
| [CNNFeatureMapNode.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapNode.cs) | 입력 픽셀 또는 특징 맵의 한 칸이다. 층·채널·행·열로 계산값을 조회하고 방문 여부·선택 강조를 합쳐 `MaterialPropertyBlock`으로 색상과 발광을 갱신한다. 입력 노드 선택은 Manager로 전달하며, 격자에서 꺼낸 상태는 별도로 기록한다. |
| [CNNFeatureMapPanel.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapPanel.cs) | 한 채널의 맵에 속한 Node를 수집하고 상위 Layer의 층 번호와 자신의 채널 번호를 각 Node에 전달한다. 맵 전체를 꺼냈는지 기록해 Layer가 해당 맵을 자동 정렬할지 판단하게 한다. 맵의 위치가 바뀌어도 Node의 계산 인덱스는 유지한다. |
| [CNNFeatureMapLayer.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapLayer.cs) | 같은 합성곱 층의 Panel들을 모아 펼침·접힘 목표 위치로 이동시킨다. 이미 꺼낸 맵은 자동 배치에서 제외하고, 하위 물체를 잡고 있으면 그 맵의 이동을 기다린다. 층 단위 배치와 개별 맵·노드의 직접 이동이 충돌하지 않게 한다. |
| [CNNFeatureMapGrabTarget.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapGrabTarget.cs) | 노드·맵·층·필터에 Rigidbody와 잡기 컴포넌트를 구성하고 초기 자세를 저장한다. 선택 시작·해제·Activate를 Manager 및 해당 Node·Panel의 상태 변경으로 연결한다. 초기 배치 복원과 크기 배율 적용도 이 객체에서 수행하며 필터는 크기 변경 대상에서 제외한다. |
| [CNNFeatureMapGrabInteractable.cs](../client/Assets/Scripts/CNNFeatureMapLab/CNNFeatureMapGrabInteractable.cs) | XRI가 잡기 대상을 초기화하기 전에 `GrabTarget`에 지정된 Collider 목록을 사용하도록 한다. 맵·층의 잡기 컴포넌트가 하위 노드의 Collider까지 자동 수집하는 것을 막아, 같은 공간에서도 노드·맵·층을 각각 선택할 수 있게 한다. |

## 초기화와 실행 흐름

1. `CNNFeatureMapLabManager.Awake()`가 하위 노드·잡기 대상·층을 수집하고 `DisplayController`를 생성한다. 각 Panel은 상위 Layer의 인덱스와 자신의 채널을 하위 Node에 전달한다. 입력 Panel의 층 인덱스는 `-1`이다.
2. `Start()`가 `_inputImage`를 확인하고 `CNNFeatureMapExampleData`를 만든다. 입력은 Read/Write가 활성화된 8×8 텍스처여야 한다.
3. `Guide.Initialize()`가 안내와 버튼을 연결한다. Manager는 Guide의 입력 루트·필터·재질 참조로 `FilterController`를 생성하고 첫 단계를 적용한다.
4. 첫 두 단계의 `LateUpdate()`에서 필터 이동·스캔으로 행·열이 바뀌면 `ApplyPatch()`를 호출한다. Manager가 방문 기록을 저장하고 선택 영역, 출력 뉴런, 합성곱 합과 ReLU 결과를 갱신한다.
5. 입력 픽셀 선택은 현재 단계에 따라 필터 위치 선택, 수용 영역 강조, 입력값 변경으로 분기한다. 입력값을 바꾸면 전체 층을 다시 계산한다.

Interaction·Display·Filter Controller는 Manager가 사용하는 일반 C# 객체다. 씬의 컴포넌트 참조는 Manager·Guide와 각 프리팹 부품에 둔다.

## 단계와 상태

Manager의 `_step`은 `0~3`이며, 단계 변경은 `Perform()` → `ApplyStep()`으로 처리한다.

| 단계 | 계산·표시 동작 |
|---|---|
| 특징 맵 만들기 | 입력과 선택한 첫 층 채널을 표시한다. `_visited[channel, row, column]`으로 방문한 출력 위치를 구분한다. |
| 필터 비교 | 첫 층의 두 맵을 펼치고 선택 채널의 필터와 출력 위치를 표시한다. |
| 수용 영역 추적 | 세 층을 표시한다. 선택한 입력 픽셀을 포함하는 수용 영역을 층별로 강조한다. |
| 입력 변경 | 입력 픽셀을 0 또는 1로 바꾸고 전체 특징 맵과 A/B 반응을 다시 표시한다. |

`_step`, `_channel`, `_visited`, `_editCount`는 Manager가 관리하고, 필터의 행·열과 스캔 상태는 FilterController가 관리한다. `Restore` 명령은 원본 텍스처에서 입력을 복원하고 방문 기록·수정 횟수·배치를 초기화한 뒤 현재 단계를 다시 적용한다.

공장 진행에서 읽는 `ReadyForModelDesign`은 초기화 완료, Guide 존재, 마지막 단계, 입력 수정 1회 이상, 잡은 대상 없음이 모두 충족될 때 참이다. Guide는 이 값을 Manager에서 읽어 제공한다. 공장 씬 전환은 [공장 환경 구현](factory-environment.md)에서 다룬다.

## 계산 데이터

`CNNFeatureMapExampleData`는 Unity 텍스처의 아래에서 위로 향하는 행을 뒤집어, 위에서 아래로 읽는 `_input[row, column]` 배열을 만든다.

- 입력 크기는 8×8이고 합성곱 층의 출력 채널은 `2 → 3 → 5`다. 각 층은 3×3 커널, stride 1, zero padding, ReLU를 적용해 공간 크기를 유지한다.
- `ReadKernelWeight()`는 채널 번호를 기준으로 좌우 변화, 위아래 변화, 평균 필터를 반복 선택한다. 첫 층의 두 필터는 각각 `dx / 3`, `dy / 3`이다.
- `ComputeConvolutions()`는 이전 층의 모든 입력 채널을 합산하고 입력 채널 수로 정규화한다. 경계 밖의 항은 건너뛰어 0 패딩으로 처리한다.
- `SelectPixel()`은 가중치와 활성값에 관계없이 연결되는 공간 위치를 다음 층으로 전파한다. 강조는 구조적 수용 영역을 뜻하며, 실제 값의 변화량이나 중요도를 계산한 결과가 아니다.
- `ToggleInput()`은 선택값이 0.5 이상이면 0, 미만이면 1로 바꾼다. `RestoreInput()`은 원본 이미지의 흑백 값을 다시 읽는다.
- `ReadOutput()`은 마지막 층의 채널 평균을 두 점수로 합산한 뒤 로지스틱 함수로 A/B 반응을 만든다. 학습된 분류 모델의 클래스 확률은 아니다.

첫 단계의 미방문 노드는 표시만 어둡게 처리한다. 계산 결과는 데이터 생성 시 전체 위치에 대해 보유한다. 노드·맵·층의 Transform을 옮겨도 계산 인덱스와 연결 관계는 바뀌지 않는다.

## 잡기와 배치 처리

`GrabTarget`은 `Rigidbody`와 `CNNFeatureMapGrabInteractable`을 구성하고 Manager의 잡기 허용 여부를 XRI Select Filter로 연결한다. 선택 시작 시 Node와 Panel을 분리 상태로 표시하며, 해제 시 크기 변경 대상을 갱신한다. 필터는 회전 추적과 크기 변경 대상에서 제외한다.

`InteractionController.CanGrab()`은 이미 잡은 대상의 부모나 자식을 동시에 잡지 못하게 한다. Manager는 잡은 대상이 있는 동안 안내 버튼 명령을 처리하지 않는다. `Layer.Update()`는 분리되지 않은 맵만 목표 배치로 보간하고, 하위 물체가 잡혀 있으면 해당 맵의 이동을 기다린다.

## 프리팹과 참조

[CNNFeatureMapWorkspace.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/CNNFeatureMapWorkspace.prefab)이 입력·층·출력·안내 UI를 조립한다. 아래 프리팹은 모두 `client/Assets/Prefabs/CNNFeatureMapLab/`에 있다.

| 프리팹 | 역할 |
|---|---|
| `CNNFeatureMapWorkspace` | Manager·Guide와 전체 콘텐츠 참조를 보관한다. |
| `CNNFeatureMapInput` | 입력 노드 64개, 입력 손잡이, 필터 도구를 구성한다. |
| `CNNFeatureMapInputPixel` | 입력 픽셀의 Node와 잡기 콜라이더를 구성한다. |
| `CNNFeatureMapNeuron` | 특징 맵의 개별 Node와 잡기 콜라이더를 구성한다. |
| `CNNFeatureMapPanel` | 뉴런 64개와 맵 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer2Channels` | 첫 층의 맵 2개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer3Channels` | 두 번째 층의 맵 3개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapLayer5Channels` | 세 번째 층의 맵 5개와 층 잡기 대상을 구성한다. |
| `CNNFeatureMapOutput` | A/B 반응을 표시하는 출력 Renderer와 캡션이다. |
| `CNNFeatureMapLessonControls` | 단계 제목·설명·계산 결과와 기본 안내 버튼 6개를 구성한다. |
| `CNNFeatureMapFilter` | 필터 도구와 잡기 대상을 구성한다. |
| `CNNFeatureMapFilterCard` | 필터 이름·가중치·패딩 안내를 표시한다. |

Node의 `_row`·`_column`, Panel의 `_channel`, Layer의 `_layerIndex`가 계산 배열과의 대응을 결정한다. Guide의 기본 버튼 배열은 `Previous`, `Next`, `Scan`, `Filter`, `Spread`, `Restore` 순서로 연결한다. Manager의 `_outputs`는 A/B 순서로 지정한다.

공용 XR 입력과 태블릿은 `client/Assets/Prefabs/XR/XRPlayerRig.prefab`을 사용한다. 입력 텍스처와 재질은 각각 `client/Assets/Textures/CNNFeatureMapLab/`, `client/Assets/Materials/CNNFeatureMapLab/`에 있다.
