using UnityEngine;

namespace Mamporro.U1
{
    // Geometría analítica compartida por mallas, controlador y horda.
    // Las rampas conectan con una meseta; el lado derecho tiene pendiente excesiva.
    public static class TechnicalWorld
    {
        public const float HalfSize = 48;
        public static readonly Vector4[] Blocks = {
            new Vector4(-8, 5, 2, 2), new Vector4(8, 8, 2, 3),
            new Vector4(-15, -10, 3, 2), new Vector4(16, -8, 2, 4)
        };

        public static float Height(float x, float z)
        {
            int ix=Mathf.FloorToInt(x), iz=Mathf.FloorToInt(z);
            float fx=x-ix, fz=z-iz;
            float a=Sample(ix,iz), b=Sample(ix,iz+1), c=Sample(ix+1,iz), d=Sample(ix+1,iz+1);
            return fx+fz<=1 ? a+(c-a)*fx+(b-a)*fz : d+(b-d)*(1-fx)+(c-d)*(1-fz);
        }

        static float Sample(float x, float z)
        {
            if (x >= -20 && x <= -4 && z >= 12 && z <= 36)
                return Mathf.Min(Mathf.Min((z - 12) * .25f, 4), (36 - z) * .5f);
            if (x >= 4 && x <= 18 && z >= 18 && z <= 30)
                return Mathf.Min(Mathf.Min((z - 18) * 1.5f, 4), (30 - z) * 1.5f);
            return 0;
        }

        public static Vector3 Normal(float x, float z)
        {
            const float e = .02f;
            return new Vector3(Height(x-e,z)-Height(x+e,z),2*e,Height(x,z-e)-Height(x,z+e)).normalized;
        }

        public static bool Blocked(Vector3 p, float radius)
        {
            foreach (var b in Blocks)
                if (Mathf.Abs(p.x-b.x) < b.z+radius && Mathf.Abs(p.z-b.y) < b.w+radius && p.y < 3)
                    return true;
            return false;
        }

        public static Vector3 Move(Vector3 from, Vector3 delta, float radius, float step)
        {
            var next = from + new Vector3(delta.x,0,0);
            if (Blocked(next,radius) || Height(next.x,next.z) > from.y+step) next.x = from.x;
            var zNext = next + new Vector3(0,0,delta.z);
            if (!Blocked(zNext,radius) && Height(zNext.x,zNext.z) <= from.y+step) next = zNext;
            next.x = Mathf.Clamp(next.x,-HalfSize+radius,HalfSize-radius);
            next.z = Mathf.Clamp(next.z,-HalfSize+radius,HalfSize-radius);
            return next;
        }
    }
}
