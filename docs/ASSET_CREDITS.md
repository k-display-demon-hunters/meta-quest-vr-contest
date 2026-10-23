# 콘텐츠 에셋 출처 및 저작권

외부 에셋(폰트·아이콘·애니메이션 포함)과 직접 생성한 미디어의 제출용 출처 목록입니다.

| 에셋명 / 종류 | 사용처 | 출처 또는 생성 방법 | 저작권·라이선스 | 수정 여부 |
| --- | --- | --- | --- | --- |
| **Noto Sans CJK KR Regular 2.004** / 폰트 | 시작 화면, 자막, UI | [Noto CJK 공식 배포본](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) | © 2014–2021 Adobe. [SIL Open Font License 1.1](https://github.com/notofonts/noto-cjk/blob/523d033d6cb47f4a80c58a35753646f5c3608a78/LICENSE) | 폰트 원본 수정 없음 |
| **Nanum Pen Script** / 폰트 | 태블릿의 양피지 문서 | [Google Fonts 공식 자료](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/METADATA.pb) | © 2010 NHN Corporation. 디자인: Sandoll Communications Inc. [SIL Open Font License 1.1](https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt) | 폰트 원본 수정 없음 |
| **Lucide / Feather 기반 아이콘** / 아이콘 | 태블릿 UI | [Lucide 공식 저장소 및 라이선스](https://github.com/lucide-icons/lucide/blob/e715245d62667c800e7f54c94b1b023692e900a3/LICENSE) | © 2026 Lucide Icons and Contributors, ISC. Feather 유래 도형: © 2013–present Cole Bemis, MIT | 선 도형을 간략화하고 아이콘 아틀라스로 변환 |
| **Universal Animation Library — Standard** / 주민 동작 | FactoryIntro·FactoryEnding 마을 주민의 걷기·시청·대화·충격 반응·축하 | [Quaternius 공식 배포 페이지](https://quaternius.itch.io/universal-animation-library), 무료 Standard 배포본 확인·다운로드: 2026-10-23 | 동봉 License/README의 제작자 표기: **Quaternius**. 공식 페이지에 애니메이터 **Gonzalo Furnier**의 제작 참여 표기. [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/): 수정·재배포·상업적 이용 허용 | 실제 관절 애니메이션을 Codex 보조로 기존 주민의 비율·관절에 맞게 변환하고 상태 전환을 혼합. 외부 캐릭터 메시·유료 Pro/Source 파일 사용 없음 |
| **VillageOpening.wav** / 음원 | 18초 마을 오프닝의 배경·효과음 | 프로젝트 작업에서 Codex 보조로 Python 사인파·필터링한 난수·음량 곡선을 직접 합성 | 프로젝트용 직접 제작 음원. 별도 공개 라이선스는 지정하지 않음. 외부 녹음·음악 샘플 사용 없음 | 오프닝의 공격·화면 고장 시점에 맞춰 제작 |
| **LearningParchment.png** / 생성 이미지 | 태블릿의 양피지 문서 | 파일 내 생성 메타데이터에 **ChatGPT / gpt-image** 표기 | 생성 계정·원프롬프트·권리자·재배포 조건 미확인 | 이번 작업에서 이미지 수정 없음 |
| **ColorPhoto.png** / 생성 이미지 | CNNPixelLab 학습 이미지. 이전 오프닝 시안에서도 사용했으나 현재 오프닝에서는 제외 | 파일 내 생성 메타데이터에 **ChatGPT / gpt-image** 표기 | 생성 계정·원프롬프트·권리자·재배포 조건 미확인 | 이번 작업에서 이미지 수정 없음 |

Universal Animation Library에서 사용한 동작은 `Idle_Loop`, `Idle_Talking_Loop`, `Walk_Loop`, `Hit_Chest`, `Hit_Head`, `Crouch_Idle_Loop`, `Dance_Loop`입니다. 무료판에 전용 놀람 동작이 없어 충격·웅크림 동작을 조합했습니다. 관절 곡선을 변환한 런타임 데이터는 `client/Assets/Meshes/Common/QuaterniusVillageMotion.asset`에 두며, 제출용 사본·ZIP·별도 라이선스 보관 폴더는 만들지 않습니다.

폰트와 아이콘의 원래 저작권·라이선스 고지는 유지합니다. 생성 도구의 표기는 저작권자를 뜻하지 않으며, 생성 이미지의 메타데이터만으로 권리 귀속이나 재배포 허가를 확인한 것은 아닙니다.
