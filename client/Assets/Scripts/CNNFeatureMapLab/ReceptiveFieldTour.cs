using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class ReceptiveFieldTour
    {
        private static readonly float[] Interval = { 0,.5f,.2f,.11f };
        private FeatureMapPanel[][] _maps;
        private FeatureMapPanel _input;
        private CorridorTheme _theme;
        [SerializeField] private Renderer _accumulated;
        [SerializeField] private Mesh _mesh;
        private Vector3[] _vertices = new Vector3[49*16];
        private int[] _triangles = new int[49*24];
        private int _size;
        private int _stage;
        private int _index;
        private int _count;
        private int _drawn;
        private int _firstRow;
        private int _lastRow;
        private int _firstColumn;
        private int _lastColumn;
        private float _start;
        private bool _running;


        internal void Bind(CorridorVisuals visuals, FeatureMapPanel input, FeatureMapPanel[][] maps, int size)
        {
            _theme = visuals.Theme;
            _input = input;
            _maps = maps;
            _size = size;
            _mesh = visuals.Own(Object.Instantiate(_mesh));
            _mesh.MarkDynamic();
            _accumulated.GetComponent<MeshFilter>().sharedMesh = _mesh;
            for (int quad = 0; quad < _triangles.Length / 6; quad++)
            {
                int vertex = quad * 4;
                int t = quad * 6;
                _triangles[t] = vertex;
                _triangles[t + 1] = vertex + 1;
                _triangles[t + 2] = vertex + 2;
                _triangles[t + 3] = vertex + 2;
                _triangles[t + 4] = vertex + 3;
                _triangles[t + 5] = vertex;
            }
            _accumulated.enabled = false;
        }

        private ReceptiveFieldTour() { }

#if UNITY_EDITOR
        internal ReceptiveFieldTour(CorridorVisuals visuals, Transform root, FeatureMapPanel input, FeatureMapPanel[][] maps, int size)
        {
            _theme = visuals.Theme;
            _input = input;
            _maps = maps;
            _size = size;
            _accumulated = visuals.Shape(root,"Accumulating cells",Vector3.zero,Vector3.one,Color.white);
            _mesh = visuals.Own(new Mesh { name = "Receptive field outlines" });
            _mesh.MarkDynamic();
            _accumulated.GetComponent<MeshFilter>().sharedMesh = _mesh;
            for (int quad = 0; quad < _triangles.Length/6; quad++)
            {
                int vertex = quad*4;
                int t = quad*6;
                _triangles[t] = vertex;
                _triangles[t+1] = vertex+1;
                _triangles[t+2] = vertex+2;
                _triangles[t+3] = vertex+2;
                _triangles[t+4] = vertex+3;
                _triangles[t+5] = vertex;
            }
            _accumulated.enabled = false;
        }
#endif

        internal void Show(int stage, int index)
        {
            Clear();
            _stage = stage;
            _index = index;
            for (int layer = 0; layer < stage; layer++) { ShowRange(layer); }
            if (stage == 0) { _input.Cell(index,Color.white); return; }
            _firstRow = Mathf.Max(0,index/_size-stage);
            _lastRow = Mathf.Min(_size-1,index/_size+stage);
            _firstColumn = Mathf.Max(0,index%_size-stage);
            _lastColumn = Mathf.Min(_size-1,index%_size+stage);
            _count = (_lastRow-_firstRow+1)*(_lastColumn-_firstColumn+1);
            _drawn = -1;
            _start = Time.unscaledTime;
            _running = true;
            FeatureMapPanel target = _maps[stage-1][0];
            Transform outline = _accumulated.transform;
            outline.SetParent(target.Root,false);
            outline.localPosition = new Vector3(0,0,-.033f);
            outline.localRotation = Quaternion.identity;
            _accumulated.enabled = true;
            Tick(_start);
        }

        internal void Tick(float now)
        {
            if (!_running) { return; }
            int step = Mathf.FloorToInt((now-_start)/Interval[_stage]);
            if (step >= _count) { Finish(); return; }
            if (step == _drawn) { return; }
            _drawn = step;
            FeatureMapPanel source = _stage == 1 ? _input : _maps[_stage-2][0];
            FeatureMapPanel target = _maps[_stage-1][0];
            int width = _lastColumn-_firstColumn+1;
            int row = _firstRow+step/width;
            int column = _firstColumn+step%width;
            source.Highlight(row-1,column-1,row+1,column+1,_theme.Amber);
            target.Cell(row*_size+column,_theme.Amber);
            float half = target.Width/_size*.5f;
            for (int i = 0; i < step; i++)
            {
                Vector3 cell = target.CellPosition((_firstRow+i/width)*_size+_firstColumn+i%width);
                DrawBox(i,cell,half);
            }
            _mesh.SetVertices(_vertices,0,step*16);
            _mesh.SetTriangles(_triangles,0,step*24,0,true);
        }

        private void DrawBox(int index, Vector3 cell, float radius)
        {
            const float stroke = .011f;
            for (int side = 0; side < 4; side++)
            {
                bool horizontal = side < 2;
                float x = cell.x+(horizontal ? 0 : (side-2.5f)*radius*2);
                float y = cell.y+(horizontal ? (side-.5f)*radius*2 : 0);
                float w = horizontal ? radius : stroke*.5f;
                float h = horizontal ? stroke*.5f : radius;
                int vertex = index*16+side*4;
                _vertices[vertex] = new Vector3(x-w,y-h,0);
                _vertices[vertex+1] = new Vector3(x-w,y+h,0);
                _vertices[vertex+2] = new Vector3(x+w,y+h,0);
                _vertices[vertex+3] = new Vector3(x+w,y-h,0);
            }
        }

        internal void Finish()
        {
            if (!_running) { return; }
            _running = false;
            _accumulated.enabled = false;
            ShowRange(_stage-1);
            ShowRange(_stage);
        }

        private void ShowRange(int layer)
        {
            int row = _index/_size;
            int col = _index%_size;
            if (layer == 0) { _input.Cell(_index,Color.white); return; }
            foreach (FeatureMapPanel panel in _maps[layer-1])
                panel.Highlight(row-layer,col-layer,row+layer,col+layer,Color.white);
        }

        internal void Clear()
        {
            // Drop old triangle indices before the next tour starts with zero vertices.
            _mesh.Clear();
            _running = false;
            _accumulated.enabled = false;
            _input.ClearHighlight();
            foreach (FeatureMapPanel[] layer in _maps)
            foreach (FeatureMapPanel panel in layer)
                panel.ClearHighlight();
        }
    }
}
