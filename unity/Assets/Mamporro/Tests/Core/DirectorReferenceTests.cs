using System;
using System.Collections.Generic;
using System.IO;
using Mamporro.Core;
using NUnit.Framework;
using UnityEngine;

namespace Mamporro.Tests
{
    // Director y apariciones contra las secciones director y spawns de u3-world.json
    // (web aprobada). Lo discreto (eventos, ticks, recuentos, tipos) exacto; posiciones y
    // multiplicadores con 1e-6. JsonUtility puede desviar un bit algunos double.
    public sealed class DirectorReferenceTests
    {
        const double Dt=1.0/60,Tolerance=1e-6;
        [Serializable] public class Reference { public string source;public DirectorRef[] director;public SpawnsRef spawns; }
        [Serializable] public class DirectorRef { public int minutes;public double pace,duration;public DirectorSample[] samples;public DirectorEvent[] events; }
        [Serializable] public class DirectorSample { public double time,minutes,rate,maxAlive,hp,xp,gold; }
        [Serializable] public class DirectorEvent { public int tick,count;public string kind,enemy,formation; }
        [Serializable] public class SpawnsRef {
            public string seed,rng;public double startTime;public Point[] spawned;public int total;public Count[] counts;
            public float[] finalX,finalZ;public int[] finalType;public Formation[] formations; }
        [Serializable] public class Point { public double x,y,z; }
        [Serializable] public class Count { public int tick,count; }
        [Serializable] public class Formation { public string formation;public int count;public float[] x,y,z,hp;public int[] type; }

        static Reference cached;
        static Reference Ref=>cached??=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u3-world.json")));
        static void Near(double actual,double expected,string what)=>Assert.That(actual,Is.EqualTo(expected).Within(Tolerance),what);
        static IEnumerable<int> Durations(){yield return 5;yield return 10;yield return 15;}
        static DirectorRef Case(int minutes)=>Array.Find(Ref.director,d=>d.minutes==minutes);

        [Test]public void ReferenceComesFromApprovedWeb()=>Assert.That(Ref.source,Is.EqualTo("0505b1690656d15188860157612455639820fe1f"));

        [Test]public void SpawnParamsMatchWebEveryFiveSeconds([ValueSource(nameof(Durations))]int minutes)
        {
            var e=Case(minutes);var d=new Director(minutes);
            Assert.That(d.Pace,Is.EqualTo(e.pace));Assert.That(d.Duration,Is.EqualTo(e.duration));
            Assert.That(e.samples.Length,Is.EqualTo((minutes*60+90)/5+1));
            var p=new SpawnParams();
            foreach(var s in e.samples){
                d.Params(s.time,SpawnModifiers.None,ref p);string what=$"{minutes} min, t={s.time}";
                Near(p.Minutes,s.minutes,what+" minuto");Near(p.Rate,s.rate,what+" ritmo");
                Assert.That(p.MaxAlive,Is.EqualTo(s.maxAlive),what+" máximo vivos");
                Near(p.Hp,s.hp,what+" vida");Near(p.Xp,s.xp,what+" experiencia");Near(p.Gold,s.gold,what+" oro");
            }
        }

        sealed class Log : IDirectorEvents
        {
            public readonly List<string> Items=new List<string>();public int Tick;
            public void Wave(SpecialWave w)=>Items.Add($"{Tick} wave {w.enemy} {w.count} {w.formation}");
            public void Elite()=>Items.Add($"{Tick} elite");
            public void Swarm()=>Items.Add($"{Tick} swarm");
        }
        [Test]public void WavesElitesAndSwarmFireOnTheSameTicks([ValueSource(nameof(Durations))]int minutes)
        {
            var e=Case(minutes);var d=new Director(minutes);var log=new Log();double time=0;
            for(int tick=1;tick<=(minutes*60+90)*60;tick++){time+=Dt;log.Tick=tick;d.Update(time,log);}
            var expected=new List<string>();
            foreach(var x in e.events)expected.Add(x.kind=="wave"?$"{x.tick} wave {x.enemy} {x.count} {x.formation}":$"{x.tick} {x.kind}");
            Assert.That(log.Items,Is.EqualTo(expected));
            // Seis oleadas, élite cada 2 minutos de dificultad y un enjambre al acabar el tiempo.
            Assert.That(expected.FindAll(s=>s.Contains(" wave ")).Count,Is.EqualTo(6));
            Assert.That(d.Swarm,Is.True);Assert.That(d.TimeLeft(minutes*60+30),Is.EqualTo(-30));
        }

        [Test]public void ContinuousSpawningMatchesWebForOneMinute()
        {
            var r=Ref.spawns;var world=WorldData.Generate(r.seed);var c=world.Collision;
            var enemies=new Enemies(WorldRun.EnemyCapacity,world.Heightfield.Size);
            var spawned=new List<Point>();var spawner=new WorldSpawns(new Rng(r.rng)){OnSpawn=(x,y,z)=>spawned.Add(new Point{x=x,y=y,z=z})};
            var d=new Director(10);var p=new SpawnParams();var counts=new List<Count>();
            for(int tick=1;tick<=3600;tick++){
                d.Params(r.startTime+tick*Dt,SpawnModifiers.None,ref p);
                double x=Math.Sin(tick*.004)*60,z=Math.Cos(tick*.003)*60-30,yaw=tick*.01;
                spawner.Update(Dt,ref p,enemies,c,x,z,yaw);
                if(tick%60==0)counts.Add(new Count{tick=tick,count=enemies.Count});
            }
            Assert.That(spawned.Count,Is.EqualTo(r.total),"apariciones");Assert.That(spawner.Spawned,Is.EqualTo(r.total));
            for(int i=0;i<r.spawned.Length;i++){
                Near(spawned[i].x,r.spawned[i].x,"aparición "+i+" x");Near(spawned[i].y,r.spawned[i].y,"aparición "+i+" y");Near(spawned[i].z,r.spawned[i].z,"aparición "+i+" z");
            }
            Assert.That(counts.Count,Is.EqualTo(r.counts.Length));
            for(int i=0;i<counts.Count;i++)Assert.That(counts[i].count,Is.EqualTo(r.counts[i].count),"vivos en el tick "+r.counts[i].tick);
            Assert.That(enemies.Count,Is.EqualTo(r.finalX.Length));
            for(int i=0;i<enemies.Count;i++){
                Assert.That(enemies.Type[i],Is.EqualTo(r.finalType[i]),"tipo "+i);
                Near(enemies.X[i],r.finalX[i],"final x "+i);Near(enemies.Z[i],r.finalZ[i],"final z "+i);
            }
        }

        [Test]public void FormationsMatchWeb([Values("line","ring","arc")]string formation)
        {
            var world=WorldData.Generate(Ref.spawns.seed);var e=Array.Find(Ref.spawns.formations,f=>f.formation==formation);
            var enemies=new Enemies(WorldRun.EnemyCapacity,world.Heightfield.Size);var spawner=new WorldSpawns(new Rng("U3-FORMACION-"+formation));
            var p=new SpawnParams();new Director(10).Params(300,SpawnModifiers.None,ref p);
            int n=spawner.SpawnFormation(0,20,formation,ref p,enemies,world.Collision,10,-20,.7);
            Assert.That(n,Is.EqualTo(e.count));Assert.That(enemies.Count,Is.EqualTo(e.count));
            for(int i=0;i<e.count;i++){
                string what=formation+" "+i;Assert.That(enemies.Type[i],Is.EqualTo(e.type[i]),what);
                Near(enemies.X[i],e.x[i],what+" x");Near(enemies.Y[i],e.y[i],what+" y");Near(enemies.Z[i],e.z[i],what+" z");Near(enemies.Hp[i],e.hp[i],what+" vida");
            }
        }

        [Test]public void PickEnemyFollowsTableAndMinutes()
        {
            var rng=new Rng("U3-SORTEO");var seen=new HashSet<int>();
            for(int i=0;i<400;i++)seen.Add(Director.PickEnemy(0,rng));
            Assert.That(seen,Is.EquivalentTo(new[]{0}),"minuto 0: solo pelusas");
            seen.Clear();for(int i=0;i<4000;i++)seen.Add(Director.PickEnemy(5,rng));
            Assert.That(seen,Is.EquivalentTo(new[]{0,1,2,3}),"minuto 5: los cuatro normales, nunca élite ni jefe");
        }
    }
}
