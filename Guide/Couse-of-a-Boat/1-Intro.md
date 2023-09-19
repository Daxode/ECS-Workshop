## Prerequisites
- GameObject, MonoBehaviour knowledge
- C# confidence
- Eager to learn
- C# IDE - VS2022 or Rider2023

(**Hint:** If Visual Studio or VSCode is not Auto-Completing, goto `Window>Package Manager` and install the Visual Studio extension. Remember to set `Edit>Preferences>External Editors` to your IDE of choice.)

## Check Setup
- **Unity 2023.3.6f1** installed
- Cloned Repository Locally
- Tried opening `/Course-of-a-Boat`

## What is it?
<p align="center">
    <img align="center"width="200" src="Resources/TechStack.png" alt="Tech stack Visual">
</p>

Workshop focus:
<p align="center">
    <img align="center"width="200" src="Resources/TechStackHighlightECS.png" alt="Tech stack Visual for Entities">
</p>

Might touch on:
<p align="center">
    <img align="center"width="200" src="Resources/TechStackHighlightECSExtra.png" alt="Tech stack Visual for Entities, Burst and Mathematics">
</p>

## Mental Model

GO:
<p align="center">
    <img align="center"width="200" src="Resources/MentalModelGO.png" alt="Tech stack Visual">
</p>

ECS:
<p align="center">
    <img align="center"width="200" src="Resources/MentalModelECS.png" alt="Tech stack Visual">
</p>


## Agenda
- Mental Model and Workflow
- Getting Started
- The ECS Dictionary
- Make an Entity (+task)
- Authoring vs Runtime (+task)
- Instantiate Prefab (+task)
- Understanding Physics (+task)
- Move the Mark (+task)

## Getting Started
For new projects:
- Install `com.unity.entities` to get started.
- If you want to draw entities, use `com.unity.entities.graphics` to draw entitites using URP or HDRP.

Today, the project is already set up. We'll walk through that. But first, let's find the editor tooling for entities. Open `Window > Entities > X` a good one to have ready is `Systems` and `Hiarchry`.

## Workflow
- What feature? What data?
- Kitbash your design
- Make it work

## The ECS Dictionary
- ***E*ntity:** A key to data in the world
- ***C*omponent:** The data
- ***S*ystem:** Steers logic in the world
- **World:** Runs systems & stores data
- **Archetype:** A set of unique components
- **EntityQuery:** Filter world by Archetype
- **Iteration:** Efficient loop over EntityQuery
- **Structural Change:** Change Layout in World

[Next Chapter ->](2-Make-An-Entity.md)