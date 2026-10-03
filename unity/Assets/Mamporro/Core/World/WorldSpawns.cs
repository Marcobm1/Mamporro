using System;

namespace Mamporro.Core
{
    // Parte espacial de SpawnSystem.ts. Sin reloj, tabla ni director: se reutilizará
    // en el paso 6. Las apariciones QA no son una curva de dificultad alternativa.
    public sealed class WorldSpawns
    {
        readonly Rng rng;
        public WorldSpawns(string seed){rng=new Rng(seed+"/run").Derive("spawn");}
        bool FindSpot(WorldCollision world,double x,double z,double yaw,out double sx,out double sz)
        {
            double fx=-Math.Sin(yaw),fz=-Math.Cos(yaw);sx=sz=0;
            for(int attempt=0;attempt<10;attempt++){
                double angle=rng.Next()*Math.PI*2;bool avoid=rng.Next()<.85;
                double distance=rng.Range(Tuning.SpawnCurveSpawnDistanceMin,Tuning.SpawnCurveSpawnDistanceMax);
                double dx=Math.Sin(angle),dz=Math.Cos(angle);
                if(avoid&&dx*fx+dz*fz>.45)continue;
                sx=x+dx*distance;sz=z+dz*distance;
                if(world.IsInside(sx,sz,3))return true;
            }
            return false;
        }
        public int SpawnQa(CombatRun run,WorldCollision world,int type,double yaw)
        {
            if(!FindSpot(world,run.Player.X,run.Player.Z,yaw,out double x,out double z))return -1;
            return run.SpawnScaled(type,x,z);
        }
        public void RecycleFar(CombatRun run,WorldCollision world,double yaw)
        {
            var e=run.Enemies;double max2=Tuning.SpawnCurveRecycleDistance*Tuning.SpawnCurveRecycleDistance;
            for(int i=0;i<e.Count;i++){
                double dx=e.X[i]-run.Player.X,dz=e.Z[i]-run.Player.Z;
                if(dx*dx+dz*dz<max2||e.Def(i).behavior=="charger"||e.Def(i).behavior=="boss")continue;
                if(FindSpot(world,run.Player.X,run.Player.Z,yaw,out double x,out double z))
                    e.Relocate(i,x,world.Height(x,z),z);
            }
            // Como la web, Enemies.Step reconstruye la rejilla al terminar el movimiento.
        }
    }
}
