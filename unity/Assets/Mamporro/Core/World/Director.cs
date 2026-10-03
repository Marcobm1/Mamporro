using System;

namespace Mamporro.Core
{
    // Cómo aparecen los enemigos en un momento dado (SpawnParams de la web).
    public struct SpawnParams { public double Minutes,Rate,MaxAlive,Hp,Xp,Gold; }
    // Cambios temporales al ritmo: tótem de desafío y santuario cargándose.
    public struct SpawnModifiers
    {
        public double Rate,Hp,Gold;
        public static SpawnModifiers None=>new SpawnModifiers{Rate=1,Hp=1,Gold=1};
    }
    public interface IDirectorEvents { void Wave(SpecialWave wave); void Elite(); void Swarm(); }

    // Port de src/systems/Director.ts y src/systems/difficulty.ts: temporizador,
    // curva de dificultad, oleadas especiales, élites periódicos y enjambre final.
    // Los minutos son «de dificultad»: en 5 min la curva va al doble y en 15, más despacio.
    public sealed class Director
    {
        public const double ReferenceMinutes=10;
        public readonly int Minutes;
        public readonly double Duration,Pace;
        public bool Swarm;
        int nextWave;
        double nextElite=Tuning.EliteFirst;
        public Director(int minutes)
        {
            if(Array.IndexOf(Catalog.RunDurations,minutes)<0)throw new ArgumentOutOfRangeException(nameof(minutes));
            Minutes=minutes;Duration=minutes*60;Pace=ReferenceMinutes/minutes;
        }
        public double Difficulty(double time)=>Math.Max(0,time)/60*Pace;
        // Negativo durante el enjambre final.
        public double TimeLeft(double time)=>Duration-time;
        public double Overtime(double time)=>Math.Max(0,time-Duration);

        public void Params(double time,SpawnModifiers mods,ref SpawnParams p)
        {
            double minutes=Difficulty(time),over=Overtime(time);
            p.Minutes=minutes;p.Hp=HpMultiplier(minutes)*mods.Hp;p.Xp=XpMultiplier(minutes)*Pace;p.Gold=mods.Gold*Pace;
            if(over>0){
                // Enjambre final: el ritmo se duplica cada pocos segundos y la vida sigue creciendo.
                p.Rate=Math.Min(Tuning.SwarmMaxRate,SpawnRate(minutes)*Math.Pow(2,over/Tuning.SwarmDoublingSeconds))*mods.Rate;
                p.MaxAlive=Tuning.SwarmMaxAlive;p.Hp*=1+Tuning.SwarmHpPerMinute*over/60;
            } else {p.Rate=SpawnRate(minutes)*mods.Rate;p.MaxAlive=MaxAlive(minutes);}
        }

        // Avanza hasta `time` y lanza las oleadas, élites y enjambre que tocan.
        public void Update(double time,IDirectorEvents events)
        {
            double minutes=Difficulty(time);
            while(nextWave<Catalog.SpecialWaves.Length&&Catalog.SpecialWaves[nextWave].at<=minutes)events.Wave(Catalog.SpecialWaves[nextWave++]);
            while(nextElite<=minutes){events.Elite();nextElite+=Tuning.EliteEvery;}
            if(!Swarm&&time>=Duration){Swarm=true;events.Swarm();}
        }

        public static double SpawnRate(double minutes)
        {double t=Math.Max(0,minutes);return Math.Min(Tuning.SpawnCurveMaxRate,Tuning.SpawnCurveBaseRate+Tuning.SpawnCurveRateGrowth*t+Tuning.SpawnCurveRateCurve*t*t);}
        public static double MaxAlive(double minutes)
        =>Math.Min(Tuning.SpawnCurveMaxAliveCap,Rules.Round(Tuning.SpawnCurveBaseMaxAlive+Tuning.SpawnCurveMaxAliveGrowth*Math.Max(0,minutes)));
        public static double HpMultiplier(double minutes)
        {double t=Math.Max(0,minutes);return 1+Tuning.SpawnCurveHpGrowth*t+Tuning.SpawnCurveHpCurve*t*t;}
        public static double XpMultiplier(double minutes)=>1+Tuning.SpawnCurveXpGrowth*Math.Max(0,minutes);

        static int[] tableTypes;
        public static int EnemyIndex(string id)
        {int i=Array.FindIndex(Catalog.Enemies,e=>e.id==id);if(i<0)throw new ArgumentException("Enemigo desconocido: "+id);return i;}
        // pickEnemy: sorteo por pesos entre los disponibles en ese minuto (un único Next).
        public static int PickEnemy(double minutes,Rng rng)
        {
            var table=Catalog.SpawnTable;
            if(tableTypes==null){var types=new int[table.Length];for(int i=0;i<types.Length;i++)types[i]=EnemyIndex(table[i].enemy);tableTypes=types;}
            double total=0;int last=-1;
            for(int i=0;i<table.Length;i++)if(table[i].fromMinute<=minutes){total+=table[i].weight;last=i;}
            double roll=rng.Next()*total;
            for(int i=0;i<table.Length;i++){if(table[i].fromMinute>minutes)continue;roll-=table[i].weight;if(roll<0)return tableTypes[i];}
            return tableTypes[last<0?0:last];
        }
    }
}
