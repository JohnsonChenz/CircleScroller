
# CircleScroller for Unity
A plugin that allows you create gui button with circle layout.

<img src="https://github.com/JohnsonChenz/CircleScroller/blob/master/Docs/ShowCase.gif" height="50%" width="50%"/>

### Install via git URL
Add url below to Package Manager.
``` 
https://github.com/JohnsonChenz/CircleScroller.git?path=Assets/Plugins/CircleScroller
```

## Features:
- Drag control with Mouse/Touch is supported.
- Works on canvas with all render mode(Overlay/Camera/World Space).

## Example:
https://github.com/JohnsonChenz/CircleScroller/tree/master/Assets/Plugins/CircleScroller/Samples~/CircleScrollerExample

**Example is also importable in package**

## Quick Setup:

### 1. Circle Button Prefab :
1. Right click "Create -> CircleScroller -> TplScripts -> TplCircleButton" to create script template of Circle Button.
2. Customize your Circle Button script.
3. Create a ui prefab, add your Circle Button script on it.

### 2. Circle Scroller 
1. Open your scene,find or create a canvas that you prefer to use to render your Circle Scroller,create a empty object under it.
2. Add CircleScroller.cs to the empty object.
3. **\*\*Important\*\*** Choose your canvas find type on the inspector (Find with tag or name).
4. **\*\*Important\*\*** Set the Canvas Find Name according to your canvas find type (Find by tag = canvas's tag name, name = canvas's gameobject name)
5. **\*\*Important\*\*** Assign your circle button prefab to the Circle Button Prefab field.
6. Modified the rest of the values based on your need. (I suggesting user to modified them in in playmode to see the actual display)

### 3. Usage 
```
private CircleScroller _circleScroller;

privtae void _InitCircleScrollerExample()
{
    // Clear data and display first (Optional)
    this._circleScroller.Clear();

    // Create 5 Circle Button
    for (int i = 0; i < 5; i++)
    {
        // Create circle button data
        YourCustomCircleButtonData yourCustomCircleButtonData = new YourCustomCircleButtonData();
        
        /* 
        * Setup your custom circle button data...
        */
        
        // Add circle button data into CircleScroller
        this._circleScroller.Add(yourCustomCircleButtonData);
    }
    
    // Refresh CircleScroller display based on your set-up data.
    this._circleScroller.Refresh();
    
    // Snap to target circle button by specific button index (The index depends on the order of Circle Button List)
    this._circleScroller.SnapToCenter(0);
}
```

## License
This library is under the MIT License.
