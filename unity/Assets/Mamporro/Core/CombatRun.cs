using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    public struct CombatEffect { public string Kind; public double X,Y,Z,X2,Y2,Z2,Radius,Angle,Life; }
    public interface ICombatEffects { void Emit(CombatEffect effect); }
    public sealed partial class CombatRun : IEnemyActions,IProjectileHits,ITargetFilter
    {
        public readonly ICombatWorld World;
        public readonly CharacterDef Character;
        public readonly CombatPlayer Player=new CombatPlayer();
        public readonly PlayerStats Stats=new PlayerStats();
        readonly PlayerStats basis=new PlayerStats();
        public readonly Enemies Enemies;
        public readonly Projectiles Projectiles=new Projectiles(256),EnemyShots=new Projectiles(256);
        public readonly Pickups Gems=new Pickups(600),Coins=new Pickups(300);
        public readonly List<Weapon> Weapons=new List<Weapon>(4);
        public readonly List<Tome> Tomes=new List<Tome>(4);
        public readonly List<ItemStack> Items=new List<ItemStack>(12);
        public readonly HashSet<string> Banished=new HashSet<string>();
        public List<Card> Offer;
        public int Level=1,PendingLevels,Kills,Rerolls=2,Skips=2,Banishes=2;
        public double Xp,Hp,Time,Invulnerable,ShieldCharge,Gold,GoldCollected,CrowdSlow;
        public bool Invincible,WeaponsOff,Paused;
        public bool Dead=>Hp<=0;
        public bool Choosing=>Offer!=null;
        public Boss Boss {get;private set;}
        // Multiplicadores de aparición según el minuto de dificultad (director web sin su calendario, que es U3).
        // U2 usa la duración de referencia: ritmo 1.
        const double Pace=1;
        public double SpawnHp=1,SpawnXp=1,SpawnGold=1;
        public double Minutes=>Math.Max(0,Time)/60*Pace;
        readonly Rng combatRng,offerRng;
        readonly ICombatEffects fx;
        readonly int[] nearby=new int[256];
        readonly List<CombatEffect> blasts=new List<CombatEffect>(4096),batch=new List<CombatEffect>(4096);
        int pearls,ollas,purses;double purseStep;
        uint excludedId;
        public CombatRun(ICombatWorld world,string seed,CharacterDef character,int enemyCapacity=4096,ICombatEffects effects=null,double worldSize=96)
        {
            World=world;Character=character;fx=effects;Enemies=new Enemies(enemyCapacity,worldSize);
            var rng=new Rng(seed+"/run");combatRng=rng.Derive("combat");offerRng=rng.Derive("offers");
            Stats.Reset(character);basis.Reset(character);Hp=Stats[Stat.maxHp];AddWeapon(character.startingWeapon);
        }
        public void Emit(string kind,double x,double y,double z,double radius=0,double angle=0,double x2=0,double y2=0,double z2=0,double life=.25)
        {fx?.Emit(new CombatEffect{Kind=kind,X=x,Y=y,Z=z,Radius=radius,Angle=angle,X2=x2,Y2=y2,Z2=z2,Life=life});}
        public void Step(double dt)
        {
            if(Paused||Choosing||Dead)return;
            Time+=dt;RefreshSpawnParams();
            if(Character.passive=="shield")ShieldCharge=Math.Min(Character.recharge,ShieldCharge+dt);
            else for(int i=0;i<Enemies.Count;i++){double dx=Enemies.X[i]-Player.X,dz=Enemies.Z[i]-Player.Z;if(dx*dx+dz*dz<=Character.radius*Character.radius&&Math.Abs(Enemies.Y[i]-Player.Y)<2)Enemies.ApplySlow(i,Character.amount,dt*2);}
            if(Boss!=null&&!Boss.Step(dt,this))Boss=null;
            double contact=Enemies.Step(dt,Player,World,this);
            Player.X+=Enemies.PushX;Player.Z+=Enemies.PushZ;
            if(contact>0)Hurt(contact);
            double slow=Math.Min(.4,Math.Max(0,Enemies.Pressure)*.2);CrowdSlow+=(slow-CrowdSlow)*Math.Min(1,dt*15);
            if(!WeaponsOff)for(int i=0;i<Weapons.Count;i++)StepWeapon(Weapons[i],states[i],dt);
            Projectiles.Step(dt,Enemies,World,this);
            double shot=EnemyShots.StepHostile(dt,World,Player);if(shot>0)Hurt(shot);
            FlushDead();
            double xp=Gems.Step(dt,Player,Stats[Stat.pickupRadius]);if(xp>0)GainXp(xp);
            double gold=Coins.Step(dt,Player,Stats[Stat.pickupRadius]);if(gold>0)GainGold(gold*Stats[Stat.goldGain]);
            Invulnerable=Math.Max(0,Invulnerable-dt);
            if(Stats[Stat.regen]>0&&Hp>0)Hp=Math.Min(Stats[Stat.maxHp],Hp+Stats[Stat.regen]*dt);
            // La web abre elección en Game tras Run.update. Aquí se impide el siguiente tick.
            OpenChoice();
        }
        public void RefreshSpawnParams()
        {double m=Minutes;SpawnHp=1+Tuning.SpawnHpGrowth*m+Tuning.SpawnHpCurve*m*m;SpawnXp=(1+Tuning.SpawnXpGrowth*m)*Pace;SpawnGold=Pace;}
        public int Spawn(int type,double x,double z,double hp=1)
        {int i=Enemies.Spawn(type,x,World.Height(x,z),z,hp,1);Enemies.Rebuild();return i;}
        // Como debugSpawnEnemy de la web: con los multiplicadores del minuto actual.
        public int SpawnScaled(int type,double x,double z)
        {RefreshSpawnParams();int i=Enemies.Spawn(type,x,World.Height(x,z),z,SpawnHp,SpawnXp,SpawnGold);Enemies.Rebuild();return i;}
        // Pelusa hija del estornudo del jefe; la rejilla se reconstruye al final del paso, como en la web.
        public void SpawnMinion(double x,double z)
        {if(World.IsInside(x,z,2))Enemies.Spawn(0,x,World.Height(x,z),z,SpawnHp,SpawnXp,SpawnGold);}
        public bool SpawnBoss(double x,double z)
        {
            if(Boss!=null)return false;if(Enemies.Count>=Enemies.Capacity)Enemies.Remove(Enemies.Count-1);
            RefreshSpawnParams();double m=Minutes,hp=1+Tuning.BossHpGrowth*m+Tuning.BossHpCurve*m*m;World.Clamp(ref x,ref z);
            int i=Enemies.Spawn(5,x,World.Height(x,z),z,hp,SpawnXp,SpawnGold);if(i<0)return false;Enemies.Xp[i]=0;Enemies.Rebuild();
            Boss=new Boss(Enemies.Id[i],combatRng.Derive("boss-"+Time.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));return true;
        }
        public void Shoot(int i,double dx,double dz)
        {
            var d=Enemies.Def(i);var r=d.ranged;double x=Enemies.X[i]+dx*d.radius,z=Enemies.Z[i]+dz*d.radius;
            EnemyShots.Spawn(x,World.Height(x,z)+1,z,dx,dz,r.projectileSpeed,r.range/r.projectileSpeed+.6,r.projectileRadius,damage:r.projectileDamage);
        }
        public void Hit(int weapon,int enemy,double dx,double dz)
        {if(weapon<Weapons.Count)DamageEnemy(enemy,Weapons[weapon],dx,dz);}
        public void DamageEnemy(int i,Weapon w,double dx,double dz)
        {
            double amount=Rules.Damage(w[WStat.damage],w[WStat.critChance],w[WStat.critMultiplier],combatRng.Next(),out int crit),before=Enemies.Hp[i];
            w.TotalDamage+=HitEnemy(i,amount,w.Def.hitFlash,dx*w[WStat.knockback],dz*w[WStat.knockback]);
            if(before>0&&Enemies.Hp[i]<=0)w.Kills++;
            if(crit>0&&pearls>0&&combatRng.Next()<Rules.PearlChance(pearls)) {
                excludedId=Enemies.Id[i];int target=Enemies.Grid.Nearest(Enemies.X[i],Enemies.Z[i],7,this);
                if(target>=0){Emit("pearl",Enemies.X[i],Enemies.Y[i]+.8,Enemies.Z[i],x2:Enemies.X[target],y2:Enemies.Y[target]+.8,z2:Enemies.Z[target]);w.TotalDamage+=HitEnemy(target,amount*.5,.5,0,0);}
            }
        }
        public bool Accept(int i)=>Enemies.Accept(i)&&Enemies.Id[i]!=excludedId;
        double HitEnemy(int i,double amount,double flash,double px,double pz)
        {
            double before=Enemies.Hp[i];Enemies.Hp[i]=(float)(before-amount);Enemies.Flash[i]=(float)Math.Max(Enemies.Flash[i],flash);
            double mass=Enemies.Def(i).mass;Enemies.Kx[i]=(float)(Enemies.Kx[i]+px/mass);Enemies.Kz[i]=(float)(Enemies.Kz[i]+pz/mass);
            return before>0?Math.Min(before,amount):0;
        }
        void RemoveDead()
        {
            for(int i=Enemies.Count-1;i>=0;i--)if(Enemies.Hp[i]<=0){
                var d=Enemies.Def(i);double x=Enemies.X[i],y=Enemies.Y[i],z=Enemies.Z[i];Kills++;
                if(Enemies.Xp[i]>0)Gems.Spawn(x,y+.4,z,Enemies.Xp[i]);
                if(d.goldChance>0&&combatRng.Next()<d.goldChance){double value=Math.Floor(combatRng.Int(d.goldMin,d.goldMax)*Enemies.Gold[i]+combatRng.Next());if(value>0)Coins.Spawn(x,y+.5,z,value);}
                if(ollas>0&&combatRng.Next()<Rules.OllaChance(ollas))blasts.Add(new CombatEffect{X=x,Y=y,Z=z,Radius=3.2*Stats[Stat.area],X2=16*Stats[Stat.damage]+.5*Enemies.MaxHp[i]});
                // La victoria pertenece a U3. En U2 el jefe solo termina el escenario de combate.
                if(d.behavior=="boss")Boss=null;
                foreach(var s in states){s.NextBite[i]=s.NextBite[Enemies.Count-1];s.NextBite[Enemies.Count-1]=0;}
                Enemies.Remove(i);
            }
            Enemies.Rebuild();
        }
        void FlushDead()
        {
            RemoveDead();
            for(int round=0;round<6&&blasts.Count>0;round++) {
                batch.Clear();batch.AddRange(blasts);blasts.Clear();
                foreach(var b in batch){Emit("blast",b.X,b.Y,b.Z,b.Radius);int n=Enemies.Query(b.X,b.Z,b.Radius,nearby);
                    for(int k=0;k<n;k++){int e=nearby[k];if(!Enemies.Accept(e))continue;double dx=Enemies.X[e]-b.X,dz=Enemies.Z[e]-b.Z,d=Rules.Hypot(dx,dz);if(d>b.Radius+Enemies.Radius(e))continue;HitEnemy(e,b.X2,.6,d>1e-4?dx/d*6:0,d>1e-4?dz/d*6:0);}}
                RemoveDead();
            }
            blasts.Clear();
        }
        public void Hurt(double amount)
        {
            if(Invincible||Invulnerable>0||Hp<=0||amount<=0)return;
            if(Character.passive=="shield"&&ShieldCharge>=Character.recharge){ShieldCharge=0;Invulnerable=.7;Emit("shield",Player.X,Player.Y,Player.Z,1);return;}
            ShieldCharge=0;Hp=Math.Max(0,Hp-Rules.Mitigate(amount,Stats[Stat.armor]));Invulnerable=.7;
            if(Hp<=0) {
                var stack=Items.Find(s=>s.Def.id=="bata");if(stack==null)return;
                if(--stack.Count<=0)Items.Remove(stack);RefreshStats();Hp=Stats[Stat.maxHp]*.5;Invulnerable=2.5;
                int n=Enemies.Query(Player.X,Player.Z,8,nearby);
                for(int k=0;k<n;k++){int i=nearby[k];if(i>=Enemies.Count)continue;double dx=Enemies.X[i]-Player.X,dz=Enemies.Z[i]-Player.Z,d=Rules.Nonzero(Rules.Hypot(dx,dz)),mass=Enemies.Def(i).mass;Enemies.Kx[i]=(float)(Enemies.Kx[i]+dx/d*14/mass);Enemies.Kz[i]=(float)(Enemies.Kz[i]+dz/d*14/mass);}
                EnemyShots.Count=0;Emit("revive",Player.X,Player.Y,Player.Z,8);
            }
        }
        public void GainXp(double amount) {PendingLevels+=Rules.AddExperience(ref Level,ref Xp,amount*Stats[Stat.xpGain]);}
        public void GainGold(double amount){Gold+=amount;GoldCollected+=amount;if(purses>0&&Math.Floor(Gold/100)!=purseStep)RefreshStats();}
        public Weapon AddWeapon(string id)
        {
            if(Weapons.Count>=4||Weapons.Exists(w=>w.Def.id==id))return null;
            var d=Array.Find(Catalog.Weapons,w=>w.id==id);if(d==null)return null;
            var weapon=new Weapon(d,Weapons.Count);weapon.Refresh(Stats);Weapons.Add(weapon);states.Add(new WeaponState(Enemies.Capacity));return weapon;
        }
        public void QaWeapons(params string[] ids)
        {Weapons.Clear();states.Clear();Projectiles.Count=0;foreach(string id in ids)AddWeapon(id);}
        public bool AddTome(string id,double[] amounts=null)
        {
            var t=Tomes.Find(v=>v.Def.id==id);
            if(t==null){if(Tomes.Count>=4)return false;var def=Array.Find(Catalog.Tomes,v=>v.id==id);if(def==null)return false;t=new Tome{Def=def,Bonus=new double[def.effects.Length]};Tomes.Add(t);}
            t.Level++;for(int i=0;i<t.Bonus.Length;i++)t.Bonus[i]+=amounts==null?t.Def.effects[i].amount:amounts[i];RefreshStats();return true;
        }
        public void AddItem(string id)
        {
            var d=Array.Find(Catalog.Items,v=>v.id==id);if(d==null)return;
            var s=Items.Find(v=>v.Def.id==id);if(s==null)Items.Add(new ItemStack{Def=d,Count=1});else s.Count++;RefreshStats();
        }
        public int ItemCount(string id){foreach(var s in Items)if(s.Def.id==id)return s.Count;return 0;}
        public void RefreshStats()
        {
            double old=Stats[Stat.maxHp];Stats.Reset(Character);
            foreach(var t in Tomes)for(int i=0;i<t.Bonus.Length;i++)Stats.Add(t.Def.effects[i],t.Bonus[i],basis);
            foreach(var s in Items)foreach(var e in s.Def.effects)Stats.Add(e,e.amount*s.Count,basis);
            pearls=ItemCount("perlas");ollas=ItemCount("olla");purses=ItemCount("monedero");purseStep=Math.Floor(Gold/100);
            Stats[Stat.damage]+=Rules.PurseBonus(Gold,purses);Stats.Cap();if(Stats[Stat.maxHp]>old)Hp+=Stats[Stat.maxHp]-old;Hp=Math.Min(Hp,Stats[Stat.maxHp]);foreach(var w in Weapons)w.Refresh(Stats);
        }
        public bool OpenChoice()
        {if(Choosing||PendingLevels<=0||Dead)return false;Offer=Offers.Generate(Weapons,Tomes,Stats,Banished,offerRng,(int)Stats[Stat.choices]);return true;}
        public bool Choose(int index)
        {
            if(Offer==null||index<0||index>=Offer.Count)return false;var c=Offer[index];
            switch(c.Kind){
                case "newWeapon":AddWeapon(c.Id);break;
                case "weaponUpgrade":var w=Weapons.Find(v=>v.Def.id==c.Id);if(w!=null){for(int k=0;k<c.Changes.Length;k++)w.Bonus[(int)c.Changes[k]]+=c.Amounts[k];w.Level++;w.Refresh(Stats);}break;
                case "tome":AddTome(c.Id,c.Amounts);break;
                case "heal":Hp=Math.Min(Stats[Stat.maxHp],Hp+Stats[Stat.maxHp]*c.Amount);break;
                case "gold":GainGold(c.Amount);break;
            }
            CloseChoice();return true;
        }
        void CloseChoice(){Offer=null;PendingLevels=Math.Max(0,PendingLevels-1);OpenChoice();}
        public bool Reroll(){if(!Choosing||Rerolls<=0)return false;Rerolls--;Offer=Offers.Generate(Weapons,Tomes,Stats,Banished,offerRng,(int)Stats[Stat.choices]);return true;}
        public bool Skip(){if(!Choosing||Skips<=0)return false;Skips--;CloseChoice();return true;}
        public bool Banish(int index)
        {
            if(!Choosing||Banishes<=0||index<0||index>=Offer.Count||Offer[index].Key==null)return false;
            Banishes--;Banished.Add(Offer[index].Key);var rest=new List<Card>(Offer);rest.RemoveAt(index);var replacement=Offers.Replace(Weapons,Tomes,Stats,Banished,offerRng,rest);
            if(replacement!=null)Offer[index]=replacement;else Offer.RemoveAt(index);if(Offer.Count==0)Offer.Add(Offers.Heal());return true;
        }
    }
}
