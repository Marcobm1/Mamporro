using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Sitios de interés (src/world/sites.ts): zonas llanas lejos del inicio y entre sí.
    public sealed class Site
    {
        public string Kind;
        public double X,Z,Radius,Rotation,FloorY;
    }

    public static class Sites
    {
        // Anchura de la transición entre el suelo aplanado y el terreno original.
        public const double Blend=5;

        static double[] SampleHeights(Heightfield hf,double x,double z,double radius)
        {
            var samples=new double[17];int n=0;
            samples[n++]=hf.HeightAt(x,z);
            foreach(double r in new[]{radius*0.5,radius})
                for(int a=0;a<8;a++){double angle=(a/8.0)*Math.PI*2;samples[n++]=hf.HeightAt(x+Math.Cos(angle)*r,z+Math.Sin(angle)*r);}
            return samples;
        }

        public static List<Site> Pick(Heightfield hf,Rng rng,SiteRequest[] requests,double limit,double spawnClear,double gap,double maxRange,int attempts=60)
        {
            var sites=new List<Site>();
            foreach(var request in requests){
                for(int n=0;n<request.count;n++){
                    for(int attempt=0;attempt<attempts;attempt++){
                        // Siempre los mismos números por intento (determinismo robusto).
                        double u=rng.Next(),v=rng.Next(),rotation=rng.Next()*Math.PI*2;
                        double maxR=limit-request.radius-Blend-4,x=(u*2-1)*maxR,z=(v*2-1)*maxR;
                        if(WorldMath.Squircle(x,z)>maxR)continue;
                        if(JsMath.Hypot(x,z)<spawnClear+request.radius)continue;
                        bool clash=false;
                        foreach(var s in sites)if(JsMath.Hypot(s.X-x,s.Z-z)<s.Radius+request.radius+Blend*2+gap){clash=true;break;}
                        if(clash)continue;
                        var heights=SampleHeights(hf,x,z,request.radius);
                        double min=double.PositiveInfinity,max=double.NegativeInfinity;
                        foreach(double h in heights){if(h<min)min=h;if(h>max)max=h;}
                        if(max-min>maxRange)continue;
                        Array.Sort(heights);
                        sites.Add(new Site{Kind=request.kind,X=x,Z=z,Radius=request.radius,Rotation=rotation,FloorY=heights[heights.Length/2]});
                        break;
                    }
                }
            }
            return sites;
        }

        // Aplana el terreno bajo cada sitio con una transición suave.
        public static void Flatten(Heightfield hf,List<Site> sites)
        {
            double cs=hf.CellSize;
            foreach(var site in sites){
                double outer=site.Radius+Blend;
                int i0=Math.Max(0,(int)Math.Floor((site.X-outer+hf.Half)/cs)),i1=Math.Min(hf.Cells,(int)Math.Ceiling((site.X+outer+hf.Half)/cs));
                int j0=Math.Max(0,(int)Math.Floor((site.Z-outer+hf.Half)/cs)),j1=Math.Min(hf.Cells,(int)Math.Ceiling((site.Z+outer+hf.Half)/cs));
                for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++){
                    double x=i*cs-hf.Half,z=j*cs-hf.Half,d=JsMath.Hypot(x-site.X,z-site.Z);
                    if(d>outer)continue;
                    int index=j*hf.Stride+i;double t=WorldMath.Smoothstep(site.Radius,outer,d);
                    hf.Heights[index]=(float)WorldMath.Lerp(site.FloorY,hf.Heights[index],t);
                }
            }
        }

        public static bool IsInAny(List<Site> sites,double x,double z,double margin=0)
        {
            foreach(var s in sites){double r=s.Radius+margin,dx=x-s.X,dz=z-s.Z;if(dx*dx+dz*dz<r*r)return true;}
            return false;
        }
    }
}
