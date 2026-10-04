using System;
using System.Collections.Generic;
using Mamporro.Core;
using Mamporro.Core.Audio;
using Mamporro.Core.Effects;

namespace Mamporro.U3
{
    // U5: traduce los sucesos de la partida (CombatFeedback) y los efectos de armas a sonido,
    // partículas, números de daño y cámara, como RunView.ts. Un único receptor por aplicación;
    // solo presenta y nunca cambia la partida. Aleatoriedad visual propia (Rng «efectos»).
    public sealed class RunFeedback : ICombatFeedback
    {
        readonly U3Game game;
        readonly SoundDef hit,critical,death,blast,hurt,xp,gold,reward,level,shield,boss;
        readonly Dictionary<string,SoundDef> weaponSounds=new Dictionary<string,SoundDef>();
        readonly Rng rng=new Rng("efectos").Derive("armas");
        public readonly ParticleField Particles=new ParticleField(new Rng("efectos").Derive("particulas"));
        public readonly DamageNumbers Numbers=new DamageNumbers(new Rng("efectos").Derive("numeros"));
        // Recuento por tipo (pruebas y ensayo).
        public readonly int[] Counts=new int[Enum.GetValues(typeof(FeedbackKind)).Length];

        // Colores de src/render/palette.ts y debrisColors de src/data/enemies.ts.
        static readonly uint[] CritColors={0xffd23f,0xc98a3c},Dust={0xd8cfb8},LevelColors={0xffd23f,0x4aa8ff,0xffffff},ShieldColors={0xc98a3c,0xffd23f},
            ChestColors={0xf6c63a,0xffd23f,0xffffff},PipaColors={0xe8e4dc,0x2a2628},BossColors={0xd8cfb8,0xb3aec2,0x8a8598},SlamColors={0xd8cfb8,0xb3aec2},
            BlastColors={0xff8a2a,0xffd23f,0xffffff},SmokeColors={0x8a8278},PearlColors={0xfdf6ee},ReviveColors={0x5e3d7a,0xff9ac8,0xffffff},
            CrumbColors={0xf3d9a4,0xc98a3c},ZapColors={0xcfefff,0xfff6a8},AuraColors={0xb27cff,0xe2c8ff},BubbleColors={0xeefaff,0x8fd3f0};
        static readonly uint[][] Debris={new uint[]{0xb8b4c4,0x8f8a9e,0xd8d4e0},new uint[]{0x6b3a1e,0x3a2012,0xf0c040},new uint[]{0xcfe3ea,0x7fb24a,0x3f7fc0},
            new uint[]{0x8a8f9c,0x5a5f6c,0x6fae8a},new uint[]{0x7a6a62,0xe07f92,0xff4a4a},new uint[]{0xb8b4c4,0xd8d4e0,0xf0c040}};
        // special de la web: élite (rata) y jefe (Pelusa Madre).
        static int Special(int type)=>type==4?1:type==5?2:0;

        public RunFeedback(U3Game owner)
        {
            game=owner;
            hit=AudioCatalog.Get("hit");critical=AudioCatalog.Get("critical");death=AudioCatalog.Get("death");blast=AudioCatalog.Get("blast");
            hurt=AudioCatalog.Get("hurt");xp=AudioCatalog.Get("xp");gold=AudioCatalog.Get("gold");reward=AudioCatalog.Get("reward");
            level=AudioCatalog.Get("level");shield=AudioCatalog.Get("shield");boss=AudioCatalog.Get("boss");
        }
        // Nueva partida (RunView.attach): sin partículas de la anterior.
        public void Clear(){Particles.Clear();Numbers.Clear();}

        public void Feedback(in CombatFeedback f)
        {
            Counts[(int)f.Kind]++;
            var audio=game.Audio;var p=game.Run.Player;
            switch(f.Kind){
                case FeedbackKind.WeaponFired:{
                    var weapons=game.Run.Weapons;if(f.Code>=weapons.Count)break;var w=weapons[f.Code];audio.Play(WeaponSound(w.Def.id));
                    if(w.Def.behavior=="trail")TrailBubbles(game.Run.StateOf(f.Code));break;}
                case FeedbackKind.PickupXp:audio.Play(xp);break;
                case FeedbackKind.PickupGold:audio.Play(gold);break;
                case FeedbackKind.Hit:
                    audio.Play(f.Code>0?critical:hit);Numbers.Spawn(f.X,f.Y,f.Z,f.Value,f.Code);
                    if(f.Code>0)Particles.Burst(f.X,f.Y,f.Z,3,CritColors,2.5,.08,.25);break;
                case FeedbackKind.EnemyKilled:{
                    int special=Special(f.Code);audio.Play(special==2?blast:death);
                    Particles.Burst(f.X,f.Y+Catalog.Enemies[f.Code].height*.5,f.Z,special==2?80:special==1?24:12,Debris[f.Code],5,.14,.7,2);break;}
                case FeedbackKind.EnemySpawned:Particles.Burst(f.X,f.Y+.2,f.Z,5,Dust,1.8,.28,.5,0,-1);break;
                case FeedbackKind.PlayerHit:audio.Play(hurt);Numbers.Spawn(p.X,p.Y+2,p.Z,f.Value,-1);break;
                case FeedbackKind.EnemyShot:Particles.Burst(f.X,f.Y,f.Z,3,PipaColors,1.5,.08,.35);break;
                case FeedbackKind.LevelUp:audio.Play(level);Particles.Burst(p.X,p.Y+1,p.Z,24,LevelColors,4,.12,.9,3,6);break;
                case FeedbackKind.Shield:audio.Play(shield);Particles.Burst(p.X,p.Y+1,p.Z,18,ShieldColors,3,.14,.45);break;
                case FeedbackKind.ItemGained:audio.Play(reward);break;
                case FeedbackKind.ChestOpened:audio.Play(reward);Particles.Burst(f.X,f.Y,f.Z,26,ChestColors,4,.12,.9,4,8);break;
                case FeedbackKind.BossSpawned:audio.Play(boss);Particles.Burst(f.X,f.Y+1,f.Z,60,BossColors,7,.4,1.4,3,3);break;
                case FeedbackKind.BossSlam:
                    audio.Play(blast);
                    for(int k=0;k<20;k++){double a=k/20.0*Math.PI*2;Particles.Burst(f.X+Math.Cos(a)*f.Radius*.8,f.Y+.2,f.Z+Math.Sin(a)*f.Radius*.8,2,SlamColors,3,.35,.9,2,4);}
                    break;
                case FeedbackKind.Explosion:
                    audio.Play(blast);
                    Particles.Burst(f.X,f.Y+.5,f.Z,14,BlastColors,f.Radius*2.2,.18,.45,2);Particles.Burst(f.X,f.Y+.6,f.Z,6,SmokeColors,1.2,.4,.9,0,-1.5);break;
                case FeedbackKind.Pearl:
                    for(int k=1;k<=5;k++){double t=k/6.0,arc=Math.Sin(t*Math.PI)*.8;Particles.Burst(f.X+(f.X2-f.X)*t,f.Y+(f.Y2-f.Y)*t+arc,f.Z+(f.Z2-f.Z)*t,1,PearlColors,.2,.13,.35,0,0);}
                    break;
                case FeedbackKind.Revive:
                    audio.Play(shield);
                    for(int k=0;k<28;k++){double a=k/28.0*Math.PI*2;Particles.Burst(f.X+Math.Cos(a)*1.2,f.Y+.8,f.Z+Math.Sin(a)*1.2,2,ReviveColors,f.Radius*1.2,.16,.7,1,0);}
                    break;
            }
        }
        // Efectos de armas (WeaponEffects de la web): migas del barrazo, chispas del rayo y vapores de naftalina.
        public void Effect(in CombatEffect e)
        {
            switch(e.Kind){
                case "arc":
                    for(int k=0;k<4;k++){double a=e.Angle+rng.Range(-ArcHalfAngle,ArcHalfAngle),r=e.Radius*rng.Range(.6,1);
                        Particles.Burst(e.X-Math.Sin(a)*r,e.Y,e.Z-Math.Cos(a)*r,1,CrumbColors,1.5,.1,.5,1);}
                    break;
                case "chain":Particles.Burst(e.X2,e.Y2,e.Z2,3,ZapColors,3,.09,.3);break;
                case "aura":
                    for(int k=0;k<3;k++){double a=rng.Next()*Math.PI*2,r=e.Radius*rng.Range(.4,1);
                        Particles.Burst(e.X+Math.Cos(a)*r,e.Y+.3,e.Z+Math.Sin(a)*r,1,AuraColors,.4,.16,.9,1,-1.5);}
                    break;
            }
        }
        const double ArcHalfAngle=75*Math.PI/180;
        void TrailBubbles(WeaponState s)
        {
            int n=Math.Min(3,s.Count);
            for(int k=0;k<n;k++){int i=rng.Int(0,s.Count-1);Particles.Burst(s.X[i],s.Y[i]+.1,s.Z[i],1,BubbleColors,.3,.13,.8,.8,-.6);}
        }
        SoundDef WeaponSound(string id)
        {
            if(!weaponSounds.TryGetValue(id,out var def)){def=AudioCatalog.Get(id);weaponSounds[id]=def;}
            return def;
        }
    }
}
