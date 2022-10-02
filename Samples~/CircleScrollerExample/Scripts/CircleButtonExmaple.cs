using CircleScroll;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CircleButtonExampleData
{
    public string text;
    public UnityAction clickEvt;
}

public class CircleButtonExmaple : CircleButtonBase
{
    private Image image;
    private Text text;

    protected override void _InitDisplay()
    {
        CircleButtonExampleData buttonData = this.buttonData as CircleButtonExampleData;
        if (buttonData == null) return;

        this.text.text = buttonData.text;
    }

    protected override void _InitComponents()
    {
        this.image = this.GetComponentInChildren<Image>();
        this.text = this.GetComponentInChildren<Text>();
    }

    protected override void _InitEvents()
    {
        CircleButtonExampleData buttonData = this.buttonData as CircleButtonExampleData;
        if (buttonData == null) return;

        this._RegisterClickCallback(buttonData.clickEvt);

        this._RegisterSelectedCallback(() =>
        {
            this.image.color = new Color32(255, 255, 255, 255);
            Debug.Log($"【OnSelect】{this.name} index : {this.index}");
        });

        this._RegisterInRangeCallback(() =>
        {
            this.image.color = new Color32(255, 255, 255, 255);
            Debug.Log($"【OnInRange】{this.name} index : {this.index}");
        });

        this._RegisterOutRangeCallback(() =>
        {
            this.image.color = new Color32(255, 255, 255, 125);
            Debug.Log($"【OutRange】{this.name} index : {this.index}");
        });
    }
}
