using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class CheckDriveIntro
{
    static async Task Step(int count)
    {
        for(int i=0;i<count;i++){EditorApplication.Step();await Task.Delay(25);}
    }
    public static async Task<string> FromShowcase(int index=0)
    {
        SceneManager.LoadScene("ShowcaseScene");await Step(3);
        var manager=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();
        for(int i=0;i<index;i++)manager.ShowNextVehicle();
        manager.OpenDriveMenu();manager.FreeRun();manager.MountainPassRide();manager.StartFreeRide();
        for(int i=0;i<120;i++)
        {
            await Step(1);
            if(UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>()!=null)break;
        }
        await Step(2);
        return State();
    }
    public static string State()
    {
        var intro=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>();
        var vehicle=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();
        return "scene="+SceneManager.GetActiveScene().name+", intro="+(intro!=null)+
            ", held="+(vehicle!=null&&vehicle.IsChallengeStopped)+
            ", follow="+UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled+
            ", title="+(intro!=null?intro.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="IntroTitle").text:"-")+
            ", selected="+TestDriveSelection.VehicleName;
    }
    // Seek a shot in the live Editor without waiting for every real-time frame.
    public static async Task<string> Seek(float time)
    {
        var intro=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>();
        typeof(TestDriveIntro).GetField("elapsed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(intro,time);
        await Step(2);return State();
    }
    public static async Task<string> Skip()
    {
        var intro=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>();
        intro.transform.Find("IntroSafeArea/SkipIntro").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        intro.Skip();await Step(3);
        var car=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();
        var follow=UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>();follow.ToggleView();
        return State()+", driverView="+follow.IsDriverView+", frozen="+(car.GetComponent<Rigidbody>().constraints==RigidbodyConstraints.FreezeAll);
    }
    public static async Task<string> CompletePaused()
    {
        float original=Time.timeScale;
        Time.timeScale=0;
        try { return await Seek(8.99f)+", timeScale="+Time.timeScale; }
        finally { Time.timeScale=original; }
    }
    public static async Task<string> Challenge()
    {
        await Load("TestTrackScene",0,false,true);
        var challenge=UnityEngine.Object.FindAnyObjectByType<TestDriveCleanLapChallenge>();
        bool noBriefing=UnityEngine.Object.FindAnyObjectByType<CleanLapBriefing>()==null;
        string before=challenge.State+", elapsed="+challenge.ElapsedSeconds+", briefingDeferred="+noBriefing;
        UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>().Skip();await Step(3);
        var briefing=UnityEngine.Object.FindAnyObjectByType<CleanLapBriefing>();
        var length=typeof(TestDriveCleanLapChallenge).GetField("length",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(challenge);
        string ready=challenge.State+", briefing="+(briefing!=null)+", routeLength="+length;
        briefing.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="StartChallenge").onClick.Invoke();
        await Step(3);
        return "Before: "+before+"; after skip: "+ready+"; after START: "+challenge.State;
    }
    public static async Task<string> ReloadNoIntro()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);await Step(3);
        return State();
    }
    public static async Task<string> ChallengeModes()
    {
        var results=new System.Collections.Generic.List<string>();
        foreach(TestDriveSelection.ChallengeKind kind in Enum.GetValues(typeof(TestDriveSelection.ChallengeKind)))
        {
            TestDriveSelection.Select(0,TestDriveSelection.DriveMode.Challenge,challenge:kind);
            SceneManager.LoadScene("TestTrackScene");await Step(3);
            await Seek(4);
            string shot=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>().GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.name=="IntroTitle").text;
            UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>().Skip();await Step(2);
            var challenge=UnityEngine.Object.FindAnyObjectByType<TestDriveCleanLapChallenge>();
            results.Add(kind+": "+shot+", "+challenge.State+", time="+challenge.ElapsedSeconds+", briefing="+(UnityEngine.Object.FindAnyObjectByType<CleanLapBriefing>()!=null));
        }
        return string.Join("; ",results);
    }
    public static async Task<string> ReturnDuringIntro()
    {
        await Load("MountainPassScene",0,false);
        UnityEngine.Object.FindAnyObjectByType<TestDriveSceneManager>().ReturnToShowroom();
        for(int i=0;i<100&&SceneManager.GetActiveScene().name!="ShowcaseScene";i++)await Step(1);
        return "scene="+SceneManager.GetActiveScene().name+", intros="+UnityEngine.Object.FindObjectsByType<TestDriveIntro>().Length;
    }
    public static async Task<string> Load(string scene,int index,bool night,bool challenge=false)
    {
        TestDriveSelection.Select(index,challenge?TestDriveSelection.DriveMode.Challenge:TestDriveSelection.DriveMode.FreeRun,
            night?TestDriveSelection.TimeOfDay.Night:TestDriveSelection.TimeOfDay.Day,course:scene=="MountainPassScene"?TestDriveSelection.CourseKind.MountainPass:scene=="LakeTownScene"?TestDriveSelection.CourseKind.LakeTown:TestDriveSelection.CourseKind.TestCircuit);
        SceneManager.LoadScene(scene);await Step(3);return State();
    }
}
