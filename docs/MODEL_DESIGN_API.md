# 모델 제작 씬과 편집 API

대상 씬은 [CNNModelDesignLab.unity](../client/Assets/Scenes/CNNModelDesignLab.unity)다. 슬롯에 배치된 블록 순서로 모델을 구성하고 서버에 학습·추론을 요청한다. 조작은 [공통 조작법](xr-simulator.md), 전송 형식은 [모델 JSON 명세](MODEL_JSON_SPEC.md)와 [WebSocket 통신](WEBSOCKET_PROTOCOL.md)을 참고한다.

## 파일과 클래스

씬 스크립트는 `client/Assets/Scripts/CNNModelDesignLab/`, 네임스페이스는 `ModelDesignScene`이다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [CNNModelDesignLabManager](../client/Assets/Scripts/CNNModelDesignLab/CNNModelDesignLabManager.cs) | Workspace와 고정 Input·Output을 준비하고 공급·배치·설정·실행 Controller의 이벤트를 연결한다. 블록을 꺼내거나 슬롯에 놓을 때 Workspace의 레이어와 연결 순서를 갱신하며, 학습 요청 시 검증·직렬화 후 ExecutionController에 JSON을 전달한다. 응답을 결과 화면에 넘기고 현재 편집 모델이 학습 당시 모델과 달라졌는지도 비교한다. |
| [ModelDesignEntrance](../client/Assets/Scripts/CNNModelDesignLab/ModelDesignEntrance.cs) | 진입 표지의 XRI Activate를 받아 제작 씬을 Additive로 연다. 진입 뒤 표지를 비활성화해 같은 안내판을 다시 선택하지 않게 한다. 실제 모델 생성과 학습 준비는 로드된 씬의 Manager가 수행한다. |
| [ModelSupplyController](../client/Assets/Scripts/CNNModelDesignLab/ModelSupplyController.cs) | 공급소에 놓일 레이어 종류별 기본값으로 블록을 준비한다. 블록이 꺼내졌는지 판별하고 원래 자리에 새 블록을 보충한다. 꺼낸 블록의 Workspace 등록과 슬롯 배치는 Manager가 처리하므로 공급소는 계속 사용할 부품을 제공하는 역할이다. |
| [ModelBlockController](../client/Assets/Scripts/CNNModelDesignLab/ModelBlockController.cs) | 하나의 `LayerDefinition`을 3D 블록에 연결하고 레이어 변경을 라벨에 반영한다. XRI 잡기·놓기를 이벤트로 알려 Manager가 공급소 출고나 슬롯 이동을 처리하게 한다. 고정 Input·Output은 선택을 차단해 공급 블록과 구분한다. |
| [ModelConveyorController](../client/Assets/Scripts/CNNModelDesignLab/ModelConveyorController.cs) | 슬롯별 점유 블록을 보관하고 잡은 블록의 중심을 저장된 슬롯 앵커의 로컬 좌표로 바꿔 배치 가능한 슬롯을 찾는다. 이동 중 후보를 표시하고 유효한 빈 슬롯에 놓으면 앵커의 위치·방향을 맞춘다. 슬롯 순서의 블록 목록과 설정 조작기기 상태를 제공해 Manager가 모델 연결을 다시 만들게 한다. |
| [SlotSettingsController](../client/Assets/Scripts/CNNModelDesignLab/SlotSettingsController.cs) | 한 슬롯의 설정 조작기기에 붙어 현재 배치된 블록과 팝업 기준점을 보관한다. 슬롯의 사용 가능 상태에 맞춰 조작기기를 갱신하고 XRI Activate를 설정 요청으로 전달한다. Manager가 입력과 블록 상태를 확인한 뒤 실제 팝업을 열게 한다. |
| [ModelXRInputController](../client/Assets/Scripts/CNNModelDesignLab/ModelXRInputController.cs) | 공용 오른손 레이와 XR UI 입력 환경을 연결한다. 슬롯 설정 요청이 이 레이에서 왔는지, 물체를 잡고 있지 않은지, 가까운 UI에 가려지지 않고 해당 슬롯 Collider에 맞았는지 검사한다. 입력 종료 시 자신이 추가한 EventSystem 또는 UI 모듈을 정리한다. |
| [ModelSettingsController](../client/Assets/Scripts/CNNModelDesignLab/ModelSettingsController.cs) | 팝업 인스턴스 하나를 만들고 현재 설정 대상 블록을 보관한다. 슬롯 요청을 받으면 팝업을 해당 `Popup Anchor`의 자식으로 옮겨 대상 레이어를 표시하고, 삭제 버튼은 블록을 지정한 이벤트로 Manager에 전달한다. 닫기·비활성화·파괴 시 대상 참조와 팝업을 정리한다. |
| [BlockSettingsPopup](../client/Assets/Scripts/CNNModelDesignLab/BlockSettingsPopup.cs) | 선택한 레이어 종류에 필요한 숫자·활성화 항목을 구성한다. 설정 버튼은 `LayerDefinition.Set*()`을 호출해 값을 바꾸고 변경 이벤트가 Workspace 검증과 블록 라벨 갱신으로 이어지게 한다. Done은 팝업을 닫고 Delete는 상위 Controller에 삭제를 요청한다. |
| [ModelExecutionController](../client/Assets/Scripts/CNNModelDesignLab/ModelExecutionController.cs) | 직렬화된 모델 JSON을 받아 통신 세션을 만들고 연결 → 시작 확인 대기 → 학습 → 완료 상태를 관리한다. 중지 시점에 따라 연결을 닫거나 서버 취소 응답을 기다리며, 학습된 모델이 있으면 이미지 ID 추론 요청을 보낸다. 진행·결과·오류를 이벤트로 알리고 세션·작업 번호로 종료된 요청의 늦은 응답을 걸러낸다. |
| [ModelResultsController](../client/Assets/Scripts/CNNModelDesignLab/ModelResultsController.cs) | 프리팹에 저장된 구성 검사·학습 지표·숫자 예측 페이지와 각 Controller를 연결한다. 실행 상태·진행·완료·오류와 편집 모델 변경 안내를 표시하며, 화면의 시작·중지·추론 요청을 이벤트로 Manager에 전달한다. UI 계층과 배치를 런타임에 생성하지 않는다. |
| [ModelValidationController](../client/Assets/Scripts/CNNModelDesignLab/ModelValidationController.cs) | 전송 검증 결과를 한국어 제목·원인·수정 행동으로 표시한다. 7종 메시지와 일반 대체 안내를 제공하며, 실제 슬롯 위치·문제 개수·이전/다음 문제 탐색을 연결한다. 잡고 있는 동안에는 중립적인 배치 안내를 표시한다. |
| [ModelTrainingMetricsController](../client/Assets/Scripts/CNNModelDesignLab/ModelTrainingMetricsController.cs) | 저장된 학습 페이지의 MonoBehaviour다. 진행 응답의 epoch와 학습·검증 loss·accuracy를 숫자·진행 막대·그래프에 누적하고 비교 안내를 갱신한다. 완료 응답의 마지막 epoch와 별도 테스트 평가 지표를 표시하며, 새 학습에서는 이전 지표를 초기화한다. |
| [ModelInferenceResultsController](../client/Assets/Scripts/CNNModelDesignLab/ModelInferenceResultsController.cs) | 저장된 예측 페이지의 MonoBehaviour다. 기존 샘플 버튼·미리보기·확률 막대에 이미지와 정답을 연결하고, 선택한 이미지의 에셋 이름을 추론 ID로 요청한다. 응답 ID가 대기 중인 샘플과 일치할 때 예측 숫자·정오답·클래스별 출력을 표시하고 학습 상태에 따라 선택 가능 여부를 갱신한다. |
| [ModelMetricGraph](../client/Assets/Scripts/CNNModelDesignLab/ModelMetricGraph.cs) | epoch별 학습·검증 수치를 저장하고 `Graphic`의 메시로 두 데이터의 선 그래프를 그린다. MetricsController가 진행 응답을 추가하거나 초기화하면 그래프가 갱신된다. 서버 통신과 지표 계산은 수행하지 않고 전달된 수치를 시각화한다. |

Manager는 Controller의 인스턴스 이벤트를 구독하고 기능 사이의 순서를 조율한다. Controller는 Manager를 직접 참조하지 않는다. `ModelValidationController`, `ModelTrainingMetricsController`, `ModelInferenceResultsController`는 `ModelResults.prefab`의 각 페이지에 저장된 MonoBehaviour이며, `ModelResultsController`가 직렬화된 참조로 연결한다. 비활성 페이지의 지표·추론 Controller도 명시적으로 초기화하므로 첫 페이지 전환 전에 데이터를 받을 수 있다.

## 블록 배치에서 모델 갱신까지

1. Manager가 `Awake()`에서 Workspace를 생성한다. `Start()`에서 고정 Input·Output 블록, 공급소, 슬롯을 초기화하고 이벤트를 연결한다.
2. 공급소의 블록을 잡으면 `ModelSupplyController.TryTakeBlock()`이 기본 블록을 보충한다. Manager는 꺼낸 블록의 레이어를 Workspace에 추가한다.
3. 슬롯에 있던 블록을 잡으면 Manager가 팝업을 닫고 해당 슬롯을 비운 뒤 모델 순서를 갱신한다.
4. 잡고 있는 동안 `LateUpdate()`가 `PreviewPlacement()`를 호출한다. `ModelConveyorController`는 블록 중심을 각 `Block Anchor`의 로컬 좌표로 변환해 `Placement Half Extents` 안에 있는 가장 가까운 슬롯을 찾는다.
5. Grip을 놓으면 `TryPlaceBlock()`이 빈 슬롯에 블록을 등록하고 위치·방향을 정렬한다. 슬롯 밖·점유된 슬롯 등으로 장착하지 못했거나 활성 상태에서 잡기가 취소되면 기존 삭제 경로로 블록과 Workspace 레이어를 함께 제거한다. 공급소에 보충된 블록은 유지한다.
6. `RefreshModelOrder()`가 이전 연결을 해제하고 `Input → 슬롯 순서의 블록 → Output`으로 연결한다. 빈 슬롯은 건너뛰며 다른 블록의 실제 위치는 옮기지 않는다.

잡고 이동하는 동안에는 블록을 유지하며, 놓는 순간 장착 여부를 판정한다. 슬롯에 장착한 블록을 다시 꺼내 바깥에 놓아도 삭제한다. 배치는 레이어 순서 오류를 허용하며, 구성 검사 페이지에 문제를 표시하고 학습 시작 시 전송 검증으로 다시 걸러낸다.

## 컨베이어와 신경망 블록 표시

컨베이어와 블록의 외형은 `Assets/Meshes/CNNModelDesignLab/`의 Unity Mesh 에셋과 `Assets/Materials/CNNModelDesignLab/`의 재질을 참조하는 프리팹으로 저장한다. `CNNModelDesignLab.unity`를 열면 Play 없이 확인하고 배치·회전·재질을 수정할 수 있다. 초기화 때 메시나 재질을 생성하거나 저장된 외형·위치를 덮어쓰지 않는다.

`ModelConveyor.prefab`은 고무 벨트와 이음매, 금속 프레임·지지 다리, 양 끝 롤러, 후면 모터를 각각 편집할 수 있는 네 메시 자식으로 구성한다. 황색 레일·사선 표시는 설비 경계를, 벨트 뒤쪽 화살표는 Input에서 Output으로 이어지는 방향을 나타낸다. 지지 다리는 씬의 벨트 기준 높이 1.05m에 맞춰져 있다. 벨트는 고정된 조립 작업대이며 블록을 자동 운반하지 않는다.

슬롯은 벨트가 보이는 열린 사각 테두리 메시를 사용하며 빈 슬롯·점유·배치 후보·배치 불가 색상은 `RefreshSlot()`이 갱신한다. 각 `Slot` 아래의 `Block Anchor`가 실제 스냅 위치·방향이다. 슬롯과 앵커는 단위 스케일을 사용한다. `Block Anchors` 배열, `Slot Visuals`의 자식 순서, `Settings Controls`의 자식 순서를 동일한 슬롯 순서로 유지한다. 설정 조작기기 8개도 프리팹에 저장되어 있으며 시작 시 재배치하지 않는다. Input·Output 위치는 씬에서, 학습 시작·중지 버튼은 제작 콘솔 프리팹에서 편집한다.

블록은 `ModelBlock.prefab`(Conv2D), `MaxPooling2DBlock.prefab`, `FlattenBlock.prefab`, `DenseBlock.prefab`, `InputBlock.prefab`, `OutputBlock.prefab`으로 구분한다. 각 프리팹은 종류에 맞는 소형 신경망 메시와 네 재질을 참조한다. Feature Map Lab의 구형 뉴런 격자를 참고해 다음 실루엣으로 구분한다.

| 종류 | 모양 |
| --- | --- |
| Conv2D | 깊이와 좌우 간격을 둔 세 겹의 뉴런 격자 |
| MaxPooling2D | 큰 격자에서 작은 격자로 좁아지는 연결 |
| Flatten | 격자에서 한 줄 벡터로 펼쳐지는 연결 |
| Dense | 세 열의 뉴런과 열 사이의 연결선 |
| Input | 입력 이미지를 나타내는 뉴런 격자 |
| Output | 허브에서 열 개 클래스 노드로 나뉘는 연결 |

뉴런 개수는 레이어 종류를 알아보기 위한 예시이며 실제 필터·유닛 수나 계산 결과를 표시하지 않는다. 실제 설정값은 블록 아래 명판의 기존 라벨로 표시하고, 값 변경 시 라벨을 갱신한다. 블록 메시와 명판은 기존 0.42 × 0.24 × 0.26m 잡기 영역 안에 배치한다.

공급대는 `PartsSupplyRack.prefab`의 가로 작업대다. 씬의 `Parts Supply` 위치는 `(2.9, 1.212, 0.3)`, Y 회전은 90도이며, 네 공급 블록은 로컬 X `-0.975, -0.325, 0.325, 0.975`에 같은 높이로 놓인다. X 방향 컨베이어 오른쪽 끝에서 공급대가 Z 방향으로 꺾여 ㄱ자를 이루며, 명판은 안쪽 작업 공간을 향한다. `ModelSupplyController`의 각 `Palette Blocks` 항목은 씬 블록과 종류별 보충 프리팹을 참조한다. 블록을 꺼내면 해당 프리팹을 원래 로컬 위치·회전에 보충하므로 외형과 가로 배치가 유지된다.

메시는 프로젝트에서 만든 기본 도형과 연결선 조합을 에셋으로 저장한 것이며 외부 모델·텍스처는 사용하지 않는다. 장식용 외형에는 Collider를 추가하지 않고 기존 블록 잡기 영역과 설정 조작기기의 Collider를 유지한다.

## 설정과 삭제

`SlotSettingsController`가 설정 요청을 보내면 Manager가 현재 블록, 잡기 상태, `ModelXRInputController.CanActivateSlot()` 결과를 확인한다. `ModelSettingsController`는 해당 슬롯의 `Popup Anchor`에 팝업을 연다. 팝업은 한 번에 하나이며 사용자 시점을 따라가지 않는다.

`BlockSettingsPopup`은 레이어 종류에 맞는 숫자·활성화 항목을 만들고 `LayerDefinition.Set*()`을 호출한다. 값 변경은 Workspace의 검증·변경 이벤트로 이어진다. 삭제 요청은 Manager가 받아 슬롯 제거, Workspace 레이어 제거, 이벤트 해제, 블록 비활성화·파괴 순서로 처리한다. Input·Output은 삭제할 수 없다.

## 학습·추론 흐름과 수명

제작 콘솔의 `ModelResultsController`에서 시작 요청을 받은 Manager는 작업 중이거나 블록을 잡고 있으면 요청을 처리하지 않는다. `ModelJsonSerializer.TrySerialize()`가 실패하면 즉시 구성 검사 페이지의 원인·수정 안내를 보여 준다. 성공하면 결과 화면을 초기화하고 `ModelExecutionController.StartTrainingAsync()`에 JSON을 넘긴다. 학습 시작·중지는 제작 콘솔 버튼으로 조작하며, 컨베이어 왼쪽의 별도 학습 조작부와 Manager의 해당 참조는 제거한다.

`ModelExecutionController`는 연결을 만들고 `TrainAsync(modelJson, epoch: null)`을 호출한다. 서버 기본 epoch를 사용하며 `Connecting → StartingTraining → Training` 상태로 진행한다. 시작 확인 전의 중지는 연결 종료로 처리하고, 학습 중의 중지는 취소 요청 후 서버 응답을 기다린다. 학습·추론·취소 대기 중에는 추가 학습·추론을 받지 않는다.

진행·완료 응답은 Manager를 거쳐 결과 화면으로 전달된다. `ModelTrainingMetricsController`는 학습·검증 지표를 epoch별로 누적하고 마지막 epoch는 완료 응답에서 추가한다. 최종 테스트 지표는 별도로 표시한다. 총 epoch 표시도 실제 진행·완료 응답을 사용한다.

학습 완료 후 `ModelInferenceResultsController`는 선택한 샘플의 `Texture2D.name`을 `image_id`로 보낸다. 응답 ID가 선택한 샘플과 일치하는지 확인한 뒤 예측 숫자와 클래스별 출력을 표시한다. 씬의 고정 Output은 Softmax이며 수신값을 재정규화하지 않고 백분율로 표시한다.

모델은 학습 중·후에도 편집할 수 있다. Manager는 현재 JSON과 `SubmittedModelJson`을 비교해 결과가 이전 구성에 해당하는지 표시한다. 추론은 서버에 남아 있는 마지막 학습 모델을 사용한다.

Manager의 `OnDisable()`은 세션 종료, 결과 초기화, 팝업 닫기, 잡기 취소와 입력 해제를 수행한다. `ModelExecutionController`는 세션·작업 세대 번호를 비교해 종료된 요청의 늦은 응답을 무시한다. `OnDestroy()`에서는 이벤트 구독과 Workspace를 해제한다. 서버 모델은 연결과 함께 사라지므로 다시 진입하면 학습이 필요하다.

## 저장된 제작 콘솔 UI와 오류 안내

[ModelResults.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelResults.prefab)은 1440 × 1120 기준의 월드 공간 Canvas다. 구성 검사·학습 지표·숫자 예측의 세 페이지, 탭, 학습 시작·중지, 상태 표시, 모델 변경 안내와 요청 오류 패널을 실제 uGUI 계층으로 저장한다. 씬이나 프리팹을 열어 위치·크기·텍스트·폰트·색상을 편집할 수 있으며, 실행 때 UI 계층이나 배치를 다시 만들지 않는다. Manager의 공통 버튼 스타일 적용에서도 제작 콘솔 하위는 제외해 저장한 화면 외형을 유지한다. 그래프는 저장된 `ModelMetricGraph`가 수신 데이터를 메시로 그리는 기존 방식을 사용한다.

구성 검사 페이지는 8개 실제 슬롯의 순서를 보여 준다. 처음 진입하거나 모든 슬롯을 비웠을 때 검증 문제가 없고 블록을 잡고 있지 않으면 청색의 `모델 만들기 안내` 상태와 `블록으로 모델을 만들어 보세요`를 표시한다. 오른쪽 선반의 블록을 Grip으로 잡고 빈 슬롯 위에서 놓는 두 단계, Trigger 설정 조작, `Conv → Pooling → Flatten → Dense` 예시를 안내한다. 이 안내는 화면 표현이며 빈 모델에 새 제약을 추가하지 않는다.

문제가 있으면 큰 제목과 원인·수정 방법을 표시한다. `ValidationIssue.Path`의 `/blocks/N`은 모델 내부 인덱스이므로 `Model.Layers[N]`의 레이어 참조를 실제 슬롯과 비교해 위치를 찾는다. 고정 Input·Output과 전체 연결 문제는 별도로 표시한다. 7종 검증 메시지를 한국어로 설명하며 알 수 없는 메시지도 원문과 일반 수정 안내를 남긴다. 여러 문제는 이전·다음 버튼으로 확인하고, 같은 문제가 유지되면 현재 선택을 보존한다. 끌기 중에는 오류 경보 대신 빈 슬롯 배치 방법과 실패 시 삭제되는 동작을 안내한다.

Workspace 변경 이벤트는 Manager에서 갱신 필요 상태로 모으고 `LateUpdate()`에서 한 번 직렬화·검증한다. `RefreshModelOrder()`의 연속 연결 해제·재연결 중간 상태는 화면에 표시하지 않는다. 같은 결과로 오류 안내와 마지막 학습 모델의 변경 여부를 갱신한다. 학습 버튼을 누를 때는 별도로 즉시 검증하며, 유효하지 않은 구성이면 요청을 보내지 않고 구성 검사 페이지에 초점을 맞춘다. 화면의 시작 버튼은 이때 `구성 문제 확인하기`로 표시되어 설명을 다시 열 수 있다. 작업 중이나 블록을 잡는 동안에는 시작할 수 없다.

`블록 순서 확인 완료`는 연결 순서와 공간 입력·벡터 전환 검사만 통과했다는 뜻이다. 예측 정확도나 학습 성공을 보장하지 않으며, 출력 공간 크기·특징값 수·파라미터 한도는 서버에서 별도로 검사한다. 빈 슬롯은 건너뛰고 모든 슬롯이 비어 있는 `Input → Output`도 기존 자동 펼치기 규칙으로 유효하다. `Input → Dense` 역시 Flatten을 요구하지 않으며, Conv·Pooling에서 Dense·Output으로 넘어갈 때는 Flatten이 필요하다.

요청 실패는 구성 검사와 구분한 큰 스크롤 가능 오류 패널에 표시한다. 서버가 보낸 실제 `message`를 보존하고, 알려진 커널·Pooling 출력 크기, 특징값 200,000개·파라미터 2,000,000개 한도, Flatten·순서 등의 거절 사유에는 한국어 원인과 수정 방법을 함께 제공한다. 서버가 문제 슬롯을 보내지 않은 경우 임의의 슬롯을 지목하지 않는다. 현재 `training_failed`는 상세 예외 대신 일반 실패 문구만 보내므로 구체적인 원인은 서버 로그에서 확인해야 한다고 안내한다. 알 수 없는 오류의 설명도 유지하며, 서버가 추가 설명을 보내지 않으면 그 사실을 표시한다.

## 씬·프리팹 연결

| 에셋·필드 | 구현 연결 |
| --- | --- |
| [XRPlayerRig.prefab](../client/Assets/Prefabs/XR/XRPlayerRig.prefab) | 공용 추적·입력 리그. `PersistentXRPlayerRig`가 씬 추가 로드 시 중복 리그를 제거 |
| [ModelBlock.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelBlock.prefab) | `ModelBlockController`, Collider, XRI 잡기 구성 |
| [PartsSupplyRack.prefab](../client/Assets/Prefabs/CNNModelDesignLab/PartsSupplyRack.prefab) | 공급소와 기본 블록 참조 |
| [ModelConveyor.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelConveyor.prefab) | 슬롯·설정 조작기기·고정 Input/Output 배치 |
| [SlotSettingsControl.prefab](../client/Assets/Prefabs/CNNModelDesignLab/SlotSettingsControl.prefab) | `SlotSettingsController`와 `Popup Anchor` |
| [ModelResults.prefab](../client/Assets/Prefabs/CNNModelDesignLab/ModelResults.prefab) | 컨베이어 중앙의 `Screen Anchor`에 연결된 제작 콘솔. 각 페이지·Controller·버튼·그래픽 참조를 저장하고, 개별 Text의 Font와 `ModelResultsController.Samples`를 Inspector에서 지정 |

벨트의 `Block Anchors` 배열이 슬롯 수·순서와 저장된 스냅 위치·방향을 지정한다. `Placement Half Extents`는 각 앵커 로컬 좌표의 배치 허용 범위다. `Slot Visuals`의 직접 자식 Renderer와 `Settings Controls`의 직접 자식 Controller를 앵커 배열 순서에 맞춰 저장한다. 초기화는 이 참조를 읽고 점유 상태를 준비하며 슬롯·설정 버튼을 생성하거나 위치·크기를 재계산하지 않는다. 실행 중 슬롯 수 변경은 지원하지 않는다.

공용 오른손 입력은 [XRPlayerInputController](../client/Assets/Scripts/Core/XR/XRPlayerInputController.cs)가 담당한다. 모델 씬은 레이 적중 여부만 검사한다. 서버 주소는 [WebSocketConnection](../client/Assets/Scripts/Core/Networking/WebSocketConnection.cs)의 `_serverUrl`에서 설정한다.

## 순수 모델 코드

소스는 [ModelDesign 폴더](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/)에 있으며 네임스페이스와 어셈블리 이름은 `ModelDesign`이다. [ModelDesign.asmdef](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelDesign.asmdef)는 `noEngineReferences: true`로 Unity API 의존성을 차단한다.

| 파일·클래스 | 역할과 처리 방식 |
| --- | --- |
| [LayerDefinition](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/LayerDefinition.cs) | 레이어 한 개의 종류와 filters·kernelSize·units·activation 같은 설정값을 보관한다. 설정 API로 실제 값이 바뀌면 `OnChanged`를 발생시켜 Workspace의 재검증과 블록 표시 갱신을 유도한다. 3D 위치나 슬롯 번호는 포함하지 않는다. |
| [ModelDefinition](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelDefinition.cs) | 서로 연결된 레이어 덩어리 하나를 읽기 전용 `Layers` 목록으로 나타낸다. Workspace가 연결·분리 작업의 결과로 이 객체를 구성하고 Validator와 Serializer가 목록을 읽는다. 블록이 이동한 뒤의 소속은 Workspace에서 다시 조회한다. |
| [ModelWorkspace](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelWorkspace.cs) | 고정 Input·Output과 모든 레이어 덩어리를 소유한다. 연결·분리·삭제·교체 요청이 오면 덩어리 목록과 레이어 이벤트 구독을 갱신하고 편집 검증을 다시 수행한다. 변경된 검증 결과와 모델 변경을 이벤트로 알려 씬·UI가 대응하게 한다. |
| [ModelValidator](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelValidator.cs) | 레이어 순서대로 공간 입력·벡터 상태를 추적해 Flatten 전환·Dense 연결 등 구조를 검사한다. 실제 공간 크기·파라미터 한도는 계산하지 않는다. 편집 검증은 아직 입력이 없는 조각의 판정을 보류할 수 있고, 전송 검증은 Input에서 고정 Output까지 유효하게 연결되었는지도 요구한다. 오류는 해당 모델과 블록 위치를 가진 `ValidationIssue` 목록으로 반환한다. |
| [ValidationIssue](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ValidationIssue.cs) | 검증에서 발견한 문제 하나를 `Model`, `Path`, `Message`로 묶는다. 호출자는 모델 참조로 문제가 속한 덩어리를 찾고 Path로 블록 위치를 구분해 오류를 표시할 수 있다. 검증을 실행하거나 모델을 수정하지 않는 결과 데이터다. |
| [ModelJsonSerializer](../client/Assets/Scripts/CNNModelDesignLab/ModelDesign/ModelJsonSerializer.cs) | 전송 검증을 먼저 실행하고 성공하면 Input에 연결된 덩어리를 실행 순서로 읽는다. Input은 입력 형태로, Output은 고정 Dense로 변환하고 레이어 종류에 필요한 필드와 `modelType`을 JSON에 기록한다. 실패 시 `TrySerialize()`는 오류 목록을, `Serialize()`는 예외를 제공한다. |

### 편집 API

| 호출·속성 | 동작 |
| --- | --- |
| `workspace.AddLayer(layer)` | 단독 블록 덩어리 추가 |
| `workspace.AddAndConnect(existingLayer, newLayer)` | 기존 덩어리의 비어 있는 끝점에 새 블록 추가·연결 |
| `workspace.Connect(first, second)` | 서로 다른 두 덩어리의 비어 있는 끝점 연결. 인자 순서 무관 |
| `workspace.Disconnect(first, second)` | 인접한 두 블록의 연결 해제 |
| `workspace.RemoveLayer(layer)` | 블록 삭제. 중간 블록의 양쪽은 별도 덩어리가 됨 |
| `workspace.ReplaceLayer(oldLayer, newLayer)` | 연결 위치를 유지하며 블록 교체 |
| `workspace.GetModel(layer)` | 블록이 현재 속한 덩어리 |
| `workspace.InputModel` / `workspace.Models` | Input 덩어리 / 전체 덩어리의 읽기 전용 목록 |
| `workspace.ValidationIssues` | 최신 편집 검증 결과 |
| `layer.SetFilters(filters)` / `SetKernelSize(height, width)` | Conv2D 설정 변경 |
| `layer.SetPoolSize(height, width)` | Pooling 크기 변경 |
| `layer.SetUnits(units)` | Dense 뉴런 수 변경 |
| `layer.SetActivation(activation)` | Conv2D·Dense 활성화 함수 변경 |
| `ModelValidator.ValidateEditing(model)` | 미완성 덩어리의 편집 검증. 입력 형태를 모르면 해당 판정 보류 |
| `ModelValidator.ValidateForTransmission(workspace)` | Input 덩어리의 구조와 고정 Output 도달 여부 검사 |
| `ModelJsonSerializer.TrySerialize(workspace, out json, out issues)` | 검증 실패 시 `false`와 오류 목록, 성공 시 JSON 반환 |
| `ModelJsonSerializer.Serialize(workspace)` | JSON 반환. 검증 실패 시 `InvalidOperationException` 발생 |

Input·Output은 Workspace 생성 시 하나씩 생기며 연결·분리만 가능하다. 분기·순환은 허용하지 않고 같은 블록은 한 Workspace의 한 덩어리에만 속한다. 덩어리 목록은 양방향으로 저장할 수 있으며 검증·직렬화는 Input에서 Output 방향으로 읽는다. 끝점 없는 조각은 양방향을 검사해 가능한 방향을 사용한다.

레이어 순서 오류와 미완성 모델은 편집 상태로 보관할 수 있다. 목록·연결·설정 변경에 성공하면 편집 검증 후 `OnValidationChanged`, `OnChanged` 순서로 이벤트가 발생한다. 동일한 값 설정과 거절된 작업은 이벤트를 발생시키지 않는다.

연결·분리·삭제·교체는 덩어리 객체를 새 목록으로 교체하므로 UI는 블록 참조를 유지하고 `GetModel()`로 현재 덩어리를 조회한다. 삭제·교체 시 레이어 이벤트 구독을 해제하고, Workspace 사용 종료 시 `Dispose()`를 호출한다. 위치·회전·크기·표시는 씬 계층에서 관리한다.

직렬화는 Input을 `input`, Output을 마지막 Dense 10개·Softmax로 변환한다. Input에 연결되지 않은 덩어리는 보내지 않는다. `modelType`은 전송 덩어리에 Conv2D 또는 Pooling이 있으면 `cnn`, 없으면 `mlp`다.

### 사용 예제

```csharp
using ModelDesign;

ModelWorkspace workspace = new ModelWorkspace();
LayerDefinition dense = LayerDefinition.Dense(32, ActivationType.ReLU);
workspace.AddLayer(dense);
workspace.Connect(workspace.Input, dense);
workspace.Connect(dense, workspace.Output);

workspace.OnValidationChanged += issues =>
{
    // issue.Model, issue.Path, issue.Message로 블록별 오류를 표시한다.
};

dense.SetUnits(64);
if (ModelJsonSerializer.TrySerialize(workspace, out var json, out var issues))
{
    // json을 ModelProtocolClient.TrainAsync()에 전달한다.
}

workspace.Dispose();
```

## 추론 샘플 연결

기본 샘플은 [Textures/CNNModelDesignLab](../client/Assets/Textures/CNNModelDesignLab/)의 MNIST 이미지다. `ModelResultsController`의 Inspector `Samples`에 이미지와 정답을 함께 연결한다. ID는 확장자를 제외한 이미지 에셋 이름이며 서버도 같은 ID의 MNIST 테스트 이미지를 사용한다. 이미지 픽셀은 전송하지 않는다.

| 정답 | 에셋 이름·전송 ID |
| --- | --- |
| 0 | `mnist-test-3` |
| 1 | `mnist-test-2` |
| 2 | `mnist-test-1` |
| 3 | `mnist-test-18` |
| 4 | `mnist-test-4` |
| 5 | `mnist-test-8` |
| 6 | `mnist-test-11` |
| 7 | `mnist-test-0` |
| 8 | `mnist-test-61` |
| 9 | `mnist-test-7` |

이미지는 공식 MNIST NPZ 테스트 데이터의 원래 순서를 기준으로 숫자별 첫 항목을 선택했다. 서버의 ID 해석은 [dataset.py](../server/app/dataset.py)의 `Dataset.index()`가 담당한다.
