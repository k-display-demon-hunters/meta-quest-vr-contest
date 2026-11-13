# CNNRealityLab 숫자 인식 체험

## 이용 흐름

모델방에서 학습을 끝낸 뒤 제어 패널의 **현실 숫자 인식**을 누른다. 학습이나 추론이 진행 중이거나 블록을 잡고 있으면 진입 버튼을 사용할 수 없다. AR은 모델방에 연결된 추가 씬으로 실행되며 모델방의 학습 모델과 XR 리그를 사용한다.

1. 카메라 권한을 허용하고 숫자 0~3 중 한 자리를 선택 프레임에 맞춘다. 촬영 대기 중 프레임은 카메라 사진 중앙보다 위쪽의 정사각형 촬영 영역을 표시한다. 카메라 기준 1.2m 평면에 배치하며, 권한 대기 중에는 시야 정면에 표시한다.
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
| `Capture/RealityCaptureManager.cs` | 사진 기준 촬영 영역의 안내 데이터 제공, 촬영과 전처리 조율 |
| `Capture/RealityCameraController.cs` | MRUK 카메라 준비 상태, XR 시점·렌즈 오프셋 기반 촬영 프레임 투영, 영상 읽기와 임시 자원 정리 |
| `Capture/RealityImagePreprocessor.cs` | 선택 영역의 28×28 RGB 변환 |
| `Capture/RealityRecognitionData.cs` | 요청 ID와 읽기 전용 입력 픽셀 보관 |
| `UI/RealityUIManager.cs` | 단계별 버튼·안내 표시와 사용자 요청 이벤트 |
| `UI/RealityViewController.cs` | 촬영 프레임 추적·고정과 결과·안내 배치 |
| `UI/RealityResultController.cs` | 현재 입력 텍스처 관리, 예측 숫자와 확률 표시 |

모델방 Manager는 제어 패널의 진입 요청을 받아 전환 Controller를 호출한다. 전환 Controller는 기존 XR 리그의 스틱 이동을 멈추고 모델방 및 로드된 공장 씬의 시각 요소와 Collider를 비활성화한다. AR 씬을 닫으면 저장한 설정을 복원한다. 학습 엔진은 모델방에서 계속 실행되며 AR Manager는 전달받은 추론·복귀 함수와 촬영·UI 진입점을 사용한다.

## 카메라와 모델 입력

카메라 사진의 짧은 변 40%를 한 변으로 하는 정사각형을 촬영 영역으로 사용한다. 영역 중심은 정규화 영상 좌표 (0.5, 0.6)으로, 영상 중앙에서 높이의 10%만큼 위로 올린다. 프레임 크기와 UI 전체 높이·글자·버튼 크기는 유지하며 하단 조작부가 시야 중앙에 더 가까워지도록 한다. 편집 화면과 카메라 준비 전의 기본 프레임도 기존 위치보다 0.12m 위에 둔다. 실제 읽은 이미지 해상도로 영역을 구하고 bilinear 방식으로 28×28 RGB로 축소한다. UI의 월드 좌표로 촬영 영역을 역산하거나 범위 초과로 촬영을 거부하지 않는다. 카메라 전체 프레임은 RGB24로 한 번 읽고 해제한다.

진입 Controller가 기존 XR Camera를 씬 Manager에 전달하고, 씬 Manager가 Capture를 초기화한다. Capture는 현재 XR Camera의 월드 위치·회전과 MRUK `Intrinsics.LensOffset`을 결합해 촬영 카메라의 월드 Pose를 만든다. 프로젝트의 `XROrigin` 이동과 `Camera Offset` 회전을 반영하며, `OVRCameraRig`를 전제로 하는 SDK 기본 Pose에 의존하지 않는다. 이 Pose를 `ViewportPointToRay`에 명시적으로 전달해 같은 촬영 영역을 카메라 정면 1.2m 평면에 투영한다.

촬영 대기 중 씬 Manager는 먼저 UI의 안내 Canvas를 사용자 정면에 배치하고, 카메라가 준비되면 Capture의 프레임 위치·크기를 UI에 전달한다. UI는 480×480 선택 프레임만 위치·회전·배율을 맞춘다. 씬의 모서리 표시 8개와 촬영 입력 미리보기는 선택 프레임의 자식이며, 버튼·안내는 별도로 기존 Canvas 배율과 시야 정면 배치를 유지한다. 촬영 후에는 프레임과 안내 Canvas를 고정한다. 표시에는 현재 XR 시점, 영상에는 카메라 프레임의 촬영 시점이 사용되므로 움직이는 동안의 지연과 물체 깊이에 따른 정합은 Quest에서 확인해야 한다.

제목은 `Capture Header`, 안내 문구와 버튼은 `Capture Instructions and Actions`에 묶는다. View Controller는 사용자 시점에서 프레임 네 모서리를 안내 Canvas 평면에 투영하고, 화면상 위·아래 경계에서 각각 32 UI 단위의 간격을 두어 두 그룹을 배치한다. 프레임 크기·기울기가 달라져도 고정된 제목·버튼 위치가 촬영 영역을 침범하지 않도록 한다. 하단은 안내 문구, 촬영 또는 인식·재촬영 버튼, 돌아가기·사용 안내 버튼의 순서이며 문구 배경과 버튼 줄 사이 및 두 버튼 줄 사이에 각각 16 UI 단위 간격을 둔다. 제목·문구·버튼의 크기는 프레임 배율에 따라 확대하지 않는다.

입력은 28×28×3, RGB 0~1, 위쪽 행부터 HWC 순서다. 추론 요청에는 `reality:` 접두사를 사용하며 모델방의 일반 예측 목록은 이 결과를 건너뛴다. AR 씬이 종료되면 늦게 완료된 결과는 UI에 반영하지 않는다.

패스스루는 Meta Core 및 MRUK의 카메라 API와 `horizonos.permission.HEADSET_CAMERA` 권한을 사용한다. 실제 카메라 정합과 모델방에서 AR로 진입하고 복귀하는 동작은 Quest에서 확인해야 한다.
