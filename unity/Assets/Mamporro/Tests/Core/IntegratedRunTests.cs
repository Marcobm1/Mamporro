using System;
using System.Collections.Generic;
using System.IO;
using Mamporro.Core;
using NUnit.Framework;
using UnityEngine;

namespace Mamporro.Tests
{
    // Las cuatro partidas completas de u3-world.json (runs), reproducidas con WorldRun y el
    // mismo guion que el generador web: ruta por vecino más cercano, E al llegar, primera carta
    // siempre, victoria tras 1,6 s. Tolerancia acordada: cronología exacta en los primeros
    // 120 s; después, totales con el margen documentado en PROGRESO_U3.md.
    public sealed class IntegratedRunTests
    {
        const double Dt=1.0/60;
        [Serializable] public class Reference { public RunRef[] runs; }
        [Serializable] public class RunRef {
            public string name,seed,character;public int minutes,ticks,detailTicks,finishedTick;public bool invincible,portalFirst,revealPortal,skipTotems;
            public RouteRef[] route;public EventRef[] events;public DetailRef[] detail;public TotalRef[] totals;public Counters counters;public FinalRef final; }
        [Serializable] public class RouteRef { public int index;public string kind;public double x,z; }
        [Serializable] public class EventRef { public int tick;public string kind,detail; }
        [Serializable] public class DetailRef { public int tick,level,kills,alive,target;public double x,y,z,hp,xp,gold;public float[] ex,ez; }
        [Serializable] public class TotalRef { public int tick,kills,alive,level,chests,spawned;public double gold,hp,time;public bool swarm; }
        [Serializable] public class Counters { public int spawned,killed,chests,items,levelUps; }
        [Serializable] public class FinalRef { public double time,gold;public int kills,level,chests;public bool cheated,victory,dead;public WeaponRef[] weapons;public ItemRef[] items; }
        [Serializable] public class WeaponRef { public string id;public int level,kills;public double damage; }
        [Serializable] public class ItemRef { public string id;public int count; }
        static Reference cached;
        static Reference Ref=>cached??=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u3-world.json")));
        static IEnumerable<string> Names(){foreach(var r in Ref.runs)yield return r.name;}
        public static RunRef[] References=>Ref.runs;

        // Resultado de reproducir una partida con el guion de la referencia.
        public sealed class Played
        {
            public readonly List<EventRef> Events=new List<EventRef>();
            public readonly List<DetailRef> Detail=new List<DetailRef>();
            public readonly List<TotalRef> Totals=new List<TotalRef>();
            public readonly List<RouteRef> Route=new List<RouteRef>();
            public int FinishedTick;public WorldRun Run;
        }

        // Orden del recorrido: interactuables por vecino más cercano desde el inicio.
        static List<RouteRef> Tour(List<InteractableSpot> spots,bool portalFirst,bool skipTotems)
        {
            var left=new List<RouteRef>();
            for(int i=0;i<spots.Count;i++){var s=spots[i];if(portalFirst?s.Kind=="portal":s.Kind!="portal"&&!(skipTotems&&s.Kind=="totem"))left.Add(new RouteRef{index=i,kind=s.Kind,x=s.X,z=s.Z});}
            var order=new List<RouteRef>();double x=0,z=0;
            while(left.Count>0){
                int best=0;double bestD=double.PositiveInfinity;
                for(int i=0;i<left.Count;i++){double d=JsMath.Hypot(left[i].x-x,left[i].z-z);if(d<bestD){bestD=d;best=i;}}
                var s=left[best];left.RemoveAt(best);order.Add(s);x=s.x;z=s.z;
            }
            return order;
        }

        // feedback (U5): observador opcional de sucesos; no debe cambiar nada de la partida.
        public static Played Play(RunRef input,ICombatFeedback feedback=null)
        {
            var data=WorldData.Generate(input.seed);var character=Array.Find(Catalog.Characters,c=>c.id==input.character);
            var s=new WorldRun(data,input.seed,character,null,input.minutes);var r=s.Combat;var p=new Played{Run=s};r.Feedback=feedback;
            // Guion web: Run.update sigue aunque quede una carta abierta (ver HoldWhileChoosing).
            r.HoldWhileChoosing=false;r.Invincible=input.invincible;if(input.revealPortal)s.Interactables.Reveal("portal");
            p.Route.AddRange(Tour(data.Interactables,input.portalFirst,input.skipTotems));
            int target=0,dwell=0,seen=0;
            for(int tick=1;tick<=input.ticks;tick++){
                double mx=0,mz=0;bool interact=false;
                if(target<p.Route.Count){
                    var wp=p.Route[target];double dx=wp.x-s.Body.X,dz=wp.z-s.Body.Z,d=JsMath.Hypot(dx,dz);dwell++;
                    if(d<1.5){
                        if(wp.kind=="shrine"){if(s.Interactables.List[wp.index].Used||dwell>700){target++;dwell=0;}}
                        else{interact=true;target++;dwell=0;}
                    } else if(dwell>1800){target++;dwell=0;}
                    else{mx=dx/d;mz=dz/d;}
                }
                double yaw=mx!=0||mz!=0?Math.Atan2(-mx,-mz):0;
                s.Step(new PlayerIntent{MoveX=mx,MoveZ=mz},Dt,yaw,interact);
                for(;seen<r.Events.Count;seen++){
                    var e=r.Events[seen];
                    // La web registra dos entradas al revivir: el efecto y el aviso.
                    p.Events.Add(new EventRef{tick=tick,kind=e.Kind,detail=e.Detail});
                    if(e.Kind=="revive")p.Events.Add(new EventRef{tick=tick,kind=e.Kind,detail=e.Detail});
                }
                bool done=false;
                if(r.Dead){p.FinishedTick=tick;p.Events.Add(new EventRef{tick=tick,kind="defeat",detail=""});done=true;}
                else if(r.Victory){if(s.Finished){p.FinishedTick=tick;p.Events.Add(new EventRef{tick=tick,kind="victory",detail=""});done=true;}}
                // Igual que la web: tras elegir se abre la siguiente y openChoice devuelve false,
                // así que con dos subidas en el mismo tick la segunda carta queda abierta.
                else while(r.OpenChoice())r.Choose(0);
                if(done)break;
                // Las elecciones también producen sucesos (subidas encadenadas, objetos...).
                for(;seen<r.Events.Count;seen++){var e=r.Events[seen];p.Events.Add(new EventRef{tick=tick,kind=e.Kind,detail=e.Detail});}
                if(tick<=input.detailTicks&&tick%10==0){
                    int n=Math.Min(8,r.Enemies.Count);var ex=new float[n];var ez=new float[n];Array.Copy(r.Enemies.X,ex,n);Array.Copy(r.Enemies.Z,ez,n);
                    p.Detail.Add(new DetailRef{tick=tick,x=s.Body.X,y=s.Body.Y,z=s.Body.Z,hp=r.Hp,level=r.Level,xp=r.Xp,gold=r.Gold,kills=r.Kills,alive=r.Enemies.Count,target=target,ex=ex,ez=ez});
                }
                if(tick%60==0)p.Totals.Add(new TotalRef{tick=tick,kills=r.Kills,alive=r.Enemies.Count,level=r.Level,gold=r.Gold,chests=s.Interactables.ChestsOpened,hp=r.Hp,time=r.Time,swarm=s.Swarm,spawned=s.Spawned});
            }
            return p;
        }

        static string Ev(EventRef e)=>$"{e.tick} {e.kind} {e.detail}";

        [Test]public void RouteMatchesWeb([ValueSource(nameof(Names))]string name)
        {
            var e=Array.Find(Ref.runs,x=>x.name==name);var data=WorldData.Generate(e.seed);
            var route=Tour(data.Interactables,e.portalFirst,e.skipTotems);
            Assert.That(route.Count,Is.EqualTo(e.route.Length));
            for(int i=0;i<route.Count;i++){Assert.That(route[i].index,Is.EqualTo(e.route[i].index));Assert.That(route[i].kind,Is.EqualTo(e.route[i].kind));}
        }

        // Ventana de cronología exacta acordada (120 s) y margen de los totales posteriores.
        const int ExactTicks=7200;
        static bool Within(double actual,double expected,double fraction,double minimum)=>Math.Abs(actual-expected)<=Math.Max(minimum,Math.Abs(expected)*fraction);

        [Test]public void MatchesWebChronologyAndTotals([ValueSource(nameof(Names))]string name)
        {
            var e=Array.Find(Ref.runs,x=>x.name==name);var p=Play(e);var r=p.Run.Combat;
            // Informe: primera diferencia (si la hay) y desvío máximo de posición en todo el detalle.
            int firstEvent=-1;for(int i=0;i<Math.Min(p.Events.Count,e.events.Length);i++)if(Ev(p.Events[i])!=Ev(e.events[i])){firstEvent=i;break;}
            double worst=0;for(int i=0;i<Math.Min(p.Detail.Count,e.detail.Length);i++){var a=p.Detail[i];var b=e.detail[i];worst=Math.Max(worst,Math.Max(Math.Abs(a.x-b.x),Math.Max(Math.Abs(a.y-b.y),Math.Abs(a.z-b.z))));}
            TestContext.WriteLine($"{name}: sucesos {p.Events.Count}/{e.events.Length}, primera diferencia: {(firstEvent<0?"ninguna":Ev(p.Events[firstEvent])+" frente a "+Ev(e.events[firstEvent]))}; desvío máximo de posición {worst:E2} m");
            TestContext.WriteLine($"  final web t {e.final.time:F2} bajas {e.final.kills} nivel {e.final.level} oro {e.final.gold} fin {e.finishedTick} | C# t {r.Time:F2} bajas {r.Kills} nivel {r.Level} oro {r.Gold} fin {p.FinishedTick}");

            // 1) Primeros 120 s: cronología exacta.
            var early=new List<string>();var earlyRef=new List<string>();
            foreach(var x in p.Events)if(x.tick<=ExactTicks)early.Add(Ev(x));
            foreach(var x in e.events)if(x.tick<=ExactTicks)earlyRef.Add(Ev(x));
            Assert.That(early,Is.EqualTo(earlyRef),"sucesos de los primeros 120 s");
            for(int i=0;i<e.detail.Length&&e.detail[i].tick<=ExactTicks;i++){
                Assert.That(i,Is.LessThan(p.Detail.Count),"filas de detalle");
                var a=p.Detail[i];var b=e.detail[i];string what=$"{name} tick {b.tick}";
                Assert.That(a.tick,Is.EqualTo(b.tick),what);Assert.That(a.target,Is.EqualTo(b.target),what+" objetivo");
                Assert.That(a.level,Is.EqualTo(b.level),what+" nivel");Assert.That(a.kills,Is.EqualTo(b.kills),what+" bajas");Assert.That(a.alive,Is.EqualTo(b.alive),what+" vivos");
                Assert.That(a.x,Is.EqualTo(b.x).Within(1e-6),what+" x");Assert.That(a.y,Is.EqualTo(b.y).Within(1e-6),what+" y");Assert.That(a.z,Is.EqualTo(b.z).Within(1e-6),what+" z");
                Assert.That(a.hp,Is.EqualTo(b.hp).Within(1e-6),what+" vida");Assert.That(a.xp,Is.EqualTo(b.xp).Within(1e-6),what+" xp");Assert.That(a.gold,Is.EqualTo(b.gold).Within(1e-6),what+" oro");
                Assert.That(a.ex.Length,Is.EqualTo(b.ex.Length),what+" enemigos");
                for(int k=0;k<a.ex.Length;k++){Assert.That(a.ex[k],Is.EqualTo(b.ex[k]).Within(1e-4),what+" enemigo x "+k);Assert.That(a.ez[k],Is.EqualTo(b.ez[k]).Within(1e-4),what+" enemigo z "+k);}
            }
            foreach(var t in e.totals){
                if(t.tick>ExactTicks)break;var a=p.Totals.Find(x=>x.tick==t.tick);Assert.That(a,Is.Not.Null,"totales "+t.tick);
                Assert.That(a.kills,Is.EqualTo(t.kills));Assert.That(a.alive,Is.EqualTo(t.alive));Assert.That(a.spawned,Is.EqualTo(t.spawned),"apariciones "+t.tick);
                Assert.That(a.chests,Is.EqualTo(t.chests));Assert.That(a.gold,Is.EqualTo(t.gold).Within(1e-6));
            }

            // 2) Después: totales cada segundo con margen (bajas, apariciones y nivel ±3 %, vivos ±5 %, oro ±5 %).
            foreach(var t in e.totals){
                if(t.tick<=ExactTicks)continue;var a=p.Totals.Find(x=>x.tick==t.tick);Assert.That(a,Is.Not.Null,"totales "+t.tick);string what=$"{name} totales {t.tick}";
                Assert.That(Within(a.kills,t.kills,.03,3),Is.True,what+$" bajas {a.kills} frente a {t.kills}");
                Assert.That(Within(a.spawned,t.spawned,.03,3),Is.True,what+$" apariciones {a.spawned} frente a {t.spawned}");
                Assert.That(Within(a.level,t.level,.03,1),Is.True,what+$" nivel {a.level} frente a {t.level}");
                Assert.That(Within(a.alive,t.alive,.05,10),Is.True,what+$" vivos {a.alive} frente a {t.alive}");
                Assert.That(Within(a.gold,t.gold,.05,10),Is.True,what+$" oro {a.gold} frente a {t.gold}");
                Assert.That(a.swarm,Is.EqualTo(t.swarm),what+" enjambre");Assert.That(Within(a.chests,t.chests,0,1),Is.True,what+" baúles");
            }
            // 3) Desenlace: el mismo, en el mismo segundo.
            Assert.That(r.Victory,Is.EqualTo(e.final.victory),"victoria");Assert.That(r.Dead,Is.EqualTo(e.final.dead),"derrota");
            Assert.That(p.FinishedTick,Is.EqualTo(e.finishedTick).Within(60),"tick final");
            Assert.That(p.Run.Cheated,Is.EqualTo(e.final.cheated));
            Assert.That(Within(r.Kills,e.final.kills,.03,3),Is.True,"bajas finales");Assert.That(Within(r.Level,e.final.level,.03,1),Is.True,"nivel final");
            var ids=new List<string>();foreach(var w in r.Weapons)ids.Add(w.Def.id);var refIds=new List<string>();foreach(var w in e.final.weapons)refIds.Add(w.id);
            if(e.finishedTick>0&&e.finishedTick<=ExactTicks)Assert.That(ids,Is.EqualTo(refIds),"armas");
        }
    }
}
