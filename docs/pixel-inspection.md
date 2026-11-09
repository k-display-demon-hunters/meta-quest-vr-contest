# 픽셀 관찰과 ROOM 1-2 체험

## 진행과 완료 경계

Core/Learning의 단계는 `Tutorial → ScreenLab → ConvolutionLab → FeatureMap → ModelDesign`이다. ROOM 1-2의 `CNNConvolutionLab`에서는 5×5 RGB 입력에 필터를 배치하고 아홉 출력 위치를 계산한다. 결과를 확인한 뒤 ROOM 2의 `CNNFeatureMapLab`으로 이어진다.

출력값을 기록할 때마다 진행률을 갱신하며 아홉 칸을 모두 기록하면 95%에 도달한다. **결과 확인**을 누르면 3×3 결과와 첫 위치의 RGB 합산식을 표시한다. **체험 마치기**를 누르면 `CNNConvolutionLabManager`가 `Progress=1`과 `CanExit=true`를 알린다. 누적 진행률은 `LearningProgress`가 체험별 최댓값으로 기록한다.

## 독립적인 관찰과 합성곱 입력

`CNNScreenLab`은 사진에서 선택한 3×3 원본 RGB 0~255 관찰을 담당한다. `PixelImageFactory`는 기존 사진에서 128×72 축소·5×5 블러·32×18 샘플링 결과를 만든다. `PixelImageData.CapturePatch`가 만든 `PixelPatchData`는 관찰 내부에서만 사용하며 `Scripts/CNNScreenLab/PixelPatchData.cs`에 둔다. 외부로 선택 데이터를 공개하거나 전달하지 않는다.

`CNNConvolutionLab`은 `Scripts/CNNConvolutionLab/ConvolutionCalculation.cs`의 `CreateExample()`에 저장된 0~3 값의 고정 5×5 RGB 배열로 시작한다. 순차 입장, 태블릿 직접 이동, 단독 실행 모두 같은 예제를 사용한다. 1-1의 선택 여부나 완료 상태로 입력을 바꾸지 않는다. 계산 결과는 이 배열과 필터로 매번 계산한다.

`FactorySceneLoadController`는 인접 씬을 미리 로드한다. ScreenLab과 ConvolutionLab은 로드 후 각자의 `Start`에서 자체 초기화하며, 공장의 현재 구역이나 실제 입장 여부를 전달받지 않는다. ConvolutionLab은 고정 예제를 준비하고 필터 배치 입력을 기다린다. 공장 내 로드와 단독 실행은 같은 시작 경로를 사용한다. 로드된 상태에서는 구역 이동만으로 체험을 중단하거나 초기화하지 않는다.

`LearningSessionManager`는 구체 체험 Manager를 참조하지 않고 `ILearningExperience`로 단계·진행률·출구 허용 상태를 조회하고 변경 이벤트를 구독한다. 언로드 전에는 같은 인터페이스의 `EndSession`으로 정리를 요청한다. 공장의 기존 문 개방 조건과 학습 순서는 유지한다.

두 체험의 UI 버튼은 공용 `XRPlayerRig`에 구성한 EventSystem·XRUIInputModule을 사용한다. `ScreenLabUIController`와 `CNNConvolutionLabManager`는 입력 환경을 생성하거나 해제하지 않으며, 버튼의 기존 On Click 연결로 각 체험의 요청을 처리한다. 체험 종료·언로드 시 공용 입력 환경은 유지된다. ScreenLab 단독 실행은 씬에 배치된 공용 리그를 사용하고, 자체 리그가 없는 ConvolutionLab의 XR 조작은 공장에 로드된 공용 리그를 사용한다.

## 합성곱 작업대와 시각 디자인

기존 공장 테마와 세 구역의 역할을 유지하면서, 모서리를 다듬은 금속 몸체·겹친 얕은 테두리·어두운 표시 면·절제된 앰버 포인트로 구성한다. 공용 UI 원본이나 ROOM 2의 재질을 바꾸지 않고 `CNNConvolutionLab` 전용 프리팹·재질·메시를 사용한다. 조명 자체보다 판·문자·타일의 대비로 정보를 구분한다.

| 구역 | 구성 |
| --- | --- |
| 왼쪽 · 01 필터 거치 | 홈이 있는 손잡이와 3×3 금속 필터, 대기 받침 |
| 가운데 · 02 입력 × 가중치 | 현재 채널의 5×5 입력, 아래 고정된 한 칸 곱셈판 |
| 오른쪽 · 03 결과 · 합계 | 3×3 판, 그 아래 작은 합계 표시, 그 아래 고정 합산식 판 |
| 위 | 행동 안내와 R/G/B 상태 카드 |
| 아래 | 짧은 계산 메모·피드백, 단계에 맞는 버튼 |

**두 식 판의 배경·테두리·크기·위치는 작업대 단계 내내 유지**한다. 도입·채널 전환·필터 이동·퀴즈에서는 식의 글자만 비운다. 첫 R의 한 칸 식은 가운데, 모든 채널 합과 RGB 합은 오른쪽에 둔다. 결과 요약으로 전환할 때 작업대를 숨기는 것은 별도 단계다.

오른쪽 결과판 위에 어두운 바탕과 얇은 금속 테두리의 명판을 둔다. `같은 위치의 곱`과 `합성곱 출력 · 3×3`은 같은 명판에서 기존 한글 글꼴로 표시한다.

`PixelCalculationUI`의 **필터 자동 배치**, **결과 확인**, **체험 마치기**는 공용 `WorldUIButton`의 중첩 인스턴스다. 세 버튼은 프리팹 override로 자체 Canvas·레이캐스터를 제외하고 부모 Canvas에서 레이 입력을 받는다. 자동 배치는 배치 단계, 결과 확인은 아홉 출력 기록 뒤, 체험 마치기는 결과 요약에서 표시한다.

입력 숫자는 타일 중앙, 겹친 필터의 가중치는 오른쪽 위에 둔다. 숫자는 원본을 유지하고 사본만 이동한다. `·`은 미기록 상태이며 유효한 0은 `0`으로 표시한다. 상태 카드도 `대기`, `진행 중`, `완료 · 합 0`을 구분한다. R/G/B는 글자와 빨강/초록/파랑을 함께 사용하며, 합산식에서도 채널 숫자의 색을 유지하고 연산 기호·최종합은 밝은 중성색으로 표시한다.

RGB 입력은 작업대의 같은 위치에서 채널별로 전환하여 표시한다. 잡기 대상은 필터 손잡이이며, RGB 층·이미지 패치 잡기 컴포넌트나 별도의 3층 가중치·재구성 패치 오브젝트는 생성하지 않는다. 자동 배치 버튼은 필터 배치만 보조한다. `PixelCalculationUI.prefab`에는 숫자 키패드·숫자 답 제출 이벤트를 두지 않으며, 퀴즈 입력은 출력판의 칸 선택으로 받는다.

`RequestSkip`, `RequestNext`, `RequestFinish`는 각각 자동 배치, 결과 확인, 체험 마치기 클릭을 알린다. 배치 Controller가 자동 배치 요청을 구독하고, 요약 Controller가 결과 확인과 종료 요청을 구독한다. `LessonUIController`는 제목·안내·피드백·진행 카드·결과 요약을 표시하고, `ConvolutionCalculation`은 입력과 계산값을 제공한다.

프리팹에는 기존 GUID와 기준점 연결을 유지한다. `_inputAnchor`, `_kernelHome`, `_kernelTarget`, `_resultAnchor`, `_sumAnchor`, `_equationAnchor`와 새 `_sumEquationAnchor`가 런타임 표시 위치를 정한다. 첫 목표는 **입력의 맨 왼쪽 위 3×3**이다. 필터 이동은 입력판 로컬 격자를 기준으로 하므로 공장의 회전과 무관하게 관람객 기준 오른쪽·아래 방향을 유지한다.

ROOM 1-2의 공통 콘텐츠 루트는 균일 배율 1.5를 사용한다. 기존 배율 2의 75%로 작업대, 표시 UI와 잡기 대상에 함께 적용한다. 공장에서는 기준점 `(-28.55, -0.375, -2.0)`에 배치한다. 발 받침이 바닥 Y=0 바로 위에 오도록 높이를 맞추고, 남쪽 연구 데스크와 거리를 두도록 기준점을 북쪽으로 0.75m 옮겼다. 계산 메모는 높이를 줄여 바닥 위, 작업대 명판 아래에 배치한다. 필터의 배치 허용 반경은 목표 로컬 좌표로 계산하여 축소 비율을 따른다.

### 계산 규칙

입력 5×5×3, 공간 필터 3×3, 전체 가중치 3×3×3, stride 1, 패딩 없음, 편향 0이다. 세 채널의 연습용 가중치는 모두 `1 0 0 / 0 1 0 / 0 0 1`이다. 각 위치에서 채널별 9개 곱을 모두 더하고 세 채널 합을 더해 출력 한 칸을 만든다. 짧은 합산식에서는 가중치 0 항만 생략한다.

필터 왼쪽 위 `(row,column)`은 출력판의 같은 `(row,column)`에 대응한다. 값이 필터 중앙에서 출발하는 연출은 입력 중앙 픽셀을 수정하지 않는다. 입력 좌표·필터 내부 좌표·출력 좌표는 구분한다.

### 진행과 애니메이션

1. **배치**: R 입력과 두 빈 식 판을 표시한다. 손잡이를 Grip으로 잡아 맨 왼쪽 위 점선에 놓는다. 자동 배치도 같은 목표·정렬 경로를 사용한다. 이후 다시 잡도록 요구하지 않는다.
2. **첫 R 아홉 칸**: 행 순서로 아홉 칸을 모두 개별 계산한다. 입력·가중치 강조 → 두 숫자 사본을 가운데로 이동 → 큰 곱셈식 → 결과 숫자만 오른쪽 대응 칸에 기록한다. 0도 같은 순서로 기록한다. 한 칸을 곱할 때마다 합계로 옮기는 이전 동작은 실행하지 않는다.
3. **첫 R 합계**: 아홉 곱의 사본을 오른쪽 결과판 자체의 가운데로 모은다. 중앙 곱도 포함한다. 별도 합계 숫자가 나타난 뒤 바로 아래 작은 합계 표시로 내려간다. 원래 곱 결과는 보존한다. 이 모임·낙하 연출은 처음 한 번만 실행한다.
4. **첫 G/B**: 필터를 고정하고 G→B로 자동 전환한다. 채널의 아홉 곱과 합을 짧게 보여준다. 합산식은 오른쪽 고정판에 바로 표시하고 모임 연출은 반복하지 않는다. 완료한 채널 합은 상단 카드에 남긴다.
5. **첫 출력**: 오른쪽 제목을 `같은 위치의 곱`에서 `합성곱 출력 · 3×3`으로 바꾸고 중간 곱을 초기화한다. RGB 색을 유지한 합산식 뒤에 출력값을 필터 중앙에서 오른쪽 1행 1열로 보낸다.
6. **위치 퀴즈**: 필터를 오른쪽 한 칸 옮기고 `이번 출력값은 어느 칸에 들어갈까요?`라고 묻는다. 기존 XR 레이로 출력칸을 직접 선택한다. 첫 출력은 유지하고 두 식 판의 글자는 비운다. 오답은 힌트와 재시도, 정답 1행 2열은 입력을 즉시 닫고 그 위치의 R→G→B 계산을 한 번 시작한다.
7. **계속 이동**: 나머지 일곱 위치 모두에서 `한 칸 이동 → 정렬 → R → G → B → RGB 합 → 출력 기록`을 보여준다. 출력판으로 전환된 뒤 기존 출력값을 중간 곱으로 덮어쓰지 않는다.
8. **요약**: 아홉 출력 뒤 `결과 확인`을 기다린다. 완성된 3×3 결과와 첫 위치 RGB 식을 표시하고 `체험 마치기`를 누르면 출구 이용이 가능해진다. 고정 입력으로 계산한 값을 표시하며 출력값 자체를 하드코딩하지 않는다.

출력 순서는 `(1,1)→(1,2)→(1,3)→(2,3)→(2,2)→(2,1)→(3,1)→(3,2)→(3,3)`이다. 배치·퀴즈·결과 확인·체험 종료은 관람객을 기다리고, 영상의 시간 때문에 강제로 넘기지 않는다.

첫 위치의 R에서 피연산자·곱의 사본과 합계 숫자가 움직이는 구간은 기본 속도의 0.75배로 재생한다. 두 번째부터 아홉 번째 위치로 옮기는 필터 슬라이드는 1.5배속으로, 한 칸에 0.30초가 걸린다.

위치 퀴즈가 시작되면 상단 안내판에 앰버색 `위치 퀴즈` 제목과 오른쪽 3×3 출력판에서 한 칸을 고르라는 안내를 표시한다. 아래에는 필터 이동 힌트와 재시도 안내를 표시하고, 정답을 고르면 상단에 초록색 `정답`을 표시한다.

### 기본 예제의 대조값

```text
R 입력             G 입력             B 입력
0 1 3 0 2          1 2 1 3 0          2 1 0 1 3
1 2 3 1 0          2 2 0 3 1          0 3 2 2 1
3 2 1 2 3          1 0 3 2 2          1 2 1 0 3
0 2 0 2 1          3 1 2 1 0          0 3 1 3 2
1 0 1 3 0          2 3 0 2 1          2 0 3 1 0

첫 R 곱            최종 출력
0 0 0              15 13 18
0 2 0              10 18 12
0 0 1              15 13 12
```

기본 예제의 첫 RGB 식은 `R 3 + G 6 + B 6 = 15`, 퀴즈 다음은 `R 6 + G 4 + B 3 = 13`이다.

## 구현 책임과 수명주기

씬 스크립트는 `client/Assets/Scripts/CNNConvolutionLab/`에 두고 `CNNConvolution` namespace를 사용한다. `CNNConvolutionLabManager`가 실습의 시작·종료, 계산 위치와 진행률을 관리한다. `KernelPlacement`, `Multiplication`, `ChannelSum`, `OutputRecording`, `WindowMovement`, `Quiz`, `ResultSummary`의 Controller는 각 단계의 입력·표시·연출을 담당한다.

| 파일·모듈 | 책임 |
| --- | --- |
| `CNNScreenLabManager`, `PixelImageData` | 3×3 관찰용 스냅샷 생성 |
| `CNNScreenLab/PixelPatchData` | 1-1 내부 관찰용 9픽셀 데이터 |
| `LearningSessionManager` | 공용 `ILearningExperience`를 통한 상태 구독·종료와 누적 진행 기록 |
| `CNNConvolutionLabManager` | 실습의 시작·단계 순서·종료, 계산 위치·진행률·출구 상태 |
| `ConvolutionCalculation` | 고정 입력과 읽기 전용 윈도 위치, 가중치·곱·채널 합·출력 계산 |
| `KernelPlacement/KernelInteractionController` | 배치 단계 진입점. Grip 상태·목표 판정·자동 배치·복귀 순서와 안내 |
| `Multiplication/MultiplicationController` | 아홉 칸 반복, 안내 선택, 연출 호출과 기록 완료 안내 |
| `Multiplication/MultiplicationAnimationController` | 한 칸의 강조·피연산자 이동·곱셈식 표시·결과 기록 |
| `Multiplication/MultiplicationContent` | 칸별 제목, 곱셈 설명, 가중치별 안내와 완료 문구 |
| `ChannelSum/ChannelSumController` | 상세 합산과 첫 G/B·이후 RGB 요약 진행, 완료 카드 갱신 |
| `ChannelSum/ChannelSumAnimationController` | 아홉 사본 모으기와 합계 이동 |
| `ChannelSum/ChannelSumContent` | 상세·요약 합산 문구와 합산식 구성 |
| `OutputRecording/OutputRecordingController` | 첫 출력판 전환, RGB 합 표시, 출력 기록 진행 |
| `OutputRecording/OutputRecordingAnimationController` | 출력값 이동·기록 확정·강조 해제 |
| `OutputRecording/OutputRecordingContent` | 출력 안내와 RGB 합산식 구성 |
| `WindowMovement/WindowMovementController` | 이전 표시 정리, 이동 안내, 필터 이동과 도착 처리 |
| `WindowMovement/WindowMovementContent` | 이동 안내 문구 |
| `Quiz/ConvolutionQuizController` | 퀴즈 진입점. 전달받은 정답 인덱스로 판정·재시도·입력 종료 처리 |
| `ResultSummary/ResultSummaryController` | 요약 진입점. 결과 확인 대기·작업대 숨김·요약 표시·체험 종료 요청 |
| `Workbench/InputGridController` | RGB 입력 셀 생성·값·채널 표시, 입력 셀과 필터 목표 위치 |
| `Workbench/WorkbenchKernelController` | 필터 본체·가중치, XR 잡기·놓기, 위치와 잡기 해제 |
| `Workbench/ResultGridController` | 공유 3×3 셀의 값·표시·강조와 곱셈 결과판·출력판 전환 |
| `Workbench/EquationDisplayController` | 곱셈식·합산식·합계판 표시와 위치 조회 |
| `Workbench/FlyingNumberController` | 이동 숫자의 값·색·크기·위치·가시성 |
| `Workbench/SideNumberController` | 입력·결과 셀의 측면 숫자 동기화와 가시성 |
| `Workbench/WorkbenchColors` | RGB 채널색과 계산 강조색 |
| `Workbench/Construction/WorkbenchBuilder` | 프리팹 기준점으로 공유 오브젝트 최초 생성, 측면 숫자 갱신, 런타임 자원 해제 |
| `Workbench/Construction/WorkbenchPrimitiveFactory` | 타일·라벨·프레임 생성, 공용 좌표 변환과 런타임 재질 |
| `KernelPlacement/KernelPlacementDisplayController` | 목표 윤곽·체크·잡기 화살표·힌트 표시 |
| `Multiplication/MultiplicationDisplayController` | 가중치 사본, 입력·가중치 강조, 연결선, 곱셈 표시 조합 |
| `ChannelSum/ChannelSumDisplayController` | 아홉 합산 사본과 원래 결과 숫자의 표시·복원 |
| `Quiz/OutputSelectionController` | 출력 셀 선택 영역·버튼·레이 입력과 셀 인덱스 전달 |
| `LessonUIController` | 공유 안내 UI 표시, RGB 카드, 메모, 고정 버튼과 결과 요약 표시·클릭 이벤트 |
| `ConvolutionLabBuilder` | 저장된 두 전용 프리팹과 같은 참조를 사용하는 씬 구성 경로 |

현재 위치는 `CNNConvolutionLabManager`가 선택한다. `ConvolutionCalculation.ForWindow`는 같은 고정 입력을 공유하는 새 읽기 전용 계산 객체를 반환한다. 기존 객체의 위치를 바꾸지 않으며 입력 배열도 외부로 공개하지 않는다. 필터 이동 연출이 끝난 뒤 Manager가 새 계산 객체를 선택해 계산 기능에 전달한다. 공유 작업대는 현재 계산 객체를 보관하지 않으며 초기 격자 설정 외에는 전달받은 값만 표시한다. 계산 기능은 해당 객체를 읽고, 퀴즈는 정답 인덱스만 받으며, 요약은 새로 계산한 출력 배열과 첫 RGB 합 배열만 받는다. 단계마다 계산 객체를 수정하거나 다음 단계를 호출하지 않는다.

공유 작업대는 `Workbench/`에 둔다. `CNNConvolutionLabManager`가 첫 실행에서 WorkbenchBuilder로 공유 컴포넌트를 만들고 각 기능에 필요한 컴포넌트만 연결한다. 의존 방향은 `CNNConvolutionLabManager → 기능 Controller → 기능 내부 표시·입력 → 공유 작업대 컴포넌트`다. 공유 작업대는 기능의 진입점이나 진행 순서를 참조하지 않는다.

결과판은 곱셈 결과와 합성곱 출력을 같은 3×3 셀에 표시한다. 퀴즈의 OutputSelectionController는 초기 생성 시 기존 셀 Canvas에 버튼과 XR Raycaster를 연결해 선택 위치를 유지한다. 퀴즈 내부에서만 선택 입력을 켜고 끄며, 필터 이동은 퀴즈를 조작하지 않는다. 곱셈과 합산의 전용 사본은 각각의 표시 Controller가 생성·관리한다. 이동 숫자 하나는 곱셈·합산·출력 기록에서 공유한다.

`CNNConvolutionLabManager`는 `Awake`에서 요약 Controller를 만들고 실습 시작 때 작업대와 나머지 Controller를 연결한다. `OnEnable`과 `OnDisable`에서 요약 이벤트를 구독하고 해제한다. 결과 확인 이벤트를 받으면 작업대를 정리하고 숨긴다. 요약 Controller는 요약 화면에서 **체험 마치기** 클릭을 한 번 받아 버튼을 비활성화하고 Manager에 `FinishRequested`를 전달한다.

종료·컴포넌트 비활성화에서는 Manager가 먼저 소유 코루틴을 중단하고 단계의 `Cancel`로 입력 구독을 해제한 뒤 공유 작업대의 숫자 사본, 퀴즈 입력, 필터 잡기와 강조를 정리한다. 자동 배치·비활성화·폐기에 의한 잡기 취소는 작업대가 사용자 손 놓기 이벤트로 전달하지 않으므로 별도 복귀 동작을 시작하지 않는다. 첫 합계 중간에 중단해도 중앙 결과 글자의 숨김 상태를 복원한다. 재시작은 새 계산 객체와 출력판·카드 상태로 시작한다. 작업대 내부 XR 리스너는 생성부터 폐기까지 유지하고 폐기 시 해제한다.
