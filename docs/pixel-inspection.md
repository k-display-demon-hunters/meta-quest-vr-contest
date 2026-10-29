# CNN 픽셀 관찰과 합성곱 구현

[CNNPixelLab.unity](../client/Assets/Scenes/CNNPixelLab.unity)는 PixelLab 체험 전체를 관리하는 씬이다. 이미지·RGB 관찰은 [CNNScreenLab.unity](../client/Assets/Scenes/CNNScreenLab.unity), 합성곱 학습은 [CNNConvolutionLab.unity](../client/Assets/Scenes/CNNConvolutionLab.unity)에 있다. 관리용 씬은 유지하고 두 체험 씬을 같은 방에서 교체한다. 조작은 [통합 조작법](xr-simulator.md), 공장 연결은 [공장 환경](factory-environment.md)을 참고한다.

## 파일과 책임

런타임 스크립트의 루트는 `Scripts/CNNPixelLab/`이다.

| 위치 | 역할 |
|---|---|
| `CNNPixelLabManager.cs` | 두 체험의 이벤트·진행 순서, 선택한 픽셀과 복귀 좌표, 전체 완료 상태를 소유한다. Factory가 사용하는 단일 진입점이다. |
| `PixelLabSceneLoadController.cs` | 내부 체험 씬을 비동기로 준비·배치·교체하고 XR 선택 해제와 언로드를 담당한다. |
| `CNNScreenLab/CNNScreenLabManager.cs` | ScreenLab의 유일한 Manager다. Factory·보드·선택·전개·UI를 연결하고 관찰 진행을 조율하며 ConvolutionRequested·ResetRequested를 상위 Manager에 알린다. |
| `CNNScreenLab/PixelImageFactory.cs` | 원본 Texture2D를 Inspector로 받는다. Create에서 128×72 축소·5×5 블러·32×18 샘플링·픽셀 텍스처 생성을 완료하고 PixelImageData를 반환한다. |
| `CNNScreenLab/PixelImageData.cs` | 생성된 사진·픽셀 텍스처와 읽기 전용 픽셀 목록을 보관한다. CNNScreenLabManager가 소유하고 종료 시 Dispose를 호출해 생성 텍스처를 해제한다. 원본 에셋은 해제하지 않는다. CapturePatch는 선택 좌표에서 위쪽 행부터 픽셀 9개를 추출한다. |
| `CNNScreenLab/PixelBoardController.cs` | `Low resolution image` 오브젝트에 부착한다. 같은 오브젝트의 Renderer를 Inspector로 받아 사진 텍스처 연결·표시·숨김과 인스턴스 재질 해제를 담당한다. |
| `CNNScreenLab/PixelSelection/PixelSelectionController.cs` | 활성 상태를 유지하는 Pixel Selection 부모에 부착한다. 필터는 그 자식이며, 선택 테두리는 보드 자식으로 유지한다. 보드 좌표계·크기·필터·선택 테두리·SelectedPixelsController를 Inspector로 받는다. XR 잡기 이벤트, 매 프레임 선택 판정, 필터 표시·정렬, 하위 SelectedPixelsController에 선택 좌표·표시 여부를 요청한다. |
| `CNNScreenLab/PixelSelection/SelectedPixelsController.cs` | FilterDisplay 프리팹의 Selected pixels 오브젝트에 부착해 사전 배치된 픽셀 블록 9개의 색상·표시를 갱신한다. SetVisible은 자신이 붙은 오브젝트를 켜거나 끄며 프레임에는 영향을 주지 않는다. |
| `CNNScreenLab/RGBObservation/PixelRevealStage.cs` | 사전 배치된 32열·576블록의 색상·투명도·열 위치를 갱신한다. RGB 공간 전개와 숫자 표시를 내부에서 조율하며 단계·완료를 로컬 이벤트로 알린다. |
| `CNNScreenLab/RGBObservation/RgbPixelStackController.cs` | 사전 배치된 RGB 3층·27블록의 색상·위치·크기를 갱신한다. UI 컴포넌트를 참조하지 않는다. |
| `CNNScreenLab/RGBObservation/RgbValuesUIController.cs` | RGB stack에 부착해 하위 숫자 Canvas의 카메라를 초기화하고 사전 배치된 숫자 27개·제목 3개의 UI를 소유한다. 숫자·글자색·층별 투명도를 갱신한다. |
| `CNNScreenLab/PixelLabUIController.cs` | Spatial controls에 부착해 패널 Canvas의 카메라를 초기화하고 자세히 보기·다음으로·처음으로와 안내를 갱신한다. RGB 숫자 Canvas 초기화는 RgbValuesUIController.Initialize에 위임한다. |
| `CNNConvolutionLab/CNNConvolutionLabManager.cs` | SetInput으로 계산 데이터를 보관하고 Start에서 학습 화면과 계산을 시작한다. ReturnRequested·Completed를 상위 Manager에 알린다. |
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

CNNScreenLabManager는 자세히 보기 시작 시 선택 좌표에서 PixelPatchData를 한 번 추출하고 해당 좌표와 함께 보관한다. PixelRevealStage.RevealPixels에 이 스냅샷을 전달하며, 관찰 단계는 픽셀을 다시 추출하지 않고 RGB 블록과 숫자 표시에 사용한다. 다음으로 진행할 때도 같은 PixelPatchData와 보관한 좌표를 CNNPixelLabManager에 전달한다. 처음으로 또는 선택 변경 시 Screen Manager가 보관한 스냅샷 참조를 해제한다. RGB 값 27개는 Color32 9개에 포함되므로 별도 배열로 중복 저장하지 않는다.

## 초기화와 관찰 흐름

공장은 비활성 관리용 루트를 로드·배치한 후 활성화한다. CNNPixelLabManager.Start가 관찰 씬을 로드한다. CNNScreenLab 스크립트는 실행 단계에 따라 PixelSelection(3×3 선택) → RGBObservation(픽셀·RGB 관찰)으로 묶는다. CNNScreenLabManager·PixelLabUIController·PixelImageData·PixelObservationPhase와 여러 단계에서 사진 표시를 제어하는 PixelBoardController는 CNNScreenLab 루트에 둔다. 이미지 생성 진입점인 PixelImageFactory도 루트에 둔다. RGB 숫자 표시는 RGB 관찰 전용이므로 RgbValuesUIController를 RGBObservation에 둔다. CNNScreenLabManager의 Inspector에는 PixelImageFactory·PixelBoardController·PixelSelectionController·PixelRevealStage·PixelLabUIController 다섯 진입점을 연결한다. 활성화된 Manager의 Start에서 이미지 데이터를 생성하고 보드·선택·UI를 초기화한다. SetInitialSelection으로 받은 복귀 좌표가 있으면 초기화 마지막에 선택을 복원한다. ScreenLab 단독 실행에서도 같은 Start가 이미지를 준비한다. Renderer·Transform·XRGrabInteractable·Material의 구체적인 조작은 Controller에 두며, Manager는 순서와 이벤트를 연결한다. 이미지 결과는 Manager가 소유하고 파괴 시 해제하며, 보드 사진용 인스턴스 재질은 PixelBoardController가 해제한다. 픽셀 블록·프레임의 공유 메시·재질은 에셋이며 런타임에 생성·해제하지 않는다. PixelSelectionController는 OnEnable/OnDisable에서 XR 이벤트를 연결·해제하고 LateUpdate에서 관찰 선택을 갱신한다. RGB 전개 시 선택 갱신을 중단하고 초기화 시 재개한다. PixelLabUIController도 MonoBehaviour로서 버튼·안내 Text를 Inspector로 받고 Canvas 카메라와 입력 모듈을 준비한다. 버튼은 UI Controller의 RequestDetails·RequestNext·RequestReset에 연결되며, 로컬 이벤트를 받은 Screen Manager가 이미지 동작과 UI 상태를 조율한다. 구독·입력 활성화와 해제는 Screen Manager의 OnEnable/OnDisable에서 대칭으로 수행한다.

원본은 `Textures/CNNPixelLab/OpeningBroadcast.png`다. 사진 표시·필터·RGB·계산에 사용하는 색상은 같은 32×18 샘플에서 나온다. 사진은 Bilinear, 렌즈 텍스처는 Point 필터링을 사용한다.

1. 필터를 Grip으로 잡아 이미지 위에 놓으면 PixelSelectionController가 거리·방향을 판정하고 3×3 선택을 확정한다.
2. 자세히 보기는 CNNScreenLabManager.ShowDetails에서 관찰 전개를 시작한다. 필터를 숨기고 PixelRevealStage.ShowImage와 RevealPixels를 연속 호출한다. 이미지 선택을 한 번 더 기다리는 별도 XR 클릭 대상은 만들지 않는다.
3. 사진이 컬러 입체 픽셀로 바뀌고 선택 영역의 R/G/B가 깊이 방향으로 펼쳐진다. 사진 Renderer는 숨긴다. 깊이는 실제 디스플레이 구조나 색 세기를 뜻하지 않는다.
4. 완료 이벤트로 다음으로 버튼을 활성화한다. 다음으로는 PixelPatchData와 선택 좌표를 ConvolutionRequested로 알린다.
5. 상위 Manager가 데이터를 보관하고 합성곱 씬 교체를 요청한다. 활성화 전에 CNNConvolutionLabManager.SetInput에 픽셀 복사본을 전달한다. 이후 Start가 해당 데이터로 계산 체험을 시작한다.

필터의 복귀 위치·회전은 보드 기준 로컬 좌표다. 이미지 크기는 4.8×2.7m, 필터 시작 위치는 콘텐츠 로컬 (-2.4, 1.15, -0.6)이다. 처음으로는 선택·전개를 초기화하고 상위 Manager에 완료 초기화를 알린다.

## 같은 방에서의 씬 교체와 종료

FactorySceneLoadController는 관리용 콘텐츠에 기존 ContentAnchor.position - Vector3.forward * 1.5f 오프셋을 한 번 적용한다. PixelLabSceneLoadController는 두 체험의 콘텐츠 루트를 관리용 루트의 월드 위치·회전에 배치하고 Rigidbody를 동기화한다. 두 체험 루트는 원점·회전 identity·스케일 1로 작성되어 있다. 계산 Canvas의 상대 위치·회전·배율은 기존 배치를 유지한다.

씬 파일의 Screen/Convolution 루트는 에디터에서 보이도록 활성 상태로 저장한다. 루트 이름은 각각 CNN Screen Lab, CNN Convolution Lab이다. 두 Manager 컴포넌트도 활성 상태로 저장한다. PixelLabSceneLoadController는 로딩 대상의 sceneLoaded 콜백에서 Start 전에 루트를 숨긴 다음 위치·회전과 Rigidbody를 맞춘다. CNNPixelLabManager는 준비된 씬 Manager에 이벤트를 연결하고 SetInput 또는 SetInitialSelection으로 시작 데이터를 전달한다. 로더의 CommitStage는 이전 씬을 정리하고 새 콘텐츠를 활성화하며, 각 Manager의 Start가 초기화를 수행한다. 로딩 이벤트 구독은 해당 씬 로드 완료·로딩 시작 실패·로더 파괴 시 해제한다. 준비 성공 시 이전 체험에서 잡은 물체의 XR 선택을 해제하고 콘텐츠를 숨겨 언로드한 뒤 새 체험을 활성화한다. 준비 중에는 잠깐 두 체험 씬이 함께 존재하며 교체 완료 후에는 하나만 남는다. 공장 활성 씬과 공용 XR 리그·태블릿을 유지하고 플레이어 위치·시점을 이동시키지 않는다. 로드 실패는 로그로 알리며 기존 체험에서 요청을 다시 할 수 있다.

다음 방 진입 시 FactoryLearningFlowManager는 CNNPixelLabManager.EndSession을 기다린다. Manager는 새 요청을 차단하고 진행 중인 로딩·교체가 끝나기를 기다린 뒤 구독과 내부 체험 씬을 정리한다. 이후 FactorySceneLoadController가 관리용 씬을 언로드한다. 관리용 씬이 외부에서 제거되면 내부 로더의 OnDestroy가 남은 씬을 정리하며, 로딩 중이었다면 완료 콜백에서 정리한다.

## 합성곱과 완료

CNNConvolutionLabManager는 Awake에서 PixelLessonManager를 생성하고 OnEnable/OnDisable에서 이벤트를 연결·해제한다. Start에서 학습 화면을 구성하고 Canvas 카메라를 연결한 뒤 입력으로 체험을 시작한다. Start 이전 준비 과정에서 비활성화될 때는 아직 생성되지 않은 학습 화면을 닫지 않는다. 입력을 전달받지 않은 단독 실행은 RGB가 모두 0인 픽셀 9개를 기본값으로 사용한다. PixelLessonManager가 입력을 int[3,9]로 변환하고 바로 계산을 시작한다. ConvolutionExercise가 R 설명, G/B 퀴즈와 RGB 합산을 진행한다. 완료는 CNNConvolutionLabManager.Completed → CNNPixelLabManager로 전달된다. 결과 화면의 계산 다시 보기는 같은 데이터로 계산을 반복한다. 다른 영역 선택은 상위 Manager에 복귀를 요청하고, 관찰 씬을 다시 로드한 뒤 저장한 좌표를 복원한다. 관찰 전개 자체는 다시 수행한다.

## 사전 배치된 표시와 UI 경계

`Prefabs/CNNScreenLab/FilterDisplay.prefab`은 프레임 4조각과 픽셀 9개, `ImagePixelGrid.prefab`은 픽셀 576개와 32열 Transform, `RgbPixelStack.prefab`은 RGB 블록 27개와 숫자 27개·제목 3개를 보관한다. CNNScreenLab 씬에 이 프리팹의 실제 인스턴스를 배치했다. 필터는 기존 XR 잡기 오브젝트 아래에, 이미지 격자·RGB 표시는 기존 보드 아래에 연결한다. 기존 필터의 얇은 프레임·평면 렌즈 Renderer는 비활성화해 새 입체 표시와 중복되지 않게 했다.

각 프리팹은 `Meshes/CNNScreenLab/PixelBlock.asset` 메시를 참조한다. 픽셀은 `Materials/CNNScreenLab/PixelBlock.mat`, 프레임은 `FilterFrame.mat`을 사용한다. 픽셀 색상·투명도는 MaterialPropertyBlock으로 변경한다. 메시·재질 에셋에 런타임 변경을 저장하지 않는다. 보드와 렌즈에 쓰던 격자 순서는 유지하며 전체 이미지에는 숫자 Text를 새로 추가하지 않았다.

`CNNScreenLab/PixelObservationPhase.cs`는 관찰 단계와 공통 UI가 공유하는 데이터 계약이다. 공통 UI는 관찰 단계 enum을 사용하고 PixelRevealStage 타입에 의존하지 않는다. 폴더는 실행 단계로 구분하며, 각 단계 안에서도 공간 표시와 UI 표시의 책임은 구분한다. 관찰 Controller에는 Text·Canvas·CanvasGroup 생성 및 제어 코드가 없다. 일반 Transform과 열별 진행값으로 이미지 블록을 전개한다. PixelRevealStage는 RgbPixelStackController와 RgbValuesUIController를 Inspector로 참조한다. ShowImage에서 숫자 UI를 초기화하고, RGB 전개 시작 시 값을 설정하며, RGB 블록의 ProgressChanged 이벤트를 받아 숫자 투명도에 반영한다. ResetStage에서 블록과 숫자를 함께 숨긴다. 진행도 이벤트는 OnEnable/OnDisable에서 대칭으로 연결·해제한다. CNNScreenLabManager는 PhaseChanged·PixelsRevealed로 공통 안내와 버튼만 갱신하며 RGB 데이터·진행도를 전달하지 않는다. RGB 숫자 Canvas는 해당 층의 자식으로 미리 배치되어 RgbPixelStackController가 층을 움직일 때 함께 이동한다. 숫자 표시를 위한 별도 애니메이션 타이머는 없다.

고정 안내 Text와 버튼의 레이아웃·스타일·피드백 컴포넌트도 씬에 저장한다. 런타임에 유지되는 생성 작업은 원본 이미지 가공 텍스처와 색상 갱신용 MaterialPropertyBlock 등 데이터 자원이다. 공용 입력 모듈의 선택적 EventSystem 보완 동작은 기존 입력 보조가 담당한다.

## 에디터 도구와 확인 범위

PixelLabBuilder는 관리용 씬, SpatialPixelLabBuilder는 관찰 씬, ConvolutionLabBuilder는 합성곱 씬을 구성한다. PixelRevealStageBuilder는 관찰 씬의 버튼을 연결하고 ScreenLabVisualsBuilder.Install을 호출한다. ScreenLabVisualsBuilder는 공유 메시·재질·프리팹이 없을 때 에디터에서 생성·저장하고 기존 프리팹과 인스턴스는 재사용한다. 전개 컴포넌트·RGB UI·안내 Text의 참조도 이 도구에서 연결한다. PixelBlockMeshBuilder는 기존 메시 생성 알고리즘을 에디터 전용으로 옮긴 도구다. 씬 파일은 모두 `Assets/Scenes/`에 둔다.

이번 변경은 소스·Unity YAML 참조·GUID·배치 값의 정적 확인만 수행했다. Unity 실행·컴파일·테스트·빌드는 수행하지 않았다. 아래 기록은 분리 전 구현에 대한 과거 확인이며 현재 실행 검증을 대신하지 않는다.

## 필터·픽셀 입체 표시 (2026-10-26)

`SelectedPixelsController`는 기존 필터 잡기 입력과 원본 샘플 좌표를 유지하면서 얇은 프레임·평면 렌즈 표시를 모서리가 다듬어진 두꺼운 프레임과 3×3 픽셀 블록으로 교체한다. `PixelVolumeMesh`의 공유 메시와 재질을 사용하고 선택 색상은 MaterialPropertyBlock으로 갱신한다. 밝기나 가중치에 따라 두께를 바꾸지 않는다. 투명도는 0.94이며 발광·추가 장식 조명은 사용하지 않는다.

`PixelRevealStage`도 평면 Image 대신 동일한 메시의 픽셀 블록을 표시한다. 열별 전환 시 블록과 앞면 숫자가 이미지 앞쪽으로 함께 나온다. 색 선택·숫자·합성곱 데이터는 기존 값 그대로이며, 숫자는 앞면에 남고 옆면으로 깊이를 확인할 수 있다. 생성 메시·재질은 소유 Controller/Stage의 종료 시 해제한다. 이번 변경은 소스 검토까지 수행했으며 컴파일·Play·APK 확인 전이다.

## 2026-10-26 Unity 실행 확인

Unity 6000.3.24f1 Play에서 CNNPixelLab을 실행하고 실제 Camera.Render 결과로 정면 필터와 측면 픽셀 화면을 확인했다. 초기 MaterialPropertyBlock 필드 생성으로 발생한 Unity 생성자 예외를 BuildPixels 초기화로 옮겨 해결했다. 숫자 Canvas의 정렬 순서를 높이고 전환 완료 후 원본 사진 Renderer를 숨겨 반투명 블록 뒤 사진의 색 혼합과 숫자 가림을 수정했다. ResetStage에서는 사진을 다시 표시한다.

최종 컴파일 완료·실패 없음, RevealPixels의 Revealing→Revealed 전환, G/B 버튼 onClick 호출 후 각각 576개 숫자의 원본 성분값 일치, NextStage 호출 후 합성곱 화면 활성화를 확인했다. 이는 에디터 실행·직접 콜백 검증이며 Quest 컨트롤러 레이 클릭이나 공장 전체 동선의 실기기 검증은 아니다. 자동화 연결의 Play/domain reload 중 응답 시간 초과 로그와 수정 전 예외 이력은 최종 콘텐츠 오류와 구분했다. 사용자의 후속 요청에 따라 APK 재빌드·기기 설치는 수행하지 않았다. 새 테스트 파일은 생성하지 않았다.


## 2026-10-29 입체 관찰 동선 변경

새 RGB 층 표시는 `Scripts/CNNPixelLab/CNNScreenLab/RGBObservation/RgbPixelStackController.cs`가 담당한다. 공용 XR 입력·공장 이동·다음 방 완료 조건은 유지한다. 기존 별도 RGB 관찰 화면은 이번 진입 흐름에서 호출하지 않으며 합성곱 설명·퀴즈는 기존 계산 화면을 사용한다. Unity 6000.3.24f1 컴파일 성공을 확인했다. Play에서 선택 상태를 설정하여 Revealing→Revealed 완료, RGB 세 층의 서로 다른 깊이, 다음으로 합성곱 화면 활성화, 처음으로 초기화를 확인했다. MaterialPropertyBlock 생성자 초기화 오류를 Initialize로 옮겨 수정했다. 실제 카메라 렌더링으로 입체 블록과 숫자 표시도 확인했으나, 근접 정면에서는 뒤 층 숫자가 겹쳐 보이는 한계가 있다. 검증은 에디터 콜백 기반이며 Quest 직접 잡기·공장 전체 동선 검증이나 APK 빌드는 수행하지 않았다.

## 2026-10-29 역할 분리 브랜치 병합

입체 관찰 동선을 PixelImageManager의 전환·선택 관리에 반영하고, PixelRevealStage와 RgbPixelStackController는 읽기 전용 픽셀 목록을 전달받도록 연결했다. PixelLessonManager의 계산·완료 이벤트와 공용 태블릿의 역할 분리는 유지한다. 위 Unity 실행 기록은 원본 브랜치에서 수행한 결과이며, 이번 병합 결과에는 Unity 실행·컴파일·빌드·테스트를 수행하지 않았다.

합성곱 씬의 루트 활성화는 기존에 저장된 오브젝트의 에디터 표시를 위한 설정이다. 계산 UI 자체는 CNNConvolutionLabManager.Start에서 초기화해 런타임에 생성하므로 에디터의 정적인 화면에 전체 계산 UI가 표시되지는 않는다.
