# HAON SD Series Free Bundle — Byte visual

Motor City can optionally use **Haon SD series Free Bundle** as the visual for the Byte companion.

- Unity Asset Store package: 84992
- Publisher: Haon
- Store page: https://assetstore.unity.com/packages/3d/characters/humanoids/haon-sd-series-free-bundle-84992
- Asset Store license: Standard Unity Asset Store EULA
- The package is an SD Unity-chan derivative and also references the Unity-chan License.
- The third-party package itself is **not committed to this repository** by this integration.
- Import it through Unity Package Manager / My Assets using the project's licensed Unity account.

After import, use:

`Motor City > Byte > Rebuild From HAON SD Bundle`

The editor integration searches the imported HAON assets, prepares a runtime prefab at:

`Assets/Resources/MotorCity/Byte/HaonByteVisual.prefab`

If that prefab is unavailable, Motor City keeps the existing primitive Byte visual as a fallback.
