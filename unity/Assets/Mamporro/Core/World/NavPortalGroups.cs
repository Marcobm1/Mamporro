using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Lotes de trabajo, no portales representativos ni ventanas geométricas.
    // Members referencia el corpus D2 intacto; no se deduplica ningún cruce.
    public sealed class NavPortalGroups
    {
        public readonly NavPortalWorld World;
        public readonly int[] Start,Members;
        public int Count=>Start.Length-1;
        public long PayloadBytes=>(Start.LongLength+Members.LongLength)*4;
        public NavPortalGroups(NavPortalWorld world)
        {
            World=world??throw new ArgumentNullException(nameof(world));
            var groups=new SortedDictionary<string,List<int>>(StringComparer.Ordinal);
            for(int i=0;i<world.Portals.Length;i++){
                string key=Signature(world,world.Portals[i]);
                if(!groups.TryGetValue(key,out var members)){members=new List<int>();groups.Add(key,members);}
                members.Add(i);
            }
            Start=new int[groups.Count+1];Members=new int[world.Portals.Length];int g=0,at=0;
            foreach(var pair in groups){Start[g++]=at;foreach(int member in pair.Value)Members[at++]=member;}
            Start[g]=at;
        }
        public static string Signature(NavPortalWorld w,NavPortal p)
        {
            var a=w.Graph(p.From);var b=w.Graph(p.To);var from=w.Position(p.From);var to=w.Position(p.To);
            // Capa ordinal y soporte, además de SCC, separan superficies superpuestas.
            // La altura exacta y el sentido se conservan también en cada miembro.
            return string.Join(":",new[]{p.ClassIndex,p.FromRegion,p.ToRegion,w.Component[p.From],w.Component[p.To],
                (int)p.Kind,Math.Sign(to.X-from.X),Math.Sign(to.Y-from.Y),Math.Sign(to.Z-from.Z),
                p.LocalFrom-a.CellStart[a.NodeCell[p.LocalFrom]],p.LocalTo-b.CellStart[b.NodeCell[p.LocalTo]],
                a.NodeTerrain[p.LocalFrom]?1:0,b.NodeTerrain[p.LocalTo]?1:0});
        }
    }
}
