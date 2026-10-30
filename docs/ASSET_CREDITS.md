# 콘텐츠 에셋 출처 및 저작권

외부 에셋(폰트·아이콘·애니메이션 포함)과 직접 생성한 미디어의 제출용 출처 목록입니다.

| 에셋명 / 종류 | 사용처 | 출처 또는 생성 방법 | 저작권·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| **InputPixels.png** / 입력 이미지 | `client/Assets/Textures/CNNFeatureMapLab/InputPixels.png`; CNNFeatureMapInput의 InputMapController 및 기존 InputSurface 재질 | 기존 프로젝트 에셋. 원출처 URL·생성 도구·제작 방법·AI 보조 여부 미확인 | 제작자·권리자·라이선스 미확인 | 이미지 파일 수정 없음. Codex 보조로 이미지 참조를 Workspace Manager에서 입력 맵 컴포넌트로 이전 |
| **OpeningBroadcast.png** / 오프닝 방송 정지 이미지 | CNNPixelLab 원본 이미지·픽셀 및 RGB 계산 입력 | 프로젝트의 `FactoryIntro/VillageDisplay` 셰이더를 Unity에서 뉴스 방송(_Channel=0), 재생 3초, 손상·간섭 0으로 렌더링. Codex 보조로 1024×576 PNG 추출 | 프로젝트 내부 절차적 방송 그래픽에서 제작. 외부 방송 영상·로고 사용 없음. 별도 공개 라이선스 지정 없음 | 원본 셰이더 수정 없음. 체험 실행 시 저해상도·블러 처리 후 동일 이미지에서 RGB 샘플링 |
| **Noto Sans CJK KR Regular 2.004** / 폰트 | 시작 화면, 자막, UI, FactoryEnvironment의 정문·후문 및 튜토리얼·ROOM #1~#3 셔터 위 간판, CNNModelDesignLab 제작 콘솔의 저장된 구성 검사·학습·예측 UI | [Noto CJK 공식 배포본](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) | © 2014–2021 Adobe. [SIL Open Font License 1.1](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/LICENSE) | 폰트 원본 수정 없음 |
| **Meta Quest Touch Plus 컨트롤러 모델·텍스처·재질·애니메이션 (Meta XR Core SDK 207.0.1 유래)** | `client/Assets`의 `Meshes/Common/MetaQuestTouchPlus`, `Textures/Common/MetaQuestTouchPlus`, `Materials/Common/MetaQuestTouchPlus`; 공용 XRPlayerRig 컨트롤러 표시와 FactoryTutorial 확대 안내 | [Meta 공식 패키지](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.core)의 207.0.1에서 표시용 에셋만 분리. [공식 애니메이션 사용 안내](https://developers.meta.com/vr/documentation/unity/controller-animations/) | 원본 LICENSE.md 표기: Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved. [Meta Platform Technologies SDK License Agreement](https://developers.meta.com/horizon/licenses/oculussdk/). 원래 LICENSE.md·NOTICE.txt를 모델 폴더에 보존. 별도 공개 라이선스로 재허가하지 않음. 확인: 2026-10-27 | FBX·PNG·Animator Controller와 원본 `.meta`는 내용 수정 없이 보존. Codex 보조로 재질 3개의 SDK 셰이더 참조를 기존 URP로 변경하고 텍스처·색상·투명도 속성을 연결. 기존 Codex 보조 코드가 Unity XR 입력과 애니메이션·버튼 표식을 연결 |
| **Nanum Pen Script** / 폰트 | 기존 양피지 서체 에셋. 현재 양피지 화면에서는 사용하지 않음 | [Google Fonts 공식 자료](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/METADATA.pb) | © 2010 NHN Corporation. 디자인: Sandoll Communications Inc. [SIL Open Font License 1.1](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt) | 폰트 원본 수정 없음 |
| **Pretendard Regular / SemiBold** / 폰트 | 공장 양피지 수집물과 공용 ParchmentReader 프리팹 | [Pretendard 공식 저장소](https://github.com/orioncactus/pretendard) | © 2021 Kil Hyung-jin 및 원저작자. 동봉한 [SIL Open Font License 1.1](../client/Assets/Fonts/Pretendard/LICENSE.txt) | 공식 OTF 원본 수정 없음 |
| **Lucide / Feather 기반 아이콘** / 아이콘 | 태블릿 UI | [Lucide 공식 저장소 및 라이선스](https://github.com/lucide-icons/lucide/blob/e715245d62667c800e7f54c94b1b023692e900a3/LICENSE) | © 2026 Lucide Icons and Contributors, ISC. Feather 유래 도형: © 2013–present Cole Bemis, MIT | 선 도형을 간략화하고 아이콘 아틀라스로 변환 |
| **Universal Animation Library — Standard** / 주민 동작 | FactoryIntro·FactoryEnding 마을 주민의 걷기·시청·대화·충격 반응·축하 | [Quaternius 공식 배포 페이지](https://quaternius.itch.io/universal-animation-library), 무료 Standard 배포본 확인·다운로드: 2026-10-23 | 동봉 License/README의 제작자 표기: **Quaternius**. 공식 페이지에 애니메이터 **Gonzalo Furnier**의 제작 참여 표기. [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/): 수정·재배포·상업적 이용 허용 | 실제 관절 애니메이션을 Codex 보조로 기존 주민의 비율·관절에 맞게 변환하고 상태 전환을 혼합. 외부 캐릭터 메시·유료 Pro/Source 파일 사용 없음 |
| **VillageOpening.wav** / 음원 | 18초 마을 오프닝의 배경·효과음 | 프로젝트 작업에서 Codex 보조로 Python 사인파·필터링한 난수·음량 곡선을 직접 합성 | 프로젝트용 직접 제작 음원. 별도 공개 라이선스는 지정하지 않음. 외부 녹음·음악 샘플 사용 없음 | 오프닝의 공격·화면 고장 시점에 맞춰 제작 |
| **LearningParchment.png** / 생성 이미지 | 공장 양피지 수집물과 공장·태블릿 공용 ParchmentReader 프리팹 배경 | 사용자가 선택한 폐공장 홀로그램 양피지 시안을 ChatGPT 이미지 생성 도구로 글자 없는 투명 스프라이트로 편집 | 프로젝트용 생성 이미지. 외부 이미지 사용 없음. 제출 시 AI 생성물 고지 요건 확인 | 기존 종이 이미지를 교체하고 Unity 텍스트를 별도 겹침 |
| **ColorPhoto.png** / 생성 이미지 | CNNPixelLab 학습 이미지. 이전 오프닝 시안에서도 사용했으나 현재 오프닝에서는 제외 | 파일 내 생성 메타데이터에 **ChatGPT / gpt-image** 표기 | 생성 계정·원프롬프트·권리자·재배포 조건 미확인 | 이번 작업에서 이미지 수정 없음 |
| **MNIST 숫자 이미지·정답** / 데이터 및 표시 이미지 | `client/Assets/Data/CNNModelDesignLab/MnistTraining.bytes`, `MnistTest.bytes`; 기존 `Textures/CNNModelDesignLab/mnist-test-*.png` 제작 콘솔의 샘플 선택·예측 미리보기 | [MNIST 원출처](http://yann.lecun.com/exdb/mnist/), [Keras 공식 배포·권리 표기](https://keras.io/api/datasets/mnist/), [Keras 공식 로더](https://github.com/keras-team/keras/blob/v3.15.1/keras/src/datasets/mnist.py); 배포 URL: [mnist.npz](https://storage.googleapis.com/tensorflow/tf-keras-datasets/mnist.npz). 확인: 2026-10-24 | Keras 공식 문서 표기: **Yann LeCun, Corinna Cortes**, 원본 NIST 데이터의 파생 데이터. [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). 이 프로젝트의 MNIST 부분집합 데이터도 같은 조건으로 제공 | Codex 보조로 원본 training에서 숫자별 첫 50장(500장), test에서 숫자별 첫 20장(200장)을 추출하고 원본 인덱스·정답·픽셀을 `.bytes`로 포장. 생성·합성 이미지 아님. 픽셀 수정 없음. 기존 표시 PNG는 원본 test에서 숫자별 첫 이미지이며 이번 작업에서 변경 없음 |

Meta 컨트롤러 에셋은 좌·우 FBX(모델 내 애니메이션 포함), Animator Controller 2개, PNG 텍스처 5개, 재질 3개입니다. 주 원출처는 SDK의 `Meshes/MetaQuestTouchPlus`, `Textures/MetaQuestTouchPlus`이며, 모델이 참조하는 `batteryIndicator_mat.mat`와 `batteryIndicatorTexture32.png`는 각각 SDK의 `Meshes/MetaQuestTouchPro/Materials`, `Textures/MetaQuestTouchPro`에서 가져왔습니다. 모델 옆의 [LICENSE.md](../client/Assets/Meshes/Common/MetaQuestTouchPlus/LICENSE.md)와 [NOTICE.txt](../client/Assets/Meshes/Common/MetaQuestTouchPlus/NOTICE.txt)는 패키지 원문을 보존합니다. SDK 코드·네이티브 플러그인·셰이더 파일은 포함하지 않습니다.

Universal Animation Library에서 사용한 동작은 `Idle_Loop`, `Idle_Talking_Loop`, `Walk_Loop`, `Hit_Chest`, `Hit_Head`, `Crouch_Idle_Loop`, `Dance_Loop`입니다. 무료판에 전용 놀람 동작이 없어 충격·웅크림 동작을 조합했습니다. 관절 곡선을 변환한 런타임 데이터는 `client/Assets/Meshes/Common/QuaterniusVillageMotion.asset`에 두며, 제출용 사본·ZIP·별도 라이선스 보관 폴더는 만들지 않습니다.

폰트와 아이콘의 원래 저작권·라이선스 고지는 유지합니다. 생성 도구의 표기는 저작권자를 뜻하지 않으며, 생성 이미지의 메타데이터만으로 권리 귀속이나 재배포 허가를 확인한 것은 아닙니다.

## 제출 전 저작권 재검토 목록 (Meta 에셋 갱신: 2026-10-27)

이 구역은 확인된 근거와 남은 확인 사항을 구분한 제출 준비 기록이며, 모든 에셋의 권리 문제가 해소됐다는 보증은 아니다.

| 대상 | 확인한 근거·허용 범위 | 제출 전 남은 확인 |
| --- | --- | --- |
| Meta Touch Plus 모델·텍스처·애니메이션 | 207.0.1의 LICENSE.md·NOTICE.txt를 선택한 에셋과 함께 보존했다. 공식 SDK 약관 1.1, 1.1.1, 1.3과 컨트롤러 애니메이션 문서에서 앱·튜토리얼 내 사용 근거를 확인했다. | 최종 APK/소스 배포물에도 필요한 저작권 문구와 고지가 포함되는지 확인. APK 고지 포함은 검증하지 않음. 대회 제출 규정의 제3자 에셋 허용·AI 사용 고지 요건 별도 확인. |
| Meta SDK 유래 에셋의 분리·배포 | 현재 Quest 앱에서 사용하는 컨트롤러 에셋만 보존했다. 모델·텍스처·애니메이션은 원본 그대로 두고 재질의 Unity 셰이더 연결만 변경했다. 런타임 입력·표시는 기존 프로젝트 코드로 연결한다. | 에셋 분리는 권리나 라이선스 변경을 뜻하지 않는다. 모델 편집이나 앱과 무관한 독립 에셋 배포는 이 기록에서 허용 여부를 판단하지 않는다. 프로젝트의 오픈소스 라이선스를 일괄 적용하지 않기. |
| LearningParchment.png / ColorPhoto.png | 양피지 이미지는 사용자가 승인한 시안에서 글자를 제거한 생성 이미지로 교체. ColorPhoto.png는 기존 생성 도구 표기만 확인됨. | 대회 AI 생성물 고지 요구 확인. ColorPhoto.png의 생성 계정·원본 요청·제작 경위·사용 권한은 미확인 상태 유지. |
| Noto / Nanum / Lucide·Feather | 기존 표의 OFL/ISC/MIT 근거와 프로젝트 고지를 보존. 이번 튜토리얼·수집 안내에서도 기존 폰트를 재사용. | 최종 제출 파일에서 각 고지가 누락되지 않는지 확인. |
| 기타 공장 배경·음원·주민 애니메이션·MNIST | 위 표의 기록을 유지. | 제출 범위에 포함되는 모든 외부 에셋의 누락 여부와 실제 배포 고지 확인. 이번 컨트롤러 검토만으로 프로젝트 전체의 권리 확인이 완료된 것은 아님. |

Meta 확인 자료: [SDK 약관](https://developers.meta.com/vr/licenses/oculussdk/), [컨트롤러 애니메이션 사용 안내](https://developers.meta.com/vr/documentation/unity/controller-animations/). SDK 제거 후에도 선택한 런타임 에셋에 필요한 원래 LICENSE/Notice를 에셋 옆에 보존한다. 제출 기록용 ZIP·별도 라이선스 보관 폴더는 추가하지 않는다.
