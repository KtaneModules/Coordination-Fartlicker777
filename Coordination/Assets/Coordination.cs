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
   public TextMesh[] Texts;
   public GameObject[] ButtonColors;
   public Material Black;

   int StartingCoordinate;
   int Current;
   int AnswerCoordinate;

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

      foreach (KMSelectable Button in Buttons) {
          Button.OnInteract += delegate () { ButtonPress(Button); return false; };
      }

   }

   void ButtonPress (KMSelectable Button) {
      if (moduleSolved) {
         return;
      }
      if (Button == Buttons[AnswerCoordinate]) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
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
      ButtonColors[StartingCoordinate].GetComponent<MeshRenderer>().material = Black;
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
