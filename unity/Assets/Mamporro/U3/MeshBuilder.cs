using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // La lógica usa las coordenadas de la web (three.js, dextrógiro). Para que el mundo se
    // vea igual y no en espejo, al pasar a Unity (levógiro) se invierte Z y el orden de
    // los triángulos. Este es el único punto de conversión del render.
    public static class WebSpace
    {
        public static Vector3 ToUnity(double x,double y,double z)=>new Vector3((float)x,(float)y,(float)-z);
        public static Color Linear(uint hex)=>new Color(((hex>>16)&255)/255f,((hex>>8)&255)/255f,(hex&255)/255f).linear;
    }

    // Vértice en espacio web.
    public struct WV { public double X,Y,Z; public WV(double x,double y,double z){X=x;Y=y;Z=z;} }

    // Acumula triángulos planos (sin índices compartidos) en espacio web y los emite a Unity.
    public sealed class MeshBuilder
    {
        readonly List<Vector3> positions=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector2> uvs=new List<Vector2>();
        readonly List<Vector4> wind=new List<Vector4>();
        public int VertexCount=>positions.Count;

        // a, b, c en sentido antihorario visto desde fuera (convenio de three.js).
        public void Triangle(WV a,WV b,WV c,Color ca,Color cb,Color cc,Vector2 ua,Vector2 ub,Vector2 uc,Vector4 wa,Vector4 wb,Vector4 wc)
        {
            Add(a,ca,ua,wa);Add(c,cc,uc,wc);Add(b,cb,ub,wb);
        }
        public void Triangle(WV a,WV b,WV c,Color color,Vector2 ua,Vector2 ub,Vector2 uc,Vector4 windData)=>Triangle(a,b,c,color,color,color,ua,ub,uc,windData,windData,windData);
        void Add(WV v,Color c,Vector2 uv,Vector4 w){positions.Add(WebSpace.ToUnity(v.X,v.Y,v.Z));colors.Add(c);uvs.Add(uv);wind.Add(w);}

        public Mesh Build(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=positions.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(positions);mesh.SetColors(colors);mesh.SetUVs(0,uvs);mesh.SetUVs(1,wind);
            var indices=new int[positions.Count];for(int i=0;i<indices.Length;i++)indices[i]=i;
            mesh.SetIndices(indices,MeshTopology.Triangles,0);mesh.RecalculateBounds();
            return mesh;
        }
    }

    // Geometrías locales equivalentes a las de three.js (centradas en el origen, espacio web).
    public sealed class Shape
    {
        public readonly List<WV> Vertices=new List<WV>();
        public readonly List<int> Triangles=new List<int>();
        public readonly List<uint> Colors=new List<uint>();
        int V(double x,double y,double z){Vertices.Add(new WV(x,y,z));return Vertices.Count-1;}
        void T(int a,int b,int c,uint color=0xffffff){Triangles.Add(a);Triangles.Add(b);Triangles.Add(c);Colors.Add(color);}

        public static Shape Box(double w,double h,double d)
        {
            var s=new Shape();double x=w/2,y=h/2,z=d/2;
            void Quad(WV a,WV b,WV c,WV e){int ia=s.V(a.X,a.Y,a.Z),ib=s.V(b.X,b.Y,b.Z),ic=s.V(c.X,c.Y,c.Z),ie=s.V(e.X,e.Y,e.Z);s.T(ia,ib,ic);s.T(ia,ic,ie);}
            Quad(new WV(x,-y,z),new WV(x,-y,-z),new WV(x,y,-z),new WV(x,y,z));
            Quad(new WV(-x,-y,-z),new WV(-x,-y,z),new WV(-x,y,z),new WV(-x,y,-z));
            Quad(new WV(-x,y,z),new WV(x,y,z),new WV(x,y,-z),new WV(-x,y,-z));
            Quad(new WV(-x,-y,-z),new WV(x,-y,-z),new WV(x,-y,z),new WV(-x,-y,z));
            Quad(new WV(-x,-y,z),new WV(x,-y,z),new WV(x,y,z),new WV(-x,y,z));
            Quad(new WV(x,-y,-z),new WV(-x,-y,-z),new WV(-x,y,-z),new WV(x,y,-z));
            return s;
        }

        // CylinderGeometry(radiusTop, radiusBottom, height, radialSegments): x = r·sin θ, z = r·cos θ.
        public static Shape Cylinder(double radiusTop,double radiusBottom,double height,int segments,bool caps=true)
        {
            var s=new Shape();double half=height/2;
            for(int k=0;k<segments;k++){
                double t0=k*Math.PI*2/segments,t1=(k+1)*Math.PI*2/segments;
                int a=s.V(radiusTop*Math.Sin(t0),half,radiusTop*Math.Cos(t0)),b=s.V(radiusBottom*Math.Sin(t0),-half,radiusBottom*Math.Cos(t0));
                int c=s.V(radiusBottom*Math.Sin(t1),-half,radiusBottom*Math.Cos(t1)),d=s.V(radiusTop*Math.Sin(t1),half,radiusTop*Math.Cos(t1));
                if(radiusTop>0)s.T(a,b,d);
                if(radiusBottom>0)s.T(b,c,d);
                if(caps&&radiusTop>0){int ct=s.V(0,half,0);s.T(ct,a,d);}
                if(caps&&radiusBottom>0){int cb=s.V(0,-half,0);s.T(cb,c,b);}
            }
            return s;
        }

        static Shape Polyhedron(double[] v,int[] idx,double radius)
        {
            var s=new Shape();
            for(int i=0;i<v.Length;i+=3){double l=Math.Sqrt(v[i]*v[i]+v[i+1]*v[i+1]+v[i+2]*v[i+2]);s.V(v[i]/l*radius,v[i+1]/l*radius,v[i+2]/l*radius);}
            for(int i=0;i<idx.Length;i+=3)s.T(idx[i],idx[i+1],idx[i+2]);
            return s;
        }
        static readonly double Phi=(1+Math.Sqrt(5))/2;
        public static Shape Icosahedron(double radius)=>Polyhedron(new[]{-1,Phi,0,1,Phi,0,-1,-Phi,0,1,-Phi,0,0,-1,Phi,0,1,Phi,0,-1,-Phi,0,1,-Phi,Phi,0,-1,Phi,0,1,-Phi,0,-1,-Phi,0,1},
            new[]{0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1},radius);
        public static Shape Dodecahedron(double radius)
        {
            double t=Phi,r=1/t;
            return Polyhedron(new[]{-1,-1,-1,-1,-1,1,-1,1,-1,-1,1,1,1,-1,-1,1,-1,1,1,1,-1,1,1,1,0,-r,-t,0,-r,t,0,r,-t,0,r,t,-r,-t,0,-r,t,0,r,-t,0,r,t,0,-t,0,-r,t,0,-r,-t,0,r,t,0,r},
                new[]{3,11,7,3,7,15,3,15,13,7,19,17,7,17,6,7,6,15,17,4,8,17,8,10,17,10,6,8,0,16,8,16,2,8,2,10,0,12,1,0,1,18,0,18,16,6,10,2,6,2,13,6,13,15,2,16,18,2,18,3,2,3,13,18,1,9,18,9,11,18,11,3,4,14,12,4,12,0,4,0,8,11,9,5,11,5,19,11,19,7,19,5,14,19,14,4,19,4,17,1,12,14,1,14,5,1,5,9},radius);
        }
        public static Shape Octahedron(double radius)=>Polyhedron(new double[]{1,0,0,-1,0,0,0,1,0,0,-1,0,0,0,1,0,0,-1},new[]{0,2,4,0,4,3,0,3,5,0,5,2,1,2,5,1,5,3,1,3,4,1,4,2},radius);

        public Shape Scale(double sx,double sy,double sz){for(int i=0;i<Vertices.Count;i++){var v=Vertices[i];Vertices[i]=new WV(v.X*sx,v.Y*sy,v.Z*sz);}return this;}
        public Shape Translate(double x,double y,double z){for(int i=0;i<Vertices.Count;i++){var v=Vertices[i];Vertices[i]=new WV(v.X+x,v.Y+y,v.Z+z);}return this;}
        public Shape Tint(uint color){for(int i=0;i<Colors.Count;i++)Colors[i]=color;return this;}
        public Shape Merge(Shape other){int offset=Vertices.Count;Vertices.AddRange(other.Vertices);foreach(int t in other.Triangles)Triangles.Add(t+offset);Colors.AddRange(other.Colors);return this;}

        // Desplaza al azar cada posición distinta (jitterVertices de la web): las caras siguen unidas.
        public Shape Jitter(Mamporro.Core.Rng rng,double amount)
        {
            var offsets=new Dictionary<string,WV>();
            for(int i=0;i<Vertices.Count;i++){
                var v=Vertices[i];string key=v.X.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+v.Y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+v.Z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
                if(!offsets.TryGetValue(key,out var o)){o=new WV(rng.Range(-amount,amount),rng.Range(-amount,amount),rng.Range(-amount,amount));offsets[key]=o;}
                Vertices[i]=new WV(v.X+o.X,v.Y+o.Y,v.Z+o.Z);
            }
            return this;
        }
    }

    // Transformación de una pieza: giro en orden YXZ de three.js (R = Ry·Rx·Rz), escala y posición.
    public struct WebTransform
    {
        double m00,m01,m02,m10,m11,m12,m20,m21,m22,tx,ty,tz;
        public static WebTransform Of(double px,double py,double pz,double rx,double ry,double rz,double sx=1,double sy=1,double sz=1)
        {
            double cx=Math.Cos(rx),sxr=Math.Sin(rx),cy=Math.Cos(ry),syr=Math.Sin(ry),cz=Math.Cos(rz),szr=Math.Sin(rz);
            // Ry·Rx·Rz
            double a00=cy*cz+syr*sxr*szr,a01=-cy*szr+syr*sxr*cz,a02=syr*cx;
            double a10=cx*szr,a11=cx*cz,a12=-sxr;
            double a20=-syr*cz+cy*sxr*szr,a21=syr*szr+cy*sxr*cz,a22=cy*cx;
            return new WebTransform{m00=a00*sx,m01=a01*sy,m02=a02*sz,m10=a10*sx,m11=a11*sy,m12=a12*sz,m20=a20*sx,m21=a21*sy,m22=a22*sz,tx=px,ty=py,tz=pz};
        }
        public WV Apply(WV v)=>new WV(m00*v.X+m01*v.Y+m02*v.Z+tx,m10*v.X+m11*v.Y+m12*v.Z+ty,m20*v.X+m21*v.Y+m22*v.Z+tz);
    }
}
