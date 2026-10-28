using UnityEngine;
using TMPro;

// Relative showroom ratings, not measured lap times or acceleration figures.
public sealed class VehicleCapabilityBars : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image[] fills;
    [SerializeField] private TMP_Text speedText;

    public void Initialize(TMP_Text description)
    {
        if (fills != null && fills.Length == 6 && speedText != null)
        {
            description.gameObject.SetActive(false);
            return;
        }
        var area = GetComponent<RectTransform>();
        area.SetParent(description.transform.parent, false);
        area.anchorMin = new Vector2(0f, 1f); area.anchorMax = new Vector2(1f, 1f);
        area.pivot = new Vector2(.5f, 1f); area.sizeDelta = new Vector2(-56f, 191f);
        area.anchoredPosition = new Vector2(0f, -201f);
        fills = new UnityEngine.UI.Image[6];
        string[] names = { "ACCELERATION", "BRAKING", "CORNERING", "STABILITY", "ON-ROAD", "OFF-ROAD" };
        for (int i = 0; i < names.Length; i++)
        {
            float y = -i * 27f;
            Label(area, description, names[i], new Vector2(0f, y), new Vector2(164f, 28f));
            var track = Image(area, "Track", new Vector2(170f, y - 6f), new Vector2(255f, 16f), new Color(.15f, .27f, .28f, 1f));
            fills[i] = Image(track.rectTransform, "Fill", Vector2.zero, new Vector2(255f, 16f), new Color(.98f, .74f, .25f, 1f));
        }
        speedText = Label(area, description, "", new Vector2(0f, -162f), new Vector2(425f, 28f));
        description.gameObject.SetActive(false);
    }

    private static TMP_Text Label(RectTransform parent, TMP_Text source, string value, Vector2 position, Vector2 size)
    {
        var obj = new GameObject("CapabilityLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = source.font; text.fontSharedMaterial = source.fontSharedMaterial;
        text.text = value; text.fontSize = 20f; text.color = new Color(.88f, .93f, .91f, 1f);
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
            fills[i].rectTransform.sizeDelta = new Vector2(255f * values[i], 16f);
        speedText.text = topSpeed.HasValue ? $"TOP SPEED: {Mathf.RoundToInt(topSpeed.Value)} km/h" : "TOP SPEED: --";
    }
}
