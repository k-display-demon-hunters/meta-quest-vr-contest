# CNNRealityLab 패스스루

독립 씬 `client/Assets/Scenes/CNNRealityLab.unity`에 기존 XRPlayerRig 프리팹 인스턴스와 OVRManager·OVRPassthroughLayer를 배치한다. 태블릿, 컨트롤러, 레이, 입력은 원본 프리팹을 그대로 사용한다. 공장과 ModelDesignLab은 로드하지 않는다. 기존 빌드 씬 목록과 시작 씬은 변경하지 않는다.

RealityPassthroughController는 연결된 카메라의 배경을 투명하게 만들고 HDR·후처리를 중지한다. 종료 시 이전 상태를 복원한다. 패스스루는 Underlay로 배치한다. 태블릿과 컨트롤러의 표시·입력은 비활성화하지 않는다. MRUK 카메라 접근, 촬영, 추론, 방 스캔, 씬 전환은 구현하지 않았다.

Meta Core와 MRUK 207.0.1을 공식 scoped registry로 선언했다. 실제 패키지 다운로드·설치·Unity 임포트는 하지 않았고 packages-lock.json은 이전 해석 결과로 유지했다. 이후 허가된 Package Manager 실행에서 잠금 파일 갱신이 필요하다.

Android 빌드 후처리 소스는 생성된 unityLibrary manifest에 optional com.oculus.feature.PASSTHROUGH를 추가한다. 카메라 픽셀 접근 권한은 추가하지 않는다. 이 후처리는 작성만 했으며 실행하지 않았다.

## UI 구성

씬의 `CNNReality UI` 아래에 선택 안내, 인식 패널, 사용 안내 팝업을 저장했다. 런타임 UI 생성 코드나 새 Manager는 추가하지 않았다. 태블릿·컨트롤러·XRPlayerRig 및 공용 UI 프리팹 원본은 변경하지 않았다.

| 오브젝트 | 현재 표시와 동작 |
| --- | --- |
| Number Selection Guide | 네 모서리로 된 빈 숫자 선택 프레임, 숫자 하나를 맞추라는 안내, 현재 촬영하지 않는다는 문구. 프레임 내부에는 배경이나 raycast 대상이 없다. |
| Recognition Panel | 촬영 카메라·사용 모델의 미연결 상태, 선택한 원본과 28×28 CNN 입력의 빈 미리보기, 예측 대기 표시, 0~9의 빈 점수 막대 자리와 `—` 표시. 실제 이미지·추론 결과·점수 데이터는 없다. |
| Capture / Recognize / Retake Button | 공용 WorldUIButton 프리팹 인스턴스. 촬영·인식하기·다시 촬영은 `Button.interactable = false`로 저장하고 클릭 동작을 연결하지 않았다. 아래에 사용 불가 이유를 표시한다. |
| Help Button / Reality Usage Guide | 공용 WorldUIButton과 WorldUIPopup 인스턴스. 사용 안내 버튼이 팝업 진입점 Show를 호출하고 팝업의 닫기 버튼이 기존 동작으로 닫는다. 팝업은 처음에는 비활성이다. 기능 연결 전인 현재 상태를 안내문에 명시한다. |

선택 안내 Canvas는 월드 좌표 `(0, 1.5, 1.5)`, 인식 패널은 `(0.72, 1.4, 1.5)`에 배치하고 UI 배율은 0.001로 저장했다. 선택 프레임은 약 0.48m 정사각형, 패널은 0.82×1.08m다. 머리를 따라다니거나 현실 표면에 고정되는 기능은 없고 씬에 배치한 위치를 유지한다. 팝업은 `(0, 1.5, 1.25)`에 표시된다.

입력은 기존 리그의 EventSystem·XRUIInputModule·컨트롤러 ray를 사용한다. 새 입력 액션이나 태블릿 단축키는 추가하지 않았다. 패널 내부 버튼은 공용 프리팹의 Canvas·raycaster를 인스턴스에서 비활성화하고 패널 Canvas·TrackedDeviceGraphicRaycaster를 사용한다. 선택 프레임과 문구는 raycast를 받지 않는다.

UI 문구는 기존 Pretendard Regular·SemiBold 원본을 참조하며 폰트의 추가 사용처를 ASSET_CREDITS.md에 기록했다. 새 이미지·아이콘·미디어 파일은 추가하지 않았다. 28×28 및 0~9 표시는 현재 UI 배치용이며 모델 입력 규격이나 추론 API를 변경하지 않는다.

## 확인 범위

씬 YAML과 기존 프리팹 참조를 정적으로 확인했다. UI의 실제 표시·한글 렌더링·컨트롤러 ray 반응 및 팝업 조작은 실행 확인 전이다. SDK 참조 GUID는 Meta 공식 Unity-PassthroughCameraApiSamples의 CameraViewer와 Unity-StarterSamples의 PassthroughAtStartup 씬에서 확인했다. 207.0.1 패키지의 실제 임포트 결과·OpenXR 연동·기기 동작은 아직 검증하지 않았다. Unity 실행·컴파일·빌드·테스트·Simulator 설치를 수행하지 않았다. 헤드셋 없는 일반 에디터 화면에서 실제 현실 영상이 나타나는 구현은 아니다.

공용 리그가 DontDestroyOnLoad로 유지되므로 이 씬은 단독 시작을 대상으로 한다. 다른 씬에서 Additive로 들어오며 유지 중인 리그에 연결하는 기능은 제공하지 않는다.
