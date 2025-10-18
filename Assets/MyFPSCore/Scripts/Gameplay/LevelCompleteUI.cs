using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MyFPSCore.Gameplay
{
    public class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text headline;
        [SerializeField] private TMP_Text subtitle;

        public void SetCopy(string h, string s)
        {
            if (headline) headline.text = h;
            if (subtitle) subtitle.text = s;
        }
    }
}

