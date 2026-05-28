# Liquid Bottle Pourer

A Unity VR component that simulates a liquid-filled bottle you can grab and tilt to pour. Includes a fake-liquid shader with real-time wobble, a pooled droplet system, and a stylized glass shader. Designed for Meta Quest with OpenXR.

---

## Requirements

| Dependency | Version |
|---|---|
| Unity | 6 (6000.0) or higher |
| Universal Render Pipeline | 17.0 or higher |
| XR Interaction Toolkit | 3.3 or higher |
| XR Hands | 1.7 or higher |
| OpenXR Plugin | 1.15 or higher |
| XR Plugin Management | 4.5 or higher |
| Meta OpenXR | 2.3 or higher |
| AR Foundation | 6.3 or higher |

---

## Installation

1. Open `Window → Package Manager` and install all dependencies listed above from the **Unity Registry**
2. Go to `Edit → Project Settings → XR Plug-in Management` and enable **OpenXR** for the Android platform
3. Under `Edit → Project Settings → XR Plug-in Management → OpenXR`, add **Meta Quest feature set** and any other interaction profiles you need (e.g. Meta Touch Controller Profile)
4. Import the `Liquid` folder into your project
5. Open the included demo scene at `Liquid/Scene/Demo`

---

## Quick Start

Drag the **Bottle** prefab into your scene. It comes pre-configured with all scripts, the liquid mesh, and the droplet pool. No further setup is required.

If you want to integrate the bottle into an existing XR rig, make sure your scene has an `XR Origin` with hand tracking or controller input set up — `BottleGrabbable` relies on `XRGrabInteractable` to detect when the player picks up the bottle.

---

## Scripts Overview

| Script | Description |
|---|---|
| `BottleGrabbable` | Wraps `XRGrabInteractable` to make the bottle graspable in VR; notifies `LiquidController` on grab and release |
| `LiquidController` | Drives the `FakeLiquid` shader — manages fill level, computes angular velocity from rotation deltas, and pushes wobble and bounds uniforms every frame |
| `LiquidPourer` | Emits droplets from the bottle spout based on tilt angle and remaining fill; implements a two-stage pour curve and pressure-based drain rate |
| `LiquidDropletPool` | Pre-instantiates a fixed pool of droplet GameObjects and hands them out on demand; avoids per-frame instantiation cost |
| `LiquidDroplet` | Controls a single droplet: billboard facing, Rigidbody physics, lifetime expiry, and collision-triggered stick-then-return-to-pool behaviour |

---

## Shaders Overview

| Shader | Description |
|---|---|
| `FakeLiquid` | Renders the liquid interior — clips geometry above the fill plane in object space so the surface stays flat regardless of bottle orientation; adds sine-wave wobble, a surface highlight band, and a Fresnel rim |
| `LiquidDroplet` | Billboard shader for individual falling droplets — procedurally draws a soft circle from UVs with a configurable squish factor for a teardrop silhouette |
| `StylizedGlass` | Bottle exterior shader — front faces use Fresnel transparency, Blinn-Phong specular, and subtle environment reflections; back faces render a darkened interior tint to simulate glass thickness |

---

## Component Setup

### LiquidController

Attach to the same GameObject as the liquid mesh `MeshRenderer`.

| Property | Description |
|---|---|
| `Liquid Renderer` | The `MeshRenderer` that uses the `FakeLiquid` material |
| `Fill Amount` | Initial fill level (0 = empty, 1 = full) |
| `Max Wobble` | Maximum wobble amplitude in degrees |
| `Wobble Speed` | How fast the wobble oscillates |
| `Recovery Speed` | How quickly wobble dampens back to zero |

### LiquidPourer

Attach to the bottle GameObject. Requires a `LiquidController` and a `LiquidDropletPool` on the same object.

| Property | Description |
|---|---|
| `Spout Transform` | Empty transform placed at the bottle mouth — droplets spawn here |
| `Pour Angle Start` | Tilt angle (°) at which pouring begins |
| `Pour Angle Full` | Tilt angle (°) at which pour strength reaches 1.0 |
| `Max Drop Rate` | Maximum droplets per second at full pour strength |
| `Drop Lifetime` | How long each droplet lives before returning to the pool |
| `Drop Speed` | Downward velocity applied to each new droplet |
| `Drop Spread` | Random position and velocity spread radius at the spout |
| `Fill Pressure Exp` | Exponent for fill-pressure scaling (2 = quadratic drain) |
| `Invert Boost` | Extra pour strength added when the bottle is fully inverted |

### LiquidDropletPool

| Property | Description |
|---|---|
| `Droplet Prefab` | The **DropletPrefab** prefab from `Liquid/Prefabs/` |
| `Pool Size` | Number of droplets pre-instantiated at startup. Rule of thumb: `maxDropRate × dropLifetime` (default 300 = 120 drops/s × 2.5 s) |

> **Required — Liquid layer:** The **DropletPrefab** must be assigned to a layer named exactly `Liquid`. This layer is used to make all droplets ignore collisions with each other (via `Physics.IgnoreLayerCollision`) while still colliding with the environment.
>
> To add the layer: go to `Edit → Project Settings → Tags and Layers`, expand **Layers**, and type `Liquid` into any free User Layer slot. Then open the **DropletPrefab** prefab and set its Layer to `Liquid`. If the layer is missing, droplets will collide with one another and cluster unnaturally.

### BottleGrabbable

Attach to the bottle root. The component auto-adds `Rigidbody` and `XRGrabInteractable` if they are missing.

| Property | Description |
|---|---|
| `Movement Type` | `VelocityTracking` for physics-driven feel; `Kinematic` for precise hand-locked movement |
| `Throw On Release` | Inherit controller velocity on release for natural throwing |

---

## Build Configuration

### Android (Meta Quest)

1. Switch platform to **Android** in `File → Build Settings`
2. In `Edit → Project Settings → Player → Android → Other Settings`:
   - Set **Minimum API Level** to Android 10 (API 29) or higher
   - Set **Target API Level** to the latest available
   - Enable **Auto Graphics API** or manually set it to **Vulkan**
3. In `Edit → Project Settings → XR Plug-in Management → OpenXR (Android)`:
   - Enable **Meta Quest feature set**
   - Add **Meta Touch Controller Profile** and/or **Hand Interaction Profile**
4. Connect your Quest via USB, enable developer mode on the headset, and use `File → Build And Run`

---

## Troubleshooting

**Liquid surface clips through the bottle mesh**
The `FakeLiquid` shader clips in object space. Make sure the bottle mesh and the liquid mesh share the same parent and are not scaled non-uniformly — non-uniform scale corrupts the bounds calculation in `LiquidController.Start()`.

**No droplets appear when tilting**
Check that `LiquidPourer` has a valid `Spout Transform` assigned and that `LiquidDropletPool` has been assigned a `Droplet Prefab`. Also confirm `Fill Amount` is greater than 0 — an empty bottle does not pour.

**Bottle cannot be grabbed**
Ensure the scene contains an `XR Origin` with an `XRInteractionManager` and that the bottle layer is not excluded from the interaction manager's layer mask.

**Droplets fall through geometry**
`LiquidDroplet` uses a `SphereCollider`. Make sure floor and surface colliders exist in the scene and are on a layer that is not ignored by the Physics matrix.

**Wobble never damps out**
If `Recovery Speed` is set too low the wobble integral will not converge. Increase it or reduce `Max Wobble` in the `LiquidController` Inspector.

**Shaders appear pink in the Editor**
The shaders require **Universal Render Pipeline**. Confirm your project's `Graphics Settings` reference a URP Asset and that no Built-in RP camera override is active in the scene.
