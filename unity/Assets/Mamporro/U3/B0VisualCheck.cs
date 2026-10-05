using System;
using System.Collections;
using System.IO;
using Mamporro.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // B0.4, solo con -b0-visual -b0-visual-check (build QA B0): capturas de la integración
    // visual para revisión humana. Partida real con guardado aislado; no mide rendimiento.
    [DefaultExecutionOrder(1001)]
    public sealed class B0VisualCheck : MonoBehaviour
    {
        public U3Game Game;public B0VisualAdapter Adapter;
        bool colliders,frontCamera;Material overlay;
        readonly Matrix4x4[] edges=new Matrix4x4[1023];int edgeCount;

        IEnumerator Start()
        {
            string output=Path.GetFullPath(Arg("-b0-output")??Path.Combine(Application.persistentDataPath,"B0Visual"));Directory.CreateDirectory(output);
            overlay=new Material(Game.CombatMaterial){name="B0 colliders",enableInstancing=true};overlay.SetColor("_BaseColor",new Color(1,.1f,.9f));
            Game.Audio.SetFocused(true);yield return null;
            Game.StartRun("MAMPORRO");Game.Run.Invincible=true;Game.HideInterface=true;Game.AutoChoose=true;
            // Cámara hacia el muro (web z = −10); la jugadora anda en paralelo a él (+X), porque el
            // muro QA no tiene collider; horda alrededor (F3 · 4: aparecen 100 enemigos).
            Game.FaceTowards(0,-10);Game.ScriptedIntent=new PlayerIntent{MoveX=1};Game.QaAction(4);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(output,"b0-juego");
            Game.SetPaused(true);yield return Frames(2);
            colliders=true;yield return Capture(output,"b0-colliders");colliders=false;
            Adapter.enabled=false;yield return Frames(2);yield return Capture(output,"b0-u6-mismo-instante");Adapter.enabled=true;yield return Frames(2);
            // Secuencia de andar vista de frente (cámara libre delante del jugador).
            frontCamera=true;Game.FreeCamera=true;Game.SetPaused(false);
            for(int k=0;k<6;k++){yield return new WaitForSecondsRealtime(.133f);yield return Capture(output,$"b0-andar-{k}");}
            Game.ScriptedIntent=new PlayerIntent();yield return new WaitForSecondsRealtime(1.2f);yield return Capture(output,"b0-inactiva");
            frontCamera=false;Game.FreeCamera=false;
            // Reinicio: partida nueva sin poses ni instancias residuales.
            Game.SetPaused(true);Game.BackToTitle();Game.StartRun("MAMPORRO");Game.SetPaused(true);yield return Frames(3);
            yield return Capture(output,"b0-reinicio");
            File.WriteAllText(Path.Combine(output,"b0-visual.json"),JsonUtility.ToJson(new Report{walk=Adapter.WalkWeight,animationTime=Adapter.AnimationTime,hordeInstances=Adapter.Horde.Instances,
                enemies=Game.Run.Enemies.Count,width=Screen.width,height=Screen.height,development=Debug.isDebugBuild},true));
            Debug.Log("B0 visual: capturas en "+output);Application.Quit(0);
        }
        static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:null;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Capture(string folder,string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;yield return null;
        }

        void LateUpdate()
        {
            if(frontCamera){var a=Game.AvatarRoot;var cam=Game.worldCamera.transform;cam.position=a.position+a.forward*3.2f+Vector3.up*1.3f;cam.LookAt(a.position+Vector3.up*.85f);}
            if(!colliders)return;
            // Colliders lógicos (cilindros) como cajas envolventes en alambre sobre el visual.
            edgeCount=0;var b=Game.Body;
            WireBox(new Vector3((float)b.X,(float)b.Y,(float)-b.Z),(float)Tuning.PlayerRadius,(float)Tuning.PlayerHeight);
            var e=Game.Run.Enemies;
            for(int i=0;i<e.Count&&edgeCount<edges.Length-12;i++)WireBox(new Vector3(e.X[i],e.Y[i],-e.Z[i]),(float)e.Radius(i),(float)e.Def(i).height);
            var rp=new RenderParams(overlay){camera=Game.worldCamera,shadowCastingMode=ShadowCastingMode.Off,worldBounds=new Bounds(Vector3.zero,Vector3.one*1000)};
            Graphics.RenderMeshInstanced(rp,Game.CombatMesh,0,edges,edgeCount);
        }
        void WireBox(Vector3 foot,float r,float h)
        {
            const float w=.03f;
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)edges[edgeCount++]=Matrix4x4.TRS(foot+new Vector3(sx*r,h*.5f,sz*r),Quaternion.identity,new Vector3(w,h,w));
            for(int y=0;y<=1;y++)for(int s=-1;s<=1;s+=2){
                edges[edgeCount++]=Matrix4x4.TRS(foot+new Vector3(0,y*h,s*r),Quaternion.identity,new Vector3(2*r,w,w));
                edges[edgeCount++]=Matrix4x4.TRS(foot+new Vector3(s*r,y*h,0),Quaternion.identity,new Vector3(w,w,2*r));
            }
        }
        void OnDestroy(){if(overlay)Destroy(overlay);}
        [Serializable]sealed class Report{public float walk;public double animationTime;public int hordeInstances,enemies,width,height;public bool development;}
    }
}
