## A - Make An Entity
Let's create our very first entity, I'll do a buying system so we can start buying some boats and selecting them.

First let's mock out the data:
```cs
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public struct BuyingStationData : IComponentData
{
    public int money;
    public int selectedBoat;
}

public struct BoatShopItemElement : IBufferElementData
{
    public int price;
}
```

Underneath, let's make a system that uses the components. _(I recommend typing this out by hand)_:
```cs
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

// ...

public partial struct BuyingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // ...
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // ...
    }
}
```

Let's create an entity, with the buffer and the component:
```cs
// ...
public void OnCreate(ref SystemState state)
{
    // Create an entity during runtime
    var harbour = state.EntityManager.CreateEntity();

    // Add BuyingStationData to the entity `harbour`
    state.EntityManager.AddComponentData(harbour, new BuyingStationData
    {
        money = 0,
        selectedBoat = -1
    });

    // Add buffer containing BoatShopItemElement's to the `harbour` entity
    var boatItems = state.EntityManager.AddBuffer<BoatShopItemElement>(harbour);
    // Add example items to buffer
    boatItems.Add(new BoatShopItemElement { price = 5 });
    boatItems.Add(new BoatShopItemElement { price = 10 });
    boatItems.Add(new BoatShopItemElement { price = 15 });
}
// ...
```
_**Hint:** to help debugging. Use `state.EntityManager.SetName(harbour, "Harbour")` to give your entity a debug name._

Let's make it so pressing 1-9 selects an index representing a boat shop item in the setup buffer. The index should be between 0-8. Note, pressing 0 gives `-1`, representing a 'none' selection.

Note that `SystemAPI.Query` is an iteration API. It filters the world for any entity containing the component (described by `RefRW`/`RefRO`) named `BuyingStationData` and the buffer (described by `DynamicBuffer`) containing the element `BoatShopItemElement`.
```cs
// ...
public void OnUpdate(ref SystemState state)
{
    // Iterate world with EntityQuery filtering for any entity with component BuyingStationData, and a buffer of BoatShopItemElement, 
    // each match, retrieves a tuple of `(RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>)`.
    // C# has `var (val1Named, val2Named) = (val1, val2)` this we use below.
    foreach (var (buyingStation, boatShopItems) in SystemAPI.Query<RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>>())
    {
        // store which number is pressed
        for (var key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                // 1-9 -> 0-8 and 0 -> deselect (-1)
                var index = (int)key - (int)KeyCode.Alpha1;
                if (index < boatShopItems.Length && index != buyingStation.ValueRO.selectedBoat)
                {
                    // select new preview
                    buyingStation.ValueRW.selectedBoat = index;

                    // If not valid index into boat shop buffer, say it has been selected
                    if (index >= 0)
                        Debug.Log($"Selected boat {index}, price: {boatShopItems[index].price}");
                }

                break;
            }
        }
    }
}
```

Let's make buying a thing. Pressing any selection key 0-9 adds 1 coin. Then we log the created boat.
```cs
// ...
public void OnUpdate(ref SystemState state)
{
    // Iterate world with EntityQuery filtering for any entity with component BuyingStationData, and a buffer of BoatShopItemElement, 
    // each match, retrieves a tuple of `(RefRW<BuyingStationData>, DynamicBuffer<BoatShopItemElement>)`.
    // C# has `var (val1Named, val2Named) = (val1, val2)` this we use below.
    foreach (var (...) in SystemAPI.Query<...>())
    {
        // store which number is pressed
        for (...)
        {
            if (Input.GetKeyDown(key))
            {
                // ...

                // Adds coin on any 0-9 press
                buyingStation.ValueRW.money += 1;
                break;
            }
        }

        // buy selected boat
        if (buyingStation.ValueRO.selectedBoat >= 0 && buyingStation.ValueRO.money >= boatShopItems[buyingStation.ValueRO.selectedBoat].price)
        {
            // Get selected boat from buffer
            var selectedBoat = boatShopItems[buyingStation.ValueRO.selectedBoat];

            // Update money left
            buyingStation.ValueRW.money -= selectedBoat.price;
            Debug.Log($"Bought boat {buyingStation.ValueRO.selectedBoat}, price: {selectedBoat.price}");
        }
    }
}
```

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
```cs
struct MoveUpAndDown : ... 
{
    public float speed;
    public float amplitude;
    public float offset;
}
```
Write a system creating two entities, changing the position.y in the component `LocalTransform`. Value should move up and down over time.

### Expect Result
![Expected Result showing y going up and down](Resources/TaskAExpectedResult.gif)
### Bonus Task
1. Make it so the different entities do not peak position.y at the same time. 
2. Additionally add `MoveUpAndDown.offset` to the y position of your `LocalTransform`. _(This will be used later to help authoring inside the editor)_

Ask for help if you're stuck, not gonna learn much if you just open the answer :P When done, you can use it to validate your solution. [Task A Solution](https://github.com/Daxode/ECS-Workshop/commit/8c734de184d8cb09f0a23edc1239722d2365d303).

-----
[<- Previous Chapter](1-Intro.md) | [Next Chapter ->](3-Authoring-vs-Runtime.md)

[EntitiesDocs]: https://docs.unity3d.com/Packages/com.unity.entities@latest
