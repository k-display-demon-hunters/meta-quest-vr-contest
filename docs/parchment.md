# 공용 양피지

## 배분과 위치

`client/Assets/Scripts/Core/Learning/LearningStageCatalog.cs`가 체험별 문서 배분의 원본이다. 각 단계의 `EntranceParchmentIds`는 바깥문 입장 필수 문서이며, `ParchmentIds`는 입장 문서와 내부 문서를 합친 전체 소속 목록이다. 태블릿 이동 시 지급과 문서 소속 조회에는 전체 목록을 사용한다. `ParchmentCatalog`는 문서 ID와 제목·본문·페이지·삽화만 소유한다.

| 체험과 사용 위치 | 문서 ID | 화면에 표시하는 문서 번호 |
| --- | --- | --- |
| PixelLab 바깥 입구 | 4 | 05 활성화 함수·ReLU |
| PixelLab 내부 진행 | 0 | 01 이미지·픽셀 |
| FeatureMap · 특징 맵 | 1 | 02 합성곱·패딩 |
| ModelDesign · 모델 제작 | 2, 3 | 03 필터·특징 추출, 04 특징 맵 |

PixelLab의 전체 소속 문서는 `{4, 0}`이며 바깥문 입장 조건은 `{4}`다. 문서 0은 방 안에 있으므로 바깥문 조건에 포함하지 않는다. 픽셀 관찰 이후의 문서 0 수집과 내부 영역 진입은 CNNPixelLabManager가 판단한다. 외부 학습 ID는 PixelLab 하나이고 내부 합성곱 단계를 별도 ID로 공개하지 않는다. 문서 ID는 LearningStageId의 숫자와 별개다.

05번은 튜토리얼 출구와 픽셀 입구 사이 통로의 월드 좌표 (-10.5, 1.45, 1.2), Y 회전 0도에 있다. 닫힌 픽셀 입구 전에 수집할 수 있다. 양피지 5개는 씬의 `Factory Environment/Parchments` 아래에 문서 번호 순서로 모아 둔다. Parchments의 로컬 위치·회전은 0, Scale은 1이며, 기존 방·통로에서 부모를 변경할 때 각 양피지의 월드 위치·회전·크기를 보존했다. 나머지 양피지의 실제 배치는 유지한다. 걸어서 이동하는 순서는 튜토리얼 완료 → 05 수집 → 픽셀 입장 → 스크린 완료 및 01 수집 → 합성곱 입장 → 합성곱 완료 및 02 수집 → 특징 맵 입장 → 특징 맵 완료 및 03·04 수집 → 모델 제작 입장이다.

`HasCollectedAll(documentIds)`는 전달된 문서를 모두 보유했는지 확인한다. 빈 요구 목록은 충족된 것으로 처리한다. 튜토리얼과 후문에 양피지 조건을 추가하지 않는다.

## 수집과 열람

Core/Parchment의 Pickup은 씬에 저장된 위치·방향을 유지하며 활성/비활성 등록과 XRI 입력을 전달한다. Manager가 3m 거리와 중복 수집·열람을 확인하고 원본을 잠시 숨긴 뒤 공용 Reader를 연다. 마지막 페이지의 [학습 완료]를 눌렀을 때 수집을 기록하고 `Collected(documentId)`를 알린다. 완료 전에 닫으면 미수집 상태로 원본을 다시 표시한다. 이미 수집한 문서의 재열람은 보유 상태를 유지한다.

수집 상태는 기존 공용 `ParchmentManager`의 영속 메모리에 유지한다. 공장 씬 종료 시 폐기되는 학습 진행 기록과는 별개다. 디스크에 저장하지 않는다. `ParchmentInventoryItem`은 문서 ID로 학습 카탈로그의 배분을 역조회해 `Stage`와 `LocationText`를 제공하므로 Pickup이 사라져도 위치 안내를 유지한다. 입장 문서는 해당 체험 입구 앞, 내부 문서 0은 “픽셀 랩 체험실 내부”로 표시한다.

태블릿 양피지 앱은 `HasCollected`로 수집 여부를 조회해 보유한 문서의 제목·주제만 목록에 표시하고, 현재 XR 카메라와 시작 위치를 전달해 재열람을 요청한다. Manager가 이동 차단과 열람창 수명을 관리한다. 현장에서 처음 여는 문서는 학습 완료 후 태블릿 재열람 목록에 추가된다. 태블릿 단계 이동으로 자동 지급된 문서는 현장 열람 없이도 재열람할 수 있다.

## 학습 세션과 공장 연결

Core의 `LearningSessionManager`가 `Collected` 이벤트를 받아 공통 학습 카탈로그의 `EntranceParchmentIds`와 `HasCollectedAll`로 진입 조건을 갱신한다. 수집 기록은 복제하지 않는다. `FactoryEnvironmentManager`는 세션이 전달한 진입 가능 상태로 실제 문과 경로를 제어한다. 문·영역·기준점 참조는 공장의 `FactoryNavigationData`에 둔다.

각 방 입구는 이전 체험의 진행 가능 상태와 콘텐츠 준비, 입장 필수 문서 수집을 요구한다. 현재 방 출구는 다음 콘텐츠 준비가 성공하면 열려 다음 입구 앞에 접근할 수 있다. ROOM #1 내부문은 CNNPixelLabManager가 픽셀 관찰 완료와 문서 0 수집을 확인해 열고 내부 씬 교체 후 닫는다. Core는 해당 체험의 ParchmentIds를 ParchmentManager.HasCollectedAll로 확인하는 조회 함수를 PixelLab에 연결한다. PixelLab이 이 함수를 사용할 내부 진행 시점을 정하며 문서 ID 목록을 중복 보관하지 않는다.

## 태블릿 단계 이동 요청에 따른 지급

`LearningSessionManager`는 `TabletManager.StageMoveRequested(LearningStageId)`를 받으면 공통 카탈로그의 순서대로 선택한 체험까지의 `ParchmentIds`를 `ParchmentManager.CollectDocuments`에 전달한다. enum 숫자나 문서 번호로 진행 순서를 추론하지 않는다. 문서 소속 조회는 같은 전체 목록을 사용하는 `GetParchmentStage(documentId)`로 수행한다.

| 선택한 체험 | 현재 배분 기준 보유하게 되는 문서 |
| --- | --- |
| 픽셀 랩 | 05, 01 |
| 특징 맵 | 05, 01, 02 |
| 모델 제작 | 05, 01, 02, 03, 04 |

지급은 이동 요청을 받았을 때 수행한다. 이동 거절·로딩 실패·이전 단계로 복귀해도 이미 수집한 문서를 회수하지 않는다. 처음 얻는 문서만 기존 Collect 경로로 기록하고 Collected 이벤트를 발생시켜 태블릿 목록과 입장 조건을 갱신한다. 등록된 Pickup은 기존 수집 표시 콜백으로 숨기며, 아직 등록되지 않은 Pickup은 나중에 등록될 때 보유 상태를 반영한다. 자동 지급은 열람창을 열지 않고 체험 완료 기록도 변경하지 않는다.

## 태블릿 재열람과 편집 도구

Pickup과 태블릿은 `client/Assets/Prefabs/Common/ParchmentReader.prefab`을 공유한다. TabletManager는 공용 ParchmentManager의 Opened·Closed·Collected 이벤트를 연결·해제한다. 열람 중 태블릿 입력을 중단하고 닫으면 이전 조작 상태와 포커스를 복원하며 다음 프레임부터 입력을 받는다. 수집 상태는 태블릿 비활성화나 씬 이동으로 초기화하지 않는다. 양피지 페이지는 오른손 UI 레이와 Trigger로 조작한다.

ParchmentSceneSetup의 Refresh Learning Parchment Labels 메뉴는 열린 씬의 기존 Pickup 라벨을 문서 데이터에 맞춘다. 이전 공간 좌표로 씬을 열고 전체 재생성하던 메뉴는 제거했다. 위치 변경은 씬 Transform에서 수행한다. Pickup/Visual 스크립트는 Core/Parchment에 있고 기존 GUID를 유지한다.

이번 PixelLab 단일 체험 통합과 문서 배분 구분은 소스 기준으로 정리했다. Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.

## 말린 양피지와 펼침 연출

현장 양피지는 기존 이미지의 양끝을 재사용한 말린 형태로 표시한다. Reader는 약 0.8초 동안 사용자 앞으로 이동하며 펼쳐지고, 펼침 후반에 본문이 나타난다. 펼침 완료 후 페이지 버튼과 UI 레이를 활성화한다. 이동·펼침 중 입력은 받지 않는다. 생성한 Sprite는 VisualController 종료 시 정리한다. 배치 Transform은 실행 시 문서 좌표로 덮어쓰지 않는다.
