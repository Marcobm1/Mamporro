using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Mamporro.U2;

namespace Mamporro.Tests
{
    public sealed class HudTests
    {
        [TearDown]public void Spanish()=>CombatText.English=false;

        [UnityTest]public IEnumerator HudShowsRunGoldRoundedDown()
        {
            yield return SceneManager.LoadSceneAsync("U2_Combate");yield return null;
            var s=Object.FindAnyObjectByType<CombatSession>();
            s.Run.GainGold(37.9);s.RefreshHud();StringAssert.Contains("Oro 37",s.View.status.text);
            CombatText.English=true;s.RefreshHud();StringAssert.Contains("Gold 37",s.View.status.text);
            s.Restart(0);StringAssert.Contains("Gold 0",s.View.status.text);
        }
    }
}
