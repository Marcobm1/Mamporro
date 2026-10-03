using System;

namespace Mamporro.Core
{
    // Port de src/systems/SpawnSystem.ts: apariciones alrededor del jugador, a ser
    // posible fuera de cámara, con el ritmo y la vida del director; oleadas en
    // formación, élites, jefe y reciclado de los que se quedan muy atrás.
    // Un único RNG (seed/run/spawn) para todo, en el mismo orden de consumo que la web.
    public sealed class WorldSpawns
    {
        const double RingRadius=24,LineDistance=32,LineLength=44,ArcDistance=26,ArcHalfAngle=1.2;
        readonly Rng rng;
        double accumulator,spotX,spotZ;
        // Enemigos creados (fx.enemySpawned de la web).
        public int Spawned;
        // onSpawn de la web (posición de cada aparición); opcional.
        public Action<double,double,double> OnSpawn;
        public WorldSpawns(string seed):this(new Rng(seed+"/run").Derive("spawn")){}
        public WorldSpawns(Rng rng){this.rng=rng;}

        public void Update(double dt,ref SpawnParams p,Enemies enemies,WorldCollision world,double x,double z,double yaw)
        {
            RecycleFar(enemies,world,x,z,yaw);
            accumulator+=p.Rate*dt;
            while(accumulator>=1){
                accumulator-=1;
                if(enemies.Count>=p.MaxAlive){accumulator=0;break;}
                SpawnOne(ref p,enemies,world,x,z,yaw);
            }
        }

        // Acción de depuración: `count` enemigos de golpe.
        public void SpawnBurst(int count,ref SpawnParams p,Enemies enemies,WorldCollision world,double x,double z,double yaw)
        {for(int n=0;n<count;n++)SpawnOne(ref p,enemies,world,x,z,yaw);}

        void SpawnOne(ref SpawnParams p,Enemies enemies,WorldCollision world,double x,double z,double yaw)
        {
            if(!FindSpot(world,x,z,yaw))return;
            int type=Director.PickEnemy(p.Minutes,rng);
            Place(type,spotX,spotZ,ref p,enemies,world,p.Hp);
        }

        // Crea un enemigo en (x, z) sobre el terreno; devuelve su índice o -1.
        public int Place(int type,double x,double z,ref SpawnParams p,Enemies enemies,WorldCollision world,double hp)
        {
            double y=world.Heightfield.HeightAt(x,z);
            int i=enemies.Spawn(type,x,y,z,hp,p.Xp,p.Gold);
            if(i>=0){Spawned++;OnSpawn?.Invoke(x,y,z);}
            return i;
        }

        // Un enemigo concreto (élite) en el anillo de aparición; índice o -1.
        public int SpawnSpecial(int type,double hp,ref SpawnParams p,Enemies enemies,WorldCollision world,double x,double z,double yaw)
        {
            if(!FindSpot(world,x,z,yaw))return -1;
            return Place(type,spotX,spotZ,ref p,enemies,world,hp);
        }

        // Oleada especial: fila que entra por un lado, anillo alrededor o arco por delante.
        public int SpawnFormation(int type,int count,string formation,ref SpawnParams p,Enemies enemies,WorldCollision world,double vx,double vz,double yaw)
        {
            double angle0=rng.Next()*Math.PI*2,fx=-Math.Sin(yaw),fz=-Math.Cos(yaw);int spawned=0;
            for(int k=0;k<count;k++){
                double t=count>1?(double)k/(count-1):.5,x=vx,z=vz;
                if(formation=="ring"){
                    double a=angle0+(double)k/count*Math.PI*2,r=RingRadius+rng.Range(-1.5,1.5);
                    x+=Math.Cos(a)*r;z+=Math.Sin(a)*r;
                } else if(formation=="arc"){
                    double a=Math.Atan2(fx,fz)+(t-.5)*2*ArcHalfAngle;
                    x+=Math.Sin(a)*ArcDistance;z+=Math.Cos(a)*ArcDistance;
                } else {
                    // Fila perpendicular a una dirección al azar; si son muchos, en varias filas.
                    int rows=(int)Math.Ceiling(count/30.0),row=k%rows;
                    double along=(Math.Floor((double)k/rows)/Math.Max(1,Math.Ceiling((double)count/rows)-1)-.5)*LineLength;
                    double dx=Math.Cos(angle0),dz=Math.Sin(angle0),dist=LineDistance+row*1.6;
                    x+=dx*dist-dz*along;z+=dz*dist+dx*along;
                }
                if(!world.IsInside(x,z,3))continue;
                if(Place(type,x,z,ref p,enemies,world,p.Hp)>=0)spawned++;
            }
            return spawned;
        }

        // Punto en el anillo de aparición, dentro del mapa y a ser posible fuera de cámara.
        bool FindSpot(WorldCollision world,double x,double z,double yaw)
        {
            double fx=-Math.Sin(yaw),fz=-Math.Cos(yaw);
            for(int attempt=0;attempt<10;attempt++){
                double angle=rng.Next()*Math.PI*2;bool avoid=rng.Next()<.85;
                double distance=rng.Range(Tuning.SpawnCurveSpawnDistanceMin,Tuning.SpawnCurveSpawnDistanceMax);
                double dx=Math.Sin(angle),dz=Math.Cos(angle);
                if(avoid&&dx*fx+dz*fz>.45)continue;
                double sx=x+dx*distance,sz=z+dz*distance;
                if(!world.IsInside(sx,sz,3))continue;
                spotX=sx;spotZ=sz;return true;
            }
            return false;
        }

        // Los normales que se quedan muy atrás reaparecen cerca (élites y jefe no).
        public void RecycleFar(Enemies e,WorldCollision world,double x,double z,double yaw)
        {
            double max2=Tuning.SpawnCurveRecycleDistance*Tuning.SpawnCurveRecycleDistance;
            for(int i=0;i<e.Count;i++){
                double dx=e.X[i]-x,dz=e.Z[i]-z;
                if(dx*dx+dz*dz<max2||IsSpecial(e.Type[i]))continue;
                if(!FindSpot(world,x,z,yaw))continue;
                e.Relocate(i,spotX,world.Heightfield.HeightAt(spotX,spotZ),spotZ);
            }
            // Como la web, Enemies.Step reconstruye la rejilla al terminar el movimiento.
        }
        public void RecycleFar(CombatRun run,WorldCollision world,double yaw)=>RecycleFar(run.Enemies,world,run.Player.X,run.Player.Z,yaw);

        // ENEMY_LIST[type].special de la web: la Rata de Gimnasio (élite) y la Pelusa Madre (jefe).
        public static bool IsSpecial(int type)=>type==EliteType||type==BossType;
        public static readonly int EliteType=Director.EnemyIndex("rata"),BossType=Director.EnemyIndex("pelusaMadre");
    }
}
