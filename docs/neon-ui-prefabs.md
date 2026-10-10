# Neon UI prefabs

Open `client` with Unity **6000.3.24f1**, then open
`Assets/Scenes/NeonUIDemo.unity` to preview the designs.

- `Assets/Prefabs/CNNFilterPanel.prefab`: a world-space 3×3 CNN filter with
  TextMeshPro weights, opaque pale-blue cells, a cyan border, and a shallow 3D frame.
- `Assets/Prefabs/NeonNode.prefab`: a smooth sphere with a pale-blue body and
  turquoise rim. Its appearance is controlled by `NeonSphere.mat` and
  `Custom/NeonSphere`.
- The demo contains four linked node prefab instances under `NodeLayer`, arranged
  vertically with 2.1 world units between centers. No edges are included.

These are visual assets only; dragging, scanning, and graph logic are not implemented.
The demo camera enables SMAA High. Its dedicated `NeonUIDemoProfile` enables Bloom;
reuse equivalent URP camera/volume settings when previewing in another scene.

TextMeshPro essentials and their metadata are included so font and shader references
remain stable. Keep all `.meta` files with their assets. The bundled Liberation Sans
font license is under `Assets/TextMesh Pro/Fonts`.

Validation: the source project rendered both prefabs and the four-node arrangement;
the copied assets were checked for matching contents, complete dependencies, unique
GUIDs, and accompanying metadata. The cloned client has not been opened in a second
Unity Editor session.
