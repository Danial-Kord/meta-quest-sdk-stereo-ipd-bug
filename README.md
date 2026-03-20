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

## Scenes Overview

| Scene | Purpose |
|-------|---------|
| **`SampleScene`** | **Fusable gun sights** — three stereo modes (per-eye default, non–per-eye, per-eye + override). Two sights; measure effective IPD via fusion. |
| **`HeadCenterExperiment`** | **Head-center alignment** — monocular phases with a single movable sight and a red screen-center reticle; records center-eye pose before/after head movement and shows translation/angle delta. **Per-eye modes only** (buggy vs fixed override). |

Open either scene, build to Quest, and use the on-screen UI for controls.

---

## Practical Test Scene: Fusable Gun Sights (`SampleScene`)

To verify the bug and the fix in-headset, this repo includes a **practical test scene** where you can measure the *effective* interocular distance (IPD) that the headset is actually rendering.

### What the scene does

- **Two gun sights** (reticles) are shown, one per eye.
- You can **control their horizontal separation** with the **left and right controller joysticks**:
  - **Left joystick** → moves the left sight horizontally.
  - **Right joystick** → moves the right sight horizontally.
- Your task: adjust the separation until your eyes **fuse the two images into one** (stereoscopic fusion). The separation at which you can comfortably fuse them corresponds to the **effective IPD** that the VR pipeline is using for your eyes.
- Optionally, you can also change the **distance from the eye in Z** (depth); the default placement is already suitable for testing the issue.
- **Controller input** is handled by `InterOccularPositionController`; UI by `InterOccularDebugUI`. You can switch **three** stereo modes (stick up/down when the mode panel is visible): per-eye default (buggy), non–per-eye, and per-eye + `CustomIPDOverride`.

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
  
[Link to the tested Scnee for 3 conditions](https://drive.google.com/file/d/1_92fCyhGPXMI1owvRM1taFkhMi8fQqBY/view?usp=sharing)


### Interpretation

- The **12 cm vs 6 cm** fusion distance shows that, in default per-eye mode, the pipeline is effectively using **twice** the intended IPD.  
- Putting the per-eye cameras at **zero separation** (so only the internal pipeline applies IPD) restores the correct effective IPD and matches the non–per-eye and real-world behaviour.  
- The gun-sight test therefore gives a **concrete, user-measurable** way to reproduce and validate the bug and the fix.

---

## Head-Center Experiment (`HeadCenterExperiment`)

A second scene runs a **monocular alignment workflow** on top of the same rig, IPD override, and gun-sight geometry (one visible sight; the second is hidden).

### Goal

Align a **single gun sight** with a **red reticle** placed at the optical center of each eye in turn, then compare **two saved center-eye poses** (after you move your head in the real world) to quantify how much the head had to move between left-eye alignment and right-eye alignment under each stereo configuration.

### Stereo modes (this scene only)

Only the **per-eye** configurations are exposed:

1. **Per-Eye Default (buggy)** — SDK default per-eye camera offsets.  
2. **Per-Eye + Override (fixed)** — `CustomIPDOverride` enabled with IPD proportion **0** (cameras at center), same idea as the fusion test’s fix.

Non–per-eye camera mode is **not** selectable here; alignment phases always use **separate eye cameras** so one eye can be cleared to **solid black**.

### Flow

1. **Idle** — UI visible: pick mode (**stick ↑↓** or keyboard **1** / **2**). **Thumbstick click** (L or R) or **Enter** starts the run. **Tab** or **Menu (Start)** toggles UI visibility.  
2. **Left eye only** — Right eye is black. The movable sight **snaps to the left eye’s world transform** at start (not parented). Align the sight to the **red dot** with sticks; **B/Y** toggles horizontal vs depth; **A / X** saves **pose 1** (center-eye position & rotation).  
3. **Right eye only** — Left eye black; **sight is locked**. Move your **head** until the dot and sight align; **A / X** saves **pose 2**.  
4. **Result** — Both eyes on; the chosen per-eye mode is applied. UI shows **pose delta**: distance (m), angle (°), and Δposition (world).

**R** resets to idle. Legacy `InterOccularPositionController` / `InterOccularDebugUI` on the TestKit prefab are **disabled** in this scene; use **`HeadCenterExperimentController`** and **`HeadCenterExperimentUI`** on the camera rig instead.

### Scripts (head-center)

| Script | Role |
|--------|------|
| `Assets/Scripts/InterOccularDebug/HeadCenterExperimentController.cs` | Phases, mono camera blackouts, pose capture, mode apply (`usePerEyeCameras` always on for results in this scene), sight snap to left eye at run start, reticle parenting to active eye anchor. |
| `Assets/Scripts/InterOccularDebug/HeadCenterExperimentUI.cs` | World-space TMP panel: phase, two modes, metrics, control legend. |

Shared with `SampleScene`: `InterOccularSightAdjuster`, `CustomIPDOverride`, `OVRCameraRig`.

## Contents of This Repo

- **Scenes**
  - `Assets/Scenes/SampleScene.unity` — fusion / dual-sight tool (three stereo modes).
  - `Assets/Scenes/HeadCenterExperiment.unity` — head-center monocular experiment (two per-eye modes); listed in **File → Build Settings** for builds.
- **Core**
  - **`CustomIPDOverride`** (`Assets/Scripts/CustomIPDOverride.cs`) — hooks `OVRCameraRig.UpdatedAnchors`, `LateUpdate`, and `Application.onBeforeRender`; can force left/right eye anchors from center + IPD proportion; used to reproduce the “cameras at center” fix.
- **Inter-ocular debug** (`Assets/Scripts/InterOccularDebug/`)
  - `InterOccularSightAdjuster` — moves one or two sight transforms (horizontal + shared depth).
  - `InterOccularPositionController` + `InterOccularDebugUI` — input and UI for **`SampleScene`** (A/X toggles sights/UI; stick cycling for three modes).
  - `HeadCenterExperimentController` + `HeadCenterExperimentUI` — **`HeadCenterExperiment`** only.
- **Prefab / kit** — TestKit with gun-sight geometry; references wired in scenes.

## How to Reproduce

1. Clone this repo and open it in Unity.
2. Install/confirm Meta XR SDK (Meta XR All-in-One or equivalent).
3. Open **`SampleScene`** (gun-sight fusion) or **`HeadCenterExperiment`** (head-center workflow) with `OVRCameraRig`.
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

- Meta Developer Forums thread: [Link](https://communityforums.atmeta.com/discussions/Questions_Discussions/ovrcamerarig-per-eye-camera-mode-appears-to-double-effective-ipd-world-scale-dis/1369800)
- If you find issues with this repro or have additional data, please open an issue or PR.

## License

[MIT](LICENSE)
