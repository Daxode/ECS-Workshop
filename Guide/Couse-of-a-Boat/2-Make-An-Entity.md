## Presented

### Summary
- [Entities Documentation][EntitiesDocs] is your friend
- Game code in **Systems** with `ISystem`
- `EntityManager` makes changes to world
- World can store **components** and **buffers**
- `SystemAPI` accesses the world
- `SystemAPI.Query` can iterate the world
- `LocalTransform` ≈ `GameObject.Transform`

---------

## Task
Given a component:
![Task A struct with public float speed, amplitude, and offset](Resources/TaskAStruct.png)
Write a system creating two entities, changing the position.y in the component `LocalTransform`. Value should move up and down over time.

### Expect Result
![Expected Result showing y going up and down](Resources/TaskAExpectedResult.gif)
### Bonus Task
Make it so the different entities do not peak position.y at the same time.

[EntitiesDocs]: https://docs.unity3d.com/Packages/com.unity.entities@latest