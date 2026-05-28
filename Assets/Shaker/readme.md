# Shaker

A Unity VR package that simulates a bar shaker — supports ingredient input, shake detection, recipe matching and liquid output. Includes ready-to-use prefabs and ScriptableObject-based configuration.

---

## Requirements

| Dependency | Version |
|---|---|
| Unity | 2022.3 LTS or higher |
| XR Interaction Toolkit | 2.0 or higher |
| XR Plugin Management | 4.0 or higher |

---

## Installation

1. Open `Window → Package Manager` and install all dependencies listed above from the **Unity Registry**
2. Import the `Shaker` package into your project
3. All assets are located under `Assets/Shaker`

### Folder Structure

```
Assets/
└── Shaker/
    ├── Code/         # All C# scripts
    ├── Liquids/      # LiquidSO ScriptableObject assets
    ├── Prefabs/      # Shaker Object and Liquid prefabs
    └── Recipes/      # ShakerRecipeSO ScriptableObject assets
```

---

## Quick Start

### Setting Up the Shaker in the Scene

1. Drag the **Shaker Object** prefab from `Assets/Shaker/Prefabs/` into your scene
   - This prefab contains both the shaker body and the lid, with all internal references pre-assigned (colliders, anchor points, pour point, `ShakerLid`, `ShakerTopCollider`)
2. Drag a **Liquid** prefab from `Assets/Shaker/Prefabs/` into your scene for each ingredient you want to place
3. Select the Liquid GameObject and assign the desired **LiquidSO** asset to the `Liquid Data` field in its `ShakerInput` component — the object will automatically take on the color of the assigned liquid
4. The Liquid is now ready to be picked up and dropped into the shaker

---

## Creating Liquids

Liquids are defined as ScriptableObjects and drive both the visual appearance and the recipe system.

1. In the Project window, **right-click** on `Assets/Shaker/Liquids/`
2. Select `Create → Shaker → Liquid`
3. Name the new asset and configure its properties in the Inspector:

| Field | Description |
|---|---|
| `Liquid Name` | Display name of the liquid |
| `Description` | Optional description |
| `Liquid Color` | Color applied to the material of any object carrying this liquid |
| `Icon` | Optional sprite for use in UI |

---

## Creating Recipes

Recipes define what the shaker produces given a set of ingredients and a mix level.

1. In the Project window, **right-click** on `Assets/Shaker/Recipes/`
2. Select `Create → Shaker → Recipe`
3. Name the new asset and configure it in the Inspector:

| Field | Description |
|---|---|
| `Ingredients` | Array of `LiquidSO` assets that make up this recipe |
| `Required Mix Level` | Minimum shake level needed to produce the result |
| `Result` | The `LiquidSO` that will be output when the recipe matches |

4. Select the **Shaker Object** in your scene and locate the `Shaker` component
5. Add the newly created recipe to the **All Recipes** array

> Ingredient order in the array does not matter — the system checks by content only.

---

## Scripts Overview

| Script | Description |
|---|---|
| `Shaker` | Main controller — manages ingredients, shake detection, recipe resolution and output |
| `ShakerLid` | Handles lid snap, detach and XR grab integration |
| `ShakerInput` | Attached to physical objects; carries a `LiquidSO` reference and applies its color on init |
| `ShakerTopCollider` | Trigger zone on the shaker's opening that absorbs incoming ingredient objects |
| `LiquidSO` | ScriptableObject defining a liquid's name, color and icon |
| `ShakerRecipeSO` | ScriptableObject defining a recipe's ingredients, required mix level and result |

---

## Mix Levels

The shake meter fills from 0 to 100 while the shaker is grabbed and the lid is on. The current level is determined as follows:

| Level | Range | Enum Value |
|---|---|---|
| Unmixed | 0 – 19% | `MixLevel.Unmixed` |
| Slightly Mixed | 20 – 39% | `MixLevel.SlightlyMixed` |
| Mixed | 40 – 59% | `MixLevel.Mixed` |
| Well Mixed | 60 – 79% | `MixLevel.WellMixed` |
| Fully Mixed | 80 – 100% | `MixLevel.FullyMixed` |

The shaker only accumulates shake progress while it is **grabbed** and the **lid is attached**.

---

## Shaker Behaviour Reference

| Condition | Result |
|---|---|
| Lid off + shaker tilted past threshold | Contents are poured out |
| Lid never placed + poured | Generic liquid is returned |
| Lid placed + shaken + poured | Recipe is matched; result liquid is spawned |
| No recipe matches | Generic liquid is spawned |
| Lid on + ingredient dropped in opening | Ingredient is rejected |

---

## Troubleshooting

**Ingredients are not absorbed by the shaker**
Ensure the Liquid prefab has a `ShakerInput` component with a valid `LiquidSO` assigned, and that the lid is not attached when dropping the ingredient.

**Lid does not snap**
Check that `Target Shaker` and `Lid Anchor` are assigned on the `ShakerLid` component. The lid must come within `Snap Distance` of the anchor while not being held.

**Shake meter does not increase**
The shaker must be actively grabbed via XR and the lid must be attached. Verify that the `XRGrabInteractable` component is present on the Shaker Object.

**No output spawned after pouring**
Ensure `Output Prefab` and `Generic Liquid` are assigned in the `Shaker` component, and that the shaker has been tilted past the `Pour Angle Threshold`.

**Using cubes instead of actual liquid**
The component is only for the shaking feature, so the liquids are cubes in the current version.