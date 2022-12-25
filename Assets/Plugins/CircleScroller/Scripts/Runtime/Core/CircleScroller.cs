using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CircleScroll
{
    public class CircleScroller : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum CanvasFindType
        {
            FindWithTag = 1,
            FindWithName = 2
        }

        [SerializeField, Tooltip("查找渲染轉盤Canvas的模式")]
        private CanvasFindType canvasFindType = CanvasFindType.FindWithTag;
        [SerializeField, Tooltip("Canvas場景物件的Tag")]
        private string canvasFindName;
        [SerializeField, Tooltip("轉盤按鈕物件")]
        private GameObject circleButtonPrefab;
        [SerializeField, Tooltip("轉盤容器")]
        private Transform container;
        [Tooltip("轉盤半徑大小")]
        public float radius = 100;
        [SerializeField, Tooltip("轉盤定位點弧度值"), Range(-Mathf.PI, Mathf.PI)]
        private float centerRad;
        [Tooltip("X軸視切角大小"), Range(0, 1f)]
        public float sightAngleX = 0;
        [Tooltip("Y軸視切角大小"), Range(0, 1f)]
        public float sightAngleY = 0;
        [SerializeField, Tooltip("轉盤自動平滑定位速度"), Range(0.05f, 1f)]
        private float snapSpeed = 1f;
        [SerializeField, Tooltip("滑鼠拖曳轉盤速度"), Range(1f, 4f)]
        private float dragSpeed = 1f;
        [Tooltip("按鈕縮小倍率"), Range(0, 10f)]
        public float reductionRatio = 1f;
        [SerializeField, Tooltip("是否在拖曳轉盤時排列按鈕階層")]
        private bool sortSiblingWhileDragging = false;
        [Tooltip("是否開啟拖曳效果")]
        public bool enableDrag = true;

        public List<CircleButtonBase> circleButtons { get; private set; }  // 轉盤按鈕物件List
        public List<object> circleButtonDatas { get; private set; }        // 轉盤按鈕物件資料
        public UnityAction<int> snapEvt { get; private set; }              // 按鈕Snap事件
        public int currentSelectedBtnIndex { get; private set; }           // 當前被選中的按鈕Index

        private Canvas mainCanvas;                                         // 渲染轉盤UI的Canvas
        private bool isSnapping;                                           // 是否正在滑動
        private Vector3 curMosuePosition, preMousePosition;                // 滑鼠點擊座標變數

        private const float centerRange = 0.01f;                           // 範圍參數，用於判斷按鈕是否在中心點
        private const float minSnapUpdateTime = 3;                         // 按鈕自動定位最小更新次數
        private const float maxSnapUpdateTime = 10;                        // 按鈕自動定位最大更新次數

        private bool initialized;                                             // 是否完成初始

#if UNITY_EDITOR
        private bool updateFlag;                                           // 是否需要刷新顯示 (用於Inspector調整參數時)
#endif

        public CircleScroller()
        {
            this.initialized = false;
        }

        public void Init()
        {
            if (this.initialized) return;

            // 初始設置Canvas，如果設置失敗就不運行轉盤UI
            if (!this._InitCanvas()) return;

            // 設置滑動點擊事件
            this.snapEvt = (index) => this.SnapToCenter(index);

            this.circleButtonDatas = new List<object>();
            this.circleButtons = new List<CircleButtonBase>();

            // 標記初始已完成，可開始做Update
            this.initialized = true;
        }

        public void Add(object data, bool withRefresh = false)
        {
            if (!this.initialized) this.Init();

            this.circleButtonDatas.Add(data);

            if (withRefresh) this.Refresh();
        }

        public void Remove(int index, bool withRefresh = false)
        {
            if (!this.initialized) this.Init();

            if (this.circleButtonDatas.Count == 0) return;

            this.circleButtonDatas.RemoveAt(index);

            if (withRefresh) this.Refresh();
        }

        public void Clear()
        {
            if (!this.initialized) this.Init();

            this.circleButtonDatas.Clear();

            this.Refresh();
        }

        public void Refresh()
        {
            if (!this.initialized) this.Init();

            // 重設相關設置及顯示
            this._Reset();

            if (this.circleButtonDatas.Count == 0) return;

            // 依據按鈕數量計算出平均弧度
            float averageRad = Mathf.PI * 2 / this.circleButtonDatas.Count;

            // 弧度參數，從0開始
            float addRad = 0;

            for (int i = 0; i < this.circleButtonDatas.Count; i++)
            {
                CircleButtonBase circleButton = this._GetAndInstantiateCircleButton();

                if (circleButton == null) return;

                // 初始化轉盤按鈕
                circleButton.Init(this, i, this.circleButtonDatas[i]);

                // 依照初始弧度設置按鈕位置
                circleButton.AddRad(addRad);

                // 以平均弧度遞增弧度參數
                addRad += averageRad;

                // 將處理好的按鈕加至列表中
                this.circleButtons.Add(circleButton);
            }
        }

        /// <summary>
        /// 使目標Index按鈕滑動至定位點
        /// </summary>
        /// <param name="index"></param>
        public void SnapToCenter(int index)
        {
            this.StopAllCoroutines();

            if (!this.initialized)
            {
                Debug.Log("初始未完成，無法執行SnapToCenter");
                return;
            }

            this.StartCoroutine(this._SnapToCenter(index));
        }

        /// <summary>
        /// 將目標按鈕跳轉至轉盤定位點
        /// </summary>
        /// <param name="target">目標按鈕</param>
        public void JumpToCenter(int index)
        {
            if (!this.initialized)
            {
                Debug.Log("初始未完成，無法執行JumpToCenter");
                return;
            }

            CircleButtonBase targetCircleButton = this._GetCircleButtonByIndex(index);

            if (targetCircleButton == null)
            {
                Debug.Log("無法取得轉盤按鈕");
                return;
            }

            // 算出按鈕移至定位點所需的偏移弧度
            float addRad = this.LimitRadBetweenPositivePIAndNegativePI(this.centerRad - targetCircleButton.rad);

            for (int i = 0; i < this.circleButtons.Count; i++)
            {
                // 代入偏移弧度，移動轉盤按鈕位置
                this.circleButtons[i].AddRad(addRad);

                // 檢查轉盤按鈕是否在轉盤定位點範圍內，並執行對應子類實作方法
                this._CheckIsInRange(this.circleButtons[i]);
            }

            // 結束滑動後，將目標按鈕標記為被選中，並呼叫OnSelect方法
            targetCircleButton.OnSelected();

            // 排列轉盤按鈕階層
            this._SortButtonSibling(this.circleButtons);

            // 紀錄按鈕Index
            this.currentSelectedBtnIndex = targetCircleButton.index;
        }

        /// <summary>
        /// 資源釋放，並銷毀按鈕
        /// </summary>
        public void Release()
        {
            this._Reset();

            this.circleButtons = null;
            this.mainCanvas = null;
        }

        /// <summary>
        /// 限制弧度在PI和-PI之間
        /// </summary>
        /// <param name="rad">弧度值</param>
        /// <returns></returns>
        public float LimitRadBetweenPositivePIAndNegativePI(float rad)
        {
            // 限制弧度在PI和-PI之間
            rad += rad < -3.14f ? Mathf.PI * 2 : 0;
            rad -= rad > 3.14f ? Mathf.PI * 2 : 0;

            return rad;
        }

        /// <summary>
        /// 取得目標與轉盤定位點的距離 (0~1)
        /// </summary>
        /// <param name="targetButtonRag"></param>
        /// <returns></returns>
        public float GetNormalizedDistanceToCenterRad(float targetButtonRag)
        {
            return Mathf.Abs(this.LimitRadBetweenPositivePIAndNegativePI(this.centerRad - targetButtonRag)) / Mathf.PI;
        }

        /// <summary>
        /// 初始渲染轉盤UI的Canvas
        /// </summary>
        /// <returns></returns>
        private bool _InitCanvas()
        {
            switch (this.canvasFindType)
            {
                case CanvasFindType.FindWithTag:

                    this.mainCanvas = GameObject.FindGameObjectWithTag(this.canvasFindName)?.GetComponent<Canvas>();

                    break;

                case CanvasFindType.FindWithName:

                    this.mainCanvas = GameObject.Find(this.canvasFindName)?.GetComponent<Canvas>();

                    break;
            }

            if (this.mainCanvas == null)
            {
                Debug.Log($"<color=#FFED94>查找不到Canvas!! 將無法運行轉盤UI</color>");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 使目標Index按鈕滑動至定位點
        /// </summary>
        /// <param name="target">目標按鈕</param>
        /// <returns></returns>
        private IEnumerator _SnapToCenter(int index)
        {
            CircleButtonBase targetCircleButton = this._GetCircleButtonByIndex(index);

            if (targetCircleButton == null)
            {
                Debug.Log("無法取得轉盤按鈕");
                yield break;
            }

            this.isSnapping = true;

            // 將中心點弧減去按鈕弧度，校正後得出要偏移的弧度量
            float radDistance = this.LimitRadBetweenPositivePIAndNegativePI(this.centerRad - targetCircleButton.rad);

            // 計算Snap更新的次數
            int snapUpdateTime = Convert.ToInt32(Math.Clamp(maxSnapUpdateTime * this.GetNormalizedDistanceToCenterRad(targetCircleButton.rad), minSnapUpdateTime, maxSnapUpdateTime));

            // 將偏移弧度量 / Snap更新次數得出平均偏移弧度量
            float averageAddRad = radDistance / snapUpdateTime;

            // 開始更新顯示
            while (snapUpdateTime > 0)
            {
                for (int i = 0; i < this.circleButtons.Count; i++)
                {
                    // 代入平均偏移弧度，移動轉盤按鈕位置
                    this.circleButtons[i].AddRad(averageAddRad);

                    // 檢查轉盤按鈕是否在轉盤定位點範圍內，並執行對應子類實作方法
                    this._CheckIsInRange(this.circleButtons[i]);
                }

                // 排列轉盤按鈕階層
                this._SortButtonSibling(this.circleButtons);

                snapUpdateTime--;

                // 更新速度最快1毫秒，最慢10毫秒
                yield return new WaitForSecondsRealtime(0.001f / this.snapSpeed);
            }

            // 結束滑動後，將目標按鈕標記為被選中，並呼叫OnSelect方法
            targetCircleButton.OnSelected();

            // 紀錄按鈕Index
            this.currentSelectedBtnIndex = targetCircleButton.index;

            // 關閉滑動標記
            this.isSnapping = false;
        }

        /// <summary>
        /// 拖曳轉盤
        /// </summary>
        private void _DragScroller()
        {
            // 依據滑鼠初始及移動座標算出拖曳的量
            float addRadY = this.curMosuePosition.y - this.preMousePosition.y;
            float addRadX = this.curMosuePosition.x - this.preMousePosition.x;
            //Debug.Log($"AddRadX : {addRadX}, AddRadY : {addRadY}");
            // 比較兩者的位移量，選出大的值做弧度增值，並依照滑鼠點擊座標位置的情況將所增加的弧度值做正負處理
            // 例:滑鼠X座標的位移量，右移為+值，左移為-值，
            // 在轉盤上半部時，滑鼠右移則按鈕應要被往"右"拖，也就是要增加-的弧度值，
            // 但在下半部時，滑鼠右移，按鈕也要被往"右"拖，但是增加的弧度值是要正的，
            // 此時，以滑鼠拖曳的Y座標來判斷滑鼠在轉盤中的相對位置 Y > 0 = 轉盤上半部，Y < 0 = 轉盤下半部
            // 故利用Y座標的正負數符號(Mathf.Sign)乘上X的滑鼠位移量，最終得出正確的弧度增加值。
            // Y座標的操作等同，只是反過來利用X座標判斷滑鼠是在左半部還是右半部
            float addRadTotal = (Mathf.Abs(addRadX) > Mathf.Abs(addRadY) ? -Mathf.Sign(this.curMosuePosition.y) * addRadX : Mathf.Sign(this.curMosuePosition.x) * addRadY) / 500f * this.dragSpeed;

            if (addRadTotal == 0) return;

            for (int i = 0; i < this.circleButtons.Count; i++)
            {
                // 代入偏移弧度，移動轉盤按鈕位置
                this.circleButtons[i].AddRad(addRadTotal);

                // 檢查轉盤按鈕是否在轉盤定位點範圍內，並執行對應子類實作方法
                this._CheckIsInRange(this.circleButtons[i]);
            }

            if (this.sortSiblingWhileDragging) this._SortButtonSibling(this.circleButtons);
        }

        /// <summary>
        /// 找到離定位點最近的轉盤按鈕，並將其滑動到轉盤定位點範圍
        /// </summary>
        private void _AutoSnapToNearestButton()
        {
            // 取得距離定位點最近的轉盤按鈕
            CircleButtonBase circleButton = this._GetClosestCircleButtonToCenter();
            // 如果該按鈕已經被視為選中就不滑動
            if (circleButton.selectStatus == CircleButtonBase.SelectStatus.Selected) return;

            // 將目標按鈕滑至定位點
            this.SnapToCenter(circleButton.index);
        }

        /// <summary>
        /// 判斷目標是否在轉盤定位點範圍
        /// </summary>
        /// <param name="target">目標按鈕</param>
        /// <returns></returns>
        private bool _IsInRange(CircleButtonBase target)
        {
            return this.GetNormalizedDistanceToCenterRad(target.rad) <= centerRange;
        }

        /// <summary>
        /// 取得實例化後的轉盤按鈕
        /// </summary>
        /// <typeparam name="T">用戶實作出的轉盤按鈕型別</typeparam>
        /// <returns></returns>
        private CircleButtonBase _GetAndInstantiateCircleButton()
        {
            GameObject obj = Instantiate(this.circleButtonPrefab, this.container);
            CircleButtonBase circleButtonBase = obj.GetComponent<CircleButtonBase>();
            if (circleButtonBase == null)
            {
                Debug.Log("無法取得正確的轉盤按鈕組件，請檢查Prefab或是代入的型別是否正確!!");
                Destroy(obj);
            }
            return circleButtonBase;
        }

        /// <summary>
        /// 從所有轉盤按鈕中取得離定位點位置最近的的轉盤按鈕
        /// </summary>
        /// <returns></returns>
        private CircleButtonBase _GetClosestCircleButtonToCenter()
        {
            return this.circleButtons.Aggregate((x, y) => this.GetNormalizedDistanceToCenterRad(x.rad) < this.GetNormalizedDistanceToCenterRad(y.rad) ? x : y);
        }

        /// <summary>
        /// 透過Index取得轉盤按鈕
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns></returns>
        private CircleButtonBase _GetCircleButtonByIndex(int index)
        {
            CircleButtonBase circleButton = null;

            try
            {
                circleButton = this.circleButtons[index];
            }
            catch
            {
                Debug.LogError("無法取得CircleButton，請檢查代入之Index是否有誤!!");
            }

            return circleButton;
        }

        /// <summary>
        /// 以轉盤按鈕對於定位點的距離對其UI階層做排序
        /// </summary>
        /// <param name="circleButtons">轉盤按鈕</param>
        private void _SortButtonSibling(List<CircleButtonBase> circleButtons)
        {
            var list = circleButtons.ToList();

            list.Sort((x, y) =>
            {
                // 開始排列，依據轉盤按鈕對於目標定位點的距離，由小排到大
                return this.GetNormalizedDistanceToCenterRad(x.rad).CompareTo(this.GetNormalizedDistanceToCenterRad(y.rad));
            });

            foreach (var circleBtn in list)
            {
                // 依序設置轉盤按鈕物件排列階層
                circleBtn.transform.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// 檢查轉盤按鈕是否在轉盤定位點範圍內，並依照情況執行相關方法
        /// </summary>
        /// <param name="circleButton"></param>
        private void _CheckIsInRange(CircleButtonBase circleButton)
        {
            // 如果轉盤按鈕在轉盤定位點範圍內
            if (this._IsInRange(circleButton))
            {
                circleButton.OnInRange();
            }
            else
            {
                circleButton.OnOutRange();
            }
        }

        /// <summary>
        /// 重新設置參數及顯示
        /// </summary>
        private void _Reset()
        {
            this.isSnapping = false;
            this.curMosuePosition = Vector3.zero;
            this.preMousePosition = Vector3.zero;

            this.circleButtons.Clear();

            if (this.container.childCount > 0)
            {
                foreach (Transform obj in this.container)
                {
                    Destroy(obj.gameObject);
                }
            }
        }

        private Vector3 _GetInputMousePosByCameraRenderType()
        {
            switch (this.mainCanvas.renderMode)
            {
                case RenderMode.WorldSpace:
                case RenderMode.ScreenSpaceCamera:

                    if (this.mainCanvas.worldCamera != null)
                    {
                        return this.mainCanvas.worldCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, this.mainCanvas.transform.localPosition.z));
                    }

                    break;

                case RenderMode.ScreenSpaceOverlay:

                    return Input.mousePosition;
            }

            return Vector2.zero;
        }

        private bool _AbleToDrag()
        {
            return (!this.isSnapping && this.circleButtons?.Count > 0 && this.initialized && this.enableDrag);
        }

        private void OnDestroy()
        {
            this.Release();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (this.initialized) this.updateFlag = true;
        }

        private void LateUpdate()
        {
            if (this.initialized && this.updateFlag)
            {
                this.updateFlag = false;
                this.JumpToCenter(0);
            }
        }
#endif

        #region Drag Handler介面實作
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!this._AbleToDrag())
            {
                this.curMosuePosition = Vector3.zero;
                this.preMousePosition = Vector3.zero;
                return;
            }

            this.curMosuePosition = this.container.InverseTransformPoint(this._GetInputMousePosByCameraRenderType());
            this.preMousePosition = this.container.InverseTransformPoint(this._GetInputMousePosByCameraRenderType());
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!this._AbleToDrag()) return;

            this.curMosuePosition = this.container.InverseTransformPoint(this._GetInputMousePosByCameraRenderType());

            this._DragScroller();

            this.preMousePosition = this.container.InverseTransformPoint(this._GetInputMousePosByCameraRenderType());
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!this._AbleToDrag()) return;

            this._AutoSnapToNearestButton();
        }
        #endregion
    }
}