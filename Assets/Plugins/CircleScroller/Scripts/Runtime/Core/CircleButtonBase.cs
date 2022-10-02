using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CircleScroll
{
    [RequireComponent(typeof(Button))]
    /// <summary>
    /// 轉盤按鈕基底操作類別，使用者可繼承<see cref="CircleButtonBase{TCircleButtonData}"/>
    /// </summary>
    public abstract class CircleButtonBase : MonoBehaviour
    {
        /// <summary>
        /// 轉盤按鈕選中狀態
        /// </summary>
        public enum SelectStatus
        {
            None = 0,
            Selected = 1,
            Deselected = 2,
        }

        /// <summary>
        /// 轉盤按鈕範圍狀態
        /// </summary>
        public enum RangeStatus
        {
            None = 0,
            InRange = 1,
            OutRange = 2,
        }

        public float rad { get; private set; }                 // 轉盤按鈕弧度變數
        public SelectStatus selectStatus { get; private set; } // 選中狀態參數
        public RangeStatus rangeStatus { get; private set; }   // 範圍狀態參數
        public int index { get; private set; }                         // 轉盤按鈕index
        public object buttonData { get; private set; }

        /// <summary>
        /// 註冊按鈕點擊事件
        /// </summary>
        /// <param name="action"></param>
        protected void _RegisterClickCallback(UnityAction action) => this.clickCb = action;

        /// <summary>
        /// 註冊按鈕進入轉盤中心範圍內時觸發事件
        /// </summary>
        protected void _RegisterInRangeCallback(UnityAction action) => this.inRangeCb = action;

        /// <summary>
        /// 註冊按鈕離開轉盤中心範圍時觸發事件
        /// </summary>
        protected void _RegisterOutRangeCallback(UnityAction action) => this.outRangeCb = action;

        /// <summary>
        /// 註冊按鈕被判定選中時觸發事件
        /// </summary>
        protected void _RegisterSelectedCallback(UnityAction action) => this.selectedCb = action;

        /// <summary>
        /// 註冊按鈕移動Update時觸發事件
        /// </summary>
        protected void _RegisterDistanceUpdateCallback(UnityAction<float> action) => this.distanceUpdateCb = action;

        // --- 成員存取
        private CircleScroller circleScroller;       // 轉盤控制組件

        // --- 預設UI組件                            
        private Button button;                       // 內建按鈕

        // --- Callbacks                         
        private UnityAction clickCb;                 // 按鈕點擊Callback
        private UnityAction inRangeCb;               // 按鈕進入轉盤中心範圍Callback
        private UnityAction outRangeCb;              // 按鈕離開轉盤中心範圍Callback
        private UnityAction selectedCb;              // 按鈕判定選中Callback
        private UnityAction<float> distanceUpdateCb; // 按鈕距離更新 Callback

        /// <summary>
        /// 初始化轉盤按鈕
        /// </summary>
        /// <param name="rad">弧度</param>
        /// <param name="radius">半徑</param>
        /// <param name="sightAngle">視切角大小</param>
        /// <param name="snapEvt">預設點擊事件</param>
        public void Init(CircleScroller circleScroller, int index, object buttonData)
        {
            // 先重置各參數
            this._Reset();

            // 指定按鈕資料
            this.buttonData = buttonData;

            // 指定Index
            this.index = index;

            // 指定轉盤控制組件
            this.circleScroller = circleScroller;

            // 初始化組件及按鈕事件
            this._InitOnceComponents();
            this._InitOnceEvents();

            // 代入按鈕資料，初始按鈕顯示
            this._InitDisplay();
        }

        /// <summary>
        /// 依代入弧度值設置按鈕座標
        /// </summary>
        /// <param name="addRad">增加的弧度參數</param>
        public void AddRad(float addRad)
        {
            // 取得校正後的弧度值(將值控制在PI和-PI之間)
            this.rad = this.circleScroller.LimitRadBetweenPositivePIAndNegativePI(this.rad + addRad);

            // 轉換得出X,Y座標，並加乘半徑參數
            float x = Mathf.Cos(this.rad) * this.circleScroller.radius;
            float y = Mathf.Sin(this.rad) * this.circleScroller.radius;

            // 設置座標和大小
            this.transform.localPosition = new Vector2(x * this.circleScroller.sightAngleX, y * this.circleScroller.sightAngleY);
            this.transform.localScale = new Vector3(1 * ((1 - this.circleScroller.GetNormalizedDistanceToCenterRad(this.rad) / this.circleScroller.radius * this.circleScroller.reductionRatio * 20f)), 1 * ((1 - this.circleScroller.GetNormalizedDistanceToCenterRad(this.rad) / this.circleScroller.radius * this.circleScroller.reductionRatio * 20f)), this.transform.localScale.z);

            this.OnMoveUpdate(this.circleScroller.GetNormalizedDistanceToCenterRad(this.rad));
        }

        /// <summary>
        /// 於按鈕在轉盤中心範圍內時觸發，僅觸發一次
        /// </summary>
        public void OnInRange()
        {
            switch (this.rangeStatus)
            {
                case RangeStatus.None:
                case RangeStatus.OutRange:

                    this.rangeStatus = RangeStatus.InRange;
                    this.inRangeCb?.Invoke();

                    break;
                case RangeStatus.InRange:
                    break;
            }
        }

        /// <summary>
        /// 於按鈕在轉盤中心範圍外時觸發，僅觸發一次
        /// </summary>
        public void OnOutRange()
        {
            switch (this.rangeStatus)
            {
                case RangeStatus.OutRange:
                    break;
                case RangeStatus.None:
                case RangeStatus.InRange:

                    this.rangeStatus = RangeStatus.OutRange;
                    this.selectStatus = SelectStatus.Deselected;
                    this.outRangeCb?.Invoke();

                    break;
            }
        }

        /// <summary>
        /// 於按鈕判斷被選中時觸發，僅觸發一次
        /// </summary>
        public void OnSelected()
        {
            switch (this.selectStatus)
            {
                case SelectStatus.Selected:
                    break;
                case SelectStatus.Deselected:
                case SelectStatus.None:

                    this.selectStatus = SelectStatus.Selected;
                    this.selectedCb?.Invoke();

                    break;
            }
        }

        /// <summary>
        /// 於按鈕備點擊時觸發，會依據按鈕被選中與否來執行對應事件
        /// </summary>
        public void OnClick()
        {
            switch (this.selectStatus)
            {
                case SelectStatus.Selected:

                    // 如果被選中，觸發子類實作Click方法
                    this.clickCb?.Invoke();

                    break;
                case SelectStatus.Deselected:
                case SelectStatus.None:

                    // 如果未被選中，即觸發Snap至定位方法
                    this.circleScroller.snapEvt.Invoke(this.index);

                    break;
            }
        }

        public void OnMoveUpdate(float distance)
        {
            this.distanceUpdateCb?.Invoke(distance);
        }

        /// <summary>
        /// 資源釋放
        /// </summary>
        public void Release()
        {
            this.circleScroller = null;
            this.clickCb = null;
            this.inRangeCb = null;
            this.outRangeCb = null;
            this.selectedCb = null;
        }

        /// <summary>
        /// 初始UI組件
        /// </summary>
        private void _InitOnceComponents()
        {
            this.button = this.GetComponent<Button>();
            this._InitComponents();
        }

        /// <summary>
        /// 初始按鈕事件
        /// </summary>
        private void _InitOnceEvents()
        {
            // 註冊按鈕事件
            this.button?.onClick.AddListener(() =>
            {
                this.OnClick();
            });
            this._InitEvents();
        }

        /// <summary>
        /// 重設相關參數
        /// </summary>
        private void _Reset()
        {
            this.selectStatus = SelectStatus.None;
            this.rangeStatus = RangeStatus.None;
            this.rad = 0;
            this.clickCb = null;
        }

        private void OnDestroy()
        {
            this.Release();
        }

        #region 子類實作方法
        /// <summary>
        /// 初始CircleButton顯示
        /// </summary>
        /// <param name="buttonData">CircleButton資料類別</param>
        protected virtual void _InitDisplay() { }

        /// <summary>
        /// 初始綁定基本UI組件
        /// </summary>
        protected virtual void _InitComponents() { }

        /// <summary>
        /// 初始註冊/綁定按鈕事件
        /// </summary>
        protected virtual void _InitEvents() { }
        #endregion
    }
}


