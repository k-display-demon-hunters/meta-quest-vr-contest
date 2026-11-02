# 픽셀 관찰과 합성곱 체험

## 씬과 진행

두 체험은 Core/Learning의 동등한 학습 단계다. 진행 순서는 Tutorial → ScreenLab → ConvolutionLab → FeatureMap → ModelDesign이다. LearningSessionManager가 단계·양피지 조건·이벤트 연결을 조율하고 FactorySceneLoadController가 준비·배치·활성화·언로드한다.

CNNScreenLab은 이미지·돋보기 선택, 선택한 3×3 픽셀의 RGB 전개와 원래 0~255 값 관찰까지 담당한다. 픽셀 설명과 RGB 세 페이지를 확인한 뒤 관찰 마치기를 누르면 완료한다. 이동 대기 중에는 관찰 결과를 유지하며 처음으로를 누르면 현재 체험의 완료 상태를 초기화한다. 세션의 누적 최대 진행 기록은 유지한다.

CNNConvolutionLab은 스크린 없는 입체 RGB 계산 체험이다. 연습용 0~3 값 소개 → 별도의 0/1 가중치 등장 → R 계산 안내 → G 계산 안내 → B 퀴즈 → 결과 확인을 한 공간에서 진행한다. 각 채널의 가중치는 대각선 세 칸이 1이고 나머지가 0이다. B 퀴즈는 숫자 블록 또는 버튼으로 답하며 오답은 재시도한다. 자동 배치는 물리 조작만 보조한다.

## 데이터와 책임

Screen의 PixelImageFactory가 기존 OpeningBroadcast.png를 128×72 축소·5×5 블러·32×18 샘플링한다. 선택 시 PixelPatchData에 Color32 9개를 복사한다. 관찰 표시는 원래 값을 사용한다. 세션은 다음 Convolution을 준비할 때 Screen 진입점의 SelectedPatch를 읽어 Convolution 진입점의 SetInput에 전달한다. 전체 이미지·좌표·Transform·계산 객체는 전달하지 않는다.

직접 Convolution으로 이동하거나 단독 실행하면 코드에 명시된 기본 9픽셀 예제로 시작하며 UI에 기본 예제임을 표시한다. 순차 진행은 직전 Screen의 선택값을 사용한다. ConvolutionCalculation이 성분별 RoundToInt(value × 3 / 255)로 int[3,9]를 만든다. 관찰 원본은 변경하지 않는다.

| 위치 | 책임 |
| --- | --- |
| Scripts/CNNScreenLab/CNNScreenLabManager.cs | 이미지 수명·관찰 순서·선택 스냅샷·Screen 완료 |
| Scripts/CNNScreenLab/PixelBoardController.cs | 이미지 표시·기존 축소 이동·관찰 단계별 전체 배치 오프셋 |
| Scripts/CNNScreenLab/PixelSelection | 돋보기 입력·선택 판정·선택 픽셀 표시 |
| Scripts/CNNScreenLab/RGBObservation | 선택 영역 RGB 공간 전개와 27개 원본 숫자 표시 |
| Scripts/CNNConvolutionLab/CNNConvolutionLabManager.cs | 입력·시작·종료·진행률과 학습 모듈 연결 |
| Scripts/CNNConvolutionLab/ConvolutionLessonManager.cs | 안내·가중치·R/G 학습·B 퀴즈·결과 순서와 UI 입력 연결 |
| Scripts/CNNConvolutionLab/CalculationLesson | R/G 곱셈·누적 합 안내와 애니메이션 |
| Scripts/CNNConvolutionLab/Quiz | B 정오답과 숫자 블록 답 슬롯 |
| Scripts/CNNConvolutionLab/RgbVolumeController.cs | 계산 공간 표시와 내부 층·가중치·답 블록 조작 연결 |
| Scripts/Core/PixelPatchData.cs | 읽기 전용 선택 픽셀 데이터 |
| Scripts/Core/PixelUIInputController.cs | 선택적 EventSystem·XR UI 입력 보조 |

씬 Manager와 학습 모듈은 각자의 활성 기간에 이벤트를 구독·해제한다. 종료 시 학습 코루틴·퀴즈 대기·상호작용·입력과 표시를 정리한 뒤 공장 로더가 씬을 언로드한다. 기존 FilterAlignmentController와 ResultObservationController는 현재 학습 흐름에서 호출하지 않는다.

## 공간과 에셋

Screen과 Convolution의 입장 문서는 모두 ID 0(표시 번호 01)이다. 내부 셔터는 Screen 출구와 Convolution 입구가 공유한다. 관찰 완료·다음 씬 준비·문서 수집을 모두 충족하면 열리고 두 번째 영역 진입 시 닫힌다. 다음 씬 준비만으로 공유 셔터를 열지 않는다. 준비 이후 Screen을 초기화하면 세션이 다음 씬을 취소하고 입구·안내선을 닫는다.

공장의 기존 공간·문·경로·콘텐츠 기준점을 일반 목적지 데이터로 연결한다. Convolution 기준점의 배율은 2이며 기존 콘텐츠 표시 크기를 유지한다. 태블릿은 픽셀 관찰·합성곱·특징 맵·모델 제작 네 체험을 제공한다. 전체 진행은 네 체험의 평균이며 Convolution은 R/G/B 완료에 따라 1/3씩 진행한다.

Screen의 로드 기준 위치에서 첫 픽셀 선택 화면은 화면 왼쪽으로 0.15m, RGB 전개 화면부터 관찰 종료 안내까지는 화면 오른쪽으로 0.5m 이동한다. PixelBoardController가 씬에 연결된 전체 콘텐츠 루트에 각 오프셋을 절대 적용하므로 이미지·돋보기·RGB·설명·버튼이 함께 이동하고, 반복 전환·초기화로 이동량이 누적되지 않는다. 기존 MoveAside 축소 이동은 유지하며 처음으로를 누르면 첫 화면 위치로 복원한다. 공장의 Y 회전 -90°에서 화면 오른쪽은 월드 +Z이며, 루트 위치는 첫 화면 (-17.6,0,-2.60), RGB 화면 (-17.6,0,-1.95)다.

Prefabs/CNNScreenLab의 FilterDisplay·ImagePixelGrid·RgbPixelStack은 관찰 전용이다. Prefabs/CNNConvolutionLab/PixelCalculationUI.prefab은 계산 전용이다. 기존 .meta와 GUID를 보존해 이동했으며 두 씬 사이에 계산 UI 인스턴스를 공유하지 않는다. 원본 이미지는 Textures/CNNScreenLab/OpeningBroadcast.png, 관찰용 재질은 Materials/CNNScreenLab에 둔다. 파일 내용·GUID와 기존 폰트 라이선스 고지를 유지한다.

ScreenLabBuilder와 ScreenLabVisualsBuilder는 관찰 표시만 연결하고 ConvolutionLabBuilder는 학습 모듈·전용 계산 프리팹을 연결한다. 저장된 씬에 같은 참조를 반영했다.

## 합성곱 계산 작업대

`Prefabs/CNNConvolutionLab/ConvolutionWorkbench.prefab`은 RgbVolumeController와 고정 작업대(상판·지지대·거치대·명판), 입력·필터 대기·필터 정렬·곱 결과·합계·이동 계산식의 기준 Transform을 보관한다. CNNConvolutionLab 씬과 ConvolutionLabBuilder가 같은 프리팹을 연결한다. 작업대의 로컬 배율 1.5와 위치 (0,0,-0.55)는 프리팹에 저장하며 안내 UI와 평행하게 배치한다. 고정 작업대는 채널 Focus에서 표시하고 다음 채널을 꺼내거나 도입을 다시 시작하면 숨긴다. 공장 기준점·배율·동선은 변경하지 않았다.

입력 타일은 어두운 몸체·RGB 데이터 면·중성색 숫자 면으로 나뉜다. 가중치는 얇은 금속 프레임과 홈이 있는 손잡이를 가지며, 활성 필터 손잡이에도 잡기 Collider가 연결된다. 결과판은 테두리와 뒷판을 갖고 미계산 칸은 점으로 표시한다. 합계는 별도의 테두리 표시부에 나타난다. 고정 작업대의 세 재질은 `Materials/CNNConvolutionLab/`에 있다. 타일·필터·숫자와 기존 연산 표시는 GeometryBuilder가 실행 중 생성한다. 일반 타일의 발광은 제거하고 배치 안내에만 약한 발광을 사용한다.

RgbVolumeController가 프리팹 기준점을 내부 계산·표시에 연결한다. KernelInteractionController는 전달받은 대기·정렬 기준점으로 필터를 복귀·배치하고, CalculationAnimationController는 계산식 기준점을 사용한다. 이동 숫자와 연결선은 각 숫자 표시의 실제 위치에서 출발한다. 씬 Manager나 다른 모듈에는 내부 기준점을 공개하지 않는다.

PixelCalculationUI는 상단 제목·행동 안내, 입력 아래의 마지막 계산식, 하단 계산 메모·피드백·고정 다음 버튼으로 배치한다. R/G 안내 계산은 현재 줄의 최대 세 계산식만 메모에 남기고 다음 줄에서 비운다. 누적 합은 별도 표시하며 계산 규칙·줄 단위 진행·B 숫자 블록/버튼 답변·정오답 처리는 유지한다. R 계산을 기준으로 만든 외형과 배치는 같은 표시 구현을 사용하는 G/B에도 적용된다. 최종 결과는 기존 텍스트 요약 방식이다.

### 필터 배치 피드백

KernelInteractionController가 필터의 대기·호버·이동·배치 가능·복귀·정렬·완료 상태를 관리한다. RgbVolumeController의 WaitForKernel이 배치 대기 중에만 UpdatePlacement를 호출한다. 상태가 바뀔 때 목표 형태·손잡이 홈의 색과 발광·안내 문구를 갱신한다. 상단 배치 안내 Text는 ConvolutionWorkbench의 Equation anchor 아래에 저장하고 `_placementHint`로 연결한다. 계산이 시작되면 배치 안내를 숨겨 이동 계산식과 겹치지 않게 한다.

| 상태 | 표시와 동작 |
| --- | --- |
| 대기·호버 | 고정 크기 점선 목표, 손잡이 강조와 Grip 안내. 호버 시 손잡이 발광을 높임 |
| 잡고 이동 | 화살표와 필터 제목을 숨기고 점선 목표로 이동하도록 안내 |
| 배치 가능 | 목표를 따뜻한 색의 실선으로 전환하고 놓기 안내. 입력 채널의 초록색과 완료 상태를 혼동하지 않도록 녹색에 의존하지 않음 |
| 범위 밖에서 놓기 | 0.30초 동안 원래 거치대로 복귀한 후 다시 잡기 가능 |
| 정렬 | 0.18초 동안 목표 위치·회전으로 이동. 이동 중 다시 잡기는 비활성화 |
| 완료 | 목표 옆 체크와 완료 안내를 0.45초 표시한 후 계산 진행 |

배치 가능 표시와 놓기 판정은 동일한 월드 거리 0.3m 이하 조건을 사용한다. 기존 회전 추적 비활성 설정을 유지한다. 자동 배치는 잡은 필터의 선택을 해제하고 같은 정렬·완료 경로를 사용하며, 정렬 중 반복 요청으로 이동 시간을 다시 시작하지 않는다. 부모로 돌아올 때 월드 크기를 보존한다. 채널 전환·종료 시 이동과 안내를 취소하고 배치 상태를 초기화한다. 이 피드백은 R/G 안내와 B 퀴즈의 공통 필터 배치에 적용된다.

## 확인 범위

이번 변경은 코드·씬 및 프리팹의 직렬화 참조·GUID·경로를 정적으로 검토했다. Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다. 실제 XR 조작·공간 가독성과 연속 학습 실행은 실행 확인 전이다.

관찰 UI는 ScreenLabUIController, 관찰 씬 생성 메뉴는 ScreenLabBuilder가 담당한다. 과거 필터 패널·네온 노드·전용 XR 프리팹과 그 미사용 재질·메시·셰이더, 과거 시안 이미지는 참조 확인 후 삭제했다.
