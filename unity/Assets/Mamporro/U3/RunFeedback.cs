using Mamporro.Core;
using Mamporro.Core.Audio;

namespace Mamporro.U3
{
    // U5: traduce los sucesos de la partida (CombatFeedback) a sonido, partículas, números de
    // daño y cámara, como RunView.ts. Un único receptor por aplicación; solo presenta y nunca
    // cambia la partida. Los timbres se resuelven una vez (sin búsquedas por suceso).
    public sealed class RunFeedback : ICombatFeedback
    {
        readonly U3Game game;
        readonly SoundDef hit,critical,death,blast,hurt,xp,gold,reward,level,shield,boss;
        public RunFeedback(U3Game owner)
        {
            game=owner;
            hit=AudioCatalog.Get("hit");critical=AudioCatalog.Get("critical");death=AudioCatalog.Get("death");blast=AudioCatalog.Get("blast");
            hurt=AudioCatalog.Get("hurt");xp=AudioCatalog.Get("xp");gold=AudioCatalog.Get("gold");reward=AudioCatalog.Get("reward");
            level=AudioCatalog.Get("level");shield=AudioCatalog.Get("shield");boss=AudioCatalog.Get("boss");
        }
        // Recuento por tipo (pruebas y ensayo).
        public readonly int[] Counts=new int[System.Enum.GetValues(typeof(FeedbackKind)).Length];

        public void Feedback(in CombatFeedback f)
        {
            Counts[(int)f.Kind]++;
            var audio=game.Audio;
            switch(f.Kind){
                case FeedbackKind.WeaponFired:{var weapons=game.Run.Weapons;if(f.Code<weapons.Count)audio.Play(WeaponSound(weapons[f.Code].Def.id));break;}
                case FeedbackKind.PickupXp:audio.Play(xp);break;
                case FeedbackKind.PickupGold:audio.Play(gold);break;
                case FeedbackKind.Hit:audio.Play(f.Code>0?critical:hit);break;
                case FeedbackKind.EnemyKilled:audio.Play(Catalog.Enemies[f.Code].behavior=="boss"?blast:death);break;
                case FeedbackKind.PlayerHit:audio.Play(hurt);break;
                case FeedbackKind.LevelUp:audio.Play(level);break;
                case FeedbackKind.Shield:case FeedbackKind.Revive:audio.Play(shield);break;
                case FeedbackKind.ItemGained:case FeedbackKind.ChestOpened:audio.Play(reward);break;
                case FeedbackKind.BossSpawned:audio.Play(boss);break;
                case FeedbackKind.BossSlam:case FeedbackKind.Explosion:audio.Play(blast);break;
            }
        }
        // Cada arma suena con su timbre (mismo ID en el catálogo sonoro).
        readonly System.Collections.Generic.Dictionary<string,SoundDef> weaponSounds=new System.Collections.Generic.Dictionary<string,SoundDef>();
        SoundDef WeaponSound(string id)
        {
            if(!weaponSounds.TryGetValue(id,out var def)){def=AudioCatalog.Get(id);weaponSounds[id]=def;}
            return def;
        }
    }
}
