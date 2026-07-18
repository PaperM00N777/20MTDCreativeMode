using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Final
{
    public class ImageRipper
    {
        public Image GenericBox;
        public Image ScrollHandle;
        public Image ScrollBar;
        public TMP_FontAsset[] fonts;
        public TMP_FontAsset Lantern;
        public TMP_FontAsset Express;
        public Color LanternRed;
        public Color ButtonBlue;

        public void FightSceneRip()
        {
            GenericBox = GameObject.Find("PauseMenu").GetComponent<Image>();
            ScrollHandle = GameObject.Find("Canvas/SynergiesUI/Scroll View/Scrollbar/Sliding Area/Handle").GetComponent<Image>();
            ScrollBar = GameObject.Find("Canvas/SynergiesUI/Scroll View/Scrollbar").GetComponent<Image>();
            fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>(); 
            GameObject ShootControls = GameObject.Find("ShootControls").transform.Find("Keybind").gameObject;
            foreach (var font in fonts)
            {
                if (font.name == "Lantern")
                {
                    Lantern = font;
                }
                else if (font.name == "Express")
                {
                    Express = font;
                }
            }
            ColorUtility.TryParseHtmlString("#fd5161", out LanternRed);
            ColorUtility.TryParseHtmlString("#293448", out ButtonBlue);
        }
    }
}
