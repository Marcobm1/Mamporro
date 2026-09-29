using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Construcciones y objetos del mapa (src/world/props.ts) como piezas simples con
    // color y, cuando hace falta, colisionador. Lógica pura: el mesh se construye aparte.
    public sealed class Part
    {
        public string Shape,Material;
        public uint Color;
        // box: ancho/alto/fondo · cylinder: radio abajo/alto/radio arriba · cone: radio/alto/- · ico/dodeca: radio/escala Y/-
        public double[] Size;
        public double[] Position;
        // Orden YXZ: primero la inclinación local, después el giro.
        public double[] Rotation;
        public int Segments;
    }

    public sealed class PropSet
    {
        public readonly List<Part> Parts=new List<Part>();
        public readonly List<Collider> Colliders=new List<Collider>();
    }

    public struct PartOptions
    {
        public string Material;
        public double RotX,RotY,RotZ;
        public bool Collide;
        public bool? Standable;
        public int? Segments;
        public double? RadiusTop;
        public string BlobShape;
        public double? ScaleY;
    }

    public sealed class PropBuilder
    {
        readonly PropSet output;
        double cos=1,sin=0,ox,oy,oz,rot;
        public PropBuilder(PropSet output){this.output=output;}

        public PropBuilder At(double x,double y,double z,double rotation){ox=x;oy=y;oz=z;rot=rotation;cos=Math.Cos(rotation);sin=Math.Sin(rotation);return this;}
        void World(double lx,double lz,out double wx,out double wz){wx=ox+lx*cos+lz*sin;wz=oz-lx*sin+lz*cos;}

        Part Add(string shape,double lx,double ly,double lz,double[] size,uint color,PartOptions o)
        {
            World(lx,lz,out double wx,out double wz);
            var part=new Part{Shape=shape,Material=o.Material??"plain",Color=color,Size=size,Position=new[]{wx,oy+ly,wz},
                Rotation=new[]{o.RotX,rot+o.RotY,o.RotZ},Segments=o.Segments??7};
            output.Parts.Add(part);return part;
        }

        public void Box(double lx,double ly,double lz,double sx,double sy,double sz,uint color,PartOptions o=default)
        {
            var p=Add("box",lx,ly,lz,new[]{sx,sy,sz},color,o);
            if(o.Collide&&o.RotX==0&&o.RotZ==0)
                output.Colliders.Add(Collider.Oriented(p.Position[0],p.Position[2],sx/2,sz/2,p.Rotation[1],p.Position[1]-sy/2,p.Position[1]+sy/2,o.Standable??true));
        }

        public void Cylinder(double lx,double ly,double lz,double radius,double height,uint color,PartOptions o=default)
        {
            double top=o.RadiusTop??radius;
            var p=Add("cylinder",lx,ly,lz,new[]{radius,height,top},color,o);
            if(o.Collide&&o.RotX==0&&o.RotZ==0)
                output.Colliders.Add(Collider.Circle(p.Position[0],p.Position[2],Math.Max(radius,top),p.Position[1]-height/2,p.Position[1]+height/2,o.Standable??true));
        }

        // Cilindro tumbado a lo largo del eje X local (troncos, columnas caídas...).
        public void LyingCylinder(double lx,double ly,double lz,double radius,double length,uint color,PartOptions o=default)
        {
            var visual=o;visual.RotZ=Math.PI/2;visual.Collide=false;
            var p=Add("cylinder",lx,ly,lz,new[]{radius,length,radius},color,visual);
            if(o.Collide)
                output.Colliders.Add(Collider.Oriented(p.Position[0],p.Position[2],length/2,radius*0.9,p.Rotation[1],p.Position[1]-radius,p.Position[1]+radius,o.Standable??true));
        }

        // Solo colisión, sin pieza visible.
        public void SolidBox(double lx,double ly,double lz,double sx,double sy,double sz,double rotY=0,bool standable=true)
        {
            World(lx,lz,out double wx,out double wz);
            output.Colliders.Add(Collider.Oriented(wx,wz,sx/2,sz/2,rot+rotY,oy+ly-sy/2,oy+ly+sy/2,standable));
        }

        public void Cone(double lx,double ly,double lz,double radius,double height,uint color,PartOptions o=default)=>Add("cone",lx,ly,lz,new[]{radius,height,0},color,o);
        public void Blob(double lx,double ly,double lz,double radius,uint color,PartOptions o=default)=>Add(o.BlobShape??"dodeca",lx,ly,lz,new[]{radius,o.ScaleY??1,0},color,o);
    }

    public static class Props
    {
        static readonly uint[] Stones={Palette.StoneLight,Palette.Stone,Palette.StoneDark};
        static uint Stone(Rng rng)=>rng.Pick(Stones);
        static readonly int[] Signs={-1,1};

        // Muro en ruinas de (x0, z0) a (x1, z1): columnas irregulares, derrumbes, ventanas y puerta opcional.
        static void RuinedWall(PropBuilder b,Rng rng,double x0,double z0,double x1,double z1,double height,bool door=false,double thickness=0.45,bool windows=true)
        {
            double dx=x1-x0,dz=z1-z0,length=JsMath.Hypot(dx,dz);
            int n=Math.Max(2,(int)Rules.Round(length/0.95));
            double colW=length/n,rotY=Math.Atan2(-dz/length,dx/length);
            for(int k=0;k<n;k++){
                double t=(k+0.5)/n,edge=1-2*Math.Min(t,1-t);
                double h=WorldMath.Lerp(0.4,1,Math.Pow(edge,0.7))+rng.Range(-0.18,0.18);
                bool collapsed=rng.Chance(0.2)&&edge<0.7,window=rng.Chance(0.35);uint shade=Stone(rng);
                if(door&&Math.Abs(t-0.5)*length<0.75)continue;
                if(collapsed)h*=0.3;
                h=WorldMath.Clamp(h,0.25,1)*height;
                double cx=x0+dx*t,cz=z0+dz*t;
                var o=new PartOptions{Material="stone",RotY=rotY,Collide=true};
                if(windows&&window&&h>2.3&&edge<0.6){
                    b.Box(cx,0.45,cz,colW+0.02,0.9,thickness,shade,o);
                    double top=h-1.8;
                    b.Box(cx,1.8+top/2,cz,colW+0.02,top,thickness,shade,o);
                }else b.Box(cx,h/2,cz,colW+0.02,h,thickness,shade,o);
            }
        }

        static void Rubble(PropBuilder b,Rng rng,int count,double spreadX,double spreadZ,double cx=0,double cz=0)
        {
            for(int i=0;i<count;i++){
                double r=rng.Range(0.12,0.32);
                double x=cx+rng.Range(-spreadX,spreadX),z=cz+rng.Range(-spreadZ,spreadZ);
                b.Blob(x,r*0.5,z,r,Stone(rng),new PartOptions{ScaleY=0.7});
            }
        }

        static void Barrel(PropBuilder b,double x,double z,double y=0)
        {
            b.Cylinder(x,y+0.475,z,0.38,0.95,Palette.Wood,new PartOptions{RadiusTop=0.34,Collide=true});
            b.Cylinder(x,y+0.25,z,0.395,0.06,Palette.WoodDark);
            b.Cylinder(x,y+0.72,z,0.37,0.06,Palette.WoodDark);
        }

        static void Crate(PropBuilder b,Rng rng,double x,double z,double y=0,double size=0.8)=>
            b.Box(x,y+size/2,z,size,size,size,Palette.WoodLight,new PartOptions{RotY=rng.Range(-0.4,0.4),Collide=true});

        static void BuildHouse(PropBuilder b,Rng rng)
        {
            double w=rng.Range(6.2,8),d=rng.Range(4.6,6),t=0.45,h=rng.Range(2.7,3.3);
            RuinedWall(b,rng,-w/2-t/2,-d/2,w/2+t/2,-d/2,h,door:true);
            RuinedWall(b,rng,w/2+t/2,d/2,-w/2-t/2,d/2,h);
            RuinedWall(b,rng,w/2,-d/2+t/2,w/2,d/2-t/2,h);
            RuinedWall(b,rng,-w/2,d/2-t/2,-w/2,-d/2+t/2,h);
            int side=rng.Chance(0.5)?1:-1;
            b.Box(side*(w/2-0.35),(h+1.3)/2,d/2-0.35,0.9,h+1.3,0.9,Palette.StoneDark,new PartOptions{Material="stone",Collide=true});
            double roofX=rng.Range(-0.8,0.8),roofRot=rng.Range(-0.2,0.2);
            b.Box(roofX,h*0.42,d*0.12,w*0.55,0.12,d*0.7,Palette.Roof,new PartOptions{RotX=-0.62,RotY=roofRot});
            double beamX=rng.Range(-1,1),beamZ=rng.Range(-0.8,0.8),beamRot=Math.PI/2+rng.Range(-0.5,0.5);
            b.Box(beamX,0.09,beamZ,0.16,0.16,w*0.85,Palette.WoodDark,new PartOptions{RotY=beamRot});
            double fallenX=-side*rng.Range(0.5,1.5),fallenRot=rng.Range(-0.3,0.3);
            b.Box(fallenX,h*0.35,0,0.16,0.16,d*0.9,Palette.WoodDark,new PartOptions{RotX=0.5,RotY=fallenRot});
            double tx=rng.Range(-w/4,w/4),tz=rng.Range(-d/5,d/6);
            b.Box(tx,0.55,tz,1.2,0.08,0.7,Palette.Wood,new PartOptions{RotZ=0.35});
            b.Box(tx+0.5,0.35,tz,0.08,0.7,0.08,Palette.WoodDark);
            for(int i=0;i<3;i++){
                bool onFront=rng.Chance(0.5);int sign=rng.Pick(Signs);double inset=rng.Range(0.35,0.8);
                double ix=onFront?sign*(w/2-inset):sign*(w/2+t/2+0.02);
                double iz=onFront?-d/2-t/2-0.02:rng.Pick(Signs)*(d/2-inset);
                double iy=rng.Range(0.6,1.2),ih=rng.Range(0.7,1.3);
                b.Box(ix,iy,iz,onFront?0.9:0.04,ih,onFront?0.04:0.9,Palette.Ivy);
            }
            double bushX=rng.Range(-w/4,w/4),bushZ=rng.Range(0,d/4);
            b.Blob(bushX,0.35,bushZ,0.55,Palette.Bush,new PartOptions{BlobShape="ico",ScaleY=0.75});
            Rubble(b,rng,9,w/2+0.8,d/2+0.8);
            if(rng.Chance(0.6))Barrel(b,rng.Range(-w/2,-1),-d/2-1.1,0);
            else Crate(b,rng,rng.Range(1,w/2),-d/2-1.1);
        }

        static void BuildTemple(PropBuilder b,Rng rng)
        {
            double r=rng.Range(4.4,5.2),platform=0.45;
            b.Cylinder(0,platform/2,0,r+1.2,platform,Palette.StoneLight,new PartOptions{Material="stone",Collide=true,Segments=12});
            const int n=8;var intact=new bool[n];double angle0=rng.Next()*Math.PI;
            for(int k=0;k<n;k++){
                double a=angle0+(k/(double)n)*Math.PI*2,x=Math.Cos(a)*r,z=Math.Sin(a)*r;
                bool missing=rng.Chance(0.22),broken=rng.Chance(0.45);
                double h=broken?rng.Range(0.8,2.3):4.2;
                intact[k]=!missing&&!broken;
                if(missing){Rubble(b,rng,3,0.5,0.5,x,z);continue;}
                b.Box(x,platform+0.12,z,0.85,0.25,0.85,Palette.Stone,new PartOptions{Material="stone",RotY=-a});
                b.Cylinder(x,platform+h/2,z,0.34,h,Palette.StoneLight,new PartOptions{Material="stone",Collide=true,Standable=broken});
                if(!broken)b.Box(x,platform+h+0.15,z,0.9,0.3,0.9,Palette.Stone,new PartOptions{Material="stone",RotY=-a});
            }
            for(int k=0;k<n;k++){
                int next=(k+1)%n;
                if(!intact[k]||!intact[next])continue;
                double a0=angle0+(k/(double)n)*Math.PI*2,a1=angle0+(next/(double)n)*Math.PI*2;
                double x0=Math.Cos(a0)*r,z0=Math.Sin(a0)*r,x1=Math.Cos(a1)*r,z1=Math.Sin(a1)*r;
                double len=JsMath.Hypot(x1-x0,z1-z0)+0.8;
                b.Box((x0+x1)/2,platform+4.55,(z0+z1)/2,len,0.45,0.8,Palette.Stone,new PartOptions{Material="stone",RotY=Math.Atan2(-(z1-z0),x1-x0),Collide=true});
            }
            b.Box(0,platform+0.6,0,1.1,1.2,1.1,Palette.Stone,new PartOptions{Material="stone",Collide=true});
            b.Box(-0.18,platform+1.65,0,0.24,0.9,0.3,Palette.Marble);
            b.Box(0.18,platform+1.65,0,0.24,0.9,0.3,Palette.Marble);
            b.Box(0,platform+2.2,0,0.6,0.25,0.34,Palette.Marble,new PartOptions{RotZ=0.1});
            double fa=rng.Next()*Math.PI*2,axis=fa+Math.PI/2,fx=Math.Cos(fa)*(r+3),fz=Math.Sin(fa)*(r+3);
            b.LyingCylinder(fx,0.34,fz,0.34,2.4,Palette.StoneLight,new PartOptions{Material="stone",RotY=axis,Collide=true});
            b.LyingCylinder(fx+Math.Cos(axis)*2.2,0.34,fz-Math.Sin(axis)*2.2,0.34,1.3,Palette.StoneLight,new PartOptions{Material="stone",RotY=axis+0.35,Collide=true});
            Rubble(b,rng,6,r+2,r+2);
        }

        static void BuildFarm(PropBuilder b,Rng rng)
        {
            double size=rng.Range(8,9.5),half=size/2;
            int posts=(int)Rules.Round(size/1.6);double step=size/posts;
            double[][] sides={new[]{-half,-half,half,-half},new[]{half,-half,half,half},new[]{half,half,-half,half},new[]{-half,half,-half,-half}};
            for(int sideIndex=0;sideIndex<4;sideIndex++){
                double x0=sides[sideIndex][0],z0=sides[sideIndex][1],x1=sides[sideIndex][2],z1=sides[sideIndex][3];
                double dirX=(x1-x0)/size,dirZ=(z1-z0)/size,rotY=Math.Atan2(-dirZ,dirX);
                for(int k=0;k<posts;k++){
                    double px=x0+dirX*step*k,pz=z0+dirZ*step*k;
                    double rx=rng.Range(-0.12,0.12),rz=rng.Range(-0.12,0.12);
                    b.Box(px,0.55,pz,0.14,1.1,0.14,Palette.WoodDark,new PartOptions{RotX=rx,RotZ=rz});
                    bool gate=sideIndex==0&&k==posts/2;
                    if(gate||rng.Chance(0.18))continue;
                    double mx=px+(dirX*step)/2,mz=pz+(dirZ*step)/2;
                    b.Box(mx,0.45,mz,step,0.1,0.06,Palette.Wood,new PartOptions{RotY=rotY});
                    b.Box(mx,0.85,mz,step,0.1,0.06,Palette.Wood,new PartOptions{RotY=rotY,RotX=rng.Chance(0.2)?0.25:0});
                    b.SolidBox(mx,0.5,mz,step,1,0.2,rotY);
                }
            }
            b.Cylinder(-half/2,0.6,half/3,1.1,1.2,Palette.Straw,new PartOptions{Collide=true});
            b.Cone(-half/2,1.6,half/3,1.18,0.8,Palette.StrawDark);
            b.Box(half/3,0.62,-half/4,1.8,0.45,1.1,Palette.Wood,new PartOptions{RotZ=-0.14,RotY=0.3});
            b.Cylinder(half/3+0.2,0.42,-half/4-0.65,0.42,0.1,Palette.WoodDark,new PartOptions{RotX=Math.PI/2,RotY=0.3,Segments=8});
            b.Cylinder(half/3+1.4,0.05,-half/4+0.9,0.42,0.1,Palette.WoodDark,new PartOptions{Segments=8});
            b.SolidBox(half/3,0.5,-half/4,1.8,1,1.1,0.3);
            double sx=rng.Range(-1,1),sz=-half/3;
            b.Box(sx,1.1,sz,0.12,2.2,0.12,Palette.WoodDark,new PartOptions{Collide=true,Standable=false});
            b.Box(sx,1.62,sz,1.6,0.1,0.1,Palette.WoodDark);
            b.Box(sx,1.42,sz,0.62,0.7,0.32,Palette.Plaid);
            b.Blob(sx,2.02,sz,0.23,Palette.Burlap,new PartOptions{BlobShape="ico"});
            b.Cylinder(sx,2.2,sz,0.36,0.04,Palette.Hat,new PartOptions{Segments=8});
            b.Cylinder(sx,2.33,sz,0.2,0.24,Palette.Hat,new PartOptions{Segments=8});
            Barrel(b,half-0.8,half-0.9);
            Barrel(b,half-1.7,half-0.7);
            Crate(b,rng,-half+0.9,-half+0.9);
            Crate(b,rng,-half+1.8,-half+0.9);
            Crate(b,rng,-half+0.9,-half+0.9,0.8);
        }

        static void BuildWell(PropBuilder b,Rng rng)
        {
            b.Cylinder(0,0.42,0,0.95,0.85,Palette.Stone,new PartOptions{Material="stone",Collide=true,Segments=10});
            b.Cylinder(0,0.86,0,0.75,0.02,Palette.Water,new PartOptions{Segments=10});
            b.Box(-0.85,1.6,0,0.15,1.6,0.15,Palette.WoodDark);
            b.Box(0.85,1.6,0,0.15,1.6,0.15,Palette.WoodDark);
            b.Cylinder(0,2.05,0,0.06,1.8,Palette.Wood,new PartOptions{RotZ=Math.PI/2});
            b.Box(-0.42,2.6,0,1.15,0.08,1.4,Palette.Roof,new PartOptions{RotZ=0.62});
            b.Box(0.42,2.6,0,1.15,0.08,1.4,Palette.Roof,new PartOptions{RotZ=-0.62});
            b.Cylinder(0.2,1.35,0,0.17,0.25,Palette.WoodLight,new PartOptions{RadiusTop=0.2});
            int n=rng.Int(1,3);
            for(int i=0;i<n;i++){
                double a=rng.Next()*Math.PI*2;
                if(rng.Chance(0.5))Barrel(b,Math.Cos(a)*2.2,Math.Sin(a)*2.2);
                else Crate(b,rng,Math.Cos(a)*2.2,Math.Sin(a)*2.2);
            }
        }

        static void BuildLog(PropBuilder b,Rng rng)
        {
            double r=rng.Range(0.32,0.48),length=rng.Range(3,4.8);
            b.LyingCylinder(0,r,0,r,length,Palette.Bark,new PartOptions{Collide=true});
            if(rng.Chance(0.5))b.Blob(rng.Range(-1,1),r*2+0.05,0,0.12,Palette.MushroomRed,new PartOptions{BlobShape="ico",ScaleY=0.5});
        }

        static void BuildStump(PropBuilder b,Rng rng)
        {
            double r=rng.Range(0.35,0.5),h=rng.Range(0.45,0.7);
            b.Cylinder(0,h/2,0,r,h,Palette.Bark,new PartOptions{RadiusTop=r*0.92,Collide=true});
            b.Cylinder(0,h+0.01,0,r*0.9,0.02,Palette.WoodLight);
        }

        static void BuildMushrooms(PropBuilder b,Rng rng)
        {
            int n=rng.Int(3,5);bool red=rng.Chance(0.6);
            for(int i=0;i<n;i++){
                double x=rng.Range(-0.6,0.6),z=rng.Range(-0.6,0.6),s=rng.Range(0.7,1.3);
                b.Cylinder(x,0.09*s,z,0.045*s,0.18*s,Palette.MushroomStem,new PartOptions{Segments=5});
                b.Blob(x,0.2*s,z,0.14*s,red?Palette.MushroomRed:Palette.MushroomBrown,new PartOptions{BlobShape="ico",ScaleY=0.55});
            }
        }

        static void BuildSignpost(PropBuilder b,Rng rng)
        {
            b.Box(0,0.9,0,0.12,1.8,0.12,Palette.WoodDark,new PartOptions{Collide=true,Standable=false});
            b.Box(0.3,1.5,0,0.8,0.2,0.05,Palette.WoodLight,new PartOptions{RotY=rng.Range(-0.4,0.4)});
            b.Box(-0.25,1.15,0,0.7,0.2,0.05,Palette.WoodLight,new PartOptions{RotY=Math.PI+rng.Range(-0.6,0.6)});
        }

        static void BuildWallFragment(PropBuilder b,Rng rng)
        {
            double len=rng.Range(2.5,4.5),height=rng.Range(1.4,2.6);
            RuinedWall(b,rng,-len/2,0,len/2,0,height,windows:false);
            Rubble(b,rng,4,len/2+0.5,1);
        }

        struct LooseKind{public string Id;public Action<PropBuilder,Rng> Build;public int Count;public bool Forest;public double MaxSlope;}

        // Todas las construcciones de los sitios y los objetos sueltos del mapa.
        public static PropSet Generate(Heightfield hf,List<Site> sites,Rng rng,double limit,double spawnClear)
        {
            var set=new PropSet();var b=new PropBuilder(set);
            for(int i=0;i<sites.Count;i++){
                var site=sites[i];var siteRng=rng.Derive("site-"+i);
                b.At(site.X,site.FloorY,site.Z,site.Rotation);
                switch(site.Kind){
                    case "house":BuildHouse(b,siteRng);break;
                    case "temple":BuildTemple(b,siteRng);break;
                    case "farm":BuildFarm(b,siteRng);break;
                    case "well":BuildWell(b,siteRng);break;
                }
            }
            var forest=new SimplexNoise2D(rng.Next);
            var kinds=new[]{
                new LooseKind{Id="log",Build=BuildLog,Count=18,Forest=true,MaxSlope=10},
                new LooseKind{Id="stump",Build=BuildStump,Count=24,Forest=true,MaxSlope=22},
                new LooseKind{Id="mushrooms",Build=BuildMushrooms,Count=30,Forest=true,MaxSlope=22},
                new LooseKind{Id="signpost",Build=BuildSignpost,Count=7,Forest=false,MaxSlope=20},
                new LooseKind{Id="wall",Build=BuildWallFragment,Count=10,Forest=false,MaxSlope=12},
            };
            foreach(var kind in kinds){
                double cosMax=Math.Cos(kind.MaxSlope*WorldMath.Deg2Rad);int placed=0;
                for(int attempt=0;attempt<kind.Count*30&&placed<kind.Count;attempt++){
                    double x=rng.Range(-limit,limit),z=rng.Range(-limit,limit),rotation=rng.Next()*Math.PI*2,roll=rng.Next();
                    if(WorldMath.Squircle(x,z)>limit-6)continue;
                    if(JsMath.Hypot(x,z)<spawnClear)continue;
                    if(Sites.IsInAny(sites,x,z,6))continue;
                    if(hf.NormalAt(x,z).Y<cosMax)continue;
                    if(kind.Forest&&forest.Noise(x*0.018,z*0.018)*0.5+0.5<roll*0.8)continue;
                    b.At(x,hf.HeightAt(x,z)-0.05,z,rotation);
                    kind.Build(b,rng.Derive("prop-"+kind.Id+"-"+placed));
                    placed++;
                }
            }
            return set;
        }
    }
}
