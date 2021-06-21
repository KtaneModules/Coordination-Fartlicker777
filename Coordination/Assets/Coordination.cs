using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class Coordination : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Buttons;
   private TextMesh[] Texts;
   private MeshRenderer[] Meshes;
    private Color[] colors;
   int StartingCoordinate;
   int Current;
   int AnswerCoordinate;
    bool[] flashed = new bool[36]; //For solve anim
    enum SqCols
    {
        White,
        Black,
        Highlight,
        Solved
    }
   
   string[] ManualCoordinates = {
      "F4", "A2", "E2", "D4", "C5", "E1",
      "A3", "F3", "D1", "C2", "E6", "D3",
      "C3", "E4", "C4", "B6", "A6", "B5",
      "E3", "C1", "D6", "B2", "A1", "E5",
      "F1", "F5", "B1", "B3", "D5", "C6",
      "F6", "D2", "A4", "B4", "A5", "F2" };
   string[] ModuleCoordinates = {
      "F4", "A2", "E2", "D4", "C5", "E1",
      "A3", "F3", "D1", "C2", "E6", "D3",
      "C3", "E4", "C4", "B6", "A6", "B5",
      "E3", "C1", "D6", "B2", "A1", "E5",
      "F1", "F5", "B1", "B3", "D5", "C6",
      "F6", "D2", "A4", "B4", "A5", "F2" };

   bool[] TakenManualSpots = new bool[36];
   bool[] TakenModuleSpots = new bool[36];
   bool Turn; //If false, we toggle the module. If true, we toggle the manual.

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   void Awake () {
      moduleId = moduleIdCounter++;
      Texts = Buttons.Select(x => x.GetComponentInChildren<TextMesh>()).ToArray();
      Meshes = Buttons.Select(x => x.GetComponent<MeshRenderer>()).ToArray();
        colors = new Color[] { "DDDDDD".Color(), "27172B".Color(), "7A9CB0".Color(), "6EFB69".Color() };
      foreach (KMSelectable Button in Buttons)
        {
          Button.OnInteract += delegate () { ButtonPress(Button); return false; };
            Button.OnHighlight += delegate () { if (!moduleSolved) { Audio.PlaySoundAtTransform("SelectionBeep", Button.transform); Button.GetComponent<MeshRenderer>().material.color = colors[(int)SqCols.Highlight]; }  };
            Button.OnHighlightEnded += delegate () { if (!moduleSolved) Button.GetComponent<MeshRenderer>().material.color =
                (Array.IndexOf(Buttons, Button) == StartingCoordinate) ? colors[(int)SqCols.Black] : colors[(int)SqCols.White]; };
      }

   }

   void ButtonPress (KMSelectable Button) {
      if (moduleSolved) {
         return;
      }
      if (Button == Buttons[AnswerCoordinate]) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
            Audio.PlaySoundAtTransform("SolveSound", transform);
            StartCoroutine(Solve(AnswerCoordinate));
      }
      else {
         GetComponent<KMBombModule>().HandleStrike();
      }
   }

   void Start () {
      Generation();
   }

   void Generation () {
      ModuleCoordinates.Shuffle();
      for (int i = 0; i < 36; i++) {
         Texts[i].text = ModuleCoordinates[i];
      }
      StartingCoordinate = Rnd.Range(0, 36);
      Meshes[StartingCoordinate].material.color = colors[(int)SqCols.Black];
      Texts[StartingCoordinate].color = Color.white;
      Current = StartingCoordinate;
      Debug.LogFormat("[Coordination #{0}] The starting coordinate is {1}.", moduleId, NumberToCoordinate(StartingCoordinate));
      Pathfinder();
   }

   void Pathfinder () {
      int Previous = Current;
      if (Turn) {
         if (TakenManualSpots[Current]) { //If we land on a spot that would cause an infinite loop.
            Debug.LogFormat("[Coordination #{0}] To prevent an infinite loop, we move one to the right.", moduleId);
            //AnswerCoordinate = StartingCoordinate;
            //return;
            if (Current % 6 == 5) {
               Current -= 5;
            }
            else {
               Current++;
            }
         }
         TakenManualSpots[Current] = true;
         Current = CoordinateToNumber(ManualCoordinates[Current]);
         Debug.LogFormat("[Coordination #{0}] From {1} in the manual, we land on {2} on the module.", moduleId, NumberToCoordinate(Previous), ModuleCoordinates[Current]);
         Turn = !Turn;
      }
      else {
         if (TakenModuleSpots[Current]) { //If we land on a spot that would cause an infinite loop.
            Debug.LogFormat("[Coordination #{0}] To prevent an infinite loop, we move one to the right.", moduleId);
            //AnswerCoordinate = StartingCoordinate;
            //return;
         }
         TakenModuleSpots[Current] = true;
         Current = CoordinateToNumber(ModuleCoordinates[Current]);
         Debug.LogFormat("[Coordination #{0}] From {1} on the module, we land on {2} on the manual.", moduleId, NumberToCoordinate(Previous), NumberToCoordinate(Current));
         Turn = !Turn;
      }
      for (int i = 0; i < 36; i++) {
         if (TakenManualSpots[i] && TakenModuleSpots[i]) {
            AnswerCoordinate = i;
            Debug.LogFormat("[Coordination #{0}] {1} has been visited in the manual and on the module, this is the button you should press.", moduleId, NumberToCoordinate(Previous));
            return;
         }
      }
      Pathfinder();
   }

   int CoordinateToNumber (string Input) {
      string Letters = "ABCDEF";
      int temp = Array.IndexOf(Letters.ToCharArray(), Input[0]);
      temp += (int.Parse(Input[1].ToString()) - 1) * 6;
      return temp;
   }

   string NumberToCoordinate (int Input) {
      return "ABCDEF"[Input % 6].ToString() + (Input / 6 + 1).ToString();
   }
    IEnumerator Solve(int pos)
    {
        flashed[pos] = true;
        Texts[pos].gameObject.SetActive(false);
        Meshes[pos].material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        List<int> adjs = new List<int>();
        if (pos > 5) adjs.Add(pos - 6);
        if (pos < 30) adjs.Add(pos + 6);
        if (pos % 6 != 0) adjs.Add(pos - 1);
        if (pos % 6 != 5) adjs.Add(pos + 1);
        foreach (int nextPos in adjs.Where(x => !flashed[x]))
            StartCoroutine(Solve(nextPos));
        yield return new WaitForSeconds(0.3f);
        float lerp = 0;
        while (lerp < 1)
        {
            lerp += 5*Time.deltaTime;
            Meshes[pos].material.color = Color.Lerp(Color.white, colors[(int)SqCols.Solved], lerp);
            yield return null;
        }
    }


#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} X# to press that button.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      if (!"ABCDEF".Contains(Command[0]) || !"123456".Contains(Command[1]) || Command.Length != 2) {
         yield return "sendtochaterror I don't understand!";
      }
      else {
         Buttons[CoordinateToNumber(Command)].OnInteract();
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      Buttons[AnswerCoordinate].OnInteract();
      yield return null;
   }
}
