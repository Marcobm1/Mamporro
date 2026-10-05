using Mamporro.Core;
using Math=System.Math;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // Una malla original sencilla, lotes por material y almacenamiento reutilizado.
    public sealed class RunRenderer : MonoBehaviour
    {
        U3Game session;
        // 12 colores de combate y la madera de las tapas de los baúles (12).
        const int Materials=13,Wood=12;
        readonly Matrix4x4[][] matrices=new Matrix4x4[Materials][];readonly int[] counts=new int[Materials];
        readonly Matrix4x4[] chunk=new Matrix4x4[1023];readonly Material[] materials=new Material[Materials];
        readonly CombatEffect[] effects=new CombatEffect[512];int effectCount;
        public int DroppedEffects {get;private set;}
        public int EffectCount=>effectCount;
        public int DrawnInstances {get;private set;}
        // B0 (solo QA): representación alternativa de los enemigos de ciertos tipos. Nula = U6.
        public IEnemyVisual EnemyVisual;
        // Instancias del estado de los interactuables (siempre presentes, haya o no combate).
        public int InteractableInstances {get;private set;}
        public void Initialize(U3Game value)
        {
            session=value;
            Color[] colors={new Color(.7f,.67f,.8f),new Color(.35f,.18f,.08f),new Color(.1f,.7f,.55f),new Color(.55f,.65f,.9f),new Color(.65f,.2f,.3f),new Color(.55f,.35f,.7f),new Color(1,.35f,.65f),new Color(.1f,.9f,.5f),new Color(1,.8f,.1f),new Color(1,.15f,.1f),new Color(.9f,.85f,.65f),new Color(.25f,.7f,1),new Color(.6f,.35f,.16f)};
            for(int i=0;i<Materials;i++){matrices[i]=new Matrix4x4[8192];materials[i]=new Material(session.CombatMaterial){name="U3 "+i,enableInstancing=true};materials[i].SetColor("_BaseColor",colors[i].linear);}
        }
        public void Clear(){effectCount=0;DroppedEffects=0;DrawnInstances=0;System.Array.Clear(counts,0,counts.Length);openedAt=null;}
        public void Emit(CombatEffect effect){if(effectCount<effects.Length)effects[effectCount++]=effect;else DroppedEffects++;}
        public void Step(double dt){for(int i=effectCount-1;i>=0;i--){effects[i].Life-=dt;if(effects[i].Life<=0)effects[i]=effects[--effectCount];}}
        // Sin rotación = identidad. Quaternion == compara por producto escalar y (0,0,0,0) nunca
        // es «igual» a default, así que se comprueba componente a componente.
        void Box(int mat,Vector3 pos,Vector3 scale,Quaternion rotation=default)
        {if(counts[mat]>=matrices[mat].Length)return;if(rotation.x==0&&rotation.y==0&&rotation.z==0&&rotation.w==0)rotation=Quaternion.identity;matrices[mat][counts[mat]++]=Matrix4x4.TRS(new Vector3(pos.x,pos.y,-pos.z),new Quaternion(-rotation.x,-rotation.y,rotation.z,rotation.w),scale);}
        void Line(int mat,Vector3 a,Vector3 b,float width=.08f,float height=-1)
        {var d=b-a;if(d.sqrMagnitude<.0001f)return;Box(mat,(a+b)*.5f,new Vector3(width,height<0?width:height,d.magnitude),Quaternion.LookRotation(d));}
        void Ring(int mat,Vector3 p,float r,float angle=0,float opening=360)
        {
            const int segments=24;var last=p+Quaternion.Euler(0,angle-opening/2,0)*Vector3.forward*r;
            for(int i=1;i<=segments;i++){var next=p+Quaternion.Euler(0,angle-opening/2+opening*i/segments,0)*Vector3.forward*r;Line(mat,last,next,.07f);last=next;}
        }
        public void Draw()
        {
            if(!isActiveAndEnabled){DrawnInstances=0;return;}
            System.Array.Clear(counts,0,counts.Length);DrawnInstances=0;var r=session.Run;var e=r.Enemies;
            float alpha=session.Paused?1:Mathf.Clamp01((Time.time-Time.fixedTime)/Time.fixedDeltaTime);
            for(int i=0;i<e.Count;i++){
                var d=e.Def(i);Vector3 p=new Vector3(Mathf.Lerp(e.Px[i],e.X[i],alpha),Mathf.Lerp(e.Py[i],e.Y[i],alpha),Mathf.Lerp(e.Pz[i],e.Z[i],alpha));
                var rot=Quaternion.Euler(0,e.Heading[i]*Mathf.Rad2Deg,0);// Destello blanco al recibir un golpe (escala web: élite 0,6, jefe 0,3), solo con «Destellos de daño».
                if(EnemyVisual==null||!EnemyVisual.Draws(e.Type[i])){
                    float flash=e.Flash[i]*(d.behavior=="boss"?.3f:d.behavior=="charger"?.6f:1f);int color=session.Settings.flashes&&flash>.3f?10:e.Type[i];
                    Box(color,p+Vector3.up*(float)d.height*.5f,new Vector3((float)d.radius*1.7f,(float)d.height,(float)d.radius*1.5f),rot);
                    Box(10,p+Vector3.up*(float)d.height*.75f+rot*Vector3.back*(float)d.radius*.7f,new Vector3((float)d.radius*.8f,.12f,.12f),rot);
                }
                // Telegrafiado (RunView.ts): embestida de la rata durante su preparación.
                if(e.State[i]==1&&d.charge!=null)TelegraphLine(e.X[i],e.Z[i],e.AimX[i],e.AimZ[i],d.charge.dashSpeed*d.charge.dashTime+d.radius,d.radius*2,1-e.StateTime[i]/d.charge.windup);
            }
            // Ataques del jefe: rodillo (franja) y culetazo (círculo) que se llenan durante la preparación.
            if(r.Boss!=null&&r.Boss.Phase=="windup"){
                int b=e.IndexOf(r.Boss.EnemyId);
                if(b>=0){double progress=r.Boss.PhaseLength>0?1-r.Boss.Timer/r.Boss.PhaseLength:0,radius=e.Radius(b);
                    if(r.Boss.Attack=="roll")TelegraphLine(e.X[b],e.Z[b],e.AimX[b],e.AimZ[b],22+radius,(radius+CombatPlayer.Radius)*2,progress);
                    else if(r.Boss.Attack=="slam")TelegraphCircle(e.X[b],e.Z[b],5.5,progress);}
            }
            EnemyVisual?.Draw(e,alpha,session.Settings.flashes,session.worldCamera);
            DrawProjectiles(r.Projectiles,6,alpha);DrawProjectiles(r.EnemyShots,9,alpha);
            DrawPickups(r.Gems,7);DrawPickups(r.Coins,8);
            int before=Total();DrawInteractables();InteractableInstances=Total()-before;
            for(int k=0;k<r.Weapons.Count;k++){
                var w=r.Weapons[k];var s=r.StateOf(k);
                if(w.Def.behavior=="orbit"&&s.Active){int n=(int)w[WStat.count];for(int j=0;j<n;j++){double a=s.Angle+j*System.Math.PI*2/n;Vector3 p=new Vector3((float)(r.Player.X+System.Math.Cos(a)*2.3*w[WStat.area]),(float)r.Player.Y+1,(float)(r.Player.Z+System.Math.Sin(a)*2.3*w[WStat.area]));Box(10,p,Vector3.one*(float)(.5*(.5+.5*w[WStat.area])));}}
                if(w.Def.behavior=="trail")for(int j=0;j<s.Count;j++)Box(11,new Vector3(s.X[j],s.Y[j]+.035f,s.Z[j]),new Vector3(s.Radius[j]*1.7f,.04f,s.Radius[j]*1.7f));
            }
            // Efectos de armas de WeaponEffects.ts (aura, barrazo, rayo). Desde U5, baúl, escudo, bata,
            // culetazo, olla y perla son partículas (RunFeedback), como en la web, no anillos.
            for(int i=0;i<effectCount;i++){
                var f=effects[i];var p=new Vector3((float)f.X,(float)f.Y+.1f,(float)f.Z);
                if(f.Kind=="chain")Line(11,p,new Vector3((float)f.X2,(float)f.Y2,(float)f.Z2),.1f);
                else if(f.Kind=="aura"||f.Kind=="arc")Ring(f.Kind=="aura"?7:8,p,(float)f.Radius,(float)f.Angle*Mathf.Rad2Deg+180,f.Kind=="arc"?150:360);
            }
            for(int mat=0;mat<Materials;mat++)for(int offset=0;offset<counts[mat];offset+=1023){int n=Mathf.Min(1023,counts[mat]-offset);DrawnInstances+=n;System.Array.Copy(matrices[mat],offset,chunk,0,n);
                var rp=new RenderParams(materials[mat]){camera=session.worldCamera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=new Bounds(new Vector3(0,80,0),new Vector3(340,200,340))};Graphics.RenderMeshInstanced(rp,session.CombatMesh,0,chunk,n);}
        }
        void DrawProjectiles(Projectiles p,int color,float alpha){for(int i=0;i<p.Count;i++)Box(color,new Vector3(Mathf.Lerp(p.Px[i],p.X[i],alpha),Mathf.Lerp(p.Py[i],p.Y[i],alpha),Mathf.Lerp(p.Pz[i],p.Z[i],alpha)),new Vector3(p.Radius[i]*1.8f,.2f,p.Radius[i]*2.5f),Quaternion.Euler(0,p.Spin[i]*Mathf.Rad2Deg,0));}
        void DrawPickups(Pickups p,int color){for(int i=0;i<p.Count;i++)Box(color,new Vector3(p.X[i],p.Y[i]+Mathf.Sin(p.Phase[i])*.08f,p.Z[i]),Vector3.one*(p.Value[i]>=20?.42f:p.Value[i]>=5?.3f:.2f),Quaternion.Euler(0,45,45));}
        // Estado de los interactuables (InteractableMeshes.update de la web): tapa de los baúles
        // con su apertura, zona y carga de las mesas camilla y brillos de tótem y armario.
        int Total(){int n=0;for(int i=0;i<counts.Length;i++)n+=counts[i];return n;}
        const float LidOpenTime=.45f,LidAngle=1.9f;
        float[] openedAt;
        void DrawInteractables()
        {
            var list=session.Session.Interactables.List;var hf=session.World.Heightfield;float now=Time.time;
            if(openedAt==null||openedAt.Length!=list.Length){openedAt=new float[list.Length];for(int i=0;i<list.Length;i++)openedAt[i]=float.NegativeInfinity;}
            float pulse=.75f+Mathf.Sin(now*4)*.25f;
            for(int i=0;i<list.Length;i++){
                var item=list[i];var s=item.Spot;var p=new Vector3((float)s.X,(float)s.Y,(float)s.Z);
                var turn=Quaternion.AngleAxis((float)s.Rotation*Mathf.Rad2Deg,Vector3.up);
                switch(s.Kind){
                    case "chest":{
                        if(item.Used&&float.IsNegativeInfinity(openedAt[i]))openedAt[i]=now;if(!item.Used)openedAt[i]=float.NegativeInfinity;
                        float t=Mathf.Clamp01((now-openedAt[i])/LidOpenTime),eased=1-(1-t)*(1-t);
                        var hinge=new Vector3(0,.55f,.38f);var open=Quaternion.AngleAxis(LidAngle*eased*Mathf.Rad2Deg,Vector3.right);
                        Box(Wood,p+turn*(hinge+open*new Vector3(0,.1f,-.38f)),new Vector3(1.3f,.26f,.86f),turn*open);
                        break;}
                    case "shrine":
                        if(item.Used){Box(10,p+Vector3.up*.05f,new Vector3(2,.07f,2),turn);break;}
                        GroundRing(8,hf,s.X,s.Z,Tuning.ShrineRadius,1);
                        if(item.Charge>0)GroundRing(9,hf,s.X,s.Z,Tuning.ShrineRadius-.15,(float)item.Charge);
                        Box(9,p+Vector3.up*.05f,new Vector3(2,.07f,2)*(.9f+.1f*pulse),turn);
                        break;
                    case "totem":Box(item.Used?10:9,p+Vector3.up*3.3f,Vector3.one*(item.Used?.3f:.3f+.1f*pulse),turn);break;
                    default:Box(item.Used?10:5,p+turn*new Vector3(0,1.42f,-.52f),new Vector3(.12f,2.1f,.06f),turn);break;
                }
            }
        }
        // Franjas y círculos de Telegraphs.ts: fondo y relleno que avanza con `progress`,
        // a 0,14 m sobre el terreno para no quedar enterrados en las cuestas.
        const int TelegraphSteps=14,CircleRings=4,CircleSegments=28;const double Lift=.14;
        void TelegraphLine(double x,double z,double dirX,double dirZ,double length,double width,double progress)
        {
            Strip(9,x,z,dirX,dirZ,length,width,0);
            Strip(4,x,z,dirX,dirZ,length*Math.Max(.02,Math.Min(1,progress)),width*.8,.02);
        }
        void Strip(int mat,double x,double z,double dirX,double dirZ,double length,double width,double extra)
        {
            var hf=session.World.Heightfield;Vector3 Point(double along){double px=x+dirX*along,pz=z+dirZ*along;return new Vector3((float)px,(float)(hf.HeightAt(px,pz)+Lift+extra),(float)pz);}
            var last=Point(0);for(int k=1;k<=TelegraphSteps;k++){var next=Point(length*k/TelegraphSteps);Line(mat,last,next,(float)width,.03f);last=next;}
        }
        void TelegraphCircle(double x,double z,double radius,double progress)
        {
            Disc(9,x,z,radius,0);Disc(4,x,z,radius*Math.Max(.02,Math.Min(1,progress)),.02);
        }
        void Disc(int mat,double x,double z,double radius,double extra)
        {
            var hf=session.World.Heightfield;double band=radius/CircleRings;
            for(int ring=0;ring<CircleRings;ring++){
                double rr=band*(ring+.5);Vector3 Point(int k){double a=k*Math.PI*2/CircleSegments,px=x+Math.Cos(a)*rr,pz=z+Math.Sin(a)*rr;return new Vector3((float)px,(float)(hf.HeightAt(px,pz)+Lift+extra),(float)pz);}
                var last=Point(0);for(int k=1;k<=CircleSegments;k++){var next=Point(k);Line(mat,last,next,(float)band*1.05f,.03f);last=next;}
            }
        }
        // Anillo que sigue el terreno; `fraction` dibuja solo esa parte (carga de la mesa).
        void GroundRing(int mat,Mamporro.Core.Heightfield hf,double x,double z,double r,float fraction)
        {
            const int segments=48;int n=Mathf.CeilToInt(segments*Mathf.Clamp01(fraction));if(n<=0)return;
            Vector3 Point(int k){double a=k*System.Math.PI*2/segments,px=x+System.Math.Sin(a)*r,pz=z+System.Math.Cos(a)*r;return new Vector3((float)px,(float)hf.HeightAt(px,pz)+.12f,(float)pz);}
            var last=Point(0);for(int k=1;k<=n;k++){var next=Point(k);Line(mat,last,next,.12f);last=next;}
        }
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);}
    }
}
