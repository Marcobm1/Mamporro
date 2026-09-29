using System;
using Mamporro.Core;
using UnityEngine;

namespace Mamporro.U3
{
    // Texturas en grises generadas por código (src/render/textures.ts), filtro puntual.
    public static class RetroTextures
    {
        static Texture2D ToTexture(float[] values,int size,string name)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,false){name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color32[size*size];
            // El canvas de la web tiene y hacia abajo; Unity, hacia arriba.
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){byte c=(byte)Rules.Round(values[y*size+x]*255);pixels[(size-1-y)*size+x]=new Color32(c,c,c,255);}
            texture.SetPixels32(pixels);texture.Apply(true,false);return texture;
        }

        // Detalle: ruido suave con píxeles oscuros o claros y trazos verticales cortos.
        public static Texture2D Detail(int size=32,string seed="detail")
        {
            var rng=new Rng(seed);var values=new float[size*size];
            for(int i=0;i<values.Length;i++){double v=0.86+rng.Next()*0.1,r=rng.Next();if(r<0.07)v=0.7;else if(r>0.95)v=1;values[i]=(float)v;}
            for(int n=0;n<size*1.5;n++){
                int x=rng.Int(0,size-1),y=rng.Int(0,size-1),len=rng.Int(2,3);float shade=rng.Chance(0.5)?0.74f:0.98f;
                for(int k=0;k<len;k++)values[((y+k)%size)*size+x]=shade;
            }
            return ToTexture(values,size,"Detalle U3");
        }

        // Sillería: bloques desfasados con juntas oscuras.
        public static Texture2D Stone(int size=32,string seed="stone")
        {
            var rng=new Rng(seed);var values=new float[size*size];int brickW=size/2,brickH=size/4;
            var shades=new double[16];for(int i=0;i<16;i++)shades[i]=0.8+rng.Next()*0.2;
            for(int y=0;y<size;y++){
                int row=y/brickH,offset=row%2==0?0:brickW/2;
                for(int x=0;x<size;x++){
                    int bx=((x+offset)/brickW)%2;bool joint=y%brickH==0||(x+offset)%brickW==0;
                    double v=joint?0.58:shades[row*2+bx]-rng.Next()*0.06;
                    if(!joint&&rng.Chance(0.05))v-=0.12;
                    values[y*size+x]=(float)v;
                }
            }
            return ToTexture(values,size,"Sillería U3");
        }
    }
}
