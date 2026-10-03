using Mamporro.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // Una malla original sencilla, lotes por material y almacenamiento reutilizado.
    public sealed class RunRenderer : MonoBehaviour
    {
        U3Game session;
        readonly Matrix4x4[][] matrices=new Matrix4x4[12][];readonly int[] counts=new int[12];
        readonly Matrix4x4[] chunk=new Matrix4x4[1023];readonly Material[] materials=new Material[12];
        readonly CombatEffect[] effects=new CombatEffect[512];int effectCount;
        public int DroppedEffects {get;private set;}
        public int EffectCount=>effectCount;
        public int DrawnInstances {get;private set;}
        public void Initialize(U3Game value)
        {
            session=value;
            Color[] colors={new Color(.7f,.67f,.8f),new Color(.35f,.18f,.08f),new Color(.1f,.7f,.55f),new Color(.55f,.65f,.9f),new Color(.65f,.2f,.3f),new Color(.55f,.35f,.7f),new Color(1,.35f,.65f),new Color(.1f,.9f,.5f),new Color(1,.8f,.1f),new Color(1,.15f,.1f),new Color(.9f,.85f,.65f),new Color(.25f,.7f,1)};
            for(int i=0;i<12;i++){matrices[i]=new Matrix4x4[8192];materials[i]=new Material(session.CombatMaterial){name="U3 "+i,enableInstancing=true};materials[i].SetColor("_BaseColor",colors[i].linear);}
        }
        public void Clear(){effectCount=0;DroppedEffects=0;DrawnInstances=0;System.Array.Clear(counts,0,counts.Length);}
        public void Emit(CombatEffect effect){if(effectCount<effects.Length)effects[effectCount++]=effect;else DroppedEffects++;}
        public void Step(double dt){for(int i=effectCount-1;i>=0;i--){effects[i].Life-=dt;if(effects[i].Life<=0)effects[i]=effects[--effectCount];}}
        void Box(int mat,Vector3 pos,Vector3 scale,Quaternion rotation=default)
        {if(counts[mat]>=matrices[mat].Length)return;if(rotation==default)rotation=Quaternion.identity;matrices[mat][counts[mat]++]=Matrix4x4.TRS(new Vector3(pos.x,pos.y,-pos.z),new Quaternion(-rotation.x,-rotation.y,rotation.z,rotation.w),scale);}
        void Line(int mat,Vector3 a,Vector3 b,float width=.08f)
        {var d=b-a;if(d.sqrMagnitude<.0001f)return;Box(mat,(a+b)*.5f,new Vector3(width,width,d.magnitude),Quaternion.LookRotation(d));}
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
                var rot=Quaternion.Euler(0,e.Heading[i]*Mathf.Rad2Deg,0);int color=e.Flash[i]>.3f?10:e.Type[i];
                Box(color,p+Vector3.up*(float)d.height*.5f,new Vector3((float)d.radius*1.7f,(float)d.height,(float)d.radius*1.5f),rot);
                Box(10,p+Vector3.up*(float)d.height*.75f+rot*Vector3.back*(float)d.radius*.7f,new Vector3((float)d.radius*.8f,.12f,.12f),rot);
                if(e.State[i]==1){float reach=e.Type[i]==5&&r.Boss?.Attack=="slam"?5.5f:(float)d.radius+1;Ring(9,p+Vector3.up*.1f,reach);Line(9,p+Vector3.up*.15f,p+new Vector3(e.AimX[i],0,e.AimZ[i])*6+Vector3.up*.15f,.18f);}
            }
            DrawProjectiles(r.Projectiles,6,alpha);DrawProjectiles(r.EnemyShots,9,alpha);
            DrawPickups(r.Gems,7);DrawPickups(r.Coins,8);
            for(int k=0;k<r.Weapons.Count;k++){
                var w=r.Weapons[k];var s=r.StateOf(k);
                if(w.Def.behavior=="orbit"&&s.Active){int n=(int)w[WStat.count];for(int j=0;j<n;j++){double a=s.Angle+j*System.Math.PI*2/n;Vector3 p=new Vector3((float)(r.Player.X+System.Math.Cos(a)*2.3*w[WStat.area]),(float)r.Player.Y+1,(float)(r.Player.Z+System.Math.Sin(a)*2.3*w[WStat.area]));Box(10,p,Vector3.one*(float)(.5*(.5+.5*w[WStat.area])));}}
                if(w.Def.behavior=="trail")for(int j=0;j<s.Count;j++)Box(11,new Vector3(s.X[j],s.Y[j]+.035f,s.Z[j]),new Vector3(s.Radius[j]*1.7f,.04f,s.Radius[j]*1.7f));
            }
            for(int i=0;i<effectCount;i++){
                var f=effects[i];var p=new Vector3((float)f.X,(float)f.Y+.1f,(float)f.Z);
                if(f.Kind=="chain"||f.Kind=="pearl")Line(f.Kind=="chain"?11:10,p,new Vector3((float)f.X2,(float)f.Y2,(float)f.Z2),.1f);
                else Ring(f.Kind=="aura"?7:f.Kind=="arc"?8:9,p,(float)f.Radius,(float)f.Angle*Mathf.Rad2Deg+180,f.Kind=="arc"?150:360);
            }
            for(int mat=0;mat<12;mat++)for(int offset=0;offset<counts[mat];offset+=1023){int n=Mathf.Min(1023,counts[mat]-offset);DrawnInstances+=n;System.Array.Copy(matrices[mat],offset,chunk,0,n);
                var rp=new RenderParams(materials[mat]){camera=session.worldCamera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=new Bounds(new Vector3(0,80,0),new Vector3(340,200,340))};Graphics.RenderMeshInstanced(rp,session.CombatMesh,0,chunk,n);}
        }
        void DrawProjectiles(Projectiles p,int color,float alpha){for(int i=0;i<p.Count;i++)Box(color,new Vector3(Mathf.Lerp(p.Px[i],p.X[i],alpha),Mathf.Lerp(p.Py[i],p.Y[i],alpha),Mathf.Lerp(p.Pz[i],p.Z[i],alpha)),new Vector3(p.Radius[i]*1.8f,.2f,p.Radius[i]*2.5f),Quaternion.Euler(0,p.Spin[i]*Mathf.Rad2Deg,0));}
        void DrawPickups(Pickups p,int color){for(int i=0;i<p.Count;i++)Box(color,new Vector3(p.X[i],p.Y[i]+Mathf.Sin(p.Phase[i])*.08f,p.Z[i]),Vector3.one*(p.Value[i]>=20?.42f:p.Value[i]>=5?.3f:.2f),Quaternion.Euler(0,45,45));}
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);}
    }
}
