using System;

namespace Mamporro.Core
{
    // Cada Advance debe devolver TODO el trabajo realizado, sin superar su cuota.
    // El registro es de carga; Tick no crea listas ni depende del reloj de CPU.
    public interface IRegionalNavWork
    {
        int ClassIndex {get;}
        bool Pending {get;}
        long RequestedTick {get;}
        int Priority {get;}
        int Advance(int budget);
    }

    public sealed class RegionalNavScheduler
    {
        public const int Budget=3000,MinimumClassQuota=1000,AgingTicks=30;
        readonly IRegionalNavWork[] jobs;
        int count,firstClass;
        public long TickIndex {get;private set;}
        public int Used {get;private set;}
        public readonly int[] ClassUsed=new int[3];
        public RegionalNavScheduler(int capacity)
        {if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));jobs=new IRegionalNavWork[capacity];}
        public void Register(IRegionalNavWork job)
        {
            if(job==null||job.ClassIndex<0||job.ClassIndex>=3)throw new ArgumentException("Clase no válida");
            for(int i=0;i<count;i++)if(ReferenceEquals(jobs[i],job))throw new ArgumentException("Trabajo duplicado");
            if(count==jobs.Length)throw new InvalidOperationException("Registro lleno");
            jobs[count++]=job;
        }
        IRegionalNavWork Next(int cls)
        {
            IRegionalNavWork best=null;
            for(int i=0;i<count;i++){
                var job=jobs[i];if(job.ClassIndex!=cls||!job.Pending)continue;
                if(best==null){best=job;continue;}
                bool aged=TickIndex-job.RequestedTick>=AgingTicks,old=TickIndex-best.RequestedTick>=AgingTicks;
                if(aged!=old?aged:aged?job.RequestedTick<best.RequestedTick:
                    job.Priority!=best.Priority?job.Priority>best.Priority:job.RequestedTick<best.RequestedTick)best=job;
            }
            return best;
        }
        int Serve(int cls,int allowance)
        {
            int used=0;
            while(used<allowance){var job=Next(cls);if(job==null)break;
                int n=job.Advance(allowance-used);
                if(n<=0||n>allowance-used)throw new InvalidOperationException("Contabilidad de trabajo no válida");
                used+=n;
            }
            ClassUsed[cls]+=used;return used;
        }
        public int Tick()
        {
            TickIndex++;Used=0;for(int c=0;c<3;c++)ClassUsed[c]=0;
            // La población no influye. Cada clase pendiente tiene 1000 antes del sobrante.
            for(int i=0;i<3;i++)Used+=Serve((firstClass+i)%3,MinimumClassQuota);
            for(int i=0;i<3&&Used<Budget;i++)Used+=Serve((firstClass+i)%3,Budget-Used);
            firstClass=(firstClass+1)%3;return Used;
        }
    }
}
