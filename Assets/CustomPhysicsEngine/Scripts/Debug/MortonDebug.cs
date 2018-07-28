using CP.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CP.Debug
{
    public class MortonDebug : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {
            PrintMorton();
        }

        private void PrintMorton()
        {
            Morton.PrintMorton3DTablesToFile();
            Morton.PrintMorton2DTablesToFile();
        }
    }
}

