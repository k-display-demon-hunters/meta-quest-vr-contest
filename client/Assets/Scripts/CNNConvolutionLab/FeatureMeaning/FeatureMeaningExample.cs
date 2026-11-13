namespace CNNConvolution
{
    // A 7×7 letter N and two 3×3 stroke filters for the closing scene that shows what outputs mean.
    internal static class FeatureMeaningExample
    {
        public const int PictureSize = 7;
        public const int MapSize = 5;
        public const int FilterCount = 2;

        private static readonly string[] FilterNames = { "대각선 필터", "세로 필터" };
        // Each filter marks three cells of its 3×3 window as (row, column).
        private static readonly int[][,] FilterCells =
        {
            new[,] { { 0, 0 }, { 1, 1 }, { 2, 2 } },
            new[,] { { 0, 1 }, { 1, 1 }, { 2, 1 } }
        };

        public static string FilterName(int filter) => FilterNames[filter];

        public static bool IsStroke(int row, int column) =>
            row >= 1 && row <= 5 && (column == 1 || column == 5 || column == row);

        public static bool IsFilterCell(int filter, int row, int column)
        {
            int[,] cells = FilterCells[filter];
            for (int cell = 0; cell < cells.GetLength(0); cell++)
            {
                if (cells[cell, 0] == row && cells[cell, 1] == column) { return true; }
            }
            return false;
        }

        // How many of the filter's cells lie on a stroke when its window starts at (row, column).
        public static int Response(int filter, int row, int column)
        {
            int[,] cells = FilterCells[filter];
            int response = 0;
            for (int cell = 0; cell < cells.GetLength(0); cell++)
            {
                if (IsStroke(row + cells[cell, 0], column + cells[cell, 1])) { response++; }
            }
            return response;
        }
    }
}
