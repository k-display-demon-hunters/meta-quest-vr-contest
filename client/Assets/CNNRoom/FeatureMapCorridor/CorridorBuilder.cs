using System;
using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    internal sealed class CorridorBuilder
    {
        private static readonly float[] Starts = { -9.5f,-5.2f,-.2f,6.4f,16.4f };
        private static readonly float[] Ends = { -5.6f,-1.2f,5.4f,15.2f,25.5f };
        private const float Width = 6.8f;
        private readonly CorridorVisuals _visuals;
        private readonly Transform _root;
        private readonly Mesh _cube;
        private readonly Matrix4x4[] _edgeLights = new Matrix4x4[70];
        private readonly Matrix4x4[] _stepLights = new Matrix4x4[108];
        private readonly Renderer[] _stepGlow = new Renderer[4];
        private readonly Renderer[] _zoneOverlay = new Renderer[5];
        private readonly Transform[] _zoneEdges = new Transform[5];
        private readonly TMP_Text[,] _names = new TMP_Text[3,2];
        private int _highlight = -1;
        internal event Action<int> ZonePressed;

        internal CorridorBuilder(CorridorVisuals visuals, Transform root, int[] mapCounts)
        {
            _visuals = visuals;
            _root = root;
            var grid = visuals.Shape(root,"Grid",new Vector3(0,-2.46f,8),new Vector3(8,37,1),new Color32(15,28,39,255),PrimitiveType.Quad);
            grid.transform.localRotation = Quaternion.Euler(90,0,0);
            Texture2D checker = visuals.Pixels(64);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                checker.SetPixel(x,y,(x+y)%2 == 0 ? new Color32(22,48,61,255) : new Color32(15,28,39,255));
            checker.Apply();
            visuals.Texture(grid,checker);
            for (int zone = 0; zone < Starts.Length; zone++)
            {
                int id = zone;
                float top = -.6f * zone;
                Renderer slab = BuildSlab(Starts[zone],Ends[zone],top,.05f);
                CorridorVisuals.Click(slab.GetComponent<Collider>(), _ => ZonePressed?.Invoke(id),trigger:false);
                _zoneOverlay[zone] = Overlay(Starts[zone],Ends[zone],top,.14f);
                _zoneEdges[zone] = CorridorVisuals.Group(root,"Zone border",Vector3.zero);
                Edges(_zoneEdges[zone],Starts[zone],Ends[zone],top,.045f,_visuals.Theme.Cyan,true);
                if (zone > 0 && zone < 4)
                {
                    float center = (Starts[zone]+Ends[zone])*.5f;
                    _names[zone-1,0] = FloorText(CorridorDialogue.Layer(zone),center+.32f,.53f,top);
                    _names[zone-1,1] = FloorText(CorridorDialogue.MapCount(mapCounts[zone-1]),center-.38f,.33f,top);
                }
                if (zone == Ends.Length-1) { continue; }
                float depth = (Starts[zone+1]-Ends[zone])/3;
                for (int step = 0; step < 3; step++)
                    BuildSlab(Ends[zone]+depth*step,Ends[zone]+depth*(step+1),top-.2f*(step+1),.035f);
                Renderer wash = Overlay(Ends[zone],Starts[zone+1],top-.3f,.09f);
                wash.transform.localRotation = Quaternion.Euler(90-Mathf.Atan2(.6f,depth*3)*Mathf.Rad2Deg,0,0);
                wash.transform.localScale = new Vector3(Width,Mathf.Sqrt(depth*depth*9+.36f),1);
                _stepGlow[zone] = wash;
            }
            _cube = _zoneEdges[0].GetComponentInChildren<MeshFilter>().sharedMesh;
            Highlight(-1);
        }

        private Renderer BuildSlab(float start, float end, float y, float edgeHeight)
        {
            Vector3 center = new Vector3(0,y-1.8f,(start+end)*.5f);
            _visuals.Shape(_root,"Slab",center,new Vector3(Width,3.6f,end-start),_visuals.Theme.SlabSide);
            Renderer cap = _visuals.Shape(_root,"Walk surface",new Vector3(0,y-.025f,center.z),
                new Vector3(Width,.05f,end-start),_visuals.Theme.SlabTop,collider:true,lit:true);
            Edges(_root,start,end,y,edgeHeight,_visuals.Theme.Edge,false);
            return cap;
        }

        private void Edges(Transform parent, float start, float end, float y, float height, Color color, bool uniform)
        {
            float center = (start+end)*.5f;
            _visuals.Shape(parent,"Front edge",new Vector3(0,y+.014f,end),new Vector3(Width,height,.035f),
                uniform ? color : (Color)_visuals.Theme.Cyan*2.5f);
            _visuals.Shape(parent,"Back edge",new Vector3(0,y+.014f,start),new Vector3(Width,.025f,.04f),color);
            for (int sign = -1; sign <= 1; sign += 2)
                _visuals.Shape(parent,"Side edge",new Vector3(sign*Width*.5f,y+.014f,center),
                    new Vector3(.04f,.025f,end-start),color);
        }

        private Renderer Overlay(float start, float end, float y, float alpha)
        {
            Color color = _visuals.Theme.Cyan;
            color.a = alpha;
            var renderer = _visuals.Shape(_root,"Zone wash",new Vector3(0,y+.015f,(start+end)*.5f),
                new Vector3(Width,end-start,1),color,PrimitiveType.Quad,transparent:true);
            renderer.transform.localRotation = Quaternion.Euler(90,0,0);
            return renderer;
        }

        private TMP_Text FloorText(string caption, float z, float size, float y)
        {
            TMP_Text label = _visuals.Label(_root,caption,new Vector3(0,y+.025f,z),size,6,new Color32(111,133,153,255),false);
            label.transform.localRotation = Quaternion.Euler(90,0,0);
            return label;
        }

        internal int HighlightedZone => _highlight;

        internal void Highlight(int zone)
        {
            _highlight = zone;
            for (int i = 0; i < _zoneOverlay.Length; i++)
            {
                _zoneOverlay[i].enabled = zone >= 0;
                Color wash = _visuals.Theme.Cyan;
                wash.a = i == zone ? .14f : .035f;
                _visuals.Paint(_zoneOverlay[i],wash);
                _zoneEdges[i].gameObject.SetActive(i == zone);
                if (i == 0 || i == 4) { continue; }
                for (int line = 0; line < 2; line++)
                {
                    TMP_Text label = _names[i-1,line];
                    label.color = i == zone ? _visuals.Theme.Cyan : new Color32(111,133,153,255);
                    label.transform.localScale = Vector3.one * (i == zone ? 1.08f : 1);
                }
            }
        }

        internal void DrawFlow(float time)
        {
            Matrix4x4 world = _root.localToWorldMatrix;
            for (int i = 0; i < _edgeLights.Length; i++)
            {
                float z = Mathf.Lerp(Starts[0],Ends[4],Mathf.Repeat(time/22+i/35f,1));
                float x = i < 35 ? -3.1f : 3.1f;
                _edgeLights[i] = world * Matrix4x4.TRS(new Vector3(x,Height(z)+.027f,z),
                    Quaternion.identity,new Vector3(.07f,.03f,.32f));
            }
            for (int zone = 0; zone < 4; zone++)
            {
                Color glow = _visuals.Theme.Cyan;
                glow.a = .095f + .025f*Mathf.Sin(time*2.1f+zone);
                _visuals.Paint(_stepGlow[zone],glow);
                for (int i = 0; i < 27; i++)
                {
                    float t = Mathf.Repeat(time/1.8f+i/9/3f+(i%9)*.019f,1);
                    float z = Mathf.Lerp(Ends[zone],Starts[zone+1],t);
                    float x = Mathf.Lerp(-2.95f,2.95f,(i%9)/8f);
                    _stepLights[zone*27+i] = world * Matrix4x4.TRS(new Vector3(x,Height(z)+.042f,z),
                        Quaternion.identity,new Vector3(.09f,.03f,.13f));
                }
            }
            _visuals.InstanceLights(_cube,_edgeLights,_edgeLights.Length,(Color)_visuals.Theme.Cyan*1.7f,_root.gameObject.layer);
            _visuals.InstanceLights(_cube,_stepLights,_stepLights.Length,new Color(.65f,1.12f,1.07f),_root.gameObject.layer);
        }

        private static float Height(float z)
        {
            for (int i = 0; i < Ends.Length-1; i++)
            {
                if (z <= Ends[i]) { return -.6f*i; }
                if (z < Starts[i+1]) { return -.6f*i-.2f*Mathf.Ceil(3*Mathf.InverseLerp(Ends[i],Starts[i+1],z)); }
            }
            return -2.4f;
        }
    }
}
