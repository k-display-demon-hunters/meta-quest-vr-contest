# 폐공장 환경 씬

`client/Assets/Scenes/FactoryEnvironment.unity`는 공장 환경과 수집형 교육 문서 5개를 담은 씬이다. 기존 공용 `XRPlayerRig` 프리팹 인스턴스를 포함하므로 단독 Play에서 양피지 수집과 태블릿 열람을 사용할 수 있다. `FactoryIntro`의 튜토리얼 후 이 씬으로 진입하며, 공장을 유지한 채 필터 → 피처맵 → 모델 제작을 순서대로 Additive 로드한다. Unity 1 unit은 1 m로 사용한다.

## 배치와 동선

```text
오프닝·튜토리얼 진입 구역 (-Z, 기존 정문·하역장/보안 구역)
  → 중앙 복도
      ├─ Convolution Room · 서쪽 아래 (기존 원본 검사실, x -19~-3, z -1~12)
      ├─ CNN Room · 서쪽 위          (기존 특징 추출실, x -19~-3, z 14~28)
      ├─ MLP Room · 동쪽             (기존 모델 조립실, x 3~21, z 10~28)
      └─ 최종 마무리 구역 · 오른쪽 위 (기존 출하 검사 구역, x 3~21, z 29~40)
```

오프닝에서 공장으로 들어오는 위치는 `(0, 0, -20)`, 출하 구역에서 엔딩으로 이어질 위치는 `(12.5, 0, 40)`이다. 중앙 복도와 각 실 사이에는 약 3.4 m의 열린 출입구를 두었다. 정문에서 필터 체험 시작을 누르면 첫 공간으로 이동한다. 각 체험의 완료 조건은 아래 연결 항목을 따른다.

## 씬 계층

`Factory Environment` 루트 아래에 `00` 정문/하역장, `01` 보안 구역, `02` 중앙 복도, `03` 원본 검사실, `04` 특징 추출실, `05` 모델 조립실, `06` 출하 구역, `07` 조명, `08` 콘텐츠 연결 기준점이 있다. 이 기존 GameObject 이름과 배치는 유지하며, 공간을 설명할 때는 위의 합의된 방 이름을 사용한다. 양피지는 `03 Original image inspection room`과 `05 Model assembly room` 아래의 `Learning Parchments`에 배치한다. 팀원이 작업할 때는 해당 실의 `CONTENT ZONE` 빈 Transform을 기준으로 교육 장치를 배치할 수 있다. 중심부의 대략 6 × 6 m는 콘텐츠용으로 비워 두고 큰 고정 설비는 가장자리로 배치했다.

씬 전용 URP 재질은 `Assets/Materials/FactoryEnvironment/`, 표면 이미지와 노멀·금속/매끄러움 맵은 `Assets/Textures/FactoryEnvironment/`에 있다. 마모된 콘크리트, 벗겨진 도장 강판, 산화된 철, 고무, 주의 도색, 철제 그레이팅을 사용한다. 에셋의 `.meta`를 이동·재생성하지 않아야 씬 참조가 유지된다.

## 방 입구 간판

중앙 생산 복도에서 보이는 세 출입구의 상부 벽에 약 3.7 × 1.1 m 크기의 낡은 금속 프레임 간판을 배치했다. 문 아래 통행 높이 3 m는 유지한다. 간판 표면은 씬 전용 텍스처와 URP Unlit 재질로 구성해 어두운 복도에서도 글자를 읽을 수 있다.

- 단비 / 서쪽 아래 원본 검사실: `Convolution Room`
- 다연 / 동쪽 모델 조립실: `MLP Room`
- 하율 / 서쪽 위 특징 추출실: `CNN Room`

## 조명과 편집

고정된 등기구 표현, 8개의 그림자 없는 점광원, 무너진 지붕 틈으로 들어오는 차가운 방향광을 배치했다. 방향광은 부드러운 실시간 그림자를 만든다. 구조물은 정적 배치로 표시했으며, 라이트맵은 아직 굽지 않았다. 상부 철판과 트러스는 일부 무너진 지붕 형태로 만들었다. Scene View에서 실 내부를 편집할 때 해당 공간의 지붕 오브젝트를 임시로 숨기면 된다.

주요 바닥과 벽 외에 보안 설비, 실별 고정 기계·보관함, 조립실 부품 랙, 출하 컨베이어와 상자에도 Box Collider를 두었다. 출하 컨베이어와 상자는 동쪽 벽 쪽에 배치해 `(12.5, 0, 40)` 출구 앞 통로를 비웠다. 특징 추출실의 관찰창은 반투명 재질이다.

씬의 카메라·입력·태블릿은 `Assets/Prefabs/XR/XRPlayerRig.prefab`의 일반 씬 인스턴스를 사용한다. 기존 리그가 있는 상태에서 이 씬을 로드하면 `PersistentXRPlayerRig`가 중복 리그를 제거하므로 기존 태블릿과 획득 상태가 유지된다. 양피지 추가 작업에서 빌드 씬 목록과 씬 전환은 변경하지 않았다.

## 태블릿 공장 미니맵

태블릿 왼쪽의 `현재 위치` 영역은 실제 공장 배치를 보여준다. 짙은 파란 바탕에 방과 복도는 파란 면, 벽은 밝은 파란 선, 플레이어는 시선 방향을 가리키는 청록색 화살표로 표시한다. 작은 지도는 플레이어의 실제 카메라 X/Z 위치를 따라가며 북쪽(+Z)을 위로 고정한다. 표시 범위의 세로 길이는 22m다. 머리의 물리 이동과 시뮬레이터 WASD 이동도 반영한다.

미니맵을 왼손 X로 선택하면 기존 상세 시트에 공장 전체 지도가 열린다. 방 이름과 미수집 양피지 번호 1~5가 표시되며, Y 또는 완료로 닫으면 이전 포커스로 돌아간다. 작은 지도에는 현재 표시 범위 안의 양피지만 표시하며 전체 지도에는 공장에 남은 양피지를 모두 표시한다. 수집된 양피지 마커는 사라지지만 기존 인벤토리 등록과 문서 열람은 유지된다.

`Assets/Scripts/Core/Tablet/TabletMinimapController.cs`는 공용 태블릿의 표시 전용 컴포넌트다. `FactoryEnvironment`의 `Factory Environment` 루트 아래에서 이름이 `structural slab`으로 끝나는 바닥 BoxCollider와 지면에 닿는 `concrete` 벽/기둥 BoxCollider를 읽어 평면도를 만든다. 지붕·기계·조명은 제외하며 추가 미니맵 카메라나 RenderTexture를 만들지 않는다. 정적 배치는 씬 진입/종료와 지도 활성화 때만 읽고, 위치·방향·수집 상태는 초당 최대 10회 갱신한다. 공장을 로드하지 않은 씬에서는 가짜 위치 대신 `공장 지도 미연결`을 표시한다. 런타임에 구조물을 이동하거나 위 이름 규칙을 바꾸는 기능은 이번 범위에 포함하지 않는다.

양피지 위치는 `ParchmentPickupController`의 실제 Transform과 읽기 전용 `LessonIndex`를 사용한다. 수집 처리는 기존 컴포넌트가 담당하며, 미니맵은 `TabletManager.HasParchment`와 활성 상태만 읽는다. 저장 키·입력 매핑·씬·공용 리그 프리팹은 변경하지 않는다.

2026-10-16 미니맵 변경은 Unity 6000.3.24f1에서 C# 오류 없이 컴파일하고 `FactoryEnvironment` Play에서 작은 지도와 전체 지도 지형·양피지 마커를 확인했다. 자동화 키 입력으로 전체 조작 흐름을 확인하지 못해, 전체 지도는 런타임 Inspector에서 활성화했다. 리그 Z 위치를 -20에서 5로 바꾸어 플레이어 표시와 주변 지도의 갱신을 확인하고 양피지 1을 런타임에 비활성화하여 마커 제거를 확인했다. Play 종료로 이 임시 변경을 폐기했으며 씬을 저장하지 않았다. 실제 수집 입력부터 지도 제거까지의 전체 흐름, 새 지도 버튼의 X/Y 입력 및 실제 Quest 기기는 미확인이다. 단위 테스트·새 테스트 파일·빌드·프로젝트 복사본은 만들거나 실행하지 않았다.

## 양피지 교육 문서

양피지는 직원이 작성한 사내 교육 문서 형식이며, 본문은 비유 없이 개념을 설명한다. 각 문서는 3페이지이고 총 15페이지다. `Assets/Scripts/Core/Tablet/ParchmentLesson.cs`가 제목·담당 부서·본문을 제공하며, 배열 인덱스는 양피지 번호에서 1을 뺀 값이다.

| 양피지 | 주제 | 배치 | 월드 좌표 |
| --- | --- | --- | --- |
| 1 | 이미지와 픽셀: 밝기 값, RGB, 채널 | Convolution Room | `(-8.5, 1.65, 9.15)` |
| 2 | 합성곱과 패딩: 곱셈·합산 예제, 5×5 입력의 출력 크기 | MLP Room | `(6.8, 1.45, 12)` |
| 3 | 필터와 특징 추출: 가중치, 경계 검출, 필터 학습 | MLP Room | `(17.3, 1.45, 12)` |
| 4 | 특징 맵: 위치별 반응, 출력 채널, 표시 비교 | MLP Room | `(6.8, 1.45, 26)` |
| 5 | 활성화 함수와 ReLU: 숫자 변환 규칙, 비선형성 | MLP Room | `(17.3, 1.45, 26)` |

양피지는 `Assets/Textures/Common/LearningParchment.png`를 사용하며, 은은한 주황색 테두리와 점광원이 맥동하고 약하게 떠오르는 동작을 한다. 제목과 본문에는 `Assets/Fonts/NanumPenScript-Regular.ttf`의 Nanum Pen Script를 사용해 만년필 필기 느낌을 낸다. 폰트의 SIL Open Font License는 같은 폴더의 `NanumPenScript-OFL.txt`에 있다.

오른손 레이로 가리킨 뒤 3m 이내에서 Trigger 또는 Grip을 누르면 획득한다. `ParchmentPickupController`는 기존 XRI의 Activate/Select 이벤트를 사용하고, 등록을 마친 양피지는 공간에서 사라진다. 아직 찾지 않은 오른쪽 아이템함 슬롯은 미발견 상태로 표시되며 선택할 수 없다.

획득한 슬롯을 왼손 X로 선택하면 중앙에 양피지가 열린다. 왼손 Trigger를 누른 채 스틱으로 이전·다음·닫기 버튼에 포커스를 옮기고 X로 선택한다. Y로도 문서를 닫을 수 있다. 양피지 4의 3페이지에는 특징 맵 표시를 각각 껐다 켜는 버튼이 있다. 이 버튼은 설명용 화면 표시만 바꾸며 실제 신경망 계산에서 맵을 제외하지 않는다.

획득 상태는 유지되는 공용 리그의 메모리에 보관하므로 씬 이동 후에도 유지된다. 앱을 다시 실행하면 미발견 상태로 초기화된다. 기존 PlayerPrefs 저장 형식에는 획득 상태를 추가하지 않았다.

`Assets/Editor/ParchmentSceneSetup.cs`의 `Tools > Factory > Place Learning Parchments`는 위 위치에 양피지를 배치하고 공용 리그를 연결한다. 기존 방의 GameObject 이름과 에셋 경로를 기준으로 동작한다.

### 이미지와 폰트 출처

양피지 이미지는 사용자 참고 이미지를 바탕으로 내장 ImageGen에서 투명 배경으로 생성했다. 결과는 `client/Assets/Textures/Common/LearningParchment.png`에 저장했다. 글자는 이미지에 포함하지 않고 Unity에서 별도로 표시한다. 사용한 프롬프트는 다음과 같다.

```text
Use case: stylized-concept. Asset type: Unity game UI parchment and collectible sprite. Reference image is a style reference for aged beige parchment and rolled vertical sides. Generate one new blank parchment, straight-on orthographic view, almost square with width:height 0.95:1, fills 95% of canvas with narrow transparent margins. Both left and right edges have narrow rolled paper curls. Slightly ragged top and bottom edges. Warm ivory writing surface, subtle fine paper fibers, restrained worn ochre edges. Broad clean light center covering at least 80% width and 84% height for long Korean text rendered separately by game. No text, no lettering, no seals, no marks, no decorative objects, no glow or external shadows. Transparent background and alpha around paper. Keep surface even and high readability. High quality realistic handpainted game prop texture.
```

Nanum Pen Script는 [Google Fonts 공식 저장소](https://github.com/google/fonts/tree/main/ofl/nanumpenscript)의 폰트이며, 배포 라이선스를 에셋과 함께 포함했다.

## 확인 결과

환경 제작 당시에는 사용자 허가에 따라 Unity 6000.3.24f1에서 장면 배치와 재질·조명을 확인했으며, 재가져오기를 마친 Console의 오류와 경고가 모두 0건이었다.

2026-10-16 양피지 통합에서는 같은 Unity 버전의 배치 `Install` 및 컴파일이 종료 코드 0으로 완료됐다. 에디터 Play에서 XRI Activate 이벤트로 양피지 1을 수집한 뒤 오른쪽 인벤토리에 등록되는 것과, 왼손 X 입력을 시뮬레이션해 중앙 문서가 열리는 것을 확인했다. C# 예외는 없었으나 실행 환경의 FMOD 오디오 출력 장치 초기화가 오류 60으로 실패했고, 무음 소프트웨어 출력으로 실행이 계속됐다.

모든 페이지의 시각적 확인과 실제 Quest 기기 검증은 수행하지 않았다. 단위 테스트, 새 테스트 파일 생성, 빌드, 라이트 베이크 및 Quest 성능 측정도 수행하지 않았다.


## 공장을 유지하는 CNN 체험 연결

`FactoryLearningFlowManager`는 공장 루트에 연결되어 이 공장 세션의 씬 로드·완료 조건·시작 위치를 조율한다. `FactoryFlowPortalController`는 안내와 오른손 Trigger 버튼을 담당한다. 전역 Singleton이나 새로운 입력 시스템은 추가하지 않는다.

- `FactoryIntro` → `FactoryEnvironment` → `CNNPixelLab` → `CNNFeatureMapLab` → `CNNModelDesignLab`.
- 공장과 공용 XR 리그는 유지한다. 공장의 조명·바닥을 사용하며 체험 씬의 독립 실행용 주변 루트와 조명은 통합 실행에서 숨긴다. 다음 체험을 성공적으로 불러온 뒤 이전 체험만 언로드한다.
- 공장 시작 버튼은 `(0, 1.4, -17)`에 있다. 체험은 기존 세 CONTENT ZONE을 기준으로 배치하며 플레이어는 각 기준점에서 Z 방향으로 3m 앞에 선다. 실제 추적 높이는 보존한다.
- 필터는 R 설명·G/B 퀴즈·최종 합을 완료하고 결과 정리까지 진행하면 다음 버튼이 활성화된다. 처음부터 다시 시작하면 완료 상태도 초기화한다.
- 피처맵은 4단계에서 입력 픽셀을 한 번 이상 변경한 뒤 모델 제작으로 이동할 수 있다. 잡은 물체가 있으면 전환하지 않는다.
- 필터 계산값을 피처맵의 데이터로 넘기는 기능은 아니다. 피처맵은 기존 독립적인 8×8 예제와 필터를 사용하며 전환 안내에도 이를 명시한다.
- 태블릿과 수집한 양피지는 기존 persistent rig에 그대로 남는다. 기존 태블릿의 일반 교육 단계 선택 메뉴를 새 씬 이동 버튼으로 바꾸지는 않는다.
- 모델 제작 이후 엔딩 연결, 이전 체험으로 되돌아가 진행 상태 복원, 앱 종료 후 진행 저장은 이번 범위에 포함하지 않는다.
- 전체 연결을 보려면 `FactoryEnvironment`에서 시작한다. 각 체험 씬을 단독으로 열면 기존 단독 체험을 유지한다.

### 씬 연결 Play 확인 (2026-10-16)

사용자 허가로 Unity 6000.3.24f1 에디터에서 컴파일 및 Play를 실행했다. 공장 시작 버튼의 Activate 이벤트, 이미지 확대·픽셀 전환, 입체 필터 정렬·RGB 층 이동의 Released 처리, R 설명의 다음 버튼, G/B 및 최종 합 퀴즈의 숫자 버튼을 순서대로 실행해 합성곱 완료 콜백을 확인했다. 정답 선택은 검증 도구가 제공된 정답에 해당하는 버튼을 호출했으며 독립적인 계산 정확도 검증은 아니다.

피처맵의 완료 상태는 진입 시 false, 4단계 입력 픽셀 변경 후 true였다. 마지막 전환 뒤 공장과 모델 제작 씬이 로드되고 이전 피처맵 씬은 언로드됐으며, 활성 XR 리그는 한 개였다. 에디터 시뮬레이터의 초기 시점이 바닥 높이 0으로 시작하는 문제를 에디터 전용 보정으로 수정했고 높이 1.6m를 확인했다. 안내 글자 크기와 다음 체험 버튼의 위치도 화면 확인 후 조정했다.

제약: 실제 Quest 하드웨어, 레이 조준부터 Trigger/Grip까지의 물리 입력 전체 경로, 모델 학습 서버 연동은 확인하지 않았다. 검증 도구의 통신 시간 초과 오류로 Play가 일시정지되는 경우가 있어 해제하고 진행했다. 따라서 전체 체험에 오류·경고가 없다는 의미의 검증 결과는 아니다.

후속 수정: 피처맵의 `CNNFeatureMapGrabInteractable`은 XRI Awake 전에 명시 콜라이더 목록을 설정한다. Play에서 피처맵을 다시 로드한 뒤 신규 경고 0건, 활성 잡기 대상 133개의 콜라이더 소유권 불일치 0건을 확인했다. 노트북 태블릿은 본체와 Canvas를 함께 왼쪽 아래에 축소 배치하고 조작 중 원래 위치·크기로 복원한다. 이 표시 보정은 에디터 시뮬레이터에만 적용한다.
