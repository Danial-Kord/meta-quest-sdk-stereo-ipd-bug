# meta-quest-sdk-stereo-ipd-bug

# Meta Quest SDK — Per-Eye Camera IPD / World Scale Bug (Reproduction)

A minimal Unity project to reproduce a bug in the Meta XR SDK where **interpupillary distance (IPD) is effectively applied twice** in per-eye camera mode, causing incorrect stereo separation and distorted world scale.

## The Core Finding

When using `OVRCameraRig` with per-eye cameras:

- If the **left and right cameras are offset horizontally** (e.g. ±IPD/2 from the center eye), the world appears **too small** and distances appear **compressed**.
- If the **left and right cameras are moved to the same position** (0 distance between them, at the center), the world scale and distances immediately look **correct**.

This still happens even when **`Camera.stereoSeparation` is set to 0**.

That means:
- The distortion is **not only** coming from `stereoSeparation`.
- There is an additional IPD-based offset applied somewhere in the stack (view matrices / native eye poses / compositor).
- When we also offset the per-eye camera transforms, IPD is effectively applied **twice**.

## Intuitive Summary

- Some part of the rendering pipeline (likely the native eye pose / compositor) already handles IPD.
- Per-eye camera mode **also** moves the left/right cameras apart in Unity space.
- Together, this leads to **double IPD** → exaggerated disparity → the whole virtual world looks **about half size**, and distances feel shorter than they should.

## Why Zero Distance Fixes It

- When left and right cameras are **0 distance apart** (both at the center eye position), only the internal IPD application remains.
- That yields the correct 1× IPD and the world appears at the correct scale again.

This is a strong indication that for per-eye cameras, the correct design is:

- **Either**: Keep both eye cameras at center and let the internal pipeline apply IPD.  
- **Or**: Offset the cameras in Unity and ensure **no extra IPD** is applied in the native/view layer.

But doing **both** (offset transforms + internal IPD) leads to the observed distortion.

## Practical Test Scene: Fusable Gun Sights

To verify the bug and the fix in-headset, this repo includes a **practical test scene** where you can measure the *effective* interocular distance (IPD) that the headset is actually rendering.

### What the scene does

- **Two gun sights** (reticles) are shown, one per eye.
- You can **control their horizontal separation** with the **left and right controller joysticks**:
  - **Left joystick** → moves the left sight horizontally.
  - **Right joystick** → moves the right sight horizontally.
- Your task: adjust the separation until your eyes **fuse the two images into one** (stereoscopic fusion). The separation at which you can comfortably fuse them corresponds to the **effective IPD** that the VR pipeline is using for your eyes.
- Optionally, you can also change the **distance from the eye in Z** (depth); the default placement is already suitable for testing the issue.

### Observations (in-headset)

**1. Per-eye mode, default SDK (bug present)**  
- To fuse the two sights, they had to be about **12 cm** apart.  
- That implies the *effective* interocular distance in VR is ~12 cm — roughly **double** a typical real-world IPD (~6 cm).  
- As a result, the whole world appears **smaller**, and **movement gain does not match the real world**.

**2. Non–per-eye mode (e.g. single center camera / stereo handled elsewhere)**  
- Scale and distances look **consistent with the real world**.  
- With the same tool, fusion is possible when the sights are about **6 cm** apart — matching real-world interocular distance.

**3. Per-eye mode + Custom IPD Override (cameras at center)**  
- Left and right cameras are overridden to **zero horizontal offset** (same position as center).  
- Scale and distances appear **correct**; stereoscopic depth is still clearly visible.  
- With the gun-sight tool, fusion is again possible at about **6 cm** — consistent with real-world IPD and confirming that the override fixes the effective IPD.

### Interpretation

- The **12 cm vs 6 cm** fusion distance shows that, in default per-eye mode, the pipeline is effectively using **twice** the intended IPD.  
- Putting the per-eye cameras at **zero separation** (so only the internal pipeline applies IPD) restores the correct effective IPD and matches the non–per-eye and real-world behaviour.  
- The gun-sight test therefore gives a **concrete, user-measurable** way to reproduce and validate the bug and the fix.

## Contents of This Repo

- **Minimal scene** with `OVRCameraRig` configured for per-eye cameras.
- A **`CustomIPDOverride` script** (under `Scripts/InterOccularDebug/`) that:
  - Hooks into `OVRCameraRig.UpdatedAnchors`, `LateUpdate`, and `Application.onBeforeRender`.
  - Lets you:
    - Scale or override the IPD effect.
    - Force left/right camera positions.
    - Control or zero `Camera.stereoSeparation`.
- **Practical test scene** with two fusable gun sights and joystick-controlled horizontal separation (and optional Z distance) to measure effective IPD in-headset.
- Simple objects in the scene to make the scale / distance distortion visible.

## How to Reproduce

1. Clone this repo and open it in Unity.
2. Install/confirm Meta XR SDK (Meta XR All-in-One or equivalent).
3. Open the provided sample scene with `OVRCameraRig`.
4. Build and run on a Meta Quest device (or use Link / Play mode with HMD).
5. Compare two cases:

   **Case A — Default per-eye offsets (buggy):**
   - Left and right cameras are offset horizontally.
   - Observe:
     - Objects feel slightly **too small**.
     - Distances feel **shorter** than they should.
   - Use the **gun-sight test**: adjust separation until you fuse; expect ~12 cm (double your real IPD).

   **Case B — Cameras forced to 0 distance (correct):**
   - Enable the provided `CustomIPDOverride` behavior that:
     - Forces left/right cameras to have 0 horizontal separation (same position as center).
   - Observe:
     - World scale returns to **normal**.
     - Distances feel more **veridical** (closer to reality).
   - Use the **gun-sight test**: fuse at ~6 cm (matches real-world IPD).

6. Optionally, log internal values:
   - `OVRPlugin.ipd`
   - `leftEyeAnchor.localPosition`
   - `rightEyeAnchor.localPosition`
   - `Camera.stereoSeparation`

   And confirm:
   - The scale error persists even when `stereoSeparation = 0`.
   - The scale error disappears when cameras are no longer separated in Unity space.

## Suggested Fix for Meta SDK

For per-eye camera mode:

- Choose **one** source of IPD, not two.

### Option 1 — Internal IPD Only

- Keep left/right cameras at the **center** (no horizontal offset in Unity).
- Allow the existing native/view/compositor pipeline to handle IPD.
- `Camera.stereoSeparation` may be 0 or unused for per-eye cameras in this mode.

### Option 2 — Unity Transform Only

- Offset left/right anchors to ±IPD/2 in Unity.
- Ensure **no additional IPD-based offset** is applied in:
  - Native eye poses
  - View matrices
  - Compositor steps
- Set `Camera.stereoSeparation = 0` for per-eye cameras so Unity doesn’t add its own offset on top of the transform.

Currently, per-eye camera mode appears to double-count the separation: one via transforms, one via internal IPD. That’s what this repo is meant to demonstrate.

## Why This Matters

- For **research / experiments** (e.g. distance perception, size constancy), correct metric scale is critical.
- For general users, incorrect stereo geometry can:
  - Break presence.
  - Make objects feel “toy-sized”.
  - Contribute to discomfort.

This repo is intended as a transparent, minimal reproduction to help Meta engineers and other developers see and reason about the issue.

## Tested Environment

| Item | Version / Details |
|------|-------------------|
| **Unity** | 6000.3.2f1 LTS |
| **Meta XR SDK** | 85.0 |
| **Device** | Meta Quest 3 |
| **Connection** | Meta Horizon Link |

## Reporting / Discussion

- Meta Developer Forums thread: [To Be Added]()
- If you find issues with this repro or have additional data, please open an issue or PR.

## License

[MIT](LICENSE)
