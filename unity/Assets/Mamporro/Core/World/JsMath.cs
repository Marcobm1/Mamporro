using System;

namespace Mamporro.Core
{
    // Operaciones de Math de JavaScript (V8) que .NET no calcula bit a bit igual.
    public static class JsMath
    {
        // Math.hypot de V8 con dos argumentos: escala por el máximo y suma de Kahan.
        public static double Hypot(double a,double b)
        {
            double x=Math.Abs(a),y=Math.Abs(b),max=x>y?x:y;
            if(double.IsInfinity(max))return double.PositiveInfinity;
            if(double.IsNaN(a)||double.IsNaN(b))return double.NaN;
            if(max==0)return 0;
            double sum=0,compensation=0,n=x/max,summand=n*n-compensation,preliminary=sum+summand;
            compensation=(preliminary-sum)-summand;sum=preliminary;
            n=y/max;summand=n*n-compensation;preliminary=sum+summand;sum=preliminary;
            return Math.Sqrt(sum)*max;
        }
    }
}
