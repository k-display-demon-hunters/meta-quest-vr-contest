# CNNRealityLab 숫자 인식 체험

## 이용 흐름

모델방에서 학습을 끝낸 뒤 제어 패널의 **현실 숫자 인식**을 누른다. 학습이나 추론이 진행 중이거나 블록을 잡고 있으면 진입 버튼을 사용할 수 없다. AR은 모델방에 연결된 추가 씬으로 실행되며 모델방의 학습 모델과 XR 리그를 사용한다.

1. 카메라 권한을 허용하고 숫자 0~3 중 한 자리를 선택 프레임에 맞춘다. 촬영 대기 중 프레임은 시야 정면 1.2m를 따라간다.
2. **촬영하기**를 누르면 프레임이 고정되고 모델에 전달할 28×28 RGB 입력이 표시된다.
3. **인식하기**를 누르면 기존 학습 모델이 입력을 예측한다. 인식 중에는 중복 입력과 화면 전환을 막는다.
4. 결과 패널은 시야 정면에 배치된 뒤 고정된다. 현재 입력, 예측 숫자와 숫자별 Softmax 출력을 백분율로 표시한다.
5. **다시 촬영**을 누르면 현재 입력을 비우고 촬영 대기로 돌아간다. **모델방으로 돌아가기**를 누르면 AR 씬을 닫고 모델 제작 화면으로 복귀한다. 촬영·추론 실패 시 화면에 이유를 표시한다.

촬영 화면에서 **사용 안내**를 누르면 안내 팝업이 시야 정면에 열린다. 캡처 입력과 결과는 현재 촬영을 표시하고 인식하는 동안 사용한다.

## 구조와 책임

Unity에서 씬을 열면 Scene 뷰에서 촬영 화면과 결과 패널의 배치를 볼 수 있다. 모델방에서 AR에 진입하면 `CNNRealityLabManager`가 초기화하면서 촬영 화면을 켜고 결과 패널을 숨긴다.

씬은 `client/Assets/Scenes/CNNRealityLab.unity`에 있다. 모델 제작의 하위 체험에 해당하는 AR 런타임 코드는 `client/Assets/Scripts/CNNModelDesignLab/Reality/` 아래에 두며 `RealityLab` 네임스페이스를 사용한다.

| 경로 | 책임 |
| --- | --- |
| `ModelRealitySessionController.cs` | AR 씬 추가 로드·해제, 기존 카메라 연결, 기존 씬의 표시와 이동 설정 복원 |
| `CNNRealityLabManager.cs` | 촬영 대기·입력 확인·인식 중·결과 단계 관리와 촬영·추론·재촬영·복귀 요청 조율 |
| `RealityPassthroughController.cs` | 카메라 권한 요청, 패스스루 활성화, 투명 배경·HDR·후처리 설정과 복원 |
| `RealityPresentationController.cs` | 모델방과 로드된 공장 씬의 Renderer·Canvas·Light·Collider 상태 저장 및 복원 |
| `Capture/RealityCaptureManager.cs` | 선택 프레임 투영, 촬영과 전처리 조율 |
| `Capture/RealityCameraController.cs` | MRUK 카메라 준비 상태, 좌표 변환, 영상 읽기와 임시 자원 정리 |
| `Capture/RealityImagePreprocessor.cs` | 선택 영역의 28×28 RGB 변환 |
| `Capture/RealityRecognitionData.cs` | 요청 ID와 읽기 전용 입력 픽셀 보관 |
| `UI/RealityUIManager.cs` | 단계별 버튼·안내 표시와 사용자 요청 이벤트 |
| `UI/RealityViewController.cs` | 촬영 프레임 추적·고정과 결과·안내 배치 |
| `UI/RealityResultController.cs` | 현재 입력 텍스처 관리, 예측 숫자와 확률 표시 |

모델방 Manager는 제어 패널의 진입 요청을 받아 전환 Controller를 호출한다. 전환 Controller는 기존 XR 리그의 스틱 이동을 멈추고 모델방 및 로드된 공장 씬의 시각 요소와 Collider를 비활성화한다. AR 씬을 닫으면 저장한 설정을 복원한다. 학습 엔진은 모델방에서 계속 실행되며 AR Manager는 전달받은 추론·복귀 함수와 촬영·UI 진입점을 사용한다.

## 카메라와 모델 입력

MRUK의 `PassthroughCameraAccess.WorldToViewportPoint`로 프레임 네 모서리를 실제 카메라 영상 좌표에 투영한다. 네 점의 축 정렬 사각형을 자르고 bilinear 방식으로 축소한다. 프레임을 정면에서 바라보도록 안내하며 선택 영역이 카메라 범위를 벗어나면 다시 맞출 수 있다. 카메라 전체 프레임은 RGB24로 한 번 읽고 해제한다.

입력은 28×28×3, RGB 0~1, 위쪽 행부터 HWC 순서다. 추론 요청에는 `reality:` 접두사를 사용하며 모델방의 일반 예측 목록은 이 결과를 건너뛴다. AR 씬이 종료되면 늦게 완료된 결과는 UI에 반영하지 않는다.

패스스루는 Meta Core 및 MRUK의 카메라 API와 `horizonos.permission.HEADSET_CAMERA` 권한을 사용한다. 실제 카메라 정합과 모델방에서 AR로 진입하고 복귀하는 동작은 Quest에서 확인해야 한다.
