using System;

namespace Mamporro.Core
{
    // Una partida de U3 (Run.ts + el orden de Game.ts) sobre el combate de U2: física con
    // la presión anterior, sincronización, combate con el director enganchado tras las
    // pasivas (como Run.update) y devolución del empuje del jefe al cuerpo.
    // Coordenadas web y dt double.
    public sealed class WorldRun : CombatRun.ISchedule,IDirectorEvents
    {
        // ENEMY_CAPACITY de la web: el enjambre llega a 750 vivos.
        public const int EnemyCapacity=800;
        public readonly WorldData World;
        public readonly PlayerBody Body=new PlayerBody();
        public readonly CombatRun Combat;
        public readonly WorldSpawns Spawns;
        public readonly Director Director;
        public SpawnParams Params;
        // false = sin director (pruebas de combate controladas).
        public bool Automatic=true;
        // Se han usado trucos de depuración (run.cheated).
        public bool Cheated;
        double viewYaw;
        public WorldRun(WorldData world,string seed,CharacterDef character,ICombatEffects effects=null,int minutes=10)
        {
            World=world;Director=new Director(minutes);
            Combat=new CombatRun(world.Collision,seed,character,EnemyCapacity,effects,world.Heightfield.Size){BossEndsRun=true,Pace=Director.Pace,Schedule=this};
            Spawns=new WorldSpawns(seed);
            Body.PlaceAt(0,world.Heightfield.HeightAt(0,0),0);SyncPlayer();
        }
        public double TimeLeft=>Director.TimeLeft(Combat.Time);
        public bool Swarm=>Director.Swarm;
        public int Spawned=>Spawns.Spawned+Combat.Spawned;
        public bool Over=>Combat.Dead||Combat.Victory;
        public void SyncPlayer()
        {
            var p=Combat.Player;
            p.X=Body.X;p.Y=Body.Y;p.Z=Body.Z;p.Vx=Body.Vx;p.Vz=Body.Vz;
            p.Facing=Body.Facing;p.Grounded=Body.Grounded;
        }
        public void Step(PlayerIntent intent,double dt,double viewYaw=0)
        {
            if(Combat.Paused||Combat.Choosing||Over)return;
            PlayerPhysics.StepInCrowd(Body,intent,World.Collision,PlayerTuning.Default,
                Tuning.PlayerBaseMoveSpeed*Combat.Stats[Stat.moveSpeed],Combat.CrowdSlow,dt);
            SyncPlayer();this.viewYaw=viewYaw;Combat.Step(dt);
            Body.X=Combat.Player.X;Body.Z=Combat.Player.Z;
        }

        // ------------------------------------------------------------ director
        SpawnModifiers Modifiers()=>SpawnModifiers.None;
        void CombatRun.ISchedule.Refresh(CombatRun run)
        {Director.Params(run.Time,Modifiers(),ref Params);run.SpawnHp=Params.Hp;run.SpawnXp=Params.Xp;run.SpawnGold=Params.Gold;}
        void CombatRun.ISchedule.Spawn(CombatRun run,double dt)
        {
            if(!Automatic)return;
            Director.Update(run.Time,this);
            Spawns.Update(dt,ref Params,run.Enemies,World.Collision,run.Player.X,run.Player.Z,viewYaw);
        }
        void IDirectorEvents.Wave(SpecialWave wave)
        {
            var p=Combat.Player;
            Spawns.SpawnFormation(Director.EnemyIndex(wave.enemy),wave.count,wave.formation,ref Params,Combat.Enemies,World.Collision,p.X,p.Z,viewYaw);
            Combat.Enemies.Rebuild();Combat.Event("wave",wave.noticeKey);
        }
        void IDirectorEvents.Elite()
        {
            var p=Combat.Player;
            if(Spawns.SpawnSpecial(WorldSpawns.EliteType,Params.Hp,ref Params,Combat.Enemies,World.Collision,p.X,p.Z,viewYaw)<0)return;
            Combat.Enemies.Rebuild();Combat.Event("elite",Catalog.Enemies[WorldSpawns.EliteType].id);
        }
        void IDirectorEvents.Swarm(){Combat.Event("swarm");}

        // ------------------------------------------------------------ depuración (F3 de la web)
        public bool DebugToggleInvincible(){Cheated=true;return Combat.Invincible=!Combat.Invincible;}
        public void DebugLevelUp(){Cheated=true;Combat.GainXp(Rules.XpNeeded(Combat.Level)-Combat.Xp);}
        public void DebugSkipMinute(){Cheated=true;Combat.Time+=60;}
        public void DebugSpawn(int count)
        {
            Cheated=true;Combat.RefreshSpawnParams();var p=Combat.Player;
            Spawns.SpawnBurst(count,ref Params,Combat.Enemies,World.Collision,p.X,p.Z,viewYaw);Combat.Enemies.Rebuild();
        }
        public void DebugKillAll(){Cheated=true;Combat.KillAll();}
        // Delante del jugador (sin buscar el armario), a 14 m como la web.
        public bool DebugSummonBoss()
        {
            Cheated=true;var p=Combat.Player;
            return Combat.SpawnBoss(p.X-Math.Sin(p.Facing)*14,p.Z-Math.Cos(p.Facing)*14);
        }
        public void DebugAddGold(double amount){Cheated=true;Combat.GainGold(amount);}
    }
}
