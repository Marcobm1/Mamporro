using System;
using System.Diagnostics;
using System.Text;
using Mamporro.Core;
using Mamporro.U1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mamporro.U2
{
    public sealed class CombatSession : PrototypeSession,ICombatEffects
    {
        public const string Seed="U2-COMBATE-6741";
        public CombatRun Run {get;private set;}
        public PrototypeController View {get;private set;}
        public CombatRenderer Presenter {get;private set;}
        public CombatBenchmark Benchmark {get;private set;}
        public LevelUpScreen Cards {get;private set;}
        public double LastTickMs {get;private set;}
        public long TickCount {get;private set;}
        public int TargetEnemies=60;
        public bool QaOpen;
        long tickStart;
        int character;double spawnClock;Rng spawnRng;
        readonly StringBuilder text=new StringBuilder(1024);
        readonly CombatWorld world=new CombatWorld();
        public override bool AllowResume=>Run!=null&&!Run.Choosing&&!Run.Dead&&!QaOpen;
        public override bool Measuring=>Benchmark&&Benchmark.Active;
        public override void Initialize(PrototypeController controller)
        {
            View=controller;CombatText.Load();Presenter=gameObject.AddComponent<CombatRenderer>();Presenter.Initialize(this);
            Benchmark=gameObject.AddComponent<CombatBenchmark>();Benchmark.Session=this;
            Cards=gameObject.AddComponent<LevelUpScreen>();Cards.Session=this;Cards.Build();
            Restart(0);gameObject.AddComponent<CombatMenu>().Session=this;
        }
        void Start(){Benchmark.ReadCommandLine();if(Array.IndexOf(Environment.GetCommandLineArgs(),"-u2-visual-check")>=0)gameObject.AddComponent<CombatVisualCheck>().Session=this;}
        public void Restart(int selected)
        {
            character=selected;Run=new CombatRun(world,Seed,Catalog.Characters[character],4096,this);spawnRng=new Rng(Seed+"/qa-spawn");spawnClock=0;TickCount=0;QaOpen=false;
            View.ResetTrial(0);View.Motor.SpeedMultiplier=1;View.Motor.CrowdSlow=0;SyncPlayer();Presenter.Clear();Cards.Hide();
            for(int i=0;i<TargetEnemies;i++)SpawnControlled(i%4);
            View.SetPaused(true);RefreshHud();
        }
        public void ChangeCharacter()=>Restart(1-character);
        void SyncPlayer()
        {
            var p=View.Motor.Position;var v=View.Motor.Velocity;var r=Run.Player;
            r.X=p.x;r.Y=p.y;r.Z=p.z;r.Vx=v.x;r.Vz=v.z;r.Grounded=View.Motor.Grounded;
            if(v.x*v.x+v.z*v.z>.04)r.Facing=Math.Atan2(-v.x,-v.z);
        }
        public void SpawnControlled(int type)
        {
            for(int attempt=0;attempt<30;attempt++){
                double angle=spawnRng.Next()*Math.PI*2,radius=spawnRng.Range(15,32),x=Run.Player.X+Math.Cos(angle)*radius,z=Run.Player.Z+Math.Sin(angle)*radius;
                world.Clamp(ref x,ref z);double y=world.Height(x,z);if(TechnicalWorld.Blocked(new Vector3((float)x,(float)y,(float)z),(float)Catalog.Enemies[type].radius))continue;
                Run.SpawnScaled(type,x,z);return;
            }
        }
        public override void UpdateSession()
        {
            var k=Keyboard.current;if(k==null)return;
            // F7 idioma y F8 reinicio (R, X y B son de la subida de nivel).
            if(k.f7Key.wasPressedThisFrame){CombatText.English=!CombatText.English;RefreshHud();if(Cards.Visible)Cards.Paint();}
            if(Measuring)return;
            if(k.f8Key.wasPressedThisFrame){Restart(character);return;}
            if(k.f4Key.wasPressedThisFrame&&!Run.Choosing){QaOpen=!QaOpen;View.SetPaused(true);}
        }
        // Acciones de la subida de nivel. Elegir y saltar cierran la elección (la siguiente
        // pendiente se abre como nueva); volver a tirar y descartar solo cambian las cartas.
        public void Choose(int index){if(Run.Choose(index))AfterAction(true);}
        public void Skip(){if(Run.Skip())AfterAction(true);}
        public void Reroll(){if(Run.Reroll())AfterAction(false);}
        public void Banish(int index){if(Run.Banish(index))AfterAction(false);}
        void AfterAction(bool closes)
        {
            if(Run.Choosing)Cards.Show(closes);
            else{Cards.Hide();if(!QaOpen)View.SetPaused(false);}
            RefreshHud();
        }
        // Abre la elección pendiente (QA o comprobación visual).
        public void OpenChoice(){if(Run.OpenChoice()||Run.Choosing){View.SetPaused(true);Cards.Show(true);RefreshHud();}}
        public void CloseQa(){QaOpen=false;if(!Run.Choosing&&!Run.Dead)View.SetPaused(false);}
        public override void BeforeMovement()
        {
            tickStart=Stopwatch.GetTimestamp();
            if(Measuring) View.SetScriptedIntent(Benchmark.ScriptedIntent());
            View.Motor.SpeedMultiplier=(float)Run.Stats[Stat.moveSpeed];View.Motor.CrowdSlow=(float)Run.CrowdSlow;
        }
        public override void AfterMovement()
        {
            SyncPlayer();Run.Step(1.0/60);
            View.Motor.Push((float)(Run.Player.X-View.Motor.Position.x),(float)(Run.Player.Z-View.Motor.Position.z));
            spawnClock+=1.0/60;
            if(Measuring){while(Run.Enemies.Count<TargetEnemies)SpawnControlled(Run.Enemies.Count%4);Run.PendingLevels=0;Run.Offer=null;}
            else if(spawnClock>=1){spawnClock=0;for(int k=0;k<3&&Run.Enemies.Count<TargetEnemies;k++)SpawnControlled(k%4);}
            Presenter.Step(1.0/60);TickCount++;LastTickMs=(Stopwatch.GetTimestamp()-tickStart)*1000.0/Stopwatch.Frequency;
            Benchmark.RecordTick(LastTickMs);
            if(Run.Choosing&&!Cards.Visible)Cards.Show(true);
            if(Run.Choosing||Run.Dead)View.SetPaused(true);
        }
        public override void Draw()=>Presenter.Draw();
        public void Emit(CombatEffect effect)=>Presenter.Emit(effect);
        public override void RefreshHud()
        {
            if(Run==null)return;text.Clear();text.AppendLine(CombatText.Get("u2.title"));
            text.Append(CombatText.Get("character."+Run.Character.id)).Append(" · ").Append(CombatText.Get("u2.health")).Append(' ').Append(Run.Hp.ToString("F0")).Append('/').Append(Run.Stats[Stat.maxHp].ToString("F0"));
            text.Append(" · ").Append(CombatText.Get("u2.level")).Append(' ').Append(Run.Level).Append(" · XP ").Append(Run.Xp.ToString("F0")).Append('/').Append(Rules.XpNeeded(Run.Level));
            // Oro de la partida, como el HUD web (hud.gold con el valor redondeado hacia abajo).
            text.Append(" · ").Append(CombatText.Format("hud.gold","n",Math.Floor(Run.Gold)));
            text.Append(" · ").Append(CombatText.Get("u2.kills")).Append(' ').Append(Run.Kills).Append(" · ").Append(CombatText.Get("u2.enemies")).Append(' ').Append(Run.Enemies.Count).AppendLine();
            foreach(var w in Run.Weapons)text.Append(CombatText.Get("weapon."+w.Def.id)).Append(" Lv").Append(w.Level).Append(" · ");text.AppendLine();
            foreach(var t in Run.Tomes)text.Append(CombatText.Get("tome."+t.Def.id)).Append(" Lv").Append(t.Level).Append(" · ");text.AppendLine();
            text.Append(Screen.width).Append('×').Append(Screen.height).Append(" / ").Append(View.InternalHeight).Append("p · ").Append(LastTickMs.ToString("F2")).Append(" ms/tick");
            if(Measuring)text.Append(" · ").Append(CombatText.Get("u2.measuring"));
            View.status.text=text.ToString();View.help.text=CombatText.Get("u2.controls");
        }
    }
}
