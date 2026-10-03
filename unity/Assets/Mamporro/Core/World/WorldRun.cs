namespace Mamporro.Core
{
    // Orden de Game.ts: física con la presión anterior, sincronización, combate y
    // devolución del empuje del jefe al cuerpo. Coordenadas web y dt double.
    public sealed class WorldRun
    {
        public readonly WorldData World;
        public readonly PlayerBody Body=new PlayerBody();
        public readonly CombatRun Combat;
        public readonly WorldSpawns Spawns;
        public bool Cheated;
        public WorldRun(WorldData world,string seed,CharacterDef character,ICombatEffects effects=null)
        {
            World=world;
            Combat=new CombatRun(world.Collision,seed,character,4096,effects,world.Heightfield.Size);
            Spawns=new WorldSpawns(seed);
            Body.PlaceAt(0,world.Heightfield.HeightAt(0,0),0);SyncPlayer();
        }
        public void SyncPlayer()
        {
            var p=Combat.Player;
            p.X=Body.X;p.Y=Body.Y;p.Z=Body.Z;p.Vx=Body.Vx;p.Vz=Body.Vz;
            p.Facing=Body.Facing;p.Grounded=Body.Grounded;
        }
        public void Step(PlayerIntent intent,double dt,double viewYaw=0)
        {
            if(Combat.Paused||Combat.Choosing||Combat.Dead)return;
            PlayerPhysics.StepInCrowd(Body,intent,World.Collision,PlayerTuning.Default,
                Tuning.PlayerBaseMoveSpeed*Combat.Stats[Stat.moveSpeed],Combat.CrowdSlow,dt);
            SyncPlayer();Spawns.RecycleFar(Combat,World.Collision,viewYaw);Combat.Step(dt);
            Body.X=Combat.Player.X;Body.Z=Combat.Player.Z;
        }
    }
}
