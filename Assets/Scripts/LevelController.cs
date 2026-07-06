using UnityEngine;

// Drag any objects into the list, then call HideObjects / ShowObjects
// from a Button: OnClick -> + -> drag this object in -> LevelController -> HideObjects.
public class LevelController : MonoBehaviour
{
    [Tooltip("Objects that the buttons will turn off or on.")]
    [SerializeField] private GameObject[] objects;

    // Hook this to a Button OnClick to turn everything in the list OFF.
    public void HideObjects()
    {
        SetAll(false);
    }

    // Hook this to a Button OnClick to turn everything in the list ON.
    public void ShowObjects()
    {
        SetAll(true);
    }

    // Flips each object: on becomes off, off becomes on.
    public void ToggleObjects()
    {
        foreach (GameObject go in objects)
        {
            if (go != null) go.SetActive(!go.activeSelf);
        }
    }

    // Turn one extra object off from a button without adding it to the list.
    public void HideSingle(GameObject target)
    {
        if (target != null) target.SetActive(false);
    }

    private void SetAll(bool state)
    {
        foreach (GameObject go in objects)
        {
            if (go != null) go.SetActive(state);
        }
    }
}
