## A - Make An Entity
Let's create our very first entity, I'll do a buying system so we can start buying some boats and selecting them.

First let's mock out the data:
![](Resources/A-CodeP1.png)

Underneath, let's make a system that uses the components:
![](Resources/A-CodeP2.png)

Let's create an entity, with the buffer and the component:
![](Resources/A-CodeP3.png)
(Hint: to help debugging use `state.EntityManager.SetName(harbour, "Harbour")` to give your entity a debug name.)

Let's make it so pressing 1-9 selects an index representing a boat shop item in the setup buffer. The index should be between 0-8. Note, pressing 0 gives `-1`, representing a 'none' selection.

Note that `SystemAPI.Query` is an iteration API. It filters the world for any entity containing the component (described by `RefRW`/`RefRO`) named `BuyingStationData` and the buffer (described by `DynamicBuffer`) containing the element `BoatShopItemElement`.
![](Resources/A-CodeP4.png)

Let's make buying a thing. Pressing any selection key 0-9 adds 1 coin. Then we log the created boat.
![](Resources/A-CodeP5.png)

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
Make it so the different entities do not peak position.y at the same time. *(Note, `MoveUpAndDown.offset` should be used for y position offsetting later. So don't use it for solving this Bonus Task.)*

Ask for help if you're stuck, not gonna learn much if you just open the answer :P When done, you can use it to validate your solution. [Task A Solution](https://github.com/Daxode/ECS-Workshop/commit/8c734de184d8cb09f0a23edc1239722d2365d303).

-----
[<- Previous Chapter](1-Intro.md) | [Next Chapter ->](3-Authoring-vs-Runtime.md)

[EntitiesDocs]: https://docs.unity3d.com/Packages/com.unity.entities@latest
