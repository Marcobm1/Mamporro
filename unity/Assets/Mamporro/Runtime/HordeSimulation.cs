using System;
using UnityEngine;

namespace Mamporro.U1
{
    // Pool plano y rejilla de listas enlazadas reutilizadas; sin GameObjects por enemigo.
    public sealed class HordeSimulation
    {
        const int Side = 64;
        const float Cell = 1.5f;
        readonly int[] heads = new int[Side*Side];
        readonly int[] links;
        readonly Vector3[] next;
        public readonly Vector3[] Positions;
        public int Count { get; private set; }
        public HordeSimulation(int capacity) { Positions = new Vector3[capacity]; next = new Vector3[capacity]; links = new int[capacity]; }
        public void Reset(int count, int seed)
        {
            if (count < 0 || count > Positions.Length) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            var random = new System.Random(seed);
            for (int i=0;i<count;i++)
            {
                Vector3 p;
                do { p = new Vector3((float)random.NextDouble()*80-40,0,(float)random.NextDouble()*80-40); }
                while (TechnicalWorld.Blocked(p,.45f));
                p.y = TechnicalWorld.Height(p.x,p.z); Positions[i] = p;
            }
        }
        static int Axis(float v) => Mathf.Clamp((int)((v+48)/Cell),0,Side-1);
        public void Step(Vector3 target, float speed, float dt)
        {
            Array.Fill(heads,-1);
            for(int i=0;i<Count;i++) { int c=Axis(Positions[i].x)+Side*Axis(Positions[i].z); links[i]=heads[c]; heads[c]=i; }
            for(int i=0;i<Count;i++)
            {
                var p=Positions[i]; var direction=target-p; direction.y=0;
                direction = direction.magnitude > 1.1f ? direction.normalized : Vector3.zero;
                var separation=Vector3.zero;
                int cx=Axis(p.x), cz=Axis(p.z);
                for(int z=Mathf.Max(0,cz-1);z<=Mathf.Min(Side-1,cz+1);z++)
                for(int x=Mathf.Max(0,cx-1);x<=Mathf.Min(Side-1,cx+1);x++)
                for(int j=heads[x+Side*z];j>=0;j=links[j])
                {
                    if(i==j) continue;
                    var away=p-Positions[j]; away.y=0; float d=away.sqrMagnitude;
                    if(d<1.44f && d>.00001f) separation += away*((1.2f-Mathf.Sqrt(d))/Mathf.Sqrt(d));
                }
                direction=Vector3.ClampMagnitude(direction+separation*2,1);
                var proposed=TechnicalWorld.Move(p,direction*(speed*dt),.45f,.22f);
                if ((proposed-p).sqrMagnitude < speed*speed*dt*dt*.1f && direction.sqrMagnitude>.1f)
                {
                    var tangent=new Vector3(-direction.z,0,direction.x);
                    proposed=TechnicalWorld.Move(p,tangent*(speed*dt),.45f,.22f);
                }
                proposed.y=TechnicalWorld.Height(proposed.x,proposed.z); next[i]=proposed;
            }
            Array.Copy(next,Positions,Count);
        }
    }
}
