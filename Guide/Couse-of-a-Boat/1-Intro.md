## Prerequisites
- GameObject, MonoBehaviour knowledge
- C# confidence
- Eager to learn
- C# IDE - VS2022 or Rider2023

## Check Setup
- **Unity 2023.3.6f1** installed
- Cloned Repository Locally
- Tried opening `/Course-of-a-Boat`

## What is it?
<p align="center">
    <img align="center"width="200" src="Resources/TechStack.png" alt="Tech stack Visual">
</p>

<p align="center">
    <img align="center"width="200" src="Resources/TechStackHighlightECS.png" alt="Tech stack Visual for Entities">
</p>

<p align="center">
    <img align="center"width="200" src="Resources/TechStackHighlightECSExtra.png" alt="Tech stack Visual for Entities, Burst and Mathematics">
</p>

## Mental Model

<p align="center">
    <img align="center"width="200" src="Resources/MentalModelGO.png" alt="Tech stack Visual">
</p>

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

```cs
void Test(){}
```

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