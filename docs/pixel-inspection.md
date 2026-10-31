# CNN 픽셀랩: 관찰 · R/G 학습 · B 퀴즈

## 씬과 공간

`CNNPixelLab.unity`는 전체 진행과 내부 씬 수명을 관리한다. 공장 ROOM #1의 첫 공간은 `CNNScreenLab.unity`에서 돋보기 관찰과 R·G 계산 학습을 진행한다. 기존 내부 문을 지나 두 번째 공간에 들어가면 `CNNConvolutionLab.unity`로 교체해 B 퀴즈를 진행한다. 단독 실행에서는 같은 위치에서 두 씬을 교체한다.

공장은 `CNNPixelLabManager`만 참조한다. 기존 `ConfigureFactoryConvolution`, `ScreenCompleted`, `WaitingForFactoryConvolution`, `StartFactoryConvolution`, `ConvolutionStarted`, `ConvolutionCompleted`, `EndSession` 계약과 양피지·문·진입 영역 조건을 유지한다. `ScreenCompleted`의 발생 시점은 RGB 관찰 직후가 아니라 R·G 학습 후 **다음 공간으로**를 눌렀을 때다.

## 첫 공간의 동작

1. 대형 이미지 앞 바닥의 푸른 원 안에 손잡이가 달린 돋보기가 놓여 있다. 기존 XRGrabInteractable을 사용하며 Grip을 유지한다. 잡으면 돋보기가 이미지 방향으로 서고, 푸른 바닥 경계는 숨겨진다.
2. 돋보기를 이미지에 가까이 대면 렌즈 안의 3×3 블록에 선택 영역의 픽셀이 보인다. Grip을 놓으면 거리·방향 조건을 만족하는 영역에 정렬된다.
3. 픽셀 설명을 두 페이지로 읽고 다음을 누르면 **픽셀 확대하기**가 나타난다. 선택 없이 또는 잡고 있는 동안에는 확대할 수 없다.
4. 확대 시 `PixelPatchData`를 한 번 추출한다. 이미지와 돋보기가 1.6초 동안 왼쪽으로 이동하며 55% 크기로 줄어든다. 원본 사진은 유지하고, 선택 영역의 RGB 3층·27개 블록과 숫자가 옆으로 펼쳐진다. 기존 전체 이미지 576블록 전개는 이 흐름에서 사용하지 않는다.
5. RGB 의미, 0~255의 세기, 같은 위치의 세 채널을 세 페이지로 설명한다. **계산 배우기**를 누르면 RGB 관찰 표시를 숨기고 같은 공간의 `PixelPracticeManager`가 학습을 시작한다.
6. 실제 RGB와 구별되는 **연습용 값 0~3**을 명시한다. RGB 입력이 정렬되고, 입력과 별개인 R·G·B 가중치 3층이 옆에서 차례로 나타난다. 가중치는 0과 1만 사용한다.
7. R층을 Grip으로 꺼낸 뒤, 화살표 아래 가중치판을 잡아 빛나는 목표 테두리에 놓는다. 가까이 놓으면 정렬되어 계산이 시작된다. 잡는 동안 화살표는 숨겨진다. 물리 조작에는 **자동 배치** 보조 버튼이 있다.
8. 같은 칸의 두 숫자가 만나 곱해지는 애니메이션을 진행한다. 오른쪽 계산판에는 `입력 × 가중치 = 곱`이 한 줄씩 쌓인다. 세 항마다 다음 줄을 확인하고, 마지막에 세 대각선 값의 합을 확인한다.
9. G도 R과 동일한 설명·조작·곱셈·합 확인을 반복한다. 완료 안내의 **다음 공간으로**를 눌러야 상위 Manager에 이동을 요청한다.

## 두 번째 공간의 동작

- 동일한 선택 영역을 동일한 규칙으로 축소한 연습용 값과 같은 가중치를 사용한다.
- B층 꺼내기와 가중치 배치를 진행한 뒤, B의 9개 곱을 더하는 문제 하나를 푼다.
- 답은 숫자 블록을 Grip으로 잡아 답 칸에 놓거나 큰 숫자 버튼으로 선택한다. 오답은 힌트를 표시하고 다시 선택하게 한다. 정답 건너뛰기는 없다. 자동 배치는 물리 조작만 보조한다.
- 정답 뒤 계산식과 결과를 확인하고 **결과 확인**을 누르면 `Completed`가 발생한다. 결과 화면에는 R·G·B 합과 세 합의 총합, 편향 0을 표시한다.
- 공장에서 완료 화면을 닫으면 완료 상태를 유지한 채 내부 표시 씬을 언로드한다. 단독 실행에서는 이전 관찰 씬으로 돌아가 선택 좌표를 복원한다.

## 데이터와 계산

`PixelImageFactory`는 기존 `OpeningBroadcast.png`를 128×72로 축소하고 5×5 블러 후 32×18로 샘플링한다. 관찰용 숫자와 색은 원래 Color32의 0~255 값을 그대로 사용한다.

`PixelImageData.CapturePatch`는 선택 영역의 위쪽 행부터 왼쪽→오른쪽 순서로 9개 픽셀을 복사한다. `PixelPatchData`는 읽기 전용 인덱서를 제공한다. 선택 스냅샷과 복귀 좌표는 상위 `CNNPixelLabManager`가 보관하며, 두 번째 씬에 전체 이미지나 Transform을 전달하지 않는다.

계산 전용 `ConvolutionCalculation`에서 각 RGB 성분을 `RoundToInt(value × 3 / 255)`로 변환한다. 관찰 데이터는 수정하지 않는다. 모든 채널은 아래와 같은 가중치를 사용한다.

```text
1 0 0
0 1 0
0 0 1
```

각 채널의 합은 0~9, RGB 최종 합은 0~27이다. R·G 학습과 B 퀴즈는 이 동일한 계산 객체 규칙을 사용한다. 학습용 가중치는 훈련된 모델의 가중치가 아니다.

## 책임과 연결

| 파일 | 현재 책임 |
| --- | --- |
| `Scripts/CNNPixelLab/CNNPixelLabManager.cs` | 선택 데이터·복귀 좌표·공장 대기·완료 상태와 두 씬의 순서 |
| `PixelLabSceneLoadController.cs` | Additive 준비·배치·교체·언로드, XR 선택 해제, 로드 실패 시 기존 체험 유지 |
| `CNNScreenLab/CNNScreenLabManager.cs` | 픽셀/RGB 안내 순서, 선택 스냅샷, 관찰→R/G 모듈 연결, 완료 후 이동 요청 |
| `CNNScreenLab/PixelPracticeManager.cs` | 첫 공간의 가중치 소개와 R/G 학습 순서, 공용 계산 UI·공간 표시 소유, 완료 이벤트 |
| `CNNScreenLab/PixelLabUIController.cs` | 사전 배치된 큰 안내문·다음·확대·초기화 버튼 표시 및 요청 이벤트 |
| `CNNScreenLab/PixelBoardController.cs` | 이미지 텍스처·재질 수명과 보드의 왼쪽 축소 이동·복원 |
| `CNNScreenLab/PixelSelection/PixelSelectionController.cs` | Grip, 선택 판정, 바닥 경계, 확대 시 돋보기를 보드에 고정, 초기화 |
| `CNNScreenLab/PixelSelection/SelectedPixelsController.cs` | 돋보기 안의 픽셀 9개 색상 갱신 |
| `CNNScreenLab/RGBObservation/PixelRevealStage.cs` | RGB 공간 전개와 숫자 진행도를 연결하고 완료 알림 |
| `CNNScreenLab/RGBObservation/RgbPixelStackController.cs` | 보드의 선택 위치에서 독립된 관찰 공간으로 3층을 이동·확대 |
| `CNNScreenLab/RGBObservation/RgbValuesUIController.cs` | 원본 RGB 숫자·층별 투명도 |
| `CNNConvolutionLab/CNNConvolutionLabManager.cs` | 두 번째 공간 B 퀴즈의 초기화·순서·완료·복귀 |
| `CNNConvolutionLab/ConvolutionCalculation.cs` | 연습용 값, 0/1 가중치, 항별 곱과 채널 합 |
| `CNNConvolutionLab/LessonUIController.cs` | 공용 저장 UI의 내용·버튼 상태, 다음/답/물리 보조 요청 |
| `CNNConvolutionLab/RgbVolumeController.cs` | 기존 런타임 입체 표시, 입력 정렬·가중치 등장·정렬 화살표·목표 테두리·계산 표시 |
| `CNNConvolutionLab/RgbLayerInteractionController.cs` | 해당 RGB층의 Grip 꺼내기 및 자동 배치 |
| `CNNConvolutionLab/KernelInteractionController.cs` | 개별 가중치판의 Grip 잡기·복귀·근접 정렬·계산 중 잠금 |
| `CNNConvolutionLab/CalculationLesson/RedCalculationController.cs` | R/G에 같은 설명 순서를 적용하는 일반 C# Controller |
| `CNNConvolutionLab/CalculationLesson/CalculationAnimationController.cs` | 숫자 사본 이동·곱·누적합 애니메이션 |
| `CNNConvolutionLab/Quiz/ConvolutionQuizController.cs` | B 한 문제의 선택지·정오답·확인 순서 |
| `CNNConvolutionLab/Quiz/AnswerInteractionController.cs` | 숫자 블록의 Grip 답 슬롯 배치 |

기존 `FilterAlignmentController`의 전체 필터 잡기 단계와 `ResultObservationController`의 별도 RGB 재관찰은 새 진행에서 호출하지 않는다. 기존 파일·GUID·폴더는 유지한다. 첫 공간은 `PixelPracticeManager`만 통해 계산 모듈을 조율하고 내부 UI·숫자 블록을 직접 조작하지 않는다. 모듈 내부의 기존 공용 계산·표시 코드는 현재 경로에서 재사용한다.

## 저장 에셋과 가독성

- `Prefabs/CNNScreenLab/FilterDisplay.prefab`: 기존 픽셀 9개와 GUID를 유지하며 기본 메시 조합의 원형 테두리·손잡이를 추가했다. 이전 사각 테두리는 비활성화했다.
- `Materials/CNNScreenLab/MagnifierBoundary.mat`: 바닥 원의 청색 발광 재질. 외부 이미지·모델을 추가하지 않았다.
- `Prefabs/CNNScreenLab/RgbPixelStack.prefab`: 숫자 66, 채널 제목 38의 굵은 글씨. 원래 RGB 값을 유지한다.
- `Prefabs/Common/PixelCalculationUI.prefab`: 두 공간이 참조하는 공용 사전 배치 UI. 제목 52, 본문 44, 버튼 44, 계산식 48, 피드백 38의 굵은 글씨와 짙은 배경을 사용한다. 계산판은 9개 항을 순차 표시한다. 고정 UI를 런타임에 다시 생성하지 않는다.
- 관찰 씬 안내·버튼은 48의 굵은 글씨다. 설명은 픽셀 2페이지, RGB 3페이지로 나누고 페이지 전환 중 선택·전개 상태를 확인한다.
- Noto Sans CJK KR Regular 에셋을 Unity Bold 스타일로 재사용한다. 폰트 파일을 변형하거나 새 외부 폰트를 추가하지 않았다. 사용 기록은 `ASSET_CREDITS.md`에 반영했다.

## 초기화·정리와 에디터 도구

첫 공간은 이미지 준비 후 돋보기를 바닥 위치로 초기화한다. 최초 계산 시작에 기존 GeometryBuilder로 계산용 공간 표시를 준비한다. 초기화 시 코루틴·XR 선택·RGB 표시·연습 UI를 정리하고 이미지와 돋보기 배치를 복원한다. 생성 텍스처·인스턴스 재질은 기존 소유자가 해제한다.

공통 UI 입력을 위해 필요할 때 생성하는 EventSystem은 안내 패널이 아니라 씬 루트 아래에 둔다. 안내 패널을 숨기고 계산 UI로 넘어가도 입력이 유지된다.

`SpatialPixelLabBuilder`·`ScreenLabVisualsBuilder`는 관찰 씬과 첫 공간 학습 모듈의 참조를 연결한다. `ConvolutionLabBuilder`는 공용 UI 프리팹과 B 공간 표시를 연결한다. 저장된 두 씬에 필요한 참조가 반영되어 있으므로 이번 변경 적용을 위해 메뉴를 실행할 필요는 없다.

## 확인 범위

2026-10-31 Unity 6000.3.24f1에서 컴파일과 FactoryEnvironment Play를 확인했다. 에디터에서 XR 선택·해제 콜백과 UI 버튼 이벤트를 발생시켜 돋보기 선택, 픽셀/RGB 안내, R·G의 9개 항 계산, 양피지 수집 전후 문 조건, 두 번째 공간 로딩, B 오답 재시도·정답 완료를 확인했다. B는 버튼 선택과 숫자 블록의 답 슬롯 해제 경로를 각각 확인했다. 확인한 패치의 결과는 R=3, G=3, B=6이었다.

Game View에서 발견한 제목 잘림과 칸막이 뒤의 이미지/RGB 배치를 수정했다. 관찰 RGB의 배율은 1.7에서 2.2, 계산 블록은 1에서 1.5로 키웠다. 깊이 간격을 줄이고 계산 중 해당 채널만 표시하며, 결과 블록과 합 슬롯을 계산판·수식에서 분리했다. 제목 영역 높이는 90이며 확인한 안내·수식·계산판 텍스트는 영역 높이를 넘지 않았다.

이는 에디터의 이벤트 구동 확인이다. 실제 Quest 착용 시의 가독성, 물리 컨트롤러 Grip 입력과 레이 조준은 별도 기기 확인이 필요하다. 테스트 파일·프로젝트 복사본·플레이어 빌드는 생성하지 않았다.
