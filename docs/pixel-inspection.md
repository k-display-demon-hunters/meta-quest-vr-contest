# 픽셀 관찰과 합성곱 체험

## 씬과 진행

두 체험은 Core/Learning의 동등한 학습 단계다. 진행 순서는 Tutorial → ScreenLab → ConvolutionLab → FeatureMap → ModelDesign이다. FactorySceneLoadController가 지역 진입 이벤트에 따라 다음 씬을 선택·로드·활성화·언로드한다. LearningSessionManager는 결과 이벤트를 받아 초기 데이터·진행률 구독·완료 기록·종료 처리를 담당한다. FactoryEnvironmentManager는 로딩 결과와 양피지 조건으로 문을 제어하고 콘텐츠를 배치한다.

CNNScreenLab은 이미지·돋보기 선택, 선택한 3×3 픽셀의 RGB 전개와 원래 0~255 값 관찰까지 담당한다. 픽셀 설명과 RGB 세 페이지를 확인한 뒤 관찰 마치기를 누르면 완료한다. 이동 대기 중에는 관찰 결과를 유지하며 처음으로를 누르면 현재 체험의 완료 상태를 초기화한다. 세션의 누적 최대 진행 기록은 유지한다.

CNNConvolutionLab은 스크린 없는 입체 RGB 계산 체험이다. 연습용 0~3 값 소개 → 별도의 0/1 가중치 등장 → R 계산 안내 → G 계산 안내 → B 퀴즈 → 결과 확인을 한 공간에서 진행한다. 각 채널의 가중치는 대각선 세 칸이 1이고 나머지가 0이다. B 퀴즈는 숫자 블록 또는 버튼으로 답하며 오답은 재시도한다. 자동 배치는 물리 조작만 보조한다.

## 데이터와 책임

Screen의 PixelImageFactory가 기존 OpeningBroadcast.png를 128×72 축소·5×5 블러·32×18 샘플링한다. 선택 시 PixelPatchData에 Color32 9개를 복사한다. 관찰 표시는 원래 값을 사용한다. Screen 구역에 들어가면 Convolution도 미리 활성화한다. 세션은 ContentLoaded에서 선택값이 있으면 전달하고, Screen 관찰 완료 시 최종 SelectedPatch를 SetInput에 전달한 뒤 공장에 진행 가능 상태를 알린다. Convolution.SetInput은 입력 참조만 저장하며 연습은 Start에서 시작한다. Start 이후 전달한 선택값은 실행 중인 연습에 반영되지 않는다. 전체 이미지·좌표·Transform·계산 객체는 전달하지 않는다.

Screen 선택값 없이 Convolution을 로드하거나 단독 실행하면 코드에 명시된 기본 9픽셀 예제로 시작하며 UI에 기본 예제임을 표시한다. 순차 진행은 직전 Screen의 선택값을 사용한다. ConvolutionCalculation이 성분별 RoundToInt(value × 3 / 255)로 int[3,9]를 만든다. 관찰 원본은 변경하지 않는다.

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

Screen과 Convolution의 입장 문서는 모두 ID 0(표시 번호 01)이다. 내부 셔터는 Screen 출구와 Convolution 입구가 공유한다. 관찰 완료·다음 콘텐츠 활성화와 첫 프레임 처리·문서 수집을 모두 충족하면 열리고 두 번째 영역 진입 시 닫힌다. 입장 시 Screen을 이전 씬으로 유지하고 이미 실행 중인 Convolution을 현재 단계로 지정한다. 다음 씬 준비만으로 공유 셔터를 열지 않는다. 준비 이후 Screen을 초기화하면 공장이 다음 입구·안내선을 닫고 미리 로드한 Convolution은 유지한다. 다시 관찰을 완료하면 새 입력을 전달한 뒤 문 개방 조건을 갱신한다.

공장의 기존 공간·문·경로·콘텐츠 기준점을 일반 목적지 데이터로 연결한다. Convolution 기준점의 배율은 2이며 기존 콘텐츠 표시 크기를 유지한다. 태블릿은 픽셀 관찰·합성곱·특징 맵·모델 제작 네 체험을 제공한다. 전체 진행은 튜토리얼을 포함한 다섯 체험의 평균이며 Convolution은 R/G/B 완료에 따라 1/3씩 진행한다.

Screen의 로드 기준 위치에서 첫 픽셀 선택 화면은 화면 왼쪽으로 0.15m, RGB 전개 화면부터 관찰 종료 안내까지는 화면 오른쪽으로 0.5m 이동한다. PixelBoardController가 씬에 연결된 전체 콘텐츠 루트에 각 오프셋을 절대 적용하므로 이미지·돋보기·RGB·설명·버튼이 함께 이동하고, 반복 전환·초기화로 이동량이 누적되지 않는다. 기존 MoveAside 축소 이동은 유지하며 처음으로를 누르면 첫 화면 위치로 복원한다. 공장의 Y 회전 -90°에서 화면 오른쪽은 월드 +Z이며, 루트 위치는 첫 화면 (-17.6,0,-2.60), RGB 화면 (-17.6,0,-1.95)다.

Prefabs/CNNScreenLab의 FilterDisplay·ImagePixelGrid·RgbPixelStack은 관찰 전용이다. Prefabs/CNNConvolutionLab/PixelCalculationUI.prefab은 계산 전용이다. 기존 .meta와 GUID를 보존해 이동했으며 두 씬 사이에 계산 UI 인스턴스를 공유하지 않는다. 원본 이미지는 Textures/CNNScreenLab/OpeningBroadcast.png, 관찰용 재질은 Materials/CNNScreenLab에 둔다. 파일 내용·GUID와 기존 폰트 라이선스 고지를 유지한다.

ScreenLabBuilder와 ScreenLabVisualsBuilder는 관찰 표시만 연결하고 ConvolutionLabBuilder는 학습 모듈·전용 계산 프리팹을 연결한다. 저장된 씬에 같은 참조를 반영했다.

## 확인 범위

이번 변경은 코드·씬 및 프리팹의 직렬화 참조·GUID·경로를 정적으로 검토했다. Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다. 실제 XR 조작·공간 가독성과 연속 학습 실행은 실행 확인 전이다.

관찰 UI는 ScreenLabUIController, 관찰 씬 생성 메뉴는 ScreenLabBuilder가 담당한다. 과거 필터 패널·네온 노드·전용 XR 프리팹과 그 미사용 재질·메시·셰이더, 과거 시안 이미지는 참조 확인 후 삭제했다.
