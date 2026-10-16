# 폐공장 환경 씬

`client/Assets/Scenes/FactoryEnvironment.unity`는 공장 환경과 수집형 교육 문서 5개를 담은 씬이다. 기존 공용 `XRPlayerRig` 프리팹 인스턴스를 포함하므로 단독 Play에서 양피지 수집과 태블릿 열람을 사용할 수 있다. 기존 `FactoryIntro`, `QuestMain`, `FactoryEnding` 및 교육 씬과의 전환·학습 진행 연결은 후속 통합 작업에서 결정한다. Unity 1 unit은 1 m로 사용한다.

## 배치와 동선

```text
오프닝·튜토리얼 진입 구역 (-Z, 기존 정문·하역장/보안 구역)
  → 중앙 복도
      ├─ Convolution Room · 서쪽 아래 (기존 원본 검사실, x -19~-3, z -1~12)
      ├─ CNN Room · 서쪽 위          (기존 특징 추출실, x -19~-3, z 14~28)
      ├─ MLP Room · 동쪽             (기존 모델 조립실, x 3~21, z 10~28)
      └─ 최종 마무리 구역 · 오른쪽 위 (기존 출하 검사 구역, x 3~21, z 29~40)
```

오프닝에서 공장으로 들어오는 위치는 `(0, 0, -20)`, 출하 구역에서 엔딩으로 이어질 위치는 `(12.5, 0, 40)`이다. 중앙 복도와 각 실 사이에는 약 3.4 m의 열린 출입구를 두었다. 실제 전환·잠금·진행 조건은 구현하지 않았다.

## 씬 계층

`Factory Environment` 루트 아래에 `00` 정문/하역장, `01` 보안 구역, `02` 중앙 복도, `03` 원본 검사실, `04` 특징 추출실, `05` 모델 조립실, `06` 출하 구역, `07` 조명, `08` 콘텐츠 연결 기준점이 있다. 이 기존 GameObject 이름과 배치는 유지하며, 공간을 설명할 때는 위의 합의된 방 이름을 사용한다. 양피지는 `03 Original image inspection room`과 `05 Model assembly room` 아래의 `Learning Parchments`에 배치한다. 팀원이 작업할 때는 해당 실의 `CONTENT ZONE` 빈 Transform을 기준으로 교육 장치를 배치할 수 있다. 중심부의 대략 6 × 6 m는 콘텐츠용으로 비워 두고 큰 고정 설비는 가장자리로 배치했다.

씬 전용 URP 재질은 `Assets/Materials/FactoryEnvironment/`, 표면 이미지와 노멀·금속/매끄러움 맵은 `Assets/Textures/FactoryEnvironment/`에 있다. 마모된 콘크리트, 벗겨진 도장 강판, 산화된 철, 고무, 주의 도색, 철제 그레이팅을 사용한다. 에셋의 `.meta`를 이동·재생성하지 않아야 씬 참조가 유지된다.

## 조명과 편집

고정된 등기구 표현, 8개의 그림자 없는 점광원, 무너진 지붕 틈으로 들어오는 차가운 방향광을 배치했다. 방향광은 부드러운 실시간 그림자를 만든다. 구조물은 정적 배치로 표시했으며, 라이트맵은 아직 굽지 않았다. 상부 철판과 트러스는 일부 무너진 지붕 형태로 만들었다. Scene View에서 실 내부를 편집할 때 해당 공간의 지붕 오브젝트를 임시로 숨기면 된다.

주요 바닥과 벽 외에 보안 설비, 실별 고정 기계·보관함, 조립실 부품 랙, 출하 컨베이어와 상자에도 Box Collider를 두었다. 출하 컨베이어와 상자는 동쪽 벽 쪽에 배치해 `(12.5, 0, 40)` 출구 앞 통로를 비웠다. 특징 추출실의 관찰창은 반투명 재질이다.

씬의 카메라·입력·태블릿은 `Assets/Prefabs/XR/XRPlayerRig.prefab`의 일반 씬 인스턴스를 사용한다. 기존 리그가 있는 상태에서 이 씬을 로드하면 `PersistentXRPlayerRig`가 중복 리그를 제거하므로 기존 태블릿과 획득 상태가 유지된다. 양피지 추가 작업에서 빌드 씬 목록과 씬 전환은 변경하지 않았다.

## 양피지 교육 문서

양피지는 직원이 작성한 사내 교육 문서 형식이며, 본문은 비유 없이 개념을 설명한다. 각 문서는 3페이지이고 총 15페이지다. `Assets/Scripts/Core/ParchmentLesson.cs`가 제목·담당 부서·본문을 제공하며, 배열 인덱스는 양피지 번호에서 1을 뺀 값이다.

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
