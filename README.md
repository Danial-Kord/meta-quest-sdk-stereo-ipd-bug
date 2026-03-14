# meta-quest-sdk-stereo-ipd-bug
# Meta Quest SDK — Per-Eye Camera Double IPD Bug (Reproduction)

A minimal Unity project to reproduce a bug in the Meta XR SDK where **interpupillary distance (IPD) is applied twice** in per-eye camera mode, causing incorrect stereo separation and distorted world scale.

## The Bug

When using `OVRCameraRig` with per-eye cameras, the SDK:

1. **Positions** the left and right eye anchors at ±IPD/2 from the center eye (correct).
2. **And** sets `Camera.stereoSeparation` to IPD/2 on each eye camera.

Unity uses `stereoSeparation` to build stereo view matrices, adding another ±IPD/2 offset. The result is **2× IPD** effective separation instead of 1× IPD.

### Consequences

- Perceived world scale is roughly **half** of intended.
- Distances appear compressed.
- Critical for research (e.g. perception, psychophysics) and affects general comfort.

## Requirements

- Unity 2021.3+ (or version you used)
- Meta XR SDK (Meta XR All-in-One or equivalent)
- Meta Quest headset (or Link for testing)

## How to Reproduce

1. Clone this repo and open the project in Unity.
2. Ensure the Meta XR SDK is installed and the scene uses `OVRCameraRig` with per-eye cameras enabled.
3. Enter Play mode and build/run on Quest (or use Link).
4. Observe: world scale and distances appear too small.
5. (Optional) Enable the included **CustomIPDOverride** component and set it to correct the separation; scale and distance should look correct.

## What This Repo Contains

- **Minimal scene** with OVRCameraRig and basic environment.
- **CustomIPDOverride script** — workaround that forces correct IPD/stereo separation in `LateUpdate` and `onBeforeRender`.
- **Instructions** to verify the bug (e.g. logging anchor positions and `stereoSeparation` at runtime).

## Suggested Fix (for Meta SDK)

Apply IPD via **one** mechanism only:

- **Option A:** Position eye anchors at ±IPD/2 and set `Camera.stereoSeparation = 0`.
- **Option B:** Keep anchors at center and set `stereoSeparation = IPD/2`.

Not both.

## Reporting

- **Meta Developer Forums:** [link to your forum post]
- **Issue / discussion:** Open an issue in this repo for reproduction problems or questions.

