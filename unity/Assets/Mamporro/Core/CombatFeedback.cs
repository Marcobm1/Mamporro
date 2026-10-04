namespace Mamporro.Core
{
    // U5: sucesos de la partida para sonido, partículas, números de daño y cámara (RunEffects de
    // Run.ts). Son observadores: se emiten donde la web llama a fx.*, después de que ocurra lo
    // que describen, sin consumir la RNG ni cambiar el orden de la simulación. Tipados y sin
    // cadenas: emitirlos no asigna memoria.
    public enum FeedbackKind : byte
    {
        WeaponFired,   // Code = posición del arma en Weapons
        PickupXp,
        PickupGold,
        Hit,           // posición del número, Value = daño, Code = nivel de crítico
        EnemyKilled,   // Code = tipo de enemigo
        EnemySpawned,
        PlayerHit,     // Value = daño recibido
        EnemyShot,
        LevelUp,       // Code = nivel alcanzado
        Shield,
        ItemGained,
        ChestOpened,
        BossSpawned,
        BossSlam,      // Radius
        Explosion,     // Radius
        Pearl,         // X2, Y2, Z2 = destino
        Revive,        // Radius
    }
    public struct CombatFeedback
    {
        public FeedbackKind Kind;
        public int Code;
        public double X,Y,Z,X2,Y2,Z2,Value,Radius;
    }
    public interface ICombatFeedback { void Feedback(in CombatFeedback feedback); }
}
