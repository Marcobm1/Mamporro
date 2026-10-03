using System;

namespace Mamporro.Core
{
    // Una partida de U3 (Run.ts + el orden de Game.ts) sobre el combate de U2: física con
    // la presión anterior, interactuar, sincronización, combate con el director enganchado
    // tras las pasivas y los interactuables tras recoger (como Run.update), devolución del
    // empuje del jefe al cuerpo y espera de 1,6 s tras la victoria. Coordenadas web y dt double.
    public sealed class WorldRun : CombatRun.ISchedule,IDirectorEvents,IInteractableEvents
    {
        // Segundos entre derrotar al jefe y los resultados (VICTORY_DELAY de Game.ts).
        public const double VictoryDelaySeconds=1.6;
        // ENEMY_CAPACITY de la web: el enjambre llega a 750 vivos.
        public const int EnemyCapacity=800;
        public readonly WorldData World;
        public readonly PlayerBody Body=new PlayerBody();
        public readonly CombatRun Combat;
        public readonly WorldSpawns Spawns;
        public readonly Director Director;
        public readonly Interactables Interactables;
        public SpawnParams Params;
        public int ShrinesCompleted,ChallengesCompleted;
        // Lo que el jugador tiene delante para usar (tras el último tick).
        public bool HasPrompt;
        public InteractPrompt Prompt;
        public double VictoryDelay=VictoryDelaySeconds;
        // Partida terminada: derrota, o victoria tras la espera.
        public bool Finished=>Combat.Dead||(Combat.Victory&&VictoryDelay<=0);
        // false = sin director (pruebas de combate controladas).
        public bool Automatic=true;
        // Se han usado trucos de depuración (run.cheated).
        public bool Cheated;
        double viewYaw;
        public WorldRun(WorldData world,string seed,CharacterDef character,ICombatEffects effects=null,int minutes=10)
        {
            World=world;Director=new Director(minutes);
            Combat=new CombatRun(world.Collision,seed,character,EnemyCapacity,effects,world.Heightfield.Size){BossEndsRun=true,Pace=Director.Pace,Schedule=this};
            Spawns=new WorldSpawns(seed);Interactables=new Interactables(world.Interactables);
            Body.PlaceAt(0,world.Heightfield.HeightAt(0,0),0);SyncPlayer();
        }
        public double TimeLeft=>Director.TimeLeft(Combat.Time);
        public bool Swarm=>Director.Swarm;
        public int Spawned=>Spawns.Spawned+Combat.Spawned;
        public bool Over=>Combat.Dead||Combat.Victory;
        public double ChallengeLeft=>Interactables.Challenge;
        public void SyncPlayer()
        {
            var p=Combat.Player;
            p.X=Body.X;p.Y=Body.Y;p.Z=Body.Z;p.Vx=Body.Vx;p.Vz=Body.Vz;
            p.Facing=Body.Facing;p.Grounded=Body.Grounded;
        }
        // Un tick de Game.update: física, E (con la posición del tick anterior, como la web),
        // Run.update y, tras la victoria, solo física y la cuenta atrás hasta los resultados.
        public void Step(PlayerIntent intent,double dt,double viewYaw=0,bool interact=false)
        {
            if(Combat.Paused||Combat.Choosing||Finished)return;
            PlayerPhysics.StepInCrowd(Body,intent,World.Collision,PlayerTuning.Default,
                Tuning.PlayerBaseMoveSpeed*Combat.Stats[Stat.moveSpeed],Combat.CrowdSlow,dt);
            if(interact)Interact();
            if(!Combat.Victory){
                SyncPlayer();this.viewYaw=viewYaw;Combat.Step(dt);
                Body.X=Combat.Player.X;Body.Z=Combat.Player.Z;
            }
            if(Combat.Victory)VictoryDelay-=dt;
        }

        // ------------------------------------------------------------ interactuables
        // Usa lo que el jugador tenga delante (tecla E).
        public bool Interact()
        {
            var p=Combat.Player;
            if(!Interactables.Prompt(p.X,p.Z,out var prompt)||Combat.Dead||Combat.Victory)return false;
            var item=Interactables.List[prompt.Index];
            switch(prompt.Kind){
                case "chest":return OpenChest(item,prompt.Cost);
                case "totem":return StartChallenge(item);
                case "portal":return OpenPortal(item);
                default:return false;
            }
        }
        bool OpenChest(InteractableState item,double cost)
        {
            var r=Combat;
            if(r.Gold<cost){r.Event("noGold",Math.Ceiling(cost-r.Gold).ToString(System.Globalization.CultureInfo.InvariantCulture));return false;}
            r.Gold-=cost;r.ChestsOpened=++Interactables.ChestsOpened;item.Used=true;
            r.Emit("chest",item.Spot.X,item.Spot.Y+.9,item.Spot.Z,1.2,life:.6);r.Event("chestOpened");
            var def=r.RollItem(r.Stats[Stat.luck]);
            if(def!=null)r.AddItem(def.id);else r.GainGold(cost);
            r.CheckPurse();return true;
        }
        bool StartChallenge(InteractableState item)
        {
            if(Interactables.Challenge>0)return false;
            item.Used=true;Interactables.Challenge=Tuning.TotemDuration;
            Combat.ChallengeLuck=Tuning.TotemLuck;Combat.RefreshStats();Combat.Event("challengeStart");return true;
        }
        // Desafío superado: vuelve la calma y cae un objeto con mucha suerte.
        void FinishChallenge()
        {
            ChallengesCompleted++;Combat.ChallengeLuck=0;Combat.RefreshStats();Combat.Event("challengeDone");
            var def=Combat.RollItem(Combat.Stats[Stat.luck]+Tuning.TotemRewardLuck);
            if(def!=null)Combat.AddItem(def.id);
        }
        // La Pelusa Madre sale por detrás del armario (el lado contrario al jugador).
        bool OpenPortal(InteractableState item)
        {
            var p=Combat.Player;double dx=item.Spot.X-p.X,dz=item.Spot.Z-p.Z,d=JsMath.Hypot(dx,dz);if(d==0)d=1;
            if(!Combat.SpawnBoss(item.Spot.X+dx/d*Tuning.PortalBossDistance,item.Spot.Z+dz/d*Tuning.PortalBossDistance))return false;
            item.Used=true;return true;
        }
        void IInteractableEvents.Discovered(InteractableState item){if(item.Spot.Kind=="portal")Combat.Event("portalFound");}
        void IInteractableEvents.ShrineCharged(InteractableState item)
        {Combat.PendingShrines++;ShrinesCompleted++;Combat.Event("shrineCharged");}
        void CombatRun.ISchedule.Late(CombatRun run,double dt)
        {
            var p=run.Player;
            if(Interactables.Update(dt,p.X,p.Z,this))FinishChallenge();
            HasPrompt=Interactables.Prompt(p.X,p.Z,out Prompt);
        }

        // ------------------------------------------------------------ director
        // Tótem de desafío (más enemigos, con más vida y más oro) y mesa camilla cargándose.
        SpawnModifiers Modifiers()
        {
            var m=SpawnModifiers.None;
            if(Interactables.Challenge>0){m.Rate*=Tuning.TotemSpawnMultiplier;m.Hp*=Tuning.TotemHpMultiplier;m.Gold*=Tuning.TotemGoldMultiplier;}
            if(Interactables.Charging>=0)m.Rate*=Tuning.ShrineSpawnMultiplier;
            return m;
        }
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
        void IDirectorEvents.Swarm()
        {
            Combat.Event("swarm");
            // Para tener una salida: el armario se revela si aún no se había encontrado.
            if(Interactables.Reveal("portal")>0)Combat.Event("portalRevealed");
        }

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
        public void DebugRevealMap(){Cheated=true;Interactables.Reveal();}
    }
}
