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
            List<CircleButtonExampleData> circleButtonDatas = new List<CircleButtonExampleData>();

            for (int i = 0; i < 8; i++)
            {
                CircleButtonExampleData circleButtonData = new CircleButtonExampleData();

                circleButtonData.text = i.ToString();
                circleButtonData.clickEvt = () =>
                {
                    Debug.Log(circleButtonData.text);
                };

                circleButtonDatas.Add(circleButtonData);
            }

            this.circleScroller[k].Init(circleButtonDatas);
            this.circleScroller[k].SnapToCenter(1);
        }
    }
}
