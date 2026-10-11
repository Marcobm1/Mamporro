using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // Barrido del contrato sobre TODAS las caras físicas del circuito. Las caras se deducen
    // de la geometría (cajas y triángulos del heightfield), nunca de una lista de IDs: cada
    // cara expuesta que se alza desde suelo transitable debe aceptar la escalada al correr
    // contra ella; las categorías excluidas nunca. Si se añade geometría, queda cubierta.
    public sealed class VerticalCoverageTests
    {
        const double Approach=1.2;
        static readonly PlayerTuning T=PlayerTuning.Default;
        static readonly double WallCos=Math.Cos(Tuning.PlayerMaxSlopeDeg*WorldMath.Deg2Rad);
        static readonly (double X,double Z)[] Sides={(1,0),(-1,0),(0,1),(0,-1)};

        // Correr desde el suelo hacia la cara, con W como en el teclado real; sin salto ni botón.
        static VerticalMotion Run(VerticalCircuit c,double x,double z,double nx,double nz,out PlayerBody b)
        {
            b=new PlayerBody();b.PlaceAt(x,c.Terrain.HeightAt(x,z),z);b.Grounded=true;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<90&&m.Grabs==0;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=-nx,MoveZ=-nz},WallVertical=1},1.0/60);
            return m;
        }
        // Punto de partida jugable: dentro del límite, suelo transitable y cuerpo libre.
        static bool Standable(VerticalCircuit c,double x,double z)=>c.Collision.IsInside(x,z,1)
            &&c.Terrain.NormalAt(x,z).Y>=WallCos&&c.Queries.Clear(new Vec3(x,c.Terrain.HeightAt(x,z),z),T.Radius,T.Height);
        // Primera superficie que encuentra el cuerpo al avanzar hacia la cara.
        static bool FirstHit(VerticalCircuit c,double x,double z,double nx,double nz,out VerticalHit hit)
        {
            double y=c.Terrain.HeightAt(x,z);
            return c.Queries.Sweep(new Vec3(x,y,z),new Vec3(x-nx*Approach,y,z-nz*Approach),T.Radius,T.Height,out hit);
        }

        [Test] public void EveryExposedSolidFaceIsClimbableOrExcludedByCategory()
        {
            var c=new VerticalCircuit();var failures=new List<string>();int climbable=0,excluded=0;
            for(int i=0;i<c.Queries.Count;i++){
                var s=c.Queries.Solid(i);
                foreach(var (nx,nz) in Sides)foreach(double f in new[]{.2,.5,.8}){
                    double fx=nx>0?s.Max.X:nx<0?s.Min.X:s.Min.X+(s.Max.X-s.Min.X)*f;
                    double fz=nz>0?s.Max.Z:nz<0?s.Min.Z:s.Min.Z+(s.Max.Z-s.Min.Z)*f;
                    double x=fx+nx*Approach,z=fz+nz*Approach;
                    if(!Standable(c,x,z))continue;
                    double ground=c.Terrain.HeightAt(x,z),foot=Math.Max(s.Min.Y,c.Terrain.HeightAt(fx,fz));
                    // Cara alcanzable desde el suelo y más alta que un escalón transitable.
                    if(s.Min.Y>ground+T.StepHeight||s.Max.Y-foot<=T.StepHeight+.05)continue;
                    // Si otra superficie la tapa, esa otra cara tiene su propia muestra.
                    if(!FirstHit(c,x,z,nx,nz,out var first)||first.Terrain||first.Id!=s.Id)continue;
                    var m=Run(c,x,z,nx,nz,out var b);string where=$"{s.Kind} {s.Id} cara ({nx},{nz}) f={f} desde ({x:F2},{ground:F2},{z:F2})";
                    if(s.Climbable){
                        climbable++;
                        if(m.Grabs==0)failures.Add("no escala: "+where+$" → {b.X:F2},{b.Y:F2},{b.Z:F2} {m.State}");
                        else if(m.Contact.Normal.X*nx+m.Contact.Normal.Z*nz<.99)failures.Add("normal ajena: "+where);
                    }else{excluded++;if(m.Grabs>0)failures.Add("agarre indebido: "+where);}
                }
            }
            TestContext.Out.WriteLine($"caras escalables {climbable}, excluidas {excluded}");
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
            // Guardas del propio barrido: no puede quedarse vacío por un filtro demasiado estricto.
            Assert.That(climbable,Is.GreaterThanOrEqualTo(40),"caras escalables muestreadas");
            Assert.That(excluded,Is.GreaterThanOrEqualTo(8),"caras excluidas muestreadas");
        }

        [Test] public void EveryPhysicalTerrainWallIsClimbableFromItsLowSide()
        {
            var c=new VerticalCircuit();var t=c.Terrain;var failures=new List<string>();int tested=0;
            for(int z=0;z<t.Cells;z++)for(int x=0;x<t.Cells;x++)for(int face=0;face<2;face++){
                Triangle(t,x,z,face,out var centroid,out var normal);
                if(normal.Y>=WallCos||normal.Y<0)continue;
                double h=Math.Sqrt(normal.X*normal.X+normal.Z*normal.Z),nx=normal.X/h,nz=normal.Z/h;
                double ax=centroid.X+nx*Approach,az=centroid.Z+nz*Approach;
                if(!Standable(c,ax,az))continue;
                // Revestida por un sólido (foso): la cubre el barrido de cajas.
                if(!FirstHit(c,ax,az,nx,nz,out var first)||!first.Terrain)continue;
                tested++;
                var m=Run(c,ax,az,nx,nz,out var b);
                if(m.Grabs==0)failures.Add($"celda {x},{z} cara {face} n=({nx:F2},{nz:F2}) desde ({ax:F2},{az:F2}) → {b.X:F2},{b.Y:F2},{b.Z:F2} {m.State}");
            }
            TestContext.Out.WriteLine($"triángulos abruptos {tested}");
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
            Assert.That(tested,Is.GreaterThanOrEqualTo(40),"triángulos abruptos muestreados");
        }

        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        public void GrazingOrRunningAlongEveryBoxFaceNeverGrabs(double nx,double nz)
        {
            // Cara de 6×8 m del edificio principal: correr pegado en paralelo y en ángulo
            // rasante (por debajo del cono frontal) no debe agarrar en ningún sentido.
            var c=new VerticalCircuit();var s=c.Queries.Solid(0);
            double fx=nx>0?s.Max.X:nx<0?s.Min.X:(s.Min.X+s.Max.X)/2,fz=nz>0?s.Max.Z:nz<0?s.Min.Z:(s.Min.Z+s.Max.Z)/2;
            double tx=-nz,tz=nx;
            foreach(double along in new[]{-1.0,1.0})foreach(double into in new[]{0,.4}){
                double x=fx+nx*(T.Radius+.02)-tx*along*2.5,z=fz+nz*(T.Radius+.02)-tz*along*2.5;
                var b=new PlayerBody();b.PlaceAt(x,0,z);b.Grounded=true;var m=new VerticalMotion(b,c.Collision,c.Queries);
                for(int i=0;i<30;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=tx*along-nx*into,MoveZ=tz*along-nz*into},WallVertical=1},1.0/60);
                Assert.That(m.Grabs,Is.Zero,$"cara ({nx},{nz}) sentido {along} componente {into}");
            }
        }

        [Test] public void InvisibleBoundaryNeverOffersAWall()
        {
            var c=new VerticalCircuit();VerticalSolid boundary=null;
            for(int i=0;i<c.Queries.Count;i++)if(c.Queries.Solid(i).Kind==VerticalSurfaceKind.Boundary)boundary=c.Queries.Solid(i);
            Assert.That(boundary,Is.Not.Null);
            // Incluso forzando el contacto contra su cara, la categoría la excluye.
            var feet=new Vec3(boundary.Min.X-T.Radius-.05,0,0);
            Assert.That(c.Queries.Wall(feet,new Vec3(1,0,0),T.Radius,T.Height,.18,out _),Is.False);
            var m=Run(c,boundary.Min.X-2,0,-1,0,out var b);
            Assert.That(m.Grabs,Is.Zero);Assert.That(m.State,Is.Not.EqualTo(VerticalState.Climbing));
        }

        // Misma triangulación que el heightfield: diagonal (i, j+1)–(i+1, j).
        static void Triangle(Heightfield t,int x,int z,int face,out Vec3 centroid,out Vec3 normal)
        {
            double px=x*t.CellSize-t.Half,pz=z*t.CellSize-t.Half,s=t.CellSize;Vec3 a,b,c;
            if(face==0){a=new Vec3(px,t.VertexHeight(x,z),pz);b=new Vec3(px,t.VertexHeight(x,z+1),pz+s);c=new Vec3(px+s,t.VertexHeight(x+1,z),pz);}
            else{a=new Vec3(px+s,t.VertexHeight(x+1,z+1),pz+s);b=new Vec3(px+s,t.VertexHeight(x+1,z),pz);c=new Vec3(px,t.VertexHeight(x,z+1),pz+s);}
            var u=new Vec3(b.X-a.X,b.Y-a.Y,b.Z-a.Z);var v=new Vec3(c.X-a.X,c.Y-a.Y,c.Z-a.Z);
            normal=new Vec3(u.Y*v.Z-u.Z*v.Y,u.Z*v.X-u.X*v.Z,u.X*v.Y-u.Y*v.X);
            double length=Math.Sqrt(normal.X*normal.X+normal.Y*normal.Y+normal.Z*normal.Z);
            normal=new Vec3(normal.X/length,normal.Y/length,normal.Z/length);
            centroid=new Vec3((a.X+b.X+c.X)/3,(a.Y+b.Y+c.Y)/3,(a.Z+b.Z+c.Z)/3);
        }
    }
}
