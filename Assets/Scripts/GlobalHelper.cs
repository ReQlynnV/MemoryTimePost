using UnityEngine;

public static class GlobalHelper
{
    public static string generateuniqueid(GameObject obj)
    {
        return $"{obj.scene.name}_{obj.transform.position.x}_{obj.transform.position.y}";
}
}
