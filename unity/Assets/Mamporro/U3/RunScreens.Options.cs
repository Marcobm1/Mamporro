using System;
using System.Text;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Paso 8 de U4: las 14 opciones guardadas (optionsPanel de la web) en la página Opciones del
    // menú y en la pausa. Cada control pasa por U3Game.ChangeSettings (validación y guardado de
    // ProgressSession) y se repinta con el valor vigente; aquí no hay reglas ni JSON.
    public sealed partial class RunScreens
    {
        GameObject pauseOptions;RectTransform pauseOptionsArea;Text fps;
        float fpsValue=60,fpsTimer;
        public bool PauseOptionsVisible=>pauseOptions&&pauseOptions.activeSelf;
        public bool FpsVisible=>fps&&fps.gameObject.activeSelf;
        public string FpsText=>fps.text;
        // Todo el texto visible de la pausa (pruebas): panel, confirmación y opciones abiertas.
        public string PauseAllText=>AllText(pause);
        static string AllText(GameObject root){var sb=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Text>())sb.Append(t.text).Append('\n');return sb.ToString();}

        void BuildPauseOptions()
        {
            pauseOptions=new GameObject("Opciones de pausa",typeof(RectTransform),typeof(Image));pauseOptions.transform.SetParent(Content(pause),false);
            var r=(RectTransform)pauseOptions.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;pauseOptions.GetComponent<Image>().color=Hex("#1d1730");
            Label(pauseOptions.transform,CombatText.Get("options.title"),52,TextAnchor.MiddleLeft,new Vector2(.03f,.88f),new Vector2(.6f,.97f));
            Button(pauseOptions.transform,CombatText.Get("options.close"),new Vector2(.7f,.89f),new Vector2(.97f,.96f),()=>ShowPauseOptions(false));
            var area=new GameObject("Rejilla",typeof(RectTransform));area.transform.SetParent(pauseOptions.transform,false);pauseOptionsArea=(RectTransform)area.transform;
            pauseOptionsArea.anchorMin=new Vector2(.03f,.03f);pauseOptionsArea.anchorMax=new Vector2(.97f,.86f);pauseOptionsArea.offsetMin=pauseOptionsArea.offsetMax=Vector2.zero;
            pauseOptions.SetActive(false);
        }
        // Opciones dentro de la pausa: la partida sigue en pausa al abrirlas, cambiarlas y cerrarlas.
        public void ShowPauseOptions(bool visible)
        {
            if(!pauseOptions)return;
            if(visible)PaintOptions(pauseOptionsArea,true);
            pauseOptions.SetActive(visible);
        }
        void BuildFps(Transform parent)
        {
            fps=Label(parent,"",28,TextAnchor.LowerRight,new Vector2(.8f,.01f),new Vector2(.99f,.06f));fps.gameObject.AddComponent<Outline>().effectColor=Hex("#140f24");
            fps.gameObject.SetActive(false);
        }
        // Contador de FPS de la opción «Mostrar FPS» (UI.updateFps: media suave, 4 veces por segundo).
        // Es independiente del panel F3.
        void LateUpdate()
        {
            if(game==null||fps==null||game.Progress==null)return;
            float dt=Time.unscaledDeltaTime;if(dt>0)fpsValue=Mathf.Lerp(fpsValue,1/dt,.08f);fpsTimer+=dt;
            bool show=game.Settings.showFps;if(fps.gameObject.activeSelf!=show){fps.gameObject.SetActive(show);fpsTimer=1;}
            if(show&&fpsTimer>=.25f){fpsTimer=0;fps.text=CombatText.Format("hud.fps","fps",Mathf.RoundToInt(fpsValue));}
        }

        // Rejilla de tres columnas: imagen y control, efectos y juego, y audio. En la pausa no
        // aparecen la duración (no cambia la partida en curso) ni la importación (solo en el inicio).
        void PaintOptions(RectTransform area,bool inPause)
        {
            for(int i=area.childCount-1;i>=0;i--)Destroy(area.GetChild(i).gameObject);
            area.DetachChildren();
            var s=game.Settings;
            void Repaint(){if(inPause){PaintOptions(area,true);pauseControls.text=Controls();}else OpenPage(Options);}
            void Change(Action<SettingsDto> apply){game.ChangeSettings(apply);Repaint();}
            // El idioma reconstruye todas las pantallas (U3Game.ApplySettings), también esta.
            Segmented(area,0,0,CombatText.Get("options.language"),new[]{CombatText.Get("lang.es"),CombatText.Get("lang.en")},s.language=="en"?1:0,i=>game.SetLanguage(i==0?"es":"en"));
            Stepper(area,0,1,CombatText.Get("options.sensitivity"),s.mouseSensitivity.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),
                s.mouseSensitivity>.2+1e-9,s.mouseSensitivity<3-1e-9,d=>Change(o=>o.mouseSensitivity=Math.Round(Math.Max(.2,Math.Min(3,o.mouseSensitivity+d*.1)),1)));
            int[] heights={240,360,480};
            Segmented(area,0,2,CombatText.Get("options.resolution"),Array.ConvertAll(heights,h=>CombatText.Format("options.resolutionValue","h",h)),Array.IndexOf(heights,s.renderHeight),i=>Change(o=>o.renderHeight=heights[i]));
            Toggle(area,0,3,CombatText.Get("options.vertexSnap"),s.vertexSnap,v=>Change(o=>o.vertexSnap=v));
            Toggle(area,0,4,CombatText.Get("options.dithering"),s.dithering,v=>Change(o=>o.dithering=v));
            Toggle(area,0,5,CombatText.Get("options.showFps"),s.showFps,v=>Change(o=>o.showFps=v));
            Toggle(area,1,0,CombatText.Get("options.reducedParticles"),s.reducedParticles,v=>Change(o=>o.reducedParticles=v));
            Toggle(area,1,1,CombatText.Get("options.cameraShake"),s.cameraShake,v=>Change(o=>o.cameraShake=v));
            Toggle(area,1,2,CombatText.Get("options.flashes"),s.flashes,v=>Change(o=>o.flashes=v));
            Toggle(area,1,3,CombatText.Get("options.slideCtrl"),s.slideWithCtrl,v=>Change(o=>o.slideWithCtrl=v));
            if(!inPause){int[] minutes=Catalog.RunDurations;
                Segmented(area,1,4,CombatText.Get("title.duration"),Array.ConvertAll(minutes,m=>CombatText.Format("title.minutes","n",m)),Array.IndexOf(minutes,s.runMinutes),i=>Change(o=>o.runMinutes=minutes[i]));}
            Stepper(area,2,0,CombatText.Get("options.musicVolume"),Percent(s.musicVolume),s.musicVolume>1e-9,s.musicVolume<1-1e-9,d=>Change(o=>o.musicVolume=Volume(o.musicVolume,d)));
            Stepper(area,2,1,CombatText.Get("options.effectsVolume"),Percent(s.effectsVolume),s.effectsVolume>1e-9,s.effectsVolume<1-1e-9,d=>Change(o=>o.effectsVolume=Volume(o.effectsVolume,d)));
            Toggle(area,2,2,CombatText.Get("options.muted"),s.muted,v=>Change(o=>o.muted=v));
            Label(area,CombatText.Get("options.audioPending"),20,TextAnchor.UpperLeft,new Vector2(.68f,.38f),new Vector2(1,.57f)).color=Hex("#a49cc0");
            if(!inPause){
                // Contrapartida Unity de «Exportar progreso para Unity» de la web.
                Label(area,CombatText.Get("import.help"),20,TextAnchor.UpperLeft,new Vector2(.68f,.16f),new Vector2(1,.36f)).color=Hex("#a49cc0");
                Small(Button(area,CombatText.Get("import.open"),new Vector2(.68f,.04f),new Vector2(1,.14f),()=>ShowImport(true)),24);
            }
            // Sin guardado utilizable, o tras un fallo, los cambios solo duran esta sesión.
            var p=game.Progress;
            if(!p.CanSave||p.SettingsSessionOnly)Label(area,CombatText.Get("options.sessionOnly"),22,TextAnchor.MiddleLeft,new Vector2(0,0),new Vector2(.66f,.1f)).color=Hex("#ff6a5a");
        }
        static double Volume(double value,int direction)=>Math.Round(Math.Max(0,Math.Min(1,value+direction*.05)),2);
        static string Percent(double value)=>Math.Round(value*100)+" %";

        // ------------------------------------------------------------ controles
        const float RowStep=.145f,RowHeight=.12f;
        static Vector2 LabelMin(int column,int row)=>new Vector2(.34f*column,.88f-RowStep*row);
        static Vector2 LabelMax(int column,int row)=>new Vector2(.34f*column+.15f,.88f-RowStep*row+RowHeight);
        static float ControlX(int column)=>.34f*column+.155f;
        const float ControlWidth=.165f;
        void RowLabel(Transform area,int column,int row,string text)=>Label(area,text,22,TextAnchor.MiddleLeft,LabelMin(column,row),LabelMax(column,row));
        static Button Small(Button b,int size){b.GetComponentInChildren<Text>().fontSize=size;return b;}
        void Segmented(Transform area,int column,int row,string label,string[] values,int selected,Action<int> choose)
        {
            RowLabel(area,column,row,label);float x=ControlX(column),w=ControlWidth/values.Length,y=LabelMin(column,row).y;
            for(int i=0;i<values.Length;i++){int index=i;
                var b=Small(Button(area,values[i],new Vector2(x+w*i+.002f,y+.015f),new Vector2(x+w*(i+1)-.002f,y+RowHeight-.015f),()=>{if(index!=selected)choose(index);}),20);
                b.targetGraphic.color=i==selected?Hex("#8a6cd8"):Hex("#463874");}
        }
        void Toggle(Transform area,int column,int row,string label,bool on,Action<bool> set)
        {
            RowLabel(area,column,row,label);float x=ControlX(column),y=LabelMin(column,row).y;
            var b=Small(Button(area,CombatText.Get(on?"options.on":"options.off"),new Vector2(x,y+.015f),new Vector2(x+ControlWidth*.6f,y+RowHeight-.015f),()=>set(!on)),22);
            b.targetGraphic.color=on?Hex("#8a6cd8"):Hex("#463874");
        }
        void Stepper(Transform area,int column,int row,string label,string value,bool canLower,bool canRaise,Action<int> step)
        {
            RowLabel(area,column,row,label);float x=ControlX(column),y=LabelMin(column,row).y,b=ControlWidth*.28f;
            Small(Button(area,"-",new Vector2(x,y+.015f),new Vector2(x+b,y+RowHeight-.015f),()=>step(-1)),26).interactable=canLower;
            Label(area,value,22,TextAnchor.MiddleCenter,new Vector2(x+b,y),new Vector2(x+ControlWidth-b,y+RowHeight));
            Small(Button(area,"+",new Vector2(x+ControlWidth-b,y+.015f),new Vector2(x+ControlWidth,y+RowHeight-.015f),()=>step(1)),26).interactable=canRaise;
        }
    }
}
