# 콘텐츠 에셋 출처 및 저작권

외부 에셋(폰트·아이콘·애니메이션 포함)과 직접 생성한 미디어의 제출용 출처 목록입니다.

| 에셋명 / 종류 | 사용처 | 출처 또는 생성 방법 | 저작권·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| **WraithAttackWarning.wav** / 습격·경고 효과음 | client/Assets/Audio/OpeningEnding/WraithAttackWarning.wav; 오프닝 4.3초부터 경고·충전·타격·화면 고장 | Codex 보조로 Python math/random/wave를 사용해 사인파·잡음·엔벌로프를 직접 합성. seed 30102026, 9초, 스테레오 32kHz/16-bit PCM. 외부 녹음·샘플 없음 | 프로젝트 내부 제작. 법적 권리자 미확인, 별도 공개 라이선스 지정 없음 | 신규 합성, 정규화 피크 0.63 |
| **FactoryDoorOpen.wav** / 공장문 효과음 | client/Assets/Audio/OpeningEnding/FactoryDoorOpen.wav; 오프닝 문 열기 | Codex 보조로 Python math/random/wave의 저음·잡음·마찰·충격 엔벌로프를 직접 합성. seed 30102026, 3.2초, 스테레오 32kHz/16-bit PCM. 외부 녹음·샘플 없음 | 프로젝트 내부 제작. 법적 권리자 미확인, 별도 공개 라이선스 지정 없음 | 신규 합성, 정규화 피크 0.63 |
| **InputPixels.png** / 입력 이미지 | 이전 경로: `client/Assets/Textures/CNNFeatureMapLab/InputPixels.png`; 2026-11-09 미사용 정리로 삭제, 현재 제출 대상 제외 | 기존 프로젝트 에셋. 원출처 URL·생성 도구·제작 방법·AI 보조 여부 미확인 | 제작자·권리자·라이선스 미확인 | Codex 보조로 이전 입력 맵·재질과 함께 삭제. 과거 출처 기록 보존 |
| **OpeningBroadcast.png** / 오프닝 방송 정지 이미지 | `client/Assets/Textures/CNNScreenLab/OpeningBroadcast.png`; CNNScreenLab 원본 이미지·픽셀 및 RGB 계산 입력; FactoryEnding의 합성곱·특징 맵 예시용 16×9 밝기 샘플 원본 | 프로젝트의 `FactoryIntro/VillageDisplay` 셰이더를 Unity에서 뉴스 방송(_Channel=0), 재생 3초, 손상·간섭 0으로 렌더링. Codex 보조로 1024×576 PNG 추출 | 프로젝트 내부 절차적 방송 그래픽에서 제작. 외부 방송 영상·로고 사용 없음. 별도 공개 라이선스 지정 없음 | 원본 PNG 수정 없음. 픽셀 체험은 저해상도·블러 처리 후 동일 이미지에서 RGB 샘플링. 엔딩은 Codex 보조로 기존 PNG의 셀 중심 밝기를 추출해 코드의 수치 예시로 사용하며 파일을 복제하지 않음. 관찰은 원래 0~255, 계산 연습은 Codex 보조 코드로 0~3에 축소해 표시 |
| **Noto Sans CJK KR Regular 2.004** / 폰트 | 시작 화면, 자막, UI, FactoryEnvironment의 정문·후문 및 튜토리얼·ROOM #1~#3 셔터 위 간판·직원 휴게실 간판·출입 금지 표지·표본 관측 콘솔 설명, CNNModelDesignLab 제작 콘솔의 저장된 구성 검사·학습·예측 UI, FactoryEnding의 큰 버튼·주민 대화창 및 `Prefabs/OpeningEnding/Ending/EndingUpscalingProcess.prefab` 단계 제목·계산식, CNNScreenLab 안내·RGB 숫자 및 CNNConvolutionLab 전용 PixelCalculationUI의 R·G 학습/B 퀴즈 숫자 키패드·RGB 진행 카드와 ConvolutionWorkbench 고정 거치 명판·필터 배치 안내(굵은 스타일로 재사용) | [Noto CJK 공식 배포본](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) | © 2014–2021 Adobe. [SIL Open Font License 1.1](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/LICENSE) | 폰트 원본 수정 없음 |
| **Meta Quest Touch Plus 컨트롤러 모델·텍스처·재질·애니메이션 (Meta XR Core SDK 207.0.1)** | 공용 XRPlayerRig, 오프닝 입장 안내, FactoryTutorial 확대 안내 | [Meta XR Core 207.0.1](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.core)의 `OVRControllerPrefab` 직접 참조. [공식 애니메이션 안내](https://developers.meta.com/vr/documentation/unity/controller-animations/) | Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved. [Meta Platform Technologies SDK License Agreement](https://developers.meta.com/horizon/licenses/oculussdk/) | 패키지 원본 수정 없음. Codex 보조로 프리팹 연결 및 프로젝트의 버튼 강조를 적용함 |
| **Nanum Pen Script** / 폰트 | 기존 양피지 서체 에셋. 현재 양피지 화면에서는 사용하지 않음 | [Google Fonts 공식 자료](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/METADATA.pb) | © 2010 NHN Corporation. 디자인: Sandoll Communications Inc. [SIL Open Font License 1.1](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt) | 폰트 원본 수정 없음 |
| **Pretendard Regular / SemiBold** / 폰트 | FactoryEnvironment의 양피지 수집물 5개, 공용 ParchmentReader·TabletUI 프리팹, CNNRealityLab의 선택 안내·인식 패널·사용 안내 팝업, 피처맵 복도의 한글 자막·라벨 | [Pretendard 공식 저장소](https://github.com/orioncactus/pretendard) | © 2021 Kil Hyung-jin 및 원저작자. 동봉한 [SIL Open Font License 1.1](../client/Assets/Fonts/Pretendard/LICENSE.txt) | 공식 OTF 원본 수정 없음 |
| **Lucide / Feather 기반 아이콘** / 아이콘 | 태블릿 UI | [Lucide 공식 저장소 및 라이선스](https://github.com/lucide-icons/lucide/blob/e715245d62667c800e7f54c94b1b023692e900a3/LICENSE) | © 2026 Lucide Icons and Contributors, ISC. Feather 유래 도형: © 2013–present Cole Bemis, MIT | 선 도형을 간략화하고 아이콘 아틀라스로 변환 |
| **Universal Animation Library — Standard** / 주민 동작 | FactoryIntro·FactoryEnding 마을 주민의 걷기·시청·대화·충격 반응·축하; 엔딩 감사 인사의 접근·대화·측면 이동 | [Quaternius 공식 배포 페이지](https://quaternius.itch.io/universal-animation-library), 무료 Standard 배포본 확인·다운로드: 2026-10-23 | 동봉 License/README의 제작자 표기: **Quaternius**. 공식 페이지에 애니메이터 **Gonzalo Furnier**의 제작 참여 표기. [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/): 수정·재배포·상업적 이용 허용 | 실제 관절 애니메이션을 Codex 보조로 기존 주민의 비율·관절에 맞게 변환하고 상태 전환을 혼합. 외부 캐릭터 메시·유료 Pro/Source 파일 사용 없음 |
| **VillageOpening.wav** / 음원 | 18초 마을 오프닝의 배경·효과음 | 프로젝트 작업에서 Codex 보조로 Python 사인파·필터링한 난수·음량 곡선을 직접 합성 | 프로젝트용 직접 제작 음원. 별도 공개 라이선스는 지정하지 않음. 외부 녹음·음악 샘플 사용 없음 | 오프닝의 공격·화면 고장 시점에 맞춰 제작 |
| **LearningParchment.png** / 생성 이미지 | FactoryEnvironment의 양피지 수집물 5개와 공장·태블릿 공용 ParchmentReader 프리팹 배경 | 사용자가 선택한 폐공장 홀로그램 양피지 시안을 ChatGPT 이미지 생성 도구로 글자 없는 투명 스프라이트로 편집 | 프로젝트용 생성 이미지. 외부 이미지 사용 없음. 제출 시 AI 생성물 고지 요건 확인 | 기존 종이 이미지를 교체하고 Unity 텍스트를 별도 겹침. Codex 보조로 기존 이미지의 양끝·중앙을 런타임 표시 조각으로 나눠 말린 형태와 펼침 연출에 재사용하며 원본 PNG 수정·복제 없음 |
| **ColorPhoto.png** / 생성 이미지 | 과거 시안용 이미지. 현재 씬·코드 참조가 없어 2026-11-01 프로젝트에서 삭제; 제출 사용 목록에서 제외 | 파일 내 생성 메타데이터에 **ChatGPT / gpt-image** 표기 | 생성 계정·원프롬프트·권리자·재배포 조건 미확인 | 이미지 내용 수정 없이 미사용 파일 삭제 |
| **MNIST 숫자 이미지·정답** / 데이터 및 표시 이미지 | 기존 `Textures/CNNModelDesignLab/mnist-test-*.png` (보존, 현재 모델 제작 콘솔에서 미사용). 이전 흑백 런타임 데이터는 아래 컬러 데이터로 교체 | [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [Keras 공식 배포·권리 표기](https://keras.io/api/datasets/mnist/), [Keras 공식 로더](https://github.com/keras-team/keras/blob/v3.15.1/keras/src/datasets/mnist.py); 배포 URL: [mnist.npz](https://storage.googleapis.com/tensorflow/tf-keras-datasets/mnist.npz). 확인: 2026-10-24 | Keras 공식 문서 표기: **Yann LeCun, Corinna Cortes**, 원본 NIST 데이터의 파생 데이터. [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). 이 프로젝트의 MNIST 부분집합 데이터도 같은 조건으로 제공 | Codex 보조로 원본 training에서 숫자별 첫 50장(500장), test에서 숫자별 첫 20장(200장)을 추출하고 원본 인덱스·정답·픽셀을 `.bytes`로 포장. 생성·합성 이미지 아님. 픽셀 수정 없음. 기존 표시 PNG는 원본 test에서 숫자별 첫 이미지이며 이번 작업에서 변경 없음 |
| **MNIST 컬러 통합 데이터 600장·미리보기** | `mnist-color-data/images.npy`, `labels.npy`, `source_indices.npy`, `style_seeds.npy`, `MnistColor.bytes`, `MnistTraining.bytes`, `MnistTest.bytes`, `preview.png` | [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [CVDF 공식 배포](https://github.com/cvdfoundation/mnist), [권리 근거](https://keras.io/api/datasets/mnist/). 2026-11-07 확인 | Keras 표기 권리자 Yann LeCun, Corinna Cortes. 원본 및 컬러 파생 데이터·미리보기 [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). 합성 부분 별도 권리자 미확인 | Codex 보조 Python/NumPy/Pillow 제작. 원본 0~3 서로 다른 손글씨 각 150장에 RGB 색·점무늬 합성, 정답·형태 보존. 생성형 이미지 AI 미사용. 통합 600장을 training 480장·test 120장으로 분리한 RGB CMNT v2 파일도 제공. `client/Assets/Data/CNNModelDesignLab/MnistTraining.bytes`, `MnistTest.bytes`로 런타임 연결. 모델 제작 콘솔의 평가 120장 미리보기는 동일 RGB 픽셀에서 실행 중 생성하며 픽셀 내용 변경 없음. 추출 URL·시드·방법은 전용 폴더 README 참조 |

공용 리그와 안내는 Meta XR Core SDK의 공식 프리팹을 직접 참조합니다. Codex 보조로 생성 인스턴스의 Touch Plus 본체 재질을 같은 SDK의 `MetaQuestTouchPlusMATphong_left.mat` / `MetaQuestTouchPlusMATphong_right.mat` 공유 참조로 연결했습니다. 모델·텍스처·재질·애니메이션 패키지 원본 수정이나 복제는 없습니다. 이전 추출본과 그 라이선스 사본 폴더는 삭제했습니다. 현재 사용하는 SDK 패키지의 `LICENSE.md`와 `NOTICE.txt` 원본은 유지합니다.

Universal Animation Library에서 사용한 동작은 `Idle_Loop`, `Idle_Talking_Loop`, `Walk_Loop`, `Hit_Chest`, `Hit_Head`, `Crouch_Idle_Loop`, `Dance_Loop`입니다. 무료판에 전용 놀람 동작이 없어 충격·웅크림 동작을 조합했습니다. 관절 곡선을 변환한 런타임 데이터는 `client/Assets/Meshes/OpeningEnding/QuaterniusVillageMotion.asset`에 두며, 제출용 사본·ZIP·별도 라이선스 보관 폴더는 만들지 않습니다.

폰트와 아이콘의 원래 저작권·라이선스 고지는 유지합니다. 생성 도구의 표기는 저작권자를 뜻하지 않으며, 생성 이미지의 메타데이터만으로 권리 귀속이나 재배포 허가를 확인한 것은 아닙니다.

## 제출 전 저작권 재검토 목록

### 태블릿 앱 아이콘 (2026-10-30)

| 에셋명 | 사용 위치 | 생성 방법·프롬프트 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| TabletMapIcon.png | `client/Assets/Textures/Common/TabletMapIcon.png`; 이전 시안, 현재 태블릿에서 미사용 | OpenAI 내장 image_gen, Codex AI 보조 생성. 둥근 사각형 파란 타일, 접힌 지도·경로·위치 핀, 부드러운 3D 앱 아이콘, 글자 없음, 타일 외부 투명 | 생성 도구는 권리자를 뜻하지 않음. 권리자 미확인, 별도 공개 라이선스 지정 없음 | 생성 원본 그대로 저장 |
| TabletStepsIcon.png | `client/Assets/Textures/Common/TabletStepsIcon.png`; 이전 시안, 현재 태블릿에서 미사용 | OpenAI 내장 image_gen, Codex AI 보조 생성. 보라색 둥근 사각형 타일, 상승하는 세 계단·민트 체크 표시, 부드러운 3D 앱 아이콘, 글자 없음, 타일 외부 투명 | 권리자 미확인, 별도 공개 라이선스 지정 없음 | 생성 원본 그대로 저장 |
| TabletScrollIcon.png | `client/Assets/Textures/Common/TabletScrollIcon.png`; 이전 시안, 현재 태블릿에서 미사용 | OpenAI 내장 image_gen, Codex AI 보조 생성. 주황색 둥근 사각형 타일, 펼친 양피지·갈색 선 세 개·붉은 봉인, 부드러운 3D 앱 아이콘, 글자 없음, 타일 외부 투명 | 권리자 미확인, 별도 공개 라이선스 지정 없음 | 생성 원본 그대로 저장 |

이 구역은 확인된 근거와 남은 확인 사항을 구분한 제출 준비 기록이며, 모든 에셋의 권리 문제가 해소됐다는 보증은 아니다.

| 대상 | 확인한 근거·허용 범위 | 제출 전 남은 확인 |
| --- | --- | --- |
| Meta Touch Plus 모델·텍스처·애니메이션 | Meta XR Core SDK 207.0.1 패키지 원본을 참조하고 기존 라이선스·고지를 보존했다. | 최종 APK·소스 배포물에 필요한 고지가 포함되는지 확인. |
| LearningParchment.png / ColorPhoto.png | 양피지 이미지는 사용자가 승인한 시안에서 글자를 제거한 생성 이미지로 교체. ColorPhoto.png는 기존 생성 도구 표기만 확인됨. | 대회 AI 생성물 고지 요구 확인. ColorPhoto.png는 미사용으로 삭제되어 현재 제출 대상에서 제외. 과거 출처 미확인 기록은 보존. |
| Noto / Nanum / Lucide·Feather | 기존 표의 OFL/ISC/MIT 근거와 프로젝트 고지를 보존. 이번 튜토리얼·수집 안내에서도 기존 폰트를 재사용. | 최종 제출 파일에서 각 고지가 누락되지 않는지 확인. |
| 기타 공장 배경·음원·주민 애니메이션·MNIST | 위 표의 기록을 유지. | 제출 범위에 포함되는 모든 외부 에셋의 누락 여부와 실제 배포 고지 확인. 이번 컨트롤러 검토만으로 프로젝트 전체의 권리 확인이 완료된 것은 아님. |

Meta 확인 자료는 [SDK 약관](https://developers.meta.com/vr/licenses/oculussdk/)과 [컨트롤러 애니메이션 안내](https://developers.meta.com/vr/documentation/unity/controller-animations/)입니다.

## 양피지 3 RGB 입력 (2026-10-30)

| 에셋·사용 위치 | 원출처·제작 방법 | 제작·권리 및 수정 |
| --- | --- | --- |
| 이전 경로: `client/Assets/Textures/CNNFeatureMapLab/FilterComparisonInput.png` · 양피지 3의 RGB 분리 입력·필터 비교. 2026-11-09 미사용 정리로 삭제, 현재 제출 대상 제외 | 사용자의 색 블록 배치 구상을 참고해 Codex 보조로 Python/Pillow에서 RGB 숫자 배열을 직접 작성한 6×6 PNG. 외부 사진이나 첨부 이미지의 픽셀을 복사하지 않음 | 프로젝트용 직접 합성 이미지. AI/Codex 보조 제작. 별도 공개 라이선스는 지정하지 않음. 픽셀 간격 없이 제작 |

병합 전 양피지 4 수집물은 기존 `LearningParchment.png`와 기존 Pretendard 폰트를 재사용했다. 해당 출처·고지는 위 기록을 따른다. 현재 피처맵 내부의 중복 수집물은 제거했으며 공장 공용 양피지에서 기존 에셋을 계속 사용한다.

## 양피지 4 컬러 입력 (2026-10-30)

이전 경로 `client/Assets/Textures/CNNFeatureMapLab/FeatureMapColorInput.png`는 위의 직접 제작 `FilterComparisonInput.png`를 Codex 보조로 Python/Pillow의 최근접 보간으로 8×8로 변환한 파생 입력이었다. 외부 이미지 사용 없음. RGB 계산과 피쳐맵 표시용이며 별도 공개 라이선스는 지정하지 않았다. 2026-11-09 Codex 보조로 미사용 입력 텍스처를 삭제해 현재 제출 대상에서 제외했으며, 과거 출처 기록은 보존한다. 기존 계산 창이 재사용하던 Noto Sans CJK KR 폰트는 다른 기능에서도 사용하므로 유지한다.
2026-10-30 폴더 통합: Codex 보조로 주민 동작 데이터를 위 OpeningEnding 경로로, 합성 효과음을 client/Assets/Audio/OpeningEnding/VillageOpening.wav로 이동했다. 미디어 내용과 기존 라이선스는 변경하지 않았다.
# 오프닝 제목용 주아체 (2026-10-30)

- 에셋: Jua-Regular.ttf. 사용 위치: `client/Assets/Fonts/Jua/`, FactoryIntro 제목과 GAME START, VillageOpening 프리팹의 CNN FACTORY 간판.
- 원출처: https://github.com/google/fonts/tree/main/ofl/jua
- 저작권 표기: Copyright 2018 The Jua Project Authors.
- 라이선스: SIL Open Font License 1.1. 원본 폰트 수정 없음. 앱 포함 배포에 필요한 원본 `OFL.txt`를 폰트와 함께 보존한다.

## 시작 화면 음악 — Cipher

- 에셋: `client/Assets/Audio/OpeningEnding/Cipher.mp3` (원본 파일명 Cipher2.mp3, ISRC USUAN1100844).
- 제작자·저작권자: Kevin MacLeod. 공식 출처: https://incompetech.com/music/royalty-free/music.html 및 https://incompetech.com/music/royalty-free/mp3-royaltyfree/Cipher2.mp3
- 공식 카탈로그의 Attribution Code에서 CC BY 4.0 확인: https://creativecommons.org/licenses/by/4.0/
- 사용: 오프닝 타이틀 반복 재생. 원본 파일 편집 없음. 런타임에서 볼륨 조절 및 1.5초 크로스페이드 적용. 시작 화면의 음악 크레딧 표시는 제거했다.
- 확인일: 2026-10-30. 공식 `pieces.json`의 제목 Cipher·원본 파일명 Cipher2.mp3·ISRC를 대조하고, 곡 상세의 Attribution Code가 CC BY 4.0임을 확인했다. 구버전 미러의 다른 라이선스 표기보다 이번 공식 다운로드 페이지의 표기를 기준으로 기록했다.
- 배포 시 유지할 고지: `Cipher — Kevin MacLeod (incompetech.com). Licensed under Creative Commons: By Attribution 4.0 License. https://creativecommons.org/licenses/by/4.0/` 원곡에 대한 보증·제작자 후원을 암시하지 않는다. 원본 파일은 변형하지 않았으며 앱 내 반복·볼륨·크로스페이드만 적용했다.
- CC BY 4.0에 따라 출처·저작자·라이선스 링크를 유지하고, 추후 음원 편집 시 변경 사실을 추가한다. 시작 화면에서 제거한 크레딧의 원문은 위 배포 고지에 보존한다. 별도 저작권 폴더·원본 사본은 생성하지 않는다.

### 태블릿 뒷면 로고 (2026-10-31)

| 에셋명 | 사용 위치 | 원출처·제공 파일 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| KD HUNTERS 로고 | `client/Assets/Textures/Common/KDHuntersLogo.png`; XRPlayerRig의 Tablet Rear Logo | 사용자가 지정한 `C:/Users/andre/Downloads/KakaoTalk_261031_001735.png`. 원출처 URL 미확인 | 제작자·저작권자·라이선스 미확인. 파일 제공자를 권리자로 추정하지 않음 | 이미지 내용 수정 없음. Codex 보조로 공용 셰이더·재질의 곱셈 혼합을 적용하여 흰 배경이 본체 색을 유지하도록 표시 |

미사용 정리(2026-11-01): GrayscalePhoto.png는 씬·코드 참조가 없어 삭제했다. 원출처·권리자·라이선스는 미확인이다. OpeningBroadcast.png는 내용과 GUID를 유지해 CNNScreenLab 텍스처 폴더로 이동했다.

## 피처맵 복도

- **JetBrains Mono Regular v2.304**. 피처맵 복도의 필터 가중치·출력 퍼센트 라벨에 사용한다. 공식 TTF를 수정 없이 사용했다. © 2020 The JetBrains Mono Project Authors. [원본 릴리스](https://github.com/JetBrains/JetBrainsMono/releases/tag/v2.304), [동봉한 SIL OFL 1.1](../client/Assets/Fonts/JetBrainsMono/OFL.txt).
- **16×16 컬러 숫자 입력·피처맵·출력 템플릿·격자·둥근 UI 텍스처**. `client/Assets/Textures/CNNFeatureMapLab/`의 Unity `.asset` 파일과 `FeatureMapCorridor.prefab`에서 사용한다. 숫자 패턴, 팔레트, 고정 시드와 더미 CNN 계산, 코드의 격자·라운드 사각형 수식으로 생성하던 결과를 2026-11-09 Codex 보조로 Unity 에디터에서 저장했다. 입력·맵은 실행 중 계산값에 따라 표시용 복사본을 갱신한다. 외부 이미지 파일 사용 없음. 제작자·법적 권리자 미확인, 별도 공개 라이선스 지정 없음.
- **복도 TMP 폰트 아틀라스**. `client/Assets/Fonts/CNNFeatureMapLab/Pretendard-Regular SDF.asset`, `JetBrainsMono-Regular SDF.asset`과 각 파일에 포함된 아틀라스에 사용한다. 기존 Pretendard Regular OTF 및 JetBrains Mono Regular TTF를 Unity TextMesh Pro의 `CreateFontAsset`으로 변환하고 Codex 보조로 저장했다. 원본 폰트 파일 수정 없음. 제작자·저작권자·SIL OFL 1.1 및 원출처는 위 Pretendard·JetBrains Mono 기록을 따르며, 기존 폰트 폴더의 라이선스 고지를 유지한다.

## ROOM 1-2 연속 체험 폰트 재사용 (2026-11-08)

- 에셋: 기존 `client/Assets/Fonts/NotoSansCJKkr-Regular.otf`.
- 사용 위치: `ConvolutionWorkbench`의 5×5 입력·3×3 결과와 두 계산판, `PixelCalculationUI`의 고정 3×3 요약, `FeatureInspectionDesk`의 명판·안내·완료 표시, 두 체험의 공용 `WorldUIButton` 인스턴스 라벨.
- 원출처·권리자·라이선스: 위 **Noto Sans CJK KR Regular 2.004** 기록의 공식 배포본과 © 2014–2021 Adobe, SIL OFL 1.1을 그대로 따른다. 기존 폰트 파일과 배포 라이선스 고지를 보존한다.
- 수정: 폰트 원본 수정·복제 없음. Codex AI 보조로 씬 전용 프리팹의 글자 배치·크기·색과 참조를 추가·수정했다.
