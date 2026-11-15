# 콘텐츠 에셋 출처 및 저작권

외부 에셋(폰트·아이콘·애니메이션 포함)과 직접 생성한 미디어의 제출용 출처 목록입니다.

| 에셋명 / 종류 | 사용처 | 출처 또는 생성 방법 | 저작권·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| **WraithAttackWarning.wav** / 습격·경고 효과음 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 client/Assets/Audio/OpeningEnding/WraithAttackWarning.wav; 오프닝 4.3초부터 경고·충전·타격·화면 고장 | Codex 보조로 Python math/random/wave를 사용해 사인파·잡음·엔벌로프를 직접 합성. seed 30102026, 9초, 스테레오 32kHz/16-bit PCM. 외부 녹음·샘플 없음 | 프로젝트 내부 제작. 법적 권리자 미확인, 별도 공개 라이선스 지정 없음 | 신규 합성, 정규화 피크 0.63 |
| **FactoryDoorOpen.wav** / 공장문 효과음 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 client/Assets/Audio/OpeningEnding/FactoryDoorOpen.wav; 오프닝 문 열기 | Codex 보조로 Python math/random/wave의 저음·잡음·마찰·충격 엔벌로프를 직접 합성. seed 30102026, 3.2초, 스테레오 32kHz/16-bit PCM. 외부 녹음·샘플 없음 | 프로젝트 내부 제작. 법적 권리자 미확인, 별도 공개 라이선스 지정 없음 | 신규 합성, 정규화 피크 0.63 |
| **InputPixels.png** / 입력 이미지 | 이전 경로: `client/Assets/Textures/CNNFeatureMapLab/InputPixels.png`; 2026-11-09 미사용 정리로 삭제, 현재 제출 대상 제외 | 기존 프로젝트 에셋. 원출처 URL·생성 도구·제작 방법·AI 보조 여부 미확인 | 제작자·권리자·라이선스 미확인 | Codex 보조로 이전 입력 맵·재질과 함께 삭제. 과거 출처 기록 보존 |
| **OpeningBroadcast.png** / 오프닝 방송 정지 이미지 | `client/Assets/Textures/CNNScreenLab/OpeningBroadcast.png`; 이전 CNNScreenLab 원본 이미지 | 프로젝트의 `FactoryIntro/VillageDisplay` 셰이더를 Unity에서 뉴스 방송(_Channel=0), 재생 3초, 손상·간섭 0으로 렌더링. Codex 보조로 1024×576 PNG 추출 | 프로젝트 내부 절차적 방송 그래픽에서 제작. 외부 방송 영상·로고 사용 없음. 별도 공개 라이선스 지정 없음 | 원본 PNG 수정 없음. 픽셀 체험은 저해상도·블러 처리 후 동일 이미지에서 RGB 샘플링. 관찰은 원래 0~255, 계산 연습은 Codex 보조 코드로 0~3에 축소해 표시 |
| **ColorDotPlate2.png** / 숫자 2 색점판 | `client/Assets/Textures/CNNScreenLab/ColorDotPlate2.png` · ROOM #1-1 픽셀 관찰 화면 및 RGB 샘플링 입력, ROOM #1-2 합성곱 체험의 입력 그림(`ConvolutionWorkbench.prefab`에 원본 연결, 플레이 시 샘플링) | OpenAI ImageGen으로 숫자 2 색점판을 생성하고 청록·초록 점이 직사각형을 채우도록 편집 | 외부 이미지·로고 사용 없음. 제작물 권리자·공개 라이선스 미확인 | Codex 보조 생성·편집. 주황·빨강 숫자와 청록·초록 배경으로 구성 |
| **Noto Sans CJK KR Regular 2.004** / 폰트 | 시작 화면, 자막, UI, FactoryEnvironment의 정문·후문 및 튜토리얼·ROOM #1~#3 셔터 위 간판·직원 휴게실 간판·출입 금지 표지·표본 관측 콘솔 설명, CNNModelDesignLab 제작 콘솔의 구성 검사·학습·예측 UI, 바닥 슬롯 표기, 공급 책상 앞 명찰(굵은 스타일로 재사용) 및 학습 흐름 라벨, CNNScreenLab 안내·RGB 숫자 및 CNNConvolutionLab 전용 PixelCalculationUI의 R·G 학습/B 퀴즈 숫자 키패드·RGB 진행 카드와 ConvolutionWorkbench 고정 거치 명판·필터 배치 안내(굵은 스타일로 재사용) | [Noto CJK 공식 배포본](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) | © 2014–2021 Adobe. [SIL Open Font License 1.1](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/LICENSE) | 폰트 원본 수정 없음 |
| **Meta Quest Touch Plus 컨트롤러 모델·텍스처·재질·애니메이션 (Meta XR Core SDK 207.0.1)** | 공용 XRPlayerRig, FactoryTutorial 확대 안내 | [Meta XR Core 207.0.1](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.core)의 `OVRControllerPrefab` 직접 참조. [공식 애니메이션 안내](https://developers.meta.com/vr/documentation/unity/controller-animations/) | Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved. [Meta Platform Technologies SDK License Agreement](https://developers.meta.com/horizon/licenses/oculussdk/) | 패키지 원본 수정 없음. Codex 보조로 프리팹 연결 및 프로젝트의 버튼 강조를 적용함 |
| **Nanum Pen Script** / 폰트 | 기존 양피지 서체 에셋. 현재 양피지 화면에서는 사용하지 않음 | [Google Fonts 공식 자료](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/METADATA.pb) | © 2010 NHN Corporation. 디자인: Sandoll Communications Inc. [SIL Open Font License 1.1](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt) | 폰트 원본 수정 없음 |
| **Pretendard Regular / SemiBold** / 폰트 | FactoryEnvironment의 양피지 수집물 2개, 공용 ParchmentReader·TabletUI 프리팹, CNNRealityLab의 선택 안내·인식 패널·사용 안내 팝업·촬영/인식/복귀 버튼, 피처맵 복도의 한글 자막·라벨, CNNConvolutionLab 자막·버튼·계산 숫자(Regular), CNNArchiveOpening의 공장 입장 안내 간판과 오프닝·엔딩의 시작·재시작 버튼 문구 및 ▶ 아이콘(SemiBold) | [Pretendard 공식 저장소](https://github.com/orioncactus/pretendard) | © 2021 Kil Hyung-jin 및 원저작자. 동봉한 [SIL Open Font License 1.1](../client/Assets/Fonts/Pretendard/LICENSE.txt) | 공식 OTF 원본 수정 없음 |
| **Lucide / Feather 기반 아이콘** / 아이콘 | 태블릿 UI | [Lucide 공식 저장소 및 라이선스](https://github.com/lucide-icons/lucide/blob/e715245d62667c800e7f54c94b1b023692e900a3/LICENSE) | © 2026 Lucide Icons and Contributors, ISC. Feather 유래 도형: © 2013–present Cole Bemis, MIT | 선 도형을 간략화하고 아이콘 아틀라스로 변환 |
| **Universal Animation Library — Standard** / 주민 동작 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 FactoryIntro·FactoryEnding 마을 주민의 걷기·시청·대화·충격 반응·축하; 엔딩 감사 인사의 접근·대화·측면 이동 | [Quaternius 공식 배포 페이지](https://quaternius.itch.io/universal-animation-library), 무료 Standard 배포본 확인·다운로드: 2026-10-23 | 동봉 License/README의 제작자 표기: **Quaternius**. 공식 페이지에 애니메이터 **Gonzalo Furnier**의 제작 참여 표기. [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/): 수정·재배포·상업적 이용 허용 | 실제 관절 애니메이션을 Codex 보조로 기존 주민의 비율·관절에 맞게 변환하고 상태 전환을 혼합. 외부 캐릭터 메시·유료 Pro/Source 파일 사용 없음 |
| **VillageOpening.wav** / 음원 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 18초 마을 오프닝의 배경·효과음 | 프로젝트 작업에서 Codex 보조로 Python 사인파·필터링한 난수·음량 곡선을 직접 합성 | 프로젝트용 직접 제작 음원. 별도 공개 라이선스는 지정하지 않음. 외부 녹음·음악 샘플 사용 없음 | 오프닝의 공격·화면 고장 시점에 맞춰 제작 |
| **LearningParchment.png** / 생성 이미지 | FactoryEnvironment의 양피지 수집물 2개와 공장·태블릿 공용 ParchmentReader 프리팹 배경 | 사용자가 선택한 폐공장 홀로그램 양피지 시안을 ChatGPT 이미지 생성 도구로 글자 없는 투명 스프라이트로 편집 | 프로젝트용 생성 이미지. 외부 이미지 사용 없음. 제출 시 AI 생성물 고지 요건 확인 | 기존 종이 이미지를 교체하고 Unity 텍스트를 별도 겹침. Codex 보조로 기존 이미지의 양끝·중앙을 런타임 표시 조각으로 나눠 말린 형태와 펼침 연출에 재사용하며 원본 PNG 수정·복제 없음 |
| **ColorPhoto.png** / 생성 이미지 | 과거 시안용 이미지. 현재 씬·코드 참조가 없어 2026-11-01 프로젝트에서 삭제; 제출 사용 목록에서 제외 | 파일 내 생성 메타데이터에 **ChatGPT / gpt-image** 표기 | 생성 계정·원프롬프트·권리자·재배포 조건 미확인 | 이미지 내용 수정 없이 미사용 파일 삭제 |
| **MNIST 숫자 이미지·정답** / 데이터 및 표시 이미지 | 기존 `Textures/CNNModelDesignLab/mnist-test-*.png` (보존, 현재 모델 제작 콘솔에서 미사용). 이전 흑백 런타임 데이터는 아래 컬러 데이터로 교체 | [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [Keras 공식 배포·권리 표기](https://keras.io/api/datasets/mnist/), [Keras 공식 로더](https://github.com/keras-team/keras/blob/v3.15.1/keras/src/datasets/mnist.py); 배포 URL: [mnist.npz](https://storage.googleapis.com/tensorflow/tf-keras-datasets/mnist.npz). 확인: 2026-10-24 | Keras 공식 문서 표기: **Yann LeCun, Corinna Cortes**, 원본 NIST 데이터의 파생 데이터. [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). 이 프로젝트의 MNIST 부분집합 데이터도 같은 조건으로 제공 | Codex 보조로 원본 training에서 숫자별 첫 50장(500장), test에서 숫자별 첫 20장(200장)을 추출하고 원본 인덱스·정답·픽셀을 `.bytes`로 포장. 생성·합성 이미지 아님. 픽셀 수정 없음. 기존 표시 PNG는 원본 test에서 숫자별 첫 이미지이며 이번 작업에서 변경 없음 |
| **MNIST 컬러 통합 데이터 600장·미리보기** | `mnist-color-data/images.npy`, `labels.npy`, `source_indices.npy`, `style_seeds.npy`, `MnistColor.bytes`, `MnistTraining.bytes`, `MnistTest.bytes`, `preview.png` | [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [CVDF 공식 배포](https://github.com/cvdfoundation/mnist), [권리 근거](https://keras.io/api/datasets/mnist/). 2026-11-07 확인 | Keras 표기 권리자 Yann LeCun, Corinna Cortes. 원본 및 컬러 파생 데이터·미리보기 [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). 합성 부분 별도 권리자 미확인 | Codex 보조 Python/NumPy/Pillow 제작. 원본 0~3 서로 다른 손글씨 각 150장에 RGB 색·점무늬 합성, 정답·형태 보존. 생성형 이미지 AI 미사용. 통합 600장을 training 480장·test 120장으로 분리한 RGB CMNT v2 파일도 제공. `client/Assets/Data/CNNModelDesignLab/MnistTraining.bytes`, `MnistTest.bytes`로 런타임 연결. 모델 제작 콘솔의 평가 120장 미리보기는 동일 RGB 픽셀에서 실행 중 생성하며 픽셀 내용 변경 없음. 추출 URL·시드·방법은 전용 폴더 README 참조 |

ROOM #1 내부 아치 문과 ROOM #1-2·#2·#3 출구의 개방 불가 안내판(`ShutterUnavailableSign.prefab`), ROOM #1·#2 입구 바닥 홀로그램(`ShutterFloorHologram.prefab`)은 위 Noto Sans CJK KR Regular를 재사용합니다. 폰트 원본 수정은 없습니다.

같은 폰트를 픽셀 관찰실과 합성곱 실험실 사이 아치의 `합성곱 실험실` 이름표에도 재사용합니다. Codex 보조로 씬의 uGUI Text와 단색 배경을 구성했으며 폰트 원본을 수정하거나 별도 이미지 파일을 생성하지 않았습니다.

FactoryEnvironment의 B 텔레포트 사용 불가 안내는 위 Noto Sans CJK KR Regular를 재사용합니다. 원출처·권리자·라이선스는 위 기록과 같고 폰트 원본 수정은 없습니다.

공용 리그와 안내는 Meta XR Core SDK의 공식 프리팹을 직접 참조합니다. Codex 보조로 생성 인스턴스의 Touch Plus 본체 재질을 같은 SDK의 `MetaQuestTouchPlusMATphong_left.mat` / `MetaQuestTouchPlusMATphong_right.mat` 공유 참조로 연결했습니다. 모델·텍스처·재질·애니메이션 패키지 원본 수정이나 복제는 없습니다. 이전 추출본과 그 라이선스 사본 폴더는 삭제했습니다. 현재 사용하는 SDK 패키지의 `LICENSE.md`와 `NOTICE.txt` 원본은 유지합니다.

Universal Animation Library에서 사용한 동작은 `Idle_Loop`, `Idle_Talking_Loop`, `Walk_Loop`, `Hit_Chest`, `Hit_Head`, `Crouch_Idle_Loop`, `Dance_Loop`입니다. 무료판에 전용 놀람 동작이 없어 충격·웅크림 동작을 조합했습니다. 관절 곡선을 변환한 런타임 데이터의 이전 경로는 `client/Assets/Meshes/OpeningEnding/QuaterniusVillageMotion.asset`이며 기존 연출과 함께 삭제했습니다.

폰트와 아이콘의 원래 저작권·라이선스 고지는 유지합니다. 생성 도구의 표기는 저작권자를 뜻하지 않으며, 생성 이미지의 메타데이터만으로 권리 귀속이나 재배포 허가를 확인한 것은 아닙니다.

CNNModelDesignLab 제작 콘솔의 현실 숫자 인식 버튼은 위 Noto Sans CJK KR Regular를 재사용합니다. 폰트 원본 수정은 없습니다.

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

- 에셋: Jua-Regular.ttf. 기존 연출과 함께 삭제되어 제출 대상에서 제외. 이전 사용 위치: `client/Assets/Fonts/Jua/`, FactoryIntro 제목과 GAME START, VillageOpening 프리팹의 CNN FACTORY 간판.
- 원출처: https://github.com/google/fonts/tree/main/ofl/jua
- 저작권 표기: Copyright 2018 The Jua Project Authors.
- 라이선스: SIL Open Font License 1.1. 원본 폰트 수정 없음. 원래 배포본에는 `OFL.txt`가 포함되었다.

## 시작 화면 음악 — Cipher

- 에셋: `client/Assets/Audio/OpeningEnding/Cipher.mp3` (원본 파일명 Cipher2.mp3, ISRC USUAN1100844). 기존 연출과 함께 삭제되어 제출 대상에서 제외.
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

- **Pretendard Regular OTF**. 기존 프로젝트 폰트를 Room 2의 판정 시작 버튼에 재사용한다. 제작자·저작권자·출처·SIL OFL 1.1은 위 Pretendard 기록을 따르며, 폰트 원본과 동봉 라이선스는 그대로 사용한다.
- **JetBrains Mono Regular v2.304**. 피처맵 복도의 필터 가중치·출력 퍼센트 라벨에 사용한다. 공식 TTF를 수정 없이 사용했다. © 2020 The JetBrains Mono Project Authors. [원본 릴리스](https://github.com/JetBrains/JetBrainsMono/releases/tag/v2.304), [동봉한 SIL OFL 1.1](../client/Assets/Fonts/JetBrainsMono/OFL.txt).
- **16×16 컬러 숫자 입력·피처맵·출력 템플릿·격자·둥근 UI 텍스처**. `client/Assets/Textures/CNNFeatureMapLab/`의 Unity `.asset` 파일과 `FeatureMapCorridor.prefab`에서 사용한다. 숫자 패턴, 팔레트, 고정 시드와 더미 CNN 계산, 코드의 격자·라운드 사각형 수식으로 생성하던 결과를 2026-11-09 Codex 보조로 Unity 에디터에서 저장했다. 입력·맵은 실행 중 계산값에 따라 표시용 복사본을 갱신한다. Room 2의 장치 위 설명 카드와 숫자 선택 버튼에는 기존 둥근 UI 배경을 그대로 사용한다. 외부 이미지 파일 사용 없음. 제작자·법적 권리자 미확인, 별도 공개 라이선스 지정 없음.
- **복도 TMP 폰트 아틀라스**. `client/Assets/Fonts/CNNFeatureMapLab/Pretendard-Regular SDF.asset`, `JetBrainsMono-Regular SDF.asset`과 각 파일에 포함된 아틀라스에 사용한다. 기존 Pretendard Regular OTF 및 JetBrains Mono Regular TTF를 Unity TextMesh Pro의 `CreateFontAsset`으로 변환하고 Codex 보조로 저장했다. 원본 폰트 파일 수정 없음. 제작자·저작권자·SIL OFL 1.1 및 원출처는 위 Pretendard·JetBrains Mono 기록을 따르며, 기존 폰트 폴더의 라이선스 고지를 유지한다. Room 2의 장치 위 설명 카드와 공용 양피지 본문에 동일한 Pretendard SDF를 사용한다.
  Room 2 설명 카드의 제목·본문·버튼 크기를 조정하면서 기존 Pretendard 글꼴과 둥근 카드 배경을 재사용했다. 글꼴 및 배경 파일 수정 없음.

- **CNNFeatureMapLab 검증 영상·스크린샷 (2026-11-09)**. Codex 작업 폴더의 `outputs/featuremap/CNNFeatureMap-clear.mp4` 및 같은 폴더의 검증 PNG. 기존 프로젝트의 실제 Unity Play 화면을 Codex 보조 임시 검증기로 렌더링하고 Unity MediaEncoder로 무음 MP4를 기록했다. 생성형 영상이나 외부 실사 영상이 아니며 게임 런타임 에셋으로 추가하지 않았다. 원본 에셋·폰트의 출처와 라이선스는 이 문서의 해당 기록을 따름. 녹화물의 법적 권리자 미확인, 별도 공개 라이선스 지정 없음.

## ROOM 1-2 연속 체험 폰트 재사용 (2026-11-08)

- 에셋: 기존 `client/Assets/Fonts/NotoSansCJKkr-Regular.otf`.
- 사용 위치: `ConvolutionWorkbench`의 5×5 입력·3×3 결과와 두 계산판, `PixelCalculationUI`의 고정 3×3 요약과 오른쪽 목표·조작 안내, 합성곱 체험의 공용 `WorldUIButton` 인스턴스 라벨.
- 원출처·권리자·라이선스: 위 **Noto Sans CJK KR Regular 2.004** 기록의 공식 배포본과 © 2014–2021 Adobe, SIL OFL 1.1을 그대로 따른다. 기존 폰트 파일과 배포 라이선스 고지를 보존한다.
- 수정: 폰트 원본 수정·복제 없음. Codex AI 보조로 씬 전용 프리팹의 글자 배치·크기·색과 참조를 추가·수정했다.

## CNN 다큐멘터리 신규 씬 (2026-11-09)

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리·수정 |
| --- | --- | --- | --- |
| PostOffice1989.png | 삭제됨, 제출 대상 제외. 이전 사용 위치는 CNNArchiveOpening 기록판. | OpenAI 내장 `image_gen`, Codex 보조 생성. 프롬프트: 사람 없는 1989년 한국 우체국, 눈높이 1.6m 정면 다큐멘터리 사진, 오래된 목재 창구·우편 선반·CRT·형광등·테라조, 자연광과 실사 질감. | AI 생성 재구성, 실제 기록사진 아님. 생성 도구를 권리자로 단정하지 않음; 권리자 미확인, 별도 공개 라이선스 지정 없음. 생성 PNG 원본 저장, Unity 셰이더로 흑백/밝기/그레인 표시. |
| EndingZipAndTranslation.png | Textures/OpeningEnding, 엔딩 우편번호 및 카메라 번역 장면 | Codex 보조, OpenAI 내장 image_gen. 손글씨 우편번호 20501이 적힌 봉투와 휴대폰 화면의 가상 간판 번역을 위아래로 생성. | AI 생성 재구성, 실제 기록사진과 제품 화면이 아님. 권리자 미확인, 별도 공개 라이선스 지정 없음. Unity UV로 각 장면을 표시. |
| EndingBaseballUpscale.png | Textures/OpeningEnding, 엔딩 2026 업스케일링 컷 | Codex 보조, OpenAI 내장 image_gen. 1990년대 야구 중계풍 실사 16:9, 타자의 등번호 23이 중앙에 보이는 가상 경기, 방송사 로고·점수판 제외. | 권리자 미확인, 별도 공개 라이선스 지정 없음. Unity에서 같은 생성 이미지의 저해상도 밉과 원본을 비교선으로 전환하는 설명 연출. |
| EverydayVisionAtlas.png | `Textures/OpeningEnding`, 엔딩의 은행 수표 컷 및 이전 기록판 | OpenAI 내장 `image_gen`, Codex 보조 생성. 프롬프트: 4×2 무테 사진 아틀라스, 수표·사진 분류·휴대폰·주차장·청소기·도로·X-ray·고양이를 촬영하는 폰, 사람/가짜 캐릭터 없이 실사풍. | AI 생성, 실제 사건·시대 기록이 아님. 권리자 미확인, 별도 공개 라이선스 지정 없음. 원본 수정 없이 Unity UV로 각 칸 표시. |
| ArchiveMaterials.png | `Textures/OpeningEnding`, 엔딩 소품의 목재·종이 재질 | OpenAI 내장 `image_gen`, Codex 보조 생성. 프롬프트: 2×2 정사영 재질 아틀라스, 낡은 꿀색 참나무·회색 베이지 테라조·상아색 회벽·크림색 종이, 균일 조명, 글자 없음. | AI 생성 재질. 권리자 미확인, 별도 공개 라이선스 지정 없음. 원본 그대로, 재질별 UV 선택·색 곱셈. |
| ArchiveCrowd.mp3 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 오프닝의 우체국 군중 공간 음향. | [Indoor adult murmur, large group.wav](https://freesound.org/people/SpliceSound/sounds/260123/), 공개 HQ MP3 미리듣기 파일. 라이선스 원문 확인: 2026-11-09. | **SpliceSound**, **CC0 1.0**. 원본 파일 내용 변경 없음. Unity에서 위치별 재생 시작 시점·음량·필터 조절. 한국어/1989년 우체국 실녹음이라고 주장하지 않음. |
| ArchivePrinter.mp3 | 삭제됨, 제출 대상 제외. 이전 사용 위치는 오프닝의 우체국 프린터. | [old dot-matrix printer](https://freesound.org/people/viertelnachvier/sounds/181420/), Epson LX-400 실제 녹음의 공개 HQ MP3. 라이선스 원문 확인: 2026-11-09. | **viertelnachvier**, **CC0 1.0**. 원본 파일 수정 없음; 장면에서는 일부 구간만 재생. |
| ArchiveType/Stamp/Beep/Hit/BigHit/Rewind.wav, ArchiveRing/CRT/Machine/Footsteps.wav | 오프닝·엔딩 3D 효과음. Ring·CRT·Machine·Footsteps는 삭제되어 제출 대상 제외. | Codex 보조 Node.js PCM 합성. 사인파·필터 없는 제한 난수·지수 감쇠·충격 반복을 조합, 44.1kHz mono PCM16. 외부 녹음·음악 샘플 사용 없음. | 프로젝트용 합성 효과음, 별도 공개 라이선스 지정 없음. 실제 피아노·현악·사람 발소리 녹음이 아닌 합성 근사음. |
| Pretendard SemiBold / Nanum Pen Script | 신규 씬 자막·표지·숫자 라벨 | 프로젝트에 있는 원본 폰트 재사용; 위 기존 폰트 출처 및 동봉 OFL 유지. | 원본 변경 없음. |

EBS 영상은 연표 이동·전환 리듬을 참고했으며 영상·음악·음성을 다운로드하여 프로젝트에 삽입하지 않았다. 오프닝에서 사용하는 내레이션과 음악은 아래 미국 우체국 오프닝 항목에 기록했다.

### 오프닝·엔딩 인물 이미지와 내레이션

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| PostOffice1989WithClerks.png | Textures/OpeningEnding, 이전 오프닝 기록판 | Codex 보조, OpenAI 내장 image_gen. 1989년 한국 우체국에서 봉투를 정리하는 가상의 직원과 손님을 실사 다큐멘터리 풍으로 생성 | 권리자 미확인, 별도 공개 라이선스 지정 없음 | 생성 PNG 원본, 실제 기록사진 아님 |
| FaceRecognition2015Portrait.png | Textures/OpeningEnding, 엔딩 얼굴 인식 기록판 | Codex 보조, OpenAI 내장 image_gen. 2010년대 실내에서 카메라를 바라보는 가상의 인물을 실사 사진 풍으로 생성 | 권리자 미확인, 별도 공개 라이선스 지정 없음 | 생성 PNG 원본 |
| ArchiveVoiceEnding01–04.wav | 이전 엔딩 음성. 프로젝트에서 제거되어 제출 대상 제외 | Codex 보조, Windows System.Speech의 Microsoft Heami Desktop(ko-KR)으로 프로젝트 대사를 22.05 kHz 16-bit 모노 WAV로 합성 | 음성 엔진 Microsoft, 생성 파일의 권리 및 배포 조건 미확인, 별도 공개 라이선스 지정 없음 | 대사 구간에 맞춰 무음·속도 조정, 일부 긴 구절은 축약 |

## 미국 우체국 오프닝

### 2026-11-13 출처·배포 조건 확인

- **Quaternius Ultimate Modular Men Pack — Suit.fbx, Casual_2.fbx**: `client/Assets/Meshes/CNNArchiveOpening/`. [제작자 공식 배포 페이지](https://quaternius.com/packs/ultimatemodularcharacters.html), [공식 배포 폴더](https://drive.google.com/drive/folders/1USAAquX2JJWuA2m6zol0KUkFe3UkZ8zX), [동봉 License.txt](https://drive.google.com/file/d/1TTvylHa1CsiJuHFWWiv6PFGhLM-aAH5z/view). 제작자·권리 표기: Quaternius. **CC0 1.0**. 공식 페이지, FAQ, 동봉 고지에서 수정·상업적 사용·재배포 허용 확인. 저작자 표기 의무는 없으나 프로젝트 제출 기록에 기재한다. CC0는 상표·초상·프라이버시 권리를 포괄 보증하지 않으며 제작자 보증이나 후원을 암시하지 않는다. FBX 원본 수정 없음. Codex AI 보조로 Unity에서 Idle/Walk 동작 선택, 크기·배치·동선과 무광 재질을 설정한다. 원래 License.txt 고지는 모델과 함께 보존한다.
- **ArchiveMaterials.png 재사용**: 기존 엔딩 소품 외에 오프닝 우체국 목재·테라조·회벽·종이 표면에도 사용. 기존 OpenAI image_gen / Codex AI 보조 생성 이미지이며 위 기록의 권리자 미확인 상태를 유지한다. PNG 원본 수정·복제 없음. 재질의 UV 영역과 색만 변경.
- **OpeningCrowd.mp3**: [4177569399의 Freesound 원본](https://freesound.org/people/4177569399/sounds/449487/)에서 CC0 표기를 재확인했다. 프로젝트 파일은 사용자 제공 Pixabay 파일이므로 원본과의 동일성까지 확인한 것은 아니다. 기존 파일은 유지하며 재생 음량·공간화·저역 통과 설정을 조정한다.
- **OpeningNarration01–06.wav**: 사용자도 제작 서비스·목소리를 확인하지 못함. 제작자·권리자·공개 배포 허용 조건은 **미확인**이며, 사용자 제공 파일이라는 사실만으로 배포 권한을 인정하지 않는다. 파일은 유지하며 내레이션 출력 설정만 조정한다.
- **OpeningScore.wav / Suno Vintage Electro Swing**: 사용자 답변은 “아마 유료구독은 아니었을 것”. 생성 당시 플랜과 개별 이용권은 확인되지 않았다. [Suno 공식 권리 안내](https://help.suno.com/en/articles/2416769)와 [배포 안내](https://help.suno.com/en/articles/2410177)에 따르면 무료 생성물은 비상업적 이용에 제한되며 사후 구독만으로 상업 이용권이 자동 소급되지 않는다. **상업적 배포 권한 미확인**. 경진대회 제출·전시·주최 측 재배포를 비상업적 이용이라고 임의 판단하지 않는다. 사용 곡의 생성 시점 약관·계정 권한과 대회 조건을 별도로 대조해야 한다.
- **ArchiveVoiceEnding01–04.wav / Windows Heami Desktop TTS**: 기존 합성 음성은 프로젝트에서 제거했다. 현재 엔딩 내레이션은 아래 EndingNarration01–05.wav 항목을 참고한다.

위 미확인 음원은 사용 가능 판정을 내린 자료가 아니다. 공개 배포 전에 제작 서비스·생성 당시 조건을 확인하거나 권한이 확인된 음원으로 교체해야 한다. 현재 작업은 배포·게시를 수행하지 않는다.

- **우체국 서비스 명판의 Pretendard SemiBold 재사용**: 기존 `Fonts/Pretendard/Pretendard-SemiBold.otf`를 창구 표지에 참조. 제작자·출처·SIL OFL 1.1은 위 Pretendard 기록과 동일하며 원본·라이선스 고지를 유지한다. Codex AI 보조로 글자 배치·색상만 설정했다.
- **2026-11-13 오프닝 검증 PNG**: Codex 시각화 작업 폴더의 `verified-opening-0.png`~`verified-opening-3.png` 등. Codex AI 보조로 현재 Unity 씬의 실제 카메라 렌더를 캡처한 검증 자료이며 게임 런타임 에셋에는 포함하지 않는다. 생성형 사진이 아니다. 원본 에셋의 권리 조건은 이 문서 해당 항목을 따르며 캡처 자체에 별도 공개 라이선스를 지정하지 않는다.

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| PostOffice_US_1989.png | 이전 오프닝 우체국 전경 | OpenAI 내장 image_gen, Codex 보조. 1989년 미국 우체국의 가상 내부 | 권리자 미확인. 별도 공개 라이선스 지정 없음 | PNG 원본. 이전 연출에서 Unity로 전체 확대와 창구 줌인 |
| PostOfficeStamp.mp4, PostOfficeStampPlate.png, PostOfficeStampedEnvelope.png | 삭제됨, 제출 대상 제외. 이전 오프닝 도장 동작 및 우편번호 인식에 사용 | Codex 보조. OpenAI 내장 image_gen으로 우체국 전경을 참고한 봉투 사진과 도장을 쥔 손을 생성하고 FFmpeg로 손동작, 도장 자국, 인식 표시 합성 | 권리자 미확인. 별도 공개 라이선스 지정 없음 | 무음 H.264 컷 및 정지 프레임 |
| OpeningCrowd.mp3 | 오프닝 우체국 군중 소리 | [Pixabay의 Group Talking](https://pixabay.com/sound-effects/people-group-talking-68823/), 사용자 제공 파일. [Freesound 원본](https://freesound.org/people/4177569399/sounds/449487/) | 4177569399, Pixabay Content License 표시, Freesound 원본 CC0 | 원본 파일 그대로, 0초부터 재생 |
| EndingNarration01–05.wav | 엔딩 대사 5개 | 사용자가 제공한 2026-11-14 화면 녹화의 Typecast 합성 음성을 Codex 보조로 추출 | 음성 제공 서비스 Typecast. 음성 권리자와 공개 배포 조건 미확인 | 대사별 절단, 모노 48 kHz PCM WAV 변환, 10 dB 증폭 |
| EndingNarrationIntro.wav, EndingNarrationUpscale.wav, EndingNarrationOutro.wav | 엔딩 첫 모델, 2026 야구, 마지막 모델 | 사용자가 제공한 2026-11-15 03:19:56 화면 녹화에서 준성 음성 추출. | Typecast 합성 음성. 음성 권리자와 공개 배포 조건 미확인. | 48 kHz 모노 PCM WAV, 게인 조정, 2026 컷 재생 속도 1.16배, 마지막 모델 앞의 구간 안내 삭제. |
| EndingNarrationFace.wav, EndingNarrationSign.wav, EndingNarrationBaseball.wav | 엔딩 얼굴, 간판, 야구 컷 | 사용자가 제공한 2026-11-15 03:54:53 화면 녹화에서 준성 음성 추출. | Typecast 합성 음성. 음성 권리자와 공개 배포 조건 미확인. | 대사별 절단, 48 kHz 모노 PCM WAV, 10 dB 증폭. |
| OpeningNarration01–06.wav | 오프닝 대사 | 사용자 화면 녹화에서 오디오 추출. 녹음·합성 방식과 음성 제공 서비스 미확인 | 제작자·권리자·배포 조건 미확인 | 대사별 절단, 모노 WAV 변환 및 게인 조절 |
| EndingScore.wav | 엔딩 배경음악 | 사용자가 제공한 2026-11-14 23:56:53 화면 녹화의 Suno 곡 「1989」에서 Codex 보조로 오디오 추출 | Suno 생성. 생성 당시 계정 플랜과 공개 배포 조건 미확인 | 시작 구간 21.05초 편집, 스테레오 48 kHz PCM WAV 변환, 10 dB 증폭, 페이드 인·아웃 |
| OpeningScore.wav | 오프닝 음악 | 사용자가 Suno에서 생성한 [Vintage Electro Swing](https://suno.com/s/X1UheAwttpqtFZhR)의 화면 녹화에서 오디오 추출 | Suno 생성. 계정의 사용 조건 및 공개 배포 권한 미확인 | 앞 무음 절단, 35초 편집, 페이드 및 게인 조절 |
| OpeningTimelinePulse.wav | 오프닝 연도표 3.5초 | Codex 보조 Python PCM 합성. 낮은 사인파 펄스와 짧은 감쇠를 연도 카운트다운 속도에 맞춰 합성 | 프로젝트 제작, 별도 공개 라이선스 지정 없음 | 사용자가 고른 C 시안, 24kHz 모노 WAV |

## 공장 환경 표면 텍스처 (2026-11-09)

아래 PNG는 `client/Assets/Textures/FactoryEnvironment/`에 있으며 `FactoryEnvironment` 씬의 표시 재질에서 사용한다. 첨부 이미지는 재질과 조명 참고용으로만 사용했다.

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `FormedConcrete_*`, `SealedConcreteFloor_*` | 벽과 실내 바닥 | Codex AI 보조, Python과 NumPy로 거푸집 줄눈·타이홀·기공과 바닥 줄눈·마모를 합성해 반복 가능한 알베도·노멀·금속성/매끄러움 맵 제작 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `FactoryPowderCoat_*`, `BrushedAlloy_*` | 사물함·도장 설비, 철제 설비·셔터 | Codex AI 보조, Python과 NumPy로 도장 박리·긁힘과 금속 연마 결·산화 얼룩을 합성해 알베도·노멀·금속성/매끄러움 맵 제작 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `LabWorktop_*`, `CorrugatedCarton_*`, `AgedPaperFibre_*` | 연구 데스크 상판, 상자, 연구 서류 | Codex AI 보조, Python과 NumPy로 작업대 입자·골판지 골·종이 섬유와 얼룩을 합성해 알베도·노멀 제작. 상판·골판지에는 금속성/매끄러움 맵 포함 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `WeatheredConcrete_*`, `OxidizedSteel_*`, `DarkIndustrialRubber_*` | 외부 및 휴게실 콘크리트, 셔터 이음쇠와 철제 부품, 분석 장비 호스 | 기존 프로젝트 파일. 원출처 미확인 | 제작자·저작권자·라이선스 미확인 | PNG 원본 유지. 재질·UV·표시 메시에서 재사용 |

ROOM #2의 벽 하단과 ROOM #3 연결 복도에는 기존 `FormedConcrete_*`를, 서쪽 문턱과 복도 계단에는 `SealedConcreteFloor_*`를 재사용했다. 출구 셔터의 레일 연장부에는 `OxidizedSteel_*`, 유리 난간의 철제 프레임에는 `BrushedAlloy_*`를 사용한다. PNG 원본과 위 출처·권리 정보를 유지했다. 난간 참고 사진은 형태 참고에만 사용했다.

후속 연구복 수정에서 `AgedPaperFibre_Normal.png`를 청색 연구복의 섬유 표면에도 재사용했다. PNG 원본을 유지하고 Codex AI 보조로 재질의 반복 크기와 노멀 강도를 조정했다. 출처·권리자·라이선스는 위 신규 합성 기록을 따른다.

튜토리얼 작업화 전용 `TutorialBootRubber` 재질은 기존 `DarkIndustrialRubber` 노멀 맵을 재사용한다. Codex AI 보조로 재질의 색과 노멀 강도를 조정했다. 원본 이미지·출처·권리자·라이선스는 위 기존 파일 기록을 따른다.

## 공장 연구 기록과 배경 장비 화면 (2026-11-09)

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `FactoryResearchPages.png` | `Textures/FactoryEnvironment/`; 공장 바닥·책상 문서와 게시물의 인쇄면 | Codex AI 보조로 Python·NumPy·Pillow와 고정 난수 시드로 RGB, 합성곱, ReLU, 풀링, 모델 형태의 계산 예시·도표·개별 문서 번호를 조판. 프로젝트 체험 문서를 참고했으며 실제 사용자 학습 기록은 사용하지 않음 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `FactoryDiagnosticDisplays.png` | `Textures/FactoryEnvironment/`; 공장 배경 모니터·계측 화면 | Codex AI 보조로 Python·NumPy·Pillow에서 5×5 입력과 대각선 3×3 필터의 valid 합성곱을 계산하고 표·그래프로 조판. 정적 설명용 표본임을 화면에 명시 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |

두 이미지의 한글·영문 조판에 기존 `Fonts/NotoSansCJKkr-Regular.otf`를 재사용했다. 폰트 원본은 수정·복제하지 않았으며 원출처·권리자·SIL OFL 1.1은 위 **Noto Sans CJK KR Regular 2.004** 기록을 따른다. 기존 배포 라이선스 고지를 유지한다. 사용자 첨부 참고 이미지 자체는 런타임 에셋으로 복제하지 않았다.

정문 아치형 간판에는 기존 Noto Sans CJK KR Regular를 굵게 표시하고, 녹색 프레임과 크림색 에나멜판에 기존 `FactoryPowderCoat_*` 텍스처를 재사용한다. 폰트·텍스처 원본은 그대로 사용하며, 출처와 라이선스는 위 폰트 및 공장 표면 텍스처 기록을 따른다.

정문 왼쪽 벽의 대형 쓰레기통 두 개에는 기존 녹색·청색 도장, 철제 손잡이와 고무 재질을 사용했다. 재질이 참조하는 기존 `FactoryPowderCoat_*`, `BrushedAlloy_*`, `DarkIndustrialRubber_*` 텍스처의 원본과 출처·권리 정보는 위 표면 텍스처 기록을 따른다.

## 공장 조명 베이크

`client/Assets/Textures/FactoryEnvironment/`의 `Lightmap-*_comp_*`와 `ReflectionProbe-*.exr`는 공장 씬의 고정 표면 조명, 그림자와 방별 반사에 사용한다. Codex AI 보조로 씬의 광원과 메시를 설정하고 Unity Progressive CPU Lightmapper와 반사 프로브로 다시 생성했다. 베이크 과정에서 외부 미디어를 추가하지 않았다. 제작자·법적 권리자는 미확인이며 별도 공개 라이선스를 지정하지 않았다.

`client/Assets/Audio/FactoryEnvironment/ElectricalCrackle.wav`는 정문 벽등과 연결 복도등의 합선 효과음이다. Codex 보조로 JavaScript에서 사인파, 필터링한 난수, 짧은 감쇠 펄스를 조합해 24 kHz 모노 PCM16으로 신규 합성했다. 외부 샘플 사용 없음. 법적 권리자 미확인, 별도 공개 라이선스 지정 없음.

## 튜토리얼 사물함과 공장 선반 상자

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `TutorialLockerCoating_*` | 튜토리얼 사물함 문, 손잡이, 통풍구와 번호판 | Codex AI 보조로 Python·NumPy·Pillow에서 도장 입자, 문별 긁힘·찍힘, 금속 표면, 번호를 합성해 알베도·노멀·금속성/매끄러움 맵 제작 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `ShelfKraftCarton_*` | 공장 선반의 골판지 상자와 재고 라벨 | Codex AI 보조로 Python·NumPy·Pillow에서 기존 `CorrugatedCarton`의 종이 결, 새 절단면·포장 테이프·재고 라벨을 합성해 알베도·노멀·금속성/매끄러움 맵 제작. 기존 Noto Sans CJK KR Regular로 라벨 조판 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음. 폰트 원출처와 SIL OFL 1.1은 위 **Noto Sans CJK KR Regular 2.004** 기록을 따름 | 신규 합성. 원본 텍스처·폰트 유지 |

선반 케이스 재질에는 기존 `FactoryPowderCoat_*`, `BrushedAlloy_*`, `DarkIndustrialRubber_Normal.png`를 재사용했다. Codex AI 보조로 재질 색·매끄러움·노멀 강도와 케이스별 UV를 조정했다. 이미지 원본을 유지했고 출처와 권리 정보는 위 공장 환경 표면 텍스처 기록을 따른다.

## 공장 연구 데스크 전자기기

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `WorkstationInputLegends.png` | 배경 키보드 각인과 키보드·마우스 외장 | Codex AI 보조로 Python·NumPy·Pillow에서 한영 키 각인, 플라스틱 색상과 미세 표면 결 합성 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `WorkstationMonitorDisplays.png` | 연구 데스크의 배경 모니터 | Codex AI 보조로 Python·Pillow에서 진단 그래프, 작업 기록, 바탕화면, 시계 등 정적 레이아웃을 직접 조판·합성 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |
| `ResearchDeskPhoneFace.png` | 연구 데스크 유선 전화기의 LCD와 키 각인 | Codex AI 보조로 Python·Pillow에서 가상 내선·회선 상태 화면, 숫자와 기능 키를 직접 조판 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 신규 합성 |

위 텍스처의 문자에 기존 **Noto Sans CJK KR Regular 2.004**를 재사용했다. 원본 폰트와 동봉한 SIL OFL 1.1 고지는 유지한다. 모니터·전화기 외장과 PC 본체 재질에는 기존 `DarkIndustrialRubber_Normal.png`를, PC 금속 부품에는 기존 `BrushedAlloy_*`를 사용한다. 원본 이미지와 출처·권리 정보는 위 공장 환경 표면 텍스처 기록을 따른다. 참고 사진 자체는 런타임 에셋에 사용하지 않았다.

## 공장 실내 벽과 환풍기

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `FactoryLabPanelCream_*`, `FactoryLabPanelSlate_*` | 공장 작업 구역 실내 벽과 오프닝 CNN FACTORY 입구의 회백색 패널·청회색 마감 | Codex AI 보조로 Python·NumPy·Pillow에서 줄눈, 표면 결, 걸레받이의 알베도·노멀·금속성/매끄러움 맵 합성 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 합성 원본 유지, 오프닝 입구에도 재사용 |
| `FaultyFanBearing.wav` | `Audio/FactoryEnvironment/`, 환풍기 3대의 재시동 마찰음 | Codex AI 보조로 Python·NumPy에서 칼날이 철판에 걸렸다가 튕기는 마찰, 불규칙한 고음과 금속 잔향을 합성. 1.13초, 24 kHz 모노 PCM | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음 | 기존 합성음 교체 |

키 큰 사물함 번호판에는 기존 `TutorialLockerCoating_*` 아틀라스의 번호 영역을, 합성곱 관찰 콘솔에는 기존 `FactoryDiagnosticDisplays.png`의 계산 표본 한 장을 재사용했다. 원본 이미지와 출처·권리 기록은 유지하고 Codex AI 보조로 메시 UV를 조정했다.

## 공장 강철의 산화 흔적과 보름달

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `FactoryLightOxidation_*` | `Textures/FactoryEnvironment/`, 공장의 `RustSteel` 표면 | Codex AI 보조로 Python·NumPy·Pillow에서 기존 브러시드 강철 `BrushedAlloy_*`와 `OxidizedSteel_*`를 좁은 산화 얼룩으로 합성해 알베도·노멀·금속성/매끄러움 맵 제작 | 법적 권리자 미확인. 별도 공개 라이선스 지정 없음. 원본은 위 표면 텍스처 기록 참조 | 신규 합성. 원본 PNG 유지 |
| `FactoryFullMoon.jpg` | `Textures/FactoryEnvironment/`, 공장 야외 하늘 | [NASA SVS Moon Phase and Libration, 2026의 Full Moon 프레임](https://svs.gsfc.nasa.gov/vis/a000000/a005000/a005048/phase_full.1571_print.jpg) | NASA Scientific Visualization Studio, 시각화 Ernie Wright. [NASA SVS 사용 안내](https://svs.gsfc.nasa.gov/help/)의 공개 저작물 지침에 따라 출처 표기 | 원본 JPEG 유지. Unity 메시 UV로 달 표면만 표시 |

## 휴게실 병 라벨과 표면 재사용

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `LoungeBottleLabels.png` | 휴게실 와인병·소주병 라벨 | Codex AI 보조로 Python·Pillow에서 가상 상표를 조판. 기존 Noto Sans CJK KR Regular 사용 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음. 폰트는 위 **Noto Sans CJK KR Regular 2.004** 출처 및 SIL OFL 1.1을 따름 | 신규 합성. 폰트 원본 유지 |
| `LoungeHandheldGameScreens.png` | 휴게실 바닥 휴대용 게임기 두 대의 LCD | Codex AI 보조로 Python·Pillow에서 서로 다른 게임 화면과 유리 균열을 합성. 기존 Noto Sans CJK KR Regular 사용 | 합성 이미지의 법적 권리자 미확인. 별도 공개 라이선스 지정 없음. 폰트는 위 **Noto Sans CJK KR Regular 2.004** 출처 및 SIL OFL 1.1을 따름 | 신규 합성. 폰트 원본 유지 |

ROOM 1-2 사물함과 휴게실 가구는 기존 도장·직물·작업대 표면 텍스처와 사물함 번호판을 재사용한다. 원본 이미지와 위 출처·권리 기록을 유지했다. 첨부 사진은 형상 참고에 사용했다.

ROOM 1-2 선반 레일의 국소 산화에는 `OxidizedSteel_*`를, 휴게실 게임기 외장에는 `DarkIndustrialRubber_Normal.png`를 재사용했다. 원본 텍스처와 위 공장 표면 기록의 출처·권리 정보를 유지했다.

## ROOM 1-2 공중 합성곱 UI의 기존 폰트 재사용 (2026-11-11)

| 에셋명 | 사용 위치 | 원출처 | 제작자·저작권자 | 라이선스·수정 여부 |
| --- | --- | --- | --- | --- |
| Noto Sans CJK KR Regular 2.004 | CNNConvolutionLab 필터 손잡이의 배치 안내 | [Noto CJK 공식 배포본](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) | © 2014–2021 Adobe | SIL Open Font License 1.1. 기존 프로젝트 폰트 재사용, 폰트 원본 수정 없음. 기존 배포용 라이선스 유지. |

- **ParchmentDigitIllustrations.png**. 공용 양피지 Reader의 01번 문서에 사용한다. 기존 숫자 획, 밝기값, RGB 패턴을 바탕으로 Codex 보조 Python/Pillow 코드로 도형·격자·색을 합성했다. 라벨에 위 Pretendard SemiBold를 재사용했으며 폰트 원본 수정은 없다. 아틀라스 제작자·법적 권리자 미확인, 별도 공개 라이선스 지정 없음.

## ROOM 1-2 합성곱 안내 음성 (2026-11-13)

| 에셋명 | 사용 위치 | 출처·제작 방법 | 제작자·권리자·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `ConvolutionNarration_*.wav` 9개 | `Audio/CNNConvolutionLab/`, CNNConvolutionLab의 단계별 안내 음성 | Claude Code AI 보조. Windows System.Speech의 Microsoft Heami Desktop(ko-KR), 말하기 속도 +2로 프로젝트 안내 문구를 22.05 kHz 16-bit 모노 WAV로 합성. 문구는 [픽셀 관찰과 ROOM 1-2 체험](pixel-inspection.md)의 안내 음성 표에 기록 | 음성 엔진 Microsoft, 생성 파일의 권리 및 배포 조건 미확인, 별도 공개 라이선스 지정 없음 | 앞 0.12초, 뒤 0.25초의 무음을 남기고 절단. 2026-11-14 체험 간소화에 맞춰 6개 합성 및 빠진 단계의 음성 삭제. 2026-11-15 그림 기반 체험에 맞춰 10개 합성(5개는 새 이름) 및 이전 단계 음성 7개 삭제. 이후 Codex 보조로 필터 배치 완료 음성을 삭제하고 `ConvolutionNarration_NextStage.wav`에서 다음 방 예고 문장을 절단 |

같은 변경에서 받침대 명판, 상단 설명판, 오른쪽 목표·조작 안내 글을 제거했다. CNNConvolutionLab에서 Pretendard Regular는 자막·버튼·계산 숫자에, Noto Sans CJK KR Regular는 필터 손잡이 배치 안내에 계속 사용한다. 두 폰트의 원본과 배포용 라이선스 고지는 그대로다.

## ROOM 1-1 관찰 안내 음성 (2026-11-13)

| 에셋 | 사용 위치 | 생성 도구·제작 방법 | 제작자·권리자 / 라이선스 | 수정 |
| --- | --- | --- | --- | --- |
| Observation0–2.wav | `client/Assets/Audio/CNNScreenLab/`, 돋보기 조작·픽셀과 RGB·RGB 값 안내 | Codex 보조로 작성한 한국어 대사를 Windows System.Speech / Microsoft Heami Desktop으로 WAV 합성 | 음성 엔진 Microsoft. 생성 파일 권리·배포 조건 미확인, 별도 공개 라이선스 부여 없음 | 2026-11-15 Codex 보조로 클립마다 끝의 조작 안내 한 문장씩 절단. 남은 PCM과 Unity GUID 유지 |


## 공장 조명 재베이크 (2026-11-14)

기존 `Textures/FactoryEnvironment/Lightmap-*_comp_*`와 `ReflectionProbe-0~4.exr`를 휴게실 Baked 조명 설정을 포함해 다시 생성했다. Unity 6000.3.24f1 Progressive CPU Lightmapper 및 반사 프로브 베이크를 Codex AI 보조로 실행했다. 기존 씬의 메시·재질·광원을 사용했고 새 외부 미디어는 추가하지 않았다. 제작자·법적 권리자 미확인, 별도 공개 라이선스 지정 없음. 기존 에셋 출처와 라이선스 고지는 유지한다.

휴게실 가구·침구 단순화, 바닥 소품·남쪽 게시판·선반 제거 및 북쪽 창문 차폐 후 같은 도구와 방식으로 조명·반사 프로브 이미지를 다시 베이크했다. 기존 생성 방법·권리 표기는 동일하다.

엔딩 잔상 사진판의 `EndingHistory1989/1998/2016/2019/2026.mat`은 위에 기록된 기존 엔딩 이미지 원본을 참조한다. Codex 보조로 씬 배치와 재질 크롭을 설정했으며 이미지 파일은 새로 생성하지 않았다. 원출처·권리·라이선스는 해당 기존 이미지 기록을 따른다.

## ROOM 1-2·2 분류 학습 필터와 파생 피처맵 (2026-11-15)

- 학습 자료: 기존 `mnist-color-data/MnistTraining.bytes`의 실제 컬러 RGB 숫자 1·2·3 360장(324장 학습·36장 검증), 별도 평가는 `MnistTest.bytes`의 90장. [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [CVDF 배포](https://github.com/cvdfoundation/mnist). 위 MNIST 항목의 Yann LeCun·Corinna Cortes / CC BY-SA 3.0 출처·권리 표기를 따른다. 기존 색·무늬 합성 자료를 16×16으로 축소하고 /255 정규화했다. R 분류 모델은 그중 R만 사용한다. 원본 마스크 재취득·색 재합성·방향 목표 필터는 사용하지 않았다.
- `CNNFeatureMapLab/ExecutionData/Digit1ExecutionData.cs`~`Digit3ExecutionData.cs`의 기존 입력 픽셀은 그대로 보존했다. 피처맵·출력 틀과 `mnist-color-data/learned-filters/preview.png`는 Codex 보조 TensorFlow 숫자 분류 학습, NumPy 순전파·C# 내보내기, Pillow 확대 시각화로 다시 계산했다. 학습된 가중치를 그대로 사용했으며 생성형 이미지 AI는 쓰지 않았다. 기존 입력은 위 **16×16 컬러 숫자 입력** 기록을 따르며 별도 권리자는 미확인이다. 학습 자료 라이선스를 근거로 기존 직접 제작 입력에 새 라이선스를 부여하지 않는다.
- 모델·가중치의 선택 방법, 원본 float32 보존, 기존 출력 판정 방식과 표시 한계는 [제작 기록](../mnist-color-data/learned-filters/README.md)에 정리한다. 기존 배포용 라이선스 고지는 보존한다.

## ROOM #3 AR 진입·완료 안내 폰트 재사용 (2026-11-15)

- **Noto Sans CJK KR Regular 2.004**. `client/Assets/Prefabs/CNNModelDesignLab/ModelControlPanel.prefab`의 AR 진입 안내 카드, 방문·복귀 완료 조건, 진입·재입장 버튼과 완료 문구에 기존 `Fonts/NotoSansCJKkr-Regular.otf`를 재사용한다. 원출처는 위 Noto CJK 공식 배포본, 제작자·저작권자는 © 2014–2021 Adobe, 라이선스는 SIL OFL 1.1이다. Codex AI 보조로 UI 문구·배치·크기·색을 변경했으며 폰트 원본 수정·복제 없이 기존 라이선스 고지를 유지했다.

## 사용자 제공 한국어 안내 WAV 교체

사용자가 제공한 한국어 TTS 원본 43개를 장면과 재생 시점에 맞춰 배치했다. 파일명과 Unity 참조를 정리했으며 PCM 데이터를 편집하거나 재합성하지 않았다. 원본은 44.1 kHz, 16-bit, 모노 WAV다. 합성 도구, 음성 모델, 제작자, 권리자 및 배포 조건은 미확인이다. 이전 음원 항목은 변경 이력이다.

| 사용 위치 | 파일 수 | 연결 |
| --- | ---: | --- |
| 오프닝 | 6 | 질문, 우체국, 봉투, 픽셀, 공장 안내 |
| 픽셀 관찰실 | 3 | 돋보기 조작과 RGB 관찰 |
| 합성곱 실험실 | 10 | 필터 배치, 계산, 특징 맵, 완료 |
| 특징 탐구실 | 10 | 순방향 안내, 계단 클립 재사용, 숫자별 판정 |
| CNN 모델 제작실 | 7 | 첫 블록 안내, AR 입장, 학습 시작 구성 오류 다섯 종류 |
| 엔딩 | 7 | 첫 모델, 사례 다섯 컷, 마무리 |
