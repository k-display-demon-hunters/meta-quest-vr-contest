using System;
using System.Collections.Generic;

namespace Parchment
{
    public enum ParchmentIllustrationType
    {
        PixelDigit,
        PaddingGrid,
        VerticalEdgeFilter,
        FeatureMaps,
        ReluGraph,
        GrayscaleDigit,
        RgbDigit
    }

    public sealed class ParchmentPage
    {
        public string Title { get; }
        public string Body { get; }
        public ParchmentIllustrationType Illustration { get; }

        public ParchmentPage(string body, ParchmentIllustrationType illustration, string title = null)
        {
            Title = title;
            Body = body;
            Illustration = illustration;
        }
    }

    public sealed class ParchmentLesson
    {
        public int Id { get; }
        public string Title { get; }
        public string Topic { get; }
        public string Department { get; }
        public IReadOnlyList<ParchmentPage> Pages { get; }

        public ParchmentLesson(int id, string title, string topic, string department,
            params ParchmentPage[] pages)
        {
            Id = id;
            Title = title;
            Topic = topic;
            Department = department;
            Pages = Array.AsReadOnly((ParchmentPage[])pages.Clone());
        }
    }

    public static class ParchmentCatalog
    {
        public static IReadOnlyList<ParchmentLesson> All { get; } = Array.AsReadOnly(new ParchmentLesson[]
        {
            new ParchmentLesson(0, "이미지와 픽셀", "픽셀 · 밝기 · RGB", "원본 검사 담당",
                new ParchmentPage("디지털 이미지는 작은 칸이 가로·세로로 늘어선 것이다. 이 칸 하나를 <b>픽셀(Pixel)</b>이라고 한다.\n8 × 8 이미지 = <b>64개</b>의 픽셀\n컴퓨터는 각 픽셀에 적힌 <b>숫자</b>를 입력으로 쓴다.", ParchmentIllustrationType.PixelDigit, "이미지와 픽셀"),
                new ParchmentPage("흑백 픽셀에는 <b>밝기 값 하나</b>가 들어 있다.\n<b>0 = 검정</b>, <b>255 = 하양</b>, 숫자가 클수록 밝다.", ParchmentIllustrationType.GrayscaleDigit, "흑백 이미지"),
                new ParchmentPage("컬러 픽셀은 <b>R, G, B 세 값</b>으로 색을 만든다. (255, 0, 0) = 빨강\n같은 색 값끼리 모은 숫자판 하나를 <b>채널(Channel)</b>이라고 한다.\nRGB 이미지 = <b>채널 3장</b>", ParchmentIllustrationType.RgbDigit, "컬러 이미지와 채널")),
            new ParchmentLesson(1, "특징 맵", "위치별 반응 · 출력 채널", "특징 분석 담당",
                new ParchmentPage("<b>특징 맵(Feature Map)</b>은 필터를 입력의 여러 위치에 적용한 계산 결과를 공간적 배열로 모은 것이다. 특징 맵의 각 칸에는 입력의 해당 영역에서 계산한 값이 들어 있다.\n\n세로 경계를 검출하는 필터를 적용했다면, 각 위치의 좌우 밝기 차이가 결과에 반영된다. 특징 맵은 원본의 색을 그대로 복사한 이미지가 아니다. 특정 필터가 입력의 어느 위치에서 얼마나 반응했는지를 나타낸다.", ParchmentIllustrationType.FeatureMaps),
                new ParchmentPage("일반적인 합성곱 층에서는 <b>필터 하나가 출력 채널 하나</b>를 만든다. 서로 다른 필터를 여러 개 적용하면 여러 채널의 특징 맵을 얻는다. 각 채널은 서로 다른 특징 정보를 담을 수 있다.\n\n특징 맵을 화면에 표시할 때는 숫자를 밝기나 색으로 바꾸기도 한다. 이때 화면의 밝기는 원본 픽셀의 밝기와 구분해야 한다. 또한 합성곱 결과에 활성화 함수를 적용한 배열도 흔히 특징 맵이라고 부른다.", ParchmentIllustrationType.FeatureMaps),
                new ParchmentPage("<b>실습: 특징 맵을 하나씩 껐다 켜며 비교한다.</b>\n\n맵 하나를 숨긴 뒤 어떤 경계나 무늬의 표시가 사라지는지 관찰한다. 다시 켜서 돌아오는 부분을 확인하면 각 맵이 담고 있는 정보를 비교할 수 있다. 같은 원본에서 만들어졌어도 맵마다 반응하는 위치는 다를 수 있다.\n\n이 실습에서 끄는 것은 <b>화면 표시</b>다. 해당 맵을 신경망의 실제 계산에서 제외하거나 신경망의 예측을 바꾸는 동작은 아니다.", ParchmentIllustrationType.FeatureMaps)),
        });

        public static ParchmentLesson Get(int id)
        {
            foreach (ParchmentLesson lesson in All)
            {
                if (lesson.Id == id) { return lesson; }
            }
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown parchment ID.");
        }
    }
}
