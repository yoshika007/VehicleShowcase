using UnityEngine;
using TMPro;

// Relative showroom ratings, not measured lap times or acceleration figures.
public sealed class VehicleCapabilityBars : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image[] fills;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private UnityEngine.UI.Image speedFill;
    [SerializeField] private TMP_Text speedValue;
    [SerializeField] private TMP_Text[] ratingValues;
    private const float BarWidth = 180f;
    private const float RowSpacing = 42f;
    // All vehicles use the same scale; 240 km/h leaves headroom above the current 220 km/h maximum.
    private const float MaxSpeedScaleKmh = 240f;

    public void Initialize(TMP_Text description)
    {
        if (fills != null && (fills.Length == 6 || fills.Length == 4) && speedText != null)
        {
            EnsureSpeedBar(description);
            EnsureRatingValues(description);
            ArrangeVisibleRows();
            description.gameObject.SetActive(false);
            return;
        }
        var area = GetComponent<RectTransform>();
        area.SetParent(description.transform.parent, false);
        area.anchorMin = new Vector2(0f, 1f); area.anchorMax = new Vector2(1f, 1f);
        area.pivot = new Vector2(.5f, 1f); area.sizeDelta = new Vector2(-56f, 240f);
        area.anchoredPosition = new Vector2(0f, -201f);
        fills = new UnityEngine.UI.Image[4];
        string[] names = { "ACCELERATION", "BRAKING", "HANDLING", "STABILITY" };
        for (int i = 0; i < names.Length; i++)
        {
            float y = -(i + 1) * RowSpacing;
            Label(area, description, names[i], new Vector2(0f, y), new Vector2(196f, 40f));
            var track = Image(area, "Track", new Vector2(205f, y - 7f), new Vector2(BarWidth, 18f), new Color(.15f, .27f, .28f, 1f));
            fills[i] = Image(track.rectTransform, "Fill", Vector2.zero, new Vector2(BarWidth, 18f), new Color(.98f, .74f, .25f, 1f));
        }
        speedText = Label(area, description, "MAX SPEED", Vector2.zero, new Vector2(196f, 40f));
        EnsureSpeedBar(description);
        EnsureRatingValues(description);
        ArrangeVisibleRows();
        description.gameObject.SetActive(false);
    }

    private void ArrangeVisibleRows()
    {
        // Keep the authored ratings intact while hiding the two road-use rows.
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.text == "ON-ROAD" || label.text == "OFF-ROAD")
                label.gameObject.SetActive(false);
        }
        for (int i = 0; i < fills.Length; i++)
        {
            if (fills[i] == null) continue;
            var track = fills[i].transform.parent as RectTransform;
            if (track == null) continue;
            track.gameObject.SetActive(i < 4);
            if (i >= 4) continue;
            track.anchoredPosition = new Vector2(205f, -(i + 1) * RowSpacing - 7f);
            track.sizeDelta = new Vector2(BarWidth, 18f);
        }
        string[] names = { "ACCELERATION", "BRAKING", "HANDLING", "STABILITY" };
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
        {
            int index = System.Array.IndexOf(names, label.text);
            if (index >= 0) { label.rectTransform.anchoredPosition = new Vector2(0f, -(index + 1) * RowSpacing); label.rectTransform.sizeDelta = new Vector2(196f, 40f); label.fontSize = 24f; }
        }
        speedText.text = "MAX SPEED";
        speedText.fontSize = 24f;
        speedText.enableAutoSizing = false;
        speedText.rectTransform.anchoredPosition = Vector2.zero;
        speedText.rectTransform.sizeDelta = new Vector2(196f, 40f);
        ((RectTransform)transform).sizeDelta = new Vector2(-56f, 240f);
        for (int i = 0; i < ratingValues.Length; i++) { ratingValues[i].fontSize = 22f; ratingValues[i].rectTransform.anchoredPosition = new Vector2(390f, -(i + 1) * RowSpacing); ratingValues[i].rectTransform.sizeDelta = new Vector2(98f, 40f); }
        speedValue.fontSize = 22f;
    }

    private void EnsureSpeedBar(TMP_Text source)
    {
        if (speedFill != null && speedValue != null)
        {
            var existingTrack = (RectTransform)speedFill.transform.parent;
            existingTrack.sizeDelta = new Vector2(BarWidth, 18f);
            existingTrack.anchoredPosition = new Vector2(205f, -7f);
            speedValue.rectTransform.SetParent(transform, false);
            speedValue.rectTransform.anchoredPosition = new Vector2(390f, 0f);
            speedValue.rectTransform.sizeDelta = new Vector2(98f, 40f);
            return;
        }
        var area = (RectTransform)transform;
        var track = Image(area, "MaxSpeedTrack", new Vector2(205f, -7f), new Vector2(BarWidth, 18f), new Color(.15f, .27f, .28f, 1f));
        speedFill = Image(track.rectTransform, "Fill", Vector2.zero, new Vector2(BarWidth, 18f), new Color(.98f, .74f, .25f, 1f));
        speedValue = Label(area, source, "-- km/h", new Vector2(390f, 0f), new Vector2(98f, 40f));
        speedValue.name = "MaxSpeedValue";
        speedValue.fontSize = 22f;
        speedValue.alignment = TextAlignmentOptions.MidlineRight;
        speedValue.color = Color.white;
    }

    private void EnsureRatingValues(TMP_Text source)
    {
        if (ratingValues != null && ratingValues.Length == 4 && System.Array.TrueForAll(ratingValues, x => x != null)) return;
        ratingValues = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var label = Label((RectTransform)transform, source, "--/10", new Vector2(390f, -(i + 1) * RowSpacing), new Vector2(98f, 40f));
            label.name = "RatingValue" + i;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            ratingValues[i] = label;
        }
    }

    private static TMP_Text Label(RectTransform parent, TMP_Text source, string value, Vector2 position, Vector2 size)
    {
        var obj = new GameObject("CapabilityLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = source.font; text.fontSharedMaterial = source.fontSharedMaterial;
        text.text = value; text.fontSize = 24f; text.color = new Color(.88f, .93f, .91f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
        return text;
    }

    private static UnityEngine.UI.Image Image(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
        return image;
    }

    public void Show(Vehicle vehicle, float? topSpeed)
    {
        // Broad relative ratings reflect the vehicles' intended roles, not physical measurements.
        Vector3 rating = vehicle switch
        {
            Hatchback => new Vector3(.48f, .72f, .25f),
            PickupTruck => new Vector3(.50f, .62f, .72f),
            Van => new Vector3(.35f, .58f, .32f),
            ClassicCar => new Vector3(.45f, .70f, .20f),
            PoliceCar => new Vector3(.67f, .82f, .28f),
            MuscleCar => new Vector3(.85f, .74f, .18f),
            SportsCar => new Vector3(.83f, .95f, .15f),
            MonsterTruck => new Vector3(.62f, .42f, 1f),
            MilitaryVehicle => new Vector3(.57f, .48f, .90f),
            _ => new Vector3(.5f, .5f, .5f)
        };
        Vector2 handling = vehicle switch
        {
            Hatchback => new Vector2(.68f, .70f),
            PickupTruck => new Vector2(.52f, .68f),
            Van => new Vector2(.48f, .64f),
            ClassicCar => new Vector2(.60f, .65f),
            PoliceCar => new Vector2(.75f, .82f),
            MuscleCar => new Vector2(.62f, .60f),
            SportsCar => new Vector2(.92f, .82f),
            MonsterTruck => new Vector2(.40f, .55f),
            MilitaryVehicle => new Vector2(.44f, .78f),
            _ => new Vector2(.5f, .5f)
        };
        float braking = vehicle switch
        {
            Hatchback => .65f,
            PickupTruck => .62f,
            Van => .52f,
            ClassicCar => .60f,
            PoliceCar => .80f,
            MuscleCar => .68f,
            SportsCar => .90f,
            MonsterTruck => .55f,
            MilitaryVehicle => .66f,
            _ => .5f
        };
        float[] values = { rating.x, braking, handling.x, handling.y, rating.y, rating.z };
        for (int i = 0; i < fills.Length; i++)
            fills[i].rectTransform.sizeDelta = new Vector2(BarWidth * values[i], 18f);
        for (int i = 0; i < ratingValues.Length; i++)
            ratingValues[i].text = (values[i] * 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "/10";
        speedValue.text = topSpeed.HasValue ? $"{Mathf.RoundToInt(topSpeed.Value)} km/h" : "-- km/h";
        speedFill.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(topSpeed.GetValueOrDefault() / MaxSpeedScaleKmh), 18f);
    }
}
