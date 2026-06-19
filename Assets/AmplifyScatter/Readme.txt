About

  Amplify Scatter FREE (c) Amplify Creations, Lda. All rights reserved.

  Amplify Scatter is a prefab scattering tool for Unity.

    https://u3d.as/3ZJi?aid=1011lPwI&pubref=Editor

Description

 Amplify Scatter is an editor tool that procedurally places prefabs across a
 user-defined region using fast Poisson-disc sampling. Drag prefabs in, set a
 Spawn Volume, and generate natural, evenly-spaced distributions in seconds.
 Ideal for vegetation, rocks, debris, props and general level dressing.

Features

  * Drag-and-drop prefab list with per-prefab priority weighting
  * Poisson-disc sampling for even, natural placement
  * Drag & drop prefab list with per-prefab customizable spawn priority
  * Interactive Spawn Volume gizmo; move and spawn
  * Slope filtering (flat, cliff, or anywhere in between)
  * Align-to-normal with optional tilt variation
  * Random Y rotation range with optional variation
  * Uniform scale randomization
  * Basic overlap avoidance (colliders + renderer bounds)
  * Random or fixed seed generation
  * Spawn parent grouping in hierarchy; w/delete spawned objects option

Supported Platforms

  * Editor tool - Windows, macOS, Linux editors
  * Compatible with Built-in, URP and HDRP render pipelines

Minimum Requirements

  Software

    Unity 6.x
    Unity 2022.2+

Quick Guide

  1) Open the tool in the main menu under Window > Amplify Scatter.
  2) Drag one or more prefabs into the "Drop Prefabs Here!" zone, or use the
     (+) button on the list. Set each prefab's Priority (lower = higher spawn
     chance; 0 is most likely).
  3) Position and size the Spawn Volume:
     - Click the orange spawner icon in the Scene view to select it.
     - W moves the center, R scales the size.
     - Press F to frame the volume in the Scene view.
     - Or edit Spawn Volume Center / Size directly in the window.
  4) Set Poisson Radius to control the minimum distance between spawns.
  5) Under Placement, configure:
     - Spawn Parent: drag a Transform to keep results organized.
     - Raycast Mask: layers the placement raycast can hit.
     - Avoid Overlap: skip placements that overlap existing geometry.
     - Slope Angle Range: only spawn where the slope is in range.
     - Scale Range: random uniform scale per spawn.
     - Align To Normal: tilt to match surface, with optional random tilt.
     - Random Y Rotation: random spin around the vertical axis.
  6) Click Spawn. Use Delete Spawned Prefabs to remove only what this tool
     made. Reset Saved Settings wipes persisted state.

Tips

  * Use Random Seed to get a different layout each click; turn it off and edit
    the Seed field to reproduce the same layout later.
  * The Spawn Volume can be hidden or shaded from the toggles
    above Reset Saved Settings.
  * The Raycast Mask is the cleanest way to exclude meshes you don't want to
    spawn on top of - put them on an excluded layer.


Feedback

  To file error reports, questions or suggestions, you may use
  our feedback form online:

    http://amplify.pt/contact

  Or contact us directly:

    For general inquiries - info@amplify.pt
    For technical support - support@amplify.pt (customers only)
