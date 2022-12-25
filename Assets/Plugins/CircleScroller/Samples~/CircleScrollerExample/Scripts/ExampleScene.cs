using CircleScroll;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScene : MonoBehaviour
{
    public CircleScroller[] circleScroller;

    // Start is called before the first frame update
    void Start()
    {
        for (int k = 0; k < this.circleScroller.Length; k++)
        {
            this.circleScroller[k].Clear();

            for (int i = 0; i < 8; i++)
            {
                CircleButtonExampleData circleButtonData = new CircleButtonExampleData();

                circleButtonData.text = i.ToString();
                circleButtonData.clickEvt = () =>
                {
                    Debug.Log(circleButtonData.text);
                };

                this.circleScroller[k].Add(circleButtonData);
            }

            this.circleScroller[k].Refresh();
            this.circleScroller[k].SnapToCenter(1);
        }
    }
}
