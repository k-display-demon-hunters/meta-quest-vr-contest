# CNN실 #8 피처맵 복도

[CNNFeatureMapLab.unity](../client/Assets/Scenes/CNNFeatureMapLab.unity)의 학습 진입점은 `CorridorRoot`의 `CorridorDirector`다. 기존 XR 리그와 공장 학습 세션을 사용한다. 공장에서는 Room 02의 기존 앵커와 출입구 사이에 들어오도록 복도를 0.53배로 배치한다. 단독 씬의 복도 좌표는 1:1이다.

## 편집 모드의 배치

씬의 `CorridorRoot`는 [FeatureMapCorridor.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/FeatureMapCorridor.prefab) 인스턴스다. 입력판, 피처맵, 바닥, 출력 장치, 문, 안내 UI와 연출용 오브젝트가 미리 저장되어 Play 없이 Scene 뷰에서 보이고 편집된다.

- 씬에서 인스턴스를 편집하거나 프리팹을 열어 Transform과 UI 배치를 수정한다. 공통 원본에 반영할 변경만 프리팹에 Apply한다. 실행 시 복도 오브젝트를 다시 생성하지 않는다.
- 입력 그림은 `Input image`, RGB 펼침 위치는 `RGB expanded 1~3`, 출력 통은 `Output 1~3`, 정답 도착 위치는 각 통의 `Answer destination`, 비교 연출 기준점은 `Evidence staging`, 문은 `Corridor door`에서 편집한다. 맵·입력판 크기는 패널 루트의 Scale로 조절한다.
- 출력 통의 액체·점수 기둥은 저장된 크기를 최대 표시 기준으로 사용한다. 문은 저장된 닫힘 위치에서 움직인다. 자막·팝업은 저장된 배율을 사용하되 위치는 실행 중 카메라를 따라간다. 필터·강조선·계산 결과판은 학습 단계에 따라 이동·표시되므로 편집 시의 위치가 고정 표시 위치를 뜻하지는 않는다.
- RGB 채널, 필터 카드, 계산 연출, 완료 팝업 등 단계별 오브젝트는 프리팹에 존재하지만 초기에는 비활성 상태일 수 있다. Hierarchy에서 선택해 편집하고 초기 표시 상태를 유지한다.
- `Editor/CorridorPrefabBuilder.cs`의 `Tools > CNN > Save Feature Map Corridor Prefab`은 최초 변환용이다. 기존 프리팹이 있으면 재생성하지 않으며, 이후 배치는 저장된 프리팹을 직접 편집한다.

## 진행

에디터에서 HMD가 없으면 Game 뷰의 마우스 왼쪽 클릭으로 그림·맵·필터·UI 버튼을 선택한다. 기존 가상 오른손 레이와 Shift 없는 T 입력도 유지한다. 마우스를 움직이면 화면 포인터, T를 누르면 오른손 레이로 선택 경로가 바뀐다. Quest에서는 기존 오른손 트리거를 사용한다. 다음 버튼이 숨겨져 있으면 말풍선이 가리키는 그림·필터·스위치를 조작한다. 이동은 기존 스틱/WASD 설정 그대로다.

1. 입력 숫자 1·2·3을 바꿔 보거나 입력 칸을 눌러 RGB를 펼친다. 같은 입력판을 0.4초 안에 두 번 누르면 모인다. 입력 칸을 누르면 흰 테두리 투어가 시작된다.
2. 흰 테두리 투어는 선택 칸 → 3×3 → 5×5 → 7×7 순으로 이어진다. 가장자리에서는 실제 맵에 포함된 칸만 그린다.
3. 의무 라운드는 피처맵 1-1, 2-1, 3-1, 현재 입력 숫자의 출력 통이다. 다른 피처맵을 열면 의무 진행률과 별도로 같은 계산을 볼 수 있다.
4. 결과 칸을 선택하고 필터를 펼친 뒤 훑기, 필터 설명, 선택 칸 비교, 합치기를 진행한다. 출력에서는 이어서 틀 맞추기, 빛 탱크 분배, 답 비행을 본다.
5. 출력 라운드가 끝나면 스위치 10개가 열린다. 맵을 끄고 다음을 누른 뒤 해당 맵을 다시 켜면 완료할 수 있다. 완료하면 복도 문이 열리고 기존 공장 세션에 출구 사용 가능 상태를 보낸다.

애니메이션은 그 단계 안에서만 끝난다. 다음 단계는 다음 버튼이나 명세에 지정된 직접 입력으로 들어간다. 이전 버튼은 앞 단계의 연출을 다시 시작한다. 공용 공간 버튼과 팝업은 [공용 공간 UI](world-ui.md)를 사용한다.

## 입력 연결 (2026-11-09 수정)

- `CorridorPointer`는 Director가 소유하는 복도 전용 입력 처리 객체다. 기존 XRI 오른손 `activateInput`과 레이 원점을 사용하며 별도 InputAction·EventSystem·SDK를 만들지 않는다. `CorridorVisuals.Listen`으로 등록된 Collider 콜백만 호출하고 기존 activated 이벤트 중복 구독을 없앴다. 기존 표시 클래스는 직렬화된 일반 클래스이므로 새 MonoBehaviour 인터페이스를 강제로 붙이는 대신 기존 콜백 계약을 유지한다.
- 37개 상호작용 Collider는 `CorridorInteractable` 레이어(8)를 사용한다. 입력/RGB 4개, 피처맵 10개, 스위치 10개, 필터 5개, 출력 3개, 바닥 구역 5개다. 장식 Collider는 선택 판정에 포함하지 않는다. RGB 판 3개는 누락된 BoxCollider와 클릭 콜백을 프리팹에 추가했다.
- 칸 좌표는 패널 로컬 hit point를 폭·16칸으로 변환한다. 기존 코드는 textureCoord를 쓰지 않으므로 BoxCollider를 유지한다. 회전·부모 배율은 InverseTransformPoint로 처리하고 왼쪽 위는 (0,0), 오른쪽 아래는 (15,15)로 clamp한다. 실제 화면의 방향·칸 대응은 실행 검증 전이다.
- UI는 기존 XRUIInputModule과 TrackedDeviceGraphicRaycaster를 사용한다. 복도 진입 시 저장된 Canvas의 Event Camera를 현재 리그 카메라에 연결한다. 복도 UI의 장식에 의한 3D 차폐·뒷면 거부를 해제했다. `CorridorMouseRaycaster`는 HMD 없는 에디터에서만 일반 포인터를 처리하며 TrackedDeviceEventData는 처리하지 않아 VR 입력과 중복되지 않는다.
- 입력 판정은 UI 우선이다. 3D는 trigger down 프레임에 한 번만 전달하며, 같은 입력/RGB/필터 대상을 0.4초 안에 다시 누르면 두 번째 입력은 접기만 한다. UI 버튼 콜백은 기존 코드 구독을 유지하고 완료 팝업 확인은 공용 WorldUIPopupController가 닫는다.
- 복도에 있는 동안 오른손 레이·표시 거리를 30m로 사용하고 나가거나 비활성화하면 원래 설정을 복원한다. 가리키는 칸에는 별도 흰 테두리를 표시하여 학습용 강조선을 덮어쓰지 않는다. 필터·스위치·출력 통은 hover 시 1.04배, 레이는 Cyan, 클릭 시 진폭 0.3/0.05초 햅틱을 요청한다. 실제 햅틱 출력은 리그/기기 지원에 따른다.
- 숫자 변경은 모델을 다시 계산하고 현재 단계에서 라운드를 재구성한 후 필터를 접는다. 완료 후 숫자 변경도 가능하다. 필터 접기는 진행 단계를 되돌리지 않는다. 다른 맵의 선택 라운드는 돌아올 상태를 보관한다. 이전·다음은 프레임당 한 번만 처리하며 다음으로 진행하기 전에 현재 훑기·합치기·출력 연출을 완료한다.
- Inspector의 `CorridorDirector > Show Debug`를 켜면 에디터/Development Build에서 상태·단계·대상·칸·마지막 클릭/UI 이벤트를 표시한다. 왼손 위 월드 텍스트와 에디터 화면 표시를 사용하고 릴리스에서는 제외한다. 클릭·상태 전환 로그는 `[Corridor]` 접두사로 남긴다.

### 진단과 확인 범위

| 항목 | 파일에서 확인한 내용 |
| --- | --- |
| SDK | manifest: XRI 3.3.2 / OpenXR 1.16.1. Meta/Oculus 전용 UI로 교체할 근거 없음. |
| 이동/트리거 | XRPlayerInputController와 XRPlayerRig는 스틱 이동, XREditorSimulator는 WASD 이동과 T 트리거를 분리한다. 트리거 이동 차단 플래그는 불필요하여 공용 이동을 수정하지 않음. |
| EventSystem | 공용 XRPlayerRig에 EventSystem 1개와 XRUIInputModule 1개. 단독 씬은 해당 리그 인스턴스를 참조하며 추가 EventSystem은 없음. |
| UI | 기존 TrackedDeviceGraphicRaycaster는 마우스용이 아니고, 숫자/중첩 버튼에는 3D occlusion이 켜져 있었음. Camera 필드는 비어 있었으나 XRI는 Camera.main fallback을 지원하므로 그 자체를 불통 원인으로 단정하지 않음. CanvasGroup은 저장된 복도에 없음. |
| 3D | 기존 XRSimpleInteractable.activated는 XRI hover/등록/거리 판정에 의존했고 기본 레이는 5m. RGB 판 3개는 Collider와 클릭 구독이 모두 없었음. OnMouseDown·마우스 전용 물리 클릭 코드는 사용하지 않았음. |
| 좌표 | 기존 BoxCollider + InverseTransformPoint 방식으로 textureCoord 오류에는 해당하지 않음. |
| 초기화/Console | 저장된 모델 참조와 Play 초기화를 확인함. 전체 입력 불통의 단일 원인을 단정하지 않았으며 실행에서 발견한 투어 메시 오류는 수정 후 재검증함. |
| 진행 | 시작은 입력 선택 대기. 다음/이전/숫자는 코드 구독 존재. RGB 미연결, 필터 접기 시 0단계로 복귀, 숫자 변경 시 라운드 이탈, 의무 맵 중 다른 층 선택 무시를 수정함. |

프리팹 파일 ID/컴포넌트 참조 점검에서 누락·중복이 없고 기존 Transform/RectTransform 블록 변경은 0개다. git diff --check를 확인했다. 이 결과는 소스·직렬화 정적 확인이며 컴파일/Play 통과를 뜻하지 않는다. 수동으로 레이어를 만들거나 버튼을 다시 연결할 필요는 없다.

2026-11-09 사용자 허가 후 Unity 6000.3.24f1의 D3D11 배치 Play 모드에서 가상 XR 트리거와 가상 마우스 경로를 각각 처음부터 끝까지 실행했다. 진행 메서드나 Button.onClick을 검증기에서 직접 호출하지 않았다. Input System의 기기 상태 이벤트 → 기존 XRI/uGUI → 실제 복도 콜백 경로를 사용했다. OS 마우스 커서·창 포커스는 조작하지 않았다.

| 실행 항목 | 가상 XR 트리거 | 가상 마우스 |
| --- | --- | --- |
| 시작 위치 입력판 선택 → RGB 펼침·투어 시작 | 통과 | 통과 |
| 입력판 두 번 누르기, RGB 채널 칸 선택 | 통과 | 통과 |
| 좌상단 (0,0), 우하단 (15,15) | 통과 | 통과 |
| 투어 다음/이전 및 의무 라운드 진입 | 통과 | 통과 |
| 필터 펼침·두 번 눌러 접기·다시 펼침, 단계 유지 | 통과 | 통과 |
| 훑기 중 숫자 1→2 변경, 재계산 및 단계 유지 | 통과 | 통과 |
| 필수 1-1 / 2-1 / 3-1 및 출력 2 라운드 | 통과 | 통과 |
| 선택 2-2 라운드 후 원래 의무 안내 복귀 | 통과 | 통과 |
| 스위치 끄기→다음→켜기→완료 | 통과 | 통과 |
| 완료 확인으로 팝업 닫힘·진행률 1·출구 사용 가능·문 애니메이션 완료 | 통과 | 통과 |
| 완료 후 숫자 3 변경 및 빈 공간 입력 시 리그 위치 유지 | 통과 | 통과 |

첫 XR 실행에서는 전체 진행이 완료됐으나 투어 재시작에서 Mesh.vertices 오류가 기록됐다. ReceptiveFieldTour.Clear에서 이전 삼각형 인덱스를 제거한 뒤 새 정점 배열을 설정하도록 수정했다. 수정 후 두 입력 경로 모두 전체 진행에서 런타임 오류 없이 PASS ALL을 확인했다.

검증기는 이동 구간의 리그 위치와 시점을 설정해 대상에 접근했다. 실제 WASD/스틱을 이용한 복도 보행, Quest 물리 컨트롤러·햅틱, 기기 렌더링/성능, APK 빌드는 이 검증에 포함하지 않았다. 가상 마우스 경로에서는 화면 밖 대상을 바라보도록 가상 HMD를 회전했다. 임시 Editor 검증 스크립트와 .meta는 검증 후 삭제했다. 삭제 후 최종 컴파일도 종료 코드 0으로 완료했으며 실제 기능 코드에 검증기는 포함되지 않는다. 아래는 이전 작업의 별도 기록이다.

## 모델 교체

`CnnModelProvider`의 Model Instance 슬롯에는 복도 안에 미리 배치한 [DummyCnnModel.prefab](../client/Assets/Prefabs/CNNFeatureMapLab/DummyCnnModel.prefab) 인스턴스의 모델 컴포넌트가 연결돼 있다. 실행 시 모델 오브젝트를 새로 생성하지 않는다. 모델을 교체할 때는 `ICnnModel`을 구현한 컴포넌트를 복도에 배치해 이 슬롯에 연결한다. 저장된 배치는 입력 세 채널, 은닉층 2·3·5장, 출력 숫자 1·2·3 및 16×16 맵을 기준으로 한다. 크기·채널 구성을 변경할 때는 저장된 패널·텍스처·참조도 함께 맞춰야 한다.

더미 모델의 각 층은 3×3 합성곱, stride 1, zero padding, bias와 ReLU를 사용한다. 꺼진 맵은 pre를 유지하고 act만 0으로 전달한다. 출력은 마지막 층의 정규화된 맵과 숫자별 템플릿을 내적으로 비교한다. softmax 확률은 최대 나머지 방식으로 정수화한다. 모델 교체 시 더미 필터에 대한 설명은 `CorridorDialogue`의 맵별 설명 테이블에서 함께 교체한다.

## 파일 위치

- 복도 코드는 `client/Assets/Scripts/CNNFeatureMapLab/`에 있다. `CorridorDirector`가 상태를, `RoundPlayer`와 `ReceptiveFieldTour`가 연출을, `CorridorDialogue`가 화면 문구를 관리한다. 스크립트의 `.meta`와 GUID는 이동 전과 동일하다.
- [CorridorTheme.asset](../client/Assets/Settings/CorridorTheme.asset)에서 폰트, 셀 패널 크기, 색과 머티리얼용 셰이더를 바꿀 수 있다.
- `FeatureMapCorridor.prefab`에는 각 표시 클래스의 직렬화 참조와 모든 복도 오브젝트가 저장돼 있다. `CorridorDirector.Awake`는 이 참조에 계산·입력을 연결하고, 활성 기간의 입력 구독은 `OnEnable`/`OnDisable`에서 연결·해제한다. 런타임은 텍스처 픽셀, 강조 메시, 문구·색·활성 상태와 연출을 갱신한다. 변경하는 텍스처·메시는 인스턴스별 복사본을 사용해 저장 원본을 보존한다.
- 복도 전용 재질·텍스처·강조 메시는 각각 `Materials/CNNFeatureMapLab/`, `Textures/CNNFeatureMapLab/`, `Meshes/CNNFeatureMapLab/`에, TMP 폰트·아틀라스는 `Fonts/CNNFeatureMapLab/`에 있다. 후처리 프로파일은 `Settings/CorridorPostProcessing.asset`이다. 버튼·팝업은 기존 Common 원본을 사용해 배치했다.
- 이전 필터 비교·8×8 피처맵·별도 활성화 함수의 미사용 코드·에셋은 제거된 상태다. 새 복도 표시용 에셋은 이전 에셋을 복원한 것이 아니라 현재 복도 생성 결과를 저장한 것이다.

## 실행 확인

2026-11-09 편집용 저장 전환: 사용자 허가 범위에서 Unity 스크립트를 컴파일하고 프리팹·씬·표시 에셋을 저장했다. Play하지 않은 Scene 뷰에서 복도 표시를 확인했다. 이번 전환의 Play 테스트·앱 빌드는 수행하지 않았다.

아래는 전환 전 런타임 생성 방식의 실행 확인 기록이며, 이번 변경의 플레이 검증 결과가 아니다.

Unity 6000.3.24f1 에디터에서 더미 숫자 1·2·3의 예측, 맵 스위치의 전파, 필수·선택 라운드와 완료 상태를 확인했다. 숫자 2의 예측 확률은 97.56%였다.

Direct3D 12에서 투어 자막·출력·완료 팝업을 렌더링했다. XRI의 추적 레이 이벤트로 이전·다음·확인 버튼의 타격을 검증하고, 확인 버튼이 팝업을 닫는지 검사했다. 복도가 활성화된 완료 상태에서 `CorridorDirector.LateUpdate`를 48회 샘플링한 관리 힙 할당은 0바이트였다. 이 수치는 에디터의 해당 경로에 한정된다. ADB에 연결된 Quest가 없어 실기기 72fps와 전체 프레임의 GC는 측정하지 못했다.

단독 씬의 기본 시작 위치에서 가상 오른손 컨트롤러로 48회 레이 입력을 수행해 숫자 변경, 입력 칸 선택, 이전·다음, 필수 라운드 4개, 스위치 복원과 완료 팝업 닫기를 검증했다. 진행 함수는 직접 호출하지 않았으며, 구역 사이의 위치 이동은 검증 코드가 설정했다.
