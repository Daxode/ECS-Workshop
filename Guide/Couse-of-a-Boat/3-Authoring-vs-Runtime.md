## Presentation.
### Updated ECS Dictionary
- ***E*ntity:** A key to data in the world
- ***C*omponent:** The data
- ***S*ystem:** Steers logic in the world
- **World:** Runs systems & stores data
- **Archetype:** A set of unique components
- **EntityQuery:** Filter world by Archetype
- **Iteration:** Efficient loop over EntityQuery
- **Structural Change:** Change Layout in World
- **Baking:** Serialize GO+MBs as Entity

Creating an Entity at Runtime can be moved to the Authoring process by using a baker. This means when a scene is put on disk it will run the baker, and request creation of an entity from a GO. 

In Runtime:
- `EntityManager.CreateEntity`
- `EntityManager.AddComponentData`
- `EntityManager.AddBuffer` 

In Baking: 
- `this.GetEntity`
- `this.AddComponent`
- `this.AddBuffer`

`GetEntity` will request the gameobject to become an entity, with a specfic transform. See, the Transform System is split into two components. `LocalTransform` and `LocalToWorld`. 

`LocalTransform` is your transform relative to your parent. This is what you should use in simulation to move around your Entity. E.g. the Physics package will move this transform around, so think of it like `UnityEngine.Transform`. 

`LocalToWorld` is for rendering. If `LocalTransform` is present it's calculated from that.

So, use `TransformUsageFlags` to request the kind of transform usage your feature is requiring. If your features is a damage system, `TUF.None` is best, as that feature will likely not require a  `LocalTransform`. But a floating boat system, would move it so `TUF.Dynamic` can be used here. (Strive to always pick the least restrictive, so optimizations can occur).

Now that you have an Entity from `GetEntity` you can add what you want to it.
So from:
![](Resources/A-CodeP3.png)
To This:
![](Resources/B-CodeP1.png)

### Summary
- **SubScenes** only stores Runtime Data 
- **SubScenes** request GameObjects to Bake
- Baking turns a GO into an **Entity**
- Inherit `Baker<T>` defines MB -> XYZ components
- `GetEntity` is your friend

---------

## Task
- No longer add the two ‘fake’ `MoveUpDown` entities.
- Without adding any new components to the prefab **Boat**, make it move up and down.

### Expect Result
![Expected Result showing boat going up and down](Resources/TaskBExpectedResult.gif)
### Bonus Task
Make `MoveUpAndDownAuthor.addGameObjectYToOffset` affect baked result.

Ask for help if you're stuck, not gonna learn much if you just open the answer :P When done, you can use it to validate your solution. [Task B Solution](https://github.com/Daxode/ECS-Workshop/commit/b9b91d09fbed8866bf4c074252d1092c34415733).

-----
[<- Previous Chapter](2-Make-An-Entity.md) | [Next Chapter ->](4-Instantiate-Prefab.md)