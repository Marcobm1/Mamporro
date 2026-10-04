using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.Persistence;

namespace Mamporro.Tests
{
    // Paso 6 de U4: progreso permanente ↔ partida. Archivos reales solo en un directorio temporal
    // propio de cada prueba; la partida es la real (WorldRun sobre el mundo MAMPORRO).
    public sealed class ProgressSessionTests
    {
        string root,tempRoot,dir;ProgressFiles files;
        static WorldData world;

        sealed class FailingFiles:IProgressFiles
        {
            readonly IProgressFiles inner;public string FailOn;
            public FailingFiles(IProgressFiles inner){this.inner=inner;}
            public IDisposable Lock()=>inner.Lock();
            public byte[] Read(string name)=>inner.Read(name);
            public bool Exists(string name)=>inner.Exists(name);
            public void WriteNew(string name,byte[] data){if(FailOn=="write")throw new IOException("fallo inyectado");inner.WriteNew(name,data);}
            public void Publish(string source,string destination,bool replace){if(FailOn=="publish")throw new IOException("fallo inyectado");inner.Publish(source,destination,replace);}
            public void DeleteTemporary(string name)=>inner.DeleteTemporary(name);
        }

        [OneTimeSetUp]public void World(){world=WorldData.Generate("MAMPORRO");}
        [SetUp]public void Setup()
        {
            tempRoot=Path.GetFullPath(Path.GetTempPath());
            root=Path.GetFullPath(Path.Combine(tempRoot,"Mamporro-U4-session-"+Guid.NewGuid().ToString("N")));
            dir=Path.Combine(root,"Progress");Directory.CreateDirectory(root);files=new ProgressFiles(dir);
        }
        [TearDown]public void Cleanup()
        {
            string full=Path.GetFullPath(root);
            Assert.That(Path.GetDirectoryName(full).TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(tempRoot.TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(full),Does.StartWith("Mamporro-U4-session-"));
            if(Directory.Exists(full))Directory.Delete(full,true);
        }

        string Primary=>Path.Combine(dir,ProgressStore.PrimaryName);
        void Save(ProgressDto p){var store=new ProgressStore(files,"es");Assert.That(store.Confirm(store.Prepare(p,store.Load()),true).Success,Is.True);}
        ProgressSession Open(IProgressFiles source=null)=>new ProgressSession(source??files,"es");
        static string Json(ProgressDto p)=>ProgressTree.Stored(p);
        static MetaRun Run(string id,double kills=0,double time=60,bool victory=false,double chests=0,double shrines=0,double challenges=0,double level=1,bool cheated=false,bool lifeTome=false)
            =>new MetaRun {id=id,kills=kills,time=time,victory=victory,chests=chests,shrines=shrines,challenges=challenges,level=level,cheated=cheated,usedLifeTome=lifeTome};
        static WorldRun NewRun(RunSetup setup)
        {
            var s=new WorldRun(world,"U4-META",Array.Find(Catalog.Characters,c=>c.id==setup.Character)){Automatic=false};
            ProgressSession.Apply(setup,s);return s;
        }

        // ------------------------------------------------------------ carga
        [Test]public void WithoutSaveStartsWithInitialProgressAndWritesNothing()
        {
            var session=Open();
            Assert.That(session.Status,Is.EqualTo("new"));Assert.That(session.CanSave,Is.True);
            Assert.That(Json(session.Progress),Is.EqualTo(Json(ProgressDto.New("es"))));
            Assert.That(File.Exists(Primary),Is.False);Assert.That(session.Character,Is.EqualTo("remedios"));
        }
        [Test]public void ValidSaveWithBaguetteAndPartialUnlocksLoadsExactly()
        {
            var p=ProgressDto.New("es");p.meta.coins=321;p.meta.characters=new[]{"remedios","baguette"};p.meta.selected="baguette";
            p.meta.weapons=new[]{"chancla","naftalina","barra","dentaduras","jersey"};p.meta.items=new[]{"gafas","zapatillas","termo","cojin","lupa","loteria","rulos","monedero","olla"};
            Save(p);var session=Open();
            Assert.That(session.Status,Is.EqualTo("loaded"));Assert.That(Json(session.Progress),Is.EqualTo(Json(p)));
            var setup=session.Begin();Assert.That(setup.Character,Is.EqualTo("baguette"));
            CollectionAssert.AreEquivalent(p.meta.weapons,setup.AllowedWeapons);CollectionAssert.AreEquivalent(p.meta.items,setup.AllowedItems);
        }
        [Test]public void ProblematicSavesAreKeptAndNeverResetSilently()
        {
            Directory.CreateDirectory(dir);File.WriteAllText(Primary,"{roto",new UTF8Encoding(false));
            var session=Open();Assert.That(session.Status,Is.EqualTo("invalid"));Assert.That(session.CanSave,Is.False);
            Assert.That(session.Progress.meta.coins,Is.Zero,"juega con progreso inicial en memoria");
            var settled=session.Settle(Run("a",kills:400,victory:true));
            Assert.That(settled.Saved,Is.False);Assert.That(settled.Error,Is.EqualTo("storage.unavailable"));
            Assert.That(File.ReadAllText(Primary),Is.EqualTo("{roto"),"el archivo se conserva");
            Assert.That(session.Progress.meta.coins,Is.Zero,"no finge que se guardó");
        }
        [Test]public void ValidBackupRequiresConfirmedRecoveryBeforeSaving()
        {
            var good=ProgressDto.New("es");good.meta.coins=90;Save(good);good.meta.coins=50;Save(good);
            File.WriteAllText(Primary,"{roto",new UTF8Encoding(false));
            var session=Open();Assert.That(session.Status,Is.EqualTo("backup"));Assert.That(session.NeedsRecovery,Is.True);Assert.That(session.CanSave,Is.False);
            Assert.That(session.Settle(Run("x",kills:100)).Error,Is.EqualTo("storage.recovery-pending"));
            Assert.That(session.Recover(false).Success,Is.False);Assert.That(File.ReadAllText(Primary),Is.EqualTo("{roto"));
            Assert.That(session.Recover(true).Success,Is.True);Assert.That(session.Status,Is.EqualTo("loaded"));Assert.That(session.Progress.meta.coins,Is.EqualTo(90));
        }

        // ------------------------------------------------------------ selección
        [Test]public void OnlyUnlockedCharactersCanBeSelectedAndSelectionPersists()
        {
            var session=Open();Assert.That(session.Select("baguette"),Is.False,"Baguette bloqueado");
            Assert.That(session.Begin().Character,Is.EqualTo("remedios"));Assert.That(File.Exists(Primary),Is.False);
            var p=ProgressDto.New("es");p.meta.characters=new[]{"remedios","baguette"};Save(p);
            session=Open();Assert.That(session.Select("baguette"),Is.True);Assert.That(session.Begin().Character,Is.EqualTo("baguette"));
            Assert.That(Open().Progress.meta.selected,Is.EqualTo("baguette"),"selección guardada");
            Assert.That(session.Select("nadie"),Is.False);
        }

        // ------------------------------------------------------------ filtros
        [Test]public void LockedWeaponsNeverAppearThroughOffersRerollsOrBanishes()
        {
            var s=NewRun(Open().Begin());var r=s.Combat;r.Rerolls=r.Skips=r.Banishes=100000;
            var seenNew=new HashSet<string>();
            void Check(string where){foreach(var c in r.Offer){if(c.Kind=="newWeapon"){seenNew.Add(c.Id);Assert.That(c.Id!="jersey"&&c.Id!="fregona",Is.True,where+": "+c.Id);}}}
            for(int i=0;i<300;i++){
                r.PendingLevels=1;Assert.That(r.OpenChoice(),Is.True);Check("generación");
                Assert.That(r.Reroll(),Is.True);Check("reroll");
                int banish=r.Offer.FindIndex(c=>c.Key!=null);if(banish>=0){Assert.That(r.Banish(banish),Is.True);Check("descarte");}
                r.Banished.Clear();Assert.That(r.Skip(),Is.True);
            }
            Assert.That(seenNew,Is.SubsetOf(new[]{"naftalina","barra","dentaduras"}));Assert.That(seenNew,Is.Not.Empty);
            // Con el catálogo completo (QA/referencias) sí aparecen.
            var qa=new WorldRun(world,"U4-META",Catalog.Characters[0]){Automatic=false};var q=qa.Combat;q.Rerolls=100000;bool jersey=false;
            for(int i=0;i<300&&!jersey;i++){q.PendingLevels=1;q.OpenChoice();jersey|=q.Offer.Exists(c=>c.Id=="jersey");q.Reroll();jersey|=q.Offer.Exists(c=>c.Id=="jersey");q.Offer=null;q.PendingLevels=0;}
            Assert.That(jersey,Is.True);
        }
        [Test]public void ChestAndTotemItemsRespectUnlocksAndUnlockedOnesAppear()
        {
            var p=ProgressDto.New("es");p.meta.items=new[]{"gafas","zapatillas","termo","cojin","lupa","loteria","rulos","monedero","olla"};Save(p);
            var r=NewRun(Open().Begin()).Combat;var seen=new HashSet<string>();
            for(int i=0;i<3000;i++){var d=r.RollItem(i%3==0?150:i%3==1?50:0);if(d!=null)seen.Add(d.id);}
            Assert.That(seen,Is.SubsetOf(p.meta.items),"baúl/tótem: solo desbloqueados");Assert.That(seen,Does.Contain("olla"),"desbloqueado sí sale");
            foreach(var locked in new[]{"baraja","perlas","bata"})Assert.That(seen,Does.Not.Contain(locked));
        }
        [Test]public void OwnedWeaponsKeepUpgradingAndStartingWeaponIsAlwaysGiven()
        {
            var setup=Open().Begin();setup.Character="baguette";var r=NewRun(setup).Combat;
            Assert.That(r.Weapons[0].Def.id,Is.EqualTo("barra"));r.Rerolls=1000;bool upgrade=false;
            for(int i=0;i<200&&!upgrade;i++){r.PendingLevels=1;r.OpenChoice();upgrade|=r.Offer.Exists(c=>c.Kind=="weaponUpgrade"&&c.Id=="barra");r.Offer=null;r.PendingLevels=0;}
            Assert.That(upgrade,Is.True,"las mejoras de un arma poseída no se filtran (web)");
        }

        // ------------------------------------------------------------ extras
        [TestCase(0,0,0)][TestCase(1,2,3)][TestCase(3,3,3)]
        public void ExtrasSetInitialUsesAndConsumingThemDoesNotTouchProgress(int rerolls,int skips,int banishes)
        {
            var p=ProgressDto.New("es");p.meta.extras.rerolls=rerolls;p.meta.extras.skips=skips;p.meta.extras.banishes=banishes;Save(p);
            var session=Open();var r=NewRun(session.Begin()).Combat;
            Assert.That(r.Rerolls,Is.EqualTo(2+rerolls));Assert.That(r.Skips,Is.EqualTo(2+skips));Assert.That(r.Banishes,Is.EqualTo(2+banishes));
            r.PendingLevels=1;r.OpenChoice();r.Reroll();r.Skip();
            Assert.That(session.Progress.meta.extras.rerolls,Is.EqualTo(rerolls));Assert.That(Json(Open().Progress),Is.EqualTo(Json(p)));
            var again=NewRun(session.Begin()).Combat;Assert.That(again.Rerolls,Is.EqualTo(2+rerolls),"nueva partida restaura usos");
        }

        // ------------------------------------------------------------ misiones y liquidación
        [Test]public void EightMissionsCompleteOncePayOnceAndUnlockTheirRewards()
        {
            var session=Open();
            var first=session.Settle(Run("r1",kills:400,time:300,chests:4,shrines:1,level:12));
            Assert.That(first.Saved,Is.True);CollectionAssert.AreEquivalent(new[]{"first"},first.Receipt.completed);
            Assert.That(first.Receipt.kills,Is.EqualTo(20));Assert.That(first.Receipt.survival,Is.EqualTo(20));Assert.That(first.Receipt.missions,Is.EqualTo(30));
            Assert.That(session.Progress.meta.coins,Is.EqualTo(70));
            session.ForgetSettlement();var second=session.Settle(Run("r2",kills:400,chests:4,shrines:1,level:20,challenges:1));
            CollectionAssert.AreEquivalent(new[]{"challenge","level"},second.Receipt.completed);
            Assert.That(session.Progress.meta.items,Does.Contain("perlas"));Assert.That(session.Progress.meta.missions.kills,Is.EqualTo(800));
            session.ForgetSettlement();var third=session.Settle(Run("r3",kills:300,chests:2,shrines:1,victory:true,lifeTome:true));
            CollectionAssert.AreEquivalent(new[]{"kills","chests","shrines","victory"},third.Receipt.completed,"noLife no con tomo de vida");
            Assert.That(session.Progress.meta.weapons,Does.Contain("fregona"));Assert.That(session.Progress.meta.missions.kills,Is.EqualTo(1000));
            session.ForgetSettlement();var fourth=session.Settle(Run("r4",kills:5000,victory:true));
            CollectionAssert.AreEquivalent(new[]{"noLife"},fourth.Receipt.completed);Assert.That(session.Progress.meta.items,Does.Contain("bata"));
            Assert.That(session.Progress.meta.completed.Length,Is.EqualTo(8));
            session.ForgetSettlement();var fifth=session.Settle(Run("r5",kills:5000,victory:true,level:30,chests:50));
            Assert.That(fifth.Receipt.missions,Is.Zero,"recompensas de misión una sola vez");
            var reloaded=Open().Progress;Assert.That(Json(reloaded),Is.EqualTo(Json(session.Progress)));
            int expected=70+second.Receipt.total+third.Receipt.total+fourth.Receipt.total+fifth.Receipt.total;
            Assert.That(reloaded.meta.coins,Is.EqualTo(expected));
        }
        [Test]public void RepeatedSettlementOfTheSameRunPaysOnceEvenAfterReload()
        {
            var session=Open();var a=session.Settle(Run("unica",kills:200,victory:true));int coins=session.Progress.meta.coins;
            Assert.That(coins,Is.EqualTo(a.Receipt.total));byte[] saved=File.ReadAllBytes(Primary);
            var b=session.Settle(Run("unica",kills:200,victory:true));Assert.That(b,Is.SameAs(a));Assert.That(session.Progress.meta.coins,Is.EqualTo(coins));
            var reloaded=Open();var c=reloaded.Settle(Run("unica",kills:200,victory:true));
            Assert.That(c.NothingToSave,Is.True);Assert.That(c.Receipt.total,Is.Zero);Assert.That(reloaded.Progress.meta.coins,Is.EqualTo(coins));
            CollectionAssert.AreEqual(saved,File.ReadAllBytes(Primary));
        }
        [Test]public void CheatedRunsGiveNothingAndWriteNothing()
        {
            var session=Open();var s=session.Settle(Run("trucos",kills:5000,victory:true,cheated:true));
            Assert.That(s.NothingToSave,Is.True);Assert.That(s.Receipt.total,Is.Zero);Assert.That(session.Progress.meta.coins,Is.Zero);
            Assert.That(session.Progress.meta.missions.first,Is.Zero);Assert.That(File.Exists(Primary),Is.False);
        }
        [Test]public void AbandoningSimplyStartsAgainWithTheSameProgress()
        {
            var p=ProgressDto.New("es");p.meta.coins=45;Save(p);var session=Open();
            var run=NewRun(session.Begin());run.Combat.Kills=500;run.Combat.GainGold(300);
            var next=session.Begin();Assert.That(Json(session.Progress),Is.EqualTo(Json(p)));Assert.That(Json(Open().Progress),Is.EqualTo(Json(p)));
            Assert.That(next.RunId,Is.Not.EqualTo(session.Begin().RunId),"cada partida tiene identidad propia");
        }
        [TestCase("write")][TestCase("publish")]
        public void SaveFailureKeepsLastValidProgressAndRetryNeverDuplicates(string failOn)
        {
            var p=ProgressDto.New("es");p.meta.coins=100;Save(p);
            var failing=new FailingFiles(files);var session=Open(failing);failing.FailOn=failOn;
            var settled=session.Settle(Run("f",kills:400,victory:true));
            Assert.That(settled.Saved,Is.False);Assert.That(settled.Error,Does.StartWith("storage.write-failed"));
            Assert.That(session.Progress.meta.coins,Is.EqualTo(100),"no finge el guardado");Assert.That(Open().Progress.meta.coins,Is.EqualTo(100));
            failing.FailOn=null;var retried=session.RetrySave();
            Assert.That(retried.Saved,Is.True);int coins=100+settled.Receipt.total;Assert.That(session.Progress.meta.coins,Is.EqualTo(coins));
            session.RetrySave();session.Settle(Run("f",kills:400,victory:true));
            Assert.That(Open().Progress.meta.coins,Is.EqualTo(coins),"una sola vez");
        }
        [Test]public void FailedSettlementIsDroppedNotReappliedWhenANewRunStarts()
        {
            var failing=new FailingFiles(files);var session=Open(failing);failing.FailOn="write";
            Assert.That(session.Settle(Run("perdida",kills:400)).Saved,Is.False);
            failing.FailOn=null;session.ForgetSettlement();session.Begin();
            Assert.That(session.Progress.meta.coins,Is.Zero);Assert.That(File.Exists(Primary),Is.False);
            Assert.That(session.Settle(Run("siguiente",kills:20)).Saved,Is.True);Assert.That(Open().Progress.meta.coins,Is.EqualTo(1+4+30),"bajas + supervivencia + primera partida");
        }

        // ------------------------------------------------------------ tienda y opciones (paso 7)
        [Test]public void ShopAndOptionsUseMetaRulesAndSafeSaving()
        {
            var p=ProgressDto.New("es");p.meta.coins=300;Save(p);var session=Open();
            Assert.That(session.Purchase("olla"),Is.True);Assert.That(session.Purchase("olla"),Is.False,"ya comprado");
            Assert.That(session.Purchase("baguette"),Is.False,"faltan 100");Assert.That(session.Purchase("inventado"),Is.False);
            Assert.That(session.PurchaseExtra("skips"),Is.True);Assert.That(session.Progress.meta.coins,Is.EqualTo(300-180-80));
            Assert.That(session.SetLanguage("en"),Is.True);Assert.That(session.SetLanguage("fr"),Is.False);
            Assert.That(session.SetRunMinutes(5),Is.True);Assert.That(session.SetRunMinutes(7),Is.False);
            var stored=Open().Progress;Assert.That(Json(stored),Is.EqualTo(Json(session.Progress)));
            Assert.That(stored.meta.items,Does.Contain("olla"));Assert.That(stored.meta.extras.skips,Is.EqualTo(1));
            Assert.That(stored.settings.language,Is.EqualTo("en"));Assert.That(stored.settings.runMinutes,Is.EqualTo(5));
            Assert.That(session.Begin().AllowedItems.Contains("olla"),Is.True);
        }
        [Test]public void WithoutUsableStorageChoicesStayInMemoryAndTheFileIsKept()
        {
            Directory.CreateDirectory(dir);File.WriteAllText(Primary,"{roto",new UTF8Encoding(false));
            var session=Open();Assert.That(session.SetRunMinutes(15),Is.True);Assert.That(session.Progress.settings.runMinutes,Is.EqualTo(15));
            Assert.That(File.ReadAllText(Primary),Is.EqualTo("{roto"));
        }

        // ------------------------------------------------------------ opciones (paso 8)
        static void ChangeAll(SettingsDto s)
        {
            s.musicVolume=.25;s.effectsVolume=1;s.muted=true;s.reducedParticles=true;s.cameraShake=false;s.flashes=false;s.language="en";
            s.mouseSensitivity=2.4;s.renderHeight=480;s.vertexSnap=false;s.dithering=false;s.slideWithCtrl=true;s.showFps=true;s.runMinutes=15;
        }
        [Test]public void TheFourteenOptionsChangeSaveAndSurviveAReload()
        {
            Save(ProgressDto.New("es"));var session=Open();
            var change=session.ChangeSettings(ChangeAll);Assert.That(change.Applied,Is.True);Assert.That(change.Saved,Is.True);Assert.That(change.Error,Is.Null);
            var expected=ProgressDto.New("es");ChangeAll(expected.settings);
            Assert.That(Json(session.Progress),Is.EqualTo(Json(expected)));
            var reloaded=Open();Assert.That(reloaded.Status,Is.EqualTo("loaded"));Assert.That(Json(reloaded.Progress),Is.EqualTo(Json(expected)),"las 14 opciones tras recargar");
            Assert.That(reloaded.Progress.meta.coins,Is.Zero,"las opciones no tocan la meta");
        }
        [Test]public void OptionLimitsComeFromTheContractAndInvalidValuesChangeNothing()
        {
            Save(ProgressDto.New("es"));var session=Open();
            // Extremos permitidos.
            foreach(Action<SettingsDto> ok in new Action<SettingsDto>[]{s=>s.mouseSensitivity=.2,s=>s.mouseSensitivity=3,s=>s.musicVolume=0,s=>s.musicVolume=1,s=>s.effectsVolume=0,
                s=>s.effectsVolume=1,s=>s.renderHeight=240,s=>s.renderHeight=360,s=>s.renderHeight=480,s=>s.runMinutes=5,s=>s.runMinutes=10,s=>s.runMinutes=15,s=>s.language="en",s=>s.language="es"})
                Assert.That(session.ChangeSettings(ok).Saved,Is.True);
            var before=File.ReadAllBytes(Primary);string current=Json(session.Progress);
            // Fuera del contrato, NaN o infinito: se rechaza sin tocar la sesión ni el archivo.
            foreach(Action<SettingsDto> bad in new Action<SettingsDto>[]{s=>s.mouseSensitivity=.19,s=>s.mouseSensitivity=3.01,s=>s.mouseSensitivity=double.NaN,
                s=>s.musicVolume=-.01,s=>s.musicVolume=1.01,s=>s.effectsVolume=double.PositiveInfinity,s=>s.renderHeight=300,s=>s.renderHeight=0,
                s=>s.runMinutes=7,s=>s.runMinutes=0,s=>s.language="fr",s=>s.language=null}){
                var result=session.ChangeSettings(bad);
                Assert.That(result.Applied,Is.False);Assert.That(result.Error,Is.EqualTo("value.invalid"));
            }
            Assert.That(Json(session.Progress),Is.EqualTo(current));CollectionAssert.AreEqual(before,File.ReadAllBytes(Primary));
        }
        [Test]public void InvalidStoredOptionsLoadAsDefaultsWithoutSilentReset()
        {
            var p=ProgressDto.New("es");p.meta.coins=77;p.settings.showFps=true;var tree=ProgressTree.From(p);var settings=(Dictionary<string,object>)tree["settings"];
            settings["mouseSensitivity"]=9.0;settings["renderHeight"]=300;settings["runMinutes"]=7;
            Directory.CreateDirectory(dir);string json=ProgressJson.Stringify(ProgressTree.Object("format","mamporro.unity-save","version",1,"progress",tree));
            File.WriteAllText(Primary,json,new UTF8Encoding(false));
            var session=Open();Assert.That(session.Status,Is.EqualTo("review"));
            var s=session.Progress.settings;Assert.That(s.mouseSensitivity,Is.EqualTo(1));Assert.That(s.renderHeight,Is.EqualTo(360));Assert.That(s.runMinutes,Is.EqualTo(10));
            Assert.That(s.showFps,Is.True,"las opciones válidas se conservan");Assert.That(session.Progress.meta.coins,Is.EqualTo(77));
            Assert.That(File.ReadAllText(Primary),Is.EqualTo(json),"sin recuperación confirmada no se reescribe");
            Assert.That(session.Recover(true).Success,Is.True);Assert.That(Open().Progress.settings.renderHeight,Is.EqualTo(360));
        }
        [TestCase("write")][TestCase("publish")]
        public void OptionSaveFailureKeepsTheChangeOnlyForThisSession(string failOn)
        {
            Save(ProgressDto.New("es"));var before=File.ReadAllBytes(Primary);
            var failing=new FailingFiles(files);var session=Open(failing);failing.FailOn=failOn;
            var change=session.ChangeSettings(s=>{s.showFps=true;s.renderHeight=240;});
            Assert.That(change.Applied,Is.True);Assert.That(change.Saved,Is.False,"no finge el guardado");Assert.That(change.Error,Does.StartWith("storage.write-failed"));
            Assert.That(session.SettingsSessionOnly,Is.True);Assert.That(session.Progress.settings.showFps,Is.True);Assert.That(session.Progress.settings.renderHeight,Is.EqualTo(240));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(Primary),"el último guardado válido sigue intacto");
            Assert.That(Open().Progress.settings.showFps,Is.False);
            // El siguiente guardado correcto escribe también lo pendiente.
            failing.FailOn=null;Assert.That(session.ChangeSettings(s=>s.flashes=false).Saved,Is.True);Assert.That(session.SettingsSessionOnly,Is.False);
            var stored=Open().Progress.settings;Assert.That(stored.showFps,Is.True);Assert.That(stored.renderHeight,Is.EqualTo(240));Assert.That(stored.flashes,Is.False);
        }
        [Test]public void ImportedOptionsReplaceTheSessionOptions()
        {
            Save(ProgressDto.New("es"));var session=Open();
            var web=ProgressDto.New("es");ChangeAll(web.settings);web.meta.coins=55;
            var bytes=Encoding.UTF8.GetBytes(ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,
                "source",ProgressTree.Object("platform","web","saveVersion",3),"progress",ProgressTree.From(web))));
            var importer=new ProgressImporter(session.Store,"es");var review=importer.Review(bytes);Assert.That(review.CanConfirm,Is.True);
            var result=importer.Confirm(review,true);Assert.That(result.Success,Is.True);session.Adopt(result);
            Assert.That(Json(session.Progress),Is.EqualTo(Json(web)));Assert.That(session.Progress.settings.language,Is.EqualTo("en"));
            Assert.That(Json(Open().Progress),Is.EqualTo(Json(web)),"y siguen tras recargar");
        }

        // ------------------------------------------------------------ partida real → recibo → guardado → recarga
        [Test]public void RealRunCountersFlowThroughMetaRulesIntoStorage()
        {
            var session=Open();var setup=session.Begin();
            var s=new WorldRun(world,"U4-META",Catalog.Characters[0]);ProgressSession.Apply(setup,s);s.Combat.Invincible=true;
            for(int t=0;t<60*60;t++){s.Step(default,1.0/60);while(s.Combat.Choosing)s.Combat.Choose(0);}
            Assert.That(s.Combat.Kills,Is.GreaterThan(0));Assert.That(s.Cheated,Is.False,"invencible por código no es una acción F3");
            s.Combat.AddTome("vitality");Assert.That(s.Combat.UsedLifeTome,Is.True);
            var summary=ProgressSession.Summary(s,setup.RunId);
            Assert.That(summary.kills,Is.EqualTo(s.Combat.Kills));Assert.That(summary.time,Is.EqualTo(s.Combat.Time));Assert.That(summary.level,Is.EqualTo(s.Combat.Level));
            Assert.That(summary.usedLifeTome,Is.True);
            var settled=session.Settle(summary);Assert.That(settled.Saved,Is.True);
            var reloaded=Open().Progress;Assert.That(Json(reloaded),Is.EqualTo(Json(session.Progress)));
            Assert.That(reloaded.meta.missions.kills,Is.EqualTo(s.Combat.Kills));Assert.That(reloaded.meta.missions.first,Is.EqualTo(1));
            Assert.That(reloaded.meta.coins,Is.EqualTo(settled.Receipt.total));Assert.That(reloaded.meta.lastRun,Is.EqualTo(setup.RunId));
            var debug=new WorldRun(world,"U4-META",Catalog.Characters[0]);debug.DebugAddGold(10);
            Assert.That(ProgressSession.Summary(debug,"d").cheated,Is.True,"una acción F3 marca la partida");
        }
    }
}
