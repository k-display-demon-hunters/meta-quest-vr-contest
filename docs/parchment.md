# 공용 양피지

## 배분과 위치

`client/Assets/Scripts/Core/Parchment/ParchmentLesson.cs`의 각 문서 `Location`이 배분과 태블릿 안내의 유일한 원본이다. `ParchmentLocation`은 None, PixelLabEntrance, ConvolutionLabEntrance, FeatureMapLabEntrance, ModelDesignLabEntrance를 구분한다. 현재 배분은 05 활성화 함수·ReLU → PixelLabEntrance, 01 이미지·픽셀 → ConvolutionLabEntrance, 02 합성곱·패딩 → FeatureMapLabEntrance, 03 필터·특징 추출 및 04 특징 맵 → ModelDesignLabEntrance이다. 내용보다 현재 공간의 접근 순서를 기준으로 지정했다. enum 설정은 수집 조건과 위치 안내를 바꾸며 씬 Transform을 자동 이동하지 않는다.

05번은 튜토리얼 출구와 픽셀 입구 사이 통로의 월드 좌표 (-10.5, 1.45, 1.2), Y 회전 0도로 이동했다. 통로 자식으로 연결해 닫힌 픽셀 입구 전에 수집할 수 있다. 나머지 양피지의 실제 배치는 유지한다. 진행은 튜토리얼 완료 → 05 수집 → 픽셀 입장 → 스크린 완료 및 01 수집 → 합성곱 입장 → 합성곱 완료 및 02 수집 → 특징 맵 입장 → 특징 맵 완료 및 03·04 수집 → 모델 설계 입장 순서다.

해당 위치의 문서를 모두 수집해야 문을 열 수 있다. 문서가 배정되지 않은 위치는 개방 조건을 충족하지 않는다. 본문·페이지·삽화는 기존 데이터를 유지한다.

## 수집과 열람

Core/Parchment의 Pickup은 활성/비활성 등록과 XRI 입력만 전달한다. Manager가 3m 거리와 중복 수집·열람을 확인하고 클릭 즉시 수집을 기록해 Collected(documentId)를 알린다. VisualController가 다음 프레임에 원본을 비활성화하며 공용 Reader를 연다. 열람을 닫아도 수집은 유지한다. 마지막 페이지 완료는 열람 종료다.

수집 상태는 공용 Manager의 세션 메모리에 유지한다. 위치 정보는 Catalog에서 조회하므로 Pickup이 사라져도 유지된다. 태블릿은 읽기 전용 ParchmentInventoryItem을 표시하고 현재 XR 카메라와 시작 위치를 전달해 재열람을 요청한다. Manager가 이동 차단과 열람창 수명을 관리한다. 최초 현장 열람 완료는 재열람의 선행 조건이 아니다.

## 공장 연결

FactoryNavigationData는 입구·출구 및 ROOM #1 내부문 참조를 소유한다. FactoryLearningFlowManager는 Collected 이벤트를 구독하고 진행 가능한 시점에 IsGroupCollected(location)와 기존 진행 조건을 함께 조회한다. 수집 상태를 복제하지 않는다.

각 방 입구는 이전 단계 완료와 콘텐츠 준비, 해당 위치 수집을 모두 요구한다. 현재 방 출구는 기존 준비 성공 시점에 열려 다음 입구 앞에 접근할 수 있다. ROOM #1 내부문은 스크린 완료와 해당 위치 수집을 요구하며 합성곱 시작 시 닫힌다. 튜토리얼 입구·후문에는 수집 조건을 추가하지 않는다. 진입·로딩 중 이벤트는 다음 조건 확인 시점까지 보류한다.

## 편집 도구

ParchmentSceneSetup의 Refresh Learning Parchment Labels 메뉴는 열린 씬의 기존 Pickup 라벨을 문서 데이터에 맞춘다. 이전 공간 좌표로 씬을 열고 전체 재생성하던 메뉴는 제거했다. 위치 변경은 씬 Transform에서 수행한다. Pickup/Visual 스크립트는 Core/Parchment로 이동했고 기존 GUID를 유지한다.

Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.
