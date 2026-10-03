using System;

namespace Mamporro.Core
{
    // Estado de un interactuable durante la partida.
    public sealed class InteractableState
    {
        public readonly InteractableSpot Spot;
        // Visto de cerca: aparece en el minimapa.
        public bool Discovered;
        // Baúl abierto, mesa camilla completada, tótem activado o armario abierto.
        public bool Used;
        // Carga de la mesa camilla (0..1).
        public double Charge;
        public InteractableState(InteractableSpot spot){Spot=spot;}
    }
    // Lo que el jugador tiene delante y puede usar con E.
    public struct InteractPrompt { public int Index;public string Kind;public double Cost; }
    public interface IInteractableEvents { void Discovered(InteractableState item); void ShrineCharged(InteractableState item); }

    // Port de src/systems/Interactables.ts: descubrimiento, carga y descarga de las mesas
    // camilla, desafío del tótem y aviso de interacción. La partida decide qué pasa al usarlos.
    public sealed class Interactables
    {
        public readonly InteractableState[] List;
        public int ChestsOpened;
        // Segundos que quedan del desafío del tótem (0 = ninguno).
        public double Challenge;
        // Índice de la mesa camilla que se está cargando (-1 = ninguna).
        public int Charging=-1;
        public Interactables(System.Collections.Generic.IList<InteractableSpot> spots)
        {List=new InteractableState[spots.Count];for(int i=0;i<List.Length;i++)List[i]=new InteractableState(spots[i]);}
        public double NextChestCost=>Rules.ChestCost(ChestsOpened);
        static double Reach(string kind)=>kind=="chest"?Tuning.ChestReach:kind=="totem"?Tuning.TotemReach:kind=="portal"?Tuning.PortalReach:0;

        // Un tick; devuelve true si el desafío del tótem ha terminado en este tick.
        public bool Update(double dt,double x,double z,IInteractableEvents events)
        {
            Charging=-1;
            for(int index=0;index<List.Length;index++){
                var item=List[index];var s=item.Spot;
                double d=JsMath.Hypot(s.X-x,s.Z-z);
                if(!item.Discovered&&d<=(s.Kind=="portal"?Tuning.DiscoveryPortalRadius:Tuning.DiscoveryRadius)){item.Discovered=true;events.Discovered(item);}
                if(s.Kind!="shrine"||item.Used)continue;
                if(d<=Tuning.ShrineRadius){
                    Charging=index;item.Charge=Math.Min(1,item.Charge+dt/Tuning.ShrineChargeTime);
                    if(item.Charge>=1){item.Used=true;events.ShrineCharged(item);}
                } else item.Charge=Math.Max(0,item.Charge-dt/Tuning.ShrineChargeTime*Tuning.ShrineDecayRate);
            }
            if(Challenge>0){Challenge=Math.Max(0,Challenge-dt);return Challenge==0;}
            return false;
        }

        // El interactuable usable más cercano al alcance del jugador.
        public bool Prompt(double x,double z,out InteractPrompt prompt)
        {
            prompt=default;double best=double.PositiveInfinity;bool found=false;
            for(int index=0;index<List.Length;index++){
                var item=List[index];var s=item.Spot;
                if(item.Used||s.Kind=="shrine")continue;
                if(s.Kind=="totem"&&Challenge>0)continue;
                double d=JsMath.Hypot(s.X-x,s.Z-z);
                if(d>Reach(s.Kind)||d>=best)continue;
                best=d;found=true;prompt=new InteractPrompt{Index=index,Kind=s.Kind,Cost=s.Kind=="chest"?NextChestCost:0};
            }
            return found;
        }

        // Marca como descubiertos los de un tipo (o todos, con null); devuelve cuántos eran nuevos.
        public int Reveal(string kind=null)
        {
            int n=0;
            foreach(var item in List){if(item.Discovered||(kind!=null&&item.Spot.Kind!=kind))continue;item.Discovered=true;n++;}
            return n;
        }
        public InteractableState Find(string kind)=>Array.Find(List,i=>i.Spot.Kind==kind);
        public int IndexOf(InteractableState item)=>Array.IndexOf(List,item);
    }
}
