using UnityEngine;
using TMPro;
using System.Collections;
namespace Final
{
    public class ScreenLogger
    {
        private TextMeshProUGUI _text;
        private MonoBehaviour _runner;

        public void LogToScreen(string message, float duration = 1f, float fadeTime = 1f)
        {
            if (_text == null)
                CreateUI();

            if (_text == null)
                return; // canvas not found

            _text.text = message;
            _text.alpha = 1f;

            _runner.StopAllCoroutines();
            _runner.StartCoroutine(FadeOut(duration, fadeTime));
        }

        private void CreateUI()
        {
            GameObject canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogWarning("ScreenLogger: Canvas not found.");
                return;
            }

            // Coroutine runner (needed because this is a static class)
            _runner = canvasGO.GetComponent<MonoBehaviour>();
            if (_runner == null)
                _runner = canvasGO.AddComponent<DummyRunner>();

            GameObject textGO = new GameObject("ScreenLoggerText");
            textGO.transform.SetParent(canvasGO.transform, false);

            _text = textGO.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 28;
            _text.alignment = TextAlignmentOptions.Center;
            _text.raycastTarget = false;
            
            _text.font = Main.Instance.imageRipper.Lantern;
            _text.color = Main.Instance.imageRipper.LanternRed;

            RectTransform rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.2f);
            rt.anchorMax = new Vector2(0.5f, 0.2f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(800, 100);
        }

        private IEnumerator FadeOut(float delay, float fadeTime)
        {
            yield return new WaitForSecondsRealtime(delay);

            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                _text.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
                yield return null;
            }

            _text.alpha = 0f;
        }

        // Empty MonoBehaviour used only to run coroutines
        private class DummyRunner : MonoBehaviour { }
    }
}