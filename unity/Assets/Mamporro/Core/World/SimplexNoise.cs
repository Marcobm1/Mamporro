/*
 * Port a C# de createNoise2D de simplex-noise 4.0.3 (la versión que usa la web
 * aprobada), alimentado por el RNG propio para reproducir el mismo terreno.

 A fast javascript implementation of simplex noise by Jonas Wagner

Based on a speed-improved simplex noise algorithm for 2D, 3D and 4D in Java.
Which is based on example code by Stefan Gustavson (stegu@itn.liu.se).
With Optimisations by Peter Eastman (peastman@drizzle.stanford.edu).
Better rank ordering method by Stefan Gustavson in 2012.

 Copyright (c) 2024 Jonas Wagner

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
 */
using System;

namespace Mamporro.Core
{
    public sealed class SimplexNoise2D
    {
        static readonly double Sqrt3=Math.Sqrt(3.0);
        static readonly double F2=0.5*(Sqrt3-1.0);
        static readonly double G2=(3.0-Sqrt3)/6.0;
        static readonly double[] Grad2={1,1,-1,1,1,-1,-1,-1,1,0,-1,0,1,0,-1,0,0,1,0,-1,0,1,0,-1};
        readonly byte[] perm=new byte[512];
        readonly double[] gradX=new double[512],gradY=new double[512];

        // Equivale a createNoise2D(random): el RNG solo se usa para barajar la tabla.
        public SimplexNoise2D(Func<double> random)
        {
            for(int i=0;i<256;i++)perm[i]=(byte)i;
            for(int i=0;i<255;i++){int r=i+(int)(random()*(256-i));byte aux=perm[i];perm[i]=perm[r];perm[r]=aux;}
            for(int i=256;i<512;i++)perm[i]=perm[i-256];
            for(int i=0;i<512;i++){gradX[i]=Grad2[(perm[i]%12)*2];gradY[i]=Grad2[(perm[i]%12)*2+1];}
        }

        static int FastFloor(double x)=>(int)Math.Floor(x);

        public double Noise(double x,double y)
        {
            double n0=0,n1=0,n2=0;
            double s=(x+y)*F2;
            int i=FastFloor(x+s),j=FastFloor(y+s);
            double t=(i+j)*G2,x0=x-(i-t),y0=y-(j-t);
            int i1,j1;
            if(x0>y0){i1=1;j1=0;}else{i1=0;j1=1;}
            double x1=x0-i1+G2,y1=y0-j1+G2,x2=x0-1.0+2.0*G2,y2=y0-1.0+2.0*G2;
            int ii=i&255,jj=j&255;
            double t0=0.5-x0*x0-y0*y0;
            if(t0>=0){int g=ii+perm[jj];t0*=t0;n0=t0*t0*(gradX[g]*x0+gradY[g]*y0);}
            double t1=0.5-x1*x1-y1*y1;
            if(t1>=0){int g=ii+i1+perm[jj+j1];t1*=t1;n1=t1*t1*(gradX[g]*x1+gradY[g]*y1);}
            double t2=0.5-x2*x2-y2*y2;
            if(t2>=0){int g=ii+1+perm[jj+1];t2*=t2;n2=t2*t2*(gradX[g]*x2+gradY[g]*y2);}
            return 70.0*(n0+n1+n2);
        }
    }
}
