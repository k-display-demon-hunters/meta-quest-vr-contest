# CNN 픽셀 관찰과 합성곱 구현

[CNNPixelLab.unity](../client/Assets/Scenes/CNNPixelLab.unity)는 PixelLab 체험 전체를 관리하는 씬이다. 이미지·RGB 관찰은 [CNNScreenLab.unity](../client/Assets/Scenes/CNNScreenLab.unity), 합성곱 학습은 [CNNConvolutionLab.unity](../client/Assets/Scenes/CNNConvolutionLab.unity)에 있다. 관리용 씬은 유지하고 두 체험 씬을 같은 방에서 교체한다. 조작은 [통합 조작법](xr-simulator.md), 공장 연결은 [공장 환경](factory-environment.md)을 참고한다.

## 파일과 책임

런타임 스크립트의 루트는 `Scripts/CNNPixelLab/`이다.

| 위치 | 역할 |
|---|---|
| `CNNPixelLabManager.cs` | 두 체험의 이벤트·진행 순서, 선택한 픽셀과 복귀 좌표, 전체 완료 상태를 소유한다. Factory가 사용하는 단일 진입점이다. |
| `PixelLabSceneLoadController.cs` | 내부 체험 씬을 비동기로 준비·배치·교체하고 XR 선택 해제와 언로드를 담당한다. |
| `CNNScreenLab/CNNScreenLabManager.cs` | 관찰 진행과 UI를 연결하고 ConvolutionRequested·ResetRequested를 상위 Manager에 알린다. |
| `CNNScreenLab/Image/PixelImageManager.cs` | 이미지 데이터·텍스처·선택·RGB 전개를 관리하고 CapturePatch로 선택 영역의 복사본을 만든다. |
| `CNNScreenLab/Image/PixelImageFactory.cs` | 원본을 128×72로 축소하고 5×5 블러를 적용한 후 32×18 픽셀을 추출한다. |
| `CNNScreenLab/Image/PixelSelectionController.cs` | 필터 위치를 3×3 좌표로 변환하고 놓기 정렬·선택 복원을 담당한다. |
| `CNNScreenLab/Image/FilterVolumeController.cs` | 필터 프레임과 선택한 9개 입체 픽셀을 표시한다. |
| `CNNScreenLab/Image/PixelRevealStage.cs` | 사진을 열별 입체 픽셀로 전환하고 RGB 전개 완료를 알린다. |
| `CNNScreenLab/Image/RgbPixelStackController.cs` | 선택 영역을 확대하고 RGB 세 층과 성분 숫자를 펼친다. |
| `CNNScreenLab/Rendering/PixelVolumeMesh.cs` | 관찰용 입체 블록 메시와 재질을 생성한다. |
| `CNNScreenLab/UI/PixelLabUIController.cs` | 자세히 보기·다음으로·처음으로와 안내를 갱신한다. |
| `CNNConvolutionLab/CNNConvolutionLabManager.cs` | Begin으로 계산 데이터를 받고 ReturnRequested·Completed를 상위 Manager에 알린다. |
| `CNNConvolutionLab/Lesson/PixelLessonManager.cs` | 입력을 int[3,9]로 변환하고 학습·결과 표시를 조율한다. |
| `CNNConvolutionLab/Lesson/RgbConvolutionView.cs` | 계산 UI와 결과, 계산 다시 보기·다른 영역 선택 요청을 표시한다. |
| `CNNConvolutionLab/Lesson/ConvolutionExercise.cs` | 정답 계산과 R 설명·G/B 퀴즈·RGB 합산을 진행한다. |
| `CNNConvolutionLab/Lesson/Spatial/` | Controller가 공간 애니메이션, ViewBuilder가 블록·라벨 생성, 두 InteractionController가 잡기 판정을 담당한다. |

PixelLab 내부에서 공유하는 데이터 계약과 입력 보조는 `Scripts/CNNPixelLab/PixelPatchData.cs`, `Scripts/CNNPixelLab/PixelUIInputController.cs`에 있다. 두 파일은 관찰·합성곱 하위 체험에서만 사용하므로 PixelLab 기능 루트에 둔다. PixelPatchData는 위쪽 행부터 왼쪽→오른쪽 순서의 Color32 9개를 복사하고 인덱서로 값만 공개한다. RGB는 0~255 정수이며 alpha는 계산에 사용하지 않는다. 입력 보조는 기존 EventSystem을 사용하고 필요한 경우 입력 모듈을 준비한다. 네임스페이스는 기존 `UI`를 유지하며, 기존 파일 이동 시 .meta와 GUID를 보존했다.

## 의존 구조와 상태 소유

```text
FactoryLearningFlowManager → CNNPixelLabManager
                              ├─ PixelLabSceneLoadController
                              ├─ CNNScreenLabManager → 이미지·선택·관찰·UI
                              └─ CNNConvolutionLabManager → Lesson → 계산·공간 표시·입력
```

Factory는 ConvolutionCompleted만 조회하고 다음 방으로 진행한다. 관찰·합성곱 Manager나 픽셀·좌표 데이터는 참조하지 않는다. 하위 체험은 서로와 Factory를 호출하지 않고 로컬 C# 이벤트로 상위 Manager에 요청과 완료를 알린다. 구독은 활성 기간에 연결·해제하고, 체험 교체 시 이전 구독을 끊고 새 구독을 연결한다.

선택 데이터·복귀 좌표·전체 완료 상태의 소유자는 CNNPixelLabManager다. 계산 씬에는 PixelPatchData만 전달하며 전체 이미지·좌표·이전 씬의 Transform을 넘기지 않는다. ConvolutionCompleted는 합성곱 완료 뒤 참이며, 내부 전환·종료 중에는 거짓이다. 새 체험 교체 성공이나 관찰 초기화 때 완료 상태를 지운다. 로드 실패 시 기존 씬과 기존 완료 상태를 유지한다.

## 초기화와 관찰 흐름

공장은 비활성 관리용 루트를 로드·배치한 후 활성화한다. CNNPixelLabManager.Start가 관찰 씬을 로드한다. CNNScreenLabManager.Awake에서 Camera.main과 직렬화된 폰트 참조로 이미지와 기본 UI를 초기화한다. 계산 Canvas 참조는 없다.

원본은 `Textures/CNNPixelLab/OpeningBroadcast.png`다. 사진 표시·필터·RGB·계산에 사용하는 색상은 같은 32×18 샘플에서 나온다. 사진은 Bilinear, 렌즈 텍스처는 Point 필터링을 사용한다.

1. 필터를 Grip으로 잡아 이미지 위에 놓으면 PixelSelectionController가 거리·방향을 판정하고 3×3 선택을 확정한다.
2. 자세히 보기가 PixelImageManager.BeginReveal을 호출한다. 필터를 숨기고 PixelRevealStage.ShowImage와 RevealPixels를 연속 호출한다.
3. 사진이 컬러 입체 픽셀로 바뀌고 선택 영역의 R/G/B가 깊이 방향으로 펼쳐진다. 사진 Renderer는 숨긴다. 깊이는 실제 디스플레이 구조나 색 세기를 뜻하지 않는다.
4. 완료 이벤트로 다음으로 버튼을 활성화한다. 다음으로는 PixelPatchData와 선택 좌표를 ConvolutionRequested로 알린다.
5. 상위 Manager가 데이터를 보관하고 합성곱 씬 교체를 요청한다. 활성화 후 CNNConvolutionLabManager.Begin에 픽셀 복사본을 전달한다.

필터의 복귀 위치·회전은 보드 기준 로컬 좌표다. 이미지 크기는 4.8×2.7m, 필터 시작 위치는 콘텐츠 로컬 (-2.4, 1.15, -0.6)이다. 처음으로는 선택·전개를 초기화하고 상위 Manager에 완료 초기화를 알린다.

## 같은 방에서의 씬 교체와 종료

FactorySceneLoadController는 관리용 콘텐츠에 기존 ContentAnchor.position - Vector3.forward * 1.5f 오프셋을 한 번 적용한다. PixelLabSceneLoadController는 두 체험의 콘텐츠 루트를 관리용 루트의 월드 위치·회전에 배치하고 Rigidbody를 동기화한다. 두 체험 루트는 원점·회전 identity·스케일 1로 작성되어 있다. 계산 Canvas의 상대 위치·회전·배율은 기존 배치를 유지한다.

새 체험은 비활성 상태로 준비한다. 준비 성공 시 이전 체험에서 잡은 물체의 XR 선택을 해제하고 콘텐츠를 숨겨 언로드한 뒤 새 체험을 활성화한다. 준비 중에는 잠깐 두 체험 씬이 함께 존재하며 교체 완료 후에는 하나만 남는다. 공장 활성 씬과 공용 XR 리그·태블릿을 유지하고 플레이어 위치·시점을 이동시키지 않는다. 로드 실패는 로그로 알리며 기존 체험에서 요청을 다시 할 수 있다.

다음 방 진입 시 FactoryLearningFlowManager는 CNNPixelLabManager.EndSession을 기다린다. Manager는 새 요청을 차단하고 진행 중인 로딩·교체가 끝나기를 기다린 뒤 구독과 내부 체험 씬을 정리한다. 이후 FactorySceneLoadController가 관리용 씬을 언로드한다. 관리용 씬이 외부에서 제거되면 내부 로더의 OnDestroy가 남은 씬을 정리하며, 로딩 중이었다면 완료 콜백에서 정리한다.

## 합성곱과 완료

PixelLessonManager가 입력을 int[3,9]로 변환하고 바로 계산을 시작한다. ConvolutionExercise가 R 설명, G/B 퀴즈와 RGB 합산을 진행한다. 완료는 CNNConvolutionLabManager.Completed → CNNPixelLabManager로 전달된다. 결과 화면의 계산 다시 보기는 같은 데이터로 계산을 반복한다. 다른 영역 선택은 상위 Manager에 복귀를 요청하고, 관찰 씬을 다시 로드한 뒤 저장한 좌표를 복원한다. 관찰 전개 자체는 다시 수행한다.

## 에디터 도구와 확인 범위

PixelLabBuilder는 관리용 씬, SpatialPixelLabBuilder는 관찰 씬, ConvolutionLabBuilder는 합성곱 씬을 구성한다. PixelRevealStageBuilder는 관찰 씬의 전개 컴포넌트와 버튼을 연결한다. 씬 파일은 모두 `Assets/Scenes/`에 둔다.

이번 변경은 소스·Unity YAML 참조·GUID·배치 값의 정적 확인만 수행했다. Unity 실행·컴파일·테스트·빌드는 수행하지 않았다. 아래 기록은 분리 전 구현에 대한 과거 확인이며 현재 실행 검증을 대신하지 않는다.

## 필터·픽셀 입체 표시 (2026-10-26)

`FilterVolumeController`는 기존 필터 잡기 입력과 원본 샘플 좌표를 유지하면서 얇은 프레임·평면 렌즈 표시를 모서리가 다듬어진 두꺼운 프레임과 3×3 픽셀 블록으로 교체한다. `PixelVolumeMesh`의 공유 메시와 재질을 사용하고 선택 색상은 MaterialPropertyBlock으로 갱신한다. 밝기나 가중치에 따라 두께를 바꾸지 않는다. 투명도는 0.94이며 발광·추가 장식 조명은 사용하지 않는다.

`PixelRevealStage`도 평면 Image 대신 동일한 메시의 픽셀 블록을 표시한다. 열별 전환 시 블록과 앞면 숫자가 이미지 앞쪽으로 함께 나온다. 색 선택·숫자·합성곱 데이터는 기존 값 그대로이며, 숫자는 앞면에 남고 옆면으로 깊이를 확인할 수 있다. 생성 메시·재질은 소유 Controller/Stage의 종료 시 해제한다. 이번 변경은 소스 검토까지 수행했으며 컴파일·Play·APK 확인 전이다.

## 2026-10-26 Unity 실행 확인

Unity 6000.3.24f1 Play에서 CNNPixelLab을 실행하고 실제 Camera.Render 결과로 정면 필터와 측면 픽셀 화면을 확인했다. 초기 MaterialPropertyBlock 필드 생성으로 발생한 Unity 생성자 예외를 BuildPixels 초기화로 옮겨 해결했다. 숫자 Canvas의 정렬 순서를 높이고 전환 완료 후 원본 사진 Renderer를 숨겨 반투명 블록 뒤 사진의 색 혼합과 숫자 가림을 수정했다. ResetStage에서는 사진을 다시 표시한다.

최종 컴파일 완료·실패 없음, RevealPixels의 Revealing→Revealed 전환, G/B 버튼 onClick 호출 후 각각 576개 숫자의 원본 성분값 일치, NextStage 호출 후 합성곱 화면 활성화를 확인했다. 이는 에디터 실행·직접 콜백 검증이며 Quest 컨트롤러 레이 클릭이나 공장 전체 동선의 실기기 검증은 아니다. 자동화 연결의 Play/domain reload 중 응답 시간 초과 로그와 수정 전 예외 이력은 최종 콘텐츠 오류와 구분했다. 사용자의 후속 요청에 따라 APK 재빌드·기기 설치는 수행하지 않았다. 새 테스트 파일은 생성하지 않았다.


## 2026-10-29 입체 관찰 동선 변경

새 RGB 층 표시는 `Scripts/CNNPixelLab/CNNScreenLab/Image/RgbPixelStackController.cs`가 담당한다. 공용 XR 입력·공장 이동·다음 방 완료 조건은 유지한다. 기존 별도 RGB 관찰 화면은 이번 진입 흐름에서 호출하지 않으며 합성곱 설명·퀴즈는 기존 계산 화면을 사용한다. Unity 6000.3.24f1 컴파일 성공을 확인했다. Play에서 선택 상태를 설정하여 Revealing→Revealed 완료, RGB 세 층의 서로 다른 깊이, 다음으로 합성곱 화면 활성화, 처음으로 초기화를 확인했다. MaterialPropertyBlock 생성자 초기화 오류를 Initialize로 옮겨 수정했다. 실제 카메라 렌더링으로 입체 블록과 숫자 표시도 확인했으나, 근접 정면에서는 뒤 층 숫자가 겹쳐 보이는 한계가 있다. 검증은 에디터 콜백 기반이며 Quest 직접 잡기·공장 전체 동선 검증이나 APK 빌드는 수행하지 않았다.

## 2026-10-29 역할 분리 브랜치 병합

입체 관찰 동선을 PixelImageManager의 전환·선택 관리에 반영하고, PixelRevealStage와 RgbPixelStackController는 읽기 전용 픽셀 목록을 전달받도록 연결했다. PixelLessonManager의 계산·완료 이벤트와 공용 태블릿의 역할 분리는 유지한다. 위 Unity 실행 기록은 원본 브랜치에서 수행한 결과이며, 이번 병합 결과에는 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다.
