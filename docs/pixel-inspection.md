# CNN 픽셀 관찰과 합성곱 구현

[CNNPixelLab.unity](../client/Assets/Scenes/CNNPixelLab.unity)는 이미지의 RGB 값을 관찰하고 선택한 3×3 영역의 합성곱을 계산하는 씬이다. 런타임 코드는 `client/Assets/Scripts/CNNPixelLab/`의 `UI` 네임스페이스에 있다. 컨트롤러·마우스 조작과 체험 순서는 [조작법](xr-simulator.md)을 참고한다.

## 파일과 클래스 역할

| 파일·클래스 | 역할과 처리 방식 |
|---|---|
| [CNNPixelLabManager.cs](../client/Assets/Scripts/CNNPixelLab/CNNPixelLabManager.cs) | PixelImageManager·PixelLessonManager와 UI를 초기화하고 이벤트를 연결한다. 선택·픽셀 전환·계산 사이의 진행을 조율하며 ConvolutionCompleted와 재시작을 관리한다. 씬의 기존 직렬화 참조는 유지한다. |
| [PixelImageManager.cs](../client/Assets/Scripts/CNNPixelLab/PixelImageManager.cs) | 사진·렌즈 텍스처, 선택 Controller, 픽셀 전환과 이미지·필터 표시를 소유한다. 읽기 전용 픽셀 목록과 선택 결과를 제공하고 선택·잡기·전환 완료를 이벤트로 알린다. |
| [PixelLessonManager.cs](../client/Assets/Scripts/CNNPixelLab/PixelLessonManager.cs) | 선택 영역의 RGB 입력과 고정 가중치를 준비하고 공간 관찰·합성곱 퀴즈·결과 표시를 조율한다. 완료와 화면 전환 요청을 씬 Manager에 이벤트로 알린다. |
| [PixelUIInputController.cs](../client/Assets/Scripts/CNNPixelLab/PixelUIInputController.cs) | 필요한 EventSystem·XRUIInputModule을 준비하고 자신이 추가한 입력 객체를 해제한다. |
| [RgbPixelStackController.cs](../client/Assets/Scripts/CNNPixelLab/RgbPixelStackController.cs) | 선택된 3×3 픽셀을 확대하고 RGB 세 층과 각 성분 숫자를 깊이 방향으로 펼친다. PixelRevealStage의 메시·재질과 읽기 전용 픽셀 데이터를 사용한다. |
| [PixelImageFactory.cs](../client/Assets/Scripts/CNNPixelLab/PixelImageFactory.cs) | 원본 Texture2D를 축소하고 평균 블러를 적용해 사진 표시용 이미지를 만든다. 여기서 다시 RGB 픽셀 배열을 샘플링하고, 배열을 Point 필터링 텍스처로 바꿔 렌즈에 표시한다. Manager가 이 결과를 보관하므로 화면에 보이는 선택과 계산에 사용하는 색상 데이터가 같은 원본에서 나온다. |
| [PixelSelectionController.cs](../client/Assets/Scripts/CNNPixelLab/PixelSelectionController.cs) | 이미지 보드·필터·선택 프레임을 연결받아 필터 위치를 3×3 영역의 원점으로 변환한다. 잡는 중에는 미리보기를 표시하고, 놓은 뒤 이미지와의 거리·방향이 유효하면 격자에 맞춰 선택을 확정한다. Manager에는 선택 유무·배열 좌표·월드 위치와 갱신 여부를 제공한다. |
| [PixelLabUIController.cs](../client/Assets/Scripts/CNNPixelLab/PixelLabUIController.cs) | 선택과 전환 상태에 따라 자세히 보기·다음으로·처음으로 버튼과 안내를 갱신한다. 조작판의 활성 상태를 보관·복원하며 이미지와 필터의 활성 상태는 PixelImageManager가 관리한다. |
| [PixelRevealStage.cs](../client/Assets/Scripts/CNNPixelLab/PixelRevealStage.cs) | 이미지를 열별 컬러 픽셀로 전환하고 RgbPixelStackController로 선택 영역의 RGB 층을 펼친다. 전체 전환이 끝나면 PixelsRevealed를 알린다. 초기화 시 애니메이션과 표시를 정리한다. |
| [RgbConvolutionView.cs](../client/Assets/Scripts/CNNPixelLab/RgbConvolutionView.cs) | 관찰·계산 UI와 결과를 표시하고 결합·재시작·다음·닫기·선택 이동 요청을 이벤트로 알린다. 계산과 학습 진행은 PixelLessonManager가 담당한다. |
| [ConvolutionExercise.cs](../client/Assets/Scripts/CNNPixelLab/ConvolutionExercise.cs) | 입력·가중치 배열을 복사해 채널별 정답을 계산하고 코루틴으로 필터 정렬 → R 설명 → G/B 퀴즈 → RGB 합산을 진행한다. 각 구간에서 공간 상호작용이나 다음 버튼을 기다리며, 숫자 블록과 UI 답변을 같은 정답 판정으로 처리한다. 오답은 힌트를 표시하고 재입력을 기다리며 결과 정리 단계에서 완료 콜백을 호출한다. |
| [SpatialConvolutionController.cs](../client/Assets/Scripts/CNNPixelLab/SpatialConvolutionController.cs) | 입력·가중치 데이터를 받아 공간 표시용 View와 상호작용 객체를 준비한다. 학습 단계의 요청에 따라 RGB 층 배치, 곱·누적 합 애니메이션, 연결선·숫자·강조를 갱신한다. 계산 진행 코드는 이 클래스의 정렬·채널 꺼내기 완료 상태를 기다려 다음 단계로 넘어간다. |
| [SpatialConvolutionViewBuilder.cs](../client/Assets/Scripts/CNNPixelLab/SpatialConvolutionViewBuilder.cs) | RGB 입력, 가중치, 곱, 답 블록과 라벨·연결선을 런타임 오브젝트로 만든다. 참조를 `View`에 묶어 Controller가 계산 단계마다 같은 오브젝트를 갱신하도록 제공한다. 표시용 공유 재질을 소유하고 종료 시 해제하며 잡기 판정은 InteractionController에 맡긴다. |
| [SpatialConvolutionInteractionController.cs](../client/Assets/Scripts/CNNPixelLab/SpatialConvolutionInteractionController.cs) | 계산용 가중치와 답 블록에 잡기 이벤트를 연결한다. 가중치를 목표 근처에 놓으면 정렬 완료로 고정하고, 답 칸에 놓은 숫자 블록은 선택지 인덱스를 계산 진행 코드에 전달한 뒤 원위치로 돌린다. 화면 종료 시 선택을 취소하고 부모·이벤트를 정리해 숨겨진 물체가 계속 잡혀 있지 않게 한다. |
| [SpatialVolumeInteractionController.cs](../client/Assets/Scripts/CNNPixelLab/SpatialVolumeInteractionController.cs) | RGB 덩어리 전체, 채널 층, 필터 전체의 잡기와 놓기를 단계별로 허용한다. 이동 거리와 놓인 위치·회전을 검사해 선택 영역 꺼내기, 필터 정렬, 채널 분리를 완료 처리한다. 선택한 입력 칸과 대응 가중치를 함께 강조해 이후 계산 단계에서 무엇을 곱하는지 보여준다. |
| [CNNMousePreviewController.cs](../client/Assets/Scripts/CNNPixelLab/CNNMousePreviewController.cs) | XR 기기가 없는 에디터에서 별도 카메라와 마우스 클릭·드래그를 제공한다. 필터 이동은 SelectionController에, 공간 물체의 이동·놓기는 SpatialConvolutionController에 전달해 기존 판정을 사용한다. 미리보기 동안 리그·시뮬레이터·입력 모듈 상태를 보관하고 종료 시 복원한다. |

`PixelImageFactory`는 정적 유틸리티다. `PixelImageManager`, `PixelUIInputController`, `PixelSelectionController`, `PixelLabUIController`, `SpatialConvolutionViewBuilder`, `SpatialConvolutionInteractionController`는 일반 C# 객체다. 나머지 런타임 클래스는 `MonoBehaviour`이며, RGB 관찰·계산 화면과 공간 상호작용 컴포넌트는 초기화 과정에서 생성한다.

## 초기화와 데이터 흐름

1. `CNNPixelLabManager.Awake()`가 `Camera.main`을 찾아 하위 Canvas의 카메라로 지정한다. 씬이 사용하는 입력·태블릿은 공용 `XRPlayerRig`에서 제공한다.
2. `PixelImageFactory.CreateSmoothPhoto()`가 원본을 128×72로 샘플링한 뒤 5×5 평균 블러를 적용한다. `SamplePixels()`가 이 결과를 32×18 `Color32[]`로 만든다.
3. `PixelImageManager`는 사진·렌즈 텍스처와 SelectionController를 준비한다. 씬 Manager는 PixelLessonManager·UI를 초기화하고 각 모듈의 이벤트를 구독한다.
4. `LateUpdate()`에서 PixelImageManager가 선택을 갱신하고 변경 이벤트를 알리면 씬 Manager가 상세 보기 버튼과 안내를 갱신한다. 픽셀 전환 중과 완료 후에는 필터 선택 갱신을 중지한다.
5. `ShowDetails()`는 선택한 영역을 유지한 채 이미지의 픽셀 전환과 RGB 깊이 분리 애니메이션을 시작한다. 완료 후 `다음으로`가 `BeginConvolution()`을 호출하고, 같은 원본·선택 좌표를 `PixelLessonManager.Show(..., true)`에 전달한다.

UI 입력은 `OnEnable()`에서 준비한다. EventSystem이 없으면 씬 하위에 생성하고, 기존 EventSystem에 `XRUIInputModule`만 없으면 그 모듈을 추가한다. 비활성화할 때는 자신이 생성한 EventSystem 또는 추가한 모듈을 정리한다.

## 선택과 단계 전환

`PixelSelectionController`의 선택값은 32×18 배열 안에서 3×3 영역의 원점을 나타낸다. 필터의 위치·깊이·방향으로 미리보기 유효성을 판단하고, 영역이 이미지 밖으로 나가지 않도록 원점을 제한한다. 필터는 상세 보기의 물체와 같은 공용 XR 잡기를 사용하며 오른손 레이의 부착점을 따라 이동한다. Grip을 누른 상태에서 레이를 필터에 맞춰도 잡기가 시작된다. 별도 레이–이미지 평면 교점 계산으로 이동을 제한하지 않는다. 놓으면 이미지 앞 0.22 로컬 단위 위치로 정렬한다. 놓기 이벤트는 `_snapPending`을 설정하고 다음 갱신에서 유효한 선택을 확정한다. 필터의 초기 복귀 위치와 회전은 보드 기준 로컬 좌표로 저장하고, 복귀 시 현재 보드 Transform으로 월드 좌표를 계산한다. 따라서 공장 배치를 위해 콘텐츠가 이동하거나 회전해도 외부 위치 보정 없이 복귀 기준이 유지된다.

`PixelRevealStage.Phase`는 다음 순서로 진행한다.

```text
Inactive → AwaitingClick → Revealing → Revealed
```

이미지는 시작부터 4.8×2.7m 크기로 표시한다. 방 안의 별도 위치에 놓인 필터를 찾아 이미지 위에 놓으면 `자세히 보기`가 활성화된다. 첫 화면은 `자세히 보기 / 처음으로`, 자세히 보기 이후는 `다음으로 / 처음으로` 두 버튼만 표시한다. 픽셀 전환과 RGB 분리가 진행되는 동안 다음 버튼은 비활성화되며 처음으로는 애니메이션을 취소한다.

`자세히 보기`에서 원본 사진이 열 순서로 컬러 입체 픽셀로 바뀌고, `RgbPixelStackController`가 선택한 3×3 영역을 앞으로 확대하며 같은 위치의 R·G·B를 깊이 방향의 세 층으로 펼친다. 숫자는 각 층의 앞면에 표시하고 별도의 R/G/B 선택 버튼은 만들지 않는다. RGB 층의 깊이는 구조를 이해하기 위한 공간 표현이며 실제 디스플레이 픽셀의 물리적인 층이나 색 세기의 크기를 뜻하지 않는다. 옆으로 이동해 겹친 층을 관찰한 후 같은 선택 영역으로 합성곱을 시작한다.

원본은 `OpeningBroadcast.png`이며 오프닝 대형 디스플레이의 뉴스 방송을 고정 시점에서 렌더링한 것이다. 표시·렌즈·계산이 같은 원본을 사용한다. 필터는 콘텐츠 로컬 (-2.4, 1.15, -0.6)에 배치하고 기존 보드 기준 복귀 좌표를 사용하여 공장 배치 회전에도 처음 위치로 돌아간다.

`PixelLessonManager.Show()`는 선택한 3×3 영역을 위에서 아래 순서로 읽어 `int[3, 9]` 입력 배열을 채운다. 관찰 모드에서는 `SpatialConvolutionController.Observe()`를 통해 영역 꺼내기와 RGB 층 표시를 진행한다. 계산 모드에서는 `ConvolutionExercise.Begin()`으로 입력과 고정 가중치를 전달한다.

## 합성곱과 퀴즈

`PixelLessonManager`의 고정 가중치는 R/G/B 모두 다음과 같다.

```text
-1  0  1
-1  0  1
-1  0  1
```

입력은 채널별 0~255 정수다. 각 채널의 9개 곱을 더하고, 세 채널 합에 편향 0을 한 번 더해 해당 위치의 출력 하나를 구한다. 결과는 음수나 255 초과 값도 그대로 유지한다.

`ConvolutionExercise.Begin()`은 입력·가중치 배열을 복사하고 합과 진행 상태를 초기화한다. `Run()` 코루틴은 공간 상호작용의 완료와 다음 버튼을 기다리며 아래 순서를 진행한다.

| 계산 단계 | 구현 |
|---|---|
| 필터 구조 확인 | `PrepareVolume()`이 RGB 가중치 전체의 정렬을 기다리고, `PullColour()`가 해당 채널을 꺼내는 동작을 기다린다. |
| R 설명 | `Focus()`로 채널을 배치하고 가중치를 정렬한다. `MultiplyAndAccumulate()`로 입력·가중치 사본, 곱, 누적 합을 표시한다. 줄별 합을 거쳐 9개 곱의 합을 구한다. |
| G 퀴즈 | 첫 줄의 세 곱의 합과 전체 9개 곱의 합을 각각 묻는다. |
| B 퀴즈 | 전체 9개 곱의 합을 묻는다. |
| RGB 합산 | R·G·B 합과 편향을 더한 값을 묻고 `CombineSums()`로 채널 합을 결합한다. |

`Ask()`는 정답과 오답 후보에서 중복을 제거해 선택지 3개를 만들고 순서를 섞는다. 숫자 블록과 UI 버튼은 모두 `Choose()`로 연결된다. 오답은 힌트를 표시하고 같은 문제에서 기다린다. 정답은 입력을 잠그고 다음 진행을 허용한다.

최종 정답 이후 결과 정리 단계까지 진행하면 `PixelLessonManager.CompleteExercise()`가 Completed 이벤트를 발생시키고 씬 Manager가 구독한 `MarkConvolutionCompleted()`에서 완료 상태를 갱신한다. 공장 흐름은 이 완료 상태를 읽어 다음 체험을 연다. `ResetLab()`은 완료 상태와 선택·픽셀 전환·상세 화면을 초기화한다. 이 씬의 출력값을 다음 [특징 맵 씬](cnn-feature-map-explorer.md)의 계산 입력으로 전달하지는 않는다.

## 공간 표시와 상호작용

`SpatialConvolutionController.Initialize()`는 ViewBuilder와 InteractionController를 만들고 `SpatialVolumeInteractionController`를 추가한다. ViewBuilder는 렌더링용 타일의 기본 콜라이더를 제거하고, 잡기용 콜라이더는 각 상호작용 담당 클래스가 묶음 단위로 구성한다.

- `SpatialVolumeInteractionController`는 영역·필터 전체·RGB 층의 놓기 시점에 꺼내기 거리, 정렬 위치·회전, 층 이동 범위를 판정한다. 현재 단계에서 필요한 대상만 잡을 수 있게 하고 채널 이동 중 대응 가중치를 함께 이동시킨다.
- `SpatialConvolutionInteractionController`는 계산용 가중치가 정렬 위치 근처에 놓이면 고정한다. 답 블록은 답 칸 안에 놓인 경우에만 선택 콜백을 호출하고 원위치로 돌린다.
- `SpatialConvolutionController`는 계산 단계의 입력–가중치–곱 연결선과 RGB 대응 위치를 표시한다. 측면 숫자는 정면 숫자와 값을 동기화하고 카메라 방향에 따라 표시한다.
- RGB 합치기는 원본 세 채널로 컬러를 재구성하는 표시 동작이다. 합성곱의 가중합 계산과 별도로 처리한다.

상세 화면을 닫거나 비활성화할 때는 잡기 선택을 취소하고 물체를 콘텐츠 부모로 회수한다. `PixelImageManager`와 `PixelLabUIController`는 각각 이미지·필터와 조작판의 활성 상태를 복원한다. PixelImageManager는 생성한 텍스처와 재질을, ViewBuilder는 공간 표시용 공유 재질을 해제한다.

## 에디터 도구와 연결 참조

| 파일·클래스 | 역할 |
|---|---|
| [SpatialPixelLabBuilder.cs](../client/Assets/Editor/SpatialPixelLabBuilder.cs) | `Create()`가 대상 씬이 없을 때 원본 이미지·필터·렌즈·UI·공용 리그를 만들고 Manager의 참조와 버튼을 연결한다. 씬을 저장한 뒤 PixelRevealStage 설치와 Build Settings 등록까지 수행해 런타임 초기화에 필요한 구성을 남긴다. 기존 씬은 새로 덮어쓰지 않는다. |
| [PixelRevealStageBuilder.cs](../client/Assets/Editor/PixelRevealStageBuilder.cs) | 활성 CNNPixelLab에서 이미지 보드·조작판·Manager를 찾아 픽셀 전환 컴포넌트와 다음 버튼을 구성한다. 폰트·UI·보드 참조를 연결하고 버튼이 Manager의 단계 전환을 호출하도록 만들어, 런타임의 이미지 선택과 픽셀 전환 이벤트가 씬 흐름에 이어지게 한다. |
| [CNNMousePreviewMenu.cs](../client/Assets/Editor/CNNMousePreviewMenu.cs) | 메뉴 선택을 현재 프로젝트 경로별 EditorPrefs에 저장하고 Play 중인 Manager에 미리보기 컴포넌트를 추가하거나 제거한다. 카메라·드래그 동작은 `CNNMousePreviewController`가 담당하며 이 클래스는 에디터 설정과 컴포넌트 설치를 연결한다. |

씬의 Manager에는 원본 사진, 이미지 Transform·Renderer, 필터, 렌즈, 선택 프레임, 상세 UI, 버튼과 PixelRevealStage를 연결한다. 초기화에서 `_values[0].font`를 RGB 관찰·학습 UI의 폰트로 사용한다. 원본 이미지는 CPU에서 샘플링하므로 Read/Write가 활성화되어 있어야 한다.

마우스 미리보기는 `UNITY_EDITOR` 안에서만 구현된다. 별도 카메라를 생성하고 기존 리그·시뮬레이터·입력 모듈을 비활성화한 뒤, 종료 시 이전 상태를 복원한다. 필터 드래그는 SelectionController에, 공간 물체 드래그와 놓기는 SpatialConvolutionController에 전달한다. 실제 XR 기기가 활성화되면 미리보기를 종료한다.

입력 사진은 `client/Assets/Textures/CNNPixelLab/OpeningBroadcast.png`, 공용 리그는 `client/Assets/Prefabs/XR/XRPlayerRig.prefab`을 사용한다. 시각 에셋 구성은 [Neon UI 프리팹](neon-ui-prefabs.md)을 참고한다.

## Quest 관찰 화면 배치

큰 이미지의 하단과 조작 버튼이 겹치지 않도록 이미지 중심을 씬 기준 높이 2.45m, 조작판 중심을 0.62m로 분리한다. 이미지 아래에는 자세히 보기 또는 다음으로, 처음으로 버튼과 단계 안내를 배치한다. RGB 성분 숫자는 선택 영역의 세 층 앞면에 표시하며, 픽셀 관찰 완료 후에는 안내에서 합성곱 연산 단계로 이동함을 설명한다.

이 배치와 벽 충돌 수정은 소스에 반영했으며, 이번 변경 후 Unity 컴파일·Play 및 Quest APK 검증은 아직 수행하지 않았다.

## 필터·픽셀 입체 표시 (2026-10-26)

`FilterVolumeController`는 기존 필터 잡기 입력과 원본 샘플 좌표를 유지하면서 얇은 프레임·평면 렌즈 표시를 모서리가 다듬어진 두꺼운 프레임과 3×3 픽셀 블록으로 교체한다. `PixelVolumeMesh`의 공유 메시와 재질을 사용하고 선택 색상은 MaterialPropertyBlock으로 갱신한다. 밝기나 가중치에 따라 두께를 바꾸지 않는다. 투명도는 0.94이며 발광·추가 장식 조명은 사용하지 않는다.

`PixelRevealStage`도 평면 Image 대신 동일한 메시의 픽셀 블록을 표시한다. 열별 전환 시 블록과 앞면 숫자가 이미지 앞쪽으로 함께 나온다. 색 선택·숫자·합성곱 데이터는 기존 값 그대로이며, 숫자는 앞면에 남고 옆면으로 깊이를 확인할 수 있다. 생성 메시·재질은 소유 Controller/Stage의 종료 시 해제한다. 이번 변경은 소스 검토까지 수행했으며 컴파일·Play·APK 확인 전이다.

## 2026-10-26 Unity 실행 확인

Unity 6000.3.24f1 Play에서 CNNPixelLab을 실행하고 실제 Camera.Render 결과로 정면 필터와 측면 픽셀 화면을 확인했다. 초기 MaterialPropertyBlock 필드 생성으로 발생한 Unity 생성자 예외를 BuildPixels 초기화로 옮겨 해결했다. 숫자 Canvas의 정렬 순서를 높이고 전환 완료 후 원본 사진 Renderer를 숨겨 반투명 블록 뒤 사진의 색 혼합과 숫자 가림을 수정했다. ResetStage에서는 사진을 다시 표시한다.

최종 컴파일 완료·실패 없음, RevealPixels의 Revealing→Revealed 전환, G/B 버튼 onClick 호출 후 각각 576개 숫자의 원본 성분값 일치, NextStage 호출 후 합성곱 화면 활성화를 확인했다. 이는 에디터 실행·직접 콜백 검증이며 Quest 컨트롤러 레이 클릭이나 공장 전체 동선의 실기기 검증은 아니다. 자동화 연결의 Play/domain reload 중 응답 시간 초과 로그와 수정 전 예외 이력은 최종 콘텐츠 오류와 구분했다. 사용자의 후속 요청에 따라 APK 재빌드·기기 설치는 수행하지 않았다. 새 테스트 파일은 생성하지 않았다.


## 2026-10-29 입체 관찰 동선 변경

새 RGB 층 표시는 `Scripts/CNNPixelLab/RgbPixelStackController.cs`가 담당한다. 공용 XR 입력·공장 이동·다음 방 완료 조건은 유지한다. 기존 별도 RGB 관찰 화면은 이번 진입 흐름에서 호출하지 않으며 합성곱 설명·퀴즈는 기존 계산 화면을 사용한다. Unity 6000.3.24f1 컴파일 성공을 확인했다. Play에서 선택 상태를 설정하여 Revealing→Revealed 완료, RGB 세 층의 서로 다른 깊이, 다음으로 합성곱 화면 활성화, 처음으로 초기화를 확인했다. MaterialPropertyBlock 생성자 초기화 오류를 Initialize로 옮겨 수정했다. 실제 카메라 렌더링으로 입체 블록과 숫자 표시도 확인했으나, 근접 정면에서는 뒤 층 숫자가 겹쳐 보이는 한계가 있다. 검증은 에디터 콜백 기반이며 Quest 직접 잡기·공장 전체 동선 검증이나 APK 빌드는 수행하지 않았다.

## 2026-10-29 역할 분리 브랜치 병합

입체 관찰 동선을 PixelImageManager의 전환·선택 관리에 반영하고, PixelRevealStage와 RgbPixelStackController는 읽기 전용 픽셀 목록을 전달받도록 연결했다. PixelLessonManager의 계산·완료 이벤트와 공용 태블릿의 역할 분리는 유지한다. 위 Unity 실행 기록은 원본 브랜치에서 수행한 결과이며, 이번 병합 결과에는 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다.
