using System.Text;
using Mamporro.Core;
using UnityEngine;

namespace Mamporro.U2
{
    // IMGUI solo dibuja la pausa y el panel QA técnico (oculto durante el benchmark).
    public sealed class CombatMenu : MonoBehaviour
    {
        public CombatSession Session;
        GUIStyle label,button;Vector2 scroll;string notice;
        string T(string key)=>CombatText.Get(key);
        bool Button(string key,Rect rect)=>GUI.Button(rect,T(key),button);
        void OnGUI()
        {
            // La subida de nivel es la pantalla uGUI LevelUpScreen: aquí no se dibuja nada encima.
            if(!Session||Session.Run==null||!Session.View.Paused||Session.Measuring||(Session.Run.Choosing&&!Session.QaOpen))return;
            if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=23,wordWrap=true};button=new GUIStyle(GUI.skin.button){fontSize=22,wordWrap=true};}
            float scale=Screen.height/1080f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float width=Screen.width/scale;
            GUI.Box(new Rect(10,220,width-20,690),GUIContent.none);var r=Session.Run;
            if(Session.QaOpen){DrawQa(width);return;}
            GUI.Label(new Rect(40,255,1500,60),T(r.Dead?"u2.dead":"u2.paused"),label);
            GUI.Label(new Rect(40,335,width-80,70),T("u2.scenario"),label);
            GUI.Label(new Rect(40,425,width-80,80),T("character."+r.Character.id+".passive"),label);
            if(!r.Dead&&Button("u2.start",new Rect(40,530,540,65)))Session.View.SetPaused(false);
            if(Button("u2.restart",new Rect(40,610,540,65)))Session.Restart(r.Character.id=="remedios"?0:1);
            if(Button("u2.qa",new Rect(40,690,width-80,65)))Session.QaOpen=true;
            var items=new StringBuilder();foreach(var item in r.Items)items.Append(T("item."+item.Def.id)).Append(" ×").Append(item.Count).Append(" · ");GUI.Label(new Rect(40,785,width-80,95),items.ToString(),label);
        }
        void DrawQa(float width)
        {
            GUI.Label(new Rect(30,235,width-330,50),T("u2.qa"),label);
            if(Button("u2.resume",new Rect(width-280,235,240,50))){Session.CloseQa();return;}
            scroll=GUI.BeginScrollView(new Rect(25,300,width-50,590),scroll,new Rect(0,0,width-90,1370));
            float col=(width-110)/3;
            GUI.Label(new Rect(0,0,col,50),T("u2.weapons"),label);
            for(int i=0;i<Catalog.Weapons.Length;i++)if(Button("weapon."+Catalog.Weapons[i].id,new Rect(0,60+i*55,col-15,50)))if(Session.Run.AddWeapon(Catalog.Weapons[i].id)==null)notice=T("u2.full");
            if(Button("u2.clearWeapons",new Rect(0,400,col-15,55)))Session.Run.QaWeapons();
            GUI.Label(new Rect(col,0,col,50),T("u2.tomes"),label);
            for(int i=0;i<Catalog.Tomes.Length;i++)if(Button("tome."+Catalog.Tomes[i].id,new Rect(col,60+i*55,col-15,50)))if(!Session.Run.AddTome(Catalog.Tomes[i].id))notice=T("u2.full");
            GUI.Label(new Rect(col*2,0,col,50),T("u2.items"),label);
            for(int i=0;i<Catalog.Items.Length;i++)if(Button("item."+Catalog.Items[i].id,new Rect(col*2,60+i*55,col-15,50))){Session.Run.AddItem(Catalog.Items[i].id);notice=T("item."+Catalog.Items[i].id+".desc");}
            if(Button("u2.levelUp",new Rect(0,530,col-15,55))){Session.Run.GainXp(Rules.XpNeeded(Session.Run.Level)-Session.Run.Xp);Session.QaOpen=false;Session.OpenChoice();}
            if(GUI.Button(new Rect(0,595,col-15,55),T("u2.invincible")+" · "+Session.Run.Invincible,button))Session.Run.Invincible=!Session.Run.Invincible;
            if(Button("u2.goldQa",new Rect(0,660,col-15,55)))Session.Run.GainGold(100);
            if(Button("u2.damageQa",new Rect(0,725,col-15,55))){Session.Run.Invulnerable=0;Session.Run.Hurt(200);}
            if(Button("u2.replace",new Rect(col,530,col-15,80)))Session.ChangeCharacter();
            if(Button("u2.clearEnemies",new Rect(col,630,col-15,80))){Session.Run.Enemies.Clear();Session.TargetEnemies=0;}
            GUI.Label(new Rect(0,820,width-100,45),T("u2.spawn"),label);
            for(int i=0;i<6;i++)if(Button("enemy."+Catalog.Enemies[i].id,new Rect((i%3)*col,875+(i/3)*65,col-15,55))){Session.TargetEnemies=0;double x=Session.Run.Player.X,z=Session.Run.Player.Z+10;if(i==5)Session.Run.SpawnBoss(x,z);else Session.Run.SpawnScaled(i,x,z);}
            if(!string.IsNullOrEmpty(notice))GUI.Label(new Rect(0,1030,width-100,60),notice,label);
            GUI.EndScrollView();
        }
    }
}
