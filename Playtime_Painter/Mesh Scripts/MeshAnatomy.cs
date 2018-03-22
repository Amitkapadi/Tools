﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif
//using TextureEditor;
using StoryTriggerData;

namespace Playtime_Painter
{

    public static class MeshAnatomyExtensions {

        public static Vector3 SmoothVector (this List<trisDta> td) {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

    }

    public class UVpoint : abstract_STD {

        public override string getDefaultTagName() {
            return stdTag_uv;
        }
        public const string stdTag_uv = "uv";

        protected EditableMesh mesh { get { return MeshManager.inst._Mesh; } }
        protected PlaytimePainter target { get { return MeshManager.inst.target; } }


     
        int uvIndex;
        public int finalIndex;
        public Color _color;

        public bool tmpMark;
        public bool HasVertex;
        public List<trisDta> tris = new List<trisDta>();
        public UVpoint MyLastCopy;
        public vertexpointDta vert;

        public Vector3 pos { get { return vert.pos; } }


        void Init(vertexpointDta nvert)
        {
            vert = nvert;
            nvert.uv.Add(this);
        }

        void Init (UVpoint other) {
            _color = other._color;
          
        }

      

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("i", finalIndex);
            cody.Add("uvi", uvIndex);
            cody.Add("col", _color);


            return cody;
        }

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "i": finalIndex = data.ToInt(); mesh.uvsByFinalIndex[finalIndex] = this; break;
                case "uvi": uvIndex = data.ToInt(); break;
                case "col": _color = data.ToColor(); break;
            }
        }

        public UVpoint DeepCopyTo(vertexpointDta nvert)
        {
            UVpoint tmp = new UVpoint(nvert, GetUV(0), GetUV(1));
            tmp.finalIndex = finalIndex;
            tmp.uvIndex = uvIndex;
            tmp._color = _color;

            MyLastCopy = tmp;
            return tmp;
        }

        public UVpoint GetConnectedUVinVert(vertexpointDta other)
        {
            foreach (trisDta t in tris)
            {
                if (t.includes(other))
                    return (t.GetByVert(other));
            }
            return null;
        }

        public List<trisDta> getTrianglesFromLine(UVpoint other) {
            List<trisDta> lst = new List<trisDta>();

                foreach (var t in tris)
                    if (t.includes(other)) lst.Add(t);
            
            return lst;
        }

        public UVpoint() {
            Init(vertexpointDta.currentlyDecoded);
        }

        public UVpoint(UVpoint other)
        {
            Init(other);
            Init(other.vert);
            uvIndex = other.uvIndex;
            
        }

        public UVpoint(vertexpointDta nvert)
        {
            Init(nvert);

            if (vert.shared_v2s.Count == 0)
                SetUVindexBy(Vector2.one * 0.5f, Vector2.one * 0.5f);
            else
                uvIndex = 0;
        }

        public UVpoint(vertexpointDta nvert, Vector2 uv_0)
        {
            Init(nvert);
          
            SetUVindexBy(uv_0, Vector2.zero);
        }

        public UVpoint(vertexpointDta nvert, Vector2 uv_0, Vector2 uv_1)
        {
            Init(nvert);
            SetUVindexBy(uv_0, uv_1);
        }

        public UVpoint(vertexpointDta nvert, UVpoint other)
        {
            Init(other);
            Init(nvert);
            SetUVindexBy(other.GetUV(0), other.GetUV(1));
        }

        public UVpoint(vertexpointDta nvert, string data) {
            Init(nvert);
            Reboot(data);
        }

        public void AssignToNewVertex(vertexpointDta vp) {
            Vector2[] myUV = vert.shared_v2s[uvIndex];
            vert.uv.Remove(this);
            vert = vp;
            vert.uv.Add(this);
            SetUVindexBy(myUV);
        }

        public Vector2 editedUV {
            get {return vert.shared_v2s[uvIndex][MeshManager.editedUV]; }
            set { vert.shared_v2s[uvIndex][MeshManager.editedUV] = value; }
        }

        public Vector2 GetUV(int ind)
        {
            return vert.shared_v2s[uvIndex][ind];
        }

        public bool SameUV(Vector2 uv, Vector2 uv1)
        {
            return (((uv - GetUV(0)).magnitude < 0.0000001f) && ((uv1 - GetUV(1)).magnitude < 0.0000001f));
        }

        public void SetUVindexBy(Vector2[] uvs)
        {
            uvIndex = vert.getIndexFor(uvs[0], uvs[1]);
        }

        public void SetUVindexBy(Vector2 uv_0, Vector2 uv_1)
        {
            uvIndex = vert.getIndexFor(uv_0, uv_1);
        }

        public bool ConnectedTo(vertexpointDta other)
        {
            foreach (trisDta t in tris)
                if (t.includes(other))
                    return true;

            return false;
        }

        public bool ConnectedTo(UVpoint other)
        {
            foreach (trisDta t in tris)
                if (t.includes(other))
                    return true;

            return false;
        }

        public void setColor_OppositeTo(ColorChanel chan)
        {
            for (int i = 0; i < 3; i++)
            {
                ColorChanel c = (ColorChanel)i;
                c.SetChanel(ref _color, c == chan ? 0 : 1);
            }
        }

        public void FlipChanel(ColorChanel chan)
        {
            float val = _color.GetChanel(chan);
            val = (val > 0.9f) ? 0 : 1;
            chan.SetChanel(ref _color, val);
        }

        ColorChanel GetZeroChanelIfOne(ref int count)
        {
            count = 0;
            ColorChanel ch = ColorChanel.A;
            for (int i = 0; i < 3; i++)
                if (_color.GetChanel((ColorChanel)i) > 0.9f)
                    count++;
                else ch = (ColorChanel)i;

            return ch;
        }

        public ColorChanel GetZeroChanel_AifNotOne()
        {
            int count = 0;

            ColorChanel ch = GetZeroChanelIfOne(ref count);

            if (count == 2)
                return ch;
            else
            {
                foreach (UVpoint u in vert.uv) if (u != this)
                    {
                        ch = GetZeroChanelIfOne(ref count);
                        if (count == 2) return ch;
                    }
            }


            return ColorChanel.A;
        }
    }

    public class BlendFrame : abstract_STD
    {
        public Vector3 deltaPosition;
        public Vector3 deltaTangent;
        public Vector3 deltaNormal;

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "p": deltaPosition = data.ToVector3(); break;
                case "t": deltaTangent = data.ToVector3(); break;
                case "n": deltaNormal = data.ToVector3(); break;
            }
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder(); 

            cody.AddIfNotZero("p", deltaPosition);
            cody.AddIfNotZero("t", deltaTangent);
            cody.AddIfNotZero("n", deltaNormal);

            return cody;
        }

        public override string getDefaultTagName()
        {
            return tagName_bs;
        }

        public BlendFrame()
        {

        }

        public BlendFrame(Vector3 pos, Vector3 norm, Vector3 tang)
        {
            deltaPosition = pos;
            deltaNormal = norm;
            deltaTangent = tang;
        }

        public const string tagName_bs = "bs";    
  
    } 

    public class vertexpointDta : abstract_STD
    {
        protected EditableMesh mesh { get { return MeshManager.inst._Mesh; } }
        protected PainterConfig cfg { get { return PainterConfig.inst; } }

        public Vector3 normal;
        public int index;
        public bool NormalIsSet;
        public float distanceToPointed;
        public Vector3 distanceToPointedV3;
        public static vertexpointDta currentlyDecoded;

        // Data to save:
        public List<Vector2[]> shared_v2s = new List<Vector2[]>();
        public List<UVpoint> uv;
        public Vector3 pos;
        public bool SmoothNormal;
        public Vector4 shadowBake;
        public Countless<Vector3> anim;
        public BoneWeight boneWeight;
        public Matrix4x4 bindPoses;
        public List<List<BlendFrame>> shapes; // not currently working
        //public int submeshIndex;
        public float edgeStrength;

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            foreach (var lst in shared_v2s) {
                cody.Add("u0", lst[0]);
                cody.Add("u1", lst[1]);
            }

            cody.AddIfNotEmpty("uvs", uv);

            cody.Add("pos", pos);

            cody.Add("smth", SmoothNormal);

            cody.Add("shad", shadowBake);

            cody.Add("bw", boneWeight);

            cody.Add("biP", bindPoses);

            cody.Add("edge", edgeStrength);

            if (shapes != null)
                cody.AddIfNotEmpty("bl",shapes);

          
            return cody;
        }
       
        public override void Decode(string tag, string data) {
            switch (tag) {
                case "u0":  shared_v2s.Add(new Vector2[2]); 
                            shared_v2s.last()[0] = data.ToVector2(); break;
                case "u1":  shared_v2s.last()[1] = data.ToVector2(); break;
                case "uvs": currentlyDecoded = this;
                            uv = data.ToListOf_STD<UVpoint>(); break;
                case "pos": pos = data.ToVector3(); break;
                case "smth": SmoothNormal = data.ToBool(); break;
                case "shad": shadowBake = data.ToVector4(); break;
                case "bw": boneWeight = data.ToBoneWeight(); break;
                case "biP": bindPoses = data.ToMatrix4x4(); break;
                case BlendFrame.tagName_bs: shapes = data.ToListOfList_STD<BlendFrame>(); break;
             
                case "edge":  edgeStrength = data.ToFloat(); break;
            }
        }

        public override string getDefaultTagName() { return stdTag_vrt;}

        public const string stdTag_vrt = "vrt";

        public int getIndexFor(Vector2 uv_0, Vector2 uv_1)
        {
            int cnt = shared_v2s.Count;
            for (int i = 0; i < cnt; i++)
            {
                Vector2[] v2 = shared_v2s[i];
                if ((v2[0] == uv_0) && (v2[1] == uv_1))
                    return i;
            }

            Vector2[] tmp = new Vector2[2];
            tmp[0] = uv_0;
            tmp[1] = uv_1;

            shared_v2s.Add(tmp);

            return cnt;
        }

        public void AnimateTo(Vector3 dest) {
            dest -= pos;
            int no = 0;
            if (dest.magnitude > 0)
                anim[no] = dest;
            else anim[no] = Vector3.zero;

            if (dest.magnitude > 0) mesh.hasFrame[no] = true;
        }

        public Vector3 getWorldPos()  {

              PlaytimePainter emc = MeshManager.inst.target;
              if (emc.AnimatedVertices())  {
                  int animNo = emc.GetVertexAnimationNumber();
                  return emc.transform.TransformPoint(pos + anim[animNo]);
              }

              return emc.transform.TransformPoint(pos);
           
        }

        public void SetFromWorldSpace(Vector3 wPos)
        {
            pos = MeshManager.inst.target.transform.InverseTransformPoint(wPos);
        }

        public vertexpointDta() {
            Reboot(Vector3.zero);
        }

        public vertexpointDta(Vector3 npos) {
            Reboot(npos);
        }

        public void PixPerfect() {
            var trg = MeshManager.inst.target;

            if ((trg!= null) && (trg.curImgData!= null)){
                var width = trg.curImgData.width*2;
                var height = trg.curImgData.height*2;

                foreach (var v2a in shared_v2s)
                    for(int i=0; i<2; i++) {

                        var x = v2a[i].x;
                        var y = v2a[i].y;
                        x = Mathf.Round(x * width)/width;
                        y = Mathf.Round(y * height) / height;
                        v2a[i] = new Vector2(x, y);
                      //  Debug.Log("UV is "+v2a[i]);
                }


            }

        }

        void Reboot(Vector3 npos) {
            pos = npos;
            anim = new Countless<Vector3>();
            uv = new List<UVpoint>();
            shadowBake = Vector4.one;

            SmoothNormal = cfg.newVerticesSmooth;
        }



        public void clearColor(BrushMask bm) {
            foreach (UVpoint uvi in uv)
                bm.Transfer(ref uvi._color, Color.black);
        }

        void SetChanel(ColorChanel chan, vertexpointDta other, float val)
        {
            foreach (UVpoint u in uv)
                if (u.ConnectedTo(other))
                    chan.SetChanel(ref u._color, val);
        }

        public bool FlipChanelOnLine(ColorChanel chan, vertexpointDta other)
        {
            float val = 1;

            if (cfg.MakeVericesUniqueOnEdgeColoring)
               mesh.GiveLineUniqueVerticles_REFRESHTRISLISTING(new LineData(this, other));

            foreach (UVpoint u in uv)
                if (u.ConnectedTo(other))
                    val *= u._color.GetChanel(chan) * u.GetConnectedUVinVert(other)._color.GetChanel(chan);

            val = (val > 0.9f) ? 0 : 1;

            SetChanel(chan, other, val);
            other.SetChanel(chan, this, val);



            mesh.dirty = true;


            return (val == 1);
        }

        public void SetColorOnLine(Color col, BrushMask bm, vertexpointDta other)
        {
            foreach (UVpoint u in uv)
                if (u.ConnectedTo(other))
                    bm.Transfer(ref u._color, col);   //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVert(other)._color.GetChanel01(chan);

        }

        public void RemoveBorderFromLine(vertexpointDta other)
        {
            foreach (UVpoint u in uv)
                if (u.ConnectedTo(other))
                    for (int i = 0; i < 4; i++)
                    {
                        UVpoint ouv = u.GetConnectedUVinVert(other);
                        ColorChanel ch = (ColorChanel)i;

                        float val = u._color.GetChanel(ch) * ouv._color.GetChanel(ch);

                        if (val > 0.9f)
                        {
                             ch.SetChanel(ref u._color, 0);
                            ch.SetChanel(ref ouv._color, 0);
                        }
                    }

        }

        public vertexpointDta DeepCopy()
        {
            vertexpointDta nyu = new vertexpointDta(pos);
            nyu.SmoothNormal = SmoothNormal;
            nyu.distanceToPointed = distanceToPointed;
            nyu.distanceToPointedV3 = distanceToPointedV3;
            nyu.index = index;
            nyu.shadowBake = shadowBake;

            foreach (UVpoint u in uv)
            {
                u.DeepCopyTo(nyu);
            }

            foreach (Vector2[] v2a in shared_v2s)
            {
                Vector2[] tmp = new Vector2[2];
                tmp[0] = v2a[0];
                tmp[1] = v2a[1];
                nyu.shared_v2s.Add(tmp);
            }


            return nyu;
        }

        public float DistanceTo (vertexpointDta other) {
            return (pos - other.pos).magnitude;
        }

        public void MergeWith (vertexpointDta other) {

            for (int i = 0; i < other.uv.Count; i++) {
                UVpoint buv = other.uv[i];
                Vector2[] uvs = new Vector2[] { buv.GetUV(0), buv.GetUV(1) };
                uv.Add(buv);
                buv.vert = this;
                buv.SetUVindexBy(uvs);
            }

            mesh.vertices.Remove(other);

        }

        public void MergeWithNearest() {

            List<vertexpointDta> vrts = mesh.vertices;

            vertexpointDta nearest = null;
            float maxDist = float.MaxValue;

            foreach (vertexpointDta v in vrts) {
                float dist = v.DistanceTo(this);
                if ((dist < maxDist) && (v != this))
                {
                    maxDist = dist;
                    nearest = v;
                }
            }

            if (nearest != null)
                MergeWith(nearest);

        }

        public List<trisDta> triangles()
        {
            List<trisDta> Alltris = new List<trisDta>();


            foreach (UVpoint uvi in uv)
                foreach (trisDta tri in uvi.tris)
                    if (!Alltris.Contains(tri))
                        Alltris.Add(tri);


            return Alltris;
        }

        public List<LineData> GetAllLines_USES_Tris_Listing()
        {
            List<LineData> Alllines = new List<LineData>();


            foreach (UVpoint uvi in uv)
            {
                foreach (trisDta tri in uvi.tris)
                {
                    LineData[] lines;
                    lines = tri.GetLinesFor(uvi);

                    for (int i = 0; i < 2; i++)
                    {
                        bool same = false;
                        for (int j = 0; j < Alllines.Count; j++)
                        {
                            if (Alllines[j].SameVerticles(lines[i]))
                            {
                                Alllines[j].trianglesCount++;
                                same = true;
                            }
                        }
                        if (!same)
                            Alllines.Add(lines[i]);

                    }

                }
            }

            // Debug.Log("Found "+Alllines.Count + " Unique Lines ");

            return Alllines;
        }

        public trisDta getTriangleFromLine(vertexpointDta other)
        {
            for (int i = 0; i < uv.Count; i++)
            {
                for (int g = 0; g < uv[i].tris.Count; g++)
                    if (uv[i].tris[g].includes(other)) return uv[i].tris[g];
            }
            return null;
        }

        public List<trisDta> getTrianglesFromLine(vertexpointDta other) {
            List<trisDta> lst = new List<trisDta>();
            for (int i = 0; i < uv.Count; i++) {
                foreach (var t in uv[i].tris) 
                    if (t.includes(other)) lst.Add(t);
            }
            return lst;
        }

    }

    [Serializable]
    public class trisDta : abstract_STD {
        protected EditableMesh mesh { get { return MeshManager.inst._Mesh; } }

        public UVpoint[] uvpnts = new UVpoint[3];
        public bool[] SharpCorner = new bool[3];
        public Vector4 textureNo = new Vector4();
        public int submeshIndex;
        public Vector3 sharpNormal;

    

        public float area { get
            {
                return Vector3.Cross(uvpnts[0].pos - uvpnts[1].pos, uvpnts[0].pos - uvpnts[2].pos).magnitude * 0.5f;
               // V.magnitude * 0.5f;



            } }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddIfTrue("f0", SharpCorner[0]);
            cody.AddIfTrue("f1", SharpCorner[1]);
            cody.AddIfTrue("f2", SharpCorner[2]);

            for (int i = 0; i < 3; i++)
                cody.Add(i.ToString(),uvpnts[i].finalIndex);

            cody.Add("tex", textureNo);

            cody.AddIfNotZero("sub", submeshIndex);
           

            return cody;
        }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "tex": textureNo = data.ToVector4(); break;
                case "f0": SharpCorner[0] = true; break;
                case "f1": SharpCorner[1] = true; break;
                case "f2": SharpCorner[2] = true; break;
                case "sub": submeshIndex = data.ToInt(); break;
                default: uvpnts[tag.ToInt()] = mesh.uvsByFinalIndex[data.ToInt()]; break;
            }

        }

        public override string getDefaultTagName() {
            return stdTag_tri;
        }

        public const string stdTag_tri = "tri";

        public trisDta CopySettingsFrom (trisDta td) {
            for (int i = 0; i < 3; i++)
                SharpCorner[i] = td.SharpCorner[i];
            textureNo = td.textureNo;
            submeshIndex = td.submeshIndex;

            return this;
        }

        public bool wasProcessed;

        public bool isVertexIn(vertexpointDta vrt)
        {
            foreach (UVpoint v in uvpnts)
            {
                if (v.vert == vrt) return true;
            }
            return false;
        }

        public bool SetSharpCorners(bool to) {
            bool changed = false;
            for (int i = 0; i < 3; i++)
                if (SharpCorner[i] != to)
                {
                    changed = true;
                    SharpCorner[i] = to;
                }
            return changed;
        }

        public void InvertNormal()
        {
            UVpoint hold = uvpnts[0];

            uvpnts[0] = uvpnts[2];
            uvpnts[2] = hold;
        }

        public bool IsSamePoints(UVpoint[] other)
        {
            foreach (UVpoint v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v.vert == v1.vert) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameUV(UVpoint[] other)
        {
            foreach (UVpoint v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v == v1) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameAs(vertexpointDta[] other)
        {
            foreach (vertexpointDta v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v == v1.vert) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public void Change(UVpoint[] nvrts)
        {
            for (int i = 0; i < 3; i++)
            {
                nvrts[i].editedUV = uvpnts[i].editedUV;
                uvpnts[i] = nvrts[i];
            }
        }

        public bool includes(UVpoint vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == uvpnts[i]) return true;
            return false;
        }

        public bool includes(vertexpointDta vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == uvpnts[i].vert) return true;
            return false;
        }

        public bool includes(vertexpointDta a, vertexpointDta b)
        {
            int cnt = 0;
            for (int i = 0; i < 3; i++)
            {
                if ((a == uvpnts[i].vert) || (b == uvpnts[i].vert)) cnt++;
            }
            return cnt > 1;
        }

        public bool includes(LineData ld)
        {
            return (includes(ld.pnts[0].vert) && (includes(ld.pnts[1].vert)));
        }

        public bool PointOnTriangle()
        {
            Vector3 va = uvpnts[0].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[0].pos);
            Vector3 vb = uvpnts[1].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[1].pos);
            Vector3 vc = uvpnts[2].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[2].pos);

            float sum = Vector3.Angle(va, vb) + Vector3.Angle(va, vc) + Vector3.Angle(vb, vc);
            return (Mathf.Abs(sum - 360) < 1);
        }

        public int NumberOf(UVpoint pnt)
        {
            for (int i = 0; i < 3; i++)
                if (pnt == uvpnts[i]) return i;

            return -1;
        }

        public UVpoint GetClosestTo(Vector3 fpos)
        {

            UVpoint nearest = uvpnts[0];
            for (int i = 1; i < 3; i++)
                if ((fpos - uvpnts[i].pos).magnitude < (fpos - nearest.pos).magnitude) nearest = uvpnts[i];

            return nearest;

        }

        public UVpoint GetByVert(vertexpointDta vrt)
        {
            for (int i = 0; i < 3; i++)
                if (uvpnts[i].vert == vrt) return uvpnts[i];

            Debug.Log("Error using Get By Vert");
            return null;//uvpnts[0];
        }

        public Vector3 DistanceToWeight(Vector3 point)
        {

            Vector3 p1 = uvpnts[0].pos;
            Vector3 p2 = uvpnts[1].pos;
            Vector3 p3 = uvpnts[2].pos;

            Vector3 f1 = p1 - point;
            Vector3 f2 = p2 - point;
            Vector3 f3 = p3 - point;

            float a = Vector3.Cross(p2 - p1, p3 - p1).magnitude; // main triangle area a
            Vector3 p = new Vector3( 
                Vector3.Cross(f2, f3).magnitude / a,
                Vector3.Cross(f3, f1).magnitude / a,
                Vector3.Cross(f1, f2).magnitude / a // p3's triangle area / a
            );
            return p; 


            /* Vector3 dst = new Vector3(point.DistanceTo(uvpnts[0].pos),
              point.DistanceTo(uvpnts[1].pos),
              point.DistanceTo(uvpnts[2].pos)).normalized;

             return (uvpnts[0].v2 * (1 - dst.x) + uvpnts[1].v2 * (1 - dst.y) + uvpnts[2].v2 * (1 - dst.z)) / 2;*/

        }

        public void AssignWeightedData (UVpoint to, Vector3 weight) {
         
            to._color = uvpnts[0]._color * weight.x + uvpnts[1]._color * weight.y + uvpnts[2]._color * weight.z;
            to.vert.shadowBake = uvpnts[0].vert.shadowBake * weight.x + uvpnts[1].vert.shadowBake * weight.y + uvpnts[2].vert.shadowBake * weight.z;
            UVpoint nearest = (Mathf.Max(weight.x, weight.y) > weight.z)  ? (weight.x > weight.y ? uvpnts[0] : uvpnts[1]) : uvpnts[2];
            to.vert.boneWeight = nearest.vert.boneWeight; //boneWeight. * weight.x + uvpnts[1]._color * weight.y + uvpnts[2]._color * weight.z;
            //to.vert.submeshIndex = nearest.vert.submeshIndex;
        }

        public void Replace(UVpoint point, UVpoint with)
        {
            for (int i = 0; i < 3; i++)
                if (uvpnts[i] == point)
                {
                    uvpnts[i] = with;
                    return;
                }

        }

        public void Replace(int i, UVpoint with)
        {
            uvpnts[i].tris.Remove(this);
            uvpnts[i] = with;
            with.tris.Add(this);

        }

        public UVpoint NotOnLine(vertexpointDta a, vertexpointDta b)
        {
            for (int i = 0; i < 3; i++)
                if ((uvpnts[i].vert != a) && (uvpnts[i].vert != b))
                    return uvpnts[i];

            return uvpnts[0];
        }
        public UVpoint NotOnLine(LineData l)
        {
            for (int i = 0; i < 3; i++)
                if ((uvpnts[i].vert != l.pnts[0].vert) && (uvpnts[i].vert != l.pnts[1].vert))
                    return uvpnts[i];

            return uvpnts[0];
        }
        public UVpoint NotOneOf(UVpoint[] others)
        {
            for (int i = 0; i < 3; i++)
            {
                bool same;
                same = false;
                foreach (UVpoint uvi in others)
                    if (uvi.vert == uvpnts[i].vert) same = true;

                if (!same) return uvpnts[i];
            }
            return null;
        }

        public void GiveUniqueVerticesAgainst(trisDta td)
        {
            for (int i = 0; i < 3; i++)
            {
                UVpoint u = uvpnts[i];

                if (td.includes(u)) uvpnts[i] = new UVpoint(u.vert);


            }


        }

        public void MergeAround(trisDta other, vertexpointDta vrt)
        {
            // if (!includes(vrt)) Debug.Log("Error Using Merge Around");

            //Debug.Log("Using Merge Around");
            for (int i = 0; i < 3; i++)
            {
                if (!includes(other.uvpnts[i].vert))
                {

                    Replace(GetByVert(vrt), other.uvpnts[i]);
                    return;
                }
            }
            // Debug.Log("Done Merge Around");



        }

        public void MakeTriangleVertUnique(UVpoint pnt)
        {
            // bool duplicant = false;

            /* for (int i = 0; i < _Mesh.triangles.Count; i++) {
                 trisDta other = _Mesh.triangles[i];
                 if ((other.includes(pnt)) && (other != tris))   {
                     duplicant = true;
                     break;
                 }
             }*/
            //if (!duplicant) return;

            if (pnt.tris.Count == 1) return;

            UVpoint nuv = new UVpoint(pnt.vert, pnt);


            Replace(pnt, nuv);

            mesh.dirty = true;


        }


        public trisDta NewForCopiedVerticles()
        {
            UVpoint[] nvpnts = new UVpoint[3];

            for (int i = 0; i < 3; i++)
            {
                if (uvpnts[i].MyLastCopy == null) { Debug.Log("Error: UV has not been copied!"); return null; }

                nvpnts[i] = uvpnts[i].MyLastCopy;
            }

            return new trisDta(nvpnts);

        }

        public trisDta(UVpoint[] nvrts)
        {
            for (int i = 0; i < 3; i++)
            {
                uvpnts[i] = nvrts[i];
                uvpnts[i].tris.Add(this);
            }
        }

        public trisDta() {
        }

        public LineData[] GetLinesFor(UVpoint pnt)
        {

            LineData[] ld = new LineData[2];
            int no = NumberOf(pnt);
            ld[0] = new LineData(this, new UVpoint[] { uvpnts[no], uvpnts[(no + 1) % 3] });
            ld[1] = new LineData(this, new UVpoint[] { uvpnts[(no + 2) % 3], uvpnts[no] });

            return ld;


        }

    }

    [Serializable]
    public class LineData
    {
        public trisDta triangle;
        public UVpoint[] pnts = new UVpoint[2];
        public int trianglesCount;

        public bool includes(UVpoint uv)
        {
            return ((uv == pnts[0]) || (uv == pnts[1]));
        }

        public bool includes(vertexpointDta vp)
        {
            return ((vp == pnts[0].vert) || (vp == pnts[1].vert));
        }

        public bool SameVerticles(LineData other)
        {
            return (((other.pnts[0].vert == pnts[0].vert) && (other.pnts[1].vert == pnts[1].vert)) ||
                ((other.pnts[0].vert == pnts[1].vert) && (other.pnts[1].vert == pnts[0].vert)));
        }

        public LineData(vertexpointDta a, vertexpointDta b)
        {

            triangle = a.getTriangleFromLine(b);
            pnts[0] = a.uv[0];
            pnts[1] = b.uv[0];
            trianglesCount = 0;
        }

        public LineData(trisDta tri, UVpoint[] npoints)
        {
            triangle = tri;
            pnts[0] = npoints[0];
            pnts[1] = npoints[1];
            trianglesCount = 0;
        }

        public LineData(trisDta tri, UVpoint a, UVpoint b)
        {
            triangle = tri;
            pnts[0] = a;
            pnts[1] = b;

            trianglesCount = 0;
        }

        public trisDta getOtherTriangle() {
            foreach (UVpoint uv0 in pnts)
                foreach (UVpoint uv in uv0.vert.uv)
                    foreach (trisDta tri in uv.tris)
                        if (tri != triangle && tri.includes(pnts[0].vert) && tri.includes(pnts[1].vert))
                            return tri;


            return null;
            
        }

        public List<trisDta> getAllTriangles_USES_Tris_Listing()
        {
            List<trisDta> allTris = new List<trisDta>();

            foreach (UVpoint uv0 in pnts)
            {
                foreach (UVpoint uv in uv0.vert.uv)
                {
                    foreach (trisDta tri in uv.tris)
                    {

                        if ((allTris.Contains(tri) == false) && tri.includes(pnts[0].vert) && (tri.includes(pnts[1].vert)))
                            allTris.Add(tri);

                    }
                }
            }
            //Debug.Log("Found "+allTris.Count+ " tris for line");

            return allTris;
        }

        public Vector3 Vector()
        {
            return pnts[1].pos - pnts[0].pos;
        }

        public Vector3 HalfVectorToB(LineData other)
        {
            LineData LineA = this;
            LineData LineB = other;

            if (other.pnts[1] == pnts[0])
            {
                LineA = other;
                LineB = this;
            }

            Vector3 a = LineA.pnts[0].pos - LineA.pnts[1].pos;
            Vector3 b = LineB.pnts[1].pos - LineB.pnts[0].pos;

            //Debug.Log("Vectors A "+ a + " and B "+ b);

            Vector2 fromVector2 = GridNavigator.inst().InPlaneVector(a);
            Vector2 toVector2 = GridNavigator.inst().InPlaneVector(b);

            // Debug.Log("Vectors2 A " + fromVector2 + " and B " + toVector2);

            Vector2 mid = (fromVector2.normalized + toVector2.normalized).normalized;

            Vector3 cross = Vector3.Cross(fromVector2, toVector2);

            if (cross.z > 0)
                mid = -mid;




            return GridNavigator.inst().PlaneToWorldVector(mid).normalized;
        }

    }

}